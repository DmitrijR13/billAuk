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


    public class GubkinFaktura : BaseFactura
    {
        int maxCountersCount = 7;
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

        /// <summary>
        /// Заполнение квартирных параметров
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
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
            dr["kolgil"] = CountGil + CountArriveGil;
            dr["vrvib"] = CountDepartureGil;
            dr["ls"] = Pkod;
            dr["ngeu"] = Geu;
            dr["ud"] = Ud;
            dr["pkod"] = "48";
            dr["pl_dom"] = DomSquare.ToString("0.00");
            dr["pl_mop"] = MopSquare.ToString("0.00");
            dr["dom_gil"] = CountDomGil.ToString();

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
             (System.Math.Max(0, SumTicket) * 100).ToString("00000000");
            return vars + BarcodeCrc(vars);
        }

        public override bool IsShowServInGrid(BaseServ aServ)
        {
            if ((System.Math.Abs(aServ.Serv.Tarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumInsaldo-aServ.Serv.SumMoney) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumCharge) < 0.001m)
            )
            {
                return false;
            }
            return true;
        }

        //protected override bool SetServRashod()
        //{
        //   /* for (int i = 0; i < listVolume.Count; i++)
        //    {
        //        for (int j = 0; j < listServ.Count; j++)
        //        {
        //            if ((listVolume[i].nzpServ == listServ[j].serv.nzpServ) & (listVolume[i].nzpServ != 8))
        //            {
        //                if (listVolume[i].isPu == 1)
        //                {
        //                    listServ[j].serv.cCalc = listVolume[i].puVolume;
        //                    listServ[j].servOdn.cCalc = listVolume[i].odnFlatPuVolume;
        //                }
        //                else
        //                {
        //                    listServ[j].serv.cCalc = listVolume[i].normaFullVolume;
        //                    listServ[j].servOdn.cCalc = listVolume[i].odnFlatNormVolume;

        //                }

        //            }
        //        }
        //    }
        //    return true;*/
        //    decimal kanNorma = 0;
        //    decimal gvsCalc = 0;
        //    decimal gvsCalcOdn = 0;

        //    for (int i = 0; i < listVolume.Count; i++)
        //    {
        //        for (int j = 0; j < listServ.Count; j++)
        //        {
        //            if ((listVolume[i].nzpServ == listServ[j].serv.nzpServ) & (listVolume[i].nzpServ != 8)
        //                & (listVolume[i].nzpServ != 7))
        //            {
        //                if (listVolume[i].isPu == 1)
        //                {
        //                    if (listServ[j].serv.nzpMeasure != 4)
        //                        listServ[j].serv.cCalc = listVolume[i].puVolume;
        //                    if (listServ[j].serv.nzpMeasure != 4)
        //                        listServ[j].servOdn.cCalc = listVolume[i].odnFlatPuVolume;
        //                }
        //                else
        //                {
        //                    if (listServ[j].serv.nzpMeasure != 4)
        //                        listServ[j].serv.cCalc = listVolume[i].normaFullVolume;
        //                    if (listServ[j].serv.nzpMeasure != 4)
        //                        listServ[j].servOdn.cCalc = listVolume[i].odnFlatNormVolume;

        //                }
        //                listServ[i].serv.isDevice = listVolume[i].isPu;
        //                listServ[j].serv.norma = listVolume[i].normaVolume;
        //                if ((listServ[j].serv.nzpServ == 6) || (listServ[j].serv.nzpServ == 9))
        //                {
        //                    kanNorma += listServ[j].serv.norma;
        //                }
        //                if (listServ[j].serv.nzpServ == 9)
        //                {
        //                    hvsGvsNorma = listServ[j].serv.norma;
        //                    gvsCalc = listServ[j].serv.cCalc;
        //                    gvsCalcOdn = listServ[j].servOdn.cCalc;

        //                }

        //            }
        //        }
        //    }
        //    for (int j = 0; j < listServ.Count; j++)
        //    {
        //        if ((listServ[j].serv.nzpServ == 7) & (listServ[j].serv.oldMeasure == 2))
        //        {
        //            listServ[j].serv.norma = kanNorma;
        //            listServ[j].serv.cCalc = listServ[j].serv.cCalc * kanNorma;
        //        }

        //    }

        //    for (int j = 0; j < listServ.Count; j++)
        //    {
        //        if (listServ[j].serv.nzpServ == 14)
        //        {
        //            listServ[j].serv.norma = hvsGvsNorma;

        //        }

        //    }
        //    return true;
        //}


        public override void FinalPass(Faktura finder)
        {
            base.FinalPass(finder);
         
            
            //sumTicket = summaryServ.serv.sumCharge + summaryServ.serv.sumInsaldo - summaryServ.serv.sumMoney;
            SumTicket = SummaryServ.Serv.SumCharge;

            if (finder.newSumOpl > 0.001m)
                SumTicket = finder.newSumOpl;

        }

        /// <summary>
        /// Заполнение банковских реквизитов и реквизитов счета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillRekvizit(DataRow dr)
        {
            if (dr == null) return false;
            dr["poluch2"] = Rekvizit.poluch2;
            dr["poluch2_adres"] = Rekvizit.adres2;
            dr["poluch2_rs"] = Rekvizit.rschet;
            dr["poluch2_bank"] = Rekvizit.bank;
            dr["poluch2_inn"] = Rekvizit.inn;
            dr["poluch2_ks"] = Rekvizit.korr_schet;
            dr["poluch2_phone"] = Rekvizit.phone2;
           
            
            dr["Data_dolg"] = "01." + Month.ToString("00") + "." + Year.ToString();
            dr["month_"] = Month;
            dr["year_"] = Year;
            dr["months"] = FullMonthName;
            dr["numls"] = LicSchet;
            dr["lday"] = new DateTime(Year, Month, 01).AddDays(-1).Day;

            if (NumberFlat.IndexOf("комн.") >= 0)
            {
                dr["kvnum"] = NumberFlat.Replace("комн.", "(") + ")";
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

         protected override void FillGoodServVolume(DataRow dr, decimal aValue, string colName)
        {

            if (System.Math.Abs(aValue) < 0.001m)
            {
                dr[colName] = "";
            }
            else
            {
                dr[colName] = aValue.ToString("0.00##");
            }
        }

        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
         protected bool FillServiceVolume(DataRow dr, int index, BaseServ serv)
         {
             int numRec = -1;
             for (int i = 0; i < ListVolume.Count; i++)
             {
                 if (ListVolume[i].NzpServ == serv.Serv.NzpServ)
                 {
                     numRec = i;
                 }
             }
             if (numRec == -1) return true;



             // FillGoodServVolume(dr, norma, "rash_norm" + index.ToString());  // Изменил Андрей Кайнов 19.12.2012


             if ((serv.Serv.NzpServ == 25) & (KfodnEl != ""))
             {
                 dr["rash_norm_odn" + index.ToString()] = KfodnEl;
             }

             if (serv.Serv.RsumTarif - serv.ServOdn.RsumTarif > 0)
                 FillGoodServVolume(dr, ListVolume[numRec].NormaVolume, "rash_norm" + index.ToString());
             FillGoodServVolume(dr, ListVolume[numRec].DomVolume, "rash_dpu_pu" + index.ToString());
             FillGoodServVolume(dr, ListVolume[numRec].OdnDomVolume, "rash_dpu_odn" + index.ToString());
             //Ищем подходящие приборы учета
             decimal sumCountersValue = 0;
             for (int k = 0; k < ListCounters.Count; k++)
             {
                 if ((ListCounters[k].NzpServ == serv.Serv.NzpServ))
                 {
                     sumCountersValue += ListCounters[k].Value;
                 }
             }
             FillGoodServVolume(dr, sumCountersValue, "rash_pu" + index.ToString());

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
            ListServ.Sort();
            int numberString = 1;
            int startKommNumber = 0;
            dr["komm_number"] = 0;
            for (int countServ = 0; countServ < System.Math.Min(22, ListServ.Count); countServ++)
            {
                if (IsShowServInGrid(ListServ[countServ]))
                {
                    if ((ListServ[countServ].KommServ)&(startKommNumber == 0)) 
                    {
                        dr["komm_number"] = numberString;
                        dr["name_serv" + numberString.ToString()] = "Коммунальные услуги";
                        startKommNumber = numberString;
                        numberString++;

                    }
                    dr["name_serv" + numberString.ToString()] = ListServ[countServ].Serv.NameServ + 
                        ListServ[countServ].Serv.NameSupp;
                    dr["measure" + numberString.ToString()] = ListServ[countServ].Serv.Measure;


                    if ((System.Math.Abs(ListServ[countServ].Serv.CCalc) > 0.001m) &
                       (ListServ[countServ].Serv.IsOdn == false))
                    {

                        if ((ListServ[countServ].Serv.RsumTarif ==
                        ListServ[countServ].ServOdn.RsumTarif) & (ListServ[countServ].Serv.RsumTarif > 0.001m))
                        {
                        }
                        else
                        {
                            dr["c_calc" + numberString.ToString()] = (ListServ[countServ].Serv.CCalc).ToString("0.00");
                        }

                    }
 
                    if (System.Math.Abs(ListServ[countServ].ServOdn.CCalc) > 0.001m)
                    {
                        dr["c_calc_odn" + numberString.ToString()] = ListServ[countServ].ServOdn.CCalc.ToString("0.0000");
                    }
                    if (System.Math.Abs(ListServ[countServ].Serv.Tarif) > 0.001m)
                    {
                        dr["tarif" + numberString.ToString()] = ListServ[countServ].Serv.Tarif.ToString("0.00");
                    }

                    if (System.Math.Abs(ListServ[countServ].Serv.RsumTarif -
                        ListServ[countServ].ServOdn.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif" + numberString.ToString()] = (ListServ[countServ].Serv.RsumTarif -
                            ListServ[countServ].ServOdn.RsumTarif).ToString("0.00");
                    }

                    if (System.Math.Abs(ListServ[countServ].ServOdn.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif_odn" + numberString.ToString()] = ListServ[countServ].ServOdn.RsumTarif.ToString("0.00");
                    }
                    if (System.Math.Abs(ListServ[countServ].Serv.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif_all" + numberString.ToString()] = ListServ[countServ].Serv.RsumTarif.ToString("0.00");
                    }

                    if (ListServ[countServ].Serv.Reval +
                        ListServ[countServ].Serv.RealCharge > 0.001m)
                    {
                        dr["revalnull" + numberString.ToString()] = (ListServ[countServ].Serv.Reval +
                            ListServ[countServ].Serv.RealCharge).ToString("0.00");
                    }

                    dr["sum_lgota" + numberString.ToString()] = "";

                    if (System.Math.Abs(ListServ[countServ].Serv.SumCharge) > 0.001m)
                    {
                        dr["sum_charge_all" + numberString.ToString()] = ListServ[countServ].Serv.SumCharge.ToString("0.00");
                    }

                    if (System.Math.Abs(ListServ[countServ].Serv.SumInsaldo - ListServ[countServ].Serv.SumMoney) > 0.001m)
                    {
                        dr["sum_dolg" + numberString.ToString()] = (ListServ[countServ].Serv.SumInsaldo - ListServ[countServ].Serv.SumMoney).ToString("0.00");
                    }

                    if (System.Math.Abs(ListServ[countServ].Serv.SumCharge -
                            ListServ[countServ].ServOdn.SumCharge) > 0.001m)
                    {
                        dr["sum_charge" + numberString.ToString()] = (ListServ[countServ].Serv.SumCharge -
                            ListServ[countServ].ServOdn.SumCharge).ToString("0.00");
                    }
                    if (System.Math.Abs(ListServ[countServ].ServOdn.SumCharge) > 0.001m)
                    {
                        dr["sum_charge_odn" + numberString.ToString()] = ListServ[countServ].ServOdn.SumCharge.ToString("0.00");
                    }


                    FillServiceVolume(dr, numberString, ListServ[countServ]);


                    numberString++;
                }
            }
            for (int i = numberString; i < 22; i++)
            {
                dr["name_serv" + i.ToString()] = "";
                dr["measure" + i.ToString()] = "";
                dr["c_calc" + i.ToString()] = "";
                dr["c_calc_odn" + i.ToString()] = "";
                dr["tarif" + i.ToString()] = "";
                dr["rsum_tarif" + i.ToString()] = "";
                dr["rsum_tarif_odn" + i.ToString()] = "";
                dr["rsum_tarif_all" + i.ToString()] = "";
                dr["reval" + i.ToString()] = "";
                dr["sum_lgota" + i.ToString()] = "";
                dr["sum_charge_all" + i.ToString()] = "";
                dr["sum_charge" + i.ToString()] = "";
                dr["sum_charge_odn" + i.ToString()] = "";
                dr["sum_nedop" + i.ToString()] = "";
                dr["sum_sn" + i.ToString()] = "";
                dr["sum_outsaldo" + i.ToString()] = "";
                dr["real_charge" + i.ToString()] = "";

            }

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
            table.Columns.Add("poluch2", typeof(string));
            table.Columns.Add("poluch2_adres", typeof(string));
            table.Columns.Add("poluch2_phone", typeof(string));
            table.Columns.Add("poluch2_bank", typeof(string));
            table.Columns.Add("poluch2_rs", typeof(string));
            table.Columns.Add("poluch2_ks", typeof(string));
            table.Columns.Add("poluch2_bik", typeof(string));
            table.Columns.Add("poluch2_inn", typeof(string));
            table.Columns.Add("pkod", typeof(string));
            table.Columns.Add("Platelchik", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("numdom", typeof(string));
            table.Columns.Add("kvnum", typeof(string));
            table.Columns.Add("kv_pl", typeof(string));
            table.Columns.Add("priv", typeof(string));
            table.Columns.Add("vrvib", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));
            table.Columns.Add("ls", typeof(string));
            table.Columns.Add("lday", typeof(string));
            table.Columns.Add("numls", typeof(string));
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
            table.Columns.Add("sum_in_minus", typeof(string));
            table.Columns.Add("sum_in_plus", typeof(string));
            table.Columns.Add("sum_last_opl", typeof(string));
            table.Columns.Add("dat_opl", typeof(string));
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
            table.Columns.Add("pl_dom", typeof(string));
            table.Columns.Add("pl_mop", typeof(string));
            table.Columns.Add("dom_gil", typeof(string));
            table.Columns.Add("komm_number", typeof(string));
            
            

            for (int i = 1; i < 22; i++)
            {
                table.Columns.Add("name_serv" + i.ToString(), typeof(string));
                table.Columns.Add("measure" + i.ToString(), typeof(string));
                table.Columns.Add("c_calc" + i.ToString(), typeof(string));
                table.Columns.Add("c_calc_odn" + i.ToString(), typeof(string));
                table.Columns.Add("tarif" + i.ToString(), typeof(string));
                table.Columns.Add("rsum_tarif" + i.ToString(), typeof(string));
                table.Columns.Add("sum_dolg" + i.ToString(), typeof(string));
                table.Columns.Add("rsum_tarif_odn" + i.ToString(), typeof(string));
                table.Columns.Add("rsum_tarif_all" + i.ToString(), typeof(string));
                table.Columns.Add("reval" + i.ToString(), typeof(string));
                table.Columns.Add("revalnull" + i.ToString(), typeof(string));
                table.Columns.Add("sum_lgota" + i.ToString(), typeof(string));
                table.Columns.Add("sum_charge_all" + i.ToString(), typeof(string));
                table.Columns.Add("sum_charge" + i.ToString(), typeof(string));
                table.Columns.Add("sum_charge_odn" + i.ToString(), typeof(string));
                table.Columns.Add("sum_nedop" + i.ToString(), typeof(string));
                table.Columns.Add("sum_sn" + i.ToString(), typeof(string));
                table.Columns.Add("sum_outsaldo" + i.ToString(), typeof(string));
                table.Columns.Add("real_charge" + i.ToString(), typeof(string));
                table.Columns.Add("rash_name" + i.ToString(), typeof(string));
                table.Columns.Add("rash_norm" + i.ToString(), typeof(string));
                table.Columns.Add("rash_norm_odn" + i.ToString(), typeof(string));
                table.Columns.Add("rash_pu" + i.ToString(), typeof(string));
                table.Columns.Add("rash_pu_odn" + i.ToString(), typeof(string));
                table.Columns.Add("rash_dpu_pu" + i.ToString(), typeof(string));
                table.Columns.Add("rash_dpu_odn" + i.ToString(), typeof(string));

            }

            for (int i = 1; i < 7; i++)
            {
                table.Columns.Add("serv_pere" + i.ToString(), typeof(string));
                table.Columns.Add("osn_pere" + i.ToString(), typeof(string));
                table.Columns.Add("sum_pere" + i.ToString(), typeof(string));
                table.Columns.Add("period_pere" + i.ToString(), typeof(string));
            }

            
            for (int i = 1; i <= 7; i++)
            {
                table.Columns.Add("lsserv" + i.ToString(), typeof(string));
                table.Columns.Add("lsnumcnt" + i.ToString(), typeof(string));            
                table.Columns.Add("lsvalcnt2_" + i.ToString(), typeof(string));
                table.Columns.Add("lsdatProv_" + i.ToString(), typeof(string));
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
            dr["sum_dolg"] = (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney).ToString("0.00"); 
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            dr["sum_in_minus"] = (-1*System.Math.Min(SummaryServ.Serv.SumInsaldo, 0)).ToString("0.00");
            dr["sum_in_plus"] = System.Math.Max(SummaryServ.Serv.SumInsaldo,0).ToString("0.00");



            return true;
        }

        public override bool FillCounters(DataRow dr)
        {
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
                dr["lsvalcnt2_" + countersIndex] = ListCounters[i].Value.ToString("0.00");
                dr["lsdatProv_" + countersIndex] = ListCounters[i].DatUchet.ToShortDateString();
                countersIndex++;
            }


            //dr["vars2"] = pkod;

            return true;
        }


        public GubkinFaktura()
            : base()
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
            FakturaBlocks.HasDatOplBlock = true;
            FakturaBlocks.HasCalcGil = true;
       

            Clear();
        }

    }


   
   
}

