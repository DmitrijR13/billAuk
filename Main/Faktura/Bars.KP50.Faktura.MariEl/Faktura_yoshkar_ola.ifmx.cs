using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.DB.Faktura;
using Bars.KP50.Faktura.Source.Base;
using Castle.Windsor.Installer;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.MariEl
{
    public class FakturaYoshkarOla : BaseFactura2
    {
        private const int MaxCountersCount = 10;
        private const int MaxServCount = 9;
        private const int MaxSpravCount = 8;
        private const int MaxRevalCount = 4;

        public override string Name
        {
            get { return "Йошкар-Ола"; }
        }


        private int[] _kommServ = { 6, 7, 8, 9, 14, 25, 210, 510, 511, 512, 513, 514, 515, 516, 517 };
        private int[] _odnServ = { 510, 511, 512, 513, 514, 515, 516, 517 };

        public override string Code { get { return "1002"; } }

        public override string FileName { get { return "yoshkarola354.frx"; } }

        //public override void Init(IDbConnection connection, STCLINE.KP50.Interfaces.Faktura startParams)
        //{
        //    StartParams = startParams;
        //    Rekvizits = new DbFakturaRekvizit(connection);
        //    SzInf = new DbFakturaSzInformation(connection, startParams.month_, startParams.year_);
        //    Area = new DbFakturaAreaPrm(connection, startParams.month_, startParams.year_);
        //    Geu = new DbFakturaGeuPrm(connection);
        //    Dom = new DbFakturaDomPrm(connection, startParams.month_, startParams.year_);
        //    Kvar = new DbFakturaKvarPrm(connection, startParams.month_, startParams.year_);
        //    Arendator = new DbFakturaArendPrm(connection, startParams.month_, startParams.year_);
        //    Counters = new DbFakturaCounters(connection, startParams.month_, startParams.year_);
        //    Charge = new DbFakturaCharge(connection, startParams.month_, startParams.year_);
        //    Reval = new DbFakturaReval(connection, startParams.month_, startParams.year_);
        //    ServVolumeDom = new DbFakturaServVolumeDom(connection, startParams.month_, startParams.year_);
        //    ServVolumeLs = new DbFakturaServVolumeLs(connection, startParams.month_, startParams.year_);
        //    Payments = new DbFakturaPayments(connection, startParams.month_, startParams.year_);
        //    Instalment = new DbFakturaInstalment354(connection, startParams.month_, startParams.year_);
        //    SpravInf = new DbFakturaSpravInformation(connection, startParams.month_, startParams.year_);
        //    Clear();
        //}

        /// <summary>
        /// Заполнение квартирных парметров
        /// </summary>
        /// <returns></returns>
        protected override bool FillKvarPrm()
        {
            if (Dr == null) return false;
            Dr["pkod"] = Pkod;
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
            return vars + BarcodeCrc(vars);
        }


        protected override bool FillBarcode()
        {
            Dr["vars"] = "12710 " + GetBarCode();
            return true;
        }


        /// <summary>
        /// Признак отображения услуги в таблице начислений
        /// </summary>
        /// <param name="aServ"></param>
        /// <returns></returns>
        public override bool IsShowServInGrid(BaseServ2 aServ)
        {
            ServVolume2 lsNorm = ServVolumeLs.GetServVolume(aServ.Serv.NzpServ);
            if (
                //(aServ.Serv.RsumTarif < 0) &&
                //(Math.Abs(aServ.Serv.CCalc) < 0.001m) &&
                //(Math.Abs(aServ.ServOdn.CCalc) < 0.001m) &&
                //(Math.Abs(lsNorm.Volume) < 0.001m) &&
                //(Math.Abs(lsNorm.Odn) < 0.001m) &&
                //(Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &&
                //(Math.Abs(aServ.Serv.Reval) < 0.001m) &&
                //(Math.Abs(aServ.Serv.RealCharge) < 0.001m) &&
                //(Math.Abs(aServ.Serv.SumInsaldo) < 0.001m) &&
                //(Math.Abs(aServ.Serv.SumCharge) < 0.001m) &&
                (Math.Abs(aServ.Serv.RsumTarif - aServ.ServOdn.RsumTarif) < 0.001m) &&
                (Math.Abs(aServ.ServOdn.RsumTarif) < 0.001m) &&
                (Math.Abs(aServ.Serv.Reval + aServ.Serv.RealCharge) < 0.001m) &&
                (Math.Abs(lsNorm != null ? lsNorm.Volume : 0) < 0.001m)
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
        public override void FinalPass(STCLINE.KP50.Interfaces.Faktura finder)
        {
            base.FinalPass(finder);

            //if (Charge.SummaryServ.Serv.SumCharge < 0) Charge.SummaryServ.Serv.SumCharge = 0;
            SumTicket = Charge.SummaryServ.Serv.SumOutsaldo;

            if (finder.newSumOpl > 0.001m)
                SumTicket = finder.newSumOpl;
        }

        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <returns></returns>
        protected override bool FillAdr()
        {
            if (Dr == null) return false;
            Dr["fio"] = Kvar.PayerFio;
            Dr["ulica"] = (Rajon.Trim() == "" || Rajon.Trim() == "-" ? string.Empty : Rajon + ", ") + 
                (Ulica.Trim() == "" || Ulica.Trim() == "-" ? string.Empty : Ulica + ", ");
            Dr["ndom"] = "д. " + NumberDom;
            if (NumberFlat == "-" || NumberFlat.Trim() == "" || NumberFlat.Trim() == "0")
            {
                Dr["nkvar"] = "";
            }
            else
            {
                Dr["nkvar"] = "кв. " + NumberFlat;
            }
            Dr["nkvar_n"] = (NumberRoom.Trim() == "-" || NumberRoom.Trim() == "" || NumberRoom.Trim() == "0") ? "" : "/ ком. " + NumberRoom;

            Dr["ob_s"] = Kvar.FullSquare.ToString("0.00");
            Dr["rooms"] = Kvar.Rooms > 1 ? Kvar.Rooms.ToString() : "-";
            Dr["gil_count"] = Kvar.CountGil;
            return true;
        }



        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="index"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        protected bool FillServiceVolumeLs(int index, int nzpServ, decimal calc, decimal odn)
        {
            ServVolume2 lsNorm = ServVolumeLs.GetServVolume(nzpServ);
            if (lsNorm != null)
            {
                //Dr["serv_volume" + index] = lsNorm.Rashod != 0 ? lsNorm.Odn < 0 ? (lsNorm.Rashod - lsNorm.Odn).ToString("0.00###") : lsNorm.Rashod.ToString("0.00###") : "";
                //Dr["serv_volume" + index] = lsNorm.Rashod != 0 ? lsNorm.Rashod.ToString("0.00###") : "";
                Dr["serv_volume_odn" + index] = lsNorm.Odn != 0 ? lsNorm.Odn.ToString("0.00###") : "";
            }
            else
            {
                Dr["serv_volume" + index] = calc != 0 ? calc.ToString("0.00###") : "";
                Dr["serv_volume_odn" + index] = odn != 0 ? odn.ToString("0.00###") : "";
            }

            return true;
        }


        protected override bool FillRemark()
        {
            //Dr["remark"] = Area.AreaId != 1 && Area.AreaId != 58
             //   ? Area.AreaRemark
              //  : string.Empty;
            return true;
        }

        /// <summary>
        /// Заполнение реквизитов территории
        /// </summary>
        /// <returns></returns>
        public override bool FillAreaData()
        {
           // Dr["area"] = Area.AreaId != 1 && Area.AreaId != 58 ? Area.AreaName : string.Empty;
            return true;
        }

        /// <summary>
        /// Отображение особых услуг в заголовке таблицы
        /// </summary>
        /// <param name="nzpServ"></param>
        /// <param name="index"></param>
        /// <param name="defaultName"></param>
        protected bool ShowSpecServ(int nzpServ, int index, string defaultName)
        {
            //Dr["serv_name" + index] = defaultName;
            if (Charge.ListServ.ContainsKey(nzpServ))
            {
                SumServ2 t = Charge.ListServ[nzpServ].Serv;
                Dr["serv_name" + index] = t.NameServ;
                Dr["serv_measure" + index] = "кв.м.";
                Dr["serv_sum_tarif" + index] = t.SumTarif.ToString("0.00");
                Dr["serv_tarif" + index] = t.Tarif.ToString("0.00");
                Dr["serv_reval" + index] = (t.Reval + t.RealCharge).ToString("0.00");
                Dr["serv_sum_charge" + index] = t.SumCharge.ToString("0.00");
                return true;
            }
            return false;
        }

        protected void ShowSimpleServ(string fieldName, int index, decimal sum,
             string defValue)
        {
            Dr[fieldName + index] = Math.Abs(sum) > 0.001m ? sum.ToString("0.00") : defValue;
        }



        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <returns></returns>
        protected override bool FillMainChargeGrid()
        {
            if (Dr == null) return false;
            ServVolumeDom.GetServNormativ(Pref, NzpDom);
            ServVolumeLs.GetLsServNormativ(Pref, NzpKvar, false);
            SpravInf.Pref = Pref;
            Charge.ListServ = SpravInf.ServRename(Charge.ListServ, NzpDom);

            int numberString = 1;

            IOrderedEnumerable<BaseServ2> values = from value in Charge.ListServ.Values
                                                   orderby value.Serv.Ordering ascending
                                                   select value;

            foreach (var bs in values)
            {
                if (numberString > 9) break;
                //BaseServ2 bs = t.Value;
                //if ((IsShowServInGrid(bs)) & (!specServ.Contains(bs.Serv.NzpServ)))
                if (IsShowServInGrid(bs))
                {

                    Dr["serv_name" + numberString] = GetSpecServName(bs.Serv.NzpServ, bs.Serv.NameServ.Trim());
                    Dr["serv_measure" + numberString] = bs.Serv.Measure.Trim();
                    ShowSimpleServ("serv_tarif", numberString, bs.Serv.Tarif, "");
                        //(bs.SlaveServ.Count > 1 || bs.ServOdn.RsumTarif != 0) ? bs.Serv.Tarif/2 : bs.Serv.Tarif, "");
                    if (_kommServ.Contains(bs.Serv.NzpServ))
                    {
                        ShowSimpleServ("serv_payment", numberString, bs.Serv.RsumTarif - bs.ServOdn.RsumTarif, "");
                        ShowSimpleServ("serv_payment_odn", numberString, bs.ServOdn.RsumTarif, "");
                    }
                    else
                    {
                        ShowSimpleServ("serv_payment", numberString, 0, "x");
                        ShowSimpleServ("serv_payment_odn", numberString, 0, "x");
                    }

                    ShowSimpleServ("serv_sum_tarif", numberString, bs.Serv.SumTarif, "");
                    ShowSimpleServ("serv_reval", numberString, bs.Serv.Reval + bs.Serv.RealCharge, "");
                    ShowSimpleServ("serv_sum_charge", numberString, bs.Serv.RsumTarif + bs.Serv.Reval + bs.Serv.RealCharge, "");

                    if (_kommServ.Contains(bs.Serv.NzpServ))
                    {
                        //if (!_odnServ.Contains(bs.Serv.NzpServ))
                        FillServiceVolumeLs(numberString, bs.Serv.NzpServ,
                            (bs.Serv.Tarif != 0 ? (bs.Serv.RsumTarif/bs.Serv.Tarif) : bs.Serv.CCalc),
                            (bs.ServOdn.Tarif != 0 ? (bs.ServOdn.RsumTarif/bs.ServOdn.Tarif) : bs.ServOdn.CCalc));
                    }
                    else
                    {
                        ShowSimpleServ("serv_volume", numberString, 0, "x");
                        ShowSimpleServ("serv_volume_odn", numberString, 0, "x");
                    }


                    numberString++;
                }
                else
                {
                    Charge.SummaryServ.Serv.RsumTarif -= bs.Serv.RsumTarif;
                    Charge.SummaryServ.Serv.Reval -= bs.Serv.Reval;
                    Charge.SummaryServ.Serv.RealCharge -= bs.Serv.RealCharge;
                    Charge.SummaryServ.Serv.SumCharge -= bs.Serv.SumCharge;
                }

            }
            return true;
        }

        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("reciever", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("ndom", typeof(string));
            table.Columns.Add("nkvar", typeof(string));
            table.Columns.Add("nkvar_n", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("phone", typeof(string));
            table.Columns.Add("ob_s", typeof(string));
            table.Columns.Add("rooms", typeof(string));
            table.Columns.Add("gil_count", typeof(string));
            table.Columns.Add("area", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("avans", typeof(string));
            table.Columns.Add("dat_opl", typeof(string));
            table.Columns.Add("sum_last_opl", typeof(string));
            table.Columns.Add("serv_sum_tarif", typeof(string));
            table.Columns.Add("serv_reval", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("remark2", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("sprav_last_opl", typeof(string));
            table.Columns.Add("avans_string", typeof(string));
            table.Columns.Add("sum_outsaldo", typeof(string));
            table.Columns.Add("sum_outsaldo2", typeof(string));

            for (int i = 1; i <= MaxCountersCount; i++)
            {
                table.Columns.Add("counter_name" + i, typeof(string));
                table.Columns.Add("counter_prev_value" + i, typeof(string));
                table.Columns.Add("counter_first_value" + i, typeof(string));
                table.Columns.Add("counter_max_value" + i, typeof(string));
            }

            for (int i = 1; i <= MaxServCount; i++)
            {
                table.Columns.Add("serv_name" + i, typeof(string));
                table.Columns.Add("serv_measure" + i, typeof(string));
                table.Columns.Add("serv_volume" + i, typeof(string));
                table.Columns.Add("serv_volume_odn" + i, typeof(string));
                table.Columns.Add("serv_tarif" + i, typeof(string));
                table.Columns.Add("serv_payment" + i, typeof(string));
                table.Columns.Add("serv_payment_odn" + i, typeof(string));
                table.Columns.Add("serv_sum_tarif" + i, typeof(string));
                table.Columns.Add("serv_reval" + i, typeof(string));
                table.Columns.Add("serv_sum_charge" + i, typeof(string));

            }

            for (int i = 1; i <= MaxSpravCount; i++)
            {
                table.Columns.Add("sprav_serv_name" + i, typeof(string));
                table.Columns.Add("sprav_type" + i, typeof(string));
                table.Columns.Add("sprav_norm" + i, typeof(string));
                table.Columns.Add("sprav_norm_odn" + i, typeof(string));
                table.Columns.Add("sprav_volume_odpu" + i, typeof(string));
                table.Columns.Add("sprav_volume_inside" + i, typeof(string));
                table.Columns.Add("sprav_volume_odn" + i, typeof(string));
                table.Columns.Add("sprav_ob_s" + i, typeof(string));

            }

            for (int i = 1; i <= MaxRevalCount; i++)
            {
                table.Columns.Add("reval_period" + i, typeof(string));
                table.Columns.Add("reval_value" + i, typeof(string));
                table.Columns.Add("reval_reason" + i, typeof(string));
            }

            return table;
        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <returns></returns>
        protected override bool FillSummuryBill()
        {
            if (Dr == null) return false;
            BaseServ2 summaryServ = Charge.SummaryServ;
            Dr["serv_sum_tarif"] = summaryServ.Serv.SumTarif.ToString("0.00");
            Dr["serv_reval"] = (summaryServ.Serv.Reval + summaryServ.Serv.RealCharge).ToString("0.00");
            Dr["sum_charge_all"] = (summaryServ.Serv.RsumTarif + summaryServ.Serv.Reval + summaryServ.Serv.RealCharge).ToString("0.00");
            Dr["sum_charge"] = (summaryServ.Serv.SumCharge - summaryServ.ServOdn.SumCharge).ToString("0.00");
            Dr["sum_outsaldo"] = summaryServ.Serv.SumOutsaldo.ToString("0.00");
            Dr["sum_outsaldo2"] = summaryServ.Serv.SumOutsaldo > 0 ? summaryServ.Serv.SumOutsaldo.ToString("0.00") : "0.00";
            if (summaryServ.Serv.SumInsaldo - summaryServ.Serv.SumMoney == 0)
            {
                Dr["avans"] = "0.00";
                Dr["avans_string"] =
                       "Задолженность на начало расчетного периода с учетом платежей в расчетном периоде";
            }
            else
            {
                Dr["avans"] = Math.Abs(summaryServ.Serv.SumInsaldo - summaryServ.Serv.SumMoney).ToString("0.00");
                if (summaryServ.Serv.SumInsaldo - summaryServ.Serv.SumMoney > 0)
                    Dr["avans_string"] =
                        "Задолженность на начало расчетного периода с учетом платежей в расчетном периоде";
                else
                    Dr["avans_string"] = "Аванс на начало расчетного периода с учетом платежей в расчетном периоде";
            }
            return true;
        }


        /// <summary>
        /// дата последней оплаты по счету
        /// </summary>
        /// <returns></returns>
        protected override bool FillDatOpl()
        {
            if (Dr == null) return false;
            Payments.SetPayment(Pref, NzpKvar);
            if (Payments.DateOplat != "")
            {
                Dr["dat_opl"] = Payments.DateOplat + "г.";
                Dr["sum_last_opl"] = Payments.LastSumOplat;
                Dr["sprav_last_opl"] =
                    "Справочно: последняя оплата - " +
                    Payments.DateOplat + "г." + " на сумму " +
                    Payments.LastSumOplat + "руб.";
            }
            return true;
        }


        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <returns></returns>
        protected override bool FillRevalReason()
        {
            if (Dr == null) return false;
            Reval.LoadRevalReason(Pref, NzpKvar);
            int rowNumber = 1;
            foreach (var bs in Charge.ListServ)
            {
                if (bs.Value.Serv.Reval != 0)
                {
                    FakturaReval fr = Reval.ListReval.Find(x => x.NzpServ == bs.Value.Serv.NzpServ);
                    if ((rowNumber < MaxRevalCount) & (fr != null))
                    {
                        Dr["reval_reason" + rowNumber] = fr.Reason;
                        Dr["reval_period" + rowNumber] = fr.ReasonPeriod;
                        Dr["reval_value" + rowNumber] = bs.Value.Serv.Reval;
                    }
                    rowNumber++;
                }

            }

            return true;
        }

        /// <summary>
        /// Заполнение банковских реквизитов и реквизитов счета
        /// </summary>
        /// <returns></returns>
        protected override bool FillRekvizit()
        {
            if (Dr == null) return false;
            _Rekvizit rekvizit = Rekvizits.GetRekvizit(NzpArea, NzpGeu, Pref);
            Dr["reciever"] = rekvizit.ercName;
            Dr["months"] = FullMonthName + "г.";
            Dr["phone"] = rekvizit.phone ?? "45-25-33";
            Dr["remark2"] = rekvizit.remark != ""
                ? rekvizit.remark
                : @"Адрес: г.Йошкар-Ола, Ленинский проспект, д.25; ИНН 1215059278 КПП 121501001; тел.42-17-87
                    р/с 4070381083780100244 в Отделении №8614 Сбербанка России г.Йошкар-Ола, к/с30101810300000000630, БИК 048860630";

            return true;
        }


        /// <summary>
        /// Возвращает имя спец услуги
        /// </summary>
        /// <returns></returns>
        private string GetSpecServName(int nzpServ, string baseServName)
        {
            if (nzpServ == 9 || nzpServ == 14 || nzpServ == 513 || nzpServ == 514)
            {
                //Dictionary<int, string> specServ = Dom.GetServicesSpecialName();
                //if (specServ.ContainsKey(nzpServ)) 
                    //return specServ[nzpServ];
                return "";
            }
            return baseServName;
        }


        /// <summary>
        /// Заполнение счетчиков
        /// </summary>
        /// <returns></returns>
        public override bool FillCounters()
        {
            int countersIndex = 1;
         
            //var listGroupCounters = Counters.LoadGroupCounters(Pref, NzpKvar, Pkod);

            int j = 0;
            //while (j < listGroupCounters.Count && countersIndex <= MaxCountersCount)
            //{
            //    Dr["counter_name" + countersIndex] = listGroupCounters[j].NumCounter;
            //    Dr["counter_prev_value" + countersIndex] = listGroupCounters[j].Value != 0 ? listGroupCounters[j].Value.ToString("0.00") : "X";
            //    Dr["counter_first_value" + countersIndex] = listGroupCounters[j].ValuePred != 0 ? listGroupCounters[j].ValuePred.ToString("0.00") : "X";
            //    Dr["counter_max_value" + countersIndex] = Math.Max(listGroupCounters[j].Value, listGroupCounters[j].ValuePred).ToString("0.00");
            //    countersIndex++;
            //    j++;
            //}

            Counters.LoadChosenMonthCounters(Pref, NzpKvar, Pkod);
            var sortedCounters = (from counter in Counters.ListCounters
                                  orderby counter.NumCounter
                                  select counter).ToList();
            int i = 0;
            while (i < sortedCounters.Count && countersIndex <= MaxCountersCount)
            {
                //Dr["counter_name" + countersIndex] = CounterName(Counters.ListCounters[i].NumCounter, Counters.ListCounters[i].ServiceSmall.Trim());
                Dr["counter_name" + countersIndex] = sortedCounters[i].NumCounter;
                Dr["counter_prev_value" + countersIndex] = sortedCounters[i].Value != 0 ? sortedCounters[i].Value.ToString("0.00") : "X";
                Dr["counter_first_value" + countersIndex] = sortedCounters[i].ValuePred != 0 ? sortedCounters[i].ValuePred.ToString("0.00") : "X";
                Dr["counter_max_value" + countersIndex] = Math.Max(sortedCounters[i].Value, sortedCounters[i].ValuePred).ToString("0.00");
                countersIndex++;
                i++;
            }

            return true;
        }

        /// <summary>
        /// Заполнение дополнительной информации
        /// </summary>
        /// <returns></returns>
        protected override bool FillSpravInf()
        {
            SpravInf.Pref = Pref;
            var sprav = SpravInf.GetSpravInformation(NzpDom);
            var index = 1;
            foreach (var sp in sprav)
            {
                if (_kommServ.Contains(sp._nzp_serv) && sp._norm_indiv != 0 && index < 9) 
                    //&& sp._nzp_serv == 8)//Костыльвания
                {
                    Dr["sprav_serv_name" + index] = sp._serv;
                    //Dr["sprav_type" + index] = sp._type != 0 ? sp._type : 1;
                    Dr["sprav_type" + index] = sp._type;
                    Dr["sprav_norm" + index] = sp._norm_indiv != 0 ? sp._norm_indiv.ToString("0.00###") : "";
                    Dr["sprav_norm_odn" + index] = sp._norm_od != 0 ? sp._norm_od.ToString("0.00###") : "";
                    Dr["sprav_volume_odpu" + index] = sp._volume_odpu != 0 ? sp._volume_odpu.ToString("0.00###") : "";
                    Dr["sprav_volume_inside" + index] = sp._volume_in != 0 ? sp._volume_in.ToString("0.00###") : "";
                    Dr["sprav_volume_odn" + index] = sp._volume_odn != 0 ? sp._volume_odn.ToString("0.00###") : "";
                    Dr["sprav_ob_s" + index] = sp._square != 0 ? sp._square.ToString("0.00##") : "";
                    index++;
                }
            }
            return true;
        }
    }
}
