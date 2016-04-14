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


namespace Bars.KP50.DB.Faktura
{


    public class SahaFaktura : BaseFactura
    {
 
        public override DataTable MakeTable()
        {
            DataTable table = new DataTable();
            table.TableName = "Q_master";
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("adress", typeof(string));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("month_num", typeof(string));
            table.Columns.Add("date_print", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("area", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));

            for (int i = 1; i<16; i++)
                table.Columns.Add("st_"+i, typeof(string));

            table.Columns.Add("sum_insaldo", typeof(string));
            table.Columns.Add("sum_tarif", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_peni_money", typeof(string));
            table.Columns.Add("sum_money_all", typeof(string));
            table.Columns.Add("sum_outsaldo", typeof(string));
            table.Columns.Add("sum_peni", typeof(string));

            for (int i = 1; i < 21; i++)
            {
                table.Columns.Add("gilserv" + i, typeof(string));
                table.Columns.Add("gilsupp" + i, typeof(string));
                table.Columns.Add("gilmeasure" + i, typeof(string));
                table.Columns.Add("giltarif" + i, typeof(string));
                table.Columns.Add("gilsum_tarif" + i, typeof(string));
                table.Columns.Add("gilreval_charge" + i, typeof(string));
                table.Columns.Add("gilsum_lgota" + i, typeof(string));
                table.Columns.Add("gilsum_charge" + i, typeof(string));
            }
            table.Columns.Add("gilsum_tarif", typeof(string));
            table.Columns.Add("gilreval_charge", typeof(string));
            table.Columns.Add("gilsum_charge", typeof(string));

            table.Columns.Add("kommsum_tarif", typeof(string));
            table.Columns.Add("kommreval_charge", typeof(string));
            table.Columns.Add("kommsum_charge", typeof(string));

            for (int i = 1; i < 18; i++)
            {
                table.Columns.Add("countersserv" + i, typeof(string));
                table.Columns.Add("num_cnt" + i, typeof(string));
                table.Columns.Add("countersmeasure" + i, typeof(string));
                table.Columns.Add("dat_uchet" + i, typeof(string));
                table.Columns.Add("val_cnt1_" + i, typeof(string));
                table.Columns.Add("val_cnt2_" + i, typeof(string));
                table.Columns.Add("countersrashod" + i, typeof(string));
            }

            for (int i = 1; i < 15; i++)
            {
                table.Columns.Add("kommserv" + i, typeof(string));
                table.Columns.Add("kommsupp" + i, typeof(string));
                table.Columns.Add("kommmeasure" + i, typeof(string));
                table.Columns.Add("kommnorm" + i, typeof(string));
                table.Columns.Add("kommrash" + i, typeof(string));
                table.Columns.Add("kommtarif_eot" + i, typeof(string));
                table.Columns.Add("kommproc" + i, typeof(string));
                table.Columns.Add("kommtarif" + i, typeof(string));
                table.Columns.Add("kommsum_tarif" + i, typeof(string));
                table.Columns.Add("kommreval_charge" + i, typeof(string));
                table.Columns.Add("kommsum_charge" + i, typeof(string));
            }

            for (int i = 1; i < 15; i++)
            {
                table.Columns.Add("rasrserv" + i, typeof(string));
                table.Columns.Add("rasrsumcur" + i, typeof(string));
                table.Columns.Add("rasrsumprev" + i, typeof(string));
                table.Columns.Add("rasrsumproc" + i, typeof(string));
                table.Columns.Add("rasrproc" + i, typeof(string));
                table.Columns.Add("rasrsum_charge" + i, typeof(string));
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

        }

        protected override bool FillKvarPrm(DataRow dr)
        {
            if (dr == null) return false;
            
            dr["kolgil2"] = CountRegisterGil;
            dr["kolgil"] = CountGil;
            if (Pkod.IndexOf(".") > -1)
                dr["pkod"] = Pkod.Substring(0, Pkod.IndexOf("."));
            else
                dr["pkod"] = "158";
            dr["kv_pl"] = FullSquare;

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
            dr["st_1"] = Rekvizit.poluch2;
            dr["st_2"] = "исполнитель " + Rekvizit.poluch2 + ",";
            dr["st_3"] = "адрес " + Rekvizit.adres2;
            dr["st_4"] = "тел." + Rekvizit.phone2;
            dr["st_5"] = "р/с " + Rekvizit.rschet + " в ";
            dr["st_6"] = Rekvizit.bank2;
            dr["st_7"] = Rekvizit.poluch;
            dr["st_8"] = "ИНН-" + Rekvizit.inn;
            dr["st_9"] = "Р/с - " + Rekvizit.rschet;
            dr["st_10"] = "Кор/счет-" + Rekvizit.korr_schet;
            dr["st_11"] = "БИК-" + Rekvizit.bik;
            dr["st_12"] = Rekvizit.bank;
            dr["st_13"] = "01." + Month.ToString("00") + "." + Year.ToString();
            dr["st_14"] = "";
            dr["st_15"] = "";
            dr["months"] = FullMonthName;
            dr["month_num"] = Month;
            dr["area"] = Geu;
            dr["date_print"] = DateTime.Now.ToShortDateString();
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
            int gilIndex = 1;
            int kommIndex = 1;

            decimal gilsum_tarif = 0;
            decimal gilreval_charge= 0;
            decimal gilsum_charge = 0;

            decimal kommsum_tarif = 0;
            decimal kommreval_charge = 0;
            decimal kommsum_charge = 0;
            decimal kommsum_lgota = 0;
            bool hasGilHeader = false;
            string space = " ";

            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (IsShowServInGrid(ListServ[countServ]))
                {
                    if (ListServ[countServ].KommServ)
                    {
                        if (ListServ[countServ].Serv.NzpServ == 25)
                        {
                            dr["kommserv" + kommIndex] = "Электроснаб. на внутрикв. нужды, в т.ч.";
                            dr["kommsupp" + kommIndex] = "";
                            dr["kommmeasure" + kommIndex] = "";
                            dr["kommnorm" + kommIndex] = "";
                            dr["kommrash" + kommIndex] = "";
                            dr["kommtarif_eot" + kommIndex] = "";
                            dr["kommproc" + kommIndex] = "";
                            dr["kommtarif" + kommIndex] = "";
                            dr["kommsum_tarif" + kommIndex] = "";
                            dr["kommreval_charge" + kommIndex] = "";
                            dr["kommsum_charge" + kommIndex] = "";
                            kommIndex++;
                        }
                        
                        
                        dr["kommserv" + kommIndex] = ListServ[countServ].Serv.NameServ;
                        dr["kommsupp" + kommIndex] = ListServ[countServ].Serv.NameSupp;
                        dr["kommmeasure" + kommIndex] = ListServ[countServ].Serv.Measure;
                        dr["kommnorm" + kommIndex] = ListServ[countServ].Serv.Norma.ToString("0.0000");
                        dr["kommrash" + kommIndex] = ListServ[countServ].Serv.CCalc.ToString("0.00");
                        dr["kommtarif_eot" + kommIndex] = ""; 
                        dr["kommproc" + kommIndex] = "";
                        dr["kommtarif" + kommIndex] = ListServ[countServ].Serv.Tarif.ToString("0.00");
                        dr["kommsum_tarif" + kommIndex] = ListServ[countServ].Serv.SumTarif.ToString("0.00");
                        dr["kommreval_charge" + kommIndex] = (ListServ[countServ].Serv.RealCharge +
                            ListServ[countServ].Serv.Reval).ToString("0.00");
                        dr["kommsum_charge" + kommIndex] = ListServ[countServ].Serv.SumCharge.ToString("0.00");

                        kommsum_tarif = kommsum_tarif + ListServ[countServ].Serv.SumTarif;
                        kommreval_charge = kommreval_charge + ListServ[countServ].Serv.Reval +
                            ListServ[countServ].Serv.RealCharge;
                        kommsum_charge = kommsum_charge + ListServ[countServ].Serv.SumCharge;
                        kommsum_lgota = kommsum_lgota + ListServ[countServ].Serv.SumLgota;
                        kommIndex++;
                    }
                    else
                    {

                        if ((ListServ[countServ].Serv.NzpServ != 15) & 
                            (ListServ[countServ].Serv.NzpServ != 206)&
                            (hasGilHeader == false))
                        {
                            dr["gilserv" + gilIndex] = "Плата за содержание жил. помещения, в т.ч. ";
                            dr["gilsupp" + gilIndex] = "";
                            dr["gilmeasure" + gilIndex] = "";
                            dr["giltarif" + gilIndex] = "";
                            dr["gilsum_tarif" + gilIndex] = "";
                            dr["gilreval_charge" + gilIndex] = "";
                            dr["gilsum_lgota" + gilIndex] = "";
                            dr["gilsum_charge" + gilIndex] = "";
                            hasGilHeader = true;
                            gilIndex++;
                        }
                        if (hasGilHeader)
                            dr["gilserv" + gilIndex] = space + ListServ[countServ].Serv.NameServ;
                        else
                            dr["gilserv" + gilIndex] = ListServ[countServ].Serv.NameServ;
                            dr["gilsupp" + gilIndex] = ListServ[countServ].Serv.NameSupp;
                            dr["gilmeasure" + gilIndex] = ListServ[countServ].Serv.Measure;
                            dr["giltarif" + gilIndex] = ListServ[countServ].Serv.TarifF.ToString("0.00");
                            dr["gilsum_tarif" + gilIndex] = ListServ[countServ].Serv.SumTarif.ToString("0.00");
                            dr["gilreval_charge" + gilIndex] = (ListServ[countServ].Serv.RealCharge +
                            ListServ[countServ].Serv.Reval).ToString("0.00");
                            dr["gilsum_lgota" + gilIndex] = ListServ[countServ].Serv.SumLgota.ToString("0.00");
                            dr["gilsum_charge" + gilIndex] = ListServ[countServ].Serv.SumCharge.ToString("0.00");
                            gilsum_tarif = gilsum_tarif + ListServ[countServ].Serv.SumTarif;
                            gilreval_charge = gilreval_charge + ListServ[countServ].Serv.Reval +
                                ListServ[countServ].Serv.RealCharge;
                            gilsum_charge = gilsum_charge + ListServ[countServ].Serv.SumCharge;
                            gilIndex++;
                    }
                   
                }
            }


            for (int i = kommIndex; i < 15; i++)
            {
                dr["kommserv" + kommIndex] = "";
                dr["kommsupp" + kommIndex] = "";
                dr["kommmeasure" + kommIndex] = "";
                dr["kommnorm" + kommIndex] = "";
                dr["kommrash" + kommIndex] = "";
                dr["kommtarif_eot" + kommIndex] = "";
                dr["kommproc" + kommIndex] = "";
                dr["kommtarif" + kommIndex] = "";
                dr["kommsum_tarif" + kommIndex] = "";
                dr["kommreval_charge" + kommIndex] = "";
                dr["kommsum_charge" + kommIndex] = "";

            }

            for (int i = kommIndex; i < 21; i++)
            {
                dr["kommserv" + kommIndex] = "";
                dr["kommsupp" + kommIndex] = "";
                dr["kommmeasure" + kommIndex] = "";
                dr["kommnorm" + kommIndex] = "";
                dr["kommrash" + kommIndex] = "";
                dr["kommtarif_eot" + kommIndex] = "";
                dr["kommproc" + kommIndex] = "";
                dr["kommtarif" + kommIndex] = "";
                dr["kommsum_tarif" + kommIndex] = "";
                dr["kommreval_charge" + kommIndex] = "";
                dr["kommsum_charge" + kommIndex] = "";

            }

            dr["gilsum_tarif"] = gilsum_tarif.ToString("0.00");
            dr["gilreval_charge"] = gilreval_charge.ToString("0.00");
            dr["gilsum_charge"] = gilsum_charge.ToString("0.00");

            dr["kommsum_tarif"] = kommsum_tarif.ToString("0.00");
            dr["kommreval_charge"] = kommreval_charge.ToString("0.00");
            dr["kommsum_charge"] = kommsum_charge.ToString("0.00");


            return true;
        }

        /// <summary>
        /// Заполнение таблицы по рассрочке
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override bool FillRassrochka(DataRow dr)
        {
            for (int i = 1; i < 15; i++)
            {
                dr["rasrserv" + i] = "";
                dr["rasrsumcur" + i] = "";
                dr["rasrsumprev" + i] = "";
                dr["rasrsumproc" + i] = "";
                dr["rasrproc" + i] = "";
                dr["rasrsum_charge" + i] = "";
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
            bool hasHeader = false;
            for (int i = 0; i < ListCounters.Count; i++)
            {
                if (((ListCounters[i].NzpServ == 25)||
                    (ListCounters[i].NzpServ == 210))&
                    (hasHeader == false))
                {
                    dr["countersserv" + countersIndex] = "Электроснаб. на внутрикв. нужды, в т.ч.";
                    hasHeader = true;
                    countersIndex++;
                }
                dr["countersserv" + countersIndex] = ListCounters[i].ServiceName;
                dr["num_cnt" + countersIndex] = ListCounters[i].NumCounters;
                dr["countersmeasure" + countersIndex] = "";
                switch (ListCounters[i].NzpServ)
                {
                    case 6: dr["countersmeasure" + countersIndex] = "м3"; break;
                    case 7: dr["countersmeasure" + countersIndex] = "м3"; break;
                    case 8: dr["countersmeasure" + countersIndex] = "Гкал"; break;
                    case 9: dr["countersmeasure" + countersIndex] = "м3"; break;
                    case 14: dr["countersmeasure" + countersIndex] = "м3"; break;
                    case 10: dr["countersmeasure" + countersIndex] = "м3"; break;
                    case 281: dr["countersmeasure" + countersIndex] = "м3"; break;
                    case 325: dr["countersmeasure" + countersIndex] = "м3"; break;
                    case 25: dr["countersmeasure" + countersIndex] = "кВтч"; break;
                    case 210: dr["countersmeasure" + countersIndex] = "кВтч"; break;
                    case 322: dr["countersmeasure" + countersIndex] = "кВтч"; break;
                    case 515: dr["countersmeasure" + countersIndex] = "кВтч"; break;
                    case 516: dr["countersmeasure" + countersIndex] = "кВтч"; break;
                }


                dr["dat_uchet" + countersIndex] = ListCounters[i].DatUchet.ToShortDateString();
                dr["val_cnt1_" + countersIndex] = ListCounters[i].ValuePred;
                dr["val_cnt2_" + countersIndex] = ListCounters[i].Value;

                if (ListCounters[i].ValuePred>ListCounters[i].Value)
                {
                    dr["countersrashod" + countersIndex] = ListCounters[i].Value - ListCounters[i].ValuePred +
                        System.Convert.ToDecimal(System.Math.Pow(10, ListCounters[i].CntStage));
                }
                else
                {
                    dr["countersrashod" + countersIndex] = ListCounters[i].Value - ListCounters[i].ValuePred;
                }
                countersIndex++;
            }


            for (int i = countersIndex; i < 16; i++)
            {
                dr["countersserv" + i] = "";
                dr["num_cnt" + i] = "";
                dr["countersmeasure" + i] = "";
                dr["dat_uchet" + i] = "";
                dr["val_cnt1_" + i] = "";
                dr["val_cnt2_" + i] = "";
                dr["countersrashod" + i] = "";
            }
            return true;
        }


        protected override bool FillAdr(DataRow dr)
        {
            if (dr == null) return false;
            dr["fio"] = PayerFio;
            dr["adress"] = Ulica + " д. " + NumberDom + " кв." + NumberFlat;
            return true;
        }

        protected override bool FillBarcode(DataRow dr)
        {
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
            dr["sum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["reval_charge"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["sum_charge"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_outsaldo"] = SummaryServ.Serv.SumOutsaldo.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_peni"] = "0.00";
            dr["sum_money_all"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_peni_money"] = "0.00";
            return true;
        }


        

        public SahaFaktura() : 
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
            FakturaBlocks.HasRassrochka = true;
            FakturaBlocks.HasCountersBlock = false;
            FakturaBlocks.HasCountersDoubleBlock = true;
            FakturaBlocks.HasNormblock = true;
            Clear();
    
        }
    }
   
}

