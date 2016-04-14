using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Faktura.Source.Base;

namespace Bars.KP50.Faktura.Saha
{
    public class FakturaYakutsk : BaseFactura2
    {

        public override string Name
        {
            get { return "Якутск"; }
        }


        public override string Code { get { return "1003"; } }

        public override string FileName { get { return "yakutsk354.frx"; } }

        private int[] _kommServ = { 6, 7, 8, 9, 14, 25, 210, 510, 511, 512, 513, 514, 515, 516, 517 };

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
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("account_month", typeof(string));
            table.Columns.Add("address", typeof(string));
            table.Columns.Add("executer", typeof(string));
            table.Columns.Add("address2", typeof(string));
            table.Columns.Add("phone", typeof(string));
            table.Columns.Add("fax", typeof(string));
            table.Columns.Add("worktime", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("kvar_s", typeof(string));
            table.Columns.Add("dom_negil_s", typeof(string));
            table.Columns.Add("dom_s", typeof(string));
            table.Columns.Add("common_use_s", typeof(string));
            table.Columns.Add("kvar_part_odn", typeof(string));
            table.Columns.Add("part_odn", typeof(string));
            table.Columns.Add("count_reg", typeof(string));
            table.Columns.Add("count_unreg", typeof(string));
            table.Columns.Add("count_ub", typeof(string));
            table.Columns.Add("count_gil", typeof(string));
            table.Columns.Add("quality", typeof(string));
            table.Columns.Add("insaldo", typeof(string));
            table.Columns.Add("insaldo_peni", typeof(string));
            table.Columns.Add("insaldo_all", typeof(string));
            table.Columns.Add("tarif", typeof(string));
            table.Columns.Add("tarif_peni", typeof(string));
            table.Columns.Add("tarif_all", typeof(string));
            table.Columns.Add("charge", typeof(string));
            table.Columns.Add("charge_peni", typeof(string));
            table.Columns.Add("charge_all", typeof(string));
            table.Columns.Add("outsaldo", typeof(string));
            table.Columns.Add("outsaldo_peni", typeof(string));
            table.Columns.Add("outsaldo_all", typeof(string));


            var table1 = new DataTable { TableName = "Q_master1" };
            table1.Columns.Add("number", typeof(int));
            table1.Columns.Add("service", typeof(string));
            table1.Columns.Add("name_supp", typeof(string));
            table1.Columns.Add("measure", typeof(string));
            table1.Columns.Add("rash_norm_one", typeof(string));
            table1.Columns.Add("vol_norm", typeof(string));
            table1.Columns.Add("vol_over", typeof(string));
            table1.Columns.Add("vol_ipu", typeof(string));
            table1.Columns.Add("tarif_nds", typeof(string));
            table1.Columns.Add("tarif_benefit", typeof(string));
            table1.Columns.Add("rsum_tarif", typeof(decimal));
            table1.Columns.Add("reval", typeof(decimal));
            table1.Columns.Add("rsum_tarif_all", typeof(decimal));

            var table2 = new DataTable { TableName = "Q_master2" };
            table2.Columns.Add("number", typeof(int));
            table2.Columns.Add("service", typeof(string));
            table2.Columns.Add("name_supp", typeof(string));
            table2.Columns.Add("measure", typeof(string));
            table2.Columns.Add("tarif", typeof(string));
            table2.Columns.Add("rsum_tarif", typeof(decimal));
            table2.Columns.Add("reval", typeof(decimal));
            table2.Columns.Add("sum_charge", typeof(decimal));

            var table3 = new DataTable { TableName = "Q_master3" };
            table3.Columns.Add("number", typeof(int));
            table3.Columns.Add("service", typeof(string));
            table3.Columns.Add("ipu_prev", typeof(string));
            table3.Columns.Add("ipu_curr", typeof(string));
            table3.Columns.Add("ipu_rash", typeof(string));
            table3.Columns.Add("ipu_date", typeof(string));
            table3.Columns.Add("odpu_prev", typeof(string));
            table3.Columns.Add("odpu_curr", typeof(string));
            table3.Columns.Add("odpu_rash", typeof(string));
            table3.Columns.Add("odpu_date", typeof(string));

            var table4 = new DataTable { TableName = "Q_master4" };
            table4.Columns.Add("number", typeof(int));
            table4.Columns.Add("service", typeof(string));
            table4.Columns.Add("measure", typeof(string));
            table4.Columns.Add("odpu_all", typeof(string));
            table4.Columns.Add("odpu_negil", typeof(string));
            table4.Columns.Add("odpu_ipu", typeof(string));
            table4.Columns.Add("odpu_norm", typeof(string));
            table4.Columns.Add("odpu_rash", typeof(string));
            table4.Columns.Add("odn_kvar_part", typeof(string));
            table4.Columns.Add("odn_kvar_rash", typeof(string));



            var ds = new DataSet();
            ds.Tables.Add(table);
            ds.Tables.Add(table1);
            ds.Tables.Add(table2);
            ds.Tables.Add(table3);
            ds.Tables.Add(table4);

            return ds;
        }

