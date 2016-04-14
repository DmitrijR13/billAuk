using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Faktura.Source.Base;

namespace Bars.KP50.Faktura.Saha
{
    public class FakturaSaha : BaseFactura2
    {

        public override string Name
        {
            get { return "Саха"; }
        }


        public override string Code { get { return "1004"; } }

        public override string FileName { get { return "saha.frx"; } }

        private readonly int[] _kommServ = { 6, 7, 8, 9, 14, 25, 210, 510, 511, 512, 513, 514, 515, 516, 517 };

        private int _currentRow = 1;

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





        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public override DataSet MakeFewTables()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("number", typeof(int));
            //table.Columns.Add("months", typeof(string));
            //table.Columns.Add("month_from", typeof(string));
            //table.Columns.Add("month_to", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("month_num", typeof(string));
            table.Columns.Add("date_print", typeof(string));
            table.Columns.Add("reciever_address", typeof(string));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("sum_charge_date", typeof(decimal));
            table.Columns.Add("sum_peni", typeof(decimal));
            table.Columns.Add("sum_charge_all", typeof(decimal));
            table.Columns.Add("area", typeof(string));
            table.Columns.Add("poluch", typeof(string));
            table.Columns.Add("bank", typeof(string));
            table.Columns.Add("rschet", typeof(string));
            table.Columns.Add("kschet", typeof(string));
            table.Columns.Add("bik", typeof(string));
            table.Columns.Add("inn", typeof(string));
            table.Columns.Add("phone", typeof(string));
            table.Columns.Add("address", typeof(string));
            table.Columns.Add("note", typeof(string));
            table.Columns.Add("poluch2", typeof(string));
            table.Columns.Add("bank2", typeof(string));
            table.Columns.Add("rschet2", typeof(string));
            table.Columns.Add("kschet2", typeof(string));
            table.Columns.Add("bik2", typeof(string));
            table.Columns.Add("inn2", typeof(string));
            table.Columns.Add("phone2", typeof(string));
            table.Columns.Add("address2", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("kolreg", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("sum_insaldo", typeof(decimal));
            table.Columns.Add("sum_tarif", typeof(decimal));
            table.Columns.Add("reval_charge", typeof(decimal));
            table.Columns.Add("sum_charge", typeof(decimal));
            table.Columns.Add("sum_money", typeof(decimal));
            table.Columns.Add("sum_outsaldo", typeof(decimal));
            table.Columns.Add("sum_peni_money", typeof(decimal));
            table.Columns.Add("sum_money_all", typeof(decimal));


            var table1 = new DataTable { TableName = "Q_master1" };
            table1.Columns.Add("number", typeof(int));
            table1.Columns.Add("months", typeof(string));
            table1.Columns.Add("month_from", typeof(string));
            table1.Columns.Add("month_to", typeof(string));
            table1.Columns.Add("gil_serv", typeof(string));
            table1.Columns.Add("gil_supp", typeof(string));
            table1.Columns.Add("gil_measure", typeof(string));
            table1.Columns.Add("gil_tarif", typeof(decimal));
            table1.Columns.Add("gil_sum_tarif", typeof(decimal));
            table1.Columns.Add("gil_reval", typeof(decimal));
            table1.Columns.Add("gil_sum_lgota", typeof(decimal));
            table1.Columns.Add("gil_sum_charge", typeof(decimal));

            var table2 = new DataTable { TableName = "Q_master2" };
            table2.Columns.Add("number", typeof(int));
            table2.Columns.Add("counters_serv", typeof(string));
            table2.Columns.Add("counters_num", typeof(string));
            table2.Columns.Add("counters_measure", typeof(string));
            table2.Columns.Add("counters_date", typeof(DateTime));
            table2.Columns.Add("counters_val_pred", typeof(string));
            table2.Columns.Add("counters_val_curr", typeof(string));
            table2.Columns.Add("counters_rash", typeof(string));
            table2.Columns.Add("counters_correction_period", typeof(string));
            table2.Columns.Add("counters_correction_value", typeof(string));
            table2.Columns.Add("counters_all", typeof(string));

            var table3 = new DataTable { TableName = "Q_master3" };
            table3.Columns.Add("number", typeof(int));
            table3.Columns.Add("komm_serv", typeof(string));
            table3.Columns.Add("komm_supp", typeof(string));
            table3.Columns.Add("komm_measure", typeof(string));
            table3.Columns.Add("komm_norm", typeof(string));
            table3.Columns.Add("komm_vol", typeof(string));
            table3.Columns.Add("komm_tarif_eot", typeof(string));
            table3.Columns.Add("komm_percent", typeof(string));
            table3.Columns.Add("komm_tarif", typeof(string));
            table3.Columns.Add("komm_sum_tarif", typeof(decimal));
            table3.Columns.Add("komm_reval", typeof(decimal));
            table3.Columns.Add("komm_sum_charge", typeof(decimal));
            
            var ds = new DataSet();
            ds.Tables.Add(table);
            ds.Tables.Add(table1);
            ds.Tables.Add(table2);
            ds.Tables.Add(table3);

            return ds;
        }

        public override bool FillTables(DataSet ds)
        {
            _currentRow = ds.Tables[0].Rows.Count;
            Dr = ds.Tables[0].NewRow();
            FillHead();
            ds.Tables[0].Rows.Add(Dr);

            ServVolumeDom.GetServNormativ(Pref, NzpDom);
            ServVolumeLs.GetLsServNormativ(Pref, NzpKvar, false);

            ds = FillNotCommServices(ds);
            ds = FillCounters(ds);
            ds = FillCommServices(ds);

            return true;
        }

        private void FillHead()
        {
            Dr["number"] = _currentRow;
            Dr["pkod"] = Pkod;
            Dr["month_num"] = FullMonthName + "г.";
            Dr["reciever_address"] = Ulica + " д. " + NumberDom +
                            ((NumberFlat != "-" && NumberFlat.Trim() != "" && NumberFlat.Trim() != "0")
                                ? " кв. " + NumberFlat
                                : "") +
                            ((NumberRoom.Trim() != "-" && NumberRoom.Trim() != "" && NumberRoom.Trim() != "0")
                                ? "/" + NumberRoom
                                : "");
            Dr["date_print"] = DateTime.Now.ToShortDateString();
            Dr["fio"] = Kvar.PayerFio;
            Dr["area"] = Area.AreaName;
            Dr["poluch"] = Area.PerformerReciever;
            Dr["bank"] = String.IsNullOrEmpty(Area.PerformerBank) ? "" : " Банк: " + Area.PerformerBank;
            Dr["rschet"] = String.IsNullOrEmpty(Area.PerformerCurrentAccount) ? "" : " Расч. Счет: " + Area.PerformerCurrentAccount;
            Dr["kschet"] = String.IsNullOrEmpty(Area.PerformerCorrelationAccount) ? "" : " Корр. Счет: " + Area.PerformerCorrelationAccount;
            Dr["bik"] = String.IsNullOrEmpty(Area.PerformerBik) ? "" : " БИК: " + Area.PerformerBik;
            Dr["inn"] = String.IsNullOrEmpty(Area.PerformerInn) ? "" : " ИНН/КПП: " + Area.PerformerInn;
            Dr["phone"] = String.IsNullOrEmpty(Area.PerformerPhone) ? "" : " Тел.: " + Area.PerformerPhone;
            Dr["address"] = String.IsNullOrEmpty(Area.PerformerAddress) ? "" : " Адрес: " + Area.PerformerAddress;
            Dr["note"] = Area.AreaRemark;
            Dr["poluch2"] = Area.BillingCenterReciever;
            Dr["bank2"] = String.IsNullOrEmpty(Area.BillingCenterBank) ? "" : " Банк: " + Area.BillingCenterBank;
            Dr["rschet2"] = String.IsNullOrEmpty(Area.BillingCenterCurrentAccount) ? "" : " Расч. Счет: " + Area.BillingCenterCurrentAccount;
            Dr["kschet2"] = String.IsNullOrEmpty(Area.BillingCenterCorrelationAccount) ? "" : " Корр. Счет: " + Area.BillingCenterCorrelationAccount;
            Dr["bik2"] = String.IsNullOrEmpty(Area.BillingCenterBik) ? "" : " БИК: " + Area.BillingCenterBik;
            Dr["inn2"] = String.IsNullOrEmpty(Area.BillingCenterPhone) ? "" : " ИНН/КПП: " + Area.BillingCenterPhone;
            Dr["phone2"] = String.IsNullOrEmpty(Area.BillingCenterPhone) ? "" : " Тел: " + Area.BillingCenterPhone;
            Dr["address2"] = String.IsNullOrEmpty(Area.BillingCenterAddress) ? "" : " Адрес: " + Area.BillingCenterAddress;
            Dr["kv_pl"] = Kvar.FullSquare;
            Dr["kolreg"] = Kvar.CountRegisterGil;
            Dr["kolgil"] = Kvar.CountGil;

            BaseServ2 summaryServ = Charge.SummaryServ;
            var peni = Charge.ListServ.FirstOrDefault(s => s.Value.Serv.NzpServ == 500).Value ?? new BaseServ2();
            Dr["sum_insaldo"] = summaryServ.Serv.SumInsaldo.ToString("0.00");
            Dr["sum_tarif"] = summaryServ.Serv.RsumTarif.ToString("0.00");
            Dr["reval_charge"] = summaryServ.Serv.Reval.ToString("0.00");
            Dr["sum_charge"] = (summaryServ.Serv.SumInsaldo + summaryServ.Serv.RsumTarif + summaryServ.Serv.Reval).ToString("0.00");
            Dr["sum_money"] = Payments.SumOplat.ToString("0.00");
            Dr["sum_outsaldo"] = summaryServ.Serv.SumOutsaldo.ToString("0.00");
            Dr["sum_peni_money"] = peni.Serv.SumMoney > 0 ? peni.Serv.SumMoney.ToString("0.00") : "0.00";
            Dr["sum_money_all"] = (summaryServ.Serv.SumMoney + peni.Serv.SumMoney).ToString("0.00");
            Dr["sum_charge_date"] = Payments.LastSumOplat.ToString("0.00");
            Dr["sum_peni"] = peni.Serv.SumMoney > 0 ? peni.Serv.SumMoney.ToString("0.00") : "0.00";
            Dr["sum_charge_all"] = summaryServ.Serv.SumCharge.ToString("0.00");
        }


        private DataSet FillNotCommServices(DataSet ds)
        {
            foreach (var bs in Charge.ListServ.Values)
            {
                if (!_kommServ.Contains(bs.Serv.NzpServ))
                {
                    var vol = ServVolumeLs.GetServVolume(bs.Serv.NzpServ);
                    Dr = ds.Tables[1].NewRow();
                    Dr["number"] = _currentRow;
                    Dr["months"] = Payments.DateFrom + (Payments.DateFrom != null && Payments.DateTo != null ? " - " : "") + (Payments.DateTo != Payments.DateFrom ? Payments.DateTo : "");
                    Dr["month_from"] = Payments.DateFrom ?? "_____________";
                    Dr["month_to"] = Payments.DateTo ?? "_____________";
                    Dr["gil_serv"] = bs.Serv.NameServ.Trim();
                    Dr["gil_supp"] = bs.Serv.Payer.payer.Trim();
                    Dr["gil_measure"] = bs.Serv.Measure.Trim();
                    Dr["gil_tarif"] = bs.Serv.Tarif.ToString("0.00");
                    Dr["gil_sum_tarif"] = bs.Serv.RsumTarif.ToString("0.00");
                    Dr["gil_reval"] = bs.Serv.Reval.ToString("0.00");
                    Dr["gil_sum_lgota"] = bs.Serv.SumLgota.ToString("0.00");
                    Dr["gil_sum_charge"] = bs.Serv.SumCharge.ToString("0.00");
                    ds.Tables[1].Rows.Add(Dr);
                }

            }
            return ds;
        }

        private DataSet FillCounters(DataSet ds)
        {
            Counters.LoadDoubleLsCounters(Pref, NzpKvar, Pkod);
            foreach (var c in Counters.ListCounters)
            {
                Dr = ds.Tables[2].NewRow();
                Dr["number"] = _currentRow;
                Dr["counters_serv"] = c.ServiceName.Trim();
                Dr["counters_num"] = c.NumCounter.Trim();
                Dr["counters_measure"] = c.Measure.Trim();
                Dr["counters_date"] = c.DatUchet;
                Dr["counters_val_pred"] = c.ValuePred;
                Dr["counters_val_curr"] = c.Value;
                Dr["counters_rash"] = c.Value - c.ValuePred;
                Dr["counters_correction_period"] = "";
                Dr["counters_correction_value"] = "";
                Dr["counters_all"] = c.Value - c.ValuePred;

                ds.Tables[2].Rows.Add(Dr);

            }
            return ds;
        }

        private DataSet FillCommServices(DataSet ds)
        {
            foreach (var bs in Charge.ListServ.Values)
            {
                if (_kommServ.Contains(bs.Serv.NzpServ))
                {
                    Dr = ds.Tables[3].NewRow();
                    Dr["number"] = _currentRow;
                    Dr["komm_serv"] = bs.Serv.NameServ.Trim();
                    Dr["komm_supp"] = bs.Serv.Payer.payer.Trim();
                    Dr["komm_measure"] = bs.Serv.Measure.Trim();
                    Dr["komm_norm"] = bs.Serv.Norma.ToString("0.00");
                    Dr["komm_vol"] = bs.Serv.CCalc;
                    Dr["komm_tarif_eot"] = bs.Serv.TarifEot;
                    Dr["komm_percent"] = bs.Serv.Tarif != 0 ? Math.Round((bs.Serv.TarifEot/bs.Serv.Tarif)*100) + "%" : "";
                    Dr["komm_tarif"] = bs.Serv.Tarif;
                    Dr["komm_sum_tarif"] = bs.Serv.RsumTarif;
                    Dr["komm_reval"] = bs.Serv.Reval;
                    Dr["komm_sum_charge"] = bs.Serv.SumCharge;

                    ds.Tables[3].Rows.Add(Dr);
                }
            }
            return ds;
        }
    }
}
