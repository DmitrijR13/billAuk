using System;
using System.Data;
using System.Collections.Generic;

using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCalcCharge : DataBaseHead
    {
        #region работа с оплатами
        /// <summary>
        /// Учет оплат в сальдо лицевых счетов
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns CalcChargeXXUchetOplat(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, CalcTypes.FunctionType modeType)
        {
            //Вызываю стандартную функцию подсчета сальдо
            Returns ret;
            // bool res = CalcChargeXX(conn_db, paramcalc, out ret, false);
            //Вызываю функцию подсчета сальдо
            CalcChargeForPayment(conn_db, paramcalc, modeType, out ret);

            if (ret.result)
            {
                CalcTypes.ChargeXX chargeXX = new CalcTypes.ChargeXX(paramcalc);
            }

            return ret;
        }

        /// <summary>
        /// Перераспределение переплат
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        protected Returns CalcChargeXXUchetPereplat(IDbConnection conn_db, CalcTypes.ChargeXX chargeXX)
        {
            //Алгоритм следуюущий:
            // 1 - выбрать все строки с sum_outsaldo < 0
            // 2 - по этим лс выбрать суммы по услуге sum_outsaldo > 0
            // 3 - выбратьм max(sum_outsaldo>0), чтобы туда переложить минус
            // 4 - переложить в пределах суммы переплаты не более суммы > 0 (заполнить sum_del)
            // 5 - цикл повторяется (sum_del увеличивается), пока есть куда перекладывать
            // anes

            MyDataReader reader;

            Returns ret = ExecRead(conn_db, out reader,
                " Select nzp_prm " +
                " From " + chargeXX.paramcalc.data_alias + "prm_5 p " +
                " Where p.nzp_prm = 1996 and p.val_prm='1' " +
                "   and p.is_actual <> 100 and p.dat_s  <= " + chargeXX.paramcalc.dat_po +
                "   and p.dat_po >= " + chargeXX.paramcalc.dat_s + " "
                , true);
            if (!ret.result) return ret;

            bool b_make_del_supplier = false;
            if (reader.Read())
            {
                b_make_del_supplier = true;
            }
            reader.Close();

            if (!b_make_del_supplier) return ret;

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
                " ) " + sUnlogTempTable
                , true);
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
            ExecSQL(conn_db, sUpdStat + " t_perek ", true);

            try
            {
                //загоним положительные суммы по услуге
                ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

                ret = ExecSQL(conn_db,
                    " Select ch.nzp_kvar,ch.num_ls,ch.nzp_supp,ch.nzp_serv,ch.nzp_charge, 0 sum_del, ch.sum_outsaldo " +
#if PG
 " Into temp t_ins_ch1  " +
                    " From " + chargeXX.charge_xx + " ch, t_selkvar k " +
                    " Where ch.nzp_kvar=k.nzp_kvar "
                     + chargeXX.paramcalc.per_dat_charge +
                    "   and ch.sum_outsaldo > 0 " +
                    "   and 0 < ( Select count(*) From t_perek gk " +
                                " Where gk.nzp_kvar = ch.nzp_kvar " +
                                "   and gk.nzp_serv = ch.nzp_serv ) "
#else
                    " From " + chargeXX.charge_xx + " ch, t_selkvar k " +
                    " Where ch.nzp_kvar=k.nzp_kvar "
                     + chargeXX.paramcalc.per_dat_charge +
                    "   and ch.sum_outsaldo > 0 " +
                    "   and 0 < ( Select count(*) From t_perek gk " +
                                " Where gk.nzp_kvar = ch.nzp_kvar " +
                                "   and gk.nzp_serv = ch.nzp_serv ) " +
                    " Into temp t_ins_ch1 With no log "
#endif
, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " Insert into t_perek (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del,sum_out) " +
                    " Select nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del, sum_outsaldo " +
                    " From t_ins_ch1 "
                    , true);
                if (!ret.result) return ret;

                ExecSQL(conn_db, sUpdStat + " t_perek ", true);

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
                    ret = ExecSQL(conn_db,
                        " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
#if PG
 " Into temp t_delmin " +
                        " From t_perek a " +
                        " Where (sum_out+sum_del) = ( Select min(sum_out+sum_del) From t_perek b " +
                                                    " Where a.nzp_kvar = b.nzp_kvar " +
                                                    "   and a.nzp_serv = b.nzp_serv " +
                                                    "   and sum_out+sum_del < 0 ) " +
                        "   and sum_out+sum_del < 0 " +
                        " Group by 1,2 "
#else
                        " From t_perek a " +
                        " Where (sum_out+sum_del) = ( Select min(sum_out+sum_del) From t_perek b " +
                                                " Where a.nzp_kvar = b.nzp_kvar " +
                                                "   and a.nzp_serv = b.nzp_serv " +
                                                "   and sum_out+sum_del < 0 ) " +
                        "   and sum_out+sum_del < 0 " +
                        " Group by 1,2 " +
                        " Into temp t_delmin With no log "
#endif
, true);
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Create unique index ix_t_delmin on t_delmin (nzp_kvar,nzp_serv) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_delmin ", true);

                    //ищем максимальный плюс
                    ret = ExecSQL(conn_db,
                        " Select nzp_kvar, nzp_serv, max(nzp_charge) as nzp_charge, max(sum_out+sum_del) as sum_del " +
#if PG
 " Into temp t_delplu " +
                        " From t_perek a " +
                        " Where (sum_out+sum_del) = ( Select max(sum_out+sum_del) From t_perek b " +
                                                    " Where a.nzp_kvar = b.nzp_kvar " +
                                                    "   and a.nzp_serv = b.nzp_serv " +
                                                    "   and sum_out+sum_del > 0 ) " +
                        "   and sum_out+sum_del > 0 " +
                        " Group by 1,2 "
#else
                        " From t_perek a " +
                        " Where (sum_out+sum_del) = ( Select max(sum_out+sum_del) From t_perek b " +
                                                " Where a.nzp_kvar = b.nzp_kvar " +
                                                "   and a.nzp_serv = b.nzp_serv " +
                                                "   and sum_out+sum_del > 0 ) " +
                        "   and sum_out+sum_del > 0 " +
                        " Group by 1,2 " +
                        " Into temp t_delplu With no log "
#endif
, true);
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Create unique index ix_t_delplu on t_delplu (nzp_kvar,nzp_serv) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_delplu ", true);

                    //соединяем между собой эти таблицы и вычисляем дельты в пределах общих абсолютных значениях

                    ret = ExecSQL(conn_db,
                        " Select a.nzp_charge as nzp_min, b.nzp_charge as nzp_plu, " +
                        " case when a.sum_del + b.sum_del > 0 then abs(a.sum_del) else b.sum_del end as sum_dlt " +
#if PG
 " Into temp t_ins_ch1 " +
                        " From t_delmin a, t_delplu b " +
                        " Where a.nzp_kvar = b.nzp_kvar " +
                        "   and a.nzp_serv = b.nzp_serv "
#else
                        " From t_delmin a, t_delplu b " +
                        " Where a.nzp_kvar = b.nzp_kvar " +
                        "   and a.nzp_serv = b.nzp_serv " +
                        " Into temp t_ins_ch1 With no log "
#endif
, true);
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Create index ix_t_ins_ch1_1 on t_ins_ch1 (nzp_min) ", true);
                    ExecSQL(conn_db, " Create index ix_t_ins_ch1_2 on t_ins_ch1 (nzp_plu) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_ins_ch1 ", true);

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
                    ret = ExecSQL(conn_db,
                        " Insert into " + chargeXX.paramcalc.pref + "_charge_" + (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "del_supplier" +
                          " (num_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_account, dat_calc) " +
                        " Select num_ls, nzp_serv, nzp_supp, sum_del, 39, " + MDY(chargeXX.paramcalc.calc_mm, 28, chargeXX.paramcalc.calc_yy) + "," + sCurDate +
                        " From t_perek " +
                        " Where abs(sum_del) > 0.000001 "
                        , true);
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
        public Returns CalcChargeXXUchetOplatForPack(IDbConnection conn_db, CalcTypes.PackXX packXX)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcChargeXXUchetOplatForPack");

            Returns ret = Utils.InitReturns();

            List<string> prefs = new List<string>();
            if (packXX.paramcalc.pref == "")
            {
                string sql =
                    " select " + sUniqueWord + " pref" +
                    " from " + Points.Pref + "_data" + tableDelimiter + "kvar k, " +
                               Points.Pref + "_fin_" + (packXX.paramcalc.calc_yy % 100).ToString("00") + tableDelimiter + "pack_ls p" +
                    " where p.num_ls = k.num_ls and p.nzp_pack = " + packXX.nzp_pack;

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

                ret = CalcChargeXXUchetOplat(conn_db, packXX.paramcalc, CalcTypes.FunctionType.Payment);
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
        public Returns CalcChargeXXUchetOplatForLs(IDbConnection conn_db, IDbTransaction transaction, Charge finder, CalcTypes.FunctionType modeType)
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
                string sql =
                    " select nzp_kvar" +
                    " from " + Points.Pref + "_data" + tableDelimiter + "kvar" +
                    " where num_ls = " + finder.num_ls;

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

            CalcTypes.ParamCalc paramCalc = new CalcTypes.ParamCalc(finder.nzp_kvar, 0, finder.pref, finder.year_, finder.month_, finder.year_, finder.month_);

            paramCalc.b_reval = false;
            paramCalc.b_must = false;

            ChoiseTempKvar(conn_db, ref paramCalc, false, out ret);
            if (!ret.result) return ret;

            ret = CalcChargeXXUchetOplat(conn_db, paramCalc, modeType);
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
        public Returns CalcChargeXXUchetOplatForArea(IDbConnection conn_db, IDbTransaction transaction, Charge finder, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
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
                string sql =
                    " select " + sUniqueWord + " pref" +
                    " from " + Points.Pref + "_data" + tableDelimiter + "kvar k " +
                    (finder.nzp_area > 0 ? " where k.nzp_area = " + finder.nzp_area : "");

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

            #region вызов по префиксам
            int cnt = prefs.Count;
            int i = 0;
            foreach (string pref in prefs)
            {
                DropTempTablesPack(conn_db);
                CalcTypes.ParamCalc paramCalc = new CalcTypes.ParamCalc(0, 0, pref, finder.year_, finder.month_, finder.year_, finder.month_);
                paramCalc.nzp_area = finder.nzp_area;

                paramCalc.b_reval = false;
                paramCalc.b_must = false;

                ChoiseTempKvar(conn_db, ref paramCalc, false, out ret);
                if (!ret.result) return ret;

                ret = CalcChargeXXUchetOplat(conn_db, paramCalc, CalcTypes.FunctionType.Payment);
                if (!ret.result) return ret;

                i++;
                if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            }
            #endregion вызов по префиксам

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции CalcChargeXXUchetOplatForArea");

            return ret;
        }

        /// <summary>
        /// Выполняет учет оплат для заданного банка данных
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="packXX">Если выбран банк, то по выбранному банку</param>
        /// <param name="nzp_pack">Если указан код пачки, то по ЛС из этих пачек</param>
        /// <returns></returns>
        public Returns CalcChargeXXUchetOplatForBank(IDbConnection conn_db, string pref, Pack pack)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcChargeXXUchetOplatForBank");

            if (pref == "")
            {
                return new Returns(false, "Не задан банк данных");
            }

            DropTempTablesPack(conn_db);
            CalcTypes.ParamCalc paramCalc = new CalcTypes.ParamCalc(0, 0, pref, Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Year, Points.DateOper.Month);
            paramCalc.nzp_pack_saldo = pack.nzp_pack;
            paramCalc.nzp_par_pack = pack.par_pack;
            paramCalc.b_reval = false;
            paramCalc.b_must = false;

            Returns ret;
            ChoiseTempKvar(conn_db, ref paramCalc, false, out ret);
            if (!ret.result) return ret;

            ret = CalcChargeXXUchetOplat(conn_db, paramCalc, CalcTypes.FunctionType.Payment);
            if (!ret.result) return ret;

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции CalcChargeXXUchetOplatForBank");

            return ret;
        }

        /// <summary>
        /// Выполняет учет оплат для заданной Управляющая организация
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="packXX"></param>
        /// <returns></returns>
        public Returns CalcChargeXXForReestrPerekidok(IDbConnection conn_db, IDbTransaction transaction, ParamsForGroupPerekidki finder, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcChargeXXForReestrPerekidok");

            Returns ret = new Returns();
            int year = Convert.ToDateTime(finder.dat_uchet).Year;
            int month = Convert.ToDateTime(finder.dat_uchet).Month;
            #region получить список префиксов
            //------------------------------------------------------------------------------------------------------------------------------------
            List<string> prefs = new List<string>();
            if (finder.pref == "")
            {


                foreach (_Point p in Points.PointList)
                {
                    string perekidka = p.pref + "_charge_" + (year % 100).ToString("00") + tableDelimiter + "perekidka";
                    if (!TempTableInWebCashe(conn_db, perekidka)) continue;
                    string sql = "select count(*) as cnt from " + perekidka + " where nzp_reestr = " + finder.nzp_reestr;
                    object count = ExecScalar(conn_db, sql, out ret, true);
                    int total_record_count = 0;
                    if (ret.result)
                    {
                        try
                        {
                            total_record_count = Convert.ToInt32(count);
                        }
                        catch (Exception ex)
                        {
                            ret.result = false;
                            ret.text = ex.Message;
                            return ret;
                        }
                    }
                    else return ret;
                    if (total_record_count > 0) prefs.Add(p.pref);
                }
            }
            else prefs.Add(finder.pref);
            //------------------------------------------------------------------------------------------------------------------------------------
            #endregion

            #region вызов по префиксам
            int cnt = prefs.Count;
            int i = 0;
            foreach (string pref in prefs)
            {
                DropTempTablesPack(conn_db);
                CalcTypes.ParamCalc paramCalc = new CalcTypes.ParamCalc(0, 0, pref, year, month, year, month);
                paramCalc.nzp_reestr = finder.nzp_reestr;

                paramCalc.b_reval = false;
                paramCalc.b_must = false;

                ChoiseTempKvar(conn_db, ref paramCalc, false, out ret);
                if (!ret.result) return ret;

                ret = CalcChargeXXUchetOplat(conn_db, paramCalc, CalcTypes.FunctionType.Perekidki);
                if (!ret.result) return ret;

                i++;
                if (setTaskProgress != null) setTaskProgress(((decimal)i) / cnt);
            }
            #endregion вызов по префиксам

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции CalcChargeXXForReestrPerekidok");

            return ret;
        }

        public Returns CalcChargeXXForDelReestrPerekidok(IDbConnection conn_db, IDbTransaction transaction, ParamsForGroupPerekidki finder, string temtable)
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции CalcChargeXXForDelReestrPerekidok");

            Returns ret = new Returns();
            int year = Convert.ToDateTime(finder.dat_uchet).Year;
            int month = Convert.ToDateTime(finder.dat_uchet).Month;
            #region получить список префиксов
            //------------------------------------------------------------------------------------------------------------------------------------
            List<string> prefs = new List<string>();

            string sql = "select distinct pref from " + temtable;

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

            //------------------------------------------------------------------------------------------------------------------------------------
            #endregion

            #region вызов по префиксам
            int cnt = prefs.Count;

            foreach (string pref in prefs)
            {
                DropTempTablesPack(conn_db);
                CalcTypes.ParamCalc paramCalc = new CalcTypes.ParamCalc(0, 0, pref, year, month, year, month)
                {
                    nzp_reestr = finder.nzp_reestr,
                    temp_table = temtable,
                    b_reval = false,
                    b_must = false
                };


                ChoiseTempKvar(conn_db, ref paramCalc, false, out ret);
                if (!ret.result) return ret;

                ret = CalcChargeXXUchetOplat(conn_db, paramCalc, CalcTypes.FunctionType.Perekidki);
                if (!ret.result) return ret;
            }
            #endregion вызов по префиксам

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции CalcChargeXXForReestrPerekidok");

            return ret;
        }


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
        #endregion работа с оплатами

        /// <summary>
        /// Функция подсчета сальдо
        /// </summary>
        /// <param name="conn_db"> Соединение</param>
        /// <param name="paramcalc">Параметры расчета(распределения оплат) { nzp_pack_saldo - номер пачки, pref - префикс бд, cur_yy - год, cur_mm - месяц, nzp_kvar - лс  }</param>
        /// <param name="Mode">Режим (Перекидки\Оплаты)</param>
        /// <param name="ret">Результат выполнения операции</param>
        protected void CalcChargeForPayment(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, CalcTypes.FunctionType Mode, out Returns ret)
        {
            CalcTypes.ChargeXX chargeXX = new CalcTypes.ChargeXX(paramcalc);

            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_selkvar"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result) { conn_db.Close(); return; }
            }
            CreateChargeXX(conn_db, chargeXX, false, out ret);
            if (!ret.result) { return; }
            var sql = "";
            if (!chargeXX.paramcalc.b_reval)
            {
                if (Mode == CalcTypes.FunctionType.Payment)
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
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    UpdateStatistics(false, paramcalc, "del_supplier", out ret);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка выборки данных ";
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    #endregion чистка del_supplier

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
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    string sfind_tmp =
                        " Insert Into t_fn_suppall (num_ls,nzp_supp,nzp_serv,sum_prih) " +
                        " Select num_ls,nzp_supp,nzp_serv,sum_prih " +
                        " From " + chargeXX.fn_supplier +
                        " Where abs(sum_prih) > 0.0001 and nzp_serv <> 1 ";

                    ret = ExecSQL(conn_db, sfind_tmp, true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    ExecSQL(conn_db, " Create index ix_t_fn_suppall on t_fn_suppall (num_ls) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_fn_suppall ", true);
  
                    ret = ExecSQL(conn_db,
                        " Create temp table t_fn_supp " +
                        " ( nzp_kvar  integer, " +
                        "   num_ls    integer, " +
                        "   nzp_supp  integer, " +
                        "   nzp_serv  integer, " +
                        "   money_to " + sDecimalType + "(14,2) " +
                        " )  " + sUnlogTempTable
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    ret = ExecSQL(conn_db,
                        " Insert Into t_fn_supp (nzp_kvar,num_ls,nzp_supp,nzp_serv,money_to) " +
                        " Select k.nzp_kvar,k.num_ls,nzp_supp,nzp_serv,sum(sum_prih) as money_to " +
                        " From t_fn_suppall a, t_selkvar k " +
                        " Where a.num_ls = k.num_ls " +
                        " Group by 1,2,3,4 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    ExecSQL(conn_db, " Create unique index ix_t_fn_supp on t_fn_supp (nzp_kvar,nzp_serv,nzp_supp) ",
                        true);
                    ExecSQL(conn_db, sUpdStat + " t_fn_supp ", true);

                    //from_supplier
                    ret = ExecSQL(conn_db,
                        " Create temp table t_from_supp " +
                        " ( nzp_kvar  integer, " +
                        "   num_ls    integer, " +
                        "   nzp_supp  integer, " +
                        "   nzp_serv  integer, " +
                        "   money_from " + sDecimalType + "(14,2) " +
                        " )  " + sUnlogTempTable
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

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
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    ExecSQL(conn_db, " Create unique index ix_t_from_supp on t_from_supp (nzp_kvar,nzp_serv,nzp_supp) ",
                        true);
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
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

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
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    ExecSQL(conn_db, " Create unique index ix_t_del_supp on t_del_supp (nzp_kvar,nzp_serv,nzp_supp) ",
                        true);
                    ExecSQL(conn_db, sUpdStat + " t_del_supp ", true);

                    #endregion выборка оплат
                }

                if (Mode == CalcTypes.FunctionType.Perekidki)
                {
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
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

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
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    ExecSQL(conn_db, " Create unique index ix_t_perek on t_perek (nzp_kvar,nzp_supp,nzp_serv) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_perek ", true);

                    #endregion выборка перекидок
                }
            }

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
                return;
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
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            if (!chargeXX.paramcalc.b_reval)
            {
                if (Mode == CalcTypes.FunctionType.Payment)
                {
                    ret = ExecSQL(conn_db,
                        " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                        " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_fn_supp group by 1,2,3,4 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    ret = ExecSQL(conn_db,
                        " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                        " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_from_supp group by 1,2,3,4 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }

                    ret = ExecSQL(conn_db,
                        " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                        " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_del_supp group by 1,2,3,4 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }
                }
                if (Mode == CalcTypes.FunctionType.Perekidki)
                {
                    ret = ExecSQL(conn_db,
                        " Insert into t_ins_ch1 (nzp_kvar,num_ls,nzp_supp,nzp_serv)  " +
                        " Select nzp_kvar,num_ls,nzp_supp,nzp_serv From t_perek group by 1,2,3,4 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
                    }
                }
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
                return;
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
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

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
                return;
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
                return;
            }

            ExecSQL(conn_db, " Drop table t_ins_ch1 ", false);

            #endregion формирование записей для вставки

            #region вставка дополнительных записей в chargeXX
            //вставка недостающих строк в charge_xx
            string p_dat_charge = DateNullString;
            if (!chargeXX.paramcalc.b_cur)
                p_dat_charge = MDY(chargeXX.paramcalc.cur_mm, 28, chargeXX.paramcalc.cur_yy);

            sql =
                " Insert into " + chargeXX.charge_xx +
                   " ( dat_charge, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, tarif_p, gsum_tarif, rsum_tarif, rsum_lgota,  sum_dlt_tarif, sum_dlt_tarif_p, " +
                     " sum_tarif_p,sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, reval, real_pere, sum_pere, " +
                     " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                     " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                     " c_nedop, isdel, c_reval, order_print) " +
                " Select " + p_dat_charge + ", nzp_kvar,num_ls, nzp_serv, nzp_supp, " +
                      "  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 " +
                " From t_ins_ch Where kod = 0 ";
            if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "t_ins_ch", "nzp_key", sql, 50000, " ", out ret);
            }

            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            UpdateStatistics(false, paramcalc, chargeXX.charge_tab, out ret);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            #endregion вставка дополнительных записей в chargeXX

            #region очищение полей ChargeXX

            var sqql = "";
            if (Mode == CalcTypes.FunctionType.Payment)
            {
                sqql = " Update " + chargeXX.charge_xx + " Set" +
                       " money_to = 0, money_from = 0, money_del = 0, sum_fakt = 0 " +
                       " Where exists(Select 1 From t_selkvar ts " +
                       " Where ts.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar) ";

                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
                }

            }
            else
            {
                sqql = " Update " + chargeXX.charge_xx + " Set" +
                     " real_charge = 0 " +
                     " Where exists(Select 1 From t_selkvar ts " +
                       " Where ts.nzp_kvar = " + chargeXX.charge_xx + ".nzp_kvar) ";

                ret = ExecSQL(conn_db, sqql, true);
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
                }
            }
            #endregion

            if (Mode == CalcTypes.FunctionType.Payment)
            {
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
                       + " and " + chargeXX.where_kvar; 

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
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
                       + " and " + chargeXX.where_kvar; 

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
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
                       + " and " + chargeXX.where_kvar;

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
                }
            }
            if (Mode == CalcTypes.FunctionType.Perekidki)
            {
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
                       + " and " + chargeXX.where_kvar; 

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
                }

            }
            if (!chargeXX.paramcalc.b_reval)
            {
                #region расчет сальдо

                //---------------------------------------------------
                //расчет сальдо
                //---------------------------------------------------
                sqql = " Update " + chargeXX.charge_xx +
                       " Set sum_money = money_to + money_from + money_del " +
                       " ,sum_outsaldo = sum_insaldo + real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                       " ,sum_pere  = sum_insaldo + reval + real_charge - (money_to + money_from + money_del) " +
                       " Where " + chargeXX.where_kvar +
                       chargeXX.paramcalc.per_dat_charge;

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, chargeXX.charge_xx, "nzp_charge",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
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
                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
                }
                bool b_make_del_supplier = reader.Read();
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
                        return;
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
                        return;
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
 " Into temp t_ins_ch1 " +
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
                        return;
                    }
                    ret = ExecSQL(conn_db,
                        " Insert into t_perek (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del,sum_out) " +
                        " Select nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_charge, sum_del, sum_outsaldo " +
                        " From t_ins_ch1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesCharge(conn_db);
                        return;
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
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return;
                        }
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
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return;
                        }
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
                        if (!ret.result)
                        {
                            DropTempTablesCharge(conn_db);
                            return;
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
                            return;
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
                            return;
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
                                return;
                            }
                            ret = ExecSQL(conn_db,
                            " Update t_perek " +
                            " Set sum_del = sum_del - ( Select sum_dlt From t_ins_ch1 Where t_perek.nzp_charge = t_ins_ch1.nzp_plu ) " +
                            " Where nzp_charge in ( Select nzp_plu From t_ins_ch1 ) "
                            , true);
                            if (!ret.result)
                            {
                                DropTempTablesCharge(conn_db);
                                return;
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
                            return;
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
                            return;
                        }

                        if (Mode == CalcTypes.FunctionType.Payment)
                        {
                            //загнать следы в del_supplier
                            ret = ExecSQL(conn_db,
                                " Insert into " + chargeXX.paramcalc.pref + "_charge_" +
                                (chargeXX.paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "del_supplier" +
                                " (num_ls, nzp_serv, nzp_supp, sum_prih, kod_sum, dat_account, dat_calc) " +
                                " Select num_ls, nzp_serv, nzp_supp, sum_del, 39, " +
                                MDY(chargeXX.paramcalc.calc_mm, 28, chargeXX.paramcalc.calc_yy) + "," + sCurDate +
                                " From t_perek " +
                                " Where abs(sum_del) > 0.000001 "
                                , true);
                            if (!ret.result)
                            {
                                DropTempTablesCharge(conn_db);
                                return;
                            }
                        }
                    }
                }
                // ... end anes - отмена перераспределения оплат
                #endregion перераспределение переплат

                #region начислено к оплате

                bool bDoExecStep = !(chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0);
                string sWhere = chargeXX.where_kvar; //nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
                string sDatCharge = chargeXX.paramcalc.per_dat_charge;
                string sChargeName = chargeXX.charge_xx;

                CalcSumToPay(conn_db, chargeXX.paramcalc.cur_mm, chargeXX.paramcalc.cur_yy, chargeXX.paramcalc.pref, sChargeName, sWhere, sDatCharge, bDoExecStep, out ret);
                if (!ret.result) { DropTempTablesCharge(conn_db); return; }
                #endregion начислено к оплате

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
                                  " sum_charge   " + sDecimalType + "(14,2) default 0.00" +
                                  " )  " + sUnlogTempTable
                                  , true);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
                }

                //ret = ExecSQL(conn_db,
                sqql =
                    " Insert into t_itog " +
                      "(nzp_kvar,num_ls,rsum_tarif,izm_saldo,real_pere,sum_pere,sum_money,money_to," +
                       " money_from,money_del,sum_outsaldo,sum_insaldo,real_charge,sum_real,sum_charge)" +
                    " Select k.nzp_kvar, k.num_ls, " +
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
                       " sum(sum_charge) as sum_charge" +
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
                    return;
                }
                ExecSQL(conn_db, " Create unique index ix1_t_itog on t_itog (nzp_key) ", true);
                ExecSQL(conn_db, " Create unique index ix2_t_itog on t_itog (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_itog ", true);

                sqql = " Update " + chargeXX.charge_xx + " set  dat_charge = ti.dat_charge, rsum_tarif = ti.rsum_tarif, izm_saldo = ti.izm_saldo, real_pere = ti.real_pere, " +
                " sum_pere = ti.sum_pere,  sum_insaldo = ti.sum_insaldo, sum_outsaldo = ti.sum_outsaldo, " +
                " sum_real = ti.sum_real,  sum_charge = ti.sum_charge, " +
                " real_charge = ti.real_charge, sum_money = ti.sum_money, money_to = ti.money_to, money_from = ti.money_from, money_del = ti.money_del " +
                " from " +
                " (Select nzp_kvar," + p_dat_charge + " dat_charge, rsum_tarif,  izm_saldo, real_pere, sum_pere,  " +
                " sum_insaldo, sum_outsaldo, sum_real,  sum_charge, real_charge, sum_money, money_to, money_from, money_del  " +
                "  From t_itog) ti Where " + chargeXX.charge_xx + ".nzp_serv = 1 and " + chargeXX.charge_xx + ".nzp_kvar = ti.nzp_kvar ";

                if (chargeXX.paramcalc.nzp_kvar > 0 || chargeXX.paramcalc.nzp_dom > 0)
                    ret = ExecSQL(conn_db, sqql, true);
                else
                    ExecByStep(conn_db, "t_itog", "nzp_key",
                        sqql, 50000, " ", out ret);

                if (!ret.result)
                {
                    DropTempTablesCharge(conn_db);
                    return;
                }
                #endregion расчет итого - nzp_serv = 1
            }

            #region обновление статистики

            UpdateStatistics(false, paramcalc, chargeXX.charge_tab, out ret);
            if (!ret.result)
            {
                DropTempTablesCharge(conn_db);
                return;
            }

            DropTempTablesCharge(conn_db);
            return;

            #endregion обновление статистики
        }
    }
}
