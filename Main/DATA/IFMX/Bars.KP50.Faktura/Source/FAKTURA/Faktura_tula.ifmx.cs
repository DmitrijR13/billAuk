using System.Globalization;
using Bars.KP50.Faktura.Source.Base;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;

    public sealed class TulaFaktura : BaseFactura
    {
        public string MessageForLs { get; set; }
        public string ContractContentLs { get; set; }
        public string ContractFooterLs { get; set; }


        /// <summary>
        /// Заполнение квартирных парметров
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
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
            dr["kolgil"] = CountGil;
            dr["ls"] = Pkod.Substring(5, 5);
            dr["num_ls"] = NumLs;
            if (NzpGeu > 100)
                dr["ngeu"] = NzpGeu.ToString(CultureInfo.InvariantCulture).Substring(1, 2);
            else
                dr["ngeu"] = Pkod.Substring(3, 2);
            dr["ud"] = Ud;
            dr["pkod"] = Pkod;
            dr["pl_dom"] = DomSquare.ToString("0.00");
            dr["pl_mop"] = MopSquare.ToString("0.00");
            dr["dom_gil"] = CountDomGil.ToString(CultureInfo.InvariantCulture);
            dr["area_adr"] = Rekvizit.adres2;
            dr["area_phone"] = Rekvizit.phone2;
            return true;
        }

        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillCalcGil(DataRow dr)
        {
            if (dr == null) return false;
            dr["countGil"] = CountGilWithoutArrived != 1 ? CountGil + CountArriveGil : CountGil;
            dr["countDepartureGil"] = CountDepartureGil;
            dr["countArriveGil"] = CountArriveGil;
            return true;
        }

        /// <summary>
        /// Подсчет контрольной суммы в штрих-коде
        /// </summary>
        /// <param name="acode">Штрих-код</param>
        /// <returns>Контрольная цифра</returns>
        public string BarcodeCrc(string acode)
        {
            int sum = 0;


            for (int i = 0; i < acode.Length; i++)
            {
                if (i != 30)
                {
                    if ((i % 2) == 1)
                    {
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1));
                    }
                    else sum = sum + 3 * Convert.ToInt16(acode.Substring(i, 1));
                }
            }

            String s = ((10 - sum % 10) % 10).ToString(CultureInfo.InvariantCulture);

            return s.Substring(0, 1);

        }

        /// <summary>
        /// Формирование штрих-кода
        /// </summary>
        /// <returns>Готовый штрих-код</returns>
        public override string GetBarCode()
        {
            if (Pkod.Length > 13)
                Pkod = Pkod.Substring(0, 13);
            else
                Pkod = ("0000000000000").Substring(0, 13 - Pkod.Length) + Pkod;

            string vars = "33" + Pkod + Month.ToString("D2") +
             (Year - 2000).ToString("D2") + "00" +
             (Math.Max(0, SumTicket) * 100).ToString("00000000");

            return (Pref.ToLower() == "tula1" || Pref.ToLower() == "ntul25" || Pref.ToLower() == "tula" || Pref.ToLower() == "ntul24")
                ? "199 " + vars + BarcodeCrc(vars) 
                : "208 " + vars + BarcodeCrc(vars);
        }

        /// <summary>
        /// Признак отображения услуги в таблице начислений
        /// </summary>
        /// <param name="aServ"></param>
        /// <returns></returns>
        public override bool IsShowServInGrid(BaseServ aServ)
        {
            if (
                (Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (Math.Abs(aServ.Serv.SumInsaldo) < 0.001m) &
                (Math.Abs(aServ.Serv.SumCharge) < 0.001m)
            )
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Вычисление суммы к оплате по счету
        /// </summary>
        /// <param name="finder"></param>
        public override void FinalPass(Faktura finder)
        {
            base.FinalPass(finder);

            if (SummaryServ.Serv.SumCharge < 0) SummaryServ.Serv.SumCharge = 0;
            SumTicket = SummaryServ.Serv.SumCharge;

            SumTicket = SummaryServ.Serv.SumCharge;
            if (finder.newSumOpl > 0.001m)
                SumTicket = finder.newSumOpl;

        }

        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillAdr(DataRow dr)
        {
            if (dr == null) return false;
            dr["fio"] = PayerFio;
            string index = (Indecs != "" && Indecs != "-" ? Indecs + ", " : "");
            string street = (Ulica != "-" && Ulica != "" ? Ulica + ", " : Ulica);
            string district = (Rajon != "" && Rajon != "-" && Rajon != null ? Rajon + ", " : "");
            string city = (Town != "" && Town != "-" && Town != null ? Town + ", " : "");
            string flat = (NumberFlat != "" && NumberFlat != "-" && NumberFlat != null ? " кв." + NumberFlat : "");
            string room = (NumberRoom != "" && NumberRoom != "-" && NumberRoom != null ? " ком." + NumberRoom : "");
            dr["adres"] = index + city + district + street + " д. " + NumberDom + flat + room;

            dr["kv_pl"] = FullSquare.ToString("0.00");
            dr["type_pl"] = "общая";
            return true;
        }

        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillServiceVolume(DataRow dr)
        {
            return true;
        }

        /// <summary>
        /// Форматирования значения для вывода объема по услуге на экран
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="aValue"></param>
        /// <param name="colName"></param>
        protected override void FillGoodServVolume(DataRow dr, decimal aValue, string colName)
        {

            if (Math.Abs(aValue) < 0.001m)
            {
                dr[colName] = "";
            }
            else
            {

                if (colName.IndexOf("rash_pu", StringComparison.Ordinal) > -1)
                    dr[colName] = aValue.ToString("0.00");
                else
                    dr[colName] = aValue.ToString("0.00##");
            }
        }

        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <param name="index"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        private void FillServiceVolume(DataRow dr, int index, int nzpServ)
        {
            int anzpServ = nzpServ;



            int numRec = -1;
            for (int i = 0; i < ListVolume.Count; i++)
            {
                if (ListVolume[i].NzpServ == anzpServ)
                {
                    numRec = i;
                }
            }
            if (numRec == -1) return;


            if ((anzpServ == 25) & (KfodnEl != ""))
            {
                dr["rash_norm_odn" + index] = KfodnEl;
            }

            if ((anzpServ == 6) & (Kfodnhvs != "") & (HasHvsDpu))
            {
                dr["rash_norm_odn" + index] = Kfodnhvs;
            }

            if ((anzpServ == 9) & (Kfodnhvs != "") & (HasGvsDpu))
            {
                dr["rash_norm_odn" + index] = Kfodnhvs;
            }

            //Если домового прибора учета нет, то не печатаем
            if (((anzpServ == 25) & (HasElDpu)) ||
                ((anzpServ == 6) & (HasHvsDpu)) ||
                ((anzpServ == 9) & (HasGvsDpu)) ||
                ((anzpServ == 10) & (HasGazDpu)) ||
                ((anzpServ == 8) & (HasOtopDpu)))
            {
                FillGoodServVolume(dr, ListVolume[numRec].DomVolume + ListVolume[numRec].OdnDomVolume,
                    "rash_dpu" + index);
                FillGoodServVolume(dr, ListVolume[numRec].DomVolume, "rash_dpu_pu" + index);
                FillGoodServVolume(dr, ListVolume[numRec].OdnDomVolume, "rash_dpu_odn" + index);
            }


            if ((anzpServ == 25) & (dr["c_calc" + index].ToString().Trim() == ""))
            {
            }
            else
            {
                FillGoodServVolume(dr, ListVolume[numRec].NormaVolume, "rash_norm" + index);

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
        }



        /// <summary>
        /// Отображение особых услуг в заголовке таблицы
        /// </summary>
        /// <param name="nzpServ"></param>
        /// <param name="dr"></param>
        /// <param name="index"></param>
        /// <param name="defaultName"></param>
        private void ShowSpecServ(int nzpServ, DataRow dr, int index, string defaultName)
        {
            //dr["name_serv" + index] = defaultName;
            foreach (BaseServ t in ListServ)
            {
                if (t.Serv.NzpServ == nzpServ)
                {
                    dr["name_serv" + index] = t.Serv.NameServ;
                    dr["rsum_tarif_all" + index] = t.Serv.RsumTarif.ToString("0.00");
                    dr["tarif" + index] = t.Serv.Tarif.ToString("0.00");
                    dr["sum_dolg" + index] = (t.Serv.SumInsaldo -
                                              t.Serv.SumMoney).ToString("0.00");
                    dr["reval" + index] = (t.Serv.Reval + t.Serv.RealCharge).ToString("0.00");
                    dr["sum_charge_all" + index] = t.Serv.SumCharge.ToString("0.00");
                    dr["measure" + index] = t.Serv.Measure.Trim();
                }
            }
        }

        /// <summary>
        /// Заполнение таблицы по поставщикам
        /// </summary>
        /// <param name="dr"></param>
        private void FillSupplierGrid(DataRow dr)
        {
            int index = 1;
            var joinedListSupp = new List<BaseServ>();
            //если услуга и поставщик повторяются, то объединяем их суммы
            foreach (var baseServ in ListSupp)
            {
                if (joinedListSupp.Exists(s => (s.Serv.NameServ == baseServ.Serv.NameServ && s.Serv.NameSupp == baseServ.Serv.NameSupp)))
                {
                    joinedListSupp.Find(
                        s => (s.Serv.NameServ == baseServ.Serv.NameServ && s.Serv.NameSupp == baseServ.Serv.NameSupp))
                        .Serv.SumOutsaldo += baseServ.Serv.SumOutsaldo;
                }
                else
                    joinedListSupp.Add(baseServ);
            }
            foreach (BaseServ t in joinedListSupp)
            {
                if (IsShowServInGrid(t))
                {
                    if (index < 10)
                    {
                        //Если выбран дом с непосредственным управлением или управление не выбрано, то услуга значится как "Содержание и текущий ремонт многоквартирного дома" 
                        if (NzpArea == 1003 || NzpArea == 1004 || NzpArea == 1016) t.Serv.NameServ = t.Serv.NameServ.Replace("Содерж. и ремонт жилого помещения, управление МКД", "Содержание и текущий ремонт многоквартирного дома");
                        dr["supp_serv" + index] = t.Serv.NameServ.TrimEnd(',');
                        dr["supp_name" + index] = t.Serv.NameSupp;
                        dr["supp_inn" + index] = t.Serv.SuppRekv;
                        dr["supp_summ" + index] = t.Serv.SumOutsaldo.ToString("0.00");
                    }

                    index++;
                }
            }
        }

        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillMainChargeGrid(DataRow dr)
        {
            if (dr == null) return false;
            FillSupplierGrid(dr);
            if (GvsNormGkal == 0) GvsNormGkal = 0.0611m;
            SetServRashod();
            ListServ.Sort();

            decimal sumTarifAll = 0;
            decimal reval = 0;
            decimal sumDolg = 0;
            decimal sumCharge = 0;
            decimal tarif = 0;
            string measure = "";

            #region Собираем содержание жилья
            int remGilCount = 0;
            foreach (BaseServ t in ListServ)
            {
                //if ((listServ[countServ].serv.nzpServ != 6) &
                //    (listServ[countServ].serv.nzpServ != 7) &
                //    (listServ[countServ].serv.nzpServ != 8) &
                //    (listServ[countServ].serv.nzpServ != 9) &
                //    (listServ[countServ].serv.nzpServ != 10) &
                //    (listServ[countServ].serv.nzpServ != 14) &
                //    (listServ[countServ].serv.nzpServ != 210) &
                //    (listServ[countServ].serv.nzpServ != 25) &
                //    (listServ[countServ].serv.nzpServ != 15) &
                //    (listServ[countServ].serv.nzpServ != 16) &
                //    (listServ[countServ].serv.nzpServ != 206))
                //{

                if (t.Serv.NzpServ == 2)
                {
                    sumTarifAll += t.Serv.RsumTarif;
                    tarif += t.Serv.Tarif;
                    sumDolg += t.Serv.SumInsaldo - t.Serv.SumMoney;
                    reval += t.Serv.Reval + t.Serv.RealCharge;
                    sumCharge += t.Serv.SumCharge;
                    if (!t.Serv.Measure.Trim().Equals(""))
                    {
                        measure = t.Serv.Measure.Trim();
                    }
                    remGilCount++;
                }

            }

            if (remGilCount > 0)
            {
                dr["name_serv1"] = "Содержание и ремонт";
                dr["rsum_tarif_all1"] = sumTarifAll.ToString("0.00");
                dr["reval1"] = reval.ToString("0.00");
                dr["sum_charge_all1"] = sumCharge.ToString("0.00");
                dr["sum_dolg1"] = sumDolg.ToString("0.00");
                dr["tarif1"] = tarif.ToString("0.00");
                dr["measure1"] = measure;
            }

            //dr["name_serv1"] = "Содержание и ремонт";
            //dr["rsum_tarif_all1"] = sumTarifAll.ToString("0.00");
            //dr["reval1"] = reval.ToString("0.00");
            //dr["sum_charge_all1"] = sumCharge.ToString("0.00");
            //dr["sum_dolg1"] = sumDolg.ToString("0.00");
            //dr["tarif1"] = tarif.ToString("0.00");
            #endregion

            //Капитальный ремонт
            ShowSpecServ(206, dr, 2, "Капитальный ремонт");

            //Вывоз ТБО
            ShowSpecServ(16, dr, 3, "Вывоз ТБО");

            //Найм
            ShowSpecServ(15, dr, 4, "Найм");

            int numberString = 5;
            
            foreach (BaseServ t in ListServ)
            {
                if ((IsShowServInGrid(t)) & (
                    (t.Serv.NzpServ != 2) &
                    (t.Serv.NzpServ != 206) &
                    (t.Serv.NzpServ != 16) &
                    (t.Serv.NzpServ != 15)))
                {
                    dr["name_serv" + numberString] = t.Serv.NameServ.Trim();

                    dr["measure" + numberString] = t.Serv.Measure.Trim();



                    if ((Math.Abs(t.Serv.CCalc) > 0.001m) &
                        (t.Serv.IsOdn == false))
                    {

                        if ((t.Serv.RsumTarif ==
                             t.ServOdn.RsumTarif) & (t.Serv.RsumTarif > 0.001m))
                        {
                        }
                        else
                        {
                            if (Math.Abs(t.Serv.RsumTarif) > 0.001m)
                            {
                                if (t.Serv.NzpServ == 8)
                                    dr["c_calc" + numberString] =
                                        Math.Round((t.Serv.Tarif != 0 ? (t.Serv.RsumTarif / t.Serv.Tarif) : t.Serv.CCalc), 4)
                                            .ToString("0.0000");
                                else
                                    dr["c_calc" + numberString] = t.Serv.CCalc.ToString("0.0000");
                            }
                        }

                    }


                    if (Math.Abs(t.ServOdn.CCalc) > 0.001m)
                    {
                        if (t.Serv.NzpServ == 7)
                            dr["c_calc_odn" + numberString] = "x";
                        else
                            dr["c_calc_odn" + numberString] = t.ServOdn.CCalc.ToString("0.0000");
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

                    if (Math.Abs(t.Serv.RsumTarif - t.ServOdn.RsumTarif) > 0.001m)
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


                    if (Math.Abs(t.Serv.SumInsaldo -
                                        t.Serv.SumMoney) > 0.001m)
                    {
                        dr["sum_dolg" + numberString] =
                            (t.Serv.SumInsaldo -
                             t.Serv.SumMoney).ToString("0.00");
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

                    numberString++;
                }
            }
            for (int i = numberString; i < 19; i++)
            {
                dr["name_serv" + i] = "";
                dr["measure" + i] = "";
                dr["c_calc" + i] = "";
                dr["c_calc_odn" + i] = "";
                dr["tarif" + i] = "";
                dr["rsum_tarif" + i] = "";
                dr["rsum_tarif_odn" + i] = "";
                dr["rsum_tarif_all" + i] = "";
                dr["reval" + i] = "";
                dr["sum_lgota" + i] = "";
                dr["sum_charge_all" + i] = "";
                dr["sum_charge" + i] = "";
                dr["sum_charge_odn" + i] = "";
                dr["sum_nedop" + i] = "";
                dr["sum_sn" + i] = "";
                dr["sum_outsaldo" + i] = "";
                dr["real_charge" + i] = "";

            }
            dr["ls_msg"] = MessageForLs;
            dr["contract_content"] = ContractContentLs;
            dr["contract_footer"] = ContractFooterLs;
            return true;
        }

        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("poluch", typeof(string));
            table.Columns.Add("poluch_adres", typeof(string));
            table.Columns.Add("poluch2", typeof(string));
            table.Columns.Add("poluch2_adres", typeof(string));
            table.Columns.Add("poluch2_phone", typeof(string));
            table.Columns.Add("poluch2_bank", typeof(string));
            table.Columns.Add("poluch2_rs", typeof(string));
            table.Columns.Add("poluch2_ks", typeof(string));
            table.Columns.Add("poluch2_bik", typeof(string));
            table.Columns.Add("poluch2_inn", typeof(string));
            table.Columns.Add("adres", typeof(string));
            table.Columns.Add("adres2", typeof(string));
            table.Columns.Add("phone", typeof(string));
            table.Columns.Add("phone2", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("fio", typeof(string));
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
            table.Columns.Add("num_ls", typeof(string));
            table.Columns.Add("ngeu", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("geu_remark", typeof(string));
            table.Columns.Add("area_remark", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("rsum_tarif_all", typeof(string));
            table.Columns.Add("rsum_tarif_odn", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_charge_odn", typeof(string));
            table.Columns.Add("sum_insaldo", typeof(string));
            table.Columns.Add("sum_dolg", typeof(string));
            table.Columns.Add("sum_avans", typeof(string));
            table.Columns.Add("sum_peni", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_rub", typeof(string));
            table.Columns.Add("sum_kop", typeof(string));
            table.Columns.Add("ud", typeof(string));
            table.Columns.Add("Data_dolg", typeof(string));
            table.Columns.Add("dat_opl", typeof(string));
            table.Columns.Add("sum_last_opl", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("month_", typeof(string));
            table.Columns.Add("year_", typeof(string));
            table.Columns.Add("sum_tarif", typeof(string));
            table.Columns.Add("reval", typeof(string));
            table.Columns.Add("real_charge", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));
            table.Columns.Add("sum_next_avans", typeof(string));
            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("pl_mop", typeof(string));
            table.Columns.Add("dom_gil", typeof(string));
            table.Columns.Add("area_adr", typeof(string));
            table.Columns.Add("area_phone", typeof(string));
            table.Columns.Add("vib_kolgil", typeof(string));
            table.Columns.Add("countGil", typeof(string));
            table.Columns.Add("countDepartureGil", typeof(string));
            table.Columns.Add("countArriveGil", typeof(string));
            table.Columns.Add("Avans", typeof(string));
            table.Columns.Add("Dolgnik", typeof(string));

            for (int i = 1; i < 20; i++)
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
                table.Columns.Add("sum_dolg" + i, typeof(string));
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
                table.Columns.Add("rash_dpu" + i, typeof(string));
                table.Columns.Add("rash_dpu_pu" + i, typeof(string));
                table.Columns.Add("rash_dpu_odn" + i, typeof(string));

            }


            for (int i = 1; i < 10; i++)
            {
                table.Columns.Add("supp_serv" + i, typeof(string));
                table.Columns.Add("supp_name" + i, typeof(string));
                table.Columns.Add("supp_inn" + i, typeof(string));
                table.Columns.Add("supp_summ" + i, typeof(string));

            }

            for (int i = 1; i < 9; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
            }

            //todo 8
            for (int i = 1; i <= 6; i++)
            {
                table.Columns.Add("lsserv" + i, typeof(string));
                table.Columns.Add("lsnumcnt" + i, typeof(string));
                table.Columns.Add("lsdatuchet1_" + i, typeof(string));
                table.Columns.Add("lsdatuchet2_" + i, typeof(string));
                table.Columns.Add("lsvalcnt1_" + i, typeof(string));
                table.Columns.Add("lsvalcnt2_" + i, typeof(string));
                table.Columns.Add("lsvalcntprev" + i, typeof(string));
                table.Columns.Add("lsvalcnt" + i, typeof(string));
                table.Columns.Add("lsdate" + i, typeof(string));
            } 
            
            for (int i = 1; i <= 5; i++)
            {
                table.Columns.Add("domserv" + i, typeof(string));
                table.Columns.Add("domnumcnt" + i, typeof(string));
                table.Columns.Add("domdatuchet1_" + i, typeof(string));
                table.Columns.Add("domdatuchet2_" + i, typeof(string));
                table.Columns.Add("domvalcnt1_" + i, typeof(string));
                table.Columns.Add("domvalcnt2_" + i, typeof(string));
                table.Columns.Add("dom_measure" + i, typeof(string));
                table.Columns.Add("dom_cur" + i, typeof(string));
                table.Columns.Add("dom_rash" + i, typeof(string));
            }
            table.Columns.Add("ls_msg", typeof(string));
            table.Columns.Add("contract_content", typeof(string));
            table.Columns.Add("contract_footer", typeof(string));

            return table;

        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
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
            if (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney > 0)
            {
                dr["sum_dolg"] = (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney).ToString("0.00");
                dr["sum_avans"] = "0.00";
            }
            else
            {
                dr["sum_dolg"] = "0.00";
                dr["sum_avans"] = (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney).ToString("0.00");
            }
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");

            var peni = ListServ.Find(s => s.Serv.NzpServ == 500);
            dr["sum_peni"] = peni != null
                ? peni.Serv.SumInsaldo - peni.Serv.SumMoney > 0
                    ? (peni.Serv.SumInsaldo - peni.Serv.SumMoney).ToString()
                    : "0.00"
                : "0.00";

            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");

            if (SummaryServ.Serv.SumOutsaldo < 0)
            {
                dr["sum_next_avans"] = SummaryServ.Serv.SumOutsaldo;
            }
            else
            {
                dr["sum_next_avans"] = "0.00";
            }

            if (((SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney) > SummaryServ.Serv.RsumTarif * 3)
                && (SummaryServ.Serv.RsumTarif > 0))
            {
                dr["Dolgnik"] = "1";
            }
            else
            {
                dr["Dolgnik"] = "0";
            }
            return true;
        }


        /// <summary>
        /// дата последней оплаты по счету
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillDatOpl(DataRow dr)
        {
            if (dr == null) return false;

            if (DateOplat != "")
            {
                dr["dat_opl"] = DateOplat + "г, ";
                dr["sum_last_opl"] = LastSumOplat + " руб.";
            }
            return true;
        }


        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillRevalReason(DataRow dr)
        {
            if (dr == null) return false;

            for (int i = 0; i < 6; i++)
            {
                dr["period_pere" + (i + 1)] = "";
                if (ListReval.Count > i)
                {
                    dr["serv_pere" + (i + 1)] = ListReval[i].ServiceName;

                    //if (listReval[i].nzpServ == 7) ;
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

                                dr["period_pere" + (i + 1)] = GilPeriods;
                                dr["osn_pere" + (i + 1)] = "Врем. выбытие жильца";
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

                            dr["period_pere" + (i + 1)] = GilPeriods + ", " + ListReval[i].ReasonPeriod;
                            dr["osn_pere" + (i + 1)] = "Врем. выбытие жильца, " + ListReval[i].Reason;
                        }
                        else
                        {

                            dr["osn_pere" + (i + 1)] = ListReval[i].Reason;
                            dr["period_pere" + (i + 1)] = ListReval[i].ReasonPeriod;
                        }
                    }
                    dr["sum_pere" + (i + 1)] = ListReval[i].SumReval;

                    //if (listReval[i].nzpServ == 6) ;
                    //if (listReval[i].nzpServ == 9) ;
                }
                else
                {
                    dr["serv_pere" + (i + 1)] = "";
                    dr["osn_pere" + (i + 1)] = "";
                    dr["sum_pere" + (i + 1)] = "";
                    dr["period_pere" + (i + 1)] = "";

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

        /// <summary>
        /// Заполнение банковских реквизитов и реквизитов счета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillRekvizit(DataRow dr)
        {
            if (dr == null) return false;
            dr["poluch2"] = Rekvizit.poluch2;
            dr["poluch2_adres"] = Rekvizit.adres2;
            dr["poluch2_rs"] = Rekvizit.rschet2;
            dr["poluch2_bank"] = Rekvizit.bank2;
            dr["poluch2_inn"] = Rekvizit.inn2;
            dr["poluch2_ks"] = Rekvizit.korr_schet2;
            dr["poluch2_phone"] = Rekvizit.phone2;



            dr["Data_dolg"] = "01." + Month.ToString("00") + "." + Year;
            dr["month_"] = Month;
            dr["year_"] = Year;
            dr["months"] = FullMonthName;

            if (NumberFlat.IndexOf("комн.", StringComparison.Ordinal) >= 0)
            {
                dr["kvnum"] = NumberFlat.Replace("комн.", "(") + ")";
            }

            return true;
        }


        /// <summary>
        /// Заполнение счетчиков
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override bool FillCounters(DataRow dr)
        {
            int countersIndex = 1;
            for (int i = 0; i < Math.Min(7, ListCounters.Count); i++)
            {
                switch (ListCounters[i].NzpServ)
                {
                    case 6: dr["lsserv" + countersIndex] = countersIndex + ". х/в " + ListCounters[i].Place.Trim(); break;
                    case 9: dr["lsserv" + countersIndex] = countersIndex + ". г/в " + ListCounters[i].Place.Trim(); break;
                    case 8: dr["lsserv" + countersIndex] = countersIndex + ". Отопл. " + ListCounters[i].Place.Trim(); break;
                    case 25: dr["lsserv" + countersIndex] = countersIndex + ". Эл.снаб. " + ListCounters[i].Place.Trim(); break;
                    case 210: dr["lsserv" + countersIndex] = countersIndex + ". Ноч.Эл. " + ListCounters[i].Place.Trim(); break;
                    default: dr["lsserv" + countersIndex] = countersIndex + ". " + ListCounters[i].ServiceName; break;
                }
                dr["lsnumcnt" + countersIndex] = ListCounters[i].NumCounters;
                dr["lsdatuchet2_" + countersIndex] = ListCounters[i].DatUchet.ToShortDateString();
                dr["lsvalcnt2_" + countersIndex] = ListCounters[i].Value.ToString("0.00");
                dr["lsvalcntprev" + countersIndex] = ListCounters[i].ValuePred.ToString("0.00");
                dr["lsvalcnt" + countersIndex] = ListCounters[i].Value.ToString("0.00");
                dr["lsdate" + countersIndex] = ListCounters[i].DatProv;
                countersIndex++;
            }

            return true;
        }

        /// <summary>
        /// Заполнение домовых счетчиков
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override bool FillDomCounters(DataRow dr)
        {
            int countersIndex = 1;
            for (int i = 0; i < Math.Min(6, ListDomCounters.Count); i++)
            {
                switch (ListDomCounters[i].NzpServ)
                {
                    case 6: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Хол. вода"; break;
                    case 9: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Гор. вода"; break;
                    case 8: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Отопл."; break;
                    case 25: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Эл.снаб."; break;
                    case 210: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Ноч.Эл.."; break;
                    default: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". " + ListDomCounters[i].ServiceName; break;
                }

                //dr["domserv" + countersIndex] = listDomCounters[i].serviceName;
                dr["domnumcnt" + countersIndex] = ListDomCounters[i].NumCounters;
                dr["domdatuchet1_" + countersIndex] = ListDomCounters[i].DatUchetPred.ToShortDateString();
                dr["domdatuchet2_" + countersIndex] = ListDomCounters[i].DatUchet.ToShortDateString();
                dr["domvalcnt1_" + countersIndex] = ListDomCounters[i].ValuePred.ToString("0.00##");
                dr["domvalcnt2_" + countersIndex] = ListDomCounters[i].Value.ToString("0.00##");
                dr["dom_measure" + countersIndex] = ListDomCounters[i].Measure;
                dr["dom_cur" + countersIndex] = ListDomCounters[i].Value.ToString("0.00##");
                dr["dom_rash" + countersIndex] = (ListDomCounters[i].Value - ListDomCounters[i].ValuePred).ToString("0.00##");
                countersIndex++;
            }


            for (int i = countersIndex; i < 8; i++)
            {
                dr["domserv" + countersIndex] = "";
                dr["domnumcnt" + countersIndex] = "";
                dr["domdatuchet1_" + countersIndex] = "";
                dr["domdatuchet2_" + countersIndex] = "";
                dr["domvalcnt1_" + countersIndex] = "";
                dr["domvalcnt2_" + countersIndex] = "";
            }
            
            return true;
        }

        public override void OrderPrint(int nzpKvar)
        {
            LsFaktura listLsServ = new LsFaktura(nzpKvar);
            int order_print = 1;
            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (IsShowServInGrid(ListServ[countServ]))
                {
                    listLsServ.AddServ(ListServ[countServ].Serv.NzpServ, order_print);
                    order_print++;
                }
            }
            if (listLsServ.ListServ.Count > 0)
                DbfakturaOrdering.AddFaktura(listLsServ);
        }

        public TulaFaktura()
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
            FakturaBlocks.HasCountersDoubleDomBlock = true;
            FakturaBlocks.HasOdnBlock = true;
            FakturaBlocks.HasRdnBlock = true;
            FakturaBlocks.HasPerekidkiSamaraBlock = true;
            FakturaBlocks.HasRemarkblock = true;
            FakturaBlocks.HasGilPeriodsBlock = true;
            FakturaBlocks.HasCountersSpisBlock = false;
            FakturaBlocks.HasDatOplBlock = true;
            FakturaBlocks.HasCalcGil = true;
            FakturaBlocks.HasGilPeriodsBlock = true;
            FakturaBlocks.HasPrintOrdering = true;
            FakturaBlocks.HasNewCountersBlock = true;

            Clear();
            Kfodnhvs = "0.03";
        }

    }


}

