using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Faktura.Source.Base;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.FAKTURA
{
    public class TulaNewFaktura : BaseFactura2
    {
        private const int MaxRevalNumber = 9;
        private const int MaxCountersCount = 8;
        private const int MaxSupplierCount = 9;
        private const int MaxServCount = 20;

        public override string Name
        {
            get { return "Авансовый счет Тулы "; }
        }

        public override string Code {get { return "10107"; }}

        public override string FileName { get { return "tula354.frx"; } }

        /// <summary>
        /// Заполнение квартирных парметров
        /// </summary>
        /// <returns></returns>
        protected override bool FillKvarPrm()
        {
            if (Dr == null) return false;
            Dr["priv"] = Kvar.Ownflat ? "Приватизирована" : "не приватизирована";
            Dr["kolgil2"] = Kvar.CountRegisterGil;
            Dr["kolgil"] = Kvar.CountGil;
            Dr["ls"] = Pkod.Substring(5, 5);
            Dr["num_ls"] = NumLs;

            Dr["ngeu"] = NzpGeu > 100
                ? NzpGeu.ToString(CultureInfo.InvariantCulture).Substring(1, 2)
                : Pkod.Substring(3, 2);
            Dr["ud"] = Ud;
            Dr["pkod"] = "13";
            Dr["pl_dom"] = Dom.DomSquare.ToString("0.00");
            Dr["pl_mop"] = Dom.MopSquare.ToString("0.00");
            Dr["dom_gil"] = Dom.CountDomGil.ToString(CultureInfo.InvariantCulture);
            Dr["countGil"] = Kvar.CountGil + Kvar.CountArriveGil;
            Dr["countDepartureGil"] = Kvar.CountDepartureGil;
            Dr["Avans"] = (Charge.Avans ? "АВАНСОВЫЙ" : "");
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
            return "208 " + vars + BarcodeCrc(vars);
        }


        protected override bool FillBarcode()
        {
            Dr["vars"] = GetBarCode();
            return true;
        }


        /// <summary>
        /// Признак отображения услуги в таблице начислений
        /// </summary>
        /// <param name="aServ"></param>
        /// <returns></returns>
        public override bool IsShowServInGrid(BaseServ2 aServ)
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
        public override void FinalPass(STCLINE.KP50.Interfaces.Faktura finder)
        {
            base.FinalPass(finder);

            if (Charge.SummaryServ.Serv.SumCharge < 0) Charge.SummaryServ.Serv.SumCharge = 0;
            SumTicket = Charge.SummaryServ.Serv.SumCharge;
           
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
            Dr["rajon"] = Rajon;
            Dr["ulica"] = Ulica;
            Dr["numdom"] = NumberDom;
            if (NumberFlat == "-" || NumberFlat.Trim() == "" || NumberFlat.Trim() == "0")
            {
                Dr["kvnum"] = "";
            }
            else
            {
                Dr["kvnum"] = ", кв. " + NumberFlat;
            }
            string index = (Indecs != "" && Indecs != "-" && Indecs != null ? Indecs + ", " : "");
            string street = (Ulica != "-" && Ulica != "" ? Ulica + ", " : Ulica);
            string district = (Rajon != "" && Rajon != "-" && Rajon != null ? Rajon + ", " : "");
            string city = (Town != "" && Town != "-" && Town != null ? Town + ", " : "");
            string flat = (NumberFlat != "" && NumberFlat != "-" && NumberFlat != null ? " кв." + NumberFlat : "");
            string room = (NumberRoom != "" && NumberRoom != "-" && NumberRoom != null ? " ком." + NumberRoom : "");
            Dr["adres"] = index + city + district + street + " д. " + NumberDom + flat + room;

            Dr["kv_pl"] = Kvar.FullSquare.ToString("0.00");
            Dr["type_pl"] = "общая";
            return true;
        }


        /// <summary>
        /// Форматирования значения для вывода объема по услуге на экран
        /// </summary>
        /// <param name="aValue"></param>
        /// <param name="colName"></param>
        protected void FillGoodServVolume(decimal aValue, string colName)
        {

            if (Math.Abs(aValue) < 0.001m)
            {
                Dr[colName] = "";
            }
            else
            {

                if (colName.IndexOf("rash_pu", StringComparison.Ordinal) > -1)
                    Dr[colName] = aValue.ToString("0.00");
                else
                    Dr[colName] = aValue.ToString("0.00##");
            }
        }

        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="index"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        protected bool FillServiceVolumeLs(int index, int nzpServ)
        {
            ServVolume2 lsNorm = ServVolumeLs.GetServVolume(nzpServ);
            if (lsNorm != null)
            {
                Dr["c_calc" + index] = lsNorm.Volume.ToString("0.00##");
                Dr["c_calc_odn" + index] = lsNorm.NzpServ == 7 ? "x" : lsNorm.Odn.ToString("0.0000");

                if (lsNorm.IsPu == ServVolume2.VolumeType.Counter)
                    FillGoodServVolume(lsNorm.Volume, "rash_pu" + index);

                if ((nzpServ == 25) & (lsNorm.Volume == 0))
                {
                }
                {
                    FillGoodServVolume(lsNorm.Normativ, "rash_norm" + index);
                }

            }
            return true;
        }

        protected bool FillServiceVolumeDom(int index, int nzpServ)
        {
            ServVolumeDom domNorm = ServVolumeDom.GetServVolume(nzpServ);
            if (domNorm != null)
            {
                if (domNorm.IsPu != Base.ServVolumeDom.VolumeType.Normativ)
                {
                    FillGoodServVolume(domNorm.Volume + domNorm.Odn, "rash_dpu" + index);
                    FillGoodServVolume(domNorm.Volume, "rash_dpu_pu" + index);
                    FillGoodServVolume(domNorm.Odn, "rash_dpu_odn" + index);
                    FillGoodServVolume(domNorm.NormOdn, "rash_norm_odn" + index);
                }
            }

            return true;
        }

        protected override bool FillRemark()
        {
            Dr["remark"] = Area.AreaRemark;
            return true;
        }

        /// <summary>
        /// Отображение особых услуг в заголовке таблицы
        /// </summary>
        /// <param name="nzpServ"></param>
        /// <param name="index"></param>
        /// <param name="defaultName"></param>
        protected void ShowSpecServ(int nzpServ, int index, string defaultName)
        {
            Dr["name_serv" + index] = defaultName;
            if (Charge.ListServ.ContainsKey(nzpServ))
            {
                SumServ2 t = Charge.ListServ[nzpServ].Serv;
                Dr["name_serv" + index] = t.NameServ;
                Dr["rsum_tarif_all" + index] = t.RsumTarif.ToString("0.00");
                Dr["tarif" + index] = t.Tarif.ToString("0.00");
                Dr["sum_dolg" + index] = (t.SumInsaldo - t.SumMoney).ToString("0.00");
                Dr["reval" + index] = (t.Reval + t.RealCharge).ToString("0.00");
                Dr["sum_charge_all" + index] = t.SumCharge.ToString("0.00");

            }
        }

        protected void ShowSimpleServ(string fieldName, int index, decimal sum,
             string defValue)
        {
            Dr[fieldName + index] = Math.Abs(sum) > 0.001m ? sum.ToString("0.00") : defValue;
        }

        /// <summary>
        /// Заполнение таблицы по поставщикам
        /// </summary>
        protected void FillSupplierGrid()
        {
            int index = 1;
            foreach (KeyValuePair<long, BaseServ2> t in Charge.ListSupp)
            {
                if (!IsShowServInGrid(t.Value)) continue;
                if (index < 9)
                {
                    //Если выбран дом с непосредственным управлением или управление не выбрано, то услуга значится как "Содержание и текущий ремонт многоквартирного дома" 
                    if (NzpArea == 1003 || NzpArea == 1004 || NzpArea == 1016) t.Value.Serv.NameServ = t.Value.Serv.NameServ.Replace("Содерж. и ремонт жилого помещения, управление МКД", "Содержание и текущий ремонт многоквартирного дома");
                    Dr["supp_serv" + index] = t.Value.Serv.NameServ.TrimEnd(',');
                    Dr["supp_name" + index] = t.Value.Serv.Payer.payer;
                    Dr["supp_inn" + index] = t.Value.Serv.Payer.inn;
                    Dr["supp_summ" + index] = t.Value.Serv.SumCharge.ToString("0.00");
                }

                index++;
            }
        }


        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <returns></returns>
        protected override bool FillMainChargeGrid()
        {
            if (Dr == null) return false;
            FillSupplierGrid();
            ServVolumeDom.GetServNormativ(Pref, NzpDom);
            ServVolumeLs.GetLsServNormativ(Pref, NzpKvar, false);
            //SetServRashod();
            //charge.listServ.Sort();

            //Капитальный ремонт
            ShowSpecServ(2, 1, "Содержание жилья");
            //Капитальный ремонт
            ShowSpecServ(206, 2, "Капитальный ремонт");
            //Вывоз ТБО
            ShowSpecServ(16, 3, "Вывоз ТБО");
            //Найм
            ShowSpecServ(15, 4, "Найм");

            int[] specServ = { 2, 206, 15, 16 };

            int numberString = 5;

            foreach (KeyValuePair<int, BaseServ2> t in Charge.ListServ)
            {
                BaseServ2 bs = t.Value;
                if ((IsShowServInGrid(bs)) & (!specServ.Contains(bs.Serv.NzpServ)))
                {

                    Dr["name_serv" + numberString] = bs.Serv.NameServ.Trim();
                    Dr["measure" + numberString] = bs.Serv.Measure.Trim();
                    ShowSimpleServ("tarif", numberString, bs.Serv.Tarif, "");
                    ShowSimpleServ("rsum_tarif", numberString, bs.Serv.RsumTarif - bs.ServOdn.RsumTarif, "");
                    ShowSimpleServ("rsum_tarif_odn", numberString, bs.ServOdn.RsumTarif, "");
                    ShowSimpleServ("rsum_tarif_all", numberString, bs.Serv.RsumTarif, "");
                    ShowSimpleServ("reval", numberString, bs.Serv.Reval + bs.Serv.RealCharge, "");
                    ShowSimpleServ("sum_dolg", numberString, bs.Serv.SumInsaldo - bs.Serv.SumMoney, "");
                    ShowSimpleServ("sum_charge", numberString, bs.Serv.SumCharge - bs.ServOdn.SumCharge, "");
                    ShowSimpleServ("sum_charge_odn", numberString, bs.ServOdn.SumCharge, "");
                    ShowSimpleServ("sum_charge_all", numberString, bs.Serv.SumCharge, "");

                    Dr["sum_lgota" + numberString] = "";
                    FillServiceVolumeLs(numberString, bs.Serv.NzpServ);
                    FillServiceVolumeDom(numberString, bs.Serv.NzpServ);

                    numberString++;
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
            table.Columns.Add("vib_kolgil", typeof(string));
            table.Columns.Add("countGil", typeof(string));
            table.Columns.Add("countDepartureGil", typeof(string));
            table.Columns.Add("countArriveGil", typeof(string));
            table.Columns.Add("Avans", typeof(string));

            for (int i = 1; i < MaxServCount; i++)
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


            for (int i = 1; i < MaxSupplierCount; i++)
            {
                table.Columns.Add("supp_serv" + i, typeof(string));
                table.Columns.Add("supp_name" + i, typeof(string));
                table.Columns.Add("supp_inn" + i, typeof(string));
                table.Columns.Add("supp_summ" + i, typeof(string));

            }

            for (int i = 1; i < MaxRevalNumber; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
            }


            for (int i = 1; i <= MaxCountersCount; i++)
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
            Dr["rsum_tarif_all"] = summaryServ.Serv.RsumTarif.ToString("0.00");
            Dr["rsum_tarif_odn"] = summaryServ.ServOdn.RsumTarif.ToString("0.00");
            Dr["rsum_tarif"] = (summaryServ.Serv.RsumTarif - summaryServ.ServOdn.RsumTarif).ToString("0.00");
            Dr["sum_tarif"] = summaryServ.Serv.SumTarif.ToString("0.00");
            Dr["reval"] = summaryServ.Serv.Reval.ToString("0.00");
            Dr["reval_charge"] = (summaryServ.Serv.Reval + summaryServ.Serv.RealCharge).ToString("0.00");
            Dr["real_charge"] = summaryServ.Serv.RealCharge.ToString("0.00");
            Dr["sum_charge_all"] = summaryServ.Serv.SumCharge.ToString("0.00");
            Dr["sum_charge"] = (summaryServ.Serv.SumCharge - summaryServ.ServOdn.SumCharge).ToString("0.00");
            Dr["sum_charge_odn"] = summaryServ.ServOdn.SumCharge.ToString("0.00");
            Dr["sum_insaldo"] = summaryServ.Serv.SumInsaldo.ToString("0.00");
            if (summaryServ.Serv.SumInsaldo - summaryServ.Serv.SumMoney > 0)
            {
                Dr["sum_dolg"] = (summaryServ.Serv.SumInsaldo - summaryServ.Serv.SumMoney).ToString("0.00");
                Dr["sum_avans"] = "0.00";
            }
            else
            {
                Dr["sum_dolg"] = "0.00";
                Dr["sum_avans"] = (summaryServ.Serv.SumInsaldo - summaryServ.Serv.SumMoney).ToString("0.00");
            }
            Dr["sum_money"] = summaryServ.Serv.SumMoney.ToString("0.00");
            Dr["sum_peni"] = "0.00";
            Dr["sum_ticket"] = SumTicket.ToString("0.00");
            Dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            Dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");

            if (summaryServ.Serv.SumOutsaldo < 0)
            {
                Dr["sum_next_avans"] = summaryServ.Serv.SumOutsaldo;
            }
            else
            {
                Dr["sum_next_avans"] = "0.00";
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
                Dr["dat_opl"] = Payments.DateOplat + "г, ";
                Dr["sum_last_opl"] = Payments.LastSumOplat + " руб.";
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
                    FakturaReval fr = Reval.ListReval.First(x => x.NzpServ == bs.Value.Serv.NzpServ);
                    if ((rowNumber < MaxRevalNumber) & (fr != null))
                    {

                        Dr["period_pere" + rowNumber] = "";
                        Dr["serv_pere" + rowNumber] = bs.Value.Serv.NameServ;
                        Dr["osn_pere" + rowNumber] = fr.Reason;
                        Dr["period_pere" + rowNumber] = fr.ReasonPeriod;
                        Dr["sum_pere" + rowNumber] = bs.Value.Serv.Reval;
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
            Dr["poluch2"] = rekvizit.poluch2;
            Dr["poluch2_adres"] = rekvizit.adres2;
            Dr["poluch2_rs"] = rekvizit.rschet2;
            Dr["poluch2_bank"] = rekvizit.bank2;
            Dr["poluch2_inn"] = rekvizit.inn2;
            Dr["poluch2_ks"] = rekvizit.korr_schet2;
            Dr["poluch2_phone"] = rekvizit.phone2;



            Dr["Data_dolg"] = "01." + Month.ToString("00") + "." + Year;
            Dr["month_"] = Month;
            Dr["year_"] = Year;
            Dr["months"] = FullMonthName;

            if (NumberFlat.IndexOf("комн.", StringComparison.Ordinal) >= 0)
            {
                Dr["kvnum"] = NumberFlat.Replace("комн.", "(") + ")";
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
            Counters.LoadDoubleLsCounters(Pref, NzpKvar, Pkod);
            for (int i = 0; i < Math.Min(8, Counters.ListCounters.Count); i++)
            {
                switch (Counters.ListCounters[i].NzpServ)
                {
                    case 6: Dr["lsserv" + countersIndex] = countersIndex + ". х/в " +
                        Counters.ListCounters[i].Place.Trim(); break;
                    case 9: Dr["lsserv" + countersIndex] = countersIndex + ". г/в " +
                        Counters.ListCounters[i].Place.Trim(); break;
                    case 8: Dr["lsserv" + countersIndex] = countersIndex + ". Отопл. " +
                        Counters.ListCounters[i].Place.Trim(); break;
                    case 25: Dr["lsserv" + countersIndex] = countersIndex + ". Эл.снаб. " +
                        Counters.ListCounters[i].Place.Trim(); break;
                    case 210: Dr["lsserv" + countersIndex] = countersIndex + ". Ноч.Эл. " +
                        Counters.ListCounters[i].Place.Trim(); break;
                    default: Dr["lsserv" + countersIndex] = countersIndex + ". " +
                        Counters.ListCounters[i].ServiceName; break;
                }
                Dr["lsnumcnt" + countersIndex] = Counters.ListCounters[i].NumCounter;
                Dr["lsdatuchet2_" + countersIndex] = Counters.ListCounters[i].DatUchet.ToShortDateString();
                Dr["lsvalcnt2_" + countersIndex] = Counters.ListCounters[i].Value.ToString("0.00");
                countersIndex++;
            }

            return true;
        }

        /// <summary>
        /// Заполнение домовых счетчиков
        /// </summary>
        /// <returns></returns>
        public override bool FillDomCounters()
        {
            int countersIndex = 1;
            Counters.LoadDoubleDomCounters(Pref, NzpDom);
            for (int i = 0; i < Math.Min(MaxCountersCount, Counters.ListDomCounters.Count); i++)
            {
                switch (Counters.ListDomCounters[i].NzpServ)
                {
                    case 6: Dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Хол. вода"; break;
                    case 9: Dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Гор. вода"; break;
                    case 8: Dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Отопл."; break;
                    case 25: Dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Эл.снаб."; break;
                    case 210: Dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Ноч.Эл.."; break;
                    default: Dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". " + Counters.ListDomCounters[i].ServiceName; break;
                }

                Dr["domnumcnt" + countersIndex] = Counters.ListDomCounters[i].NumCounter;
                Dr["domdatuchet1_" + countersIndex] = Counters.ListDomCounters[i].DatUchetPred.ToShortDateString();
                Dr["domdatuchet2_" + countersIndex] = Counters.ListDomCounters[i].DatUchet.ToShortDateString();
                Dr["domvalcnt1_" + countersIndex] = Counters.ListDomCounters[i].ValuePred.ToString("0.00##");
                Dr["domvalcnt2_" + countersIndex] = Counters.ListDomCounters[i].Value.ToString("0.00##");
                countersIndex++;
            }
            return true;
        }


    }


}

