using System.Globalization;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;
    using System.IO;
    using Bars.KP50.Faktura.Source.Base.Barcode;

    public class SamaraFaktura : BaseFactura
    {
        private int MaxCountersCount = 6;

        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillCalcGil(DataRow dr)
        {
            if (dr == null) return false;
            return true;
        }

        protected override bool FillKvarPrm(DataRow dr)
        {
            //StreamWriter sw = new StreamWriter(@"C:\temp\people3.txt", true);
            //sw.WriteLine("1");
            string[] strArray = new string[13]
      {
        "Январь",
        "Февраль",
        "Март",
        "Апрель",
        "Май",
        "Июнь",
        "Июль",
        "Август",
        "Сентябрь",
        "Октябрь",
        "Ноябрь",
        "Декабрь",
        "Январь"
      };
            try
            {
                //if (dr == null) return false;
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

                if (NzpGeu > 100)
                    dr["ngeu"] = NzpGeu;
                else
                    dr["ngeu"] = Pkod.Substring(3, 2);
                dr["indecs"] = Indecs;
                dr["ud"] = Ud;
                dr["num_ls"] = NumLs.PadLeft(6, '0');
                dr["pkod"] = Pkod;
                dr["pl_dom"] = DomSquare.ToString("0.00");
                dr["pl_mop"] = MopSquare.ToString("0.00");//MopSquare
                dr["dom_gil"] = CountDomGil.ToString();
                decimal otopDpu;
                try
                {
                    otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu.Replace('.', ','));
                }
                catch
                {
                    otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu);
                }
                dr["rash_dpu_pu_otop"] = otopDpu.ToString("0.00");
                dr["monthscounters"] = Month == 12 ? (strArray[Month] + " " + (Year + 1)) : (strArray[Month] + " " + Year);
                //sw.Close();
                return true;
            }
            catch (Exception e)
            {
                //sw.WriteLine(e.ToString());
                //sw.Close();
                return true;
            }
        }

        public string BarcodeCrcSamara(string acode)
        {
            int sum_ = 0;


            for (int i = 0; i < acode.Length; i++)
            {
                if (i != 29)
                {
                    if ((i % 2) == 1)
                    {
                        sum_ = sum_ + Convert.ToInt16(acode.Substring(i, 1));
                    }
                    else sum_ = sum_ + 3 * Convert.ToInt16(acode.Substring(i, 1));
                }
            }

            String s = ((10 - sum_ % 10) % 10).ToString();

            return s.Substring(0, 1);

        }

        public override string GetBarCode()
        {
            StreamWriter streamWriter = new StreamWriter("C:\\temp\\bar.txt", false);
            streamWriter.WriteLine("1");
            string vars = "63" + Pkod + Month.ToString("D2") +
             (Year - 2000).ToString("D2") +
             (Math.Max(0, SumTicket) * 100).ToString("000000000");
            Shtrih = vars + BarcodeCrcSamara(vars) + BarcodeCrcSamara(vars);
            GeuKodErc = "630100000015";
            streamWriter.Close();
            return Shtrih;
        }

        public override string GetQRCode()
        {
            StreamWriter streamWriter = new StreamWriter("C:\\temp\\qr.txt", false);
            var dm = new DataMatrix(Rekvizit.poluch2, Rekvizit.rschet2, Rekvizit.bank2, Rekvizit.bik2);//создается новый объект класса DataMatrix, представляющий сущность qrкода
            dm.CorrespAcc = Rekvizit.korr_schet2;//заполнение корр.счета
            dm.PayeeINN = Rekvizit.inn2;//заполнение ИНН
            dm.KPP = Rekvizit.kpp2;//заполнение КПП
            var fio = PayerFio.Trim().Split((string[])null, StringSplitOptions.RemoveEmptyEntries);//заполнение ФИО
            if (fio.Length == 3)//если ФИО из трех частей, то заполнить отдельно фамилию имя и отчество
            { dm.LastName = fio[0]; dm.FirstName = fio[1]; dm.MiddleName = fio[2]; }
            else
                dm.LastName = string.Join(" ", fio);//иначе заполнить целиком в поле с фамилией
            dm.PayerAddress = Indecs + ", " + (Town != "" ? Town : Rajon) + ", " + Ulica + ", д." + NumberDom +
            (NumberFlat != "" ? ", кв. " + NumberFlat : string.Empty) +
            (NumberRoom != "" ? ", комн. " + NumberRoom : string.Empty);//заполнить адрес
            dm.Purpose = "Оплата за ЖКУ";//назначение оплаты вшита жестко
            dm.PersAcc = Convert.ToInt32(Pkod.Substring(Pkod.Length - 6, 5) + (Pkod.Substring(Pkod.Length - 1, 1) == "0" ? "0" : "")).ToString();//заполнение лицевого счета
            dm.PaymPeriod = DateTime.Now.ToShortDateString().Substring(3);//дата без первых трех символов(это 2 цифры - день и точка после)
            dm.Sum = (int)SumTicket + " руб. " + (int)((SumTicket % 1) * 100) + " коп.";//сумма к оплате в рублях и копейках
            streamWriter.WriteLine("1");
            if (dm.CodingPayment()) //метод кодирует все поля в одну строку для дальнейшей генарации qr кода
            {
                streamWriter.WriteLine("2");
                streamWriter.Close();
                return dm.EncodedString;
            }
            streamWriter.Close();
            return "0";
        }

        public override bool IsShowServInGrid(BaseServ aServ)
        {


            if (
                (System.Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (System.Math.Abs(aServ.Serv.CCalc) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumCharge) < 0.001m)
            )
            {
                return false;
            }
            return true;
        }

        public override void FinalPass(Faktura finder)
        {
            base.FinalPass(finder);
            if ((finder.withDolg) || ((SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney) < 0))
            {
                SumTicket = SummaryServ.Serv.SumCharge + SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney;
                if (SumTicket < 0) SumTicket = 0;
            }
            else
            {
                SumTicket = SummaryServ.Serv.SumCharge;
                if (SumTicket < 0) SumTicket = 0;
            }

            if (finder.newSumOpl > 0.001m)
                SumTicket = finder.newSumOpl;

        }

        /// <summary>
        /// Заполнение счетчиков
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override bool FillCounters(DataRow dr)
        {
            try
            {
                int num = 1;
                for (int index = 0; index < Math.Min(MaxCountersCount, ListCounters.Count); ++index)
                {
                    switch (ListCounters[index].NzpServ)
                    {
                        case 6:
                            dr["lsserv" + num] = num + ". х/в " + ListCounters[index].Place.Trim();
                            break;
                        case 8:
                            dr["lsserv" + num] = num + ". Отопл. " + ListCounters[index].Place.Trim();
                            break;
                        case 9:
                            dr["lsserv" + num] = num + ". г/в " + ListCounters[index].Place.Trim();
                            break;
                        case 25:
                            dr["lsserv" + num] = num + ". Э/Э Д" + ListCounters[index].Place.Trim();
                            break;
                        case 210:
                            dr["lsserv" + num] = num + ". Э/Э Н" + ListCounters[index].Place.Trim();
                            break;
                        default:
                            dr["lsserv" + num] = num + ". " + ListCounters[index].ServiceName;
                            break;
                    }
                    dr["lsnumcnt" + num] = ListCounters[index].NumCounters;
                    dr["lsdatuchet2_" + num] = ListCounters[index].DatUchet.ToShortDateString();
                    dr["lsvalcnt2_" + num] = ListCounters[index].Value.ToString("0.00");
                    ++num;
                }
            }
            catch (Exception ex)
            {
                
            }
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



        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillServiceVolume(DataRow dr)
        {
            return true;
        }

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
                else if (colName.IndexOf("rash_norm", StringComparison.Ordinal) > -1)
                    dr[colName] = aValue.ToString("0.00##");
                else
                    dr[colName] = aValue.ToString("0.0000");
            }
        }

        protected bool FillServiceVolume(DataRow dr, int index, int nzpServ)
        {
            int anzpServ = nzpServ;

            if (nzpServ == 9)
            {
                if ((anzpServ == 9) & (HasGvsDpu))
                {
                    dr["rash_norm_odn" + index] = GvsNormGkal;
                }


                //FillGoodServVolume(dr, hvsGvsNorma, "rash_norm" + index.ToString());

                return true;
            }

            if (nzpServ == 14)
            {
                anzpServ = 9;
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

            return true;
        }

        protected bool FillServiceVolumeRashPU(DataRow dr, int index, int nzpServ)
        {
            int num1 = nzpServ;
            int num2 = nzpServ;
            if (nzpServ == 99)
                return true;
            if (nzpServ == 14)
                num1 = 9;
            int index1 = -1;
            decimal sumCountersValue = 0;
            string countersVal = "";
            for (int k = 0; k < ListCounters.Count; k++)
            {
                if ((ListCounters[k].NzpServ == num1))
                {
                    if (countersVal.Length == 0)
                        countersVal += ListCounters[k].Value.ToString();
                    else
                        countersVal += "/" + ListCounters[k].Value.ToString();

                }
            }

            if (nzpServ != 9)
                dr["rash_pu" + index] = countersVal;
           
            return true;
        }

        /// <summary>
        /// Процедура определяет способ получения объема для расчета
        /// </summary>
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

                //    for (int i = 0; i < listCounters.Count; i++)
                //    {
                //        if ((aserv == listCounters[i].nzpServ)&(listCounters[i].datUchet>
                //             System.Convert.ToDateTime("01." + month.ToString() + "." + year.ToString())))
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
            if(aserv == 210)
                return "(2)";

            return "";
        }


        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillMainChargeGrid(DataRow dr)
        {
            if (dr == null) return false;
            if (GvsNormGkal == 0) GvsNormGkal = 0.0611m;
            SetServRashod();
            ListServ.Sort();
            int numberString = 1;

            foreach (BaseServ t in ListServ)
            {
                if (IsShowServInGrid(t))
                {
                    if (t.Serv.NameServ.Trim() == "Холодная вода")
                        dr["name_serv" + numberString] = t.Serv.NameServ.Trim();
                    else
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
                                dr["c_calc" + numberString] = t.Serv.CCalc.ToString("0.00##") +
                                                              GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                            else
                                if (Math.Abs(t.ServOdn.CCalc) > 0.00001m)
                                {
                                    dr["c_calc" + numberString] = t.Serv.CCalc.ToString("0.00##") +
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

                        dr["c_calc_odn" + numberString] = t.ServOdn.CCalc.ToString("0.0000") +
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


                    //Для единицы измерения Гкал 
                    if ((t.Serv.NzpMeasure == 4) & (Math.Abs(t.Serv.RsumTarif) > 0.001m))
                    {

                        /*  dr["c_calc" + numberString.ToString()] = ((listServ[countServ].serv.rsumTarif -
                            listServ[countServ].servOdn.rsumTarif) /
                            listServ[countServ].serv.tarif).ToString(precision)+
                             GetVolumeSource(listServ[countServ].serv.nzpServ, listServ[countServ].serv.isDevice);
                      */

                        if (t.Serv.OldMeasure == 4)
                        {
                            if (t.Serv.NzpServ == 9)
                            {
                                if (Math.Abs(t.Serv.CCalc) > 0.00001m)
                                {
                                    dr["c_calc" + numberString] = (t.Serv.CCalc).ToString("0.0000") +
                                                                             GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                }
                            }
                            else
                            {
                                dr["c_calc" + numberString] = (t.Serv.CCalc).ToString("0.0000") +
                                                                         GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                            }
                        }
                        else
                        {
                            if (t.Serv.NzpServ == 9)
                            {
                                if (Math.Abs(t.Serv.CCalc) > 0.00001m)
                                {
                                    dr["c_calc" + numberString] = (t.Serv.CCalc * GvsNormGkal).ToString("0.0000") +
                                                                             GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);
                                }
                            }
                            else
                            {
                                dr["c_calc" + numberString] = (t.Serv.CCalc * OtopNorm).ToString("0.0000") +
                                                                         GetVolumeSource(t.Serv.NzpServ, t.Serv.IsDevice);

                            }

                        }

                        if (Math.Abs(t.ServOdn.CCalc) > 0.000001m)
                        {
                            string sourceOdn = "(1)";

                            if (HasGvsDpu) sourceOdn = "(4)";
                            //  dr["c_calc_odn" + numberString.ToString()] = (listServ[countServ].servOdn.rsumTarif /
                            //      listServ[countServ].serv.tarif).ToString("0.0000") + sourceOdn;

                            /*  if (listServ[countServ].serv.oldMeasure == 4)
                            {
                                dr["c_calc_odn" + numberString.ToString()] =
                                     listServ[countServ].servOdn.cCalc.ToString("0.0000") + sourceOdn;
                            }
                            else
                            {
                                dr["c_calc_odn" + numberString.ToString()] =
                                     (listServ[countServ].servOdn.cCalc * 0.0611m).ToString("0.0000") + sourceOdn;
                            }
                           */
                            if (t.Serv.NzpServ == 9)
                            {
                                dr["c_calc_odn" + numberString] =
                                    (t.ServOdn.CCalc).ToString("0.0000") + sourceOdn;
                            }
                            else
                            {
                                dr["c_calc_odn" + numberString] =
                                    (t.ServOdn.CCalc).ToString("0.0000") + sourceOdn;

                            }
                        }

                        FillGoodServVolume(dr, t.Serv.NzpServ == 9
                            ? GvsNormGkal
                            : OtopNorm, "rash_norm" + numberString);
                    }



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


            return true;
        }


        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected List<BaseServ> SortServ(List<BaseServ> baseList)
        {
            List<BaseServ> returnList = new List<BaseServ>();
            int pos = 0;
            int posAfter = 0;
            foreach (BaseServ bs in baseList)
            {
                if (bs.Serv.NzpServ == 9)
                {
                    returnList.Insert(pos, bs);
                    posAfter = pos + 1;
                }
                else if (bs.Serv.NzpServ == 14)
                {
                    returnList.Insert(posAfter, bs);
                }
                else
                {
                    returnList.Insert(pos, bs);
                }
                pos++;
            }
            return returnList;
        }

        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillRevalReason(DataRow dr)
        {
            if (dr == null) return false;

            //int kanNumber = -1;
            //string hvsReason = "";
            //string gvsReason = "";
            for (int i = 0; i < 9; i++)
            {
                if (ListReval.Count > i)
                {
                    dr["serv_pere" + (i + 1)] = ListReval[i].ServiceName;

                    //if (listReval[i].nzpServ == 7) kanNumber = i + 1;
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
                            //dr["osn_pere" + (i + 1).ToString()] = "Врем. выбытие жильца, " + listReval[i].reason ;
                            dr["osn_pere" + (i + 1)] = "Врем. выбытие жильца  " + GilPeriods +
                                ", " + ListReval[i].Reason + " " + ListReval[i].ReasonPeriod;
                        }
                        else
                        {
                            dr["osn_pere" + (i + 1)] = ListReval[i].Reason + " " + ListReval[i].ReasonPeriod;
                        }
                    }
                    dr["sum_pere" + (i + 1)] = ListReval[i].SumReval;
                    //if (listReval[i].nzpServ == 6) hvsReason = dr["osn_pere" + (i + 1)].ToString();
                    //if (listReval[i].nzpServ == 9) gvsReason = dr["osn_pere" + (i + 1)].ToString();
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

        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("str_rekv1", typeof(string));
            table.Columns.Add("str_rekv2", typeof(string));
            table.Columns.Add("str_rekv3", typeof(string));
            table.Columns.Add("str_rekv4", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("typek", typeof(string));
            table.Columns.Add("Platelchik", typeof(string));
            table.Columns.Add("indecs", typeof(string));
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
            table.Columns.Add("datamatrix", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("rsum_tarif_all", typeof(string));
            table.Columns.Add("rsum_tarif_odn", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_charge_odn", typeof(string));
            table.Columns.Add("sum_insaldo", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_rub", typeof(string));
            table.Columns.Add("sum_kop", typeof(string));
            table.Columns.Add("sum_rub_debt", typeof(string));
            table.Columns.Add("sum_kop_debt", typeof(string));
            table.Columns.Add("ud", typeof(string));
            table.Columns.Add("Data_dolg", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("monthscounters", typeof(string));
            table.Columns.Add("month_", typeof(string));
            table.Columns.Add("year_", typeof(string));
            table.Columns.Add("sum_tarif", typeof(string));
            table.Columns.Add("reval", typeof(string));
            table.Columns.Add("revalEpd", typeof(string));
            table.Columns.Add("real_charge", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));
            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("pl_mop", typeof(string));
            table.Columns.Add("dom_gil", typeof(string));
            table.Columns.Add("rash_dpu_pu_otop", typeof(string));
            table.Columns.Add("sum_peni", typeof(string));
            table.Columns.Add("sum_peni_reval", typeof(string));
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

            for (int i = 1; i < 10; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
            }

            for (int i = 1; i < 8; i++)
            {
                table.Columns.Add("domserv" + i, typeof(string));
                table.Columns.Add("domnumcnt" + i, typeof(string));
                table.Columns.Add("domdatuchet1_" + i, typeof(string));
                table.Columns.Add("domdatuchet2_" + i, typeof(string));
                table.Columns.Add("domvalcnt1_" + i, typeof(string));
                table.Columns.Add("domvalcnt2_" + i, typeof(string));
                table.Columns.Add("lsserv" + i, typeof(string));
                table.Columns.Add("lsnumcnt" + i, typeof(string));
                table.Columns.Add("lsdatuchet1_" + i, typeof(string));
                table.Columns.Add("lsdatuchet2_" + i, typeof(string));
                table.Columns.Add("lsvalcnt1_" + i, typeof(string));
                table.Columns.Add("lsvalcnt2_" + i, typeof(string));
                table.Columns.Add("datuchet2_" + i, typeof(string));
                table.Columns.Add("domdatProv_" + i, typeof(string));
                table.Columns.Add("lsdatProv_" + i, typeof(string));

            }
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
            Decimal d = 0;
            try
            {
                IDbConnection _conDb = DBManager.GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
                DBManager.OpenDb(_conDb, true);
                string dateTo = (Year).ToString() + "-" + (Month).ToString("00") + "-16";
                string dateFrom = (Year).ToString() + "-" + (Month).ToString("00") + "-01";
                string numLs = NumLs;
                string sql = "SELECT num_ls, sum(g_sum_ls) as  g_sum_ls FROM fbill_fin_15.pack_ls  WHERE num_ls = " + numLs + " and dat_vvod < '" + dateTo + "' and dat_vvod > '" + dateFrom + "' group by 1";
               
                IDataReader reader = null;
                IDbCommand cmd = DBManager.newDbCommand(sql, _conDb);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    d = Decimal.Parse(reader["g_sum_ls"].ToString());
                }
                DBManager.CloseDb(_conDb);
                
            }
            catch (Exception e)
            {
            }
                   
            //dr["sum_insaldo"] = (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.MoneyTo - d).ToString("0.00");
            //dr["sum_money"] = (SummaryServ.Serv.SumMoney - SummaryServ.Serv.MoneyTo).ToString("0.00");
            dr["sum_insaldo"] = (SummaryServ.Serv.SumInsaldo).ToString("0.00");
            dr["sum_money"] = (SummaryServ.Serv.SumMoney).ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            dr["sum_rub_debt"] = (SumTicket + SummaryServ.Serv.SumInsaldo > 0) ? Decimal.Truncate(SumTicket + SummaryServ.Serv.SumInsaldo).ToString("0") : 0.ToString("0");
            dr["sum_kop_debt"] = (SumTicket + SummaryServ.Serv.SumInsaldo > 0) ? (((SumTicket + SummaryServ.Serv.SumInsaldo) % 1) * 100).ToString("0") : 0.ToString("00");


            return true;
        }


        public SamaraFaktura()
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
            FakturaBlocks.HasPerekidkiSamaraBlock = true;
            FakturaBlocks.HasRemarkblock = true;
            FakturaBlocks.HasGilPeriodsBlock = true;
            FakturaBlocks.HasCountersSpisBlock = true;
            FakturaBlocks.HasNormblock = true;
            FakturaBlocks.HasCalcGil = true;
            Clear();
            //kfodnhvs = "0.03";
        }

    }


    public class SamaraFakturaDolg : SamaraFaktura
    {
        public override void AddServ(BaseServ aServ)
        {

            //Проверка на пустоту добавляемой услуги
            if (aServ.Empty()) return;
            SummaryServ.AddSum(aServ.Serv, false); //Подсчитываем Итого
        }

        public override void AddReval(SumServ aServ)
        {

        }

        public override void AddCounters(Counters aCounter)
        {
        }


        public override void AddVolume(ServVolume aVolume)
        {
        }

        public override void AddDomVolume(ServVolume aVolume)
        {
        }

        public override void AddPerekidkaOdn(int nzpServ, decimal sumRcl)
        {
        }

        protected override bool FillMainChargeGrid(DataRow dr)
        {
            return true;
        }

        protected override bool FillKvarPrm(DataRow dr)
        {
            StreamWriter sw = new StreamWriter(@"C:\temp\people2.txt", false);
            sw.WriteLine("1");
            sw.Close();
            //if (dr == null) return false;
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

            if (NzpGeu > 100)
                dr["ngeu"] = NzpGeu.ToString().Substring(1, 2);
            else
                dr["ngeu"] = Pkod.Substring(3, 2);
            dr["indecs"] = Indecs;
            dr["ud"] = Ud;
            dr["num_ls"] = NumLs;
            dr["pkod"] = Pkod;
            dr["pl_dom"] = DomSquare.ToString("0.00");
            dr["pl_mop"] = MopSquare.ToString("0.00");//MopSquare
            dr["dom_gil"] = CountDomGil.ToString();
            decimal otopDpu;
            try
            {
                otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu.Replace('.', ','));
            }
            catch
            {
                otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu);
            }
            dr["rash_dpu_pu_otop"] = otopDpu.ToString("0.00");
            return true;
        }

        /// Заполнение итоговой строки по счету
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillSummuryBill(DataRow dr)
        {
            if (dr == null) return false;
            dr["sum_insaldo"] = (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.MoneyTo).ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            return true;
        }

        public override bool DoPrint()
        {
            if (((SumTicket > 0.001m) & (SumTicket > SummaryServ.Serv.RsumTarif * 3))
                || (BillRegim == Faktura.WorkFakturaRegims.One))
            {
                return true;
            }
            return false;
        }

        public override void FinalPass(Faktura finder)
        {
            SumTicket = SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney;
        }

        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("str_rekv1", typeof(string));
            table.Columns.Add("str_rekv2", typeof(string));
            table.Columns.Add("str_rekv3", typeof(string));
            table.Columns.Add("str_rekv4", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("geu_remark", typeof(string));
            table.Columns.Add("area_remark", typeof(string));
            table.Columns.Add("Platelchik", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("priv", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("ls", typeof(string));
            table.Columns.Add("ngeu", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_charge_odn", typeof(string));
            table.Columns.Add("sum_insaldo", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_rub", typeof(string));
            table.Columns.Add("sum_kop", typeof(string));
            table.Columns.Add("ud", typeof(string));
            table.Columns.Add("Data_dolg", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("month_", typeof(string));
            table.Columns.Add("year_", typeof(string));
            table.Columns.Add("sum_tarif", typeof(string));
            table.Columns.Add("reval", typeof(string));
            table.Columns.Add("revalEpd", typeof(string));
            table.Columns.Add("real_charge", typeof(string));
            return table;

        }

        public override void Clear()
        {
            CountGil = 0;
            CountRegisterGil = 0;
            CountDepartureGil = 0;
            CountArriveGil = 0;

            FullSquare = 0;
            LiveSquare = 0;
            CalcSquare = 0;
            HeatSquare = 0;

            Ownflat = false;
            IsolateFlat = true;
            PayerFio = "";

            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
            FullMonthName = "";
            Pkod = "";
            LicSchet = "";
            Geu = "";
            Ud = "";
            Ulica = "";
            NumberDom = "";
            NumberFlat = "";
            PrefixUk = "";
            CodeUk = "";

            NzpArea = 0;
            NzpGeu = 0;


            SummaryServ.Clear();

            Rekvizit.nzp_geu = 0;
            Rekvizit.nzp_area = 0;
            Rekvizit.poluch = "";
            Rekvizit.bank = "";
            Rekvizit.rschet = "";
            Rekvizit.korr_schet = "";
            Rekvizit.bik = "";
            Rekvizit.inn = "";
            Rekvizit.phone = "";
            Rekvizit.adres = "";
            Rekvizit.pm_note = "";
            Rekvizit.poluch2 = "";
            Rekvizit.bank2 = "";
            Rekvizit.rschet2 = "";
            Rekvizit.korr_schet2 = "";
            Rekvizit.bik2 = "";
            Rekvizit.inn2 = "";
            Rekvizit.phone2 = "";
            Rekvizit.adres2 = "";
            Rekvizit.filltext = 0;

            Shtrih = "";

        }


        public SamaraFakturaDolg()
        {
            Rekvizit = new _Rekvizit();

            SummaryServ = new BaseServ(false);
            FakturaBlocks.HasAdrBlock = true;
            FakturaBlocks.HasRekvizitBlock = true;
            FakturaBlocks.HasKvarPrmBlock = true;
            FakturaBlocks.HasSummuryBillBlock = true;
            FakturaBlocks.HasMainChargeGridBlock = false;
            FakturaBlocks.HasRevalReasonBlock = false;
            FakturaBlocks.HasServiceVolumeBlock = false;
            Clear();

        }
    }

}