        public override bool FillTables(DataSet ds)
        {
            _currentRow = ds.Tables[0].Rows.Count;
            Dr = ds.Tables[0].NewRow();
            FillHead();
            FillSection1();
            FillSection2();
            FillSection3();
            ds.Tables[0].Rows.Add(Dr);

            ServVolumeDom.GetServNormativ(Pref, NzpDom);
            ServVolumeLs.GetLsServNormativ(Pref, NzpKvar, false);

            ds = FillSection4(ds);
            ds = FillSection5(ds);
            ds = FillSection6(ds);
            ds = FillSection7(ds);
            
            return true;
        }

        private void FillHead()
        {
            Dr["number"] = _currentRow;
            Dr["pkod"] = Pkod;
            Dr["account_month"] = FullMonthName + "г.";
            Dr["vars"] = "12710 " + GetBarCode();
        }

        private void FillSection1()
        {
            Dr["address"] = Ulica + " д. " + NumberDom +
                            ((NumberFlat != "-" && NumberFlat.Trim() != "" && NumberFlat.Trim() != "0")
                                ? " кв. " + NumberFlat
                                : "") +
                            ((NumberRoom.Trim() != "-" && NumberRoom.Trim() != "" && NumberRoom.Trim() != "0")
                                ? "/" + NumberRoom
                                : "");
            Dr["executer"] = Area.PerformerReciever;
            Dr["address2"] = Area.PerformerAddress;
            Dr["phone"] = Area.PerformerPhone;
            Dr["fax"] = "";
            Dr["worktime"] = "";
            Dr["remark"] = (String.IsNullOrEmpty(Area.PerformerBank) ? "" : " Банк: " + Area.PerformerBank) +
                           (String.IsNullOrEmpty(Area.PerformerCurrentAccount) ? "" : " Расч. Счет: " + Area.PerformerCurrentAccount) +
                           (String.IsNullOrEmpty(Area.PerformerCorrelationAccount) ? "" : " Корр. Счет: " + Area.PerformerCorrelationAccount) +
                           (String.IsNullOrEmpty(Area.PerformerBik) ? "" : " БИК: " + Area.PerformerBik) +
                           (String.IsNullOrEmpty(Area.PerformerInn) ? "" : " ИНН/КПП: " + Area.PerformerInn) +
                           (String.IsNullOrEmpty(Area.AreaRemark) ? "" : " Примечание: " + Area.AreaRemark);
        }

        private void FillSection2()
        {
            Dr["kvar_s"] = Kvar.FullSquare.ToString("0.00");
            Dr["dom_negil_s"] = "";
            Dr["dom_s"] = Dom.DomSquare.ToString("0.00");
            Dr["common_use_s"] = Dom.MopSquare.ToString("0.00");
            Dr["kvar_part_odn"] = (Dom.DomSquare != 0 ? (Dom.MopSquare / Dom.DomSquare) : 0).ToString("0.00##");
            Dr["part_odn"] = ((Dom.DomSquare != 0 ? (Dom.MopSquare / Dom.DomSquare) : 0) * Kvar.FullSquare).ToString("0.00"); 
            Dr["count_reg"] = "";
            Dr["count_unreg"] = "";
            Dr["count_ub"] = "";
            Dr["count_gil"] = Kvar.CountGil;
            Dr["quality"] = "";
        }

