using System.Globalization;
using FastReport;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;

    public class BezenchukFakturaKaprOtopl : BezenchukFakturaKapr
    {
        private const int MaxCountersCount = 6;
        private const decimal SumTicketKapr = 0;
        private const int MaxStringCount = 4;
        private const int MaxKomponentCount = 2;
        

        /// <Summary>
        /// Заполнение данных таблицы начислений
        /// </Summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillMainChargeGrid(DataRow dr)
        {

            if (dr == null) return false;
            if (GvsNormGkal == 0) GvsNormGkal = 0.0611m;
            SetServRashod();
            ListServ.Sort();
            int numberString = 1;
            int numberKomponent = 1;
            bool hasKapr = false;
            List<BaseServ> ListKomp = new List<BaseServ>();
            List<BaseServ> ListServCopy = new List<BaseServ>();
            ListServCopy.AddRange(ListServ);
            //переносиим компонеты в новые список
            foreach (BaseServ t in ListServ)
            {
                if (t.Serv.NzpServ == 9 || t.Serv.NzpServ == 14)
                {
                    ListKomp.Add(t);
                    ListServCopy.Remove(t);
                }
            }
            ListServ = ListServCopy;

            //создаем услугу по гвс из объединения компонент
            if (ListKomp.Count > 0)
            {
                BaseServ GvsServ = new BaseServ(true);
                GvsServ.Serv.NzpServ = -1;
                if (HasOpenVodozabor)
                {
                    GvsServ.Serv.NameServ = "Нагрет.т/носит.";
                }
                else
                {
                    GvsServ.Serv.NameServ = "ГВС";
                }

                for (int i = 0; i < ListKomp.Count; i++)
                {
                    GvsServ.Serv.RsumTarif += ListKomp[i].Serv.RsumTarif;
                    GvsServ.ServOdn.RsumTarif += ListKomp[i].ServOdn.RsumTarif;
                    GvsServ.Serv.Reval += (ListKomp[i].Serv.Reval + ListKomp[i].Serv.RealCharge);
                    GvsServ.Serv.SumCharge += ListKomp[i].Serv.SumCharge;
                    GvsServ.ServOdn.SumCharge += ListKomp[i].ServOdn.SumCharge;
                    GvsServ.Serv.NameSupp = ListKomp[i].Serv.NameSupp;
                }
                ListServ.Add(GvsServ);
            }

            #region Цикл по услугам

            foreach (BaseServ t in ListServ)
            {
                if (numberString >= MaxStringCount) break;
                if (IsShowServInGrid(t) || (t.Serv.NzpServ == -1))
                {
                    if (t.Serv.NzpServ == 206)
                    {
                        dr["measure0"] = t.Serv.Measure.Trim();
                        dr["c_calc0"] = t.Serv.CCalc.ToString("0.00##") +
                                        GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                        dr["tarif0"] = t.Serv.Tarif.ToString("0.00");
                        dr["rsum_tarif_all0"] = t.Serv.RsumTarif.ToString("0.00");
                        dr["reval0"] = (t.Serv.Reval + t.Serv.RealCharge).ToString("0.00");
                        dr["sum_charge_all0"] = SumTicketKapr.ToString("0.00"); //t.Serv.SumCharge.ToString("0.00");
                        dr["sum_ticketKapr"] = SumTicketKapr.ToString("0.00");
                        SummaryServ.Serv.SumCharge -= t.Serv.SumCharge;
                        SummaryServ.Serv.SumInsaldo -= t.Serv.SumInsaldo;
                        SummaryServ.Serv.SumMoney -= t.Serv.SumMoney;
                        SummaryServ.Serv.Reval -= t.Serv.Reval;
                        SummaryServ.Serv.RealCharge -= t.Serv.RealCharge;
                        SummaryServ.Serv.RsumTarif -= t.Serv.RsumTarif;

                        SummaryServ.ServOdn.SumCharge -= t.ServOdn.SumCharge;
                        SummaryServ.ServOdn.SumInsaldo -= t.ServOdn.SumInsaldo;
                        SummaryServ.ServOdn.SumMoney -= t.ServOdn.SumMoney;
                        SummaryServ.ServOdn.Reval -= t.ServOdn.Reval;
                        SummaryServ.ServOdn.RealCharge -= t.ServOdn.RealCharge;
                        SummaryServ.ServOdn.RsumTarif -= t.ServOdn.RsumTarif;
                        hasKapr = true;

                    }
                    else
                    {
                        if (t.Serv.NzpServ == -1)
                        {
                            dr["name_serv" + numberString] = (t.Serv.NameSupp.Trim().Length > 17
                                ? t.Serv.NameServ + t.Serv.NameSupp.Trim().Remove(16) + "*"
                                : t.Serv.NameServ.Trim() + t.Serv.NameSupp.Trim()) + "*";
                        }
                        else
                        {
                            dr["name_serv" + numberString] = t.Serv.NameServ.Trim() + t.Serv.NameSupp.Trim();
                        }
                        dr["measure" + numberString] = t.Serv.Measure.Trim();

                        if ((Math.Abs(t.Serv.CCalc) > 0.00001m) &
                            (t.Serv.IsOdn == false))
                        {

                            if ((t.Serv.RsumTarif ==
                                 t.ServOdn.RsumTarif) & (t.Serv.RsumTarif > 0.001m))
                            {
                            }
                            else
                            {
                                if (Math.Abs(t.Serv.RsumTarif) > 0.001m)
                                    dr["c_calc" + numberString] = t.Serv.CCalc.ToString("0.00000") +
                                                                  GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                else if (Math.Abs(t.ServOdn.CCalc) > 0.00001m)
                                {
                                    dr["c_calc" + numberString] = t.Serv.CCalc.ToString("0.00000") +
                                                                  GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                }



                            }

                        }


                        if (Math.Abs(t.ServOdn.CCalc) > 0.00001m)
                        {

                            string sourceOdn = "(1)";

                            if ((t.Serv.NzpServ == 6) & (HasHvsDpu)) sourceOdn = "(4)";
                            if ((t.Serv.NzpServ == 9) & (HasGvsDpu)) sourceOdn = "(4)";
                            if ((t.Serv.NzpServ == 14) & (HasGvsDpu)) sourceOdn = "(4)";
                            if ((t.Serv.NzpServ == 25) & (HasElDpu)) sourceOdn = "(4)";

                            dr["c_calc_odn" + numberString] = t.ServOdn.CCalc.ToString("0.00000") +
                                                              sourceOdn;
                        }

                        if (t.Serv.NzpServ == 7)
                        {
                            if (t.Serv.NzpFrm == 26907209)
                            {
                                foreach (ServVolume t1 in ListVolume)
                                    if (t1.NzpServ == 7)
                                        t1.NormaVolume = KanNormCalc;
                            }
                        }



                        if (Math.Abs(t.Serv.Tarif) > 0.001m)
                        {
                            dr["tarif" + numberString] = t.Serv.Tarif.ToString("0.00");
                        }

                        if (((t.Serv.NzpServ == 6) ||
                             (t.Serv.NzpServ == 7)) & (t.Serv.NzpMeasure != 3))
                        {
                            if (t.Serv.Norma > 0.001m)
                            {
                                dr["tarif" + numberString] = (t.Serv.Tarif /
                                                              t.Serv.Norma).ToString("0.00");

                            }
                            dr["measure" + numberString] = "Куб.м.";
                        }

                        //if (t.Serv.NzpServ > 0)
                        if (Math.Abs(t.Serv.RsumTarif -
                                     t.ServOdn.RsumTarif) > 0.001m)
                        {
                            dr["rsum_tarif" + numberString] = (t.Serv.RsumTarif -
                                                               t.ServOdn.RsumTarif).ToString("0.00");
                        }
                        //if (t.Serv.NzpServ > 0)
                        if (Math.Abs(t.ServOdn.RsumTarif) > 0.001m)
                        {
                            dr["rsum_tarif_odn" + numberString] = t.ServOdn.RsumTarif.ToString("0.00");
                        }
                        if (Math.Abs(t.Serv.RsumTarif) > 0.001m)
                        {
                            dr["rsum_tarif_all" + numberString] = t.Serv.RsumTarif.ToString("0.00");
                        }

                        if (Math.Abs(t.Serv.Reval +
                                     t.Serv.RealCharge) > 0.001m)
                        {
                            if (t.Serv.NzpServ == 8)
                            {
                                dr["reval" + numberString] = (t.Serv.Reval + t.Serv.RealCharge-RevalOtopl307).ToString("0.00");
                                dr["revalo" + numberString] = RevalOtopl307.ToString("0.00");
                                SummaryServ.Serv.Reval -= RevalOtopl307;

                            }
                            else
                                dr["reval" + numberString] = (t.Serv.Reval +
                                                              t.Serv.RealCharge).ToString("0.00");
                        }

                        


                        dr["sum_lgota" + numberString] = "";

                        if (Math.Abs(t.Serv.SumCharge) > 0.001m)
                        {
                            dr["sum_charge_all" + numberString] = t.Serv.SumCharge.ToString("0.00");
                        }

                        if (Math.Abs(t.Serv.SumCharge -
                                     t.ServOdn.SumCharge) > 0.001m)
                        {
                            dr["sum_charge" + numberString] = (t.Serv.SumCharge -
                                                               t.ServOdn.SumCharge).ToString("0.00");
                        }
                        if (Math.Abs(t.ServOdn.SumCharge) > 0.001m)
                        {
                            dr["sum_charge_odn" + numberString] = t.ServOdn.SumCharge.ToString("0.00");
                        }

                        if (t.Serv.NzpServ > 0)
                            FillServiceVolume(dr, numberString, t.Serv.NzpServ);


                        #region Для единицы измерения Гкал

                        if ((t.Serv.NzpMeasure == 4) & (Math.Abs(t.Serv.RsumTarif) > 0.001m))
                        {

                            if (t.Serv.OldMeasure == 4)
                            {
                                dr["c_calc" + numberString] = (t.Serv.CCalc).ToString("0.00000") +
                                                              GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);

                            }
                            else
                            {
                                dr["c_calc" + numberString] = (t.Serv.CCalc * OtopNorm).ToString("0.00000") +
                                                              GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);

                            }

                            if (Math.Abs(t.ServOdn.CCalc) > 0.000001m)
                            {
                                string sourceOdn = "(1)";

                                if (HasGvsDpu) sourceOdn = "(4)";

                                dr["c_calc_odn" + numberString] =
                                    (t.ServOdn.CCalc).ToString("0.0000#") + sourceOdn;

                            }


                        }

                        #endregion

                        //фиксированные нормативы по Безенчуку
                        switch (t.Serv.NzpServ)
                        {
                            case 8:
                                {
                                    string val_cnt = "";
                                    foreach (var DomPu in ListDomCounters)
                                    {
                                        if (DomPu.NzpServ == 8)
                                            val_cnt = DomPu.Value.ToString("0.0000");
                                    }
                                    dr["rash_norm" + numberString] = "0.02";
                                    dr["rash_pu_odn" + numberString] = (0 == t.Serv.OdnDomVolumePu
                                        ? val_cnt
                                        : t.Serv.OdnDomVolumePu.ToString("0.0000"));
                                    break;
                                }
                        }

                        numberString++;
                    }
                }
            }

            #endregion

            #region Цикл по компонентам

            foreach (BaseServ t in ListKomp)
            {
                if (numberKomponent > MaxKomponentCount) break;
                if (IsShowServInGrid(t))
                {
                    if (t.Serv.NzpServ == 14 && HasOpenVodozabor)
                    {
                        t.Serv.NameServ = "Ком-т на т.носит.";
                    }
                    dr["name_serv_kom" + numberKomponent] = '*' + t.Serv.NameServ.Trim() +
                                                            (t.Serv.NameSupp.Trim().Length > 17
                                                                ? t.Serv.NameSupp.Remove(16)
                                                                : t.Serv.NameSupp.Trim());
                    dr["measure_kom" + numberKomponent] = t.Serv.Measure.Trim();

                    if ((Math.Abs(t.Serv.CCalc) > 0.00001m) &
                        (t.Serv.IsOdn == false))
                    {

                        if ((t.Serv.RsumTarif ==
                             t.ServOdn.RsumTarif) & (t.Serv.RsumTarif > 0.001m))
                        {
                        }
                        else
                        {
                            if (Math.Abs(t.Serv.RsumTarif) > 0.001m)
                                dr["c_calc_kom" + numberKomponent] = t.Serv.CCalc.ToString("0.00000") +
                                                                     GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                            else if (Math.Abs(t.ServOdn.CCalc) > 0.00001m)
                            {
                                dr["c_calc_kom" + numberKomponent] = t.Serv.CCalc.ToString("0.00000") +
                                                                     GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                            }
                        }

                    }

                    if (Math.Abs(t.ServOdn.CCalc) > 0.00001m)
                    {

                        string sourceOdn = "(1)";

                        if ((t.Serv.NzpServ == 6) & (HasHvsDpu)) sourceOdn = "(4)";
                        if ((t.Serv.NzpServ == 9) & (HasGvsDpu)) sourceOdn = "(4)";
                        if ((t.Serv.NzpServ == 14) & (HasGvsDpu)) sourceOdn = "(4)";
                        if ((t.Serv.NzpServ == 25) & (HasElDpu)) sourceOdn = "(4)";

                        dr["c_calc_odn_kom" + numberKomponent] = t.ServOdn.CCalc.ToString("0.00000") +
                                                                 sourceOdn;
                    }


                    if (Math.Abs(t.Serv.Tarif) > 0.001m)
                    {
                        dr["tarif_kom" + numberKomponent] = t.Serv.Tarif.ToString("0.00");
                    }


                    if (Math.Abs(t.Serv.RsumTarif - t.ServOdn.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif_kom" + numberKomponent] =
                            (t.Serv.RsumTarif - t.ServOdn.RsumTarif).ToString("0.00");
                    }

                    if (Math.Abs(t.ServOdn.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif_odn_kom" + numberKomponent] = t.ServOdn.RsumTarif.ToString("0.00");
                    }

                    dr["sum_lgota_kom" + numberKomponent] = "";

                    if (Math.Abs(t.Serv.Reval +
                                 t.Serv.RealCharge) > 0.001m)
                    {
                        dr["reval_kom" + numberKomponent] = (t.Serv.Reval +
                                                             t.Serv.RealCharge).ToString("0.00");
                    }

                    if (Math.Abs(t.Serv.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif_all_kom" + numberKomponent] = t.Serv.RsumTarif.ToString("0.00");
                    }


                    {
                        dr["sum_charge_all_kom" + numberKomponent] = t.Serv.SumCharge.ToString("0.00");
                    }

                    if (Math.Abs(t.Serv.SumCharge -
                                 t.ServOdn.SumCharge) > 0.001m)
                    {
                        dr["sum_charge_kom" + numberKomponent] = (t.Serv.SumCharge -
                                                                  t.ServOdn.SumCharge).ToString("0.00");
                    }
                    if (Math.Abs(t.ServOdn.SumCharge) > 0.001m)
                    {
                        dr["sum_charge_odn_kom" + numberKomponent] = t.ServOdn.SumCharge.ToString("0.00");
                    }

                    FillServiceVolume(dr, numberKomponent, t.Serv.NzpServ);


                    #region Для единицы измерения Гкал

                    if ((t.Serv.NzpMeasure == 4) & (Math.Abs(t.Serv.RsumTarif) > 0.001m))
                    {

                        if (t.Serv.OldMeasure == 4)
                        {
                            if (t.Serv.NzpServ == 9)
                            {
                                if (Math.Abs(t.Serv.CCalc) > 0.00001m)
                                {
                                    dr["c_calc_kom" + numberKomponent] = (t.Serv.CCalc).ToString("0.00000") +
                                                                         GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                }
                            }
                            else
                            {
                                dr["c_calc_kom" + numberKomponent] = (t.Serv.CCalc).ToString("0.00000") +
                                                                     GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                            }
                        }
                        else
                        {
                            if (t.Serv.NzpServ == 9)
                            {
                                if (Math.Abs(t.Serv.CCalc) > 0.00001m)
                                {
                                    dr["c_calc_kom" + numberKomponent] =
                                        (t.Serv.CCalc * GvsNormGkal).ToString("0.00000") +
                                        GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                }
                            }
                            else
                            {
                                dr["c_calc_kom" + numberKomponent] = (t.Serv.CCalc * OtopNorm).ToString("0.00000") +
                                                                     GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);

                            }

                        }

                        if (Math.Abs(t.ServOdn.CCalc) > 0.000001m)
                        {
                            string sourceOdn = "(1)";

                            if (HasGvsDpu) sourceOdn = "(4)";

                            if (t.Serv.NzpServ == 9)
                            {
                                dr["c_calc_odn_kom" + numberKomponent] =
                                    (t.ServOdn.CCalc).ToString("0.0000#") + sourceOdn;
                            }
                            else
                            {
                                dr["c_calc_odn_kom" + numberKomponent] =
                                    (t.ServOdn.CCalc).ToString("0.0000#") + sourceOdn;

                            }
                        }


                    }

                    #endregion

                    //фиксированные нормативы по Безенчуку
                    switch (t.Serv.NzpServ)
                    {
                        case 8:
                            {
                                decimal val_cnt = 0;
                                foreach (var DomPu in ListDomCounters)
                                {
                                    if (DomPu.NzpServ == 8)
                                        val_cnt = DomPu.Value;
                                }
                                dr["rash_norm" + numberString] = "0.02";
                                dr["rash_pu_odn" + numberString] = (0 == t.Serv.OdnDomVolumePu
                                    ? val_cnt.ToString()
                                    : t.Serv.OdnDomVolumePu.ToString());
                                break;
                            }
                    }

                    numberKomponent++;

                }
            }

            #endregion


            if (!hasKapr)
            {
                dr["measure0"] = "кв.м.";
                dr["c_calc0"] = FullSquare.ToString("0.00");
                dr["tarif0"] = Convert.ToDecimal(TarifKapr).ToString("0.00");
                dr["rsum_tarif_all0"] = (FullSquare * Convert.ToDecimal(TarifKapr)).ToString("0.00");
                dr["reval0"] = "0.00";
                dr["sum_charge_all0"] = "0.00";
                dr["sum_ticketKapr"] = "0.00";

            }
            return true;
        }



        /// <Summary>
        /// Заполнение причин перерасчета
        /// </Summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillRevalReason(DataRow dr)
        {
            if (dr == null) return false;

            //int kanNumber = -1;
            //string hvsReason = "";
            //string gvsReason = "";
            for (int i = 0; i < 6; i++)
            {
                if (ListReval.Count > i)
                {
                    dr["serv_pere" + (i + 1)] = ListReval[i].ServiceName;

                    //if (ListReval[i].NzpServ == 7) kanNumber = i + 1;
                    if (ListReval[i].Reason == null)
                    {
                        if (Math.Abs(ListReval[i].CReval) < 0.001m)
                        {

                            if (((ListReval[i].NzpServ == 6) ||
                                (ListReval[i].NzpServ == 7) ||
                                (ListReval[i].NzpServ == 9) ||
                                (ListReval[i].NzpServ == 14) ||
                                (ListReval[i].NzpServ == 10) ||
                                (ListReval[i].NzpServ == 25) ||
                                (ListReval[i].NzpServ == 210) ||
                                (ListReval[i].NzpServ == 510) ||
                                (ListReval[i].NzpServ == 513) ||
                                (ListReval[i].NzpServ == 514) ||
                                (ListReval[i].NzpServ == 515) ||
                                (ListReval[i].NzpServ == 516) ||
                                (ListReval[i].NzpServ == 517) ||
                                (ListReval[i].NzpServ == 518)) & (GilPeriods != ""))
                            {

                                dr["osn_pere" + (i + 1)] = "Врем. выбытие жильца " + GilPeriods;
                            }

                        }
                        else
                        {
                            dr["osn_pere" + (i + 1)] = "Изм. расхода по услуге";
                        }
                    }
                    else
                    {

                        if (((ListReval[i].NzpServ == 6) ||
                              (ListReval[i].NzpServ == 7) ||
                              (ListReval[i].NzpServ == 9) ||
                              (ListReval[i].NzpServ == 14) ||
                              (ListReval[i].NzpServ == 10) ||
                              (ListReval[i].NzpServ == 25) ||
                              (ListReval[i].NzpServ == 210) ||
                              (ListReval[i].NzpServ == 510) ||
                              (ListReval[i].NzpServ == 513) ||
                              (ListReval[i].NzpServ == 514) ||
                              (ListReval[i].NzpServ == 515) ||
                              (ListReval[i].NzpServ == 516) ||
                              (ListReval[i].NzpServ == 517) ||
                              (ListReval[i].NzpServ == 518)) & (GilPeriods != ""))
                        {

                            //dr["osn_pere" + (i + 1).ToString()] = "Врем. выбытие жильца, " + ListReval[i].reason ;
                            dr["osn_pere" + (i + 1)] = "Врем. выбытие жильца  " + GilPeriods +
                                ", " + ListReval[i].Reason + " " + ListReval[i].ReasonPeriod;
                        }
                        else
                        {

                            dr["osn_pere" + (i + 1)] = ListReval[i].Reason + " " + ListReval[i].ReasonPeriod;
                        }
                    }
                    dr["sum_pere" + (i + 1)] = ListReval[i].SumReval;

                    //if (ListReval[i].NzpServ == 6) hvsReason = dr["osn_pere" + (i + 1)].ToString();
                    //if (ListReval[i].NzpServ == 9) gvsReason = dr["osn_pere" + (i + 1)].ToString();

                    if (ListReval[i].NzpServ == 8)
                    {
                        if (ListReval[i].SumReval == RevalOtopl307)
                            dr["osn_pere" + (i + 1)] =
                                "Корректировка размера платы за отопление в отопительный период 2013 г.-2014г., Пост.Правительства РФ от 23.05.06г. №307";
                        else
                            dr["osn_pere" + (i + 1)] = ListReval[i].Reason +
                                                       " Корректировка размера платы за отопление в отопительный период 2013 г.-2014г., Пост.Правительства РФ от 23.05.06г. №307";


                    }
                }
                else
                {
                    dr["serv_pere" + (i + 1)] = "";
                    dr["osn_pere" + (i + 1)] = "";
                    dr["sum_pere" + (i + 1)] = "";

                }
            }
            //if (kanNumber > -1)
            //{
            //    if (hvsReason != "")
            //        dr["osn_pere" + kanNumber] = hvsReason;
            //    else
            //        dr["osn_pere" + kanNumber] = gvsReason;
            //}

            return true;
        }

        /// <Summary>
        /// Создание перечня всех полей счета
        /// </Summary>
        /// <returns></returns>
        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("str_rekv1", typeof(string));
            table.Columns.Add("str_rekv2", typeof(string));
            table.Columns.Add("str_rekv3", typeof(string));
            table.Columns.Add("str_rekv4", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("pkodkapr", typeof(string));
            table.Columns.Add("Platelchik", typeof(string));
            table.Columns.Add("rajon", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("type_pl", typeof(string));
            table.Columns.Add("priv", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("ls", typeof(string));
            table.Columns.Add("ngeu", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("geu_remark", typeof(string));
            table.Columns.Add("area_remark", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("vars2", typeof(string));
            table.Columns.Add("varskapr", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("rsum_tarif_all", typeof(string));
            table.Columns.Add("rsum_tarif_odn", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_ticketKapr", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_charge_odn", typeof(string));
            table.Columns.Add("sum_insaldo", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_rub", typeof(string));
            table.Columns.Add("sum_kop", typeof(string));
            table.Columns.Add("ud", typeof(string));
            table.Columns.Add("Data_dolg", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("monthscounters", typeof(string));
            table.Columns.Add("month_", typeof(string));
            table.Columns.Add("Year_", typeof(string));
            table.Columns.Add("sum_tarif", typeof(string));
            table.Columns.Add("reval", typeof(string));
            table.Columns.Add("revalo", typeof(string));
            table.Columns.Add("real_charge", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));
            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("pl_mop", typeof(string));
            table.Columns.Add("dom_gil", typeof(string));
            table.Columns.Add("ShowKaprBlock", typeof(int));

            for (int i = 0; i < 20; i++)
            {
                table.Columns.Add("name_serv" + i, typeof(string));
                table.Columns.Add("measure" + i, typeof(string));
                table.Columns.Add("c_calc" + i, typeof(string));
                table.Columns.Add("c_calc_odn" + i, typeof(string));
                table.Columns.Add("tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif_odn" + i, typeof(string));
                table.Columns.Add("rsum_tarif_all" + i, typeof(string));
                table.Columns.Add("reval" + i, typeof(string));
                table.Columns.Add("revalo" + i, typeof(string));
                table.Columns.Add("revalnull" + i, typeof(string));
                table.Columns.Add("sum_lgota" + i, typeof(string));
                table.Columns.Add("sum_charge_all" + i, typeof(string));
                table.Columns.Add("sum_charge" + i, typeof(string));
                table.Columns.Add("sum_charge_odn" + i, typeof(string));
                table.Columns.Add("sum_nedop" + i, typeof(string));
                table.Columns.Add("sum_sn" + i, typeof(string));
                table.Columns.Add("sum_outsaldo" + i, typeof(string));
                table.Columns.Add("real_charge" + i, typeof(string));
                table.Columns.Add("rash_name" + i, typeof(string));
                table.Columns.Add("rash_norm" + i, typeof(string));
                table.Columns.Add("rash_norm_odn" + i, typeof(string));
                table.Columns.Add("rash_pu" + i, typeof(string));
                table.Columns.Add("rash_pu_odn" + i, typeof(string));
                table.Columns.Add("rash_dpu_pu" + i, typeof(string));
                table.Columns.Add("rash_dpu_odn" + i, typeof(string));

            }


            for (int i = 1; i <= MaxKomponentCount; i++)
            {
                table.Columns.Add("name_serv_kom" + i, typeof(string));
                table.Columns.Add("measure_kom" + i, typeof(string));
                table.Columns.Add("c_calc_kom" + i, typeof(string));
                table.Columns.Add("c_calc_odn_kom" + i, typeof(string));
                table.Columns.Add("tarif_kom" + i, typeof(string));
                table.Columns.Add("rsum_tarif_kom" + i, typeof(string));
                table.Columns.Add("rsum_tarif_odn_kom" + i, typeof(string));
                table.Columns.Add("rsum_tarif_all_kom" + i, typeof(string));
                table.Columns.Add("reval_kom" + i, typeof(string));
                table.Columns.Add("revalnull_kom" + i, typeof(string));
                table.Columns.Add("sum_lgota_kom" + i, typeof(string));
                table.Columns.Add("sum_charge_all_kom" + i, typeof(string));
                table.Columns.Add("sum_charge_kom" + i, typeof(string));
                table.Columns.Add("sum_charge_odn_kom" + i, typeof(string));
                table.Columns.Add("sum_nedop_kom" + i, typeof(string));
                table.Columns.Add("sum_sn_kom" + i, typeof(string));
                table.Columns.Add("sum_outsaldo_kom" + i, typeof(string));
                table.Columns.Add("real_charge_kom" + i, typeof(string));
                table.Columns.Add("rash_name_kom" + i, typeof(string));
                table.Columns.Add("rash_norm_kom" + i, typeof(string));
                table.Columns.Add("rash_norm_odn_kom" + i, typeof(string));
                table.Columns.Add("rash_pu_kom" + i, typeof(string));
                table.Columns.Add("rash_pu_odn_kom" + i, typeof(string));
                table.Columns.Add("rash_dpu_pu_kom" + i, typeof(string));
                table.Columns.Add("rash_dpu_odn_kom" + i, typeof(string));

            }

            for (int i = 1; i < 7; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
            }

            for (int i = 1; i < MaxCountersCount; i++)
            {
                table.Columns.Add("domserv" + i, typeof(string));
                table.Columns.Add("domnumcnt" + i, typeof(string));
                table.Columns.Add("domdatuchet1_" + i, typeof(string));
                table.Columns.Add("domdatuchet2_" + i, typeof(string));
                table.Columns.Add("domvalcnt1_" + i, typeof(string));
                table.Columns.Add("domvalcnt2_" + i, typeof(string));
                table.Columns.Add("lsserv" + i, typeof(string));

                table.Columns.Add("lsdatuchet1_" + i, typeof(string));
                table.Columns.Add("lsdatuchet2_" + i, typeof(string));
                table.Columns.Add("lsvalcnt1_" + i, typeof(string));

                table.Columns.Add("datuchet2_" + i, typeof(string));
                table.Columns.Add("domdatProv_" + i, typeof(string));
                table.Columns.Add("lsdatProv_" + i, typeof(string));

            }

            for (int i = 1; i <= 6; i++)
            {
                table.Columns.Add("lsnumcnt" + i, typeof(string));
                table.Columns.Add("lsvalcnt2_" + i, typeof(string));
            }
            return table;

        }

        /// <Summary>
        /// Заполнение итоговой строки по счету
        /// </Summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillSummuryBill(DataRow dr)
        {
            if (dr == null) return false;
            dr["rsum_tarif_all"] = SummaryServ.Serv.RsumTarif.ToString("0.00");
            dr["rsum_tarif_odn"] = SummaryServ.ServOdn.RsumTarif.ToString("0.00");
            dr["rsum_tarif"] = (SummaryServ.Serv.RsumTarif - SummaryServ.ServOdn.RsumTarif).ToString("0.00");
            dr["sum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["reval"] = SummaryServ.Serv.Reval.ToString("0.00");
            dr["reval_charge"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["real_charge"] = SummaryServ.Serv.RealCharge.ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_charge"] = (SummaryServ.Serv.SumCharge - SummaryServ.ServOdn.SumCharge).ToString("0.00");
            dr["sum_charge_odn"] = SummaryServ.ServOdn.SumCharge.ToString("0.00");
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            dr["revalo"] = RevalOtopl307;

            return true;
        }


        public BezenchukFakturaKaprOtopl()
        {

            Rekvizit = new _Rekvizit();

            SummaryServ = new BaseServ(false);
            ListServ = new List<BaseServ>();
            ListReval = new List<ServReval>();
            ListVolume = new List<ServVolume>();
            ListCounters = new List<Counters>();
            CUnionServ = new CUnionServ();

            FakturaBlocks.HasAdrBlock = true;
            FakturaBlocks.HasRekvizitBlock = true;
            FakturaBlocks.HasKvarPrmBlock = true;
            FakturaBlocks.HasSummuryBillBlock = true;
            FakturaBlocks.HasMainChargeGridBlock = true;
            FakturaBlocks.HasRevalReasonBlock = true;
            FakturaBlocks.HasServiceVolumeBlock = true;
            FakturaBlocks.HasCountersBlock = true;
            FakturaBlocks.HasCountersDoubleBlock = false;
            FakturaBlocks.HasCountersDoubleDomBlock = false;
            FakturaBlocks.HasRTCountersDoubleDomBlock = true;
            FakturaBlocks.HasOdnBlock = true;
            FakturaBlocks.HasRdnBlock = true;
            FakturaBlocks.HasRemarkblock = true;
            FakturaBlocks.HasGilPeriodsBlock = true;
            FakturaBlocks.HasCountersSpisBlock = true;
            FakturaBlocks.HasNormblock = true;
            FakturaBlocks.HasCalcGil = true;
            FakturaBlocks.HasSupplierblock = true;
            FakturaBlocks.HasSupplierPkod = true;
            FakturaBlocks.HasBezenchuk = true;
            FakturaBlocks.HasNewCountersBlock = true;
            Clear();

        }

    }


}

