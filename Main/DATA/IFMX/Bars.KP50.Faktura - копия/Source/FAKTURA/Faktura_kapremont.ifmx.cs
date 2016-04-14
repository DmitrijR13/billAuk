
using System.Globalization;

namespace Bars.KP50.DB.Faktura
{

    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;


    public class KapremontFaktura : BaseFactura
    {
        private const int MaxStringCount = 3;
        private const int MaxPereCount = 7;

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

            dr["pkod"] = PkodKapr.Replace(".0", "");
            dr["pl_dom"] = DomSquare.ToString("0.00");
            dr["pl_mop"] = MopSquare.ToString("0.00");
            return true;
        }

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

        public override string GetBarCode()
        {         

            string vars = "00" + PkodKapr.Substring(0,13)+ Month.ToString("D2") +
             (Year - 2000).ToString("D2") +
             (Math.Max(0, SumTicket) * 100).ToString("000000000");
            Shtrih = vars + Utils.BarcodeCrcSamara(vars);
            GeuKodErc = "630100000015";
            return Shtrih;
      
        }

        public override bool IsShowServInGrid(BaseServ aServ)
        {
            if (
                (Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
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

            SumTicket = SummaryServ.Serv.SumCharge + SummaryServ.Serv.SumInsaldo-SummaryServ.Serv.SumMoney;
        }

        protected override bool FillAdr(DataRow dr)
        {
            if (dr == null) return false;
            dr["fio"] = PayerFio;
            string index = (Indecs != "" && Indecs != "-" ? Indecs + ", " : "");
            string street = (Ulica != "-" && Ulica != "" ? Ulica + ", " : Ulica);
            string district = (Rajon != "" && Rajon != "-" && Rajon != null ? Rajon + ", " : "");
            string city = (Town != "" && Town != "-" && Town != null ? Town + ", " : "");
            dr["ulica"] = index + city + district + street;
            dr["numdom"] = NumberDom;
            dr["kvnum"] = NumberFlat;
            dr["adress"] = index + city + district + street + " д. " + NumberDom + " кв." + NumberFlat;
            dr["typek"] = Typek;
            if (IsolateFlat)
            {
                dr["kv_pl"] = FullSquare.ToString("0.00");
                if (FullSquare.ToString("0.000") == "59.999") dr["utoch_pl"] = "Значение общей площади требует уточнения!";
                dr["type_pl"] = "общая";
            }
            else
            {
                dr["kv_pl"] = LiveSquare.ToString("0.00");
                if (LiveSquare.ToString("0.000") == "59.999") dr["utoch_pl"] = "Значение общей площади требует уточнения!";
                dr["type_pl"] = "жилая";
            }
            return true;
        }

        protected override bool FillServiceVolume(DataRow dr)
        {
            if (dr == null) return false;
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
            dr["str_rekv1"] = Rekvizit.poluch2;
            dr["str_rekv2"] = "исполнитель " + Rekvizit.poluch2 + ", адрес " +
                            Rekvizit.adres2 + ", тел." + Rekvizit.phone2 +
                        "; р/с " + Rekvizit.rschet2 + " в " + Rekvizit.bank2;
            dr["str_rekv3"] = Rekvizit.poluch + " ИНН-" + Rekvizit.inn;
            dr["str_rekv4"] = "Р/с - " + Rekvizit.rschet + "   Кор/счет-" + Rekvizit.korr_schet + "  " +
                        "  БИК-" + Rekvizit.bik + " " + Rekvizit.bank;

            dr["month_"] = Month;
            dr["year_"] = Year;
            dr["months"] = FullMonthName;
            dr["poluch"] = Rekvizit.poluch;
            dr["rs_poluch"] = Rekvizit.rschet;
            dr["bank_poluch"] = Rekvizit.bank;
            dr["ks_poluch"] = Rekvizit.korr_schet;
            dr["inn_poluch"] = Rekvizit.inn;
            dr["bik_poluch"] = Rekvizit.bik;
            dr["adres_poluch"] = Rekvizit.adres;
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
            SetServRashod();
            int numberString = 1;
            for (int countServ = 0; countServ < Math.Min(MaxStringCount, ListServ.Count); countServ++)
            {
                if (IsShowServInGrid(ListServ[countServ]))
                {
                    dr["name_serv" + numberString] = ListServ[countServ].Serv.NameServ.Trim() +
                        ListServ[countServ].Serv.NameSupp.Trim();
                    dr["measure" + numberString] = ListServ[countServ].Serv.Measure.Trim();



                    dr["c_calc" + numberString] = ListServ[countServ].Serv.CCalc.ToString("0.00");


                    dr["tarif" + numberString] = ListServ[countServ].Serv.Tarif.ToString("0.00");


                    dr["rsum_tarif" + numberString] = ListServ[countServ].Serv.RsumTarif.ToString("0.00");
                    dr["rsum_tarif_all" + numberString] = ListServ[countServ].Serv.RsumTarif.ToString("0.00");
                    dr["reval" + numberString] = (ListServ[countServ].Serv.Reval +
                        ListServ[countServ].Serv.RealCharge).ToString("0.00");

                    dr["sum_money" + numberString] = ListServ[countServ].Serv.SumMoney.ToString("0.00");
                    dr["sum_dolg" + numberString] = (ListServ[countServ].Serv.SumInsaldo -
                        ListServ[countServ].Serv.SumMoney).ToString("0.00");

                    dr["sum_charge_all" + numberString] = ListServ[countServ].Serv.SumCharge.ToString("0.00");
                    dr["sum_charge" + numberString] = ListServ[countServ].Serv.SumCharge.ToString("0.00");

                    numberString++;
                }
            }
          
            for (int i = numberString; i < 4; i++)
            {
                dr["name_serv" + i] = "";
                dr["measure" + i] = "";
                dr["c_calc" + i] = "";
                dr["tarif" + i] = "";
                dr["rsum_tarif" + i] = "";
                dr["rsum_tarif_all" + i] = "";
                dr["reval" + i] = "";
                dr["sum_lgota" + i] = "";
                dr["sum_charge_all" + i] = "";
                dr["sum_charge" + i] = "";
                dr["sum_nedop" + i] = "";
                dr["sum_outsaldo" + i] = "";
                dr["real_charge" + i] = "";

            }
            //if (ListServ.Count == 0)
            //{
            //    dr["tarif1"] = TarifKapr;
            //    dr["c_calc1"] = FullSquare.ToString("0.00");
            //    dr["rsum_tarif_all1"] = (Convert.ToDecimal(TarifKapr) * FullSquare).ToString("0.00");
            //    dr["sum_charge_all1"] = "0.00";
            //    dr["reval1"] = "0.00";
            //}
            //dr["sum_charge_all1"] = "0.00";

            return true;
        }

        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public override DataTable MakeTable()
        {
            DataTable table = new DataTable();
            table.TableName = "Q_master";
            table.Columns.Add("str_rekv1", typeof(string));
            table.Columns.Add("str_rekv2", typeof(string));
            table.Columns.Add("str_rekv3", typeof(string));
            table.Columns.Add("str_rekv4", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("typek", typeof(string));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("adress", typeof(string));
            table.Columns.Add("date_print", typeof(string));
            table.Columns.Add("datedestv", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("type_pl", typeof(string));
            table.Columns.Add("priv", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("geu_remark", typeof(string));
            table.Columns.Add("area_remark", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("rsum_tarif_all", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
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
            table.Columns.Add("real_charge", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));

            table.Columns.Add("poluch", typeof(string));
            table.Columns.Add("rs_poluch", typeof(string));
            table.Columns.Add("bank_poluch", typeof(string));
            table.Columns.Add("ks_poluch", typeof(string));
            table.Columns.Add("inn_poluch", typeof(string));
            table.Columns.Add("bik_poluch", typeof(string));
            table.Columns.Add("adres_poluch", typeof(string));

            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("pl_mop", typeof(string));
            table.Columns.Add("utoch_pl", typeof(string));

            for (int i = 1; i < MaxStringCount+1; i++)
            {
                table.Columns.Add("name_serv" + i, typeof(string));
                table.Columns.Add("measure" + i, typeof(string));
                table.Columns.Add("c_calc" + i, typeof(string));
                table.Columns.Add("sum_dolg" + i, typeof(string));
                table.Columns.Add("sum_money" + i, typeof(string));
                table.Columns.Add("tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif_all" + i, typeof(string));
                table.Columns.Add("reval" + i, typeof(string));
                table.Columns.Add("sum_lgota" + i, typeof(string));
                table.Columns.Add("sum_charge_all" + i, typeof(string));
                table.Columns.Add("sum_charge" + i, typeof(string));
                table.Columns.Add("sum_nedop" + i, typeof(string));
                table.Columns.Add("sum_outsaldo" + i, typeof(string));
                table.Columns.Add("real_charge" + i, typeof(string));

            }

            for (int i = 1; i < MaxPereCount; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
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
            dr["rsum_tarif"] = (SummaryServ.Serv.RsumTarif - SummaryServ.ServOdn.RsumTarif).ToString("0.00");
            dr["sum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["reval"] = SummaryServ.Serv.Reval.ToString("0.00");
            dr["reval_charge"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["real_charge"] = SummaryServ.Serv.RealCharge.ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_charge"] = (SummaryServ.Serv.SumCharge - SummaryServ.ServOdn.SumCharge).ToString("0.00");
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00"); 
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            dr["date_print"] = DateTime.Now.ToShortDateString();
            dr["datedestv"] = DateTime.Now.ToString("28.MM.yyyy");
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            return true;
        }


        public KapremontFaktura()
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
            FakturaBlocks.HasCountersBlock = false;
            FakturaBlocks.HasCountersDoubleBlock = false;
            FakturaBlocks.HasCountersDoubleDomBlock = false;
            FakturaBlocks.HasOdnBlock = false;
            FakturaBlocks.HasRdnBlock = false;
            FakturaBlocks.HasPerekidkiSamaraBlock = false;
            FakturaBlocks.HasRemarkblock = true;
            FakturaBlocks.HasGilPeriodsBlock = false;
            FakturaBlocks.HasCountersSpisBlock = false;
            FakturaBlocks.HasSupplierPkod = true;
            FakturaBlocks.HasSupplierblock = true;
            Clear();
        }

    }




}

