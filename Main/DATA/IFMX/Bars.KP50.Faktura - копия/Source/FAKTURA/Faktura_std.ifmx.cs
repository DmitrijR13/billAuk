
namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Xml.Linq;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using FastReport;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;



    public class StandartFaktura : BaseFactura
    {
 
        public override DataTable MakeTable()
        {
            DataTable table = new DataTable();
            table.TableName = "Q_master";
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("adress", typeof(string));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("date_print", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("poluch", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("datedestv", typeof(string));
            table.Columns.Add("kommItogIndex", typeof(string));
            table.Columns.Add("gilItogIndex", typeof(string));


            for (int i = 1; i<16; i++)
                table.Columns.Add("st_"+i, typeof(string));

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
            

            for (int i = 1; i < 30; i++)
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

            }

            for (int i = 1; i < 7; i++)
            {
                table.Columns.Add("serv_pere" + i.ToString(), typeof(string));
                table.Columns.Add("osn_pere" + i.ToString(), typeof(string));
                table.Columns.Add("sum_pere" + i.ToString(), typeof(string));
                table.Columns.Add("period_pere" + i.ToString(), typeof(string));
            }
            
           
           
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

            Month = System.DateTime.Now.Month;
            Year = System.DateTime.Now.Year;
            FullMonthName = "";
            Pkod = "";
            Geu = "";
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

        }

        /// <summary>
        /// Заполнение квартирных параметров
        /// </summary>
        /// <param name="dr">Строка таблицы</param>
        /// <returns></returns>
        protected override bool FillKvarPrm(DataRow dr)
        {
            if (dr == null) return false;
            
            dr["kolgil"] = CountGil;
            dr["kv_pl"] = FullSquare.ToString("0.00");

                return true;
        }

        /// <summary>
        /// Заполнение реквизитов счета
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillRekvizit(DataRow dr)
        {
            if (dr == null) return false;
            dr["st_1"] = "Получатель " + Rekvizit.poluch + " р/с " +
                Rekvizit.rschet + " в " +Rekvizit.bank + " к/с " +
                Rekvizit.korr_schet + " ИНН/КПП " + Rekvizit.inn + " БИК " + Rekvizit.bik+" "+
                Rekvizit.adres;
            dr["st_2"] = "";
            dr["st_3"] = "";
            dr["st_4"] = "";
            dr["st_5"] = "";
            dr["st_6"] = "";
            dr["st_7"] = "";
            dr["st_8"] = "";
            dr["st_9"] = "";
            dr["st_10"] = "";
            dr["st_11"] = "";
            dr["st_12"] = "";
            dr["st_13"] = "";
            dr["st_14"] = "";
            dr["st_15"] = "";
            dr["months"] = FullMonthName;
            dr["poluch"] = Rekvizit.poluch;
            dr["date_print"] = DateTime.Now.ToShortDateString();
            dr["datedestv"] = DateTime.Now.ToString("28.MM.yyyy");
            return true;
        }

        /// <summary>
        /// Заполнение одной строки в таблице начислений
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="stringIndex">Номер строки</param>
        /// <param name="bs">Услуга</param>
        /// <returns></returns>
        protected override bool FillOneRowInChargeGrid(DataRow dr, int stringIndex, BaseServ bs, string numSt)
        {
            if (bs == null)
            {
                dr["name_serv" + stringIndex] = "";
                dr["measure" + stringIndex] = "";
                dr["c_calc" + stringIndex] = "";
                dr["c_calc_odn" + stringIndex] = "";
                dr["tarif" + stringIndex] = "";
                dr["rsum_tarif" + stringIndex] = "";
                dr["rsum_tarif_odn" + stringIndex] = "";
                dr["reval_charge" + stringIndex] = "";
                dr["sum_nedop" + stringIndex] = "";
                dr["sum_ito" + stringIndex] = "";
            }
            else
            {
                dr["name_serv" + stringIndex] = bs.Serv.NameServ;
                dr["measure" + stringIndex] = bs.Serv.Measure;
                dr["c_calc" + stringIndex] = bs.Serv.CCalc.ToString("0.00");
                dr["c_calc_odn" + stringIndex] = bs.ServOdn.CCalc.ToString("0.00");
                dr["tarif" + stringIndex] = bs.Serv.Tarif.ToString("0.00");
                dr["rsum_tarif" + stringIndex] = bs.Serv.RsumTarif.ToString("0.00");
                dr["rsum_tarif_odn" + stringIndex] = bs.ServOdn.RsumTarif.ToString("0.00");
                dr["reval_charge" + stringIndex] = (bs.Serv.RealCharge + bs.Serv.Reval).ToString("0.00");
                dr["sum_nedop" + stringIndex] = bs.Serv.SumNedop.ToString("0.00");
                dr["sum_ito" + stringIndex] = (bs.Serv.RsumTarif -
                    bs.Serv.SumNedop + bs.Serv.Reval + bs.Serv.RealCharge).ToString("0.00");
            }
            return true;
        }

        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillMainChargeGrid(DataRow dr)
        {
            if (dr == null) return false;
            int stIndex = 1;
            decimal gilrsum_tarif = 0;
            decimal gilreval_charge= 0;
            decimal gilsum_nedop = 0;
            decimal gilrsum_tarif_odn = 0;

            decimal kommrsum_tarif = 0;
            decimal kommrsum_tarif_odn = 0;
            decimal kommreval_charge = 0;
            decimal kommsum_nedop = 0;
            bool hasKommItog = false;
            bool hasGilItog = false;
            

            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (IsShowServInGrid(ListServ[countServ]))
                {
                    if (ListServ[countServ].KommServ)
                    {
                        FillOneRowInChargeGrid(dr, stIndex, ListServ[countServ],"");

                        kommrsum_tarif = kommrsum_tarif + ListServ[countServ].Serv.RsumTarif;
                        kommreval_charge = kommreval_charge + ListServ[countServ].Serv.Reval +
                            ListServ[countServ].Serv.RealCharge;
                        kommsum_nedop = kommsum_nedop + ListServ[countServ].Serv.SumNedop;
                        kommrsum_tarif_odn = kommrsum_tarif_odn + ListServ[countServ].ServOdn.RsumTarif;
                        stIndex++;
                    }
                    else
                    {

                        if (hasKommItog == false)
                        {
                            dr["name_serv" + stIndex] = "<b>Итого по коммунальным услугам</b>";
                            dr["measure" + stIndex] = "";
                            dr["c_calc" + stIndex] = "";
                            dr["c_calc_odn" + stIndex] = "";
                            dr["tarif" + stIndex] = "";
                            dr["rsum_tarif" + stIndex] = "<b>" + kommrsum_tarif.ToString("0.00") + "</b>";
                            dr["rsum_tarif_odn" + stIndex] = "<b>" + kommrsum_tarif_odn.ToString("0.00") + "</b>";
                            dr["reval_charge" + stIndex] = "<b>" + kommreval_charge.ToString("0.00") + "</b>";
                            dr["sum_nedop" + stIndex] = "<b>" + kommsum_nedop.ToString("0.00") + "</b>";
                            dr["sum_ito" + stIndex] = "<b>" + (kommrsum_tarif - kommsum_nedop +
                                kommreval_charge).ToString("0.00") + "</b>";
                            dr["kommItogIndex"] = stIndex;
                            hasKommItog = true;
                            stIndex++;
                        }

                        FillOneRowInChargeGrid(dr, stIndex, ListServ[countServ],"");

                        gilrsum_tarif = gilrsum_tarif + ListServ[countServ].Serv.RsumTarif;
                        gilreval_charge = gilreval_charge + ListServ[countServ].Serv.Reval +
                            ListServ[countServ].Serv.RealCharge;
                        gilsum_nedop = gilsum_nedop + ListServ[countServ].Serv.SumNedop;
                        gilrsum_tarif_odn = gilrsum_tarif_odn + ListServ[countServ].ServOdn.RsumTarif;
                        stIndex++;
                    }
                   
                }
            }

            if ((hasKommItog == false) & (kommrsum_tarif + System.Math.Abs(kommreval_charge) > 0))
            {
                dr["name_serv" + stIndex] = "<b>Итого по коммунальным услугам</b>";
                dr["measure" + stIndex] = "";
                dr["c_calc" + stIndex] = "";
                dr["c_calc_odn" + stIndex] = "";
                dr["tarif" + stIndex] = "";
                dr["rsum_tarif" + stIndex] = "<b>" + kommrsum_tarif.ToString("0.00") + "</b>";
                dr["rsum_tarif_odn" + stIndex] = "<b>" + kommrsum_tarif_odn.ToString("0.00") + "</b>";
                dr["reval_charge" + stIndex] = "<b>" + kommreval_charge.ToString("0.00") + "</b>";
                dr["sum_nedop" + stIndex] = "<b>" + kommsum_nedop.ToString("0.00") + "</b>";
                dr["sum_ito" + stIndex] = "<b>" + (kommrsum_tarif - kommsum_nedop +
                    kommreval_charge).ToString("0.00") + "</b>";
                dr["kommItogIndex"] = stIndex;
                hasKommItog = true;
                stIndex++;
            }

            if ((hasGilItog == false) & (gilrsum_tarif + System.Math.Abs(gilreval_charge) > 0.001m))
            {
                dr["name_serv" + stIndex] = "<b>Итого по жилищным услугам</b>";
                dr["measure" + stIndex] = "";
                dr["c_calc" + stIndex] = "";
                dr["c_calc_odn" + stIndex] = "";
                dr["tarif" + stIndex] = "";
                dr["rsum_tarif" + stIndex] = "<b>" + gilrsum_tarif.ToString("0.00") + "</b>";
                dr["rsum_tarif_odn" + stIndex] = "0.00";
                dr["reval_charge" + stIndex] = "<b>" + gilreval_charge.ToString("0.00") + "</b>"; ;
                dr["sum_nedop" + stIndex] = "<b>" + gilsum_nedop.ToString("0.00") + "</b>"; ;
                dr["sum_ito" + stIndex] = "<b>" + (gilrsum_tarif - gilsum_nedop +
                    gilreval_charge).ToString("0.00") + "</b>"; ;
                dr["gilItogIndex"] = stIndex;
                hasKommItog = true;
                stIndex++;
            }

            for (int i = stIndex; i < 30; i++)
            {
                FillOneRowInChargeGrid(dr, stIndex, null,"");
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
            for (int i = 0; i < ListCounters.Count; i++)
            {
                switch (ListCounters[i].NzpServ)
                {
                    case 6: dr["lsserv" + countersIndex] = countersIndex + ". Хол. вода"; break;
                    case 9: dr["lsserv" + countersIndex] = countersIndex + ". Гор. вода"; break;
                    case 8: dr["lsserv" + countersIndex] = countersIndex + ". Отопл."; break;
                    case 25: dr["lsserv" + countersIndex] = countersIndex + ". Эл.снаб."; break;
                    case 210: dr["lsserv" + countersIndex] = countersIndex + ". Ноч.Эл.."; break;
                    default: dr["lsserv" + countersIndex] = countersIndex + ". " + ListCounters[i].ServiceName; break;
                }
                dr["lsnumcnt" + countersIndex] = ListCounters[i].NumCounters;
                dr["lsdatuchet1_" + countersIndex] = ListCounters[i].DatUchetPred.ToShortDateString();
                dr["lsdatuchet2_" + countersIndex] = ListCounters[i].DatUchet.ToShortDateString(); 
                dr["lsvalcnt1_" + countersIndex] = ListCounters[i].ValuePred;
                dr["lsvalcnt2_" + countersIndex] = ListCounters[i].Value;
                countersIndex++;
            }


            for (int i = countersIndex; i < 8; i++)
            {
                dr["lsserv" + countersIndex] = "";
                dr["lsnumcnt" + countersIndex] = "";
                dr["lsdatuchet1_" + countersIndex] = "";
                dr["lsdatuchet2_" + countersIndex] = "";
                dr["lsvalcnt1_" + countersIndex] = "";
                dr["lsvalcnt2_" + countersIndex] = "";
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
            for (int i = 0; i < ListDomCounters.Count; i++)
            {
                switch (ListDomCounters[i].NzpServ)
                {
                    case 6: dr["domserv" + countersIndex] = countersIndex + ". Хол. вода"; break;
                    case 9: dr["domserv" + countersIndex] = countersIndex + ". Гор. вода"; break;
                    case 8: dr["domserv" + countersIndex] = countersIndex + ". Отопл."; break;
                    case 25: dr["domserv" + countersIndex] = countersIndex + ". Эл.снаб."; break;
                    case 210: dr["domserv" + countersIndex] = countersIndex + ". Ноч.Эл.."; break;
                    default: dr["domserv" + countersIndex] = countersIndex + ". " + ListDomCounters[i].ServiceName; break;
                }

                dr["domserv" + countersIndex] = ListDomCounters[i].ServiceName;
                dr["domnumcnt" + countersIndex] = ListDomCounters[i].NumCounters;
                dr["domdatuchet1_" + countersIndex] = ListDomCounters[i].DatUchetPred.ToShortDateString();
                dr["domdatuchet2_" + countersIndex] = ListDomCounters[i].DatUchet.ToShortDateString();
                dr["domvalcnt1_" + countersIndex] = ListDomCounters[i].ValuePred;
                dr["domvalcnt2_" + countersIndex] = ListDomCounters[i].Value;
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

        /// <summary>
        /// Заполнение строки Адреса
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillAdr(DataRow dr)
        {
            if (dr == null) return false;
            dr["fio"] = PayerFio;
            dr["adress"] = Ulica + " д. " + NumberDom + " кв." + NumberFlat;
            if (Pkod.IndexOf(".") > -1)
                dr["pkod"] = Pkod.Substring(0, Pkod.IndexOf("."));
            else
                dr["pkod"] = "456";
            return true;
        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillSummuryBill(DataRow dr)
        {
            if (dr == null) return false;
            dr["rsum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["reval_charge"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["rsum_tarif_odn"] = SummaryServ.ServOdn.RsumTarif.ToString("0.00");
            dr["sum_nedop"] = SummaryServ.Serv.SumNedop.ToString("0.00");
            dr["sum_ito"] = (SummaryServ.Serv.RsumTarif + SummaryServ.Serv.Reval+
            SummaryServ.Serv.RealCharge - SummaryServ.Serv.SumNedop).ToString("0.00");
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_outsaldo"] = SummaryServ.Serv.SumOutsaldo.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            return true;
        }

        /// <summary>
        /// Подсчет контрольной суммы в штрих-коде
        /// </summary>
        /// <param name="acode">Штрих-код</param>
        /// <returns>Контрольная цифра</returns>
        public string BarcodeCrc(string acode)
        {
            int sum_ = 0;


            for (int i = 0; i < acode.Length; i++)
            {
                if (i != 28)
                {
                    if ((i % 2) == 1)
                    {
                        sum_ = sum_ + System.Convert.ToInt16(acode.Substring(i, 1));
                    }
                    else sum_ = sum_ + 3 * System.Convert.ToInt16(acode.Substring(i, 1));
                }
            }

            String s = ((10 - sum_ % 10) % 10).ToString();

            return s.Substring(0, 1);

        }

        /// <summary>
        /// Формирование штрих-кода
        /// </summary>
        /// <returns>Готовый штрих-код</returns>
        public override string GetBarCode()
        {
            if (Pkod.Length > 10)
                Pkod = Pkod.Substring(0, 10);
            else
                Pkod = ("0000000000").Substring(0, 10 - Pkod.Length) + Pkod;
            
            string vars = "33" + Pkod + Month.ToString("D2") +
             (Year - 2000).ToString("D2") +"0000"+
             (System.Math.Max(0, SumTicket) * 100).ToString("0000000");
            return vars + BarcodeCrc(vars) + BarcodeCrc(vars);
        }

        /// <summary>
        /// Отображать ли услугу в таблице начислений
        /// </summary>
        /// <param name="aServ">Услуга</param>
        /// <returns>Истина, если отображать</returns>
        public override bool IsShowServInGrid(BaseServ aServ)
        {
            if ((System.Math.Abs(aServ.Serv.Tarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumNedop) < 0.001m)
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
        public override void FinalPass(Faktura finder)
        {

            if (SummaryServ.Serv.SumOutsaldo > 0.001m)
            {
                SumTicket = SummaryServ.Serv.SumOutsaldo;
            }
            else
            {
                SumTicket = 0;
            }

        }


        public StandartFaktura() : 
            base()
        {
            Rekvizit = new _Rekvizit();

            SummaryServ = new BaseServ(false);
            FakturaBlocks.HasAdrBlock = true;
            FakturaBlocks.HasRekvizitBlock = true;
            FakturaBlocks.HasKvarPrmBlock = true;
            FakturaBlocks.HasSummuryBillBlock = true;
            FakturaBlocks.HasMainChargeGridBlock = true;
            FakturaBlocks.HasRevalReasonBlock = false;
            FakturaBlocks.HasServiceVolumeBlock = false;
            FakturaBlocks.HasRassrochka = false;
            FakturaBlocks.HasCountersBlock = false;
            FakturaBlocks.HasCountersDoubleBlock = true;
            FakturaBlocks.HasCountersDoubleDomBlock = true;
            FakturaBlocks.HasNormblock = false;
            //fakturaBlocks.hasRemarkblock = true;
            Clear();
    
        }
    }
   
}