        private void FillSection3()
        {
            BaseServ2 summaryServ = Charge.SummaryServ;
            var peni = Charge.ListServ.FirstOrDefault(s => s.Value.Serv.NzpServ == 500).Value ?? new BaseServ2();

            Dr["insaldo"] = summaryServ.Serv.SumInsaldo.ToString("0.00");            
            Dr["insaldo_peni"] = peni.Serv.SumInsaldo > 0 ? peni.Serv.SumInsaldo.ToString("0.00") : "0.00";
            Dr["insaldo_all"] = (summaryServ.Serv.SumInsaldo + (peni.Serv.SumInsaldo > 0 ? peni.Serv.SumInsaldo : 0)).ToString("0.00");
            Dr["tarif"] = summaryServ.Serv.SumMoney.ToString("0.00");
            Dr["tarif_peni"] = peni.Serv.SumMoney > 0 ? peni.Serv.SumMoney.ToString("0.00") : "0.00";
            Dr["tarif_all"] = (summaryServ.Serv.SumMoney + (peni.Serv.SumMoney > 0 ? peni.Serv.SumMoney : 0)).ToString("0.00");
            Dr["charge"] = (summaryServ.Serv.SumReal + summaryServ.Serv.SumNedop + summaryServ.Serv.Reval + summaryServ.Serv.RealCharge).ToString("0.00");
            Dr["charge_peni"] = peni.Serv.SumCharge > 0 ? peni.Serv.SumCharge.ToString("0.00") : "0.00";
            Dr["charge_all"] =
                (summaryServ.Serv.SumReal + summaryServ.Serv.SumNedop + summaryServ.Serv.Reval +
                 summaryServ.Serv.RealCharge + (peni.Serv.SumCharge > 0 ? peni.Serv.SumCharge : 0)).ToString("0.00");
            Dr["outsaldo"] = summaryServ.Serv.SumOutsaldo.ToString("0.00");
            Dr["outsaldo_peni"] = peni.Serv.SumOutsaldo > 0 ? peni.Serv.SumOutsaldo.ToString("0.00") : "0.00"; 
            Dr["outsaldo_all"] = (summaryServ.Serv.SumOutsaldo + (peni.Serv.SumOutsaldo > 0 ? peni.Serv.SumOutsaldo : 0)).ToString("0.00"); 
        }

        private DataSet FillSection4(DataSet ds)
        {
            foreach (var bs in Charge.ListServ.Values)
            {
                if (_kommServ.Contains(bs.Serv.NzpServ))
                {
                    var vol = ServVolumeLs.GetServVolume(bs.Serv.NzpServ);
                    Dr = ds.Tables[1].NewRow();
                    Dr["number"] = _currentRow;
                    Dr["service"] = bs.Serv.NameServ.Trim();
                    Dr["name_supp"] = bs.Serv.Payer.payer.Trim();
                    Dr["measure"] = bs.Serv.Measure.Trim();
                    Dr["rash_norm_one"] = vol != null ? vol.Normativ.ToString("0.00").Trim() : "";
                    Dr["vol_norm"] = vol != null ? vol.FullNormativ.ToString("0.00").Trim() : "";
                    Dr["vol_over"] = "";
                    Dr["vol_ipu"] = vol != null ? vol.Volume.ToString("0.00").Trim() : "";
                    Dr["tarif_nds"] = "";
                    Dr["tarif_benefit"] = bs.Serv.Tarif.ToString("0.00").Trim();
                    Dr["rsum_tarif"] = bs.Serv.RsumTarif.ToString("0.00").Trim();
                    Dr["reval"] = (bs.Serv.Reval + bs.Serv.RealCharge + bs.Serv.SumNedop).ToString("0.00").Trim();
                    Dr["rsum_tarif_all"] = (bs.Serv.SumReal + bs.Serv.SumNedop + bs.Serv.Reval + bs.Serv.RealCharge).ToString("0.00");

                    ds.Tables[1].Rows.Add(Dr);
                }

            }
            return ds;
        }

