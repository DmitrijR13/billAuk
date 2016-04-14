
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

    public class AstrahanFaktura : BaseFactura
    {

        int maxCountersCount = 7;
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
                if (i != 30)
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
            if (Pkod.Length > 13)
                Pkod = Pkod.Substring(0, 13);
            else
                Pkod = ("0000000000000").Substring(0, 13 - Pkod.Length) + Pkod;

            string vars = "33" + Pkod + Month.ToString("D2") +
             (Year - 2000).ToString("D2") + "00" +
             (System.Math.Min(99999.99m, SumTicket) * 100).ToString("00000000");

            //barcode 2


            Shtrih = vars + BarcodeCrc(vars);
            GeuKodErc = "000000000000";
            return Shtrih;
        }

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
                table.Columns.Add("name_supp" + i, typeof(string));
                table.Columns.Add("supp_serv" + i, typeof(string));
                table.Columns.Add("supp_rekv" + i, typeof(string));
                table.Columns.Add("sum_money" + i, typeof(string));
                table.Columns.Add("day_nedop" + i, typeof(string));
                table.Columns.Add("reval" + i, typeof(string));
                table.Columns.Add("sum_dolg" + i, typeof(string));
                table.Columns.Add("sum_charge_all" + i, typeof(string));
                table.Columns.Add("sum_charge" + i, typeof(string));

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

            for (int i = 1; i < 7; i++)
            {
                table.Columns.Add("domrash_service" + i, typeof(string));
                table.Columns.Add("domrash_measure" + i, typeof(string));
                table.Columns.Add("domrash_domVolume" + i, typeof(string));
                table.Columns.Add("domrash_odnDomVolume" + i, typeof(string));
                table.Columns.Add("domrash_domArendatorsVolume" + i, typeof(string));
            }

            for (int i = 1; i < 10; i++)
            {
                table.Columns.Add("serv_pere" + i.ToString(), typeof(string));
                table.Columns.Add("osn_pere" + i.ToString(), typeof(string));
                table.Columns.Add("sum_pere" + i.ToString(), typeof(string));
                table.Columns.Add("period_pere" + i.ToString(), typeof(string));
            }

            table.Columns.Add("vars2", typeof(string));

            return table;
        }

        //public override void Clear()
        //{
        //    countGil = 0;
        //    countRegisterGil = 0;
        //    countDepartureGil = 0;
        //    countArriveGil = 0;

        //    fullSquare = 0;
        //    liveSquare = 0;
        //    calcSquare = 0;
        //    heatSquare = 0;

        //    ownflat = false;
        //    isolateFlat = true;
        //    payerFio = "";

        //    month = System.DateTime.Now.Month;
        //    year = System.DateTime.Now.Year;
        //    fullMonthName = "";
        //    pkod = "";
        //    geu = "";
        //    ulica = "";
        //    numberDom = "";
        //    numberFlat = "";
        //    prefixUk = "";
        //    codeUk = "";

        //    indecs = "";

        //    nzpArea = 0;
        //    nzpGeu = 0;


        //    summaryServ.Clear();
        //    listCounters.Clear();
        //    listDomCounters.Clear();

        //    rekvizit.nzp_geu = 0;
        //    rekvizit.nzp_area = 0;
        //    rekvizit.poluch = "";
        //    rekvizit.bank = "";
        //    rekvizit.rschet = "";
        //    rekvizit.korr_schet = "";
        //    rekvizit.bik = "";
        //    rekvizit.inn = "";
        //    rekvizit.phone = "";
        //    rekvizit.adres = "";
        //    rekvizit.pm_note = "";
        //    rekvizit.poluch2 = "";
        //    rekvizit.bank2 = "";
        //    rekvizit.rschet2 = "";
        //    rekvizit.korr_schet2 = "";
        //    rekvizit.bik2 = "";
        //    rekvizit.inn2 = "";
        //    rekvizit.phone2 = "";
        //    rekvizit.adres2 = "";
        //    rekvizit.filltext = 0;

        //}

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
                Rekvizit.rschet + " в " + Rekvizit.bank + " к/с " +
                Rekvizit.korr_schet + " ИНН/КПП " + Rekvizit.inn + " БИК " + Rekvizit.bik + " " +
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
            dr["datedestv"] = new DateTime(Year, Month, 01).AddMonths(2).AddDays(-1).ToString("dd.MM.yyyy");//DateTime.Now.ToString("28.MM.yyyy");

            dr["month_"] = Month;
            dr["year_"] = Year;
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
                dr["sum_money" + stringIndex] = "";
                dr["measure" + stringIndex] = "";
                dr["tarif" + stringIndex] = "";
                dr["c_calc" + stringIndex] = "";
                dr["c_calc_odn" + stringIndex] = "";
                dr["rsum_tarif" + stringIndex] = "";
                dr["rsum_tarif_odn" + stringIndex] = "";
                dr["sum_nedop" + stringIndex] = "";
                dr["day_nedop" + stringIndex] = "";
                dr["reval" + stringIndex] = "";
                dr["sum_dolg" + stringIndex] = "";
                dr["sum_charge_all" + stringIndex] = "";
            }
            else
            {
                dr["name_serv" + stringIndex] = bs.Serv.NameServ;
                dr["sum_money" + stringIndex] = bs.Serv.SumMoney.ToString("0.00");
                dr["measure" + stringIndex] = bs.Serv.Measure;
                dr["tarif" + stringIndex] = bs.Serv.Tarif.ToString("0.00");
                dr["c_calc" + stringIndex] = bs.Serv.CCalc.ToString("0.00");
                dr["c_calc_odn" + stringIndex] = bs.ServOdn.CCalc.ToString("0.00");
                dr["rsum_tarif" + stringIndex] = bs.Serv.RsumTarif.ToString("0.00");
                dr["rsum_tarif_odn" + stringIndex] = bs.ServOdn.RsumTarif.ToString("0.00");
                dr["sum_nedop" + stringIndex] = bs.Serv.SumNedop.ToString("0.00");
                dr["day_nedop" + stringIndex] = (DateTime.DaysInMonth(Year, Month) - bs.Serv.COkaz).ToString();
                dr["reval" + stringIndex] = (bs.Serv.Reval + bs.Serv.RealCharge).ToString("0.00");
                dr["sum_dolg" + stringIndex] = (bs.Serv.SumInsaldo - bs.Serv.SumMoney).ToString("0.00");
                dr["sum_charge_all" + stringIndex] = bs.Serv.SumCharge.ToString("0.00");
            }
            return true;
        }


        /// <summary>
        /// Заполнение таблицы по поставщикам
        /// </summary>
        /// <param name="dr"></param>
        protected void FillSupplierGrid(DataRow dr)
        {
            int index = 1;
            for (int countServ = 0; countServ < ListSupp.Count; countServ++)
            {
                if (IsShowServInGrid(ListSupp[countServ]))
                {

                    if (index < 9)
                    {
                        dr["supp_serv" + index] = ListSupp[countServ].Serv.NameServ.Trim();
                        dr["name_supp" + index] = ListSupp[countServ].Serv.NameSupp.Trim();
                        dr["supp_rekv" + index] = ListSupp[countServ].Serv.SuppRekv;
                        index++;
                    }


                }

            }

        }

        public override void AddSupp(BaseServ aServ)
        {

            //Проверка на пустоту добавляемой услуги
            if (aServ.Empty()) return;
            bool findSupp = false;


            for (int i = 0; i < ListSupp.Count; i++)
            {
                if (ListSupp[i].Serv.NzpSupp == aServ.Serv.NzpSupp && ListSupp[i].Serv.NzpServ == aServ.Serv.NzpServ) //если такой поставщик уже есть то добавляем к нему
                {
                    ListSupp[i].AddSum(aServ.Serv);
                    ListSupp[i].Serv.NameSupp = aServ.Serv.NameSupp;
                    ListSupp[i].Serv.NameServ = aServ.Serv.NameServ;
                    ListSupp[i].Serv.SuppRekv = aServ.Serv.SuppRekv;

                    findSupp = true;
                }
            }
            if (!findSupp)
            {
                BaseServ newServ = new BaseServ(false);
                newServ.AddSum(aServ.Serv);
                newServ.Serv.NameSupp = aServ.Serv.NameSupp;
                newServ.Serv.SuppRekv = aServ.Serv.SuppRekv;
                newServ.Serv.NameServ = aServ.Serv.NameServ;
                ListSupp.Add(newServ); //иначе добавляем 
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
            int stIndex = 1;

            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (IsShowServInGrid(ListServ[countServ]))
                {

                    FillOneRowInChargeGrid(dr, stIndex, ListServ[countServ], "");
                    stIndex++;
                }
            }

            for (int i = stIndex; i < 30; i++)
            {
                FillOneRowInChargeGrid(dr, stIndex, null, "");
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
            DateTime FakturaDate = new DateTime(Year, Month, 1);
            int countersIndex = 1;
            for (int i = 0; i < Math.Min(maxCountersCount, ListCounters.Count); i++)
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
                if (ListCounters[i].DatUchetPred.ToShortDateString() == "01.01.1900")
                    dr["lsdatuchet1_" + countersIndex] = "";
                else
                    dr["lsdatuchet1_" + countersIndex] = ListCounters[i].DatUchetPred.ToShortDateString();
                dr["lsdatuchet2_" + countersIndex] = ListCounters[i].DatUchet.ToShortDateString();
              
                //dr["lsvalcnt1_" + countersIndex] = (FakturaDate <= ListCounters[i].DatUchet ? ListCounters[i].Value.ToString("0.00#") : ListCounters[i].ValuePred.ToString("0.00#"));
                //dr["lsvalcnt2_" + countersIndex] = (FakturaDate > ListCounters[i].DatUchet ? ListCounters[i].Value.ToString("0.00#") : "");
                dr["lsvalcnt1_" + countersIndex] = (FakturaDate >= ListCounters[i].DatUchet ? ListCounters[i].Value.ToString("0.00#") : ListCounters[i].ValuePred.ToString("0.00#"));
                dr["lsvalcnt2_" + countersIndex] = (FakturaDate < ListCounters[i].DatUchet ? ListCounters[i].Value.ToString("0.00#") : "");
          
                dr["lsdatProv_" + countersIndex] = ListCounters[i].DatProv;
                countersIndex++;
            }


            dr["vars2"] = Pkod;

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
            for (int i = 0; i < Math.Min(maxCountersCount, ListDomCounters.Count); i++)
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
                if (ListDomCounters[i].DatUchetPred > DateTime.Now.AddYears(-10))
                    dr["domdatuchet1_" + countersIndex] = ListDomCounters[i].DatUchetPred.ToShortDateString();
                if (ListDomCounters[i].DatUchet > DateTime.Now.AddYears(-10))
                    dr["domdatuchet2_" + countersIndex] = ListDomCounters[i].DatUchet.ToShortDateString();
                dr["domvalcnt1_" + countersIndex] = ListDomCounters[i].ValuePred.ToString("0.00#");
                dr["domvalcnt2_" + countersIndex] = ListDomCounters[i].Value.ToString("0.00#");
                dr["domdatProv_" + countersIndex] = ListDomCounters[i].DatProv;
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
                dr["domdatProv_" + countersIndex] = "";
            }

            int rashIndex = 0;
            for (int i = 1; i < ListVolume.Count; i++)
            {
                dr["domrash_service" + rashIndex] = ListVolume[i].ServiceName;
                string measure = ""; ;
                switch (i)
                {
                    case 1: { measure = "кВт*ч"; break; }
                    case 2: { measure = "кВт*ч"; break; }
                    case 3: { measure = "куб.м"; break; }
                    case 4: { measure = "куб.м"; break; }
                    case 5: { measure = "с чел.в мес"; break; }
                    case 6: { measure = "кв.метр"; break; }
                }

                dr["domrash_measure" + rashIndex] = measure;
                dr["domrash_domVolume" + rashIndex] = ListVolume[i].DomVolume;
                dr["domrash_odnDomVolume" + rashIndex] = ListVolume[i].OdnDomVolume;
                dr["domrash_domArendatorsVolume" + rashIndex] = ListVolume[i].DomArendatorsVolume;


                rashIndex++;
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
                dr["pkod"] = "35";

            dr["kolgil2"] = CountRegisterGil;
            dr["ulica"] = Ulica;
            dr["numdom"] = NumberDom;
            dr["kvnum"] = NumberFlat;
            dr["dom_gil"] = CountDomGil;
            dr["pl_dom"] = DomSquare.ToString("0.00");
            dr["pl_mop"] = MopSquare.ToString("0.00"); ;
            dr["type_pl"] = IsolateFlat ? "общая" : "жилая";
            dr["indecs"] = Indecs;

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
            dr["sum_ito"] = (SummaryServ.Serv.RsumTarif + SummaryServ.Serv.Reval +
            SummaryServ.Serv.RealCharge - SummaryServ.Serv.SumNedop).ToString("0.00");
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_outsaldo"] = SummaryServ.Serv.SumOutsaldo.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");

            dr["sum_charge"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["reval"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_dolg"] = (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney).ToString("0.00");
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
        public override bool IsShowServInGrid(BaseServ aServ)
        {
            if ((System.Math.Abs(aServ.Serv.Tarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumNedop) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumInsaldo) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumMoney) < 0.001m)
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

            if (SummaryServ.Serv.SumCharge > 0.001m)
            {
                SumTicket = SummaryServ.Serv.SumCharge;
            }
            else
            {
                SumTicket = 0;
            }

        }


        public AstrahanFaktura() :
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
            FakturaBlocks.HasRTCountersDoubleBlock = true;
            FakturaBlocks.HasNormblock = false;
            FakturaBlocks.HasRTCountersDoubleDomBlock = true;
            FakturaBlocks.HasRemarkblock = true;
            FakturaBlocks.HasNewDoubleCountersBlock = false;
            Clear();

        }
    }
}
