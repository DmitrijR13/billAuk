// Подсчет расходов

using System.Collections.Generic;
using System.Linq;
using Bars.KP50.Utils;
using Bars.KP50.Utils.Annotations;

#region Подключаемые модули

using System;
using System.Data;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;

#endregion Подключаемые модули

#region здесь производится подсчет расходов

namespace STCLINE.KP50.DataBase
{

    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {

        public bool CalcRashodNorm(IDbConnection conn_db, Rashod rashod,
            bool bIsSaha, bool b_calc_kvar, bool bIsCalcSubsidyBill,
            string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region ... формирование нормативов ...

            // Поправки в данные для коммуналок по ЭлЭн и сохранение стека 29 - если расчет дома или БД
            SetAndSaveTempRashodKommunal(conn_db, rashod, b_calc_kvar, bIsSaha, p_dat_charge, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            if (Points.isNewNorms)
            {
                //получаем значения нормативов по новому алгоритму
                GetNewNorms(conn_db, rashod, p_dat_charge, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка получения нормативов по новому алгоритму GetNewNorms: " +
                   ret.text, MonitorLog.typelog.Error, 1, 2, true);
                    return false;
                }
            }
            else
            {
                //N: электроснабжение
                CalcRashodNormElEn(conn_db, rashod, bIsSaha, b_calc_kvar, p_dat_charge, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //N: хвс гвс канализация
                CalcRashodNormHVandKAN(conn_db, rashod, bIsSaha, b_calc_kvar, bIsCalcSubsidyBill, p_dat_charge, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //N: газ 
                CalcRashodNormGas(conn_db, rashod, bIsSaha, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            //N: электроотопление
            CalcRashodNormElOtopl(conn_db, rashod, bIsSaha, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //N: газовое отопление
            CalcRashodNormGasOtopl(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //N: полив
            CalcRashodNormPoliv(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //N: отопление
            CalcRashodNormOtopl(conn_db, rashod, bIsSaha, b_calc_kvar, bIsCalcSubsidyBill, p_dat_charge, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //N: вода для бани
            CalcRashodNormHVforBanja(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //N: питьевая вода
            CalcRashodNormPitHV(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion ... формирование нормативов ...

            // Учет нормативов по дням
            CalcRashodNormPoDn(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //копируем val1,val4 в val1_source,val4_source - для сохранения нормативных расходов без учета повышающего коэф.
            CopySourceVal(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //применяем повышающий коэффициент для расхода по нормативам 
            ApplyUpKoef(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // сохранить ttt_counters_xx (нормативы!) в стек 30 для Анэса
            CalcRashodNormInsInStek30(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }

        #region N: электроснабжение
        public bool CalcRashodNormElEn(IDbConnection conn_db, Rashod rashod, bool bIsSaha, bool b_calc_kvar, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            var iNzpPrmEE = 730;
            if ((new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 01)) >= (new DateTime(2012, 09, 01)))
            {
                iNzpPrmEE = 1079;
            }

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set cnt4 = " + iNzpPrmEE + //дом не-МКД (по всем нормативным строкам лс и домам)
                " Where nzp_serv in (25,210,410) " +
                "   and 0 < (" +
                          " Select count(*) From ttt_prm_2 p " +
                          " Where ttt_counters_xx.nzp_dom = p.nzp " +
                          "   and p.nzp_prm = " + iNzpPrmEE +
                          " ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_aid_c1 (nzp_kvar integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_c1 (nzp_kvar)" +
                  " Select nzp as nzp_kvar " +
                  " From ttt_counters_xx a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and a.nzp_serv in (25,210,410) " +
                  "   and p.nzp_prm = 19 " + // электроплита
                  " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_aid_c1x (nzp_kvar integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_c1x (nzp_kvar)" +
                " Select nzp_kvar From ttt_aid_c1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1x on ttt_aid_c1x (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1x ", true);

            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_c1 (nzp_kvar)" +
                  " Select a.nzp_kvar " +
                  " From ttt_counters_xx a, ttt_prm_2 p " +
                  " Where a.nzp_dom = p.nzp " +
                  "   and a.nzp_serv in (25,210,410) " +
                  "   and p.nzp_prm = 28 " + // электроплита на дом
                  "   and 0 = ( Select count(*) From ttt_aid_c1x k Where a.nzp_kvar = k.nzp_kvar ) " +
                  " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);

            int iNzpRes;
            var iNzpResMKD = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 181, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
            if (bIsSaha)
            {
                iNzpRes = iNzpResMKD;
            }
            else
            {
                iNzpRes = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 183, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
            }
            if ((iNzpRes != 0) && (iNzpResMKD != 0))
            {
                ret = ExecSQL(conn_db,
                    " Update ttt_counters_xx " +
                    " Set cnt3 = (case when cnt4 = " + iNzpPrmEE + " then " + iNzpRes + " else " + iNzpResMKD + " end) " + //лс с электроплитой (в зависимости от МКД)
                    " Where nzp_serv in (25,210,410) " +
                    "   and 0 < ( Select count(*) From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            if (bIsSaha)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);
                ret = ExecSQL(conn_db,
                    " create temp table ttt_aid_c1x (nzp_kvar integer) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_c1x (nzp_kvar)" +
                    " Select a.nzp_kvar " +
                    " From ttt_counters_xx a, ttt_prm_1 p " +
                    " Where a.nzp_kvar = p.nzp " +
                    "   and a.nzp_serv in (25,210,410) " +
                    "   and p.nzp_prm = 1172 " + // огневая плита
                    " Group by 1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1x on ttt_aid_c1x (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_c1x ", true);

                iNzpRes = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 182, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
                if (iNzpRes != 0)
                {
                    ret = ExecSQL(conn_db,
                        " Update ttt_counters_xx " +
                        " Set cnt3 = " + iNzpRes +
                        " Where nzp_serv in (25,210,410) " +
                        "   and 0 < ( Select count(*) From ttt_aid_c1x k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }

                ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);
            }

            iNzpResMKD = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 180, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
            if (bIsSaha)
            {
                iNzpRes = iNzpResMKD;
            }
            else
            {
                iNzpRes = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 182, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
            }

            if ((iNzpRes != 0) && (iNzpResMKD != 0))
            {
                ret = ExecSQL(conn_db,
                    " Update ttt_counters_xx " +
                    " Set cnt3 = (case when cnt4 = " + iNzpPrmEE + " then " + iNzpRes + " else " + iNzpResMKD + " end) " + //значит остальные без электроплиты (где <> -2;-17)
                    " Where nzp_serv in (25,210,410) " +
                    "   and cnt3 = 0 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            var sKolGil = "cnt1";
            var sKolGil_g = "cnt1_g";
            if ((new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 01)) < (new DateTime(2012, 09, 01)))
            {
                sKolGil = "1"; sKolGil_g = "1";
            }

            string sql;
            if (bIsSaha)
            {
                sql =
                        " Update ttt_counters_xx " +
                        " Set val1 = (" + sNvlWord +
                        " (( Select a.value From " + rashod.paramcalc.kernel_alias + "res_values a " +
                                     " Where a.nzp_res = cnt3 " +

                                     "   and a.nzp_y = (" +
                                         " Select b.nzp_y From " + rashod.paramcalc.kernel_alias + "res_values b," +
                                         rashod.paramcalc.kernel_alias + "res_values pn," + rashod.paramcalc.kernel_alias + "res_values pk " +
                                         " Where b.nzp_res = cnt3 and pn.nzp_res = cnt3 and pk.nzp_res = cnt3 " +
                                         " and b.nzp_y=pn.nzp_y and pn.nzp_y=pk.nzp_y and b.nzp_x=1 and pn.nzp_x=2 and pk.nzp_x=3 " +
                                         " and (b.value" + sConvToInt + ")=(case when cnt2 >=4 then 4 else cnt2 end) " +
                                         " and pn.value" + sConvToNum + "<=squ1 and pk.value" + sConvToNum + ">=squ1 " +
                                         " ) " + //кол-во комнат (cnt2 >=4) & площадь в диапазоне!

                                     "   and a.nzp_x = (case when cnt1 >=5 then 5+3 else cnt1+3 end) " + //кол-во людей
                                     " ),'0') " + sConvToNum + ") " +
                                     " * " + sKolGil +
                        " , rash_norm_one = " + sNvlWord +
                        " (( Select a.value From " + rashod.paramcalc.kernel_alias + "res_values a " +
                                     " Where a.nzp_res = cnt3 " +

                                     "   and a.nzp_y = (" +
                                         " Select b.nzp_y From " + rashod.paramcalc.kernel_alias + "res_values b," +
                                         rashod.paramcalc.kernel_alias + "res_values pn," + rashod.paramcalc.kernel_alias + "res_values pk " +
                                         " Where b.nzp_res = cnt3 and pn.nzp_res = cnt3 and pk.nzp_res = cnt3 " +
                                         " and b.nzp_y=pn.nzp_y and pn.nzp_y=pk.nzp_y and b.nzp_x=1 and pn.nzp_x=2 and pk.nzp_x=3 " +
                                         " and (b.value" + sConvToInt + ")=(case when cnt2 >=4 then 4 else cnt2 end) " +
                                         " and pn.value" + sConvToNum + "<=squ1 and pk.value" + sConvToNum + ">=squ1 " +
                                         " ) " + //кол-во комнат (cnt2 >=4) & площадь в диапазоне!

                                     "   and a.nzp_x = (case when cnt1 >=5 then 5+3 else cnt1+3 end) " + //кол-во людей
                                     " ),'0') " + sConvToNum +
                        " , val1_g = (" + sNvlWord +
                        " (( Select a.value From " + rashod.paramcalc.kernel_alias + "res_values a " +
                                     " Where a.nzp_res = cnt3 " +

                                     "   and a.nzp_y = (" +
                                         " Select b.nzp_y From " + rashod.paramcalc.kernel_alias + "res_values b," +
                                         rashod.paramcalc.kernel_alias + "res_values pn," + rashod.paramcalc.kernel_alias + "res_values pk " +
                                         " Where b.nzp_res = cnt3 and pn.nzp_res = cnt3 and pk.nzp_res = cnt3 " +
                                         " and b.nzp_y=pn.nzp_y and pn.nzp_y=pk.nzp_y and b.nzp_x=1 and pn.nzp_x=2 and pk.nzp_x=3 " +
                                         " and (b.value" + sConvToInt + ")=(case when cnt2 >=4 then 4 else cnt2 end) " +
                                         " and pn.value" + sConvToNum + "<=squ1 and pk.value" + sConvToNum + ">=squ1 " +
                                         " ) " + //кол-во комнат (cnt2 >=4) & площадь в диапазоне!

                                     "   and a.nzp_x = (case when cnt1_g >=5 then 5+3 else cnt1_g+3 end) " + //кол-во людей
                                     " ),'0') " + sConvToNum + ") " +
                                     " * " + sKolGil_g +
                        " Where nzp_serv in (25,210,410) " +
                        "   and cnt1_g > 0 and cnt2 > 0 " +
                        "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";
            }
            else
            {
                #region применить спец.таблицу расходов ЭЭ в РТ - если стоит признак на базу (nzp_prm=163) и наличие ИПУ (nzp_prm=101)

                ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);

                ret = ExecSQL(conn_db,
                    " create temp table ttt_aid_c1x (nzp_kvar integer)" + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // 101|Наличие ЛС счетчика эл/эн|1||bool||1||||
                ret = ExecSQL(conn_db,
                      " Insert into ttt_aid_c1x (nzp_kvar)" +
                      " Select a.nzp_kvar " +
                      " From ttt_counters_xx a, ttt_prm_1 p " +
                      " Where a.nzp_kvar = p.nzp " +
                      "   and a.nzp_serv in (25,210,410) " +
                      "   and p.nzp_prm = 101 " +

                      // 163|Отменить таблицу нормативов по эл/энергии Пост.№761|1||bool||5||||
                      "   and 0 < ( Select count(*) From " + rashod.paramcalc.data_alias + "prm_5 p5 " +
                           " where p5.nzp_prm=163 and p5.val_prm='1' and p5.is_actual<>100 " +
                           "   and p5.dat_s  <= " + rashod.paramcalc.dat_po + " and p5.dat_po >= " + rashod.paramcalc.dat_s +
                           "   ) " +

                      " Group by 1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1x on ttt_aid_c1x (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_c1x ", true);

                sql =
                    " Update ttt_counters_xx " +
                    " Set cnt3 = 13 " +
                    " Where nzp_serv in (25,210,410) " +
                    "   and 0 < ( Select count(*) From ttt_aid_c1x k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_c1xx ", false);

                #endregion применить спец.таблицу расходов ЭЭ в РТ - если стоит признак на базу (nzp_prm=163) и наличие ИПУ (nzp_prm=101)

                sql =
                        " Update ttt_counters_xx " +
                        " Set val1 = (" + sNvlWord +
                                   "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                     " Where nzp_res = cnt3 " +
                                     "   and nzp_y = (case when cnt3 = 13" + //кол-во людей
                                                    " then (case when cnt1 >11 then 11 else cnt1 end)" +
                                                    " else (case when cnt1 > 6 then  6 else cnt1 end)" +
                                                    " end) " +
                                     "   and nzp_x = (case when cnt3 = 13" + //кол-во комнат
                                                    " then 1" +
                                                    " else (case when cnt2 > 5 then  5 else cnt2 end)" +
                                                    " end) " +
                                     " ),'0')" + sConvToNum + ") " +
                                     " * (case when cnt3 = 13 then 1 else " + sKolGil + " end)" +
                        " , rash_norm_one = " + sNvlWord +
                                   "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                     " Where nzp_res = cnt3 " +
                                     "   and nzp_y = (case when cnt3 = 13" + //кол-во людей
                                                    " then (case when cnt1 >11 then 11 else cnt1 end)" +
                                                    " else (case when cnt1 > 6 then  6 else cnt1 end)" +
                                                    " end) " +
                                     "   and nzp_x = (case when cnt3 = 13" + //кол-во комнат
                                                    " then 1" +
                                                    " else (case when cnt2 > 5 then  5 else cnt2 end)" +
                                                    " end) " +
                                     " ),'0') " + sConvToNum +
                                     " / (case when cnt3 = 13 and " + sKolGil + " > 0 then " + sKolGil + " else 1 end)" +
                        " , val1_g = (" + sNvlWord +
                                   "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                     " Where nzp_res = cnt3 " +
                                     "   and nzp_y = (case when cnt3 = 13" + //кол-во людей
                                                    " then (case when cnt1_g >11 then 11 else cnt1_g end)" +
                                                    " else (case when cnt1_g > 6 then  6 else cnt1_g end)" +
                                                    " end) " +
                                     "   and nzp_x = (case when cnt3 = 13" + //кол-во комнат
                                                    " then 1" +
                                                    " else (case when cnt2 > 5 then  5 else cnt2 end)" +
                                                    " end) " +
                                     " ),'0')" + sConvToNum + ") " +
                                     " * (case when cnt3 = 13 then 1 else " + sKolGil_g + " end)" +
                        " Where nzp_serv in (25,210,410) " +
                        "   and cnt1_g > 0 and cnt2 > 0 " +
                        "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";
            }

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_counters_xx", "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: электроснабжение

        #region N: хвс гвс канализация
        public bool CalcRashodNormHVandKAN(IDbConnection conn_db, Rashod rashod, bool bIsSaha, bool b_calc_kvar, bool bIsCalcSubsidyBill,
            string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //----------------------------------------------------------------
            //N: хвс гвс канализация
            //----------------------------------------------------------------

            var iNzpRes = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 172, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
            if (iNzpRes != 0)
            {
                ret = ExecSQL(conn_db,
                    " Update ttt_counters_xx " +
                    " Set cnt3 = " + iNzpRes +
                    " Where nzp_serv in (6,7,324) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            iNzpRes = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 177, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
            if (iNzpRes != 0)
            {
                ret = ExecSQL(conn_db,
                    " Update ttt_counters_xx " +
                    " Set cnt3 = " + iNzpRes +
                    " Where nzp_serv in (9,281,323) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            string sql;
            var sdn = "";
            if (bIsSaha &&
                (Convert.ToDateTime("01." + rashod.paramcalc.cur_mm + "." + rashod.paramcalc.cur_yy) < Convert.ToDateTime("01.07.2013"))
                ) //&& ()) !!! before '01.07.2013' !!!
            {
                // вид дома по ГВС
                ExecSQL(conn_db, " Drop table t1189 ", false);
                ret = ExecSQL(conn_db,
                    " create temp table t1189 (nzp_dom integer) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                sdn = " * gl7kw ";
                // для Сахи до '01.07.2013' умножим на количество дней:

                // для биллинга в Сахе кол-во дней пропорционально 365/12 - таблица s_ot_period не использовать для биллинга
                if (bIsCalcSubsidyBill)
                {
                    // !) круглогодичное оказание услуги. умножить на количество дней месяца = 365/12
                    sql =
                        " Update ttt_counters_xx " +
                        " Set gl7kw = 365/12 " +
                        " Where nzp_serv in (6,7,324, 9,281,323) " +
                        "   and cnt2 > 0 ";
                }
                else
                {
                    // выбрать дома с учетом количества дней по отопительному периоду - p.nzp_prm=p.1189 and p.val_prm+0>1
                    // p.val_prm = 1; - централизованное ГВС - круглый год
                    ret = ExecSQL(conn_db,
                        " insert into t1189 (nzp_dom) select k.nzp_dom From ttt_prm_1 p,t_selkvar k" +
                        " Where p.nzp_prm=1189 and p.val_prm" + sConvToInt + ">1 and p.nzp=k.nzp_kvar" +
                        " group by 1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ExecSQL(conn_db, " Create index ix_t1189 on t1189 (nzp_dom) ", true);
                    ExecSQL(conn_db, sUpdStat + " t1189 ", true);
                    // 1) оказание услуги в отопительный период. умножить на количество дней отопления в месяце - s_ot_period.mХХ = месяц и s_ot_period.nzp_town = дом
                    sql =
                        " Update ttt_counters_xx " +
                        " Set gl7kw =" +
                            " ( Select s.m" + rashod.paramcalc.cur_mm.ToString("00") +
                            " From " +
                            rashod.paramcalc.kernel_alias + "s_ot_period s, " +
                            Points.Pref + "_data" + tableDelimiter + "s_rajon r, " +
                            Points.Pref + "_data" + tableDelimiter + "s_ulica u, " +
                            rashod.paramcalc.data_alias + "dom d " +
                            " where s.year_=" + rashod.paramcalc.cur_yy +
                            "   and s.nzp_town = r.nzp_town and r.nzp_raj=u.nzp_raj and u.nzp_ul=d.nzp_ul and d.nzp_dom=ttt_counters_xx.nzp_dom" +
                            " ) " +
                        " Where nzp_serv in (6,7,324, 9,281,323) " +
                        "   and cnt2 > 0 and 0<(select count(*) from t1189 where t1189.nzp_dom=ttt_counters_xx.nzp_dom)";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // 2) круглогодичное оказание услуги. умножить на количество дней месяца - s_ot_period.mХХ = месяц и s_ot_period.nzp_town = 0
                    sql =
                        " Update ttt_counters_xx " +
                        " Set gl7kw =" +
                            " ( Select s.m" + rashod.paramcalc.cur_mm.ToString("00") +
                              " From " + rashod.paramcalc.kernel_alias + "s_ot_period s " +
                              " where s.year_=" + rashod.paramcalc.cur_yy +
                              "   and s.nzp_town = 0" +
                            " ) " +
                        " Where nzp_serv in (6,7,324, 9,281,323) " +
                        "   and cnt2 > 0 and 0=(select count(*) from t1189 where t1189.nzp_dom=ttt_counters_xx.nzp_dom)";
                }

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Drop table t1189 ", false);
            }


            #region  выбрать ЛС с расходом по КАН меньше суммы расхода ХВС+ГВС


            ret = ExecSQL(conn_db,
                " insert into t_is391 (nzp_kvar) " +
                " select nzp_kvar " +
                " from temp_table_tarif t " +
                " where nzp_serv in (7,324) " +
                " and exists (select 1 from " + rashod.paramcalc.pref + sKernelAliasRest + " formuls_opis f where f.nzp_frm=t.nzp_frm and f.nzp_frm_typrs=391) " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_t_is391 on t_is391 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_is391 ", true);

            #endregion

            // выбрать КАН только по ХВ
            ret = ExecSQL(conn_db,
                " create temp table t_is339 (nzp_kvar integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_is339 (nzp_kvar) " +
                " select nzp_kvar " +
                " from temp_table_tarif " +
                " where nzp_serv in (7,324) and nzp_frm=339 " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_t_is339 on t_is339 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_is339 ", true);

            // отметить КАН только по ХВ
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set rvirt = 1" +
                " Where nzp_serv in (7,324) " +
                  " and 0<(select count(*) from t_is339 t where t.nzp_kvar=ttt_counters_xx.nzp_kvar )"
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            sql =
                " Update ttt_counters_xx Set " +
                // норма на 1 человека - ХВС, ГВС и КАН
                " rash_norm_one =" +

                " case when nzp_serv in (9,281,323) then " +

                // ... beg норматив на ГВС
                " (( Select " + sNvlWord + "(value,'0') From " + rashod.paramcalc.kernel_alias + "res_values " +
                    "  Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = 2 " +
                    " ) " + sConvToNum + ") " +
                    sdn +
                // ... end норматив на ГВС

                " else " +

                // ... beg норматив на ХВС и КАН
                " (( Select " + sNvlWord + "(value,'0') From " + rashod.paramcalc.kernel_alias + "res_values " +
                    "  Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = (case when nzp_serv=6 then 1 else 3 end) " + // на нужды ХВ
                    " )" + sConvToNum + ") " +
                    sdn +

                " + case when nzp_serv=6 or rvirt = 1 then 0 else " +

                   "(( Select " + sNvlWord + "(value,'0') From " + rashod.paramcalc.kernel_alias + "res_values " +
                       " Where nzp_res = cnt3 " +
                       "   and nzp_y = cnt2 " + //тип водоснабжения
                       "   and nzp_x = (case when nzp_serv=6 then 2 else 4 end) " + // на нужды ГВ
                       " )" + sConvToNum + ") " +
                       sdn +

                   " end " +
                // ... end норматив на ХВС и КАН

               " end " +

                // норма на 1 человека в части ХВС - ХВС и КАН
                ", val3 = " +
                // ... beg доля норматива на ХВС - только для ХВС и КАН - для ГВС нет
                " case when nzp_serv in (9,281,323) then 0 else " +
                " (( Select " + sNvlWord + "(value,'0') From " + rashod.paramcalc.kernel_alias + "res_values " +
                    " Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = (case when nzp_serv=6 then 1 else 3 end) " + // на нужды ХВ
                    " )" + sConvToNum + ") " +
                    sdn + " " +
               " end " +
                // ... end доля норматива на ХВС - только для ХВС и КАН - для ГВС нет

               " Where nzp_serv in (6,7,324, 9,281,323) " +
               "   and cnt2 > 0 " +
               "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_counters_xx", "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // нужды ХВ + нужды ГВ !!! для ХВ (и для КАН по ХВ -> rvirt = 1) расход ГВ НЕ добавлять !!!
            sql =
                " Update ttt_counters_xx Set " +
                " val1   = " + sNvlWord + "(rash_norm_one * gil1,0) " +
                ",val1_g = " + sNvlWord + "(rash_norm_one * gil1_g,0) " +
                ",val4   = " + sNvlWord + "(val3 * gil1,0) " +
                " Where nzp_serv in (6,7,324, 9,281,323) " +
                "   and cnt2 > 0 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: хвс гвс канализация

        #region N: отопление
        public bool CalcRashodNormOtopl(IDbConnection conn_db, Rashod rashod,
            bool bIsSaha, bool b_calc_kvar, bool bIsCalcSubsidyBill,
            string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            //N: отопление
            //----------------------------------------------------------------
            var sql =
                " Update ttt_counters_xx " +
                " Set cnt3 = 61" + // норма по площади
                " Where nzp_serv in (8,322,325) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            var sKolGPl = "5";
            if (bIsSaha)
            {
                sKolGPl = "2";
            }

            // норма по площади на 1 человека (Кж=cnt1)
            sql =
                " Update ttt_counters_xx " +
                " Set cnt2 = ( Select " + sNvlWord + "(value,'0') From " + rashod.paramcalc.kernel_alias + "res_values " +
                             " Where nzp_res = cnt3 " +
                             "   and nzp_y = (case when cnt1 > " + sKolGPl + " then " + sKolGPl + " else cnt1 end) " + //кол-во людей
                             "   and nzp_x = 2 " + //
                             " )" + sConvToInt +
                " Where nzp_serv in (8,322,325) " +
                "   and cnt1 > 0 " +
                "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //
            // ... расчет норматива на 1 кв.м ...
            //

            // признак отключения расчета норматива отопления
            var bCalcNormOtopl =
                !CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 478, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);

            ret = ExecSQL(conn_db, " drop table t_norm_otopl ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_norm_otopl ( " +
                " nzp_dom         integer, " +
                " nzp_kvar        integer, " +
                " s_otopl         " + sDecimalType + "( 8,2) default 0, " +    // отапливаемая площадь
                " rashod_gkal_m2f " + sDecimalType + "(12,8) default 0, " +    // расход в ГКал на 1 м2 фактический
                // вид учтенного расхода на л/с (1-норматив/2-ИПУ расход ГКал/3-дом.норма ГКал/4-ОДПУ расход ГКал)
                " vid_gkal_ls     integer default 0,       " +
                " rashod_gkal_dom " + sDecimalType + "(12,8) default 0, " +    // расход в ГКал на 1 м2
                " vid_gkal_dom    integer default 0,       " +    // вид учтенного домового расхода

                " rashod_gkal_m2  " + sDecimalType + "(12,8) default 0, " +    // нормативный расход в ГКал на 1 м2
                // для расчета норматива по отоплению
                " koef_god_pere   " + sDecimalType + "( 6,4) default 1, " +    // коэффициент перерасчета по итогам года
                " ugl_kv          integer default 0,       " +    // признак угловой квартиры (0-обычная/1-угловая)
                " vid_alg         integer default 1,       " +    // вид методики расчета норматива
                " tmpr_vnutr_vozd " + sDecimalType + "( 8,4) default 0, " +    // температура внутреннего воздуха (обычно = 20, для угловых = 22)
                " tmpr_vnesh_vozd " + sDecimalType + "( 8,4) default 0, " +    // средняя температура внешнего воздуха  (обычно = -5.7)
                " otopl_period     integer default 0,      " +    // продолжительность отопительного периода в сутках (обычно = 218)
                " nzp_res0         integer default 0,      " +    // таблица нормативов
                " dom_klimatz      integer default 0,      " +    // Климатическая зона
                // vid_alg=1 - памфиловская методика расчета норматива
                " dom_objem        " + sDecimalType + "(12,2) default 0, " +   // объем дома
                " dom_pol_pl       " + sDecimalType + "(12,2) default 1, " +   // полезная/отапливаемая площадь дома
                " dom_ud_otopl_har " + sDecimalType + "(12,8) default 1, " +   // удельная отопительная характеристика дома
                " dom_otopl_koef   " + sDecimalType + "( 8,4) default 1, " +   // поправочно-отопительный коэффициент для дома
                // vid_alg=2 - методика расчета норматива по Пост306 без интерполяции удельного расхода тепловой энергии
                // vid_alg=3 - методика расчета норматива по Пост306  с интерполяцией удельного расхода тепловой энергии
                // vid_alg=4 - методика расчета норматива - табличное значение от этажа и года постройки дома
                " dom_dat_postr    date default '1.1.1900', " +   // дата постройки дома
                " dom_kol_etag     integer default 0,       " +   // количество этажей дома (этажность)
                " pos_etag         integer default 0,       " +   // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии
                " pos_narug_vozd   integer default 0,       " +   // позиция по температуре наружного воздуха в таблице удельных расходов тепловой энергии
                " dom_ud_tepl_en1  " + sDecimalType + "(12,8) default 0, " +   // минимальный  удельный расход тепловой энергии для дома по температуре и этажности
                " dom_ud_tepl_en2  " + sDecimalType + "(12,8) default 0, " +   // максимальный удельный расход тепловой энергии для дома по температуре и этажности
                " tmpr_narug_vozd1 " + sDecimalType + "( 8,4) default 0, " +   // минимально  близкая температура наружного воздуха в таблице
                " tmpr_narug_vozd2 " + sDecimalType + "( 8,4) default 0, " +   // максимально близкая температура наружного воздуха в таблице
                " tmpr_narug_vozd  " + sDecimalType + "( 8,4) default 0, " +   // температура наружного воздуха по проекту (паспорту) дома
                " dom_ud_tepl_en   " + sDecimalType + "(12,8) default 0,  " +   // удельный расход тепловой энергии для дома по температуре и этажности
                " norm_type_id integer,  " +
                " norm_tables_id integer " + // id норматива - по нему можно получить набор влияющих пар-в и их знач.
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // === перечень л/с для расчета нормативов ===
            ret = ExecSQL(conn_db,
                  " insert into t_norm_otopl (nzp_dom,nzp_kvar,s_otopl)" +
                  " select nzp_dom,nzp_kvar,max(squ2) from ttt_counters_xx " +
                  " where nzp_serv=8 " +
                  " group by 1,2 "
                  , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_norm_otopl on t_norm_otopl (nzp_kvar) ", true);
            ExecSQL(conn_db, " create index ix2_norm_otopl on t_norm_otopl (nzp_dom) ", true);
            ExecSQL(conn_db, sUpdStat + " t_norm_otopl ", true);

            int iNzpRes;
            // если разрешено рассчитывать норматив по отоплению
            if (bCalcNormOtopl)
            {
                #region N: отопление - расчет нормативов по типам

                // === параметры для всех алгоритмов расчета нормативов ===

                // нормативы от этажей (для РТ) и климатических зон (для сах РС-Я)
                iNzpRes = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 186, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
                if (iNzpRes != 0)
                {
                    ret = ExecSQL(conn_db,
                          " Update t_norm_otopl " +
                          " Set nzp_res0 = " + iNzpRes + " Where 1=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }

                // количество этажей дома (этажность)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_kol_etag=" +
                      "    ( select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToInt + " ) from ttt_prm_2 p " +
                      "      where t_norm_otopl.nzp_dom=p.nzp " +
                             " and p.nzp_prm=37 " +
                      " )" +
                      " where exists" +
                      "    ( select 1 from ttt_prm_2 p " +
                      "      where t_norm_otopl.nzp_dom=p.nzp " +
                             " and p.nzp_prm=37 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //ViewTbl(conn_db, " select * from t_norm_otopl where nzp_kvar=3829 ");

                // поиск указанного норматива в ГКал на 1 кв. метр. вид методики расчета норматива = 0
                if (bIsSaha)
                {
                    // позиция по количеству этажей дома в таблице расходов: 1-5
                    ret = ExecSQL(conn_db,
                          " update t_norm_otopl set pos_etag=" +
                          "    (case when dom_kol_etag<5 then (case when dom_kol_etag<=0 then 1 else dom_kol_etag end) else 5 end)" +
                          " where 1=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // Климатическая зона
                    ret = ExecSQL(conn_db,
                          " update t_norm_otopl set dom_klimatz=" +
                          "    ( select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToInt + " ) from ttt_prm_2 p " +
                          "      where t_norm_otopl.nzp_dom=p.nzp " +
                                    " and p.nzp_prm=1180 " +
                          " )" +
                          " where exists " +
                          "    ( select 1 from ttt_prm_2 p " +
                          "      where t_norm_otopl.nzp_dom=p.nzp " +
                                 " and p.nzp_prm=1180 " +
                          " ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // позиция по Климатическая зона дома в таблице расходов: 1-5
                    ret = ExecSQL(conn_db,
                          " update t_norm_otopl set dom_klimatz=" +
                          "    (case when dom_klimatz<5 then (case when dom_klimatz<=0 then 1 else dom_klimatz end) else 5 end)" +
                          " where 1=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db,
                        " Update t_norm_otopl Set " +
                        "  vid_alg=0, dom_ud_tepl_en = ( Select max(" + sNvlWord + "(r3.value,'0')" + sConvToNum + ")" +
                        " From " + rashod.paramcalc.kernel_alias + "res_values r1, " + rashod.paramcalc.kernel_alias + "res_values r2, " +
                            rashod.paramcalc.kernel_alias + "res_values r3 " +
                            "  Where r1.nzp_res = nzp_res0 and r2.nzp_res = nzp_res0 and r3.nzp_res = nzp_res0" +
                            "   and r1.nzp_x = 1 and r2.nzp_x = 2 and r3.nzp_x = 3 and r1.nzp_y=r2.nzp_y and r1.nzp_y=r3.nzp_y " +
                            "   and r1.value" + sConvToInt + " = dom_klimatz " +
                            "   and r2.value" + sConvToInt + " = pos_etag " +
                            " ) " +
                        " Where dom_klimatz > 0 and pos_etag>0 " +
                        "   and nzp_res0 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ViewTbl(conn_db, " select * from t_norm_otopl order by nzp_kvar ");
                    //////////////
                    // для биллинга в Сахе расход в ГКАл пропорционально 365/12 - таблицу s_ot_raspred не использовать для биллинга
                    if (bIsCalcSubsidyBill)
                    {
                        // !) круглогодичное пропорциональное оказание услуги = 365/12
                        ret = ExecSQL(conn_db,
                            " Update t_norm_otopl Set rashod_gkal_m2 = dom_ud_tepl_en Where 1=1 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }
                    else
                    {
                        // оказание услуги в отопительный период. умножить на процент распределения норматива отопления по месяцам
                        // - s_ot_raspred.mХХ = месяц и s_ot_raspred.nzp_town = дом
                        ret = ExecSQL(conn_db,
                            " Update t_norm_otopl Set " +
                            " koef_god_pere = ( Select s.m" + rashod.paramcalc.cur_mm.ToString("00") +
                                " From " +
                                rashod.paramcalc.kernel_alias + "s_ot_raspred s, " +
                                Points.Pref + "_data" + tableDelimiter + "s_rajon r, " +
                                Points.Pref + "_data" + tableDelimiter + "s_ulica u, " +
                                rashod.paramcalc.data_alias + "dom d " +
                                " where s.year_=" + rashod.paramcalc.cur_yy +
                                "   and s.nzp_town = r.nzp_town and r.nzp_raj=u.nzp_raj and u.nzp_ul=d.nzp_ul and d.nzp_dom=t_norm_otopl.nzp_dom" +
                                " ) " +
                            " Where 1=1 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                        ret = ExecSQL(conn_db,
                            " Update t_norm_otopl Set rashod_gkal_m2 = dom_ud_tepl_en * 12 * koef_god_pere / 100 Where 1=1 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }
                    ViewTbl(conn_db, " select * from t_norm_otopl order by nzp_kvar ");
                    //////////////
                }
                else
                {
                    ret = ExecSQL(conn_db,
                        " create temp table t_otopl_m2 ( " +
                        " nzp_dom         integer, " +
                        " rashod_gkal_m2  " + sDecimalType + "(12,8) default 0 " +    // нормативный расход в ГКал на 1 м2
                        " )  " + sUnlogTempTable
                         , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // === алгоритм расчет нормативов 0 ===
                    ret = ExecSQL(conn_db,
                          " Insert into t_otopl_m2 (nzp_dom,rashod_gkal_m2)" +
                          " Select p.nzp,max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                          " From ttt_prm_2 p " +
                          " Where p.nzp_prm=723 " +
                          "    and exists (select 1 from t_norm_otopl t where t.nzp_dom=p.nzp) " +
                          " group by 1 "
                          , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // вид методики расчета норматива
                    ret = ExecSQL(conn_db,
                          " Update t_norm_otopl set vid_alg=" +
                          "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.') " + sConvToInt + ") " +
                               " From ttt_prm_2 p " +
                          "      Where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=709 " +
                          " )" +
                          " Where vid_alg<>0 and exists" +
                          "    ( Select 1 " +
                               " From ttt_prm_2 p " +
                          "      Where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=709 " +
                          " ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // коэффициент перерасчета по итогам года
                    ret = ExecSQL(conn_db,
                          " update t_norm_otopl set koef_god_pere=" +
                          "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                               " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                          "      where p.is_actual<>100 and p.nzp_prm=108" +
                          "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                          "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                          " where exists" +
                          "    ( select 1 " +
                               " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                          "      where p.is_actual<>100 and p.nzp_prm=108" +
                          "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                          "        and p.dat_po >= " + rashod.paramcalc.dat_s +
                          " ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ExecSQL(conn_db, " create index ix1_otopl_m2 on t_otopl_m2 (nzp_dom) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_otopl_m2 ", true);

                    ret = ExecSQL(conn_db,
                          " Update t_norm_otopl" +
                          " Set vid_alg=0, rashod_gkal_m2=( Select rashod_gkal_m2 From t_otopl_m2 p Where t_norm_otopl.nzp_dom=p.nzp_dom )" +
                          " Where exists ( Select 1 From t_otopl_m2 p Where t_norm_otopl.nzp_dom=p.nzp_dom ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //
                    #region Выбрать норму Гкал на 1 м2 для отопления на лицевой счет prm_1 nzp_prm =2463

                    // выбрать параметры л/с для типа 1814 на дату расчета
                    ExecSQL(conn_db, " drop table t_p2463 ", false);

                    ret = ExecSQL(conn_db,
                        " create temp table t_p2463 (" +
                        " nzp     integer," +
                        " vald    " + sDecimalType + "(14,7) " +
                        " )  " + sUnlogTempTable
                        , true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                    // Выбрать квартирный норматив гкал на м3
                    ret = ExecSQL(conn_db,
                        " insert into t_p2463 (nzp,vald)" +
                        " select nzp,max(" + sNvlWord + "(val_prm,'0')" + sConvToNum + ") " +
                        " from ttt_prm_1" +
                        " where nzp_prm=2463 " +
                        " group by 1 "
                        , true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                    ret = ExecSQL(conn_db, " create index ixt_p2463 on t_p2463(nzp) ", true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                    ret = ExecSQL(conn_db, sUpdStat + " t_p2463 ", true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                    ret = ExecSQL(conn_db,
                        " update t_norm_otopl" +
                        " set vid_alg=0, rashod_gkal_m2=( select p2.vald from t_p2463 p2 where p2.nzp=t_norm_otopl.nzp_kvar ) " +
                        " where exists ( select 1 from t_p2463 p1 where p1.nzp=t_norm_otopl.nzp_kvar ) "
                        , true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                    ExecSQL(conn_db, " drop table t_p2463 ", false);

                    #endregion Выбрать норму Гкал на 1 м2 для отопления на лицевой счет prm_1 nzp_prm =2463
                }

                // === алгоритмы расчета нормативов 1, 2, 3, 4 ===

                // признак угловой квартиры (0-обычная/1-угловая)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set ugl_kv=1" +
                      " where 0<(select count(*) from ttt_prm_1 p where t_norm_otopl.nzp_kvar=p.nzp and p.nzp_prm=310) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // vid_alg=1 ==================================

                // === параметры на всю БД (prm_5 - в 2.0 указаны в формуле!) ===
                // tmpr_vnutr_vozd -- температура внутреннего воздуха (обычно = 20, для угловых = 22)
                // tmpr_vnesh_vozd -- средняя температура внешнего воздуха  (обычно = -5.7)
                // otopl_period    -- продолжительность отопительного периода в сутках (обычно = 218)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set" +
                      "  tmpr_vnutr_vozd = (case when ugl_kv=1 then 22 else 20 end)," +
                      "  tmpr_vnesh_vozd = -5.7," +
                      "  otopl_period    = 218" +
                      " where vid_alg=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // === домовые параметры (prm_2) ===
                // поправочно-отопительный коэффициент для дома
                ret = ExecSQL(conn_db,
                      " Update t_norm_otopl Set dom_otopl_koef=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " From ttt_prm_2 p Where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=33 " +
                      " )" +
                      " Where vid_alg=1 and 0<" +
                      "    ( Select count(*)" +
                           " From ttt_prm_2 p Where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=33 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // объем дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_objem=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=32 " +
                      " )" +
                      " where vid_alg=1 and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=32 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // полезная/отапливаемая площадь дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_pol_pl=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=36 " +
                      " )" +
                      " where vid_alg=1 and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=36 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // удельная отопительная характеристика дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_otopl_har=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=31 " +
                      " )" +
                      " where vid_alg=1 and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=31 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // vid_alg=2 / 3 / 4 ==================================

                // === параметры на всю БД (prm_5) ===
                // температура внутреннего воздуха (обычно = 20, для угловых = 22)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set tmpr_vnutr_vozd=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=54" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                      " where vid_alg in (2,3) and 0<" +
                      "    ( select count(*) from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=54" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // для угловых
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set tmpr_vnutr_vozd=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=713" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                      " where vid_alg in (2,3) and ugl_kv=1 and 0<" +
                      "    ( select count(*) from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=713" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // средняя температура внешнего воздуха  (обычно = -5.7)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set tmpr_vnesh_vozd=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=710" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                      " where vid_alg in (2,3) and 0<" +
                      "    ( select count(*) from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=710" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // продолжительность отопительного периода в сутках (обычно = 218)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set otopl_period=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=712" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                      " where vid_alg in (2,3) and 0<" +
                      "    ( select count(*) from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=712" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // === домовые параметры (prm_2) ===
                // дата постройки дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_dat_postr=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToDate + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=150 " +
                      " )" +
                      " where vid_alg in (2,3,4) and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=150 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // количество этажей дома (этажность)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_kol_etag=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToInt + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=37 " +
                      " )" +
                      " where vid_alg in (2,3,4) and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=37 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // температура наружного воздуха по проекту (паспорту) дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set tmpr_narug_vozd=" +
                      "    ( Select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=711 " +
                      " )" +
                      " where vid_alg in (2,3) and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=711 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // таблица диапазонов этажей для таблицы удельных расходов тепловой энергии (Пост.306) до 1999 года
                /*
                  " select " +
                  " r.nzp_y y1,r.value val1" +
                  ",(case when strpos( r.value,'-')=0" +
                  "       then (r.value::int) else substring(r.value,1,strpos( r.value,'-')-1)::int" +
                  "  end) etag1 " +
                  ",coalesce((case when strpos(b.value,'-')=0" +
                  "       then (b.value::int) else substr(b.value,1,strpos( b.value,'-')-1)::int" +
                  "       end),9999) etag2 " +
                   " into temp t_etag1999  " +
                  " from " + rashod.paramcalc.pref + "_kernel.res_values r "+
                  " left outer join " + rashod.paramcalc.pref + "_kernel.res_values b on r.nzp_y=b.nzp_y-1 and b.nzp_res=9996 and b.nzp_x=1 " +
                  " where r.nzp_res=9996 and r.nzp_x=1 "  
                  */
                string ssql;
                ret = ExecSQL(conn_db,
                    " create temp table t_etag1999(y1 int,val1 char(20),etag1 integer,etag2 integer) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

#if PG
                var sposfun = "strpos";
#else
                string sposfun = rashod.paramcalc.data_alias + "pos";
#endif

                ssql =
                    " insert into t_etag1999(y1,val1,etag1,etag2) " +
                    " select " +
                    " r.nzp_y y1,r.value val1" +
                    ",(case when " + sposfun + "(r.value,'-')=0" +
                    "       then (r.value" + sConvToInt + ") else substr(r.value,1," +
                    sposfun + "(r.value,'-')-1)" + sConvToInt +
                    "  end) etag1 " +
                    "," + sNvlWord + "((case when " + sposfun + "(b.value,'-')=0" +
                    "       then (b.value" + sConvToInt + ") else substr(b.value,1," +
                    sposfun + "(b.value,'-')-1)" + sConvToInt +
                    "  end),9999) etag2 " +
                    " from " + rashod.paramcalc.kernel_alias + "res_values r " +
#if PG
 " left outer join " + rashod.paramcalc.kernel_alias + "res_values b " +
                    " on r.nzp_y=b.nzp_y-1 and b.nzp_res=9996 and b.nzp_x=1 where 1=1" +
#else
                    ",outer " + rashod.paramcalc.kernel_alias + "res_values b " +
                    " where b.nzp_res=9996 and b.nzp_x=1 and r.nzp_y=b.nzp_y-1 " +
#endif
 " and r.nzp_res=9996 and r.nzp_x=1  ";
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " create index ix_etag1999 on t_etag1999 (etag1,etag2) ", true);
                ExecSQL(conn_db, sUpdStat + " t_etag1999 ", true);

                // таблица диапазонов этажей для таблицы удельных расходов тепловой энергии (Пост.306) после 1999 года
                ret = ExecSQL(conn_db,
                    " create temp table t_etag(y1 int,val1 char(20),etag1 integer,etag2 integer) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ssql =
                    " insert into t_etag(y1,val1,etag1,etag2) " +
                    " select " +
                    " r.nzp_y y1,r.value val1" +
                    ",(case when " + sposfun + "(r.value,'-')=0" +
                    "       then (r.value" + sConvToInt + ") else substr(r.value,1," +
                    sposfun + "(r.value,'-')-1)" + sConvToInt +
                    "  end) etag1 " +
                    "," + sNvlWord + "((case when " + sposfun + "(b.value,'-')=0" +
                    "       then (b.value" + sConvToInt + ") else substr(b.value,1," +
                    sposfun + "(b.value,'-')-1)" + sConvToInt +
                    "       end),9999) etag2 " +
                    " from " + rashod.paramcalc.kernel_alias + "res_values r " +
#if PG
 " left outer join " + rashod.paramcalc.kernel_alias + "res_values b" +
                    " on r.nzp_y=b.nzp_y-1 and b.nzp_res=9997 and b.nzp_x=1 where 1=1 " +
#else
                    ",outer " + rashod.paramcalc.kernel_alias + "res_values b " +
                    " where b.nzp_res=9997 and b.nzp_x=1 and r.nzp_y=b.nzp_y-1" + 
#endif
 " and r.nzp_res=9997 and r.nzp_x=1 ";
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " create index ix_etag on t_etag (etag1,etag2) ", true);
                ExecSQL(conn_db, sUpdStat + " t_etag ", true);

                // таблица диапазонов температур для таблицы удельных расходов тепловой энергии (Пост.306)
                ret = ExecSQL(conn_db,
#if PG
 " select " +
                      " r.nzp_y y1,r.value val1" +
                      ",(case when strpos(r.value,'-')=0" +
                      "       then (r.value::int) else substring(r.value,1,strpos(r.value,'-')-1)::int" +
                      "  end) tmpr1 " +
                      ",coalesce((case when strpos(b.value,'-')=0" +
                      "       then (b.value::int) else substring(b.value,1,strpos(b.value,'-')-1)::int" +
                      "       end),9999) tmpr2 " +
                      " into temp t_tmpr  " +
                      " from " + rashod.paramcalc.pref + "_kernel.res_values r " +
                      " left outer join " + rashod.paramcalc.pref + "_kernel.res_values b on r.nzp_y=b.nzp_y-1 and b.nzp_res=9991 and b.nzp_x=1 " +
                      " where r.nzp_res=9991 and r.nzp_x=1 "

#else
                      " select " +
                      " r.nzp_y y1,r.value val1" +
                      ",(case when " + rashod.paramcalc.pref + "_data:pos(r.value,'-')=0" +
                      "       then (r.value+0) else substr(r.value,1," + rashod.paramcalc.pref + "_data:pos(r.value,'-')-1)+0" +
                      "  end) tmpr1 " +
                      ",nvl((case when " + rashod.paramcalc.pref + "_data:pos(b.value,'-')=0" +
                      "       then (b.value+0) else substr(b.value,1," + rashod.paramcalc.pref + "_data:pos(b.value,'-')-1)+0" +
                      "       end),9999) tmpr2 " +
                      " from " + rashod.paramcalc.pref + "_kernel:res_values r,outer " + rashod.paramcalc.pref + "_kernel:res_values b " +
                      " where r.nzp_res=9991 and r.nzp_x=1 and b.nzp_res=9991 and b.nzp_x=1 and r.nzp_y=b.nzp_y-1" +
                      " into temp t_tmpr with no log "
#endif
, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " create index ix_tmpr on t_tmpr (tmpr1,tmpr2) ", true);
                ExecSQL(conn_db, sUpdStat + " t_tmpr ", true);

                // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии до 1999
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set pos_etag=" +
                      "    (select max(b.y1) from t_etag1999 b where t_norm_otopl.dom_kol_etag>=b.etag1 and t_norm_otopl.dom_kol_etag<b.etag2)" +
                      " where vid_alg in (2,3) and dom_dat_postr<=" + MDY(1, 1, 1999)
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии после 1999
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set pos_etag=" +
                      "    (select max(b.y1) from t_etag b where t_norm_otopl.dom_kol_etag>=b.etag1 and t_norm_otopl.dom_kol_etag<b.etag2)" +
                      " where vid_alg in (2,3) and dom_dat_postr>" + MDY(1, 1, 1999)
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // позиция по количеству этажей дома в таблице нормативных расходов тепловой энергии с 09.2012г в РТ 
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set pos_etag=" +
                      "    (case when dom_kol_etag>16 then 16 else dom_kol_etag end)" +
                      " where vid_alg=4 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //ViewTbl(conn_db, " select * from t_norm_otopl where nzp_kvar=3829 ");

                // pos_narug_vozd   - позиция по температуре наружного воздуха в таблице удельных расходов тепловой энергии
                // tmpr_narug_vozd1 - минимально  близкая температура наружного воздуха в таблице
                // tmpr_narug_vozd2 - максимально близкая температура наружного воздуха в таблице
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set (pos_narug_vozd,tmpr_narug_vozd1,tmpr_narug_vozd2)=" +
                      "    ((select max(b.y1) from t_tmpr b " +
                      "      where abs(t_norm_otopl.tmpr_narug_vozd)>=b.tmpr1 and abs(t_norm_otopl.tmpr_narug_vozd)<b.tmpr2)," +
                           "(select max(abs(b.tmpr1)) from t_tmpr b " +
                      "      where abs(t_norm_otopl.tmpr_narug_vozd)>=b.tmpr1 and abs(t_norm_otopl.tmpr_narug_vozd)<b.tmpr2)," +
                           "(select max(abs(b.tmpr2)) from t_tmpr b " +
                      "      where abs(t_norm_otopl.tmpr_narug_vozd)>=b.tmpr1 and abs(t_norm_otopl.tmpr_narug_vozd)<b.tmpr2))" +
                      " where vid_alg in (2,3) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // минимальный  удельный расход тепловой энергии для дома по температуре и этажности
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en1=" +
                      "    (select max(replace(" + sNvlWord + "(r.value,'0'),',','.')" + sConvToNum + ") " +
                      " from " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=9996 and t_norm_otopl.pos_etag=r.nzp_y" +
                            " and (case when t_norm_otopl.pos_narug_vozd>=2 then t_norm_otopl.pos_narug_vozd else 2 end)=r.nzp_x)" +
                      " where vid_alg in (2,3) and dom_dat_postr<=" + MDY(1, 1, 1999)
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en1=" +
                      "    (select max(replace(" + sNvlWord + "(r.value,'0'),',','.')" + sConvToNum + ") " +
                      " from " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=9997 and t_norm_otopl.pos_etag=r.nzp_y" +
                            " and (case when t_norm_otopl.pos_narug_vozd>=2 then t_norm_otopl.pos_narug_vozd else 2 end)=r.nzp_x)" +
                      " where vid_alg in (2,3) and dom_dat_postr> " + MDY(1, 1, 1999)
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // максимальный удельный расход тепловой энергии для дома по температуре и этажности
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en2=" +
                      "    (select max(replace(" + sNvlWord + "(r.value,'0'),',','.')" + sConvToNum + ") " +
                      " from " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=9996 and t_norm_otopl.pos_etag=r.nzp_y and t_norm_otopl.pos_narug_vozd+1=r.nzp_x)" +
                      " where vid_alg in (2,3) and dom_dat_postr<=" + MDY(1, 1, 1999)
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                      " Update t_norm_otopl set dom_ud_tepl_en2=" +
                      "    (select max(replace(" + sNvlWord + "(r.value,'0'),',','.')" + sConvToNum + ") " +
                      " From " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=9997 and t_norm_otopl.pos_etag=r.nzp_y and t_norm_otopl.pos_narug_vozd+1=r.nzp_x)" +
                      " Where vid_alg in (2,3) and dom_dat_postr> " + MDY(1, 1, 1999)
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // dom_ud_tepl_en - удельный расход тепловой энергии для дома по температуре и этажности
                // без интерполяции
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en=dom_ud_tepl_en1" +
                      " where vid_alg=2 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // с интерполяцией
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en=dom_ud_tepl_en1+" +
                      "   (dom_ud_tepl_en2-dom_ud_tepl_en1)*(abs(tmpr_narug_vozd)-tmpr_narug_vozd1)/(tmpr_narug_vozd2-tmpr_narug_vozd1)" +
                      " where vid_alg=3 and abs(tmpr_narug_vozd2-tmpr_narug_vozd1)>0.0001 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // === расчет нормативов в ГКал на 1 кв.м - vid_alg=1 ===
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set rashod_gkal_m2 = 0.98 * dom_ud_otopl_har * dom_objem / dom_pol_pl" +
                      " where vid_alg=1 and dom_pol_pl> 0.0001"
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // === расчет нормативов в ГКал на 1 кв.м - vid_alg in (2,3) ===
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set rashod_gkal_m2 = dom_ud_tepl_en / (tmpr_vnutr_vozd - tmpr_narug_vozd)" +
                      " where vid_alg in (2,3) and abs(tmpr_vnutr_vozd - tmpr_narug_vozd)> 0.0001 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // === расчет нормативов в ГКал на 1 кв.м - общее для vid_alg in (1,2,3) ===
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set rashod_gkal_m2 =" +
                      " rashod_gkal_m2 * (tmpr_vnutr_vozd - tmpr_vnesh_vozd) * otopl_period * 24 * 0.000001 / 12 * koef_god_pere" +
                      " where vid_alg in (1,2,3) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // === установить норматив в ГКал на 1 кв.м - из таблицы для vid_alg=4 ===
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set rashod_gkal_m2=" +
                      "    (select max(replace(" + sNvlWord + "(r.value,'0'),',','.')" + sConvToNum + ") " +
                      " from " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=t_norm_otopl.nzp_res0 and t_norm_otopl.pos_etag=r.nzp_y" +
                      " and r.nzp_x=(case when dom_dat_postr<=" + MDY(1, 1, 1999) + " then 1 else 2 end) )" +
                      " where vid_alg=4 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // vid_alg = 5
                if (Points.isNewNorms)
                {
                    if (!rashod.paramcalc.b_cur)
                        p_dat_charge = MDY(rashod.paramcalc.cur_mm, 28, rashod.paramcalc.cur_yy);
                    GetNewNormsForOtpl(conn_db, rashod, p_dat_charge, out ret);
                }

                #endregion N: отопление - расчет нормативов по типам
            }
            else
            {
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set vid_alg=0,rashod_gkal_m2=0 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            //ViewTbl(conn_db, " select * from t_norm_otopl where nzp_kvar=3829 ");

            ret = ExecSQL(conn_db,
                  " update t_norm_otopl set rashod_gkal_m2=0 where rashod_gkal_m2 is null  "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ===  установка норматива по отоплению в counters_xx ===
            sql =
                " Update ttt_counters_xx " +
                " Set (val1,val3,rash_norm_one,kod_info) = " +
                "((Select rashod_gkal_m2 * ttt_counters_xx.squ2 From t_norm_otopl k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)," +
                "(Select rashod_gkal_m2 From t_norm_otopl k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)," +
                "(Select rashod_gkal_m2 From t_norm_otopl k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)," +
                "(Select vid_alg From t_norm_otopl k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)) " +
                " Where nzp_serv=8 " +
                "   and 0 < ( Select count(*) From t_norm_otopl k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar and k.vid_alg in (0,1,2,3,4,5) ) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: отопление

        #region N: электроотопление
        public bool CalcRashodNormElOtopl(IDbConnection conn_db, Rashod rashod, bool bIsSaha, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            //N: электроотопление
            //----------------------------------------------------------------
            ret = ExecSQL(conn_db,
                " create temp table t_norm_eeot ( " +
                //" create table are.t_norm_otopl ( " +
                " nzp_dom       integer, " +
                " nzp_kvar      integer, " +
                " s_otopl       " + sDecimalType + "( 8,2) default 0, " +    // отапливаемая площадь
                " rashod_kvt_m2 " + sDecimalType + "(12,8) default 0, " +    // нормативный расход в квт*час на 1 м2
                // для расчета норматива по отоплению
                " nzp_res0      integer default 0,      " +    // таблица нормативов
                " dom_klimatz   integer default 0,      " +    // Климатическая зона
                " dom_kol_etag  integer default 0,       " +   // количество этажей дома (этажность)
                " pos_etag      integer default 0        " +   // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // === перечень л/с для расчета нормативов ===
            ret = ExecSQL(conn_db,
                  " insert into t_norm_eeot (nzp_dom,nzp_kvar,s_otopl)" +
                  " select nzp_dom,nzp_kvar,max(squ2) from ttt_counters_xx " +
                  " where nzp_serv=322 " +
                  " group by 1,2 "
                  , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_norm_eeot on t_norm_eeot (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_norm_eeot ", true);

            // поиск указанного норматива в Квт*час на 1 кв. метр. вид методики расчета норматива = 0

            if (bIsSaha)
            {
                // нормативы для сах РС(Я) от этажей и климатических зон
                var iNzpRes = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 183, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
                if (iNzpRes != 0)
                {
                    ret = ExecSQL(conn_db,
                          " Update t_norm_eeot " +
                          " Set nzp_res0 = " + iNzpRes +
                          " Where 1=1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }
                // количество этажей дома (этажность)
                ret = ExecSQL(conn_db,
                      " update t_norm_eeot set dom_kol_etag=" +
                      "    ( select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.') ) from ttt_prm_2 p " +
                      "      where t_norm_eeot.nzp_dom=p.nzp and p.nzp_prm=37 " +
                      " )" + sConvToInt +
                      " where 0<" +
                      "    ( select count(*) from ttt_prm_2 p " +
                      "      where t_norm_eeot.nzp_dom=p.nzp and p.nzp_prm=37 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // позиция по количеству этажей дома в таблице расходов: 1-5
                ret = ExecSQL(conn_db,
                      " update t_norm_eeot set pos_etag=" +
                      "    (case when dom_kol_etag<5 then (case when dom_kol_etag<=0 then 1 else dom_kol_etag end) else 5 end)" +
                      " where 1=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // Климатическая зона
                ret = ExecSQL(conn_db,
                      " update t_norm_eeot set dom_klimatz=" +
                      "    ( select max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.') ) from ttt_prm_2 p " +
                      "      where t_norm_eeot.nzp_dom=p.nzp " +
                                " and p.nzp_prm=1180 " +
                      " )" + sConvToInt +
                      " where 0<" +
                      "    ( select count(*) from ttt_prm_2 p " +
                      "      where t_norm_eeot.nzp_dom=p.nzp " +
                                " and p.nzp_prm=1180 " +
                      " ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // позиция по Климатическая зона дома в таблице расходов: 1-5
                ret = ExecSQL(conn_db,
                      " update t_norm_eeot set dom_klimatz=" +
                      "    (case when dom_klimatz<5 then (case when dom_klimatz<=0 then 1 else dom_klimatz end) else 5 end)" +
                      " where 1=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Update t_norm_eeot Set " +
                    "  rashod_kvt_m2 = ( Select max(" + sNvlWord + "(r3.value,'0'))" +
                        " From " + rashod.paramcalc.kernel_alias + "res_values r1, " +
                          rashod.paramcalc.kernel_alias + "res_values r2, " + rashod.paramcalc.kernel_alias + "res_values r3 " +
                        "  Where r1.nzp_res = nzp_res0 and r2.nzp_res = nzp_res0 and r3.nzp_res = nzp_res0" +
                        "   and r1.nzp_x = 1 and r2.nzp_x = 2 and r3.nzp_x = 3 and r1.nzp_y=r2.nzp_y and r1.nzp_y=r3.nzp_y " +
                        "   and r1.value" + sConvToInt + " = dom_klimatz " +
                        "   and r2.value" + sConvToInt + " = pos_etag " +
                        " ) " + sConvToNum +
                    " Where dom_klimatz > 0 and pos_etag>0 " +
                    "   and nzp_res0 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            // домовые
            ret = ExecSQL(conn_db,
              " update t_norm_eeot set rashod_kvt_m2= t.val_prm::numeric " +
              " from ttt_prm_2 t where t.nzp = t_norm_eeot.nzp_dom and nzp_prm = 1479"
            , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // квартирные
            ret = ExecSQL(conn_db,
              " update t_norm_eeot set rashod_kvt_m2=t.val_prm::numeric " +
              "  from ttt_prm_1 t where t.nzp = t_norm_eeot.nzp_kvar and nzp_prm = 1480"
            , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " update t_norm_eeot set rashod_kvt_m2=0 where rashod_kvt_m2 is null  "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ===  установка норматива по электроотоплению в counters_xx ===
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set (val1,val3) = " +
                "((Select rashod_kvt_m2 * s_otopl From t_norm_eeot k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)," +
                " (Select rashod_kvt_m2 From t_norm_eeot k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)) " +
                " Where nzp_serv=322 " +
                "   and EXISTS ( Select 1 From t_norm_eeot k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: электроотопление

        #region N: газовое отопление
        public bool CalcRashodNormGasOtopl(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            //N: газовое отопление
            //----------------------------------------------------------------
            ret = ExecSQL(conn_db,
                " create temp table t_norm_gasot ( " +
                " nzp_dom       integer, " +
                " nzp_kvar      integer, " +
                " s_otopl       " + sDecimalType + "( 8,2) default 0, " +    // отапливаемая площадь
                " rashod_kbm_m2 " + sDecimalType + "(12,8) default 0  " +    // нормативный расход в куб.м газа на 1 м2
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // === перечень л/с для расчета нормативов ===
            ret = ExecSQL(conn_db,
                  " insert into t_norm_gasot (nzp_dom,nzp_kvar,s_otopl)" +
                  " select nzp_dom,nzp_kvar,max(squ2) from ttt_counters_xx " +
                  " where nzp_serv=325 " +
                  " group by 1,2 "
                  , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_norm_gasot on t_norm_gasot (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_norm_gasot ", true);

            ret = ExecSQL(conn_db,
                  " update t_norm_gasot set rashod_kbm_m2=" +
                  " (Select max(val_prm" + sConvToNum + ")" +
                   " From " + rashod.paramcalc.data_alias + "prm_5 " +
                   " Where nzp_prm = 169 and is_actual <> 100" +
                   " and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s + " ) "
                  , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " update t_norm_gasot set rashod_kbm_m2=0 Where rashod_kbm_m2 is null "
                  , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ===  установка норматива по газовому отоплению в counters_xx ===
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set (val1,val3) = " +
                "((Select rashod_kbm_m2 * s_otopl From t_norm_gasot k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)," +
                " (Select rashod_kbm_m2 From t_norm_gasot k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)) " +
                " Where nzp_serv=325 " +
                "   and 0 < ( Select count(*) From t_norm_gasot k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: газовое отопление

        #region N: газ
        public bool CalcRashodNormGas(IDbConnection conn_db, Rashod rashod, bool bIsSaha, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            //N: газ 
            //----------------------------------------------------------------
            if (Constants.Trace) Utility.ClassLog.WriteLog("N: газ - ");
            ret = ExecSQL(conn_db,
                " create temp table t_norm_gas ( " +
                " nzp_dom    integer, " +
                " nzp_kvar   integer, " +
                " rashod_kbm " + sDecimalType + "(12,8) default 0, " +    // нормативный расход в куб.м газа на 1 человека
                " nzp_res0   integer default 0,       " +    // таблица нормативов
                " is_gp      integer default 0,      " +   // Климатическая зона
                " is_gvs     integer default 0,      " +   // количество этажей дома (этажность)
                " is_gk      integer default 0       " +   // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // === перечень л/с для расчета нормативов ===
            ret = ExecSQL(conn_db,
                " insert into t_norm_gas (nzp_dom,nzp_kvar)" +
                " select nzp_dom,nzp_kvar from ttt_counters_xx " +
                " where nzp_serv=10 " +
                " group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_norm_gas on t_norm_gas (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_norm_gas ", true);

            // поиск указанного норматива в куб.м на 1 человека

            // нормативы
            var iNzpRes = LoadValPrmForNorm(conn_db, rashod.paramcalc.data_alias, 173, "13", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);
            if (iNzpRes != 0)
            {
                ret = ExecSQL(conn_db,
                      " Update t_norm_gas " +
                      " Set nzp_res0 = " + iNzpRes + " Where 1=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            // наличие газовой плиты
            ret = ExecSQL(conn_db,
                  " update t_norm_gas set is_gp=1" +
                  " where exists( select 1 from ttt_prm_1 p where t_norm_gas.nzp_kvar=p.nzp and p.nzp_prm=551 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // наличие газовой колонки (водонагревателя)
            ret = ExecSQL(conn_db,
                  " update t_norm_gas set is_gk=1" +
                  " where exists( select 1 from ttt_prm_1 p where t_norm_gas.nzp_kvar=p.nzp and p.nzp_prm=1 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // наличие ГВС
            var sql =
                " update t_norm_gas set is_gvs=1" +
                " where exists( select 1 from ttt_prm_1 p where t_norm_gas.nzp_kvar=p.nzp and p.nzp_prm=7 " +
                " and p.val_prm" + sConvToInt + " in (";

            if (bIsSaha)
            {
                //  8, 9,10,11,12,13,14,15,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37 - after 01.07.2013!!!
                //   sql = sql.Trim() + "01,02,03,04,05,12,13,14,15,16,17,18,19,20,24,25,26,27,28,30,31,32,36,37,38,39"; - 11.10.2014 по новой таблице нормативов
                sql = sql.Trim() + "09,10,11,12,13,14,15,16,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,37,37,39,40";
            }
            else if (Points.IsSmr)
            {
                sql = sql.Trim() + "10";
            }
            else
            {
                sql = sql.Trim() + "05,07,08,09,14,15,16,17";
            }
            sql = sql.Trim() + ") ) ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update t_norm_gas Set " +
                "  rashod_kbm = ( Select max(r2.value" + sConvToNum + ")" +
                " From " + rashod.paramcalc.kernel_alias + "res_values r1, " + rashod.paramcalc.kernel_alias + "res_values r2 " +
                    "  Where r1.nzp_res = nzp_res0 and r2.nzp_res = nzp_res0 " +
                    "   and r1.nzp_x = 1 and r2.nzp_x = 2 and r1.nzp_y=r2.nzp_y " +
                    "   and trim(r1.value) = (" +
                        " (case when is_gp =1 then '1' else '0' end) ||" +
                        " (case when is_gvs=1 then '1' else '0' end) ||" +
                        " (case when is_gk =1 then '1' else '0' end)" +
                    ") " +
                    " ) " +
                " Where (is_gp > 0 or is_gk>0) " +
                "   and nzp_res0 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update t_norm_gas Set rashod_kbm = 0 Where rashod_kbm is null "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ===  установка норматива по газу в counters_xx ===
            //По просьбе БФТ
            //sql =
            //    " Update ttt_counters_xx " +
            //    " Set (val1,val1_g,val3,rash_norm_one) = " +
            //    "((Select rashod_kbm * gil1   From t_norm_gas k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)," +
            //    " (Select rashod_kbm * gil1_g From t_norm_gas k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)," +
            //    " (Select rashod_kbm          From t_norm_gas k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)," +
            //    " (Select rashod_kbm          From t_norm_gas k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar)) " +
            //    " Where nzp_serv=10 " +
            //    "   and 0 < ( Select count(*) From t_norm_gas k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) ";
            sql =
                " UPDATE ttt_counters_xx " +
                " SET val1   = n.rashod_kbm * gil1   * (cntd * 1.00 / cntd_mn), " +
                "     val1_g = n.rashod_kbm * gil1_g * (cntd * 1.00 / cntd_mn), " +
                "     val3          = n.rashod_kbm, " +
                "     rash_norm_one = n.rashod_kbm " +
                " FROM t_norm_gas n " +
                " WHERE ttt_counters_xx.nzp_kvar = n.nzp_kvar AND ttt_counters_xx.nzp_serv = 10 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: газ

        #region N: полив
        public bool CalcRashodNormPoliv(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            //N: полив
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   ival1 integer default 0, " +
                "   rval1  " + sDecimalType + "(12,4) default 0.00," +
                "   rval2  " + sDecimalType + "(12,4) default 0.00," +
                "   rval3  " + sDecimalType + "(12,4) default 0.00," +
                "   rval4  " + sDecimalType + "(12,4) default 0.00 " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для РТ нормативный расход полива = объем на сотку * кол-во соток
            var sNorm200 =
            ",1 as ival1 " +
            ",max(case when nzp_prm=262 then " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " else 0 end) as rval1 " +
            ",max(case when nzp_prm=390 then " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " else 0 end) as rval2 " +
                // для садов в Туле!
            ",max(case when nzp_prm=2466 then " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " else 0 end) as rval3 " +
            ",max(case when nzp_prm=2467 then " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " else 0 end) as rval4 ";

            var sFlds200 = "262,390,2466,2467";
            if (Points.IsSmr)
            {
                // для Самары нормативный расход полива = кол-во поливок * площадь полива в кв.м * объем на 1 кв.м
                sNorm200 =
                ",max(case when nzp_prm=2044 then " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " else 0 end) as ival1 " +
                ",max(case when nzp_prm=2011 then " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " else 0 end) as rval1 " +
                ",max(case when nzp_prm=2043 then " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " else 0 end) as rval2 " +
                ",0 as rval3 " +
                ",0 as rval4 ";
                sFlds200 = "2011,2043,2044";
            }

            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, ival1, rval1, rval2, rval3, rval4) " +
                  " Select nzp as nzp_kvar" + sNorm200 +
                  " From ttt_counters_xx a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and a.nzp_serv=200 " +
                  "   and p.nzp_prm in (" + sFlds200 + ") " +
                  " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set val1 =" +
                " ( Select k.ival1 * (k.rval1 * k.rval2 + k.rval3 * k.rval4)" +
                  " From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) " +
                " , rash_norm_one =" +
                " ( Select k.ival1 * (k.rval2 + k.rval4)" +
                  " From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) " +
                " Where nzp_serv=200 " +
                "   and 0 < ( Select count(*)" +
                  " From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: полив

        #region N: вода для бани
        public bool CalcRashodNormHVforBanja(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            //N: вода для бани
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   gil1   " + sDecimalType + "(12,4) default 0.00," +
                "   gil1_g " + sDecimalType + "(12,4) default 0.00," +
                "   dat_s date not null," +
                "   dat_po date not null," +
                "   norm   " + sDecimalType + "(12,4) default 0.00 " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для РТ нормативный расход вода для бани = объем на Кж * Норма на 1 чел.
            var sFlds200 = "268";
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, gil1, gil1_g,dat_s,dat_po, norm) " +
                  " Select nzp as nzp_kvar, gil1, gil1_g, a.dat_s,a.dat_po, max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                  " From ttt_counters_xx a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and a.nzp_serv=203 " +
                  "   and p.nzp_prm in (" + sFlds200 + ") " +
                  "   and p.dat_s<=a.dat_po and p.dat_po>=a.dat_s"+
                  " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ret = ExecSQL(conn_db,
                 " Update ttt_counters_xx t " +
                 " Set" +
                   " cnt1 = " + sFlds200 +
                   ",val1 =  k.gil1 * k.norm " +
                   ",val1_g =  k.gil1_g * k.norm " +
                   ",rash_norm_one =  k.norm " +
                 " From ttt_aid_c1 k "+
                 " Where t.nzp_kvar = k.nzp_kvar " +
                 " and t.nzp_serv=203 " +
                 " and t.dat_s=k.dat_s and t.dat_po=k.dat_po"
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: вода для бани

        #region N: питьевая вода
        public bool CalcRashodNormPitHV(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            //N: питьевая вода
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   ival1 integer default 0, " +
                "   rval1  " + sDecimalType + "(12,4) default 0.00," +
                "   rval1_g " + sDecimalType + "(12,4) default 0.00," +
                "   rval2  " + sDecimalType + "(12,4) default 0.00 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для РТ нормативный расход пит.воды = кол-во жильцов * кол-во литров (норма на дом)
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_c1 (nzp_kvar, ival1, rval1, rval1_g, rval2) " +
                " Select nzp_kvar,max(cnt1),max(gil1),max(gil1_g),max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + " )" +
                " From ttt_counters_xx a " +
#if PG
 " left outer join ttt_prm_2 p on a.nzp_dom=p.nzp and p.nzp_prm=705 " +
                " Where 1=1 " +
#else
                ", outer ttt_prm_2 p " +
                " Where a.nzp_dom=p.nzp and p.nzp_prm=705 "+
#endif
 "   and a.nzp_serv=253 " +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set (val1,val1_g,rash_norm_one) =" +
                " (( Select k.rval1 * k.rval2" +
                   " From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar )," +
                "  ( Select k.rval1_g * k.rval2" +
                   " From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar )," +
                "  ( Select k.rval2" +
                   " From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar )) " +
                " Where nzp_serv=253 " +
                "   and 0 < ( Select count(*) From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion N: питьевая вода

        #region Учет нормативов по дням
        public bool CalcRashodNormPoDn(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            // val1 - норматив на ЛС
            // val1_g - норматив на ЛС без врем.выбвших
            // val4 = val3 * gil1 - нужды ХВ

            // установить ЛС, для кот. по-дневного расчета нет!
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx" +
                " Set stek = 3 " +
                " Where is_day_calc = 0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // поделить расход по дням
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set val_s  = val1, " +
                    " val_po = val4 " +
                " Where stek = 10 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // поделить расход по дням
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set val1   = val_s  * (cntd * 1.00 / cntd_mn), " +
                    " val1_g = val1_g * (cntd * 1.00 / cntd_mn), " +
                    " val4   = val_po * (cntd * 1.00 / cntd_mn)  " +
                " Where stek = 10 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_counters_xx ( " +
                  " nzp_dom, nzp_kvar, nzp_serv, stek, " +
                  " nzp_type, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, rashod, " +
                  " dat_s, dat_po, val1, val4, val1_g, " +
                  " val_s, val_po, val2, val3, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                  " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info," +
                  " cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls," +
                  " kf307n, kf307f9, rash_norm_one, gil1_g, cnt1_g, " +
                  " nzp_period, is_day_calc, cntd, cntd_mn " +
                  " ) " +
                " Select " +
                  " nzp_dom, nzp_kvar, nzp_serv,3 stek, " +
                  " max(nzp_type), max(dat_charge), max(cur_zap), max(nzp_counter), max(cnt_stage), max(mmnog), max(rashod), " +
                  " min(dat_s), max(dat_po), sum(val1), sum(val4), sum(val1_g), " +
                  " max(val_s), max(val_po), max(val2), max(val3), max(rvirt), " +
                  //" max(squ1), max(squ2)," +
                  " sum(squ1 * (cntd * 1.00 / cntd_mn))," + // площадь
                  " sum(squ2 * (cntd * 1.00 / cntd_mn))," + // площадь
                  " sum(gil1 * (cntd * 1.00 / cntd_mn))," + // жильцы
                  " sum(gil2 * (cntd * 1.00 / cntd_mn))," + // жильцы
                  " round(sum(gil1 * (cntd * 1.00 / cntd_mn))) as cnt1," + // жильцы
                  " max(cnt2), max(cnt3), max(cnt4), " +
                  " max(cnt5), max(dop87), max(pu7kw), max(gl7kw), max(vl210), max(kf307), max(kod_info)," +
                  " max(cls1), max(cls2), max(kf_dpu_kg), max(kf_dpu_plob), max(kf_dpu_plot), max(kf_dpu_ls)," +
                  " max(kf307n), max(kf307f9), max(rash_norm_one)," +
                  " sum(gil1_g * (cntd * 1.00 / cntd_mn))," +  // жильцы
                  " round(sum(gil1_g * (cntd * 1.00 / cntd_mn))) as cnt1_g," + // жильцы
                  " max(nzp_period), max(is_day_calc), sum(cntd), max(cntd_mn) " +
                " From ttt_counters_xx " +
                " Where stek = 10 " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            //
            return true;
        }
        #endregion Учет нормативов по дням

        #region  сохранить ttt_counters_xx (нормативы!) в стек 30 для Анэса
        public bool CalcRashodNormInsInStek30(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            // сохранить стек 3 (нормативы!) в стек 30 для Анэса

            #region выбрать нормативы для стека 30

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
            ret = ExecSQL(conn_db,
                " Select nzp_cntx, nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                      " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, rash_norm_one, val1_g, gil1_g, cnt1_g,val1_source,val4_source,up_kf " +
#if PG
 " Into temp ttt_aid_kpu From ttt_counters_xx "
#else
                " From ttt_counters_xx Into temp ttt_aid_kpu With no log "
#endif
, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " create index ix1_ttt_aid_kpu on ttt_aid_kpu (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, " create index ix2_ttt_aid_kpu on ttt_aid_kpu (nzp_cntx) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            #endregion выбрать нормативы для стека 30

            #region сохраним параметры расчета норматива отопления
            // в ttt_aid_kpu уже nzp_type = 3 and stek = 3 !

            // сохраним параметры расчета норматива отопления

            // val1 = rashod_gkal_m2 * s_otopl
            // rashod_gkal_m2   -> val3        - нормативный расход в ГКал на 1 м2
            // s_otopl          -> squ2        - отапливаемая площадь
            // vid_alg          -> kod_info    - вид методики расчета норматива
            // ugl_kv           -> cnt4        - признак угловой квартиры (0-обычная/1-угловая)
            // otopl_period     -> cnt5        - продолжительность отопительного периода в сутках (обычно = 218)
            // koef_god_pere    -> gil2        - коэффициент перерасчета по итогам года
            // tmpr_vnutr_vozd  -> pu7kw       - температура внутреннего воздуха (обычно = 20, для угловых = 22)
            // tmpr_vnesh_vozd  -> gl7kw       - средняя температура внешнего воздуха  (обычно = -5.7)

            // vid_alg=1 - памфиловская методика расчета норматива

            // dom_objem        -> kf_dpu_kg   - объем дома
            // dom_pol_pl       -> kf_dpu_plob - полезная/отапливаемая площадь дома
            // dom_ud_otopl_har -> kf_dpu_plot - удельная отопительная характеристика дома
            // dom_otopl_koef   -> kf_dpu_ls   - поправочно-отопительный коэффициент для дома

            var sql = " UPDATE ttt_aid_kpu SET" +
                        " cnt4 = k.ugl_kv, " +
                        " cnt5 = k.otopl_period," +
                        " gil2 = k.koef_god_pere," +
                        " pu7kw = k.tmpr_vnutr_vozd," +
                        " gl7kw = k.tmpr_vnesh_vozd," +
                        " kf_dpu_kg =  k.dom_objem," +
                        " kf_dpu_plob = k.dom_pol_pl," +
                        " kf_dpu_plot = k.dom_ud_otopl_har," +
                        " kf_dpu_ls = k.dom_otopl_koef" +
                        " FROM t_norm_otopl k " +
                        " WHERE ttt_aid_kpu.nzp_serv=8 AND ttt_aid_kpu.nzp_kvar = k.nzp_kvar  " +
                        " AND EXISTS (SELECT 1 From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar and k.vid_alg=1 ) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // vid_alg=2 - методика расчета норматива по Пост306 без интерполяции удельного расхода тепловой энергии
            // vid_alg=3 - методика расчета норматива по Пост306  с интерполяцией удельного расхода тепловой энергии

            // dom_kol_etag     -> cnt_stage   - количество этажей дома (этажность)
            // pos_etag         -> cls1        - позиция по количеству этажей дома в таблице удельных расходов тепловой энергии
            // pos_narug_vozd   -> cls2        - позиция по температуре наружного воздуха в таблице удельных расходов тепловой энергии
            // dom_dat_postr    -> nzp_counter - год даты постройки дома
            // dom_ud_tepl_en1  -> kf_dpu_kg   - минимальный  удельный расход тепловой энергии для дома по температуре и этажности
            // dom_ud_tepl_en2  -> kf_dpu_plob - максимальный удельный расход тепловой энергии для дома по температуре и этажности
            // tmpr_narug_vozd1 -> kf_dpu_plot - минимально  близкая температура наружного воздуха в таблице
            // tmpr_narug_vozd2 -> kf_dpu_ls   - максимально близкая температура наружного воздуха в таблице
            // tmpr_narug_vozd  -> kf307n      - температура наружного воздуха по проекту (паспорту) дома
            // dom_ud_tepl_en   -> kf307f9     - удельный расход тепловой энергии для дома по температуре и этажности

            sql = " UPDATE ttt_aid_kpu SET " +
                  " cnt4 = ugl_kv, " +
                  " cnt5 = otopl_period, " +
                  " gil2 = koef_god_pere," +
                  " pu7kw = tmpr_vnutr_vozd," +
                  " gl7kw = tmpr_vnesh_vozd," +
                  " cnt_stage = dom_kol_etag," +
                  " cls1 = pos_etag," +
                  " cls2 = pos_narug_vozd," +
                  " nzp_counter = " + DBManager.sYearFromDate + "dom_dat_postr), " +
                  " kf_dpu_kg = dom_ud_tepl_en1," +
                  " kf_dpu_plob = dom_ud_tepl_en2," +
                  " kf_dpu_plot = tmpr_narug_vozd1," +
                  " kf_dpu_ls = tmpr_narug_vozd2," +
                  " kf307n = tmpr_narug_vozd, " +
                  " kf307f9 = dom_ud_tepl_en " +
                  " FROM t_norm_otopl k " +
                  " WHERE nzp_serv=8 AND ttt_aid_kpu.nzp_kvar = k.nzp_kvar " +
                  " AND EXISTS (SELECT 1 FROM t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar and k.vid_alg in (2,3))";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


            #endregion сохраним параметры расчета норматива отопления

            #region вставка нормативов и параметров в стек 30
            //-- вставка нормативов и параметров --------------------------------------------------------------
            sql =
                " Insert into " + rashod.counters_xx +
                      " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                      "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                      "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info, rash_norm_one, val1_g, gil1_g, cnt1_g,val1_source,val4_source,up_kf ) " +
                " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, 30 stek, rashod, " +
                     "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                     "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info, rash_norm_one, val1_g, gil1_g, cnt1_g,val1_source,val4_source,up_kf " +
                " From ttt_aid_kpu " +
                " Where stek = 3 ";
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_kpu", "nzp_cntx", sql, 50000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //чистим kod_info для отопления
            sql = "UPDATE  ttt_counters_xx SET kod_info=0 WHERE nzp_serv=8 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion вставка нормативов и параметров в стек 30

            UpdateStatistics(false, rashod.paramcalc, rashod.counters_tab, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion  сохранить ttt_counters_xx (нормативы!) в стек 30 для Анэса

        #region изменить нормативы по ночному э/э с учетом дневного э/э
        public bool CorrNorm210with25(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            // e/e 210
            ExecSQL(conn_db, " Drop table ttt_ans_ee210 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_ee210 " +
                " ( nzp_kvar integer, " +
                "   ipr      integer default 0, " +
                "   val410   " + sDecimalType + "(12,4) default 0.00," +
                "   val210   " + sDecimalType + "(12,4) default 0.00," +
                "   val25    " + sDecimalType + "(12,4) default 0.00," +
                "   valn210  " + sDecimalType + "(12,4) default 0.00," +
                "   valn410  " + sDecimalType + "(12,4) default 0.00 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // взять расходы по нормативу ночьЭЭ - остаток норматива пойдет на эту услугу, норматив для полупикЭЭ = 0
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_ee210 (nzp_kvar,val210) " +
                " Select nzp_kvar,sum(rashod) " +
                " From ttt_counters_xx " +
                " Where stek=3 and cnt_stage=0 and kod_info=0 and nzp_serv=210 " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ans_ee210 on ttt_ans_ee210 (nzp_kvar) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_ee210 ", false);

            ExecSQL(conn_db, " Drop table ttt_ans_eeSEL ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_eeSEL ( nzp_kvar integer ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_eeSEL (nzp_kvar) " +
                " Select nzp_kvar From ttt_ans_ee210 " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ans_eeSEL on ttt_ans_eeSEL (nzp_kvar) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_eeSEL ", false);

            // взять расходы по нормативу полупикЭЭ, кроме тех у кого есть ночьЭЭ - остаток норматива пойдет на эту услугу
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_ee210 (nzp_kvar,ipr,val410) " +
                " Select nzp_kvar,1 ipr, sum(rashod) " +
                " From ttt_counters_xx " +
                " Where stek=3 and cnt_stage=0 and kod_info=0 and nzp_serv=410 " +
                " and not exists (select 1 from ttt_ans_eeSEL where ttt_ans_eeSEL.nzp_kvar=ttt_counters_xx.nzp_kvar) " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix2_ans_ee210 on ttt_ans_ee210 (ipr) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_ee210 ", false);
            ExecSQL(conn_db, " Drop table ttt_ans_eeSEL ", false);

            // дополнить расходами по ЭЭ все ЛС
            ret = ExecSQL(conn_db,
                " update ttt_ans_ee210 set val25 = ( Select sum(rashod) " +
                "   From ttt_counters_xx r Where ttt_ans_ee210.nzp_kvar=r.nzp_kvar and r.stek=3 and r.nzp_serv=25 ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // дополнить расходами по ночьЭЭ, ЛС где их нет
            ret = ExecSQL(conn_db,
                " update ttt_ans_ee210 set val210 = ( Select sum(rashod) " +
                "   From ttt_counters_xx r Where ttt_ans_ee210.nzp_kvar=r.nzp_kvar and r.stek=3 and r.nzp_serv=210 )" +
                " where ipr = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // вычислить норматив по ночьЭЭ / (норматив по полупикЭЭ = 0) & (val410 = 1 - для обнуления норматива по-дневного расчета, на него будет делиться 0!)
            ret = ExecSQL(conn_db,
                " update ttt_ans_ee210 set valn210 = val210 - val25, val410 = 1 Where ipr = 0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // вычислить норматив по полупикЭЭ
            ret = ExecSQL(conn_db,
                " update ttt_ans_ee210 set valn410 = val410 - val210 - val25 Where ipr = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // полный расход по ночьЭЭ
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx b " +
                " Set kod_info=25, " +
                " rashod =(case when a.valn210>0 then a.valn210 else 0 end)," +
                " val1   =(case when a.valn210>0 then a.valn210 else 0 end) " +
                " from ttt_ans_ee210 a" +
                " where b.nzp_kvar=a.nzp_kvar and b.stek=3 and b.nzp_serv=210 and b.cnt_stage=0 and a.val25 > 0 and a.ipr=0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // по-дневной расход по ночьЭЭ
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx b " +
                " Set kod_info=25, " +
                " rashod =(case when a.valn210>0 and a.val210>0 then b.rashod * (a.valn210/a.val210) else 0 end)," +
                " val1   =(case when a.valn210>0 and a.val210>0 then b.val1   * (a.valn210/a.val210) else 0 end) " +
                " from ttt_ans_ee210 a" +
                " where b.nzp_kvar=a.nzp_kvar and b.stek=10 and b.nzp_serv=210 and b.cnt_stage=0 and a.val25 > 0 and a.ipr=0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // полный расход по полупикЭЭ
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx b " +
                " Set kod_info=25, " +
                " rashod =(case when a.valn410>0 then a.valn410 else 0 end)," +
                " val1   =(case when a.valn410>0 then a.valn410 else 0 end) " +
                " from ttt_ans_ee210 a" +
                " where b.nzp_kvar=a.nzp_kvar and b.stek=3 and b.nzp_serv=410 and b.cnt_stage=0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // по-дневной расход по полупикЭЭ
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx b " +
                " Set kod_info=25, " +
                " rashod =(case when a.valn410>0 and a.val410>0 then b.rashod * (a.valn410/a.val410) else 0 end)," +
                " val1   =(case when a.valn410>0 and a.val410>0 then b.val1   * (a.valn410/a.val410) else 0 end) " +
                " from ttt_ans_ee210 a" +
                " where b.nzp_kvar=a.nzp_kvar and b.stek=10 and b.nzp_serv=410 and b.cnt_stage=0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion изменить нормативы по ночному э/э с учетом дневного э/э



        public bool GetNewNorms(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        {
            ret = Utils.InitReturns();

            // проставляем типы нормативов для услуг 
            // при получении типа норматива для услуги учитываем, что:
            // 324-очистка стоков = 7-канализация 
            // 281-Теплоноситель (отопление) = 9 - ГВС
            var sql = " UPDATE ttt_counters_xx SET norm_type_id=( " +
                         " SELECT n.id " +
                         " FROM " + Points.Pref + sKernelAliasRest + "norm_types n " +

                         " WHERE n.nzp_serv=" +
                         " (CASE WHEN ttt_counters_xx.nzp_serv=324 THEN 7 " +
                         " WHEN ttt_counters_xx.nzp_serv=281 THEN 9 " +
                         " ELSE ttt_counters_xx.nzp_serv END)" +

                         " and " + rashod.paramcalc.dat_s + " between n.date_from and n.date_to and n.is_finished=true " +
                         " and n.nzp_measure=ttt_counters_xx.nzp_measure and n.s_kind_norm_id=1" +
                         " and 0<(SELECT count(*) FROM " + Points.Pref + sKernelAliasRest + "norm_banks b" +
                         " WHERE b.norm_type_id=n.id and b.nzp_wp=" + rashod.paramcalc.nzp_wp + "))";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //создаем таблицы для хранения значений параметров по лс
            var tableName = "t_norm_sign_" + DateTime.Now.Ticks;

            sql = " CREATE TEMP TABLE " + tableName +
                  " (nzp_kvar INTEGER,nzp_dom INTEGER, norm_type_id INTEGER,nzp_prm_ls INTEGER,nzp_prm_house INTEGER, nzp_prm INTEGER," +
                  " type_val_sign_id INTEGER,nzp_serv INTEGER,param_value1 " + sDecimalType + ", date_value1 " + sDateTimeType + "," +
                  " date_from DATE, date_to DATE)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //получаем сигнатуры типов нормативов
            sql = " INSERT INTO " + tableName + "(nzp_kvar,nzp_dom,norm_type_id,nzp_prm_ls,nzp_prm_house,nzp_prm,type_val_sign_id,nzp_serv," +
                  " date_from,date_to)" +
                  " SELECT c.nzp_kvar,c.nzp_dom,c.norm_type_id,s.nzp_prm_ls,s.nzp_prm_house," +
                  " CASE WHEN s.nzp_prm_ls>0 THEN s.nzp_prm_ls ELSE s.nzp_prm_house END, s.type_val_sign_id,c.nzp_serv," +
                  " c.dat_s,c.dat_po " +
                  " FROM ttt_counters_xx c, " + Points.Pref + sKernelAliasRest + "norm_types_sign s " +
                  " WHERE c.norm_type_id=s.norm_type_id";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //добавляем канализование ГВС к услугам ЛС, если есть канализование ХВС.
            sql = " INSERT INTO " + tableName +
                  " (nzp_kvar,nzp_dom,norm_type_id,nzp_prm_ls,nzp_prm_house,nzp_prm,type_val_sign_id,nzp_serv," +
                  " date_from,date_to)" +
                  " SELECT c.nzp_kvar,c.nzp_dom,n.id,s.nzp_prm_ls,s.nzp_prm_house, " +
                  " CASE WHEN s.nzp_prm_ls>0 THEN s.nzp_prm_ls ELSE s.nzp_prm_house END as nzp_prm, s.type_val_sign_id,n.nzp_serv," +
                  " c.dat_s,c.dat_po  " +
                  " FROM " + Points.Pref + sKernelAliasRest + "norm_types n,ttt_counters_xx c," +
                  " " + Points.Pref + sKernelAliasRest + "norm_types_sign s " +
                  " WHERE n.nzp_serv=1007 and c.nzp_serv=7" + // добавляем услугу кан.ГВС для тех ЛС, где есть кан.ХВс nzp_serv=1007 - кан ГВС, 7 - кан ХВС
                  " and " + rashod.paramcalc.dat_s + " between n.date_from and n.date_to and n.is_finished=true and n.s_kind_norm_id=1 " +
                  " and c.nzp_measure=n.nzp_measure " +
                  " and 0<(SELECT count(*) FROM " + Points.Pref + sKernelAliasRest + "norm_banks b" +
                  " WHERE b.norm_type_id=n.id and b.nzp_wp=" + rashod.paramcalc.nzp_wp + ")" +
                  " and s.norm_type_id=n.id";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, "CREATE INDEX ixx_" + tableName + "_1 on " + tableName + "(nzp_kvar,date_from,date_to,norm_type_id)", true);

            var listHousesParams = new Dictionary<int, KeyValuePair<int, int>>(); //список домовых параметров
            var listLsParams = new Dictionary<int, KeyValuePair<int, int>>();//список квартирных параметров
            var knowParamsList = new Dictionary<int, string>();//список известных параметров

            #region уже известные параметры для ЛС
            knowParamsList.Add(4, "squ1");      //общая площадь
            knowParamsList.Add(133, "squ2");    //отап.площадь
            knowParamsList.Add(6, "sqgil");     //жилая площадь
            knowParamsList.Add(5, "gil1");      //кол-во жильцов
            knowParamsList.Add(131, "val3");    //кол-во временно проживающих
            knowParamsList.Add(107, "cnt2");    //кол-во комнат

            #endregion

            #region получаем список домовых и квартирных параметров

            //домовые
            sql = " SELECT  t.nzp_prm_house,p.prm_num,max(type_prm) as type_prm," +
                  " max(t.type_val_sign_id) as  type_val_sign_id " +
                  " FROM " + tableName + " t, " + Points.Pref + sKernelAliasRest + "prm_name p" +
                  " WHERE t.nzp_prm_house=p.nzp_prm and p.type_prm is not null" +
                  " GROUP BY 1,2 ";
            var dtHousePrmTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (var i = 0; i < dtHousePrmTable.Rows.Count; i++)
            {
                if (dtHousePrmTable.Rows[i]["type_prm"].ToString().Trim() == "serv")
                {
                    dtHousePrmTable.Rows[i]["prm_num"] = -1; //в данном случае нужно определить наличие определенной услуги у ЛС
                }

                var key = GetInt(dtHousePrmTable.Rows[i]["nzp_prm_house"]);
                var key1 = GetInt(dtHousePrmTable.Rows[i]["type_val_sign_id"]);
                var value = GetInt(dtHousePrmTable.Rows[i]["prm_num"]);
                listHousesParams.Add(key, new KeyValuePair<int, int>(key1, value));
            }

            //квартирные
            sql = " SELECT  t.nzp_prm_ls,p.prm_num,max(type_prm) as type_prm," +
                  " max(t.type_val_sign_id) as  type_val_sign_id" +
                " FROM " + tableName + " t, " + Points.Pref + sKernelAliasRest + "prm_name p" +
                " WHERE t.nzp_prm_ls=p.nzp_prm and p.type_prm is not null" +
                " GROUP BY 1,2 ";
            var dtLsPrmTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (var i = 0; i < dtLsPrmTable.Rows.Count; i++)
            {
                if (dtLsPrmTable.Rows[i]["type_prm"].ToString().Trim() == "serv")
                {
                    dtLsPrmTable.Rows[i]["prm_num"] = -1; //в данном случае нужно определить наличие определенной услуги у ЛС
                }
                var key = GetInt(dtLsPrmTable.Rows[i]["nzp_prm_ls"]);
                var key1 = GetInt(dtLsPrmTable.Rows[i]["type_val_sign_id"]);
                var value = GetInt(dtLsPrmTable.Rows[i]["prm_num"]);
                listLsParams.Add(key, new KeyValuePair<int, int>(key1, value));
            }

            #endregion

            #region Заполняем значения параметров для ЛС

            #region проставляем значения домовых параметров
            foreach (var prm in listHousesParams)
            {
                var type_val_sign_id = prm.Value.Key;
                var param_name = "";
                switch (type_val_sign_id)
                {
                    case (int)STypeValSign.Boolean:
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.NumPeriod:
                    case (int)STypeValSign.Sprav:
                        param_name = "param_value1"; break;
                    case (int)STypeValSign.Date:
                    case (int)STypeValSign.Period:
                        param_name = "date_value1"; break;
                }
                var prm_table = rashod.paramcalc.pref + sDataAliasRest + "prm_" + prm.Value.Value;
                var where = " p.is_actual<>100 and " + rashod.paramcalc.dat_s + " between p.dat_s and p.dat_po ";
                if (prm.Value.Value == 1 || prm.Value.Value == 2)
                {
                    prm_table = "ttt_prm_" + prm.Value.Value;
                    where = "1=1";
                }

                sql = " UPDATE " + tableName + " SET " + param_name + "=" +
                      " val_prm" + sConvToNum +
                      " FROM " + prm_table + "  p" +
                      " WHERE " + where + " and p.nzp=" + tableName + ".nzp_dom" +
                      " and p.nzp_prm=" + prm.Key + "" +
                      " AND nzp_prm_house=" + prm.Key +
                      " AND p.dat_s<=" + tableName + ".date_from" +
                      " AND p.dat_po>=" + tableName + ".date_to";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

            }
            #endregion
            #region проставляем значения квартирных параметров
            // проставляем уже известные значения квартирных параметров 
            foreach (var prm in knowParamsList)
            {
                if (listLsParams.ContainsKey(prm.Key))
                {
                    sql = " UPDATE " + tableName + " SET param_value1=t." + prm.Value + " FROM " +
                          " ttt_counters_xx t" +
                          " WHERE " + tableName + ".nzp_kvar=t.nzp_kvar " +
                          " AND " + tableName + ".norm_type_id=t.norm_type_id" +
                          " AND nzp_prm_ls=" + prm.Key +
                          " AND " + tableName + ".date_from=t.dat_s " +
                          " AND " + tableName + ".date_to=t.dat_po ";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                    listLsParams.Remove(prm.Key);
                }
            }

            foreach (var prm in listLsParams)
            {
                var type_val_sign_id = prm.Value.Key;
                var param_name = "";
                var conv_num = "";
                switch (type_val_sign_id)
                {
                    case (int)STypeValSign.Boolean:
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.NumPeriod:
                    case (int)STypeValSign.Sprav:
                        param_name = "param_value1";
                        conv_num = sConvToNum; break;
                    case (int)STypeValSign.Date:
                    case (int)STypeValSign.Period:
                        param_name = "date_value1";
                        conv_num = "::date "; break;
                }
                // проверка наличия услуги для ЛС
                if (prm.Value.Value < 0)
                {
                    sql = " SELECT max(prm_num) FROM " + Points.Pref + sKernelAliasRest + "prm_name where nzp_prm=" +
                          prm.Key;
                    var obj = ExecScalar(conn_db, sql, out ret, true);
                    var prm_num = 0;
                    if (ret.result) int.TryParse(obj.ToString(), out prm_num);

                    sql = " SELECT trim(max(val_prm)) FROM " + Points.Pref + sDataAliasRest + "prm_" + prm_num +
                          " WHERE nzp_prm=" + prm.Key;
                    obj = ExecScalar(conn_db, sql, out ret, true);
                    var nzp_serv = 0; //номер услуги для проверки на ее наличие у ЛС
                    if (ret.result) int.TryParse(obj.ToString(), out nzp_serv);

                    //проверяем наличие услуги у ЛС
                    sql = " UPDATE " + tableName + " SET " + param_name + "= 1" +
                          " WHERE EXISTS (SELECT 1 FROM ttt_counters_xx t" +
                          "               WHERE t.nzp_kvar=" + tableName + ".nzp_kvar" +
                          "               AND t.nzp_serv=" + nzp_serv +
                          "               AND " + tableName + ".date_from=t.dat_s " +
                          "               AND " + tableName + ".date_to=t.dat_po )" +
                          " AND nzp_prm_ls=" + prm.Key;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                }
                else
                {
                    var prm_table = rashod.paramcalc.pref + sDataAliasRest + "prm_" + prm.Value.Value;
                    var where = " p.is_actual<>100 and " + rashod.paramcalc.dat_s + " between p.dat_s and p.dat_po ";
                    if (prm.Value.Value == 1 || prm.Value.Value == 2)
                    {
                        prm_table = "ttt_prm_" + prm.Value.Value;
                        where = "1=1";
                    }

                    sql = " UPDATE " + tableName + " SET " + param_name + "=" +
                          sNvlWord + "(val_prm" + conv_num + ", " + tableName + "." + param_name + ")" +
                          " FROM " + prm_table + " p" +
                          " WHERE " + where + " " +
                          " AND p.nzp=" + tableName + ".nzp_kvar" +
                          " AND p.nzp_prm=" + prm.Key +
                          " AND nzp_prm_ls=" + prm.Key +
                          " AND p.dat_s<=" + tableName + ".date_from" +
                          " AND p.dat_po>=" + tableName + ".date_to";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }

                }
            }
            #endregion

            //проставляем нули 
            sql = " UPDATE " + tableName + " SET param_value1=0" +
                  " WHERE param_value1 is null ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //для дат
            sql = " UPDATE " + tableName + " SET date_value1="
                + Utils.EStrNull(DateTime.MinValue.ToShortDateString()) +
                  " WHERE date_value1 is null";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            #endregion

            #region получение нормативов по сигнатуре


            //получаем список всех типов нормативов
            sql = "SELECT DISTINCT norm_type_id, nzp_serv FROM " + tableName;
            var listTypesTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

            //по каждому типу нормативов
            for (var i = 0; i < listTypesTable.Rows.Count; i++)
            {
                var nzp_serv = GetInt(listTypesTable.Rows[i]["nzp_serv"]);
                var norm_type_id = GetInt(listTypesTable.Rows[i]["norm_type_id"]);
                var localTableName = "t_norm_type1_" + DateTime.Now.Ticks;
                var infPrmTableName = "t_norm_type2_" + DateTime.Now.Ticks;

                sql = " SELECT * FROM " + Points.Pref + sKernelAliasRest +
                      " norm_types_sign WHERE norm_type_id=" + norm_type_id + " ORDER BY ordering";

                var typeSign = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

                sql = "CREATE TEMP TABLE " + localTableName + " (nzp_kvar INTEGER,date_from DATE,date_to DATE, norm_tables_id INTEGER DEFAULT 0," +
                      "norm_type_id INTEGER DEFAULT 0,norm_value " + sDecimalType + " DEFAULT 0,";
                var internalPrms = new NormInternalParams(typeSign.Rows.Count) { typeSign = typeSign };
                for (var j = 0; j < typeSign.Rows.Count; j++)
                {
                    GenerateWhereForNorms(ref internalPrms, j, localTableName);
                }
                //создаем таблицу с имеющимися значениями параметров лс 
                sql += string.Join(",", internalPrms.param) + ")" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                sql = " INSERT INTO " + localTableName + " (nzp_kvar,date_from,date_to,norm_type_id," + string.Join(",", internalPrms.paramUpdate) + ")" +
                      " SELECT nzp_kvar,date_from,date_to,norm_type_id," + string.Join(",", internalPrms.caseStrings) +
                      " FROM " + tableName + " WHERE norm_type_id=" + norm_type_id +
                      " GROUP BY 1,2,3,4";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                ExecSQL(conn_db, "CREATE INDEX ixx_" + localTableName + "_1 on " + localTableName + "(nzp_kvar,date_from,date_to)", true);
                ExecSQL(conn_db, "CREATE INDEX ixx_" + localTableName + "_2 on " + localTableName + "(" + string.Join(",", internalPrms.paramUpdate) + ")", true);
                #region формируем таблицу для сигнатур нормативов


                sql = "CREATE TEMP TABLE " + infPrmTableName + " (id INTEGER,norm_value " + sDecimalType + ",";
                sql += string.Join(",", internalPrms.paramInf) + ")" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                sql = " INSERT INTO " + infPrmTableName + " (id, norm_value," + string.Join(",", internalPrms.paramUpdateInf) + ")" +
                   " SELECT n.id,MAX(n.norm_value) as norm," + string.Join(",", internalPrms.caseStringsInf) +
                   " FROM " + Points.Pref + sKernelAliasRest + "influence_params i, " + Points.Pref + sKernelAliasRest + "norm_tables n " +
                   " WHERE n.id=i.norm_tables_id and n.norm_type_id=" + norm_type_id +
                   " GROUP BY n.id";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                ExecSQL(conn_db, "CREATE INDEX ixx_" + infPrmTableName + "_1 on " + infPrmTableName + "(" + string.Join(",", internalPrms.paramUpdateInf) + ")", true);

                //проставляем максимальные значения из сигнатуры для параметров со значениями больше 
                for (var j = 0; j < internalPrms.listMoreVals.Count; j++)
                {
                    sql = " UPDATE " + localTableName + " SET " + internalPrms.listMoreVals[j] + " = " +
                          " (SELECT max(" + internalPrms.listMoreVals[j] + ") FROM " + infPrmTableName + ")" +
                          " WHERE " + internalPrms.listMoreVals[j] + ">(SELECT max(" + internalPrms.listMoreVals[j] + ") FROM " + infPrmTableName + " ) ";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                }

                //проставляем нормативы и id
                sql = "UPDATE " + localTableName + " SET norm_value=" +
               sNvlWord + "(i.norm_value,0),  norm_tables_id=i.id " +
               " FROM " + infPrmTableName + " i WHERE " + string.Join(" and ", internalPrms.paramCompare);
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }



                #endregion

                #region проставляем расход по нормативам по кол-ву жильцов
                var sKolGil = "gil1";
                var sKolGil_g = "gil1_g";
                if (nzp_serv == 25) //для электроэнергии берем только целые значения жильцов
                {
                    sKolGil = "cnt1";
                    sKolGil_g = "cnt1_g";
                    if ((new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 01)) <
                        (new DateTime(2012, 09, 01)))
                    {
                        sKolGil = "1";
                        sKolGil_g = "1";
                    }
                }

                //специфика услуг 
                switch (nzp_serv)
                {
                    //324 - эквивалент 7, 281 - эквивалент - 9, поэтому нормативы ищем по родительским nzp_serv, а проставляем по дочерним
                    case 324:
                    case 1007:
                    case 281:
                        {
                            //норматив на 1 жильца,расход по нормативу и расход без учета выбывших
                            sql = " UPDATE ttt_counters_xx SET " +
                                  " rash_norm_one =rash_norm_one+b.norm_value, val1=val1+b.norm_value*" + sKolGil +
                                  ",val1_g=val1_g+b.norm_value*" + sKolGil_g + ", norm_tables_id=b.norm_tables_id " +
                                  " FROM " + localTableName +
                                  " b WHERE ttt_counters_xx.nzp_kvar=b.nzp_kvar " +
                                  " AND ttt_counters_xx.nzp_serv=" + (nzp_serv == 1007 ? 7 : nzp_serv) + //кан. по ГВС прибавляем к общей КАН.
                                  " AND b.norm_type_id=" + norm_type_id +
                                  " AND ttt_counters_xx.dat_s=b.date_from" +
                                  " AND ttt_counters_xx.dat_po=b.date_to;";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result)
                            {
                                return false;
                            }
                            break;
                        }
                    case 7:
                        {
                            //норматив на 1 жильца,расход по нормативу и расход без учета выбывших
                            sql = " UPDATE ttt_counters_xx SET " +
                                  " rash_norm_one =rash_norm_one+b.norm_value, val1=val1+b.norm_value*" + sKolGil +
                                  ",val1_g=val1_g+b.norm_value*" + sKolGil_g +
                                  ", val4=b.norm_value*" + sKolGil + ", norm_tables_id=b.norm_tables_id " +
                                  " FROM " + localTableName +
                                  " b WHERE ttt_counters_xx.nzp_kvar=b.nzp_kvar " +
                                  " AND b.norm_type_id=" + norm_type_id +
                                  " AND ttt_counters_xx.norm_type_id=b.norm_type_id" +
                                  " AND ttt_counters_xx.dat_s=b.date_from" +
                                  " AND ttt_counters_xx.dat_po=b.date_to;";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result)
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        {
                            //норматив на 1 жильца,расход по нормативу и расход без учета выбывших
                            sql = " UPDATE ttt_counters_xx SET " +
                                  " rash_norm_one = b.norm_value, val1=b.norm_value*" + sKolGil + ",val1_g=b.norm_value*" + sKolGil_g +
                                  " , norm_tables_id=b.norm_tables_id " +
                                     " FROM " + localTableName +
                                  " b WHERE ttt_counters_xx.nzp_kvar=b.nzp_kvar " +
                                  " AND b.norm_type_id=" + norm_type_id +
                                  " AND ttt_counters_xx.norm_type_id=b.norm_type_id" +
                                  " AND ttt_counters_xx.dat_s=b.date_from" +
                                  " AND ttt_counters_xx.dat_po=b.date_to;";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result)
                            {
                                return false;
                            }

                            break;
                        }
                }

                ExecSQL(conn_db, " DROP TABLE " + localTableName, false);
                ExecSQL(conn_db, " DROP TABLE " + infPrmTableName, false);
                #endregion
            }

            #region Выбрать канализование только по ХВС
            // выбрать КАН только по ХВ
            ret = ExecSQL(conn_db,
                " create temp table t_is339 (nzp_kvar integer) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_is339 (nzp_kvar) " +
                " select nzp_kvar " +
                " from temp_table_tarif " +
                " where nzp_serv in (7,324) and nzp_frm=339 " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_t_is339 on t_is339 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_is339 ", true);

            // отметить КАН только по ХВ
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set rvirt = 1" +
                " Where nzp_serv in (7,324) " +
                  " and 0<(select count(*) from t_is339 t where t.nzp_kvar=ttt_counters_xx.nzp_kvar )"
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            #endregion

            ExecSQL(conn_db, " DROP TABLE " + tableName, false);
            #endregion
            return true;
        }

        static int GetInt(object obj)
        {
            if (obj != null && obj != DBNull.Value)
            {
                return obj as int? ?? default(int);
            }
            return default(int);
        }

        public bool GetNewNormsForODN(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Определение площади МОП
            // проставить домовые параметры для расчета норматива ОДН
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set (etag,squ_dom,dat_post,is_cgvs,is_hvobor,is_liftobor,is_obor, is_gvobor,is_otobor,is_zapir,is_anten,is_ppa,is_liftonly)=( " +
#if PG
 "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as etg From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=37 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToNum + ") as squ From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=40 ),0)," +

                " cast(coalesce((Select max( replace( val_prm, ',', '.'))  as dpost From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp  and p.nzp_prm=150 ),'01.01.1900') as date)," +

                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_cgvs     From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=27   ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_hvobor   From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1080 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_liftobor From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1081 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_obor     From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1082 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_gvobor   From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1341 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_otobor   From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1342 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_zapir    From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1343 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_anten    From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1344 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_ppa      From ttt_prm_2 p " +
                " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=1345 ),0)," +
                "coalesce((Select max( replace( val_prm, ',', '.')" + sConvToInt +
                ") as is_liftonly From ttt_prm_2 p " +
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
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            //2465|Если нет Площади МОП на ОДН по услугам - НЕ использовать для дома|||bool||5||||
            var bUchetMopODN =
                CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 2465, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_s);
            var ssql = "1 = 1";
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
            #endregion

            var sql = " UPDATE ttt_aid_virt0 SET norm_type_id=( " +
                         " SELECT max(n.id) " +
                         " FROM " + Points.Pref + sKernelAliasRest + "norm_types n," + Points.Pref + sKernelAliasRest + "serv_odn s " +
                         " WHERE n.nzp_serv = s.nzp_serv and s.nzp_serv_link = ttt_aid_virt0.nzp_serv" +
                         " and " + rashod.paramcalc.dat_s + " between n.date_from and n.date_to and n.is_finished and n.s_kind_norm_id = 2 " +
                         " and 0<(SELECT count(*) FROM " + Points.Pref + sKernelAliasRest + "norm_banks b" +
                         " WHERE b.norm_type_id=n.id and b.nzp_wp=" + rashod.paramcalc.nzp_wp + "))";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //создаем таблицы для хранения значений параметров по дому
            var tableName = "t_norm_sign_" + DateTime.Now.Ticks;

            ExecSQL(conn_db, "drop table " + tableName, false);
            sql = " CREATE TEMP TABLE " + tableName +
                  " (nzp_kvar INTEGER,nzp_dom INTEGER, norm_type_id INTEGER,nzp_prm_ls INTEGER,nzp_prm_house INTEGER, nzp_prm INTEGER," +
                  " type_val_sign_id INTEGER,nzp_serv INTEGER,param_value1 " + sDecimalType + ", date_value1 " + sDateTimeType + ")" + sUnlogTempTable;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //получаем сигнатуры типов нормативов
            sql = " INSERT INTO " + tableName + "(nzp_dom,norm_type_id,nzp_prm_ls,nzp_prm_house,nzp_prm,type_val_sign_id,nzp_serv)" +
                  " SELECT distinct c.nzp_dom,c.norm_type_id,s.nzp_prm_ls,s.nzp_prm_house," +
                  " CASE WHEN s.nzp_prm_ls>0 THEN s.nzp_prm_ls ELSE s.nzp_prm_house END, s.type_val_sign_id,so.nzp_serv " +
                  " FROM ttt_aid_virt0 c, " + Points.Pref + sKernelAliasRest + "norm_types_sign s," + Points.Pref + sKernelAliasRest + "serv_odn so " +
                  " WHERE c.norm_type_id=s.norm_type_id and so.nzp_serv_link = c.nzp_serv and s.nzp_prm_ls = -1 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            var listHousesParams = new Dictionary<int, KeyValuePair<int, int>>(); //список домовых параметров

            #region получаем список домовых

            //домовые
            sql = " SELECT  t.nzp_prm_house,p.prm_num,max(type_prm) as type_prm," +
                  " max(t.type_val_sign_id) as  type_val_sign_id " +
                  " FROM " + tableName + " t, " + Points.Pref + sKernelAliasRest + "prm_name p" +
                  " WHERE t.nzp_prm_house=p.nzp_prm and p.type_prm is not null" +
                  " GROUP BY 1,2 ";
            var dtHousePrmTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (var i = 0; i < dtHousePrmTable.Rows.Count; i++)
            {
                if (dtHousePrmTable.Rows[i]["type_prm"].ToString().Trim() == "serv")
                {
                    dtHousePrmTable.Rows[i]["prm_num"] = -1; //в данном случае нужно определить наличие определенной услуги у дома
                }

                var key = GetInt(dtHousePrmTable.Rows[i]["nzp_prm_house"]);
                var key1 = GetInt(dtHousePrmTable.Rows[i]["type_val_sign_id"]);
                var value = GetInt(dtHousePrmTable.Rows[i]["prm_num"]);
                listHousesParams.Add(key, new KeyValuePair<int, int>(key1, value));
            }
            #endregion


            #region проставляем значения домовых параметров
            foreach (var prm in listHousesParams)
            {
                var type_val_sign_id = prm.Value.Key;
                var param_name = "";
                switch (type_val_sign_id)
                {
                    case (int)STypeValSign.Boolean:
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.NumPeriod:
                    case (int)STypeValSign.Sprav:
                        param_name = "param_value1"; break;
                    case (int)STypeValSign.Date:
                    case (int)STypeValSign.Period:
                        param_name = "date_value1"; break;
                }
                var prm_table = rashod.paramcalc.pref + sDataAliasRest + "prm_" + prm.Value.Value;
                var where = " p.is_actual<>100 and " + rashod.paramcalc.dat_s + " between p.dat_s and p.dat_po ";
                if (prm.Value.Value == 1 || prm.Value.Value == 2 || prm.Value.Value == 17)
                {
                    prm_table = "ttt_prm_" + prm.Value.Value;
                    where = "1=1";
                }

                sql = " UPDATE " + tableName + " SET " + param_name + "=" +
                      " (SELECT  max(val_prm)" + sConvToNum + "  FROM " + prm_table + "  p" +
                      " WHERE " + where + " and p.nzp=" + tableName + ".nzp_dom" +
                      " and p.nzp_prm=" + prm.Key + ")" +
                      " WHERE nzp_prm_house=" + prm.Key;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

            }

            //проставляем нули 
            sql = " UPDATE " + tableName + " SET param_value1=0" +
                  " WHERE param_value1 is null ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //для дат
            sql = " UPDATE " + tableName + " SET date_value1="
                + Utils.EStrNull(DateTime.MinValue.ToShortDateString()) +
                  " WHERE date_value1 is null";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            #endregion

            #region получение нормативов по сигнатуре
            //получаем список всех типов нормативов 
            sql = "SELECT DISTINCT norm_type_id, nzp_serv FROM " + tableName;
            var listTypesTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

            //по каждому типу нормативов
            for (var i = 0; i < listTypesTable.Rows.Count; i++)
            {
                var norm_type_id = GetInt(listTypesTable.Rows[i]["norm_type_id"]);
                var localTableName = "t_norm_type1_" + DateTime.Now.Ticks;
                var infPrmTableName = "t_norm_type2_" + DateTime.Now.Ticks;

                sql = " SELECT * FROM " + Points.Pref + sKernelAliasRest +
                      " norm_types_sign WHERE norm_type_id=" + norm_type_id + " ORDER BY ordering";

                var typeSign = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

                sql = "CREATE TEMP TABLE " + localTableName + " (nzp_dom INTEGER, norm_tables_id INTEGER,norm_value " + sDecimalType + ",";
                var internalPrms = new NormInternalParams(typeSign.Rows.Count) { typeSign = typeSign };
                for (var j = 0; j < typeSign.Rows.Count; j++)
                {
                    GenerateWhereForNorms(ref internalPrms, j, localTableName);
                }
                //создаем таблицу с имеющимися значениями параметров дома
                sql += string.Join(",", internalPrms.param) + ")" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                sql = " INSERT INTO " + localTableName + " (nzp_dom," + string.Join(",", internalPrms.paramUpdate) + ")" +
                      " SELECT nzp_dom," + string.Join(",", internalPrms.caseStrings) +
                      " FROM " + tableName + " WHERE norm_type_id=" + norm_type_id +
                      " GROUP BY nzp_dom";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                ExecSQL(conn_db, "CREATE INDEX ixx_" + localTableName + "_1 on " + localTableName + "(nzp_dom)", true);
                ExecSQL(conn_db, sUpdStat + " " + localTableName, true);

                #region формируем таблицу для сигнатур нормативов


                sql = "CREATE TEMP TABLE " + infPrmTableName + " (id INTEGER,norm_value " + sDecimalType + ",";
                sql += string.Join(",", internalPrms.paramInf) + ")" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                sql = " INSERT INTO " + infPrmTableName + " (id, norm_value," + string.Join(",", internalPrms.paramUpdateInf) + ")" +
                   " SELECT n.id,MAX(n.norm_value) as norm," + string.Join(",", internalPrms.caseStringsInf) +
                   " FROM " + Points.Pref + sKernelAliasRest + "influence_params i, " + Points.Pref + sKernelAliasRest + "norm_tables n " +
                   " WHERE n.id=i.norm_tables_id and n.norm_type_id=" + norm_type_id +
                   " GROUP BY n.id";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                //проставляем максимальные значения из сигнатуры для параметров со значениями больше 
                for (var j = 0; j < internalPrms.listMoreVals.Count; j++)
                {
                    sql = " UPDATE " + localTableName + " SET " + internalPrms.listMoreVals[j] + " = " +
                          " (SELECT max(" + internalPrms.listMoreVals[j] + ") FROM " + infPrmTableName + ")" +
                          " WHERE " + internalPrms.listMoreVals[j] + ">(SELECT max(" + internalPrms.listMoreVals[j] + ") FROM " + infPrmTableName + " ) ";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                }

                //проставляем нормативы
                sql = "UPDATE " + localTableName + " SET norm_value=" +
               sNvlWord + "((SELECT max(norm_value) FROM " + infPrmTableName + " i WHERE " + string.Join(" and ", internalPrms.paramCompare) + "),0)";//!!!
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                //проставляем id
                sql = "UPDATE " + localTableName + " SET norm_tables_id=" +
             sNvlWord + "((SELECT max(id) FROM " + infPrmTableName + " i WHERE " + string.Join(" and ", internalPrms.paramCompare) + "),0)";//!!!
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }
                #endregion

                #region проставляем расход по нормативам по кол-ву жильцов

                //норматив 
                sql =
               " UPDATE ttt_aid_virt0" +
               " SET " +
               " norm_odn = b.norm," +
               " norm_tables_id=b.norm_tables_id" +
               " FROM (SELECT t.norm_value as norm, t.norm_tables_id,t.nzp_dom " +
                    " FROM " + localTableName + " t, ttt_aid_virt0 c WHERE t.nzp_dom=c.nzp_dom and c.norm_type_id=" + norm_type_id + ") b" +
               " WHERE ttt_aid_virt0.nzp_dom=b.nzp_dom and ttt_aid_virt0.norm_type_id=" + norm_type_id +
               " ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                ExecSQL(conn_db, " DROP TABLE " + localTableName, false);
                ExecSQL(conn_db, " DROP TABLE " + infPrmTableName, false);
                #endregion
            }
            #endregion получение нормативов по сигнатуре
            return true;
        }

        public bool GetNewNormsForODNCountersGroup(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Определение площади МОП
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
            //2465|Если нет Площади МОП на ОДН по услугам - НЕ использовать для дома|||bool||5||||
            var bUchetMopODN =
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

            // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС и НЕ был установлен параметр дома "Площадь МОП..."
            ret = ExecSQL(conn_db,
                " update ttt_aid_virt0 set is_odn=1, squ_mop = squ_dom - squ1 where squ_dom-squ1>0.000001 and is_odn=0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            #endregion

            var sql = " UPDATE ttt_aid_virt0 SET norm_type_id=( " +
                         " SELECT max(n.id) " +
                         " FROM " + Points.Pref + sKernelAliasRest + "norm_types n," + Points.Pref + sKernelAliasRest + "serv_odn s " +
                         " WHERE n.nzp_serv = s.nzp_serv and s.nzp_serv_link = ttt_aid_virt0.nzp_serv" +
                         " and " + rashod.paramcalc.dat_s + " between n.date_from and n.date_to and n.is_finished and n.s_kind_norm_id = 2 " +
                         " and 0<(SELECT count(*) FROM " + Points.Pref + sKernelAliasRest + "norm_banks b" +
                         " WHERE b.norm_type_id=n.id and b.nzp_wp=" + rashod.paramcalc.nzp_wp + "))";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }
            //создаем таблицы для хранения значений параметров по групповым приборам
            var tableName = "t_norm_sign_" + DateTime.Now.Ticks;

            ExecSQL(conn_db, "drop table " + tableName, false);
            sql = " CREATE TEMP TABLE " + tableName +
                  " (nzp_counter INTEGER,nzp_dom INTEGER, norm_type_id INTEGER,nzp_prm_ls INTEGER,nzp_prm_house INTEGER, nzp_prm INTEGER," +
                  " type_val_sign_id INTEGER,nzp_serv INTEGER,param_value1 " + sDecimalType + ", date_value1 " + sDateTimeType + ")" + sUnlogTempTable;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //получаем сигнатуры типов нормативов
            sql = " INSERT INTO " + tableName + "(nzp_counter,nzp_dom,norm_type_id,nzp_prm_ls,nzp_prm_house,nzp_prm,type_val_sign_id,nzp_serv)" +
                  " SELECT distinct c.nzp_counter,c.nzp_dom,c.norm_type_id,s.nzp_prm_ls,s.nzp_prm_house," +
                  " CASE WHEN s.nzp_prm_ls>0 THEN s.nzp_prm_ls ELSE s.nzp_prm_house END, s.type_val_sign_id,so.nzp_serv " +
                  " FROM ttt_aid_virt0 c, " + Points.Pref + sKernelAliasRest + "norm_types_sign s," + Points.Pref + sKernelAliasRest + "serv_odn so " +
                  " WHERE c.norm_type_id=s.norm_type_id and so.nzp_serv_link = c.nzp_serv ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }
            var listHousesParams = new Dictionary<int, KeyValuePair<int, int>>(); //список домовых параметров
            var listLsParams = new Dictionary<int, KeyValuePair<int, int>>();//список групповых параметров

            #region получаем список домовых и групповых параметров

            //домовые
            sql = " SELECT  t.nzp_prm_house,p.prm_num,max(type_prm) as type_prm," +
                  " max(t.type_val_sign_id) as  type_val_sign_id " +
                  " FROM " + tableName + " t, " + Points.Pref + sKernelAliasRest + "prm_name p" +
                  " WHERE t.nzp_prm_house=p.nzp_prm and p.type_prm is not null" +
                  " GROUP BY 1,2 ";
            var dtHousePrmTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (var i = 0; i < dtHousePrmTable.Rows.Count; i++)
            {
                if (dtHousePrmTable.Rows[i]["type_prm"].ToString().Trim() == "serv")
                {
                    dtHousePrmTable.Rows[i]["prm_num"] = -1; //в данном случае нужно определить наличие определенной услуги у ЛС
                }

                var key = GetInt(dtHousePrmTable.Rows[i]["nzp_prm_house"]);
                var key1 = GetInt(dtHousePrmTable.Rows[i]["type_val_sign_id"]);
                var value = GetInt(dtHousePrmTable.Rows[i]["prm_num"]);
                listHousesParams.Add(key, new KeyValuePair<int, int>(key1, value));
            }

            //по групповым ПУ
            sql = " SELECT  t.nzp_prm_ls,p.prm_num,max(type_prm) as type_prm," +
                  " max(t.type_val_sign_id) as  type_val_sign_id" +
                " FROM " + tableName + " t, " + Points.Pref + sKernelAliasRest + "prm_name p" +
                " WHERE t.nzp_prm_ls=p.nzp_prm and p.type_prm is not null" +
                " GROUP BY 1,2 ";
            var dtLsPrmTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (var i = 0; i < dtLsPrmTable.Rows.Count; i++)
            {
                if (dtLsPrmTable.Rows[i]["type_prm"].ToString().Trim() == "serv")
                {
                    dtLsPrmTable.Rows[i]["prm_num"] = -1; //в данном случае нужно определить наличие определенной услуги у ЛС
                }
                var key = GetInt(dtLsPrmTable.Rows[i]["nzp_prm_ls"]);
                var key1 = GetInt(dtLsPrmTable.Rows[i]["type_val_sign_id"]);
                var value = GetInt(dtLsPrmTable.Rows[i]["prm_num"]);
                listLsParams.Add(key, new KeyValuePair<int, int>(key1, value));
            }

            #endregion

            #region Заполняем значения параметров для Групповых счетчиков

            #region проставляем значения домовых параметров
            foreach (var prm in listHousesParams)
            {
                var type_val_sign_id = prm.Value.Key;
                var param_name = "";
                switch (type_val_sign_id)
                {
                    case (int)STypeValSign.Boolean:
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.NumPeriod:
                    case (int)STypeValSign.Sprav:
                        param_name = "param_value1"; break;
                    case (int)STypeValSign.Date:
                    case (int)STypeValSign.Period:
                        param_name = "date_value1"; break;
                }
                var prm_table = rashod.paramcalc.pref + sDataAliasRest + "prm_" + prm.Value.Value;
                var where = " p.is_actual<>100 and " + rashod.paramcalc.dat_s + " between p.dat_s and p.dat_po ";
                if (prm.Value.Value == 1 || prm.Value.Value == 2 || prm.Value.Value == 17)
                {
                    prm_table = "ttt_prm_" + prm.Value.Value;
                    where = "1=1";
                }

                sql = " UPDATE " + tableName + " SET " + param_name + "=" +
                      " (SELECT  max(val_prm)" + sConvToNum + "  FROM " + prm_table + "  p" +
                      " WHERE " + where + " and p.nzp=" + tableName + ".nzp_dom" +
                      " and p.nzp_prm=" + prm.Key + ")" +
                      " WHERE nzp_prm_house=" + prm.Key;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

            }

            #endregion
            #region проставляем значения квартирных параметров(в данном случае параметров групповых счетчиков)

            foreach (var prm in listLsParams)
            {
                var type_val_sign_id = prm.Value.Key;
                var param_name = "";
                var conv_num = "";
                switch (type_val_sign_id)
                {
                    case (int)STypeValSign.Boolean:
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.NumPeriod:
                    case (int)STypeValSign.Sprav:
                        param_name = "param_value1";
                        conv_num = sConvToNum; break;
                    case (int)STypeValSign.Date:
                    case (int)STypeValSign.Period:
                        param_name = "date_value1";
                        conv_num = "::date "; break;
                }
                // проверка наличия услуги для групповых приборов
                if (prm.Value.Value < 0)
                {
                    sql = " SELECT max(prm_num) FROM " + Points.Pref + sKernelAliasRest + "prm_name where nzp_prm=" +
                          prm.Key;
                    var obj = ExecScalar(conn_db, sql, out ret, true);
                    var prm_num = 0;
                    if (ret.result) int.TryParse(obj.ToString(), out prm_num);

                    sql = " SELECT trim(max(val_prm)) FROM " + Points.Pref + sDataAliasRest + "prm_" + prm_num +
                          " WHERE nzp_prm=" + prm.Key;
                    obj = ExecScalar(conn_db, sql, out ret, true);
                    var nzp_serv = 0; //номер услуги для проверки на ее наличие у групповых приборов
                    if (ret.result) int.TryParse(obj.ToString(), out nzp_serv);
                    //проверяем наличие услуги у групповых приборов
                    sql = " UPDATE " + tableName + " SET " + param_name + "=" +
                          " (CASE WHEN 0<(SELECT count(t.*) FROM ttt_aid_virt0 t WHERE t.nzp_counter=" + tableName +
                          ".nzp_counter and t.nzp_serv=" + nzp_serv + " ) THEN 1 ELSE 0 END)" +
                          " WHERE nzp_prm_ls=" + prm.Key;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                }
                else
                {
                    var prm_table = rashod.paramcalc.pref + sDataAliasRest + "prm_" + prm.Value.Value;
                    var where = " p.is_actual<>100 and " + rashod.paramcalc.dat_s + " between p.dat_s and p.dat_po ";
                    if (prm.Value.Value == 1 || prm.Value.Value == 2 || prm.Value.Value == 17)
                    {
                        prm_table = "ttt_prm_" + prm.Value.Value;
                        where = "1=1";
                    }

                    sql = " UPDATE " + tableName + " SET " + param_name + "=(" +
                          sNvlWord + " ((SELECT max(val_prm) " + conv_num + " FROM " + prm_table + " p" +
                          " WHERE " + where + " and p.nzp=" + tableName + ".nzp_counter" +
                          " and p.nzp_prm=" + prm.Key + "), " + tableName + "." + param_name + "))" +
                          " WHERE nzp_prm_ls=" + prm.Key;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }

                }
            }
            #endregion

            //проставляем нули 
            sql = " UPDATE " + tableName + " SET param_value1=0" +
                  " WHERE param_value1 is null ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //для дат
            sql = " UPDATE " + tableName + " SET date_value1="
                + Utils.EStrNull(DateTime.MinValue.ToShortDateString()) +
                  " WHERE date_value1 is null";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            #endregion

            #region получение нормативов по сигнатуре
            //получаем список всех типов нормативов
            sql = "SELECT DISTINCT norm_type_id, nzp_serv FROM " + tableName;
            var listTypesTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

            //по каждому типу нормативов
            for (var i = 0; i < listTypesTable.Rows.Count; i++)
            {
                var norm_type_id = GetInt(listTypesTable.Rows[i]["norm_type_id"]);
                var localTableName = "t_norm_type1_" + DateTime.Now.Ticks;
                var infPrmTableName = "t_norm_type2_" + DateTime.Now.Ticks;

                sql = " SELECT * FROM " + Points.Pref + sKernelAliasRest +
                      " norm_types_sign WHERE norm_type_id=" + norm_type_id + " ORDER BY ordering";

                var typeSign = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

                sql = "CREATE TEMP TABLE " + localTableName + " (nzp_counter INTEGER, norm_tables_id INTEGER,norm_value " + sDecimalType + ",";
                var internalPrms = new NormInternalParams(typeSign.Rows.Count) { typeSign = typeSign };
                for (var j = 0; j < typeSign.Rows.Count; j++)
                {
                    GenerateWhereForNorms(ref internalPrms, j, localTableName);
                }
                //создаем таблицу с имеющимися значениями параметров групповых приборов
                sql += string.Join(",", internalPrms.param) + ")" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                sql = " INSERT INTO " + localTableName + " (nzp_counter," + string.Join(",", internalPrms.paramUpdate) + ")" +
                      " SELECT nzp_counter," + string.Join(",", internalPrms.caseStrings) +
                      " FROM " + tableName + " WHERE norm_type_id=" + norm_type_id +
                      " GROUP BY nzp_counter";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                ExecSQL(conn_db, "CREATE INDEX ixx_" + localTableName + "_1 on " + localTableName + "(nzp_counter)", true);
                ExecSQL(conn_db, sUpdStat + " " + localTableName, true);

                #region формируем таблицу для сигнатур нормативов


                sql = "CREATE TEMP TABLE " + infPrmTableName + " (id INTEGER,norm_value " + sDecimalType + ",";
                sql += string.Join(",", internalPrms.paramInf) + ")" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                sql = " INSERT INTO " + infPrmTableName + " (id, norm_value," + string.Join(",", internalPrms.paramUpdateInf) + ")" +
                   " SELECT n.id,MAX(n.norm_value) as norm," + string.Join(",", internalPrms.caseStringsInf) +
                   " FROM " + Points.Pref + sKernelAliasRest + "influence_params i, " + Points.Pref + sKernelAliasRest + "norm_tables n " +
                   " WHERE n.id=i.norm_tables_id and n.norm_type_id=" + norm_type_id +
                   " GROUP BY n.id";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                //проставляем максимальные значения из сигнатуры для параметров со значениями больше 
                for (var j = 0; j < internalPrms.listMoreVals.Count; j++)
                {
                    sql = " UPDATE " + localTableName + " SET " + internalPrms.listMoreVals[j] + " = " +
                          " (SELECT max(" + internalPrms.listMoreVals[j] + ") FROM " + infPrmTableName + ")" +
                          " WHERE " + internalPrms.listMoreVals[j] + ">(SELECT max(" + internalPrms.listMoreVals[j] + ") FROM " + infPrmTableName + " ) ";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                }

                //проставляем нормативы
                sql = "UPDATE " + localTableName + " SET norm_value=" +
               sNvlWord + "((SELECT max(norm_value) FROM " + infPrmTableName + " i WHERE " + string.Join(" and ", internalPrms.paramCompare) + "),0)";//!!!
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                //проставляем id
                sql = "UPDATE " + localTableName + " SET norm_tables_id=" +
             sNvlWord + "((SELECT max(id) FROM " + infPrmTableName + " i WHERE " + string.Join(" and ", internalPrms.paramCompare) + "),0)";//!!!
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }
                #endregion

                #region проставляем расход по нормативам по кол-ву жильцов
                //норматив 
                sql = " UPDATE ttt_aid_virt0 SET " +
                          " norm_odn = b.norm FROM (SELECT t.norm_value as norm, t.norm_tables_id,t.nzp_counter " +
                          " FROM " + localTableName + " t, ttt_aid_virt0 c WHERE t.nzp_counter=c.nzp_counter and c.norm_type_id=" + norm_type_id + ") b" +
                          " WHERE ttt_aid_virt0.norm_type_id=" + norm_type_id +
                          " and ttt_aid_virt0.nzp_counter=b.nzp_counter;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }
                ExecSQL(conn_db, " DROP TABLE " + localTableName, false);
                ExecSQL(conn_db, " DROP TABLE " + infPrmTableName, false);
                #endregion
            }
            #endregion получение нормативов по сигнатуре
            return true;
        }

        public bool GetNewNormsForOtpl(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        {
            ret = Utils.InitReturns();
            var sql = " UPDATE t_norm_otopl SET norm_type_id=( " +
                         " SELECT n.id " +
                         " FROM " + Points.Pref + sKernelAliasRest + "norm_types n " +
                         " WHERE n.nzp_serv = 8 " +
                         " and t_norm_otopl.vid_alg = 5 " +
                         " and " + rashod.paramcalc.dat_s + " between n.date_from and n.date_to and n.is_finished=true " +
                         " and 0<(SELECT count(*) FROM " + Points.Pref + sKernelAliasRest + "norm_banks b" +
                         " WHERE b.norm_type_id=n.id and b.nzp_wp=" + rashod.paramcalc.nzp_wp + "))";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //создаем таблицы для хранения значений параметров по лс
            var tableName = "t_norm_sign_" + DateTime.Now.Ticks;

            sql = " CREATE TEMP TABLE " + tableName +
                  " (nzp_kvar INTEGER,nzp_dom INTEGER, norm_type_id INTEGER,nzp_prm_ls INTEGER,nzp_prm_house INTEGER, nzp_prm INTEGER," +
                  " type_val_sign_id INTEGER,nzp_serv INTEGER,param_value1 " + sDecimalType + ", date_value1 " + sDateTimeType + ")" + sUnlogTempTable;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //получаем сигнатуры типов нормативов
            sql = " INSERT INTO " + tableName + "(nzp_kvar,nzp_dom,norm_type_id,nzp_prm_ls,nzp_prm_house,nzp_prm,type_val_sign_id,nzp_serv)" +
                  " SELECT c.nzp_kvar,c.nzp_dom,c.norm_type_id,s.nzp_prm_ls,s.nzp_prm_house," +
                  " CASE WHEN s.nzp_prm_ls>0 THEN s.nzp_prm_ls ELSE s.nzp_prm_house END, s.type_val_sign_id,8 " +
                  " FROM t_norm_otopl c, " + Points.Pref + sKernelAliasRest + "norm_types_sign s " +
                  " WHERE c.norm_type_id=s.norm_type_id and c.vid_alg = 5";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            var listHousesParams = new Dictionary<int, KeyValuePair<int, int>>(); //список домовых параметров
            var listLsParams = new Dictionary<int, KeyValuePair<int, int>>();//список квартирных параметров

            #region получаем список домовых и квартирных параметров

            //домовые
            sql = " SELECT  t.nzp_prm_house,p.prm_num,max(type_prm) as type_prm," +
                  " max(t.type_val_sign_id) as  type_val_sign_id " +
                  " FROM " + tableName + " t, " + Points.Pref + sKernelAliasRest + "prm_name p" +
                  " WHERE t.nzp_prm_house=p.nzp_prm and p.type_prm is not null" +
                  " GROUP BY 1,2 ";
            var dtHousePrmTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (var i = 0; i < dtHousePrmTable.Rows.Count; i++)
            {
                if (dtHousePrmTable.Rows[i]["type_prm"].ToString().Trim() == "serv")
                {
                    dtHousePrmTable.Rows[i]["prm_num"] = -1; //в данном случае нужно определить наличие определенной услуги у ЛС
                }

                var key = GetInt(dtHousePrmTable.Rows[i]["nzp_prm_house"]);
                var key1 = GetInt(dtHousePrmTable.Rows[i]["type_val_sign_id"]);
                var value = GetInt(dtHousePrmTable.Rows[i]["prm_num"]);
                listHousesParams.Add(key, new KeyValuePair<int, int>(key1, value));
            }

            //квартирные
            sql = " SELECT  t.nzp_prm_ls,p.prm_num,max(type_prm) as type_prm," +
                  " max(t.type_val_sign_id) as  type_val_sign_id" +
                " FROM " + tableName + " t, " + Points.Pref + sKernelAliasRest + "prm_name p" +
                " WHERE t.nzp_prm_ls=p.nzp_prm and p.type_prm is not null" +
                " GROUP BY 1,2 ";
            var dtLsPrmTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (var i = 0; i < dtLsPrmTable.Rows.Count; i++)
            {
                if (dtLsPrmTable.Rows[i]["type_prm"].ToString().Trim() == "serv")
                {
                    dtLsPrmTable.Rows[i]["prm_num"] = -1; //в данном случае нужно определить наличие определенной услуги у ЛС
                }
                var key = GetInt(dtLsPrmTable.Rows[i]["nzp_prm_ls"]);
                var key1 = GetInt(dtLsPrmTable.Rows[i]["type_val_sign_id"]);
                var value = GetInt(dtLsPrmTable.Rows[i]["prm_num"]);
                listLsParams.Add(key, new KeyValuePair<int, int>(key1, value));
            }

            #endregion

            #region Заполняем значения параметров для ЛС

            #region проставляем значения домовых параметров
            foreach (var prm in listHousesParams)
            {
                var type_val_sign_id = prm.Value.Key;
                var param_name = "";
                switch (type_val_sign_id)
                {
                    case (int)STypeValSign.Boolean:
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.NumPeriod:
                    case (int)STypeValSign.Sprav:
                        param_name = "param_value1"; break;
                    case (int)STypeValSign.Date:
                    case (int)STypeValSign.Period:
                        param_name = "date_value1"; break;
                }
                var prm_table = rashod.paramcalc.pref + sDataAliasRest + "prm_" + prm.Value.Value;
                var where = " p.is_actual<>100 and " + rashod.paramcalc.dat_s + " between p.dat_s and p.dat_po ";
                if (prm.Value.Value == 1 || prm.Value.Value == 2)
                {
                    prm_table = "ttt_prm_" + prm.Value.Value;
                    where = "1=1";
                }

                sql = " UPDATE " + tableName + " SET " + param_name + "=" +
                      " (SELECT  max(val_prm)" + sConvToNum + "  FROM " + prm_table + "  p" +
                      " WHERE " + where + " and p.nzp=" + tableName + ".nzp_dom" +
                      " and p.nzp_prm=" + prm.Key + ")" +
                      " WHERE nzp_prm_house=" + prm.Key;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

            }
            #endregion
            #region проставляем значения квартирных параметров
            foreach (var prm in listLsParams)
            {
                var type_val_sign_id = prm.Value.Key;
                var param_name = "";
                var conv_num = "";
                switch (type_val_sign_id)
                {
                    case (int)STypeValSign.Boolean:
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.NumPeriod:
                    case (int)STypeValSign.Sprav:
                        param_name = "param_value1";
                        conv_num = sConvToNum; break;
                    case (int)STypeValSign.Date:
                    case (int)STypeValSign.Period:
                        param_name = "date_value1";
                        conv_num = "::date "; break;
                }
                // проверка наличия услуги для ЛС
                if (prm.Value.Value < 0)
                {
                    sql = " SELECT max(prm_num) FROM " + Points.Pref + sKernelAliasRest + "prm_name where nzp_prm=" +
                          prm.Key;
                    var obj = ExecScalar(conn_db, sql, out ret, true);
                    var prm_num = 0;
                    if (ret.result) int.TryParse(obj.ToString(), out prm_num);

                    sql = " SELECT trim(max(val_prm)) FROM " + Points.Pref + sDataAliasRest + "prm_" + prm_num +
                          " WHERE nzp_prm=" + prm.Key;
                    obj = ExecScalar(conn_db, sql, out ret, true);
                    var nzp_serv = 0; //номер услуги для проверки на ее наличие у ЛС
                    if (ret.result) int.TryParse(obj.ToString(), out nzp_serv);
                    //проверяем наличие услуги у ЛС
                    sql = " UPDATE " + tableName + " SET " + param_name + "=" +
                          " (CASE WHEN 0<(SELECT count(t.*) FROM t_norm_otopl t WHERE t.nzp_kvar=" + tableName +
                          ".nzp_kvar ) THEN 1 ELSE 0 END)" +
                          " WHERE nzp_prm_ls=" + prm.Key;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                }
                else
                {
                    var prm_table = rashod.paramcalc.pref + sDataAliasRest + "prm_" + prm.Value.Value;
                    var where = " p.is_actual<>100 and " + rashod.paramcalc.dat_s + " between p.dat_s and p.dat_po ";
                    if (prm.Value.Value == 1 || prm.Value.Value == 2)
                    {
                        prm_table = "ttt_prm_" + prm.Value.Value;
                        where = "1=1";
                    }

                    sql = " UPDATE " + tableName + " SET " + param_name + "=" +
                        sNvlWord + "(val_prm" + conv_num + ", " + tableName + "." + param_name + ")" +
                        " FROM " + prm_table + " p" +
                        " WHERE " + where + " " +
                        " AND p.nzp=" + tableName + ".nzp_kvar" +
                        " AND p.nzp_prm=" + prm.Key +
                        " AND nzp_prm_ls=" + prm.Key;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }

                }
            }
            #endregion

            //проставляем нули 
            sql = " UPDATE " + tableName + " SET param_value1=0" +
                  " WHERE param_value1 is null ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //для дат
            sql = " UPDATE " + tableName + " SET date_value1="
                + Utils.EStrNull(DateTime.MinValue.ToShortDateString()) +
                  " WHERE date_value1 is null";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            #endregion

            #region получение нормативов по сигнатуре


            //получаем список всех типов нормативов
            sql = "SELECT DISTINCT norm_type_id, nzp_serv FROM " + tableName;
            var listTypesTable = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

            //по каждому типу нормативов
            for (var i = 0; i < listTypesTable.Rows.Count; i++)
            {
                var norm_type_id = GetInt(listTypesTable.Rows[i]["norm_type_id"]);
                var localTableName = "t_norm_type1_" + DateTime.Now.Ticks;
                var infPrmTableName = "t_norm_type2_" + DateTime.Now.Ticks;

                sql = " SELECT * FROM " + Points.Pref + sKernelAliasRest +
                      " norm_types_sign WHERE norm_type_id=" + norm_type_id + " ORDER BY ordering";

                var typeSign = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

                sql = "CREATE TEMP TABLE " + localTableName + " (nzp_kvar INTEGER, norm_tables_id INTEGER,norm_value " + sDecimalType + ",";
                var internalPrms = new NormInternalParams(typeSign.Rows.Count) { typeSign = typeSign };
                for (var j = 0; j < typeSign.Rows.Count; j++)
                {
                    GenerateWhereForNorms(ref internalPrms, j, localTableName);
                }
                //создаем таблицу с имеющимися значениями параметров лс 
                sql += string.Join(",", internalPrms.param) + ")" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                sql = " INSERT INTO " + localTableName + " (nzp_kvar," + string.Join(",", internalPrms.paramUpdate) + ")" +
                      " SELECT nzp_kvar," + string.Join(",", internalPrms.caseStrings) +
                      " FROM " + tableName + " WHERE norm_type_id=" + norm_type_id +
                      " GROUP BY nzp_kvar";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                ExecSQL(conn_db, "CREATE INDEX ixx_" + localTableName + "_1 on " + localTableName + "(nzp_kvar)", true);
                ExecSQL(conn_db, sUpdStat + " " + localTableName, true);

                #region формируем таблицу для сигнатур нормативов


                sql = "CREATE TEMP TABLE " + infPrmTableName + " (id INTEGER,norm_value " + sDecimalType + ",";
                sql += string.Join(",", internalPrms.paramInf) + ")" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                sql = " INSERT INTO " + infPrmTableName + " (id, norm_value," + string.Join(",", internalPrms.paramUpdateInf) + ")" +
                   " SELECT n.id,MAX(n.norm_value) as norm," + string.Join(",", internalPrms.caseStringsInf) +
                   " FROM " + Points.Pref + sKernelAliasRest + "influence_params i, " + Points.Pref + sKernelAliasRest + "norm_tables n " +
                   " WHERE n.id=i.norm_tables_id and n.norm_type_id=" + norm_type_id +
                   " GROUP BY n.id";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                //проставляем максимальные значения из сигнатуры для параметров со значениями больше 
                for (var j = 0; j < internalPrms.listMoreVals.Count; j++)
                {
                    sql = " UPDATE " + localTableName + " SET " + internalPrms.listMoreVals[j] + " = " +
                          " (SELECT max(" + internalPrms.listMoreVals[j] + ") FROM " + infPrmTableName + ")" +
                          " WHERE " + internalPrms.listMoreVals[j] + ">(SELECT max(" + internalPrms.listMoreVals[j] + ") FROM " + infPrmTableName + " ) ";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return false;
                    }
                }

                //проставляем нормативы и id
                sql = "UPDATE " + localTableName + " SET norm_value=" +
               sNvlWord + "(i.norm_value,0),  norm_tables_id=i.id " +
               " FROM " + infPrmTableName + " i WHERE " + string.Join(" and ", internalPrms.paramCompare);
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

                #endregion

                #region проставляем расход по нормативам по кол-ву жильцов
                sql = " UPDATE t_norm_otopl SET " +
                        " rashod_gkal_m2 = b.norm FROM (SELECT t.norm_value as norm, t.norm_tables_id,t.nzp_counter " +
                        " FROM " + localTableName + " t, t_norm_otopl c WHERE t.nzp_kvar=c.nzp_kvar and c.norm_type_id=" + norm_type_id + ") b" +
                        " WHERE t_norm_otopl.norm_type_id=" + norm_type_id +
                        " and t_norm_otopl.nzp_kvar=b.nzp_kvar and t_norm_otopl.vid_alg = 5;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }
                ExecSQL(conn_db, " DROP TABLE " + localTableName, false);
                ExecSQL(conn_db, " DROP TABLE " + infPrmTableName, false);
                #endregion
            }

            ExecSQL(conn_db, " DROP TABLE " + tableName, false);
            #endregion
            return true;
        }

        public class NormInternalParams
        {
            public string[] param { get; set; }
            public string[] paramInf { get; set; }
            public string[] paramUpdate { get; set; }
            public string[] paramUpdateInf { get; set; }
            public string[] paramCompare { get; set; }
            public string[] caseStrings { get; set; }
            public string[] caseStringsInf { get; set; }
            public List<string> listMoreVals { get; set; }

            public DataTable typeSign { get; set; }

            public NormInternalParams(int count)
            {
                param = new string[count];
                paramInf = new string[count];
                paramUpdate = new string[count];
                paramUpdateInf = new string[count];
                paramCompare = new string[count];
                caseStrings = new string[count];
                caseStringsInf = new string[count];
                listMoreVals = new List<string>();
            }

        }

        /// <summary>
        /// конструктор условий для получения нормативов
        /// </summary>
        /// <param name="internalParams"></param>
        /// <param name="j"></param>
        /// <param name="localTableName"></param>
        protected void GenerateWhereForNorms(ref NormInternalParams internalParams, int j, string localTableName)
        {
            var nzp_prm_ls = GetInt(internalParams.typeSign.Rows[j]["nzp_prm_ls"]);
            var nzp_prm = nzp_prm_ls > 0 ? nzp_prm_ls : GetInt(internalParams.typeSign.Rows[j]["nzp_prm_house"]);
            var type_val_sign_id = GetInt(internalParams.typeSign.Rows[j]["type_val_sign_id"]);
            switch (type_val_sign_id)
            {
                case (int)STypeValSign.NumPeriod:
                    internalParams.param[j] = string.Format("param_value{0} {1}", j, sDecimalType);
                    internalParams.paramInf[j] = string.Format("param_value{0}_from {1}, param_value{0}_to {1}", j, sDecimalType);
                    internalParams.paramUpdate[j] = string.Format("param_value{0}", j);
                    internalParams.paramUpdateInf[j] = string.Format("{0}_from, {0}_to", internalParams.paramUpdate[j]);
                    internalParams.paramCompare[j] = string.Format("{1}.param_value{0} BETWEEN i.{2}_from AND i.{2}_to ", j, localTableName, internalParams.paramUpdate[j]);
                    internalParams.caseStrings[j] = string.Format("MAX(CASE WHEN nzp_prm={0} THEN param_value1 END) as {1}", nzp_prm, internalParams.paramUpdate[j]);
                    internalParams.caseStringsInf[j] = string.Format(" MAX(CASE WHEN nzp_prm={0} THEN param_value1 END) as {1}_from," +
                                        " MAX(CASE WHEN nzp_prm={0} THEN param_value2 END) as {1}_to", nzp_prm, internalParams.paramUpdate[j]);
                    break;
                case (int)STypeValSign.Num:
                    internalParams.param[j] = string.Format("param_value{0} {1}", j, sDecimalType);
                    internalParams.paramInf[j] = internalParams.param[j];
                    internalParams.paramUpdate[j] = string.Format("param_value{0}", j);
                    internalParams.paramUpdateInf[j] = internalParams.paramUpdate[j];
                    internalParams.paramCompare[j] = string.Format("i.param_value{0} = {1}.param_value{0}", j, localTableName);
                    internalParams.caseStrings[j] = string.Format("MAX(CASE WHEN nzp_prm={0} THEN param_value1 END) as {1}", nzp_prm, internalParams.paramUpdate[j]);
                    internalParams.caseStringsInf[j] = internalParams.caseStrings[j];
                    internalParams.listMoreVals.Add(internalParams.paramUpdate[j]);
                    break;
                case (int)STypeValSign.Boolean:
                case (int)STypeValSign.Sprav:
                    internalParams.param[j] = string.Format("param_value{0} {1}", j, sDecimalType);
                    internalParams.paramInf[j] = internalParams.param[j];
                    internalParams.paramUpdate[j] = string.Format("param_value{0}", j);
                    internalParams.paramUpdateInf[j] = internalParams.paramUpdate[j];
                    internalParams.paramCompare[j] = string.Format("i.param_value{0} = {1}.param_value{0}", j, localTableName);
                    internalParams.caseStrings[j] = string.Format("MAX(CASE WHEN nzp_prm={0} THEN param_value1 END) as {1}", nzp_prm, internalParams.paramUpdate[j]);
                    internalParams.caseStringsInf[j] = internalParams.caseStrings[j];
                    break;
                case (int)STypeValSign.Date:
                    internalParams.param[j] = string.Format("date_value{0} {1}", j, "date");
                    internalParams.paramInf[j] = internalParams.param[j];
                    internalParams.paramUpdate[j] = string.Format("date_value{0}", j);
                    internalParams.paramUpdateInf[j] = internalParams.paramUpdate[j];
                    internalParams.paramCompare[j] = string.Format("i.date_value{0} = {1}.date_value{0}", j, localTableName);
                    internalParams.caseStrings[j] = string.Format("MAX(CASE WHEN nzp_prm={0} THEN date_value1 END) as {1}", nzp_prm, internalParams.paramUpdate[j]);
                    internalParams.caseStringsInf[j] = internalParams.caseStrings[j];
                    break;
                case (int)STypeValSign.Period:
                    internalParams.param[j] = string.Format("date_value{0} {1}", j, "date");
                    internalParams.paramInf[j] = string.Format("date_value{0}_from {1}, date_value{0}_to {1}", j, "date");
                    internalParams.paramUpdate[j] = string.Format("date_value{0}", j);
                    internalParams.paramUpdateInf[j] = string.Format("{0}_from, {0}_to", internalParams.paramUpdate[j]);
                    internalParams.paramCompare[j] = string.Format("{1}.date_value{0} BETWEEN i.{2}_from AND i.{2}_to ", j, localTableName, internalParams.paramUpdate[j]);
                    internalParams.caseStrings[j] = string.Format("MAX(CASE WHEN nzp_prm={0} THEN date_value1 END) as {1}", nzp_prm, internalParams.paramUpdate[j]);
                    internalParams.caseStringsInf[j] = string.Format(" MAX(CASE WHEN nzp_prm={0} THEN date_value1 END) as {1}_from," +
                                        " MAX(CASE WHEN nzp_prm={0} THEN date_value2 END) as {1}_to", nzp_prm, internalParams.paramUpdate[j]);
                    break;
            }
        }
    }
}

#endregion здесь производится подсчет расходов