        private DataSet FillSection5(DataSet ds)
        {
            foreach (var bs in Charge.ListServ.Values)
            {
                if (!_kommServ.Contains(bs.Serv.NzpServ))
                {
                    Dr = ds.Tables[2].NewRow();
                    Dr["number"] = _currentRow;
                    Dr["service"] = bs.Serv.NameServ.Trim();
                    Dr["name_supp"] = bs.Serv.Payer.payer.Trim();
                    Dr["measure"] = bs.Serv.Measure.Trim();
                    Dr["tarif"] = bs.Serv.Tarif.ToString("0.00").Trim();
                    Dr["rsum_tarif"] = bs.Serv.RsumTarif.ToString("0.00").Trim();
                    Dr["reval"] = (bs.Serv.Reval + bs.Serv.RealCharge + bs.Serv.SumNedop).ToString("0.00").Trim();
                    Dr["sum_charge"] = (bs.Serv.SumReal + bs.Serv.SumNedop + bs.Serv.Reval + bs.Serv.RealCharge).ToString("0.00");

                    ds.Tables[2].Rows.Add(Dr);
                }
            }
            return ds;
        }

        private DataSet FillSection6(DataSet ds)
        {
            Counters.LoadDoubleLsCounters(Pref, NzpKvar, Pkod);
            foreach (var c in Counters.ListCounters)
            {
                Dr = ds.Tables[3].NewRow();
                Dr["number"] = _currentRow;
                Dr["service"] = c.ServiceName.Trim();
                Dr["ipu_prev"] = c.ValuePred != 0 ? c.ValuePred.ToString("0.00") : "X";
                Dr["ipu_curr"] = c.Value != 0 ? c.Value.ToString("0.00") : "X";
                Dr["ipu_rash"] = (c.Value != 0 && c.ValuePred != 0) ? (c.Value - c.ValuePred).ToString("0.00") : "X";
                Dr["ipu_date"] = c.DatUchet.ToShortDateString();

                var dom = Counters.LoadDoubleDomCounters(Pref, NzpDom).Find(d => d.NzpServ == c.NzpServ);

                Dr["odpu_prev"] = (dom != null && dom.ValuePred != 0) ? dom.ValuePred.ToString("0.00") : "X";
                Dr["odpu_curr"] = (dom != null && dom.Value != 0) ? dom.Value.ToString("0.00") : "X";
                Dr["odpu_rash"] = (dom != null && dom.Value != 0 && dom.ValuePred != 0) ? (dom.Value - dom.ValuePred).ToString("0.00") : "X";
                Dr["odpu_date"] = dom != null ? dom.DatUchet.ToString("0.00") : "";

                ds.Tables[3].Rows.Add(Dr);

            }
            return ds;
        }

        private DataSet FillSection7(DataSet ds)
        {
            foreach (var bs in Charge.ListServ.Values)
            {
                var odpu = ServVolumeDom.GetServVolume(bs.Serv.NzpServ);
                if (bs.Serv.NzpServ > 500 && bs.Serv.NzpServ < 600)
                {
                    Dr = ds.Tables[4].NewRow();
                    Dr["number"] = _currentRow;
                    Dr["service"] = bs.Serv.NameServ.Trim();
                    Dr["measure"] = bs.Serv.Measure.Trim();
                    Dr["odpu_all"] = odpu != null ? odpu.OdpuExpend.ToString("0.00").Trim() : "";
                    Dr["odpu_negil"] = "";
                    Dr["odpu_ipu"] = odpu != null ? odpu.IpuExpend.ToString("0.00").Trim() : "";
                    Dr["odpu_norm"] = odpu != null ? odpu.NormExpend.ToString("0.00").Trim() : "";
                    Dr["odpu_rash"] = odpu != null ? odpu.OdnExpend.ToString("0.00").Trim() : "";
                    Dr["odn_kvar_part"] = "";
                    Dr["odn_kvar_rash"] = "";

                    ds.Tables[4].Rows.Add(Dr);
                }
            }
            return ds;
        }


    }
}
