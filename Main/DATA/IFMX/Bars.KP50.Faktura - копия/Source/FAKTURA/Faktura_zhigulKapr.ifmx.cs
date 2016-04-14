using System.Globalization;
using FastReport;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;

    public class ZhigulFakturaKapr : BaseFactura
    {
        private const int MaxCountersCount = 7;
        private decimal _sumTicketKapr;
        private const int MaxStringCount = 9;

        /// <Summary>
        /// Заполнение адреса
        /// </Summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillCalcGil(DataRow dr)
        {
            if (dr == null) return false;
            return true;
        }

        protected override bool FillKvarPrm(DataRow dr)
        {
            if (dr == null) return false;
            if (Ownflat)
            {
                dr["priv"] = "Приватизирована";
            }
            else
            {
                dr["priv"] = "не приватизирована";
            }
            dr["kolgil2"] = CountRegisterGil;
            dr["kolgil"] = CountGil + CountArriveGil - CountDepartureGil;
            dr["ls"] = Pkod.Substring(5, 5);
            if (Pkod.Substring(10, 1) == "0")
                dr["ls"] = Pkod.Substring(5, 5);
            else
                dr["ls"] = Pkod.Substring(5, 5) + " " + Pkod.Substring(10, 1);

            //if (nzpGeu > 100)
            //{

            //    if (nzpGeu > 1000)
            //        dr["ngeu"] = nzpGeu.ToString().Substring(2, 2);
            //    else
            //        dr["ngeu"] = nzpGeu.ToString().Substring(1, 2);
            //}
            //else
            dr["ngeu"] = Pkod.Substring(3, 2);
            dr["ud"] = Ud;
            dr["pkod"] = "987654";//Pkod.Substring(0, 13);
            dr["pkodkapr"] = PkodKapr.Substring(0, 13);
            dr["pl_dom"] = DomSquare.ToString("0.00");
            dr["pl_mop"] = MopSquare.ToString("0.00");
            dr["dom_gil"] = CountDomGil.ToString(CultureInfo.InvariantCulture);
            dr["varskapr"] = GetBarCodeKapr();
            return true;
        }



        public override string GetBarCode()
        {

            string vars = "00" + Pkod + Month.ToString("D2") +
             (Year - 2000).ToString("D2") +
             (Math.Max(0, SumTicket) * 100).ToString("000000000");
            Shtrih = vars + STCLINE.KP50.Global.Utils.BarcodeCrcSamara(vars);
            GeuKodErc = "630100000015";
            return Shtrih;
        }

        public string GetBarCodeKapr()
        {
            string vars2 = "00" + PkodKapr.Substring(0, 13) + Month.ToString("D2") +
             (Year - 2000).ToString("D2") +
             (Math.Max(0, _sumTicketKapr) * 100).ToString("000000000");
            string ShtrihKapr = vars2 + STCLINE.KP50.Global.Utils.BarcodeCrcSamara(vars2);
            return ShtrihKapr;
        }

        public override bool IsShowServInGrid(BaseServ aServ)
        {


            if (
                (Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (Math.Abs(aServ.Serv.CCalc) < 0.001m) &
                (Math.Abs(aServ.Serv.SumCharge) < 0.001m)
            )
            {
                return false;
            }
            return true;
        }

        public override void FinalPass(Faktura finder)
        {
            base.FinalPass(finder);
            //if ((finder.withDolg) || ((SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney) < 0))
            //{
            SumTicket = SummaryServ.Serv.SumCharge + SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney;
            if (SumTicket < 0) SumTicket = 0;
            //}
            //else
            //{
            //    SumTicket = SummaryServ.Serv.SumCharge;
            //    if (SumTicket < 0) SumTicket = 0;
            //}

            _sumTicketKapr = 0;
            foreach (var serv in ListServ)
            {
                if (serv.Serv.NzpServ == 206)
                {
                    _sumTicketKapr += serv.Serv.SumCharge + serv.Serv.SumInsaldo - serv.Serv.SumMoney;
                }
            }

            SumTicket -= _sumTicketKapr;

            if (finder.newSumOpl > 0.001m)
                SumTicket = finder.newSumOpl;

        }

        /// <Summary>
        /// Заполнение счетчиков
        /// </Summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override bool FillCounters(DataRow dr)
        {

            int monthscounters = new DateTime(Year, Month, 01).AddMonths(1).Month;
            switch (monthscounters)
            {
                case 1:
                    dr["monthscounters"] = "Январь " + Year + 1;
                    break;
                case 2:
                    dr["monthscounters"] = "Февраль " + Year;
                    break;
                case 3:
                    dr["monthscounters"] = "Март " + Year;
                    break;
                case 4:
                    dr["monthscounters"] = "Апрель " + Year;
                    break;
                case 5:
                    dr["monthscounters"] = "Май " + Year;
                    break;
                case 6:
                    dr["monthscounters"] = "Июнь " + Year;
                    break;
                case 7:
                    dr["monthscounters"] = "Июль " + Year;
                    break;
                case 8:
                    dr["monthscounters"] = "Август " + Year;
                    break;
                case 9:
                    dr["monthscounters"] = "Сентябрь " + Year;
                    break;
                case 10:
                    dr["monthscounters"] = "Октябрь " + Year;
                    break;
                case 11:
                    dr["monthscounters"] = "Ноябрь " + Year;
                    break;
                case 12:
                    dr["monthscounters"] = "Декабрь " + Year;
                    break;

            }


            for (int i = 0; i < Math.Min(MaxCountersCount, ListCounters.Count); i++)
            {
                //if (ListCounters[i].NzpServ == 9 && j <= 3)//Г.В.
                //{
                //    j++;
                //    dr["lsserv" + j] = "Г/В";
                //    dr["lsnumcnt" + j] = ListCounters[i].numCounters;
                //    dr["lsvalcnt2_" + j] = ListCounters[i].value.ToString("0.00");
                //}
                //if (ListCounters[i].NzpServ == 6 && k <= 6) //Х.В.
                //{
                //    k++;
                //    dr["lsserv" + j] = "Х/В";
                //    dr["lsnumcnt" + k] = ListCounters[i].numCounters;
                //    dr["lsvalcnt2_" + k] = ListCounters[i].value.ToString("0.00");
                //}


                if (ListCounters[i].NzpServ == 9)
                {
                    dr["lsserv" + (i + 1)] = "Г/В";
                }
                else if (ListCounters[i].NzpServ == 6)
                {
                    dr["lsserv" + (i + 1)] = "Х/В";
                }
                else
                {
                    dr["lsserv" + (i + 1)] = ListCounters[i].ServiceName;
                }


                dr["lsnumcnt" + (i + 1)] = ListCounters[i].NumCounters;
                dr["lsvalcnt2_" + (i + 1)] = ListCounters[i].Value.ToString("0.00");
            }




            dr["vars2"] = Pkod;
            return true;
        }

        protected override bool FillAdr(DataRow dr)
        {
            if (dr == null) return false;
            dr["Platelchik"] = PayerFio;
            dr["ulica"] = Ulica;
            dr["numdom"] = NumberDom;
            dr["kvnum"] = NumberFlat;
            if (IsolateFlat)
            {
                dr["kv_pl"] = FullSquare.ToString("0.00");
                dr["type_pl"] = "общая";
            }
            else
            {
                dr["kv_pl"] = LiveSquare.ToString("0.00");
                dr["type_pl"] = "жилая";
            }
            return true;
        }



        /// <Summary>
        /// Заполнение расходов по услугам
        /// </Summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillServiceVolume(DataRow dr)
        {
            return true;
        }

        protected override void FillGoodServVolume(DataRow dr, decimal aValue, string colName)
        {
            FillGoodServVolume(dr, aValue, colName, 0);
        }

        protected void FillGoodServVolume(DataRow dr, decimal aValue, string colName, int nzpServ)
        {

            if (Math.Abs(aValue) < 0.001m)
            {
                dr[colName] = "";
            }
            else
            {
                if (nzpServ == 8 || nzpServ == 9)
                {
                    if (colName.IndexOf("rash_pu", StringComparison.Ordinal) > -1)
                        dr[colName] = aValue.ToString("0.00");
                    else if (colName.IndexOf("rash_norm", StringComparison.Ordinal) > -1)
                        dr[colName] = aValue.ToString("0.0000#");
                    else
                        dr[colName] = aValue.ToString("0.0000#");
                }
                else
                {

                    if (colName.IndexOf("rash_pu", StringComparison.Ordinal) > -1)
                        dr[colName] = aValue.ToString("0.00");
                    else if (colName.IndexOf("rash_norm", StringComparison.Ordinal) > -1)
                        dr[colName] = aValue.ToString("0.00###");
                    else
                        dr[colName] = aValue.ToString("0.0000#");
                }
            }
        }

        /// <Summary>
        /// Заполнение расходов по услугам
        /// </Summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <param name="index"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        protected bool FillServiceVolume(DataRow dr, int index, int nzpServ)
        {
            int anzpServ = nzpServ;

            if (nzpServ == 9)
            {
                if ((anzpServ == 9) & (HasGvsDpu))
                {
                    dr["rash_norm_odn" + index] = GvsNormGkal.ToString("0.00000");
                }


                //FillGoodServVolume(dr, hvsGvsNorma, "rash_norm" + index.ToString());

                return true;
            }
            int numRec14 = -1;
            if (nzpServ == 14)
            {
                anzpServ = 9;
                for (int i = 0; i < ListVolume.Count; i++)
                {
                    if (ListVolume[i].NzpServ == nzpServ)
                    {
                        numRec14 = i;
                    }
                }
            }



            int numRec = -1;
            for (int i = 0; i < ListVolume.Count; i++)
            {
                if (ListVolume[i].NzpServ == anzpServ)
                {
                    numRec = i;
                }
            }
            if (numRec == -1) return true;

            // FillGoodServVolume(dr, norma, "rash_norm" + index.ToString());  // Изменил Андрей Кайнов 19.12.2012


            if ((anzpServ == 25) & (KfodnEl != ""))
            {
                dr["rash_norm_odn" + index] = KfodnEl;
            }

            if ((anzpServ == 6) & (Kfodnhvs != "") & (HasHvsDpu))
            {
                dr["rash_norm_odn" + index] = Kfodnhvs;
            }

            if ((anzpServ == 9) & (Kfodngvs != "") & (HasGvsDpu))
            {
                dr["rash_norm_odn" + index] = Kfodngvs;
            }

            //Если домового прибора учета нет, то не печатаем
            if (((anzpServ == 25) & (HasElDpu)) ||
                ((anzpServ == 6) & (HasHvsDpu)) ||
                ((anzpServ == 9) & (HasGvsDpu)) ||
                ((anzpServ == 10) & (HasGazDpu)) ||
                ((anzpServ == 8) & (HasOtopDpu)))
            {
                FillGoodServVolume(dr, ListVolume[numRec].DomVolume, "rash_dpu_pu" + index);
                FillGoodServVolume(dr, ListVolume[numRec].OdnDomVolume, "rash_dpu_odn" + index);
            }


            if ((anzpServ == 25) & (dr["c_calc" + index].ToString().Trim() == ""))
            {
            }
            else
            {
                decimal norma14 = numRec14 > -1 ? ListVolume[numRec14].NormaVolume : Math.Round(GvsNorm, 3);
                FillGoodServVolume(dr, nzpServ == 14 ? norma14 : ListVolume[numRec].NormaVolume, "rash_norm" + index, anzpServ);
            }
            //Ищем подходящие приборы учета
            decimal sumCountersValue = 0;
            for (int k = 0; k < ListCounters.Count; k++)
            {
                if ((ListCounters[k].NzpServ == anzpServ))
                {
                    sumCountersValue += ListCounters[k].Value;
                }
            }
            FillGoodServVolume(dr, sumCountersValue, "rash_pu" + index);

            return true;
        }


        /// <Summary>
        /// Процедура определяет способ получения объема для расчета
        /// </Summary>
        /// <param name="nzpServ"></param>
        /// <param name="isPu"></param>
        /// <returns></returns>
        protected string GetVolumeSource(int nzpServ, int isPu)
        {
            int aserv = nzpServ;
            if (aserv == 14) aserv = 9;
            if (nzpServ == 7) aserv = 6;

            if (nzpServ == 7)
            {
                foreach (BaseServ t in ListServ)
                {
                    if (t.Serv.NzpServ == 6)
                        isPu = t.Serv.IsDevice;
                }
            }

            if ((aserv == 6) || (aserv == 9) || (aserv == 25))
            {

                //    for (int i = 0; i < ListCounters.Count; i++)
                //    {
                //        if ((aserv == ListCounters[i].NzpServ)&(ListCounters[i].datUchet>
                //             System.Convert.ToDateTime("01." + month.ToString() + "." + Year.ToString())))
                //        {
                //            return "(2)";
                //        }
                //    }



                //if (isPu == 1) return "(3)";
                //else return "(1)";

                switch (isPu)
                {
                    case 0: return "(1)";
                    case 1: return "(2)";
                    case 9: return "(3)";
                }
                return "(1)";
            }

            return "";
        }


        /// <Summary>
        /// Заполнение данных таблицы начислений
        /// </Summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillMainChargeGrid(DataRow dr)
        {
            _sumTicketKapr = 0;
            if (dr == null) return false;
            if (GvsNormGkal == 0) GvsNormGkal = 0.0611m;
            SetServRashod();
            ListServ.Sort();
            int numberString = 1;
            bool hasKapr = false;
            foreach (BaseServ t in ListServ)
            {
                if (numberString >= MaxStringCount) break;
                if (IsShowServInGrid(t))
                {
                    if (t.Serv.NzpServ == 206)
                    {
                        dr["measure0"] = t.Serv.Measure.Trim();
                        dr["c_calc0"] = t.Serv.CCalc.ToString("0.00##") +
                                                                  GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                        dr["tarif0"] = t.Serv.Tarif.ToString("0.00");
                        dr["rsum_tarif_all0"] = t.Serv.RsumTarif.ToString("0.00");
                        dr["reval0"] = (t.Serv.Reval + t.Serv.RealCharge).ToString("0.00");
                        //dr["sum_charge_all0"] = SumTicketKapr.ToString("0.00");//t.Serv.SumCharge.ToString("0.00");
                        dr["sum_charge_all0"] = t.Serv.SumCharge.ToString("0.00");
                        _sumTicketKapr = t.Serv.SumCharge + t.Serv.SumInsaldo - t.Serv.SumMoney;

                        dr["sum_ticketKapr"] = _sumTicketKapr.ToString("0.00");
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
                        dr["name_serv" + numberString] = t.Serv.NameServ.Trim() +
                                                                    t.Serv.NameSupp.Trim();
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
                                else
                                    if (Math.Abs(t.ServOdn.CCalc) > 0.00001m)
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

                        if (Math.Abs(t.Serv.RsumTarif -
                                     t.ServOdn.RsumTarif) > 0.001m)
                        {
                            dr["rsum_tarif" + numberString] = (t.Serv.RsumTarif -
                                                               t.ServOdn.RsumTarif).ToString("0.00");
                        }

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


                        FillServiceVolume(dr, numberString, t.Serv.NzpServ);


                        #region Для единицы измерения Гкал
                        if ((t.Serv.NzpMeasure == 4) & (Math.Abs(t.Serv.RsumTarif) > 0.001m))
                        {

                            /*  dr["c_calc" + numberString.ToString()] = ((listServ[countServ].Serv.RsumTarif -
                                listServ[countServ].ServOdn.RsumTarif) /
                                listServ[countServ].Serv.Tarif).ToString(precision)+
                                 GetVolumeSource(listServ[countServ].Serv.NzpServ, listServ[countServ].Serv.IsDevice);
                          */

                            if (t.Serv.OldMeasure == 4)
                            {
                                if (t.Serv.NzpServ == 9)
                                {
                                    if (Math.Abs(t.Serv.CCalc) > 0.00001m)
                                    {
                                        dr["c_calc" + numberString] = (t.Serv.CCalc).ToString("0.00000") +
                                                                                 GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                    }
                                }
                                else
                                {
                                    dr["c_calc" + numberString] = (t.Serv.CCalc).ToString("0.00000") +
                                                                             GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                }
                            }
                            else
                            {
                                if (t.Serv.NzpServ == 9)
                                {
                                    if (Math.Abs(t.Serv.CCalc) > 0.00001m)
                                    {
                                        dr["c_calc" + numberString] = (t.Serv.CCalc * GvsNormGkal).ToString("0.00000") +
                                                                                 GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                    }
                                }
                                else
                                {
                                    dr["c_calc" + numberString] = (t.Serv.CCalc * OtopNorm).ToString("0.00000") +
                                                                             GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);

                                }

                            }

                            if (Math.Abs(t.ServOdn.CCalc) > 0.000001m)
                            {
                                string sourceOdn = "(1)";

                                if (HasGvsDpu) sourceOdn = "(4)";
                                //  dr["c_calc_odn" + numberString.ToString()] = (listServ[countServ].ServOdn.RsumTarif /
                                //      listServ[countServ].Serv.Tarif).ToString("0.0000") + sourceOdn;

                                /*  if (listServ[countServ].Serv.oldMeasure == 4)
                                {
                                    dr["c_calc_odn" + numberString.ToString()] =
                                         listServ[countServ].ServOdn.CCalc.ToString("0.0000") + sourceOdn;
                                }
                                else
                                {
                                    dr["c_calc_odn" + numberString.ToString()] =
                                         (listServ[countServ].ServOdn.CCalc * 0.0611m).ToString("0.0000") + sourceOdn;
                                }
                               */
                                if (t.Serv.NzpServ == 9)
                                {
                                    dr["c_calc_odn" + numberString] =
                                        (t.ServOdn.CCalc).ToString("0.0000#") + sourceOdn;
                                }
                                else
                                {
                                    dr["c_calc_odn" + numberString] =
                                        (t.ServOdn.CCalc).ToString("0.0000#") + sourceOdn;

                                }
                            }

                            FillGoodServVolume(dr, t.Serv.NzpServ == 9
                                ? GvsNormGkal
                                : OtopNorm, "rash_norm" + numberString, t.Serv.NzpServ);
                        }
                        #endregion



                        numberString++;
                    }
                }
                else
                {
                    if (t.Serv.NzpServ == 206)
                    {
                        SummaryServ.Serv.SumInsaldo -= t.Serv.SumInsaldo;
                        SummaryServ.ServOdn.SumInsaldo -= t.ServOdn.SumInsaldo;
                    } 
                }
            }

            //if (!hasKapr && numberString < 19)
            //{
            //    dr["name_serv" + numberString] = "Капитальный ремонт";
            //    dr["measure" + numberString] = "кв.м.";
            //    dr["c_calc" + numberString] = fullSquare.ToString("0.00#");
            //}

            //if (!hasKapr)
            //{
            //    dr["measure0"] = "кв.м.";
            //    dr["c_calc0"] = FullSquare.ToString("0.00");
            //    dr["tarif0"] = Convert.ToDecimal(TarifKapr).ToString("0.00");
            //    dr["rsum_tarif_all0"] = (FullSquare * Convert.ToDecimal(TarifKapr)).ToString("0.00");
            //    dr["reval0"] = "0.00";
            //    dr["sum_charge_all0"] = "0.00";
            //    dr["sum_ticketKapr"] = "0.00";

            //}
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
            table.Columns.Add("real_charge", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));
            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("pl_mop", typeof(string));
            table.Columns.Add("dom_gil", typeof(string));

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


            return true;
        }


        public ZhigulFakturaKapr()
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
            FakturaBlocks.HasOdnBlock = true;
            FakturaBlocks.HasRdnBlock = true;
            FakturaBlocks.HasRemarkblock = true;
            FakturaBlocks.HasGilPeriodsBlock = true;
            FakturaBlocks.HasCountersSpisBlock = true;
            FakturaBlocks.HasNormblock = true;
            FakturaBlocks.HasCalcGil = true;
            FakturaBlocks.HasSupplierblock = true;
            FakturaBlocks.HasSupplierPkod = true;
            FakturaBlocks.HasNewCountersBlock = true;
            Clear();

        }

    }


}

