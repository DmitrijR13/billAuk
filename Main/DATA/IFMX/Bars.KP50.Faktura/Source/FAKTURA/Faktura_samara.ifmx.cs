using System.Globalization;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;
    using System.IO;

    public class SamaraFaktura : BaseFactura
    {
        private int MaxCountersCount = 6;

        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillCalcGil(DataRow dr)
        {
            if (dr == null) return false;
            return true;
        }

        protected override bool FillKvarPrm(DataRow dr)
        {
            //StreamWriter sw = new StreamWriter(@"C:\temp\people3.txt", true);
            //sw.WriteLine("1");
            string[] strArray = new string[13]
      {
        "Январь",
        "Февраль",
        "Март",
        "Апрель",
        "Май",
        "Июнь",
        "Июль",
        "Август",
        "Сентябрь",
        "Октябрь",
        "Ноябрь",
        "Декабрь",
        "Январь"
      };
            try
            {
                //if (dr == null) return false;
                if (Ownflat)
                {
                    dr["priv"] = "Приватизирована";
                }
                else
                {
                    dr["priv"] = "не приватизирована";
                }
                dr["kolgil2"] = CountRegisterGil;
                dr["kolgil"] = CountGil + CountArriveGil - CountDepartureGil;
                dr["ls"] = Pkod.Substring(5, 5);
                if (Pkod.Substring(10, 1) == "0")
                    dr["ls"] = Pkod.Substring(5, 5);
                else
                    dr["ls"] = Pkod.Substring(5, 5) + " " + Pkod.Substring(10, 1);

                if (NzpGeu > 100)
                    dr["ngeu"] = NzpGeu;
                else
                    dr["ngeu"] = Pkod.Substring(3, 2);
                dr["indecs"] = Indecs;
                dr["ud"] = Ud;
                dr["num_ls"] = NumLs.PadLeft(6, '0');
                dr["pkod"] = Pkod;
                dr["pl_dom"] = DomSquare.ToString("0.00");
                dr["pl_mop"] = MopSquare.ToString("0.00");//MopSquare
                dr["dom_gil"] = CountDomGil.ToString();
                decimal otopDpu;
                try
                {
                    otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu.Replace('.', ','));
                }
                catch
                {
                    otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu);
                }
                dr["rash_dpu_pu_otop"] = otopDpu.ToString("0.00");
                dr["monthscounters"] = Month == 12 ? (object)(strArray[this.Month] + " " + (this.Year + 1).ToString()) : (object)(strArray[this.Month] + (object)" " + (object)(System.ValueType)this.Year);
                //sw.Close();
                return true;
            }
            catch (Exception e)
            {
                //sw.WriteLine(e.ToString());
                //sw.Close();
                return true;
            }
        }

        public string BarcodeCrcSamara(string acode)
        {
            int sum_ = 0;


            for (int i = 0; i < acode.Length; i++)
            {
                if (i != 29)
                {
                    if ((i % 2) == 1)
                    {
                        sum_ = sum_ + Convert.ToInt16(acode.Substring(i, 1));
                    }
                    else sum_ = sum_ + 3 * Convert.ToInt16(acode.Substring(i, 1));
                }
            }

            String s = ((10 - sum_ % 10) % 10).ToString();

            return s.Substring(0, 1);

        }

        public override string GetBarCode()
        {
            string vars = "63" + Pkod + Month.ToString("D2") +
             (Year - 2000).ToString("D2") +
             (Math.Max(0, SumTicket) * 100).ToString("000000000");
            Shtrih = vars + BarcodeCrcSamara(vars) + BarcodeCrcSamara(vars);
            GeuKodErc = "630100000015";
            return Shtrih;
        }

        public override bool IsShowServInGrid(BaseServ aServ)
        {


            if (
                (System.Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (System.Math.Abs(aServ.Serv.CCalc) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumCharge) < 0.001m)
            )
            {
                return false;
            }
            return true;
        }

        public override void FinalPass(Faktura finder)
        {
            base.FinalPass(finder);
            if ((finder.withDolg) || ((SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney) < 0))
            {
                SumTicket = SummaryServ.Serv.RsumTarif +
                            (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge) +
                            SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney;
                if (SumTicket < 0) SumTicket = 0;
            }
            else
            {
                SumTicket = SummaryServ.Serv.RsumTarif +
                            (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge) +
                            SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney; ;
                if (SumTicket < 0) SumTicket = 0;
            }

            if (finder.newSumOpl > 0.001m)
                SumTicket = finder.newSumOpl;

        }

        /// <summary>
        /// Заполнение счетчиков
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override bool FillCounters(DataRow dr)
        {
            StreamWriter streamWriter = new StreamWriter("C:\\temp\\people123.txt", false);
            streamWriter.WriteLine("1");
            try
            {
                int num = 1;
                for (int index = 0; index < Math.Min(this.MaxCountersCount, this.ListCounters.Count); ++index)
                {
                    switch (this.ListCounters[index].NzpServ)
                    {
                        case 6:
                            dr["lsserv" + num] = num + ". х/в " + this.ListCounters[index].Place.Trim();
                            break;
                        case 8:
                            dr["lsserv" + num] = num + ". Отопл. " + this.ListCounters[index].Place.Trim();
                            break;
                        case 9:
                            dr["lsserv" + num] = num + ". г/в " + this.ListCounters[index].Place.Trim();
                            break;
                        case 25:
                            dr["lsserv" + num] = num + ". Э/Э Д" + this.ListCounters[index].Place.Trim();
                            break;
                        case 210:
                            dr["lsserv" + num] = num + ". Э/Э Н" + this.ListCounters[index].Place.Trim();
                            break;
                        default:
                            dr["lsserv" + num] = num + ". " + this.ListCounters[index].ServiceName;
                            break;
                    }
                    dr["lsnumcnt" + num] = (object)this.ListCounters[index].NumCounters;
                    dr["lsdatuchet2_" + num] = (object)this.ListCounters[index].DatUchet.ToShortDateString();
                    streamWriter.WriteLine(this.ListCounters[index].Value);
                    dr["lsvalcnt2_" + num] = (object)this.ListCounters[index].Value.ToString("0.00");
                    ++num;
                }
                streamWriter.WriteLine("2");
            }
            catch (Exception ex)
            {
                streamWriter.WriteLine(ex.ToString());
            }
            streamWriter.Close();
            return true;
        }

        protected override bool FillAdr(DataRow dr)
        {
            if (dr == null) return false;
            dr["Platelchik"] = PayerFio;
            if (Ulica.ToUpper().Contains("ПРОЕЗД") || Ulica.ToUpper().Contains("ПРОСЕК"))
                Ulica = "УЛ. " + Ulica;
            dr["ulica"] = Ulica;
            dr["numdom"] = NumberDom;
            dr["kvnum"] = NumberFlat;
            if (IsolateFlat)
            {
                dr["kv_pl"] = FullSquare.ToString("0.00");
                dr["type_pl"] = "общая";
            }
            else
            {
                dr["kv_pl"] = LiveSquare.ToString("0.00");
                dr["type_pl"] = "жилая";
            }
            return true;
        }



        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillServiceVolume(DataRow dr)
        {
            return true;
        }

        protected override void FillGoodServVolume(DataRow dr, Decimal aValue, string colName)
        {
            if (Math.Abs(aValue) < new Decimal(1, 0, 0, false, (byte)3))
                dr[colName] = (object)"";
            else
                dr[colName] = colName.IndexOf("rash_pu", StringComparison.Ordinal) <= -1 ? (colName.IndexOf("rash_dpu_pu", StringComparison.Ordinal) <= -1 ? (colName.IndexOf("rash_dpu_odn", StringComparison.Ordinal) <= -1 ? (colName.IndexOf("rash_norm", StringComparison.Ordinal) <= -1 ? (object)aValue.ToString("0.0000") : (object)aValue.ToString("0.00##")) : (object)aValue.ToString("0.00")) : (object)aValue.ToString("0.00")) : (object)aValue.ToString("0.00");
        }

        protected void FillGoodServVolume2(DataRow dr, ServVolume serv, string colName)
        {
            if (colName.Contains("rash_dpu_odn"))
            {
                if (this.Month == 9)
                {
                    if (this.NumberDom == "40")
                        dr[colName] = (object)(serv.OdnDomVolume * new Decimal(1237, 0, 0, false, (byte)4)).ToString("0.00");
                    else if (this.NumberDom == "44")
                    {
                        dr[colName] = (object)(serv.OdnDomVolume * new Decimal(1265, 0, 0, false, (byte)4)).ToString("0.00");
                    }
                    else
                    {
                        if (!(this.NumberDom == "50"))
                            return;
                        dr[colName] = (object)(serv.OdnDomVolume * new Decimal(1392, 0, 0, false, (byte)4)).ToString("0.00");
                    }
                }
                else
                {
                    if (this.Month != 10)
                        return;
                    if (this.NumberDom == "40")
                        dr[colName] = (object)(serv.OdnDomVolume * new Decimal(1234, 0, 0, false, (byte)4)).ToString("0.00");
                    else if (this.NumberDom == "44")
                        dr[colName] = (object)(serv.OdnDomVolume * new Decimal(121, 0, 0, false, (byte)3)).ToString("0.00");
                    else if (this.NumberDom == "50")
                        dr[colName] = (object)(serv.OdnDomVolume * new Decimal(1274, 0, 0, false, (byte)4)).ToString("0.00");
                }
            }
            else if (this.Month == 9)
            {
                if (this.NumberDom == "40")
                    dr[colName] = (object)(serv.DomVolume * new Decimal(1237, 0, 0, false, (byte)4)).ToString("0.00");
                else if (this.NumberDom == "44")
                    dr[colName] = (object)(serv.DomVolume * new Decimal(1265, 0, 0, false, (byte)4)).ToString("0.00");
                else if (this.NumberDom == "50")
                    dr[colName] = (object)(serv.DomVolume * new Decimal(1392, 0, 0, false, (byte)4)).ToString("0.00");
            }
            else if (this.Month == 10)
            {
                if (this.NumberDom == "40")
                    dr[colName] = (object)(serv.DomVolume * new Decimal(1234, 0, 0, false, (byte)4)).ToString("0.00");
                else if (this.NumberDom == "44")
                    dr[colName] = (object)(serv.DomVolume * new Decimal(121, 0, 0, false, (byte)3)).ToString("0.00");
                else if (this.NumberDom == "50")
                    dr[colName] = (object)(serv.DomVolume * new Decimal(1274, 0, 0, false, (byte)4)).ToString("0.00");
            }
        }

        protected void FillGoodServVolume3(DataRow dr, ServVolume serv, string colName, Decimal value)
        {
            if (this.Month == 9)
            {
                if (this.NumberDom == "40")
                    dr[colName] = (object)Math.Round(value * new Decimal(1237, 0, 0, false, (byte)4)).ToString("0.00");
                else if (this.NumberDom == "44")
                {
                    dr[colName] = (object)Math.Round(value * new Decimal(1265, 0, 0, false, (byte)4)).ToString("0.00");
                }
                else
                {
                    if (!(this.NumberDom == "50"))
                        return;
                    dr[colName] = (object)Math.Round(value * new Decimal(1392, 0, 0, false, (byte)4)).ToString("0.00");
                }
            }
            else
            {
                if (this.Month != 10)
                    return;
                if (this.NumberDom == "40")
                    dr[colName] = (object)Math.Round(value * new Decimal(1234, 0, 0, false, (byte)4)).ToString("0.00");
                else if (this.NumberDom == "44")
                    dr[colName] = (object)Math.Round(value * new Decimal(121, 0, 0, false, (byte)3)).ToString("0.00");
                else if (this.NumberDom == "50")
                    dr[colName] = (object)Math.Round(value * new Decimal(1274, 0, 0, false, (byte)4)).ToString("0.00");
            }
        }

        protected void FillGoodServVolume4(DataRow dr, ServVolume serv, string colName, Decimal domCountersValue)
        {
            Exception exception;
            if (this.Month == 9)
            {
                if (this.NumberDom == "40")
                {
                    Decimal num = new Decimal(12922, 0, 0, false, (byte)2);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    if (num < new Decimal(129222, 0, 0, false, (byte)3))
                    {
                        if (domCountersValue - serv.DomVolume < new Decimal(12922, 0, 0, false, (byte)2))
                            dr[colName] = (object)(domCountersValue - serv.DomVolume).ToString("0.00");
                        else
                            dr[colName] = (object)new Decimal(129222, 0, 0, false, (byte)3).ToString("0.00");
                    }
                    else
                        dr[colName] = (object)new Decimal(129222, 0, 0, false, (byte)3).ToString("0.00");
                }
                else if (this.NumberDom == "44")
                {
                    Decimal num = new Decimal(12693, 0, 0, false, (byte)2);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    if (num < new Decimal(12993, 0, 0, false, (byte)2))
                    {
                        if (domCountersValue - serv.DomVolume < new Decimal(12993, 0, 0, false, (byte)2))
                            dr[colName] = (object)(domCountersValue - serv.DomVolume).ToString("0.00");
                        else
                            dr[colName] = (object)new Decimal(12993, 0, 0, false, (byte)2).ToString("0.00");
                    }
                    else
                        dr[colName] = (object)new Decimal(12993, 0, 0, false, (byte)2).ToString("0.00");
                }
                else
                {
                    if (this.NumberDom != "50")
                        return;
                    Decimal num = new Decimal(1317, 0, 0, false, (byte)1);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    dr[colName] = !(num < new Decimal(1317, 0, 0, false, (byte)1)) ? (object)new Decimal(1317, 0, 0, false, (byte)1).ToString("0.00") : (!(domCountersValue - serv.DomVolume < new Decimal(1317, 0, 0, false, (byte)1)) ? (object)new Decimal(1317, 0, 0, false, (byte)1).ToString("0.00") : (object)(domCountersValue - serv.DomVolume).ToString("0.00"));
                }
            }
            else
            {
                if (this.Month != 10)
                    return;
                if (this.NumberDom == "40")
                {
                    Decimal num = new Decimal(64611, 0, 0, false, (byte)3);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    dr[colName] = !(num < new Decimal(64611, 0, 0, false, (byte)3)) ? (object)new Decimal(64611, 0, 0, false, (byte)3).ToString("0.00") : (!(domCountersValue - serv.DomVolume < new Decimal(64611, 0, 0, false, (byte)3)) ? (object)new Decimal(64611, 0, 0, false, (byte)3).ToString("0.00") : (object)(domCountersValue - serv.DomVolume).ToString("0.00"));
                }
                else if (this.NumberDom == "44")
                {
                    Decimal num = new Decimal(63465, 0, 0, false, (byte)3);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    dr[colName] = !(num < new Decimal(63465, 0, 0, false, (byte)3)) ? (object)new Decimal(63465, 0, 0, false, (byte)3).ToString("0.00") : (!(domCountersValue - serv.DomVolume < new Decimal(63465, 0, 0, false, (byte)3)) ? (object)new Decimal(63465, 0, 0, false, (byte)3).ToString("0.00") : (object)(domCountersValue - serv.DomVolume).ToString("0.00"));
                }
                else
                {
                    if (this.NumberDom != "50")
                        return;
                    Decimal num = new Decimal(6585, 0, 0, false, (byte)2);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    dr[colName] = !(num < new Decimal(6585, 0, 0, false, (byte)2)) ? (object)new Decimal(6585, 0, 0, false, (byte)2).ToString("0.00") : (!(domCountersValue - serv.DomVolume < new Decimal(6585, 0, 0, false, (byte)2)) ? (object)new Decimal(6585, 0, 0, false, (byte)2).ToString("0.00") : (object)(domCountersValue - serv.DomVolume).ToString("0.00"));
                }
            }
        }

        protected void FillGoodServVolume5(DataRow dr, ServVolume serv, string colName, Decimal domCountersValue)
        {
            Exception exception;
            if (this.Month == 9)
            {
                if (this.NumberDom == "40")
                {
                    Decimal num = new Decimal(12922, 0, 0, false, (byte)2);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    if (num < new Decimal(129222, 0, 0, false, (byte)3))
                    {
                        if (domCountersValue - serv.DomVolume < new Decimal(12922, 0, 0, false, (byte)2))
                            dr[colName] = (object)(domCountersValue - serv.DomVolume).ToString("0.00");
                        else
                            dr[colName] = (object)new Decimal(129222, 0, 0, false, (byte)3).ToString("0.00");
                    }
                    else
                        dr[colName] = (object)new Decimal(129222, 0, 0, false, (byte)3).ToString("0.00");
                }
                else if (this.NumberDom == "44")
                {
                    Decimal num = new Decimal(12693, 0, 0, false, (byte)2);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    if (num < new Decimal(12993, 0, 0, false, (byte)2))
                    {
                        if (domCountersValue - serv.DomVolume < new Decimal(12993, 0, 0, false, (byte)2))
                            dr[colName] = (object)(domCountersValue - serv.DomVolume).ToString("0.00");
                        else
                            dr[colName] = (object)new Decimal(12993, 0, 0, false, (byte)2).ToString("0.00");
                    }
                    else
                        dr[colName] = (object)new Decimal(12993, 0, 0, false, (byte)2).ToString("0.00");
                }
                else
                {
                    if (this.NumberDom != "50")
                        return;
                    Decimal num = new Decimal(1317, 0, 0, false, (byte)1);
                    try
                    {
                        num = serv.OdnFlatNormVolume - serv.DomVolume;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    dr[colName] = !(num < new Decimal(1317, 0, 0, false, (byte)1)) ? (object)new Decimal(1317, 0, 0, false, (byte)1).ToString("0.00") : (!(domCountersValue - serv.DomVolume < new Decimal(1317, 0, 0, false, (byte)1)) ? (object)new Decimal(1317, 0, 0, false, (byte)1).ToString("0.00") : (object)(domCountersValue - serv.DomVolume).ToString("0.00"));
                }
            }
            else
            {
                if (this.Month != 10)
                    return;
                dr[colName] = !(this.NumberDom == "40") ? (!(this.NumberDom == "44") ? (object)((domCountersValue - serv.DomVolume) * new Decimal(1274, 0, 0, false, (byte)4)).ToString("0.00") : (object)((domCountersValue - serv.DomVolume) * new Decimal(121, 0, 0, false, (byte)3)).ToString("0.00")) : (object)((domCountersValue - serv.DomVolume) * new Decimal(1234, 0, 0, false, (byte)4)).ToString("0.00");
            }
        }

        protected void FillGoodServVolume6(DataRow dr, ServVolume serv, string colName, Decimal domCountersValue)
        {
            if (Month >= 10 && Year == 2015)
            {
                if (NumberDom == "40")
                {
                    if (domCountersValue > 64.611m)
                    {
                        dr[colName] = 64.611m;
                    }
                    else
                    {
                        dr[colName] = Math.Round(domCountersValue, 3);
                    }
                }
                else if (NumberDom == "44")
                {
                    if (domCountersValue > 63.465m)
                    {
                        dr[colName] = 63.465m;
                    }
                    else
                    {
                        dr[colName] = Math.Round(domCountersValue, 3);
                    }
                }
                else if (NumberDom == "50")
                {
                    if (domCountersValue > 65.85m)
                    {
                        dr[colName] = 65.85m;
                    }
                    else
                    {
                        dr[colName] = Math.Round(domCountersValue, 3);
                    }
                }
            }
            else
            {
                if (NumberDom == "40")
                {
                    if (domCountersValue > 129.222m)
                    {
                        dr[colName] = 129.222m;
                    }
                    else
                    {
                        dr[colName] = domCountersValue;
                    }
                }
                else if (NumberDom == "44")
                {
                    if (domCountersValue > 126.93m)
                    {
                        dr[colName] = 126.93m;
                    }
                    else
                    {
                        dr[colName] = domCountersValue;
                    }
                }
                else if (NumberDom == "50")
                {
                    if (domCountersValue > 131.70m)
                    {
                        dr[colName] = 131.70m;
                    }
                    else
                    {
                        dr[colName] = domCountersValue;
                    }
                }
            }
        }

        protected void FillGoodServVolume4(DataRow dr, Decimal aValue, string colName)
        {
            if (Math.Abs(aValue) < new Decimal(1, 0, 0, false, (byte)3))
                dr[colName] = (object)"";
            else if (this.NumberDom == "40")
                dr[colName] = !(aValue <= new Decimal(64611, 0, 0, false, (byte)3)) ? (object)new Decimal(64611, 0, 0, false, (byte)3).ToString("0.00") : (object)aValue.ToString("0.00");
            else if (this.NumberDom == "44")
                dr[colName] = !(aValue <= new Decimal(63465, 0, 0, false, (byte)3)) ? (object)new Decimal(63465, 0, 0, false, (byte)3).ToString("0.00") : (object)aValue.ToString("0.00");
            else if (this.NumberDom == "50")
                dr[colName] = !(aValue <= new Decimal(6585, 0, 0, false, (byte)2)) ? (object)new Decimal(6585, 0, 0, false, (byte)2).ToString("0.00") : (object)aValue.ToString("0.00");
        }

        protected bool FillServiceVolume(DataRow dr, int index, int nzpServ)
        {
            int num1 = nzpServ;
            int num2 = nzpServ;
            if (nzpServ == 99)
                return true;
            if (nzpServ == 14)
                num1 = 9;
            int index1 = -1;
            for (int index2 = 0; index2 < this.ListVolume.Count; ++index2)
            {
                if (this.ListVolume[index2].NzpServ == num1)
                    index1 = index2;
            }
            if (index1 == -1)
                return true;
            if (num1 == 25 & this.KfodnEl != "")
                dr["rash_norm_odn" + (object)index] = (object)this.KfodnEl;
            if (nzpServ == 14 & this.Kfodngvs != "")
                dr["rash_norm_odn" + index] = Kfodngvs;
            if (num1 == 6 & this.Kfodnhvs != "" & this.HasHvsDpu)
                dr["rash_norm_odn" + (object)index] = (object)this.Kfodnhvs;
            if (num1 == 25 & this.HasElDpu || num1 == 210 & this.HasElDpu || (num1 == 10 & this.HasGazDpu || num1 == 6 & this.HasGazDpu) || num2 == 14 & this.HasGvsDpu || num1 == 8 & this.HasOtopDpu)
            {
                this.FillGoodServVolume(dr, this.ListVolume[index1].DomVolume, "rash_dpu_pu" + (object)index);
                this.FillGoodServVolume(dr, this.ListVolume[index1].OdnDomVolume, "rash_dpu_odn" + (object)index);
                StreamWriter sw = new StreamWriter(@"C:\temp\FillServVolume.txt", true);
                sw.WriteLine(num1);
                sw.WriteLine(this.ListVolume[index1].OdnDomVolume);
                sw.Close();
            }
            else if (num1 == 6 & this.HasHvsDpu)
            {
                this.FillGoodServVolume4(dr, this.ListVolume[index1].OdnDomVolume, "rash_dpu_odn" + (object)index);
                this.FillGoodServVolume(dr, this.ListVolume[index1].DomVolume, "rash_dpu_pu" + (object)index);
            }
            else if (num1 == 9 & this.HasGvsDpu)
            {
                this.FillGoodServVolume2(dr, this.ListVolume[index1], "rash_dpu_odn" + (object)index);
                this.FillGoodServVolume2(dr, this.ListVolume[index1], "rash_dpu_pu" + (object)index);
            }
            if (!(num1 == 25 & dr["c_calc" + (object)index].ToString().Trim() == ""))
                this.FillGoodServVolume(dr, this.ListVolume[index1].NormaVolume, "rash_norm" + (object)index);
            decimal sumCountersValue = 0;
            string countersVal = "";
            for (int k = 0; k < ListCounters.Count; k++)
            {
                if ((ListCounters[k].NzpServ == num1))
                {
                    if (countersVal.Length == 0)
                        countersVal += ListCounters[k].Value.ToString();
                    else
                        countersVal += "/" + ListCounters[k].Value.ToString();

                }
            }
            //FillGoodServVolume(dr, sumCountersValue, "rash_pu" + index);
            if (nzpServ != 9)
                dr["rash_pu" + index] = countersVal;
            //if (nzpServ == 14)
            //{
            //    dr["rash_dpu_pu" + index] = "";
            //}
            //if (nzpServ == 9)
            //{
            //    try
            //    {
            //        if (NumberDom == "94" && Month == 9)
            //            dr["rash_dpu_pu" + index] = Math.Round(Convert.ToDecimal(dr["rash_dpu_pu" + index])
            //                * 16.864m, 2);
            //        else
            //            dr["rash_dpu_pu" + index] = Math.Round(Convert.ToDecimal(dr["rash_dpu_pu" + index])
            //                * 0.0611m, 2);
            //    }
            //    catch
            //    {

            //    }
            //}

            //StreamWriter sw = new StreamWriter(@"C:\temp\FillServVolume.txt", true);
            string domCountersValue = "";
            decimal sumDomCountersValue = 0;

            //sw.WriteLine("anzpServ = " + anzpServ);
            for (int k = 0; k < ListDomCounters.Count; k++)
            {
                //sw.WriteLine("ListDomCounters[k].NzpServ = " + ListDomCounters[k].NzpServ);
                if ((ListDomCounters[k].NzpServ == num1))
                {
                    sumCountersValue += ListDomCounters[k].Value;
                    sumDomCountersValue += ListDomCounters[k].Value - ListDomCounters[k].ValuePred;
                    if (domCountersValue.Length == 0)
                        domCountersValue += ListDomCounters[k].Value.ToString();
                    else
                        domCountersValue += "/" + ListDomCounters[k].Value.ToString();
                }
            }
            if (nzpServ != 8)
            {
                if (nzpServ == 99)
                    this.FillGoodServVolume3(dr, this.ListVolume[index1], "rash_pu_odn" + (object)index, Convert.ToDecimal(sumDomCountersValue));
                else if (domCountersValue.Length != 0)
                    dr["rash_pu_odn" + index] = domCountersValue;
                else
                    dr["rash_pu_odn" + index] = "";
            }
            if (nzpServ == 6 & this.HasHvsDpu || nzpServ == 14 & this.HasGvsDpu)
            {

                this.FillGoodServVolume6(dr, this.ListVolume[index1], "rash_dpu_odn" + (object)index, sumDomCountersValue - ListVolume[index1].DomVolume);

            }
            else if (nzpServ == 9 & NumberDom != "94")
                this.FillGoodServVolume(dr, this.ListVolume[index1].OdnDomVolume, "rash_dpu_odn" + (object)index);
            return true;
        }

        /// <summary>
        /// Процедура определяет способ получения объема для расчета
        /// </summary>
        /// <param name="nzpServ"></param>
        /// <param name="isPu"></param>
        /// <returns></returns>
        protected string GetVolumeSource(int nzpServ, int isPu)
        {
            int aserv = nzpServ;
            if (aserv == 14) aserv = 9;
            if (nzpServ == 7) aserv = 6;

            if (nzpServ == 7)
            {
                foreach (BaseServ t in ListServ)
                {
                    if (t.Serv.NzpServ == 6)
                        isPu = t.Serv.IsDevice;
                }
            }

            if ((aserv == 6) || (aserv == 9) || (aserv == 25))
            {

                //    for (int i = 0; i < listCounters.Count; i++)
                //    {
                //        if ((aserv == listCounters[i].nzpServ)&(listCounters[i].datUchet>
                //             System.Convert.ToDateTime("01." + month.ToString() + "." + year.ToString())))
                //        {
                //            return "(2)";
                //        }
                //    }



                //if (isPu == 1) return "(3)";
                //else return "(1)";

                switch (isPu)
                {
                    case 0: return "(1)";
                    case 1: return "(2)";
                    case 9: return "(3)";
                }
                return "(1)";
            }

            return "";
        }


        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillMainChargeGrid(DataRow dr)
        {
            //StreamWriter streamWriter = new StreamWriter("C:\\temp\\FillMainChargeGrid.txt", true);
           // streamWriter.WriteLine("1");
            //streamWriter.Close();
            if (dr == null)
                return false;
            if (this.GvsNormGkal == new Decimal(0))
                this.GvsNormGkal = new Decimal(611, 0, 0, false, (byte)4);


            
            foreach (BaseServ aServ in this.ListServ)
            {             
                if (aServ.Serv.NameServ.Trim().Contains("п\\к") &&
                    aServ.Serv.NameServ.Trim().Contains("ОДН-Горячая вода"))
                {
                    foreach (BaseServ aServMain in this.ListServ)
                    {
                        if (!aServMain.Serv.NameServ.Trim().Contains("п\\к") &&
                            aServMain.Serv.NameServ.Trim().Contains("Горячая вода"))
                        {
                            aServMain.ServOdn.RsumTarif += aServ.Serv.RsumTarif;
                            aServMain.Serv.RsumTarif += aServ.Serv.RsumTarif;
                        }
                    }
                }
                else if (aServ.Serv.NameServ.Trim().Contains("п\\к") &&
                    aServ.Serv.NameServ.Trim().Contains("ОДН-Холодная вода для нужд ГВС"))
                {
                    foreach (BaseServ aServMain in this.ListServ)
                    {
                        if (!aServMain.Serv.NameServ.Trim().Contains("п\\к") &&
                            aServMain.Serv.NameServ.Trim().Contains("для ГВС"))
                        {
                            aServMain.ServOdn.RsumTarif += aServ.Serv.RsumTarif;
                            aServMain.Serv.RsumTarif += aServ.Serv.RsumTarif;
                        }
                    }
                }
            }
            this.SetServRashod();
            this.ListServ.Sort();
            this.ListServ = this.SortServ(this.ListServ);
            int index1 = 1;
            Decimal num1 = new Decimal(0);
            foreach (BaseServ aServ in this.ListServ)
            {
                if (this.IsShowServInGrid(aServ))
                {
                    if(aServ.Serv.Tarif == 0m)
                        continue;
                    string str1;
                    try
                    {
                        str1 = aServ.Serv.NameSupp.Trim().Split(',')[1].Trim();
                    }
                    catch
                    {
                        try
                        {
                            str1 = aServ.Serv.NameSupp.Trim().Split('/')[1].Trim();
                        }
                        catch
                        {
                            str1 = aServ.Serv.NameSupp.Trim();
                        }
                    }
                    //streamWriter.WriteLine(aServ.Serv.NameServ.Trim());
                    if (str1.Length == 0)
                        dr["name_serv" + (object)index1] = (object)aServ.Serv.NameServ.Trim();
                    else
                        dr["name_serv" + (object)index1] = (object)aServ.Serv.NameServ.Trim() + "-" + str1;
                    if ((aServ.Serv.NameServ.Trim().Contains("п\\к") &&
                         aServ.Serv.NameServ.Trim().Contains("ОДН-Горячая вода")) ||
                        (aServ.Serv.NameServ.Trim().Contains("п\\к") &&
                         aServ.Serv.NameServ.Trim().Contains("ОДН-Холодная вода для нужд ГВС")))
                    {
                        
                    }             
                    else
                    {
                        Exception exception;
                        if (aServ.Serv.NameServ.Trim() == "Холоднжжжая вода")
                            dr["name_serv" + (object)index1] = (object)aServ.Serv.NameServ.Trim();
                        else if (aServ.Serv.NameServ.Trim() == "Горячажжжя вода")
                        {
                            dr["name_serv" + (object)index1] = (object)"Горжжячая вода:";
                            ++index1;
                            dr["name_serv" + (object)index1] = (object)"Поджжогрев";
                        }
                        else if (aServ.Serv.NameServ.Trim() == "ХВС джжля ГВС")
                            dr["name_serv" + (object)index1] = (object)"Хим. очжжищенная вода";
                        else if (aServ.Serv.NameServ.Trim() == "Электроснабженижже ночное")
                            dr["name_serv" + (object)index1] = (object)"Элжжектроэнергия ночь";
                        else if (aServ.Serv.NameServ.Trim() == "Электроснабжение" && (aServ.Serv.Tarif == new Decimal(245, 0, 0, false, (byte)2) || aServ.Serv.Tarif == 7.92m))
                        {
                            dr["name_serv" + (object)index1] = (object)"ОДН-Электроснабжение день";
                            dr["measure" + (object)index1] = (object)aServ.Serv.Measure.Trim();
                            if (Math.Abs(aServ.ServOdn.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                            {
                                string str2 = "(1)";
                                if (aServ.Serv.NzpServ == 6 & this.HasHvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 9 & this.HasGvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 14 & this.HasGvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 25 & this.HasElDpu)
                                    str2 = "(4)";
                                dr["c_calc_odn" + (object)index1] = (object)(aServ.ServOdn.CCalc.ToString("0.0000") + str2);
                            }
                            if (aServ.Serv.NzpServ == 7 && aServ.Serv.NzpFrm == 26907209)
                            {
                                foreach (ServVolume servVolume in this.ListVolume)
                                {
                                    if (servVolume.NzpServ == 7)
                                        servVolume.NormaVolume = this.KanNormCalc;
                                }
                            }
                            dr["tarif" + (object)index1] = (object)new Decimal(245, 0, 0, false, (byte)2);
                            if (Math.Abs(aServ.Serv.RsumTarif - aServ.ServOdn.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                                dr["rsum_tarif" + (object)index1] = (object)"";
                            if (Math.Abs(aServ.ServOdn.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                                dr["rsum_tarif_odn" + (object)index1] = (object)aServ.ServOdn.RsumTarif.ToString("0.00");
                            dr["rsum_tarif_all" + (object)index1] = (object)aServ.ServOdn.RsumTarif.ToString("0.00");
                            Decimal num2;
                            if (Math.Abs(aServ.Serv.Reval + aServ.Serv.RealCharge) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                DataRow dataRow = dr;
                                string index2 = "reval" + (object)index1;
                                num2 = aServ.Serv.Reval + aServ.Serv.RealCharge;
                                string str2 = num2.ToString("0.00");
                                dataRow[index2] = (object)str2;
                            }
                            if (Math.Abs(aServ.Serv.Reval + aServ.Serv.RealCharge) > Math.Abs(aServ.Serv.RsumTarif))
                                num1 += aServ.Serv.Reval + aServ.Serv.RealCharge + aServ.Serv.RsumTarif;
                            dr["sum_lgota" + (object)index1] = (object)"";
                            if (Math.Abs(aServ.Serv.SumCharge) > new Decimal(1, 0, 0, false, (byte)3))
                                dr["sum_charge_all" + (object)index1] = (object)aServ.Serv.SumCharge.ToString("0.00");
                            if (Math.Abs(aServ.Serv.SumCharge - aServ.ServOdn.SumCharge) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                DataRow dataRow = dr;
                                string index2 = "sum_charge" + (object)index1;
                                num2 = aServ.Serv.SumCharge - aServ.ServOdn.SumCharge;
                                string str2 = num2.ToString("0.00");
                                dataRow[index2] = (object)str2;
                            }
                            try
                            {
                                this.FillServiceVolume(dr, index1, aServ.Serv.NzpServ);
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                            if (Math.Abs(aServ.ServOdn.SumCharge) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                if (aServ.ServOdn.RsumTarif + aServ.Serv.Reval < aServ.ServOdn.SumCharge)
                                {
                                    DataRow dataRow = dr;
                                    string index2 = "sum_charge_odn" + (object)index1;
                                    //num2 = aServ.ServOdn.RsumTarif + aServ.Serv.Reval;
                                    num2 = aServ.ServOdn.RsumTarif;
                                    string str2 = num2.ToString("0.00");
                                    dataRow[index2] = (object)str2;
                                }
                                else
                                    dr["sum_charge_odn" + (object)index1] = (object)aServ.ServOdn.RsumTarif.ToString("0.00");
                            }
                            try
                            {
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                            if (aServ.Serv.NzpMeasure == 4 & Math.Abs(aServ.Serv.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                if (aServ.Serv.OldMeasure == 4)
                                {
                                    if (aServ.Serv.NzpServ == 9)
                                    {
                                        if (Math.Abs(aServ.Serv.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                                            dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                    }
                                    else
                                        dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                }
                                else if (aServ.Serv.NzpServ == 9)
                                {
                                    if (Math.Abs(aServ.Serv.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                                    {
                                        DataRow dataRow = dr;
                                        string index2 = "c_calc" + (object)index1;
                                        num2 = aServ.Serv.CCalc * this.GvsNormGkal;
                                        string str2 = num2.ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice);
                                        dataRow[index2] = (object)str2;
                                    }
                                }
                                else
                                {
                                    DataRow dataRow = dr;
                                    string index2 = "c_calc" + (object)index1;
                                    num2 = aServ.Serv.CCalc * this.OtopNorm;
                                    string str2 = num2.ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice);
                                    dataRow[index2] = (object)str2;
                                }
                                if (Math.Abs(aServ.ServOdn.CCalc) > new Decimal(1, 0, 0, false, (byte)6))
                                {
                                    string str2 = "(1)";
                                    if (this.HasGvsDpu)
                                        str2 = "(4)";
                                    if (aServ.Serv.NzpServ == 9)
                                        dr["c_calc_odn" + (object)index1] = (object)(aServ.ServOdn.CCalc.ToString("0.0000") + str2);
                                    else
                                        dr["c_calc_odn" + (object)index1] = (object)(aServ.ServOdn.CCalc.ToString("0.0000") + str2);
                                }
                                this.FillGoodServVolume(dr, aServ.Serv.NzpServ == 9 ? this.GvsNormGkal : this.OtopNorm, "rash_norm" + (object)index1);
                            }
                            ++index1;
                            dr["name_serv" + (object)index1] = (object)"Электроснабжение";
                            dr["measure" + (object)index1] = (object)"кВт*час";
                            if (Math.Abs(aServ.Serv.CCalc) > new Decimal(1, 0, 0, false, (byte)5) & !aServ.Serv.IsOdn && !(aServ.Serv.RsumTarif == aServ.ServOdn.RsumTarif & aServ.Serv.RsumTarif > new Decimal(1, 0, 0, false, (byte)3)))
                            {
                                if (Math.Abs(aServ.Serv.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                                    dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.00##") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                else if (Math.Abs(aServ.ServOdn.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                                    dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.00##") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                            }
                            if (Math.Abs(aServ.ServOdn.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                            {
                                string str2 = "(1)";
                                if (aServ.Serv.NzpServ == 6 & this.HasHvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 9 & this.HasGvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 14 & this.HasGvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 25 & this.HasElDpu)
                                    str2 = "(4)";
                            }
                            if (aServ.Serv.NzpServ == 7 && aServ.Serv.NzpFrm == 26907209)
                            {
                                foreach (ServVolume servVolume in this.ListVolume)
                                {
                                    if (servVolume.NzpServ == 7)
                                        servVolume.NormaVolume = this.KanNormCalc;
                                }
                            }
                            if (Math.Abs(aServ.Serv.Tarif) > new Decimal(1, 0, 0, false, (byte)3))
                                dr["tarif" + (object)index1] = aServ.Serv.Tarif > 2.45m ? aServ.Serv.Tarif : 2.41m;
                            if (((aServ.Serv.NzpServ == 6 ? 1 : (aServ.Serv.NzpServ == 7 ? 1 : 0)) & (aServ.Serv.NzpMeasure != 3 ? 1 : 0)) != 0)
                            {
                                if (aServ.Serv.Norma > new Decimal(1, 0, 0, false, (byte)3))
                                {
                                    DataRow dataRow = dr;
                                    string index2 = "tarif" + (object)index1;
                                    num2 = aServ.Serv.Tarif / aServ.Serv.Norma;
                                    string str2 = num2.ToString("0.000");
                                    dataRow[index2] = (object)str2;
                                }
                                dr["measure" + (object)index1] = (object)"Куб.м.";
                            }
                            if (Math.Abs(aServ.Serv.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                DataRow dataRow = dr;
                                string index2 = "rsum_tarif" + (object)index1;
                                num2 = aServ.Serv.RsumTarif - aServ.ServOdn.RsumTarif;
                                string str2 = num2.ToString("0.00");
                                dataRow[index2] = (object)str2;
                            }
                            if (Math.Abs(aServ.Serv.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                DataRow dataRow = dr;
                                string index2 = "rsum_tarif_all" + (object)index1;
                                num2 = aServ.Serv.RsumTarif - aServ.ServOdn.RsumTarif;
                                string str2 = num2.ToString("0.00");
                                dataRow[index2] = (object)str2;
                            }
                            if (Math.Abs(aServ.Serv.Reval + aServ.Serv.RealCharge) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                DataRow dataRow = dr;
                                string index2 = "reval" + (object)index1;
                                num2 = 0;
                                string str2 = num2.ToString("0.00");
                                dataRow[index2] = (object)str2;
                            }
                            if (Math.Abs(aServ.Serv.Reval + aServ.Serv.RealCharge) > Math.Abs(aServ.Serv.RsumTarif))
                                num1 += aServ.Serv.Reval + aServ.Serv.RealCharge + aServ.Serv.RsumTarif;
                            dr["sum_lgota" + (object)index1] = (object)"";
                            if (Math.Abs(aServ.Serv.SumCharge) > new Decimal(1, 0, 0, false, (byte)3))
                                dr["sum_charge_all" + (object)index1] = (object)aServ.Serv.SumCharge.ToString("0.00");
                            if (Math.Abs(aServ.Serv.SumCharge - aServ.ServOdn.SumCharge) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                DataRow dataRow = dr;
                                string index2 = "sum_charge" + (object)index1;
                                num2 = aServ.Serv.SumCharge - aServ.ServOdn.SumCharge;
                                string str2 = num2.ToString("0.00");
                                dataRow[index2] = (object)str2;
                            }
                            //try
                            //{
                            //    this.FillServiceVolume(dr, index1, aServ.Serv.NzpServ);
                            //}
                            //catch (Exception ex)
                            //{
                            //    exception = ex;
                            //}
                            if (aServ.Serv.NzpMeasure == 4 & Math.Abs(aServ.Serv.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                if (aServ.Serv.OldMeasure == 4)
                                {
                                    if (aServ.Serv.NzpServ == 9)
                                    {
                                        if (Math.Abs(aServ.Serv.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                                            dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                    }
                                    else
                                        dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                }
                                else if (aServ.Serv.NzpServ == 9)
                                {
                                    if (Math.Abs(aServ.Serv.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                                        dr["c_calc" + (object)index1] = (object)((aServ.Serv.CCalc * this.GvsNormGkal).ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                }
                                else
                                    dr["c_calc" + (object)index1] = (object)((aServ.Serv.CCalc * this.OtopNorm).ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                if (Math.Abs(aServ.ServOdn.CCalc) > new Decimal(1, 0, 0, false, (byte)6))
                                {
                                    string str2 = "(1)";
                                    if (this.HasGvsDpu)
                                        str2 = "(4)";
                                    if (aServ.Serv.NzpServ == 9)
                                        dr["c_calc_odn" + (object)index1] = (object)(aServ.ServOdn.CCalc.ToString("0.0000") + str2);
                                    else
                                        dr["c_calc_odn" + (object)index1] = (object)(aServ.ServOdn.CCalc.ToString("0.0000") + str2);
                                }
                                this.FillGoodServVolume(dr, aServ.Serv.NzpServ == 9 ? this.GvsNormGkal : this.OtopNorm, "rash_norm" + (object)index1);
                            }
                            ++index1;
                        }
                        else if (aServ.Serv.NameServ.Trim() == "Электроснабжение ночное")
                            dr["name_serv" + (object)index1] = (object)"ОДН-Электроснабжение ночь";
                        else if (str1.Length == 0)
                            dr["name_serv" + (object)index1] = (object)aServ.Serv.NameServ.Trim();
                        else
                            dr["name_serv" + (object)index1] = (object)aServ.Serv.NameServ.Trim() + "-" + str1;
                        dr["measure" + (object)index1] = (object)aServ.Serv.Measure.Trim();
                        if (!(aServ.Serv.NameServ.Trim() == "Электроснабжение") || !(aServ.Serv.Tarif == new Decimal(245, 0, 0, false, (byte)2) || aServ.Serv.Tarif == 7.92m))
                        {
                            Decimal num2 = new Decimal(0);
                            Decimal num3 = new Decimal(0);
                            Decimal num4 = new Decimal(0);
                            if (Math.Abs(aServ.Serv.CCalc) > new Decimal(1, 0, 0, false, (byte)5) & !aServ.Serv.IsOdn && !(aServ.Serv.RsumTarif == aServ.ServOdn.RsumTarif & aServ.Serv.RsumTarif > new Decimal(1, 0, 0, false, (byte)3)))
                            {
                                if (Math.Abs(aServ.Serv.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3) && aServ.Serv.Tarif > 0.001m)
                                {
                                    dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.00##") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                    try
                                    {
                                        num2 = aServ.Serv.CCalc + Convert.ToDecimal(this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                    }
                                    catch (Exception ex1)
                                    {
                                        try
                                        {
                                            num2 = aServ.Serv.CCalc;
                                        }
                                        catch (Exception ex2)
                                        {
                                            num2 = new Decimal(0);
                                        }
                                    }
                                }
                                else if (Math.Abs(aServ.ServOdn.CCalc) > new Decimal(1, 0, 0, false, (byte)5) && aServ.Serv.Tarif > 0.001m)
                                {
                                    dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.00##") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                    try
                                    {
                                        num2 = aServ.Serv.CCalc + Convert.ToDecimal(this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                    }
                                    catch (Exception ex1)
                                    {
                                        try
                                        {
                                            num2 = aServ.Serv.CCalc;
                                        }
                                        catch (Exception ex2)
                                        {
                                            num2 = new Decimal(0);
                                        }
                                    }
                                }
                            }
                            if (Math.Abs(aServ.ServOdn.CCalc) > new Decimal(1, 0, 0, false, (byte)5) && aServ.Serv.Tarif > 0.001m)
                            {
                                string str2 = "(1)";
                                if (aServ.Serv.NzpServ == 6 & this.HasHvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 9 & this.HasGvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 14 & this.HasGvsDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 25 & this.HasElDpu)
                                    str2 = "(4)";
                                if (aServ.Serv.NzpServ == 210)
                                    str2 = "(4)";
                                //streamWriter.WriteLine("прибор учета = " + HasGvsDpu + "; str = " + str2);
                                    
                                dr["c_calc_odn" + (object)index1] = (object)(aServ.ServOdn.CCalc.ToString("0.0000") + str2);
                                try
                                {
                                    num4 = aServ.ServOdn.CCalc;
                                }
                                catch (Exception ex)
                                {
                                    num3 = new Decimal(0);
                                }
                            }
                            if (aServ.Serv.NzpServ == 7 && aServ.Serv.NzpFrm == 26907209)
                            {
                                foreach (ServVolume servVolume in this.ListVolume)
                                {
                                    if (servVolume.NzpServ == 7)
                                        servVolume.NormaVolume = this.KanNormCalc;
                                }
                            }
                            if (Math.Abs(aServ.Serv.Tarif) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                dr["tarif" + (object)index1] = (object)aServ.Serv.Tarif.ToString("0.000");
                                try
                                {
                                    num3 = aServ.Serv.Tarif;
                                }
                                catch (Exception ex)
                                {
                                    num3 = new Decimal(0);
                                }
                            }
                            Decimal num5;
                            if (((aServ.Serv.NzpServ == 6 ? 1 : (aServ.Serv.NzpServ == 7 ? 1 : 0)) & (aServ.Serv.NzpMeasure != 3 ? 1 : 0)) != 0)
                            {
                                if (aServ.Serv.Norma > new Decimal(1, 0, 0, false, (byte)3))
                                {
                                    DataRow dataRow = dr;
                                    string index2 = "tarif" + (object)index1;
                                    num5 = aServ.Serv.Tarif / aServ.Serv.Norma;
                                    string str2 = num5.ToString("0.000");
                                    dataRow[index2] = (object)str2;
                                }
                                dr["measure" + (object)index1] = (object)"Куб.м.";
                            }
                            if (Math.Abs(aServ.Serv.RsumTarif - aServ.ServOdn.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                if (num4 < new Decimal(0))
                                {
                                    DataRow dataRow = dr;
                                    string index2 = "rsum_tarif" + (object)index1;
                                    num5 = Math.Round(num2 * num3, 2);
                                    string str2 = num5.ToString("0.00");
                                    dataRow[index2] = (object)str2;
                                }
                                else
                                {
                                    DataRow dataRow = dr;
                                    string index2 = "rsum_tarif" + (object)index1;
                                    //StreamWriter streamWriter = new StreamWriter("C:\\temp\\ServIndiv.txt", true);
                                    //streamWriter.WriteLine("услуга = " + aServ.Serv.NzpServ);
                                    //streamWriter.WriteLine(aServ.Serv.RsumTarif);
                                    //streamWriter.WriteLine(aServ.ServOdn.RsumTarif);
                                    //streamWriter.Close();
                                    num5 = aServ.Serv.RsumTarif - aServ.ServOdn.RsumTarif;
                                    string str2 = num5.ToString("0.00");
                                    dataRow[index2] = (object)str2;
                                }
                            }
                            if (Math.Abs(aServ.ServOdn.RsumTarif) > new Decimal(1, 0, 0, true, (byte)3))
                            {
                                if (num4 < new Decimal(0))
                                {
                                    DataRow dataRow = dr;
                                    string index2 = "rsum_tarif_odn" + (object)index1;
                                    num5 = Math.Round(num3 * num4, 2);
                                    string str2 = num5.ToString("0.00");
                                    dataRow[index2] = (object)str2;
                                }
                                else if (aServ.ServOdn.RsumTarif > new Decimal(0))
                                    dr["rsum_tarif_odn" + (object)index1] = (object)aServ.ServOdn.RsumTarif.ToString("0.00");
                                else
                                    dr["rsum_tarif_odn" + (object)index1] = (object)"";
                            }
                            if (Math.Abs(aServ.Serv.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                                dr["rsum_tarif_all" + (object)index1] = (object)aServ.Serv.RsumTarif.ToString("0.00");
                            if (Math.Abs(aServ.Serv.Reval + aServ.Serv.RealCharge) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                DataRow dataRow = dr;
                                string index2 = "reval" + (object)index1;
                                num5 = aServ.Serv.Reval + aServ.Serv.RealCharge;
                                string str2 = num5.ToString("0.00");
                                dataRow[index2] = (object)str2;
                            }
                            if (Math.Abs(aServ.Serv.Reval + aServ.Serv.RealCharge) > Math.Abs(aServ.Serv.RsumTarif))
                                num1 += aServ.Serv.Reval + aServ.Serv.RealCharge + aServ.Serv.RsumTarif;
                            dr["sum_lgota" + (object)index1] = (object)"";
                            if (Math.Abs(aServ.Serv.SumCharge) > new Decimal(1, 0, 0, false, (byte)3))
                                dr["sum_charge_all" + (object)index1] = (object)aServ.Serv.SumCharge.ToString("0.00");
                            if (Math.Abs(aServ.Serv.SumCharge - aServ.ServOdn.SumCharge) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                DataRow dataRow = dr;
                                string index2 = "sum_charge" + (object)index1;
                                num5 = aServ.Serv.SumCharge - aServ.ServOdn.SumCharge;
                                string str2 = num5.ToString("0.00");
                                dataRow[index2] = (object)str2;
                            }
                            if (Math.Abs(aServ.ServOdn.SumCharge) > new Decimal(1, 0, 0, false, (byte)3))
                            {
                                if (num4 < new Decimal(0))
                                    dr["sum_charge_odn" + (object)index1] = (object)"";
                                else
                                    dr["sum_charge_odn" + (object)index1] = (object)aServ.ServOdn.RsumTarif.ToString("0.00");
                            }
                            else
                            {
                                if (num4 < new Decimal(0))
                                    dr["sum_charge_odn" + (object)index1] = (object)"";
                                if (Math.Abs(aServ.ServOdn.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3))
                                    dr["sum_charge_odn" + (object)index1] = (object)aServ.ServOdn.RsumTarif.ToString("0.00");
                            }

                            try
                            {
                                if(aServ.Serv.NameServ.Trim() != "Подогрев")
                                    this.FillServiceVolume(dr, index1, aServ.Serv.NzpServ);
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                            if (aServ.Serv.NzpMeasure == 4 & Math.Abs(aServ.Serv.RsumTarif) > new Decimal(1, 0, 0, false, (byte)3) && aServ.Serv.Tarif > 0.001m)
                            {
                                if (aServ.Serv.OldMeasure == 4)
                                {
                                    if (aServ.Serv.NzpServ == 9)
                                    {
                                        if (Math.Abs(aServ.Serv.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                                            dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                    }
                                    else
                                        dr["c_calc" + (object)index1] = (object)(aServ.Serv.CCalc.ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                }
                                else if (aServ.Serv.NzpServ == 9)
                                {
                                    if (Math.Abs(aServ.Serv.CCalc) > new Decimal(1, 0, 0, false, (byte)5))
                                        dr["c_calc" + (object)index1] = (object)((aServ.Serv.CCalc * this.GvsNormGkal).ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                }
                                else
                                    dr["c_calc" + (object)index1] = (object)((aServ.Serv.CCalc * this.OtopNorm).ToString("0.0000") + this.GetVolumeSource(aServ.Serv.NzpServ, aServ.Serv.IsDevice));
                                if (Math.Abs(aServ.ServOdn.CCalc) > new Decimal(1, 0, 0, false, (byte)6))
                                {
                                    string str2 = "(1)";
                                    if (this.HasGvsDpu)
                                        str2 = "(4)";
                                    //streamWriter.WriteLine("прибор учета = " + HasGvsDpu + "; str = " + str2);
                                    if (aServ.Serv.NzpServ == 9)
                                        dr["c_calc_odn" + (object)index1] = (object)(aServ.ServOdn.CCalc.ToString("0.0000") + str2);
                                    else
                                        dr["c_calc_odn" + (object)index1] = (object)(aServ.ServOdn.CCalc.ToString("0.0000") + str2);
                                }
                                this.FillGoodServVolume(dr, aServ.Serv.NzpServ == 9 ? this.GvsNormGkal : this.OtopNorm, "rash_norm" + (object)index1);
                            }
                            if (aServ.Serv.NzpMeasure == 26)
                            {
                                dr["measure" + (object)index1] = (object)"";
                                dr["c_calc" + (object)index1] = (object)"";
                            }
                            ++index1;
                        }
                    }
                }
            }
            dr["revalEpd"] = (object)num1.ToString();
            for (int index2 = index1; index2 < 19; ++index2)
            {
                dr["name_serv" + (object)index2] = (object)"";
                dr["measure" + (object)index2] = (object)"";
                dr["c_calc" + (object)index2] = (object)"";
                dr["c_calc_odn" + (object)index2] = (object)"";
                dr["tarif" + (object)index2] = (object)"";
                dr["rsum_tarif" + (object)index2] = (object)"";
                dr["rsum_tarif_odn" + (object)index2] = (object)"";
                dr["rsum_tarif_all" + (object)index2] = (object)"";
                dr["reval" + (object)index2] = (object)"";
                dr["sum_lgota" + (object)index2] = (object)"";
                dr["sum_charge_all" + (object)index2] = (object)"";
                dr["sum_charge" + (object)index2] = (object)"";
                dr["sum_charge_odn" + (object)index2] = (object)"";
                dr["sum_nedop" + (object)index2] = (object)"";
                dr["sum_sn" + (object)index2] = (object)"";
                dr["sum_outsaldo" + (object)index2] = (object)"";
                dr["real_charge" + (object)index2] = (object)"";
            }
           
            return true;
        }


        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected List<BaseServ> SortServ(List<BaseServ> baseList)
        {
            List<BaseServ> returnList = new List<BaseServ>();
            int pos = 0;
            int posAfter = 0;
            foreach (BaseServ bs in baseList)
            {
                if (bs.Serv.NzpServ == 9)
                {
                    returnList.Insert(pos, bs);
                    posAfter = pos + 1;
                }
                else if (bs.Serv.NzpServ == 14)
                {
                    returnList.Insert(posAfter, bs);
                }
                else
                {
                    returnList.Insert(pos, bs);
                }
                pos++;
            }
            return returnList;
        }

        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillRevalReason(DataRow dr)
        {
            if (dr == null) return false;

            //int kanNumber = -1;
            //string hvsReason = "";
            //string gvsReason = "";
            for (int i = 0; i < 9; i++)
            {
                if (ListReval.Count > i)
                {
                    dr["serv_pere" + (i + 1)] = ListReval[i].ServiceName;

                    //if (listReval[i].nzpServ == 7) kanNumber = i + 1;
                    if (ListReval[i].Reason == null)
                    {
                        if (Math.Abs(ListReval[i].CReval) < 0.001m)
                        {

                            if (((ListReval[i].NzpServ == 6) ||
                                (ListReval[i].NzpServ == 7) ||
                                (ListReval[i].NzpServ == 9) ||
                                (ListReval[i].NzpServ == 14) ||
                                (ListReval[i].NzpServ == 10) ||
                                (ListReval[i].NzpServ == 25) ||
                                (ListReval[i].NzpServ == 210) ||
                                (ListReval[i].NzpServ == 510) ||
                                (ListReval[i].NzpServ == 513) ||
                                (ListReval[i].NzpServ == 514) ||
                                (ListReval[i].NzpServ == 515) ||
                                (ListReval[i].NzpServ == 516) ||
                                (ListReval[i].NzpServ == 517) ||
                                (ListReval[i].NzpServ == 518)) & (GilPeriods != ""))
                            {
                                dr["osn_pere" + (i + 1)] = "Врем. выбытие жильца " + GilPeriods;
                            }

                        }
                        else
                        {

                            dr["osn_pere" + (i + 1)] = "Изм. расхода по услуге";
                        }
                    }
                    else
                    {

                        if (((ListReval[i].NzpServ == 6) ||
                              (ListReval[i].NzpServ == 7) ||
                              (ListReval[i].NzpServ == 9) ||
                              (ListReval[i].NzpServ == 14) ||
                              (ListReval[i].NzpServ == 10) ||
                              (ListReval[i].NzpServ == 25) ||
                              (ListReval[i].NzpServ == 210) ||
                              (ListReval[i].NzpServ == 510) ||
                              (ListReval[i].NzpServ == 513) ||
                              (ListReval[i].NzpServ == 514) ||
                              (ListReval[i].NzpServ == 515) ||
                              (ListReval[i].NzpServ == 516) ||
                              (ListReval[i].NzpServ == 517) ||
                              (ListReval[i].NzpServ == 518)) & (GilPeriods != ""))
                        {
                            //dr["osn_pere" + (i + 1).ToString()] = "Врем. выбытие жильца, " + listReval[i].reason ;
                            dr["osn_pere" + (i + 1)] = "Врем. выбытие жильца  " + GilPeriods +
                                ", " + ListReval[i].Reason + " " + ListReval[i].ReasonPeriod;
                        }
                        else
                        {
                            dr["osn_pere" + (i + 1)] = ListReval[i].Reason + " " + ListReval[i].ReasonPeriod;
                        }
                    }
                    dr["sum_pere" + (i + 1)] = ListReval[i].SumReval;
                    //if (listReval[i].nzpServ == 6) hvsReason = dr["osn_pere" + (i + 1)].ToString();
                    //if (listReval[i].nzpServ == 9) gvsReason = dr["osn_pere" + (i + 1)].ToString();
                }
                else
                {
                    dr["serv_pere" + (i + 1)] = "";
                    dr["osn_pere" + (i + 1)] = "";
                    dr["sum_pere" + (i + 1)] = "";

                }
            }
            //if (kanNumber > -1)
            //{
            //    if (hvsReason != "")
            //        dr["osn_pere" + kanNumber] = hvsReason;
            //    else
            //        dr["osn_pere" + kanNumber] = gvsReason;
            //}

            return true;
        }

        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("str_rekv1", typeof(string));
            table.Columns.Add("str_rekv2", typeof(string));
            table.Columns.Add("str_rekv3", typeof(string));
            table.Columns.Add("str_rekv4", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("typek", typeof(string));
            table.Columns.Add("Platelchik", typeof(string));
            table.Columns.Add("indecs", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("type_pl", typeof(string));
            table.Columns.Add("priv", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("ls", typeof(string));
            table.Columns.Add("num_ls", typeof(string));
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
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_rub", typeof(string));
            table.Columns.Add("sum_kop", typeof(string));
            table.Columns.Add("sum_rub_debt", typeof(string));
            table.Columns.Add("sum_kop_debt", typeof(string));
            table.Columns.Add("ud", typeof(string));
            table.Columns.Add("Data_dolg", typeof(string));
            table.Columns.Add("months", typeof(string));
            table.Columns.Add("monthscounters", typeof(string));
            table.Columns.Add("month_", typeof(string));
            table.Columns.Add("year_", typeof(string));
            table.Columns.Add("sum_tarif", typeof(string));
            table.Columns.Add("reval", typeof(string));
            table.Columns.Add("revalEpd", typeof(string));
            table.Columns.Add("real_charge", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));
            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("pl_mop", typeof(string));
            table.Columns.Add("dom_gil", typeof(string));
            table.Columns.Add("rash_dpu_pu_otop", typeof(string));
            table.Columns.Add("sum_peni", typeof(string));
            for (int i = 1; i < 20; i++)
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
                table.Columns.Add("rash_dpu_pu" + i, typeof(string));
                table.Columns.Add("rash_dpu_odn" + i, typeof(string));

            }

            for (int i = 1; i < 10; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
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
            dr["rsum_tarif_odn"] = SummaryServ.ServOdn.RsumTarif.ToString("0.00");
            dr["rsum_tarif"] = (SummaryServ.Serv.RsumTarif - SummaryServ.ServOdn.RsumTarif).ToString("0.00");
            dr["sum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["reval"] = SummaryServ.Serv.Reval.ToString("0.00");
            dr["reval_charge"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["real_charge"] = SummaryServ.Serv.RealCharge.ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_charge"] = (SummaryServ.Serv.SumCharge - SummaryServ.ServOdn.SumCharge).ToString("0.00");
            dr["sum_charge_odn"] = SummaryServ.ServOdn.SumCharge.ToString("0.00");
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            dr["sum_rub_debt"] = (SumTicket + SummaryServ.Serv.SumInsaldo > 0) ? Decimal.Truncate(SumTicket + SummaryServ.Serv.SumInsaldo).ToString("0") : 0.ToString("0");
            dr["sum_kop_debt"] = (SumTicket + SummaryServ.Serv.SumInsaldo > 0) ? (((SumTicket + SummaryServ.Serv.SumInsaldo) % 1) * 100).ToString("0") : 0.ToString("00");


            return true;
        }


        public SamaraFaktura()
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
            FakturaBlocks.HasCountersBlock = true;
            FakturaBlocks.HasCountersDoubleBlock = false;
            FakturaBlocks.HasCountersDoubleDomBlock = false;
            FakturaBlocks.HasOdnBlock = true;
            FakturaBlocks.HasRdnBlock = true;
            FakturaBlocks.HasPerekidkiSamaraBlock = true;
            FakturaBlocks.HasRemarkblock = true;
            FakturaBlocks.HasGilPeriodsBlock = true;
            FakturaBlocks.HasCountersSpisBlock = true;
            FakturaBlocks.HasNormblock = true;
            FakturaBlocks.HasCalcGil = true;
            Clear();
            //kfodnhvs = "0.03";
        }

    }


    public class SamaraFakturaDolg : SamaraFaktura
    {
        public override void AddServ(BaseServ aServ)
        {

            //Проверка на пустоту добавляемой услуги
            if (aServ.Empty()) return;
            SummaryServ.AddSum(aServ.Serv); //Подсчитываем Итого
        }

        public override void AddReval(SumServ aServ)
        {

        }

        public override void AddCounters(Counters aCounter)
        {
        }


        public override void AddVolume(ServVolume aVolume)
        {
        }

        public override void AddDomVolume(ServVolume aVolume)
        {
        }

        public override void AddPerekidkaOdn(int nzpServ, decimal sumRcl)
        {
        }

        protected override bool FillMainChargeGrid(DataRow dr)
        {
            return true;
        }

        protected override bool FillKvarPrm(DataRow dr)
        {
            StreamWriter sw = new StreamWriter(@"C:\temp\people2.txt", false);
            sw.WriteLine("1");
            sw.Close();
            //if (dr == null) return false;
            if (Ownflat)
            {
                dr["priv"] = "Приватизирована";
            }
            else
            {
                dr["priv"] = "не приватизирована";
            }
            dr["kolgil2"] = CountRegisterGil;
            dr["kolgil"] = CountGil + CountArriveGil - CountDepartureGil;
            dr["ls"] = Pkod.Substring(5, 5);
            if (Pkod.Substring(10, 1) == "0")
                dr["ls"] = Pkod.Substring(5, 5);
            else
                dr["ls"] = Pkod.Substring(5, 5) + " " + Pkod.Substring(10, 1);

            if (NzpGeu > 100)
                dr["ngeu"] = NzpGeu.ToString().Substring(1, 2);
            else
                dr["ngeu"] = Pkod.Substring(3, 2);
            dr["indecs"] = Indecs;
            dr["ud"] = Ud;
            dr["num_ls"] = NumLs;
            dr["pkod"] = Pkod;
            dr["pl_dom"] = DomSquare.ToString("0.00");
            dr["pl_mop"] = MopSquare.ToString("0.00");//MopSquare
            dr["dom_gil"] = CountDomGil.ToString();
            decimal otopDpu;
            try
            {
                otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu.Replace('.', ','));
            }
            catch
            {
                otopDpu = (DomSquare - MopSquare) * Convert.ToDecimal(RashDpuPu);
            }
            dr["rash_dpu_pu_otop"] = otopDpu.ToString("0.00");
            return true;
        }

        /// Заполнение итоговой строки по счету
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillSummuryBill(DataRow dr)
        {
            if (dr == null) return false;
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            return true;
        }

        public override bool DoPrint()
        {
            if (((SumTicket > 0.001m) & (SumTicket > SummaryServ.Serv.RsumTarif * 3))
                || (BillRegim == Faktura.WorkFakturaRegims.One))
            {
                return true;
            }
            return false;
        }

        public override void FinalPass(Faktura finder)
        {
            //SumTicket = SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney;
            SumTicket = SummaryServ.Serv.RsumTarif +
                            (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge) +
                            SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney;
        }

        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };
            table.Columns.Add("str_rekv1", typeof(string));
            table.Columns.Add("str_rekv2", typeof(string));
            table.Columns.Add("str_rekv3", typeof(string));
            table.Columns.Add("str_rekv4", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("geu_remark", typeof(string));
            table.Columns.Add("area_remark", typeof(string));
            table.Columns.Add("Platelchik", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("priv", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("ls", typeof(string));
            table.Columns.Add("ngeu", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_charge_odn", typeof(string));
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
            table.Columns.Add("revalEpd", typeof(string));
            table.Columns.Add("real_charge", typeof(string));
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

            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
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

            Shtrih = "";

        }


        public SamaraFakturaDolg()
        {
            Rekvizit = new _Rekvizit();

            SummaryServ = new BaseServ(false);
            FakturaBlocks.HasAdrBlock = true;
            FakturaBlocks.HasRekvizitBlock = true;
            FakturaBlocks.HasKvarPrmBlock = true;
            FakturaBlocks.HasSummuryBillBlock = true;
            FakturaBlocks.HasMainChargeGridBlock = false;
            FakturaBlocks.HasRevalReasonBlock = false;
            FakturaBlocks.HasServiceVolumeBlock = false;
            Clear();

        }
    }

}

