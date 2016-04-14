
using System;
using System.Data;
using System.Globalization;
using Bars.KP50.DB.Faktura;
using STCLINE.KP50.Interfaces;
using Bars.KP50.Faktura.Source.Base;

namespace Bars.KP50.Faktura.Source.FAKTURA
{
    public class AstrahanNewFaktura : BaseFactura2
    {
        private const int MaxRevalNumber = 9;
        private const int MaxCountersCount = 8;
        private const int MaxServCount = 20;

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

            //barcode 2


            Shtrih = vars + BarcodeCrc(vars);
            return Shtrih;
        }

        public override DataTable MakeTable()
        {

            var table = new DataTable {TableName = "Q_master"};
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("adress", typeof(string));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("date_print", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("poluch", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("datedestv", typeof(string));
            table.Columns.Add("kommItogIndex", typeof(string));
            table.Columns.Add("gilItogIndex", typeof(string));

                        
            table.Columns.Add("type_pl", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));            
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("pl_mop", typeof(string));
            table.Columns.Add("dom_gil", typeof(string));
            table.Columns.Add("indecs", typeof(string));   
            table.Columns.Add("month_", typeof(string));
            table.Columns.Add("year_", typeof(string));
            table.Columns.Add("reval", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_dolg", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("geu_remark", typeof(string));
            table.Columns.Add("area_remark", typeof(string));

           


            for (int i = 1; i < 16; i++)
            {
                table.Columns.Add("st_" + i, typeof(string));
            }

            table.Columns.Add("sum_insaldo", typeof(string));
            table.Columns.Add("sum_ito", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_outsaldo", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("rsum_tarif_odn", typeof(string));
            table.Columns.Add("sum_nedop", typeof(string));
            table.Columns.Add("vars", typeof(string));


            for (int i = 1; i < MaxServCount; i++)
            {
                table.Columns.Add("name_serv" + i, typeof(string));
                table.Columns.Add("measure" + i, typeof(string));
                table.Columns.Add("tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif_odn" + i, typeof(string));
                table.Columns.Add("c_calc" + i, typeof(string));
                table.Columns.Add("c_calc_odn" + i, typeof(string));
                table.Columns.Add("reval_charge" + i, typeof(string));
                table.Columns.Add("sum_nedop" + i, typeof(string));
                table.Columns.Add("sum_ito" + i, typeof(string));
                table.Columns.Add("name_supp" + i, typeof(string));
                table.Columns.Add("supp_rekv" + i, typeof(string));
                table.Columns.Add("sum_money" + i, typeof(string));
                table.Columns.Add("day_nedop" + i, typeof(string));
                table.Columns.Add("reval" + i, typeof(string));
                table.Columns.Add("sum_dolg" + i, typeof(string));
                table.Columns.Add("sum_charge_all" + i, typeof(string));
                table.Columns.Add("sum_charge" + i, typeof(string));
                
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
                table.Columns.Add("lsnumcnt" + i, typeof(string));
                table.Columns.Add("lsdatuchet1_" + i, typeof(string));
                table.Columns.Add("lsdatuchet2_" + i, typeof(string));
                table.Columns.Add("lsvalcnt1_" + i, typeof(string));
                table.Columns.Add("lsvalcnt2_" + i, typeof(string));
                table.Columns.Add("datuchet2_" + i, typeof(string));
                table.Columns.Add("domdatProv_" + i, typeof(string));
                table.Columns.Add("lsdatProv_" + i, typeof(string));

            }

            for (int i = 1; i < MaxCountersCount; i++)
            {
                table.Columns.Add("domrash_service" + i, typeof(string));
                table.Columns.Add("domrash_measure" + i, typeof(string));
                table.Columns.Add("domrash_domVolume" + i, typeof(string));
                table.Columns.Add("domrash_odnDomVolume" + i, typeof(string));
                table.Columns.Add("domrash_domArendatorsVolume" + i, typeof(string));
            }

            for (int i = 1; i < MaxRevalNumber; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
            }

            table.Columns.Add("vars2", typeof(string));

            return table;
        }



        /// <summary>
        /// Заполнение квартирных параметров
        /// </summary>
        /// <returns></returns>
        protected override bool FillKvarPrm()
        {
            if (Dr == null) return false;

            Dr["kolgil"] = Kvar.CountGil;
            Dr["kv_pl"] = Kvar.FullSquare.ToString("0.00");
            return true;
        }

        /// <summary>
        /// Заполнение реквизитов счета
        /// </summary>
        /// <returns></returns>
        protected override bool FillRekvizit()
        {
            if (Dr == null) return false;
            _Rekvizit rekvizit = Rekvizits.GetRekvizit(NzpArea, NzpGeu, Pref);
            Dr["st_1"] = "Получатель " + rekvizit.poluch + " р/с " +
                         rekvizit.rschet + " в " + rekvizit.bank + " к/с " +
                         rekvizit.korr_schet + " ИНН/КПП " + rekvizit.inn + " БИК " + rekvizit.bik + " " +
                         rekvizit.adres;

            Dr["months"] = FullMonthName;
            Dr["poluch"] = rekvizit.poluch;
            Dr["date_print"] = DateTime.Now.ToShortDateString();
            Dr["datedestv"] =
                new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 01).AddMonths(2)
                    .AddDays(-1)
                    .ToString("dd.MM.yyyy"); //DateTime.Now.ToString("28.MM.yyyy");

            Dr["month_"] = Month;
            Dr["year_"] = Year;
            return true;
        }

        /// <summary>
        /// Заполнение одной строки в таблице начислений
        /// </summary>
        
        /// <param name="stringIndex">Номер строки</param>
        /// <param name="bs">Услуга</param>
        /// <param name="numSt"></param>
        /// <returns></returns>
        protected override bool FillOneRowInChargeGrid(int stringIndex, BaseServ2 bs, string numSt)
        {
            if (bs == null)
            {
                Dr["name_supp" + stringIndex] = "";
                Dr["supp_rekv" + stringIndex] = "";
                Dr["name_serv" + stringIndex] = "";
                Dr["sum_money" + stringIndex] = "";
                Dr["measure" + stringIndex] = "";
                Dr["tarif" + stringIndex] = "";
                Dr["c_calc" + stringIndex] = "";
                Dr["c_calc_odn" + stringIndex] = "";
                Dr["rsum_tarif" + stringIndex] = "";
                Dr["rsum_tarif_odn" + stringIndex] = "";
                Dr["sum_nedop" + stringIndex] = "";
                Dr["day_nedop" + stringIndex] = "";
                Dr["reval" + stringIndex] = "";
                Dr["sum_dolg" + stringIndex] = "";
                Dr["sum_charge_all" + stringIndex] = "";
            }
            else
            {
                Dr["name_supp" + stringIndex] = bs.Serv.Payer.payer;
                Dr["supp_rekv" + stringIndex] = bs.Serv.Payer.bank+ " "+bs.Serv.Payer.Rcount;
                Dr["name_serv" + stringIndex] = bs.Serv.NameServ;
                Dr["sum_money" + stringIndex] = bs.Serv.SumMoney.ToString("0.00");
                Dr["measure" + stringIndex] = bs.Serv.Measure;
                Dr["tarif" + stringIndex] = bs.Serv.Tarif.ToString("0.00");
                Dr["c_calc" + stringIndex] = bs.Serv.CCalc.ToString("0.00");
                Dr["c_calc_odn" + stringIndex] = bs.ServOdn.CCalc.ToString("0.00");                
                Dr["rsum_tarif" + stringIndex] = bs.Serv.RsumTarif.ToString("0.00");
                Dr["rsum_tarif_odn" + stringIndex] = bs.ServOdn.RsumTarif.ToString("0.00");
                Dr["sum_nedop" + stringIndex] = bs.Serv.SumNedop.ToString("0.00");
                Dr["day_nedop" + stringIndex] = (DateTime.DaysInMonth(Year,Month) - bs.Serv.COkaz).ToString(CultureInfo.InvariantCulture);
                Dr["reval" + stringIndex] = (bs.Serv.Reval + bs.Serv.RealCharge).ToString("0.00");
                Dr["sum_dolg" + stringIndex] = (bs.Serv.SumInsaldo - bs.Serv.SumMoney).ToString("0.00");
                Dr["sum_charge_all" + stringIndex] = bs.Serv.SumCharge.ToString("0.00");               
            }
            return true;
        }

        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <returns></returns>
        protected override bool FillMainChargeGrid()
        {
            if (Dr == null) return false;
            int stIndex = 1;
            
            for (int countServ = 0; countServ < Charge.ListServ.Count; countServ++)
            {
                if (IsShowServInGrid(Charge.ListServ[countServ]))
                {

                    FillOneRowInChargeGrid(stIndex, Charge.ListServ[countServ],"");
                    stIndex++;
                }
            }

                      
            return true;
        }

        /// <summary>
        /// Заполнение счетчиков
        /// </summary>
        /// <returns></returns>
        public override bool FillCounters()
        {
            int countersIndex = 1;
            for (int i = 0; i < Math.Min(MaxCountersCount, Counters.ListCounters.Count); i++)
            {
                switch (Counters.ListCounters[i].NzpServ)
                {
                    case 6: Dr["lsserv" + countersIndex] = countersIndex + ". Хол. вода"; break;
                    case 9: Dr["lsserv" + countersIndex] = countersIndex + ". Гор. вода"; break;
                    case 8: Dr["lsserv" + countersIndex] = countersIndex + ". Отопл."; break;
                    case 25: Dr["lsserv" + countersIndex] = countersIndex + ". Эл.снаб."; break;
                    case 210: Dr["lsserv" + countersIndex] = countersIndex + ". Ноч.Эл.."; break;
                    default: Dr["lsserv" + countersIndex] = countersIndex + ". " + Counters.ListCounters[i].ServiceName; break;
                }
                Dr["lsnumcnt" + countersIndex] = Counters.ListCounters[i].NumCounter;
                if (Counters.ListCounters[i].DatUchetPred.ToShortDateString() == "01.01.1900")
                    Dr["lsdatuchet1_" + countersIndex] = "";
                else
                    Dr["lsdatuchet1_" + countersIndex] = Counters.ListCounters[i].DatUchetPred.ToShortDateString();
                Dr["lsdatuchet2_" + countersIndex] = Counters.ListCounters[i].DatUchet.ToShortDateString();
                Dr["lsvalcnt1_" + countersIndex] = Counters.ListCounters[i].ValuePred;
                Dr["lsvalcnt2_" + countersIndex] = Counters.ListCounters[i].Value;
                Dr["lsdatProv_" + countersIndex] = Counters.ListCounters[i].DatProv;
                countersIndex++;
            }


            Dr["vars2"] = Pkod;

            return true;
        }

        /// <summary>
        /// Заполнение домовых счетчиков
        /// </summary>
        /// <returns></returns>
        public override bool FillDomCounters()
        {
            int countersIndex = 1;
            for (int i = 0; i < Math.Min(MaxCountersCount, Counters.ListDomCounters.Count); i++)
            {
                switch (Counters.ListDomCounters[i].NzpServ)
                {
                    case 6: Dr["domserv" + countersIndex] = countersIndex + ". Хол. вода"; break;
                    case 9: Dr["domserv" + countersIndex] = countersIndex + ". Гор. вода"; break;
                    case 8: Dr["domserv" + countersIndex] = countersIndex + ". Отопл."; break;
                    case 25: Dr["domserv" + countersIndex] = countersIndex + ". Эл.снаб."; break;
                    case 210: Dr["domserv" + countersIndex] = countersIndex + ". Ноч.Эл.."; break;
                    default: Dr["domserv" + countersIndex] = countersIndex + ". " + Counters.ListDomCounters[i].ServiceName; break;
                }

                Dr["domserv" + countersIndex] = Counters.ListDomCounters[i].ServiceName;
                Dr["domnumcnt" + countersIndex] = Counters.ListDomCounters[i].NumCounter;
                if (Counters.ListDomCounters[i].DatUchetPred > DateTime.Now.AddYears(-10))
                    Dr["domdatuchet1_" + countersIndex] = Counters.ListDomCounters[i].DatUchetPred.ToShortDateString();
                if (Counters.ListDomCounters[i].DatUchet > DateTime.Now.AddYears(-10))
                    Dr["domdatuchet2_" + countersIndex] = Counters.ListDomCounters[i].DatUchet.ToShortDateString();
                Dr["domvalcnt1_" + countersIndex] = Counters.ListDomCounters[i].ValuePred;
                Dr["domvalcnt2_" + countersIndex] = Counters.ListDomCounters[i].Value;
                Dr["domdatProv_" + countersIndex] = Counters.ListDomCounters[i].DatProv;
                countersIndex++;
            }

            int rashIndex = 0;
            ServVolumeDom.GetServNormativ(Pref, NzpDom);
            for (int i = 1; i <ServVolumeDom.ListDomNormativ.Count; i++)
            {
                Dr["domrash_service" + rashIndex] = Charge.GetServName(ServVolumeDom.ListDomNormativ[i].NzpServ);
                string measure = ""; 
                switch (i)
                {
                    case 1: { measure = "кВт*ч"; break; }
                    case 2: { measure = "кВт*ч"; break; }
                    case 3: { measure = "куб.м"; break; }
                    case 4: { measure = "куб.м"; break; }
                    case 5: { measure = "с чел.в мес"; break; }
                    case 6: { measure = "кв.метр"; break; }
                }

                Dr["domrash_measure" + rashIndex] = measure;
                Dr["domrash_domVolume" + rashIndex] = ServVolumeDom.ListDomNormativ[i].Volume;
                Dr["domrash_odnDomVolume" + rashIndex] = ServVolumeDom.ListDomNormativ[i].Odn;
                //dr["domrash_domArendatorsVolume" + rashIndex] = servVolumeDom.ListDomNormativ[i].;


                rashIndex++;
            }
                return true;
        }

        /// <summary>
        /// Заполнение строки Адреса
        /// </summary>
        /// <returns></returns>
        protected override bool FillAdr()
        {
            if (Dr == null) return false;
            Dr["fio"] = Kvar.PayerFio;
            Dr["adress"] = Ulica + " д. " + NumberDom + " кв." + NumberFlat;
            if (Pkod.IndexOf(".", StringComparison.Ordinal) > -1)
                Dr["pkod"] = Pkod.Substring(0, Pkod.IndexOf(".", StringComparison.Ordinal));
            else
                Dr["pkod"] = "45";

            Dr["kolgil2"] = Kvar.CountRegisterGil;
            Dr["ulica"] = Ulica;
            Dr["numdom"] = NumberDom;
            Dr["kvnum"] = NumberFlat;
            Dr["dom_gil"] = Dom.CountDomGil;
            Dr["pl_dom"] = Dom.DomSquare.ToString("0.00");
            Dr["pl_mop"] = Dom.MopSquare.ToString("0.00"); 
            Dr["type_pl"] = Kvar.IsolateFlat ? "общая" : "жилая";
            Dr["indecs"] = Dom.Indecs;
           
            return true;
        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <returns></returns>
        protected override bool FillSummuryBill()
        {
            if (Dr == null) return false;
            var summaryServ = Charge.SummaryServ;
            Dr["rsum_tarif"] = summaryServ.Serv.SumTarif.ToString("0.00");
            Dr["reval_charge"] = (summaryServ.Serv.Reval + summaryServ.Serv.RealCharge).ToString("0.00");
            Dr["rsum_tarif_odn"] = summaryServ.ServOdn.RsumTarif.ToString("0.00");
            Dr["sum_nedop"] = summaryServ.Serv.SumNedop.ToString("0.00");
            Dr["sum_ito"] = (summaryServ.Serv.RsumTarif + summaryServ.Serv.Reval+
            summaryServ.Serv.RealCharge - summaryServ.Serv.SumNedop).ToString("0.00");
            Dr["sum_insaldo"] = summaryServ.Serv.SumInsaldo.ToString("0.00");
            Dr["sum_ticket"] = SumTicket.ToString("0.00");
            Dr["sum_outsaldo"] = summaryServ.Serv.SumOutsaldo.ToString("0.00");
            Dr["sum_money"] = summaryServ.Serv.SumMoney.ToString("0.00");

            Dr["sum_charge"] = summaryServ.Serv.SumCharge.ToString("0.00");
            Dr["reval"] = (summaryServ.Serv.Reval+summaryServ.Serv.RealCharge).ToString("0.00");
            Dr["sum_charge_all"] = summaryServ.Serv.SumCharge.ToString("0.00");
            Dr["sum_dolg"] = (summaryServ.Serv.SumInsaldo - summaryServ.Serv.SumMoney).ToString("0.00");
            return true;
        }

        ///// <summary>
        ///// Подсчет контрольной суммы в штрих-коде
        ///// </summary>
        ///// <param name="acode">Штрих-код</param>
        ///// <returns>Контрольная цифра</returns>
        //public string BarcodeCrc(string acode)
        //{
        //    int sum_ = 0;


        //    for (int i = 0; i < acode.Length; i++)
        //    {
        //        if (i != 28)
        //        {
        //            if ((i % 2) == 1)
        //            {
        //                sum_ = sum_ + System.Convert.ToInt16(acode.Substring(i, 1));
        //            }
        //            else sum_ = sum_ + 3 * System.Convert.ToInt16(acode.Substring(i, 1));
        //        }
        //    }

        //    String s = ((10 - sum_ % 10) % 10).ToString();

        //    return s.Substring(0, 1);

        //}

        ///// <summary>
        ///// Формирование штрих-кода
        ///// </summary>
        ///// <returns>Готовый штрих-код</returns>
        //public override string GetBarCode()
        //{
        //    if (pkod.Length > 10)
        //        pkod = pkod.Substring(0, 10);
        //    else
        //        pkod = ("0000000000").Substring(0, 10 - pkod.Length) + pkod;
            
        //    string vars = "33" + pkod + month.ToString("D2") +
        //     (year - 2000).ToString("D2") +"0000"+
        //     (System.Math.Max(0, sumTicket) * 100).ToString("0000000");
        //    return vars + BarcodeCrc(vars) + BarcodeCrc(vars);
        //}

        /// <summary>
        /// Отображать ли услугу в таблице начислений
        /// </summary>
        /// <param name="aServ">Услуга</param>
        /// <returns>Истина, если отображать</returns>
        public bool IsShowServInGrid(BaseServ2 aServ)
        {
            if ((Math.Abs(aServ.Serv.Tarif) < 0.001m) &
                (Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (Math.Abs(aServ.Serv.SumNedop) < 0.001m) &
                (Math.Abs(aServ.Serv.SumInsaldo) < 0.001m) &
                (Math.Abs(aServ.Serv.SumMoney) < 0.001m)
            )
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Формирование суммы к оплате в счете
        /// </summary>
        /// <param name="finder"></param>
        public override void FinalPass(STCLINE.KP50.Interfaces.Faktura finder)
        {
            SumTicket = Charge.SummaryServ.Serv.SumOutsaldo > 0.001m 
                ? Charge.SummaryServ.Serv.SumCharge 
                : 0;
        }

       }
}
