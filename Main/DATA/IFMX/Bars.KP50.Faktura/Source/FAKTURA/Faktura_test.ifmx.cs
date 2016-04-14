
using Bars.KP50.Faktura.Source.Base;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;



    public class TestFaktura : BaseFactura2
    {
        private const int MaxNachStringCount = 33;
        private const int MaxCountersCount = 8;
        private const int MaxLgotStringCount = 5;

        public override DataTable MakeTable()
        {
            DataTable table = new DataTable();
            table.TableName = "Q_master";

            table.Columns.Add("poluch", typeof(string));
            table.Columns.Add("poluch_adres", typeof(string));
            table.Columns.Add("poluch_phone", typeof(string));
            table.Columns.Add("poluch_bank", typeof(string));
            table.Columns.Add("poluch_rs", typeof(string));
            table.Columns.Add("poluch_ks", typeof(string));
            table.Columns.Add("poluch_bik", typeof(string));
            table.Columns.Add("poluch_inn", typeof(string));
            table.Columns.Add("pm_note", typeof(string));

            table.Columns.Add("poluch2", typeof(string));
            table.Columns.Add("poluch2_adres", typeof(string));
            table.Columns.Add("poluch2_phone", typeof(string));
            table.Columns.Add("poluch2_bank", typeof(string));
            table.Columns.Add("poluch2_rs", typeof(string));
            table.Columns.Add("poluch2_ks", typeof(string));
            table.Columns.Add("poluch2_bik", typeof(string));
            table.Columns.Add("poluch2_inn", typeof(string));

            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("adress", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("date_print", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("kolvgil", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("datedestv", typeof(string));
            table.Columns.Add("kommItogIndex", typeof(int));
            table.Columns.Add("gilItogIndex", typeof(int));
            table.Columns.Add("et", typeof(string));
            table.Columns.Add("komf", typeof(string));


            table.Columns.Add("sum_dolg", typeof(string));
            table.Columns.Add("reval", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("rsum_tarif_all", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_tarif", typeof(string));
            table.Columns.Add("sum_nedop", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("vars2", typeof(string));

            for (int i = 1; i < 16;i++)
            {
                table.Columns.Add("sb" + i, typeof(string));
            }

            for (int i = 1; i < MaxNachStringCount; i++)
                {
                    table.Columns.Add("num" + i, typeof(string));
                    table.Columns.Add("name_serv" + i, typeof(string));
                    table.Columns.Add("measure" + i, typeof(string));
                    table.Columns.Add("tarif" + i, typeof(string));
                    table.Columns.Add("sum_dolg" + i, typeof(string));
                    table.Columns.Add("sum_money" + i, typeof(string));
                    table.Columns.Add("rsum_tarif_all" + i, typeof(string));
                    table.Columns.Add("sum_tarif" + i, typeof(string));
                    table.Columns.Add("c_calc" + i, typeof(string));
                    table.Columns.Add("reval" + i, typeof(string));
                    table.Columns.Add("sum_nedop" + i, typeof(string));
                    table.Columns.Add("sum_charge_all" + i, typeof(string));
                }


            for (int i = 1; i < 13; i++)
            {

                table.Columns.Add("lsord" + i, typeof(string));
                table.Columns.Add("lsserv" + i, typeof(string));
                table.Columns.Add("lsnumcnt" + i, typeof(string));
                table.Columns.Add("lsdatuchet2_" + i, typeof(string));
                table.Columns.Add("lsvalcnt2_" + i, typeof(string));
                table.Columns.Add("srokslugb" + i, typeof(string));
            }


            table.Columns.Add("el_arend", typeof(string));
            table.Columns.Add("el_dpu", typeof(string));
            table.Columns.Add("el_dpu_odn", typeof(string));
            table.Columns.Add("el_kv", typeof(string));
            table.Columns.Add("el_norm", typeof(string));
            table.Columns.Add("el_odn", typeof(string));
            table.Columns.Add("el", typeof(string));

            table.Columns.Add("ni_arend", typeof(string));
            table.Columns.Add("ni_dpu", typeof(string));
            table.Columns.Add("ni_dpu_odn", typeof(string));
            table.Columns.Add("ni_kv", typeof(string));
            table.Columns.Add("ni_norm", typeof(string));
            table.Columns.Add("ni_odn", typeof(string));
            table.Columns.Add("ni", typeof(string));

            table.Columns.Add("hv_arend", typeof(string));
            table.Columns.Add("hv_dpu", typeof(string));
            table.Columns.Add("hv_dpu_odn", typeof(string));
            table.Columns.Add("hv_kv", typeof(string));
            table.Columns.Add("hv_norm", typeof(string));
            table.Columns.Add("hv_odn", typeof(string));
            table.Columns.Add("hv", typeof(string));

            table.Columns.Add("gv_arend", typeof(string));
            table.Columns.Add("gv_dpu", typeof(string));
            table.Columns.Add("gv_dpu_odn", typeof(string));
            table.Columns.Add("gv_kv", typeof(string));
            table.Columns.Add("gv_norm", typeof(string));
            table.Columns.Add("gv_odn", typeof(string));
            table.Columns.Add("gv", typeof(string));

            table.Columns.Add("kan_arend", typeof(string));
            table.Columns.Add("kan_dpu", typeof(string));
            table.Columns.Add("kan_dpu_odn", typeof(string));
            table.Columns.Add("kan_kv", typeof(string));
            table.Columns.Add("kan_norm", typeof(string));
            table.Columns.Add("kan_odn", typeof(string));
            table.Columns.Add("kan", typeof(string));

            table.Columns.Add("otop_arend", typeof(string));
            table.Columns.Add("otop_dpu", typeof(string));
            table.Columns.Add("otop_dpu_odn", typeof(string));
            table.Columns.Add("otop_kv", typeof(string));
            table.Columns.Add("otop_norm", typeof(string));
            table.Columns.Add("otop_odn", typeof(string));
            table.Columns.Add("otop", typeof(string));
            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("k_el", typeof(string));
            table.Columns.Add("k_hv", typeof(string));
            table.Columns.Add("k_gv", typeof(string));

            for (int i = 1; i < 6; i++)
            {
                table.Columns.Add("lgfio" + i, typeof(string));
                table.Columns.Add("lglgota" + i, typeof(string));
                table.Columns.Add("lgsubs" + i, typeof(string));
                table.Columns.Add("lgedv" + i, typeof(string));
                table.Columns.Add("lgsv" + i, typeof(string));
            }
         


            return table;

        }

        /// <summary>
        /// Заполнение квартирных параметров
        /// </summary>
        /// <param name="dr">Строка таблицы</param>
        /// <returns></returns>
        protected override bool FillKvarPrm()
        {
            if (Dr == null) return false;

            Dr["kolgil"] = Kvar.CountGil;
            Dr["kolvgil"] = Kvar.CountDepartureGil;
            Dr["kv_pl"] = Kvar.FullSquare.ToString("0.00");
            Dr["et"] = Kvar.Stage;
            if (Kvar.IsolateFlat)
                Dr["komf"] = "изолированное";
            else
                Dr["komf"] = "коммунальное";


            return true;
        }

        /// <summary>
        /// Заполнение реквизитов счета
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillRekvizit()
        {
            if (Dr == null) return false;
            _Rekvizit rekvizit = Rekvizits.GetRekvizit(NzpArea,NzpGeu,Pref);
            if (rekvizit != null)
            {
                Dr["sb1"] = rekvizit.poluch;
                Dr["sb2"] = rekvizit.bank;
                Dr["sb3"] = rekvizit.rschet;
                Dr["sb4"] = rekvizit.korr_schet;
                Dr["sb5"] = rekvizit.bik;
                Dr["sb6"] = rekvizit.inn;
                Dr["sb7"] = rekvizit.phone;
                Dr["sb8"] = rekvizit.adres;
                Dr["sb9"] = rekvizit.pm_note;
                Dr["sb10"] = rekvizit.poluch2;
                Dr["sb11"] = rekvizit.bank2;
                Dr["sb12"] = rekvizit.rschet2;
                Dr["sb13"] = rekvizit.korr_schet2;
                Dr["sb14"] = rekvizit.bik2;
                Dr["sb15"] = rekvizit.inn2;
                Dr["poluch"] = rekvizit.ercName;
            }
         

            Dr["months"] = FullMonthName;
            Dr["date_print"] = DateTime.Now.ToShortDateString();
            Dr["datedestv"] = DateTime.Now.ToString("28.MM.yyyy");
            Dr["vars2"] = Geu.GeuKodErc;
            return true;
        }


        /// <summary>
        /// Заполнение одной строки в таблице начислений
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="stringIndex">Номер строки</param>
        /// <param name="bs">Услуга</param>
        /// <returns></returns>
        protected virtual bool FillOneRowInChargeGrid(ref int stringIndex, BaseServ2 bs, string num_st)
        {
            //Если в наборе больше строк чем в счете, то не пытаемся печатать лишние строки
#warning Поставить логгирование таких ситуаций
            //if (stringIndex > maxNachStringCount) return true;
            //if (bs != null)
            //{
            //    dr["num" + stringIndex] = num_st;
            //    if (bs.serv.nzpServ == 9)
            //        dr["name_serv" + stringIndex] = "Горячее водоснабжение";
            //    else
            //        dr["name_serv" + stringIndex] = bs.serv.nameServ;
            //    dr["measure" + stringIndex] = bs.serv.measure;
            //    dr["tarif" + stringIndex] = bs.serv.tarif.ToString("0.00");
            //    dr["sum_dolg" + stringIndex] = bs.serv.sumInsaldo.ToString("0.00");
            //    dr["sum_money" + stringIndex] = bs.serv.sumMoney.ToString("0.00");
            //    dr["rsum_tarif_all" + stringIndex] = bs.serv.rsumTarif.ToString("0.00");
            //    dr["sum_tarif" + stringIndex] = bs.serv.sumTarif.ToString("0.00");
            //    dr["sum_charge_all" + stringIndex] = bs.serv.sumCharge.ToString("0.00");
            //    dr["reval" + stringIndex] = (bs.serv.realCharge + bs.serv.reval).ToString("0.00");
            //    dr["sum_nedop" + stringIndex] = bs.serv.sumNedop.ToString("0.00");
            //    stringIndex++;
            //    if (bs.serv.nzpServ == 9)
            //        foreach (SumServ2 ss in bs.SlaveServ)
            //        {
            //            dr["name_serv" + stringIndex] = "в т.ч." + ss.nameServ;
            //            dr["measure" + stringIndex] = ss.measure;
            //            dr["tarif" + stringIndex] = ss.tarif.ToString("0.00");
            //            dr["sum_dolg" + stringIndex] = ss.sumInsaldo.ToString("0.00");
            //            dr["sum_money" + stringIndex] = ss.sumMoney.ToString("0.00");
            //            dr["rsum_tarif_all" + stringIndex] = ss.rsumTarif.ToString("0.00");
            //            dr["sum_tarif" + stringIndex] = ss.sumTarif.ToString("0.00");
            //            dr["sum_charge_all" + stringIndex] = ss.sumCharge.ToString("0.00");
            //            dr["reval" + stringIndex] = (ss.realCharge + ss.reval).ToString("0.00");
            //            dr["sum_nedop" + stringIndex] = ss.sumNedop.ToString("0.00");
            //            stringIndex++;
            //        }
            //}
            return true;
        }

        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillMainChargeGrid()
        {
            //if (dr == null) return false;
            //int stIndex = 1;

          
            //int num_st = 0;
            //BaseServ2 itogServ = new BaseServ2();

            //dr["kommItogIndex"] = 0;
            //dr["gilItogIndex"] = 0;

            //var listServs = charge.ListServ.OrderBy(x => x.Value.serv.ordering);

            //#region Коммунальные услуги
            //foreach (KeyValuePair<int, BaseServ2> kp in listServs)
            //{

            //    if (kp.Value.KommServ)
            //    {
            //        if (IsShowServInGrid(kp.Value))
            //        {
            //            itogServ.serv.AddSum(kp.Value.serv);
            //            num_st++;
            //            FillOneRowInChargeGrid(dr, ref stIndex, kp.Value, num_st.ToString());

            //        }
            //    }

            //}
            //if ((stIndex > 1) & (stIndex <= maxNachStringCount))
            //{
            //    dr["name_serv" + stIndex] = "<b>Итого по коммунальным услугам</b>";
            //    dr["measure" + stIndex] = "";
            //    dr["tarif" + stIndex] = "";
            //    dr["sum_dolg" + stIndex] = "<b>" + itogServ.serv.sumInsaldo.ToString("0.00") + "</b>";
            //    dr["rsum_tarif_all" + stIndex] = "<b>" + itogServ.serv.rsumTarif.ToString("0.00") + "</b>";
            //    dr["sum_tarif" + stIndex] = "<b>" + itogServ.serv.sumTarif.ToString("0.00") + "</b>";
            //    dr["reval" + stIndex] = "<b>" + (itogServ.serv.reval +
            //    itogServ.serv.realCharge).ToString("0.00") + "</b>";
            //    dr["sum_charge_all" + stIndex] = "<b>" + itogServ.serv.sumCharge.ToString("0.00") + "</b>";
            //    dr["sum_nedop" + stIndex] = "<b>" + itogServ.serv.sumNedop.ToString("0.00") + "</b>";
            //    dr["kommItogIndex"] = stIndex;
            //    stIndex++;
            //}

            //#endregion
            //itogServ.serv.Clear();

            //#region Жилищные услуги
            //foreach (KeyValuePair<int, BaseServ2> kp in listServs)
            //{
            //    if (!kp.Value.KommServ)
            //    {
            //        if (IsShowServInGrid(kp.Value))
            //        {
            //            itogServ.serv.AddSum(kp.Value.serv);
            //            num_st++;
            //            FillOneRowInChargeGrid(dr, ref stIndex, kp.Value, num_st.ToString());

            //        }
            //    }

            //}

            //if (((itogServ.serv.rsumTarif + System.Math.Abs(itogServ.serv.realCharge) +
            //    System.Math.Abs(itogServ.serv.sumCharge) +
            //    System.Math.Abs(itogServ.serv.sumPere) > 0.001m)) & (stIndex <= maxNachStringCount))
            //{
            //    dr["name_serv" + stIndex] = "<b>Итого по жилищным услугам</b>";
            //    dr["measure" + stIndex] = "";
            //    dr["tarif" + stIndex] = "";
            //    dr["sum_dolg" + stIndex] = "<b>" + itogServ.serv.sumInsaldo.ToString("0.00") + "</b>";
            //    dr["rsum_tarif_all" + stIndex] = "<b>" + itogServ.serv.rsumTarif.ToString("0.00") + "</b>";
            //    dr["sum_tarif" + stIndex] = "<b>" + itogServ.serv.sumTarif.ToString("0.00") + "</b>";
            //    dr["reval" + stIndex] = "<b>" + (itogServ.serv.reval +
            //    itogServ.serv.realCharge).ToString("0.00") + "</b>";
            //    dr["sum_charge_all" + stIndex] = "<b>" + itogServ.serv.sumCharge.ToString("0.00") + "</b>";
            //    dr["sum_nedop" + stIndex] = "<b>" + itogServ.serv.sumNedop.ToString("0.00") + "</b>";
            //    dr["gilItogIndex"] = stIndex;
            //    stIndex++;
            //}

            //#endregion
            return true;
        }

        /// <summary>
        /// Заполнение счетчиков
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override bool FillCounters()
        {
            int countersIndex = 1;
           List<FakturaCounters> cnt = Counters.LoadDoubleLsCounters(Pref, NzpKvar, Pkod);
            for (int i = 0; i < Math.Min(MaxCountersCount,cnt.Count); i++)
            {
                switch (cnt[i].NzpServ)
                {
                    case 6: Dr["lsserv" + countersIndex] = countersIndex + ". Хол. вода"; break;
                    case 9: Dr["lsserv" + countersIndex] = countersIndex + ". Гор. вода"; break;
                    case 8: Dr["lsserv" + countersIndex] = countersIndex + ". Отопл."; break;
                    case 25: Dr["lsserv" + countersIndex] = countersIndex + ". Эл.снаб."; break;
                    case 210: Dr["lsserv" + countersIndex] = countersIndex + ". Ноч.Эл.."; break;
                    default: Dr["lsserv" + countersIndex] = countersIndex + ". " + cnt[i].ServiceName; break;
                }
                Dr["lsnumcnt" + countersIndex] = cnt[i].NumCounter;
                Dr["lsdatuchet2_" + countersIndex] = cnt[i].DatUchet.ToShortDateString();
                Dr["lsvalcnt2_" + countersIndex] = cnt[i].Value;
                Dr["srokslugb" + countersIndex] =
                    cnt[i].IsProv(new DateTime(Year, Month, 1)) == 1 ? "" : "Срок службы истек " +
                    cnt[i].DatProv;
                countersIndex++;
            }


            return true;
        }

        /// <summary>
        /// Заполнение строки Адреса
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillAdr()
        {
            if (Dr == null) return false;
            Dr["fio"] = Kvar.PayerFio;
            Dr["adress"] = Ulica + " д. " + NumberDom + " кв." + NumberFlat;
            if (Pkod.IndexOf(".") > -1)
                Dr["pkod"] = Pkod.Substring(0, Pkod.IndexOf("."));
            else
                Dr["pkod"] = "741";
            Dr["numdom"] = NumberDom;
            Dr["kvnum"] = NumberFlat;
            Dr["ulica"] = Ulica;
            return true;
        }

        /// <summary>
        /// Загрузка информации от СЗ
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillSzInf()
        {
            if (Dr == null) return false;
            int gilIndex = 1;
            SzInformation szinf = SzInf.GetSzInformation(NzpKvar);
            for (int i = 0; i < Math.Min(MaxLgotStringCount, szinf.ListGilec.Count); i++)
            {
                Dr["lgfio" + gilIndex] = szinf.ListGilec[i].FIO;
                Dr["lglgota" + gilIndex] = szinf.ListGilec[i].SumLgota;
                Dr["lgsubs" + gilIndex] = szinf.ListGilec[i].SumSubs + szinf.ListGilec[i].SumTepl;
                Dr["lgedv" + gilIndex] = szinf.ListGilec[i].SumEdv;
                Dr["lgsv" + gilIndex] = szinf.ListGilec[i].SumSv;
                gilIndex++;
            }
            return true;
        }

        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillServiceVolume()
        {
            if (Dr == null) return false;
            List<ServVolume2> listVolume = ServVolumeLs.GetLsServNormativ(Pref, NzpKvar, true);
            foreach (ServVolume2 sv in listVolume)
            {
                string servPref = "";
                switch (sv.NzpServ)
                {
                    case 25: servPref = "el"; break;
                    case 210: servPref = "ni"; break;
                    case 6: servPref = "hv"; break;
                    case 9: servPref = "gv"; break;
                    case 8: servPref = "otop"; break;
                    case 7: servPref = "kan"; break;
                }
                
                if (servPref != "")
                {
                        Dr[servPref + "_kv"] = sv.Volume.ToString("0.000");
                        Dr[servPref + "_norm"] = sv.FullNormativ.ToString("0.00");
                        Dr[servPref + "_odn"] = (sv.Odn).ToString("0.000");
                        Dr[servPref] = (sv.Odn + sv.Volume).ToString("0.000");
                }
            }

            //FillDomRashod(dr);
            return true;
        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillSummuryBill()
        {
            //if (dr == null) return false;
            //dr["reval"] = (charge.SummaryServ.serv.reval +
            //    charge.SummaryServ.serv.realCharge).ToString("0.00");
            //dr["sum_tarif"] = charge.SummaryServ.serv.sumTarif.ToString("0.00");
            //dr["rsum_tarif_all"] = charge.SummaryServ.serv.rsumTarif.ToString("0.00");
            //dr["sum_nedop"] = charge.SummaryServ.serv.sumNedop.ToString("0.00");
            //dr["sum_charge_all"] = charge.SummaryServ.serv.sumCharge.ToString("0.00");
            //dr["sum_dolg"] = charge.SummaryServ.serv.sumInsaldo.ToString("0.00");
            //dr["sum_ticket"] = sumTicket.ToString("0.00");
            //dr["sum_money"] = charge.SummaryServ.serv.sumMoney.ToString("0.00");
            
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
            Shtrih = vars + BarcodeCrc(vars);
            return Shtrih;
        }

        /// <summary>
        /// Отображать ли услугу в таблице начислений
        /// </summary>
        /// <param name="aServ">Услуга</param>
        /// <returns>Истина, если отображать</returns>
        public virtual bool IsShowServInGrid()
        {
        //{
        //    if ((System.Math.Abs(aServ.serv.tarif) < 0.001m) &
        //        (System.Math.Abs(aServ.serv.rsumTarif) < 0.001m) &
        //        (System.Math.Abs(aServ.serv.reval) < 0.001m) &
        //        (System.Math.Abs(aServ.serv.realCharge) < 0.001m) &
        //        (System.Math.Abs(aServ.serv.sumNedop) < 0.001m) &
        //        (System.Math.Abs(aServ.serv.sumCharge) < 0.001m)
        //    )
        //    {
        //        return false;
        //    }
            return true;
        }


        /// <summary>
        /// Формирование суммы к оплате в счете
        /// </summary>
        /// <param name="finder"></param>
        public override void FinalPass(Faktura finder)
        {

            //if (charge.SummaryServ.serv.sumCharge > 0.001m)
            //{
            //    sumTicket = charge.SummaryServ.serv.sumCharge;
            //}
            //else
            //{
            //    sumTicket = 0;
            //}

        }


        public override bool FillGeuData()
        {
            if (Dr == null) return false;
            Dr["vars2"] = Geu.GeuKodErc;
            return true;
        }

       
    }
   
}

