using System.Globalization;
using System.Linq;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;

    public class KalugaFaktura : BaseFactura
    {
     

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
            if ((finder.withDolg)||((SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney)<0))
            {
                SumTicket = SummaryServ.Serv.SumCharge;
                if (SumTicket < 0) SumTicket = 0;
            }
            else
            {
                SumTicket = SummaryServ.Serv.SumCharge;
                if (SumTicket < 0) SumTicket = 0;
            }

            if (finder.newSumOpl > 0.001m)
                SumTicket = finder.newSumOpl;

        }

        protected override bool FillAdr(DataRow dr)
        {
            if (dr == null) return false;
            dr["Platelchik"] = PayerFio;
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

         protected override void FillGoodServVolume(DataRow dr, decimal aValue, string colName)
        {

            if (Math.Abs(aValue) < 0.001m)
            {
                dr[colName] = "";
            }
            else
            {

                if (colName.IndexOf("rash_pu", StringComparison.Ordinal) > -1)
                    dr[colName] = aValue.ToString("0.00");
                else
                    dr[colName] = aValue.ToString("0.0000");
            }
        }

        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <param name="index"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        protected bool FillServiceVolume(DataRow dr, int index, int nzpServ)
         {
             int anzpServ = nzpServ;

             if (nzpServ == 9)
             {
                 if ((anzpServ == 9) & (HasGvsDpu))
                 {
                     dr["rash_norm_odn" + index] = GvsNormGkal;
                 }


                 //FillGoodServVolume(dr, hvsGvsNorma, "rash_norm" + index.ToString());

                 return true;
             }

             if (nzpServ == 14)
             {
                 anzpServ = 9;
             }

             
             
             int numRec = -1;
             for (int i = 0; i < ListVolume.Count; i++)
             {
                 if (ListVolume[i].NzpServ == anzpServ)
                 {
                     numRec = i;
                     break;
                 }
             }
             if (numRec == -1) return true;

             // FillGoodServVolume(dr, norma, "rash_norm" + index.ToString());  // Изменил Андрей Кайнов 19.12.2012


             if ((anzpServ == 25) & (KfodnEl != ""))
             {
                 dr["rash_norm_odn" + index] = KfodnEl;
             }

             if ((anzpServ == 6)& (Kfodnhvs != "")&(HasHvsDpu))
             {
                 dr["rash_norm_odn" + index] = Kfodnhvs;
             }

             if ((anzpServ == 9) & (Kfodnhvs != "") & (HasGvsDpu))
             {
                 dr["rash_norm_odn" + index] = Kfodnhvs;
             }
         
             //Если домового прибора учета нет, то не печатаем
             //if (((anzpServ == 25) & (HasElDpu)) ||
             //    ((anzpServ == 6) & (HasHvsDpu)) ||
             //    ((anzpServ == 9) & (HasGvsDpu)) ||
             //    ((anzpServ == 10) & (HasGazDpu)) ||
             //    ((anzpServ == 8) & (HasOtopDpu)))
             //{
             //    FillGoodServVolume(dr, ListVolume[numRec].DomVolume + ListVolume[numRec].OdnDomVolume,
             //        "rash_dpu" + index);
             //    FillGoodServVolume(dr, ListVolume[numRec].DomVolume, "rash_dpu_pu" + index);
             //    FillGoodServVolume(dr, ListVolume[numRec].OdnDomVolume, "rash_dpu_odn" + index);   
             //}
             

             if ((anzpServ == 25) & (dr["c_calc" + index].ToString().Trim() == ""))
             {
             }
             else
             {
                 FillGoodServVolume(dr, ListVolume[numRec].NormaVolume, "rash_norm" + index);
             
             }
             //Ищем подходящие приборы учета
             decimal sumCountersValue = 0;
             for (int k = 0; k < ListCounters.Count; k++)
             {
                 if ((ListCounters[k].NzpServ == anzpServ))
                 {
                     sumCountersValue += ListCounters[k].Value;
                 }
             }
             FillGoodServVolume(dr, sumCountersValue, "rash_pu" + index);

             return true;
         }

         public override void AddReasonReval(int nzpServ, string reason, string period)
         {
             if (reason == "перекидка") reason = "Компенсация";
             for (int i = 0; i < ListReval.Count; i++)
             {
                 if (ListReval[i].NzpServ == nzpServ)
                 {
                     ServReval aReval = ListReval[i];
                     if (aReval.Reason != null)
                     {
                         aReval.Reason += "," + reason;
                         aReval.ReasonPeriod += "," + period;
                     }
                     else
                     {
                         aReval.Reason = reason;
                         aReval.ReasonPeriod = period;
                     }
                     ListReval[i] = aReval;
                 }
             }
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
                 
                     for (int i = 0; i < ListCounters.Count; i++)
                     {
                         if ((aserv == ListCounters[i].NzpServ)&(ListCounters[i].DatUchet>
                              Convert.ToDateTime("01." + Month + "." + Year)))
                         {
                             return "(2)";
                         }
                     }
                 


                 if (isPu == 1) return "(3)";
                 return "(1)";
             }

             return "";
         }

         /// <summary>
         /// Добавление услуги в счета
         /// </summary>
         /// <param name="aServ">Услуга</param>
         public override void AddServ(BaseServ aServ)
         {

             //Проверка на пустоту добавляемой услуги
             if (aServ.Empty()) return;

             foreach (BaseServ t in ListKommServ)
             {
                 if (t.Serv.NzpServ == aServ.Serv.NzpServ)
                 {
                     aServ.KommServ = true;
                 }
             }

             SummaryServ.AddSum(aServ.Serv); //Подсчитываем Итого
             if ((Math.Abs(aServ.Serv.Reval) > 0.001m) || (Math.Abs(aServ.Serv.RealCharge) > 0.001m))
             {
                 AddReval(aServ.Serv);
             }
             BaseServ mainServ = CUnionServ.GetMainServBySlave(aServ.Serv.NzpServ);
             //Определяем к какой услуге добавить
             if (mainServ == null) //услуга не объединяемая
             {
                 bool findServ = false;
                 foreach (BaseServ t in ListServ)
                 {
                     if (t.Serv.NzpServ == aServ.Serv.NzpServ) //если такая услуга уже есть то добавляем к ней
                     {
                         t.AddSum(aServ.Serv);
                         if (t.Serv.NameSupp == "")
                         {
                             t.Serv.NameSupp = aServ.Serv.NameSupp;
                             t.Serv.SuppRekv = aServ.Serv.SuppRekv;
                         }
                         else
                         {
                             if (aServ.Serv.Tarif > 0)
                             {
                                 t.Serv.NameSupp = aServ.Serv.NameSupp;
                                 t.Serv.SuppRekv = aServ.Serv.SuppRekv;
                                 if (aServ.Serv.IsOdn == false)
                                     t.Serv.NameServ = aServ.Serv.NameServ;
                             }
                         }
                         findServ = true;
                     }
                 }
                 if (!findServ) ListServ.Add(aServ); //иначе добавляем новую
             }
             else //если услуга объединяемая
             {
                 bool findServ = false;
                 foreach (BaseServ t in ListServ)
                 {
                     if (t.Serv.NzpServ == mainServ.Serv.NzpServ) //если мастер услуга уже присутствует, то добавляем к ней
                     {
                         if (aServ.Serv.IsOdn) aServ.Serv.CanAddTarif = false;


                         if (aServ.Serv.Tarif > 0)
                         {
                             t.Serv.NameSupp = aServ.Serv.NameSupp;
                             t.Serv.SuppRekv = aServ.Serv.SuppRekv;
                             //t.ServOdn.CCalc = aServ.Serv.CCalc;
                             //t.ServOdn.CReval = aServ.Serv.CReval;
                             //t.ServOdn.NzpServ = aServ.Serv.NzpServ;
                             if (aServ.Serv.IsOdn == false)
                                 t.Serv.NameServ = aServ.Serv.NameServ;
                         }
                         t.AddSum(aServ.Serv);
                         findServ = true;
                     }
                 }
                 if (!findServ) //Не найдена объединенная услуга, добавляем её
                 {
                     var newMainServ = (BaseServ)mainServ.Clone();
                     newMainServ.KommServ = aServ.KommServ;

                     if (aServ.Serv.IsOdn) aServ.Serv.CanAddTarif = false;

                     if (aServ.Serv.Tarif > 0)
                     {
                         newMainServ.Serv.NameSupp = aServ.Serv.NameSupp;
                         newMainServ.Serv.SuppRekv = aServ.Serv.SuppRekv;
                         if (aServ.Serv.IsOdn == false)
                         {
                             newMainServ.Serv.NameServ = aServ.Serv.NameServ;
                             newMainServ.Serv.IsDevice = aServ.Serv.IsDevice;
                         }

                     }
                     //newMainServ.Serv.Compensation = aServ.Serv.Compensation;
                     newMainServ.Serv.Measure = aServ.Serv.Measure;
                     newMainServ.AddSum(aServ.Serv);
                     ListServ.Add(newMainServ);
                 }
             }

         }

     
        /// <summary>
         /// Процедура проставляет расход по услугам из calc_gku
         /// </summary>
         /// <returns></returns>
         protected override bool SetServRashod()
         {
             decimal kanNorma = 0;
             decimal gvsNorm = 0;

             int numhvsgvs = -1;

             foreach (ServVolume t in ListVolume)
             {
                 for (int j = 0; j < ListServ.Count; j++)
                 {
                     //if (t.NzpServ == ListServ[j].ServOdn.NzpServ)
                     //{
                     //    if (t.IsPu > 0)
                     //    {
                     //        ListServ[j].ServOdn.CCalc = t.OdnFlatPuVolume;
                     //    }
                     //    else
                     //    {
                     //        ListServ[j].ServOdn.CCalc = t.OdnFlatNormVolume;
                     //    }
                     //}
                     if ((t.NzpServ == ListServ[j].Serv.NzpServ) & (t.NzpServ != 8) &
                         (t.NzpServ != 7))
                     {
                         if (t.IsPu > 0)
                         {
                             ListServ[j].Serv.CCalc = t.PUVolume;
                             ListServ[j].ServOdn.CCalc = t.OdnFlatPuVolume;
                         }
                         else
                         {

                             ListServ[j].Serv.CCalc = t.NormaFullVolume;
                             ListServ[j].ServOdn.CCalc = t.OdnFlatNormVolume;
                         }

                         ListServ[j].Serv.Norma = t.NormaVolume;

                         if (ListServ[j].Serv.NzpServ == 14)
                         {
                             numhvsgvs = j;
                             gvsNorm = t.NormaVolume;
                         }
                         if (ListServ[j].Serv.NzpServ == 6)
                         {

                             kanNorma = t.NormaVolume;
                         }

                     }
                 }
             }

             //Если есть ХВС для ГВС
             if (numhvsgvs > -1)
             {
                 //Норматив 9-ке проставляем кубометровый
                 foreach (ServVolume t in ListVolume)
                     if (t.NzpServ == 9) t.NormaVolume = gvsNorm;


                 //Норматив канализации проставляем как сумму ХВС и ГВС
                 foreach (ServVolume t in ListVolume)
                     if (t.NzpServ == 7) t.NormaVolume = gvsNorm + kanNorma;
             }


             //Для канализации проставляем кубометры в нормативе если расчет с человека
             foreach (BaseServ t in ListServ)
             {
                 if ((t.Serv.NzpServ == 7) & (t.Serv.OldMeasure == 2))
                 {
                     t.Serv.Norma = kanNorma;
                     t.Serv.CCalc = t.Serv.CCalc * kanNorma;
                 }
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
            FillSupplierGrid(dr);
            SetServRashod();
            ListServ.Sort();
            int numberString = 1;

            #region Обнуление массива
            for (int i = numberString; i < 19; i++)
            {
                dr["name_serv" + i] = "";
                dr["measure" + i] = "";
                dr["c_calc" + i] = "";
                dr["c_calc_odn" + i] = "";
                dr["tarif" + i] = "";
                dr["rsum_tarif" + i] = "";
                dr["rsum_tarif_odn" + i] = "";
                dr["rsum_tarif_all" + i] = "";
                dr["reval" + i] = "";
                dr["comp" + i] = "";
                dr["sum_lgota" + i] = "";
                dr["sum_charge_all" + i] = "";
                dr["sum_charge" + i] = "";
                dr["sum_charge_odn" + i] = "";
                dr["sum_nedop" + i] = "";
                dr["sum_sn" + i] = "";
                dr["sum_outsaldo" + i] = "";
                dr["real_charge" + i] = "";

            }
            #endregion

            #region Для начала отображаем жилищные услуги
            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if ((IsShowServInGrid(ListServ[countServ]))&(ListServ[countServ].KommServ == false))
                {
                    dr["name_serv" + numberString] = ListServ[countServ].Serv.NameServ.Trim();
                    dr["measure" + numberString] = ListServ[countServ].Serv.Measure.Trim();
                    if (Math.Abs(ListServ[countServ].Serv.Tarif) > 0.001m)
                    {
                        dr["tarif" + numberString] = ListServ[countServ].Serv.Tarif.ToString("0.00");
                    }
                    if (Math.Abs(ListServ[countServ].Serv.Compensation) > 0.001m)
                    {
                        dr["comp" + numberString] = ListServ[countServ].Serv.Compensation.ToString("0.00");
                    }

                    if (Math.Abs(ListServ[countServ].Serv.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif_all" + numberString] = ListServ[countServ].Serv.RsumTarif.ToString("0.00");
                    }
                   
                    if (Math.Abs(ListServ[countServ].Serv.SumInsaldo -
                      ListServ[countServ].Serv.SumMoney) > 0.001m)
                    {
                        dr["sum_dolg" + numberString] = (ListServ[countServ].Serv.SumInsaldo -
                        ListServ[countServ].Serv.SumMoney).ToString("0.00");
                    }

                    if (Math.Abs(ListServ[countServ].Serv.Reval +
                        ListServ[countServ].Serv.RealCharge) > 0.001m)
                    {
                        dr["reval" + numberString] = (ListServ[countServ].Serv.Reval +
                            ListServ[countServ].Serv.RealCharge).ToString("0.00");
                    }

                    dr["sum_lgota" + numberString] = "";

                    if (Math.Abs(ListServ[countServ].Serv.SumCharge) > 0.001m)
                    {
                        dr["sum_charge_all" + numberString] = ListServ[countServ].Serv.SumCharge.ToString("0.00");
                    }
                    numberString++;

                }
            }
#endregion

            #region Коммунальные услуги

            numberString = 10;
            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if ((IsShowServInGrid(ListServ[countServ])) & ListServ[countServ].KommServ)
                {
                    dr["name_serv" + numberString] = ListServ[countServ].Serv.NameServ.Trim();
                    dr["measure" + numberString] = ListServ[countServ].Serv.Measure.Trim();



                    if ((Math.Abs(ListServ[countServ].Serv.CCalc) > 0.001m) &
                       (ListServ[countServ].Serv.IsOdn == false))
                    {

                        if ((ListServ[countServ].Serv.RsumTarif ==
                        ListServ[countServ].ServOdn.RsumTarif) & (ListServ[countServ].Serv.RsumTarif > 0.001m))
                        {
                        }
                        else
                        {
                            if (Math.Abs(ListServ[countServ].Serv.RsumTarif) > 0.001m)
                                if (ListServ[countServ].Serv.NzpServ == 8)
                                    dr["c_calc" + numberString] =
                                        Math.Round(
                                            (ListServ[countServ].Serv.Tarif != 0
                                                ? (ListServ[countServ].Serv.RsumTarif/ListServ[countServ].Serv.Tarif)
                                                : ListServ[countServ].Serv.CCalc), 6)
                                            .ToString("0.0000##") +
                                        GetVolumeSource(ListServ[countServ].Serv.NzpServ,
                                            ListServ[countServ].Serv.IsDevice);
                                else
                                    dr["c_calc" + numberString] = ListServ[countServ].Serv.CCalc.ToString("0.0000##") +
                                                                  GetVolumeSource(ListServ[countServ].Serv.NzpServ,
                                                                      ListServ[countServ].Serv.IsDevice);
                            else
                                if (Math.Abs(ListServ[countServ].ServOdn.CCalc) > 0.001m)
                                {
                                    dr["c_calc" + numberString] = ListServ[countServ].Serv.CCalc.ToString("0.0000##") +
                              GetVolumeSource(ListServ[countServ].Serv.NzpServ, ListServ[countServ].Serv.IsDevice);
                                }



                        }

                    }

                 
                    if (Math.Abs(ListServ[countServ].ServOdn.CCalc) > 0.001m)
                    {

                        string sourceOdn = "(1)";

                        if ((ListServ[countServ].Serv.NzpServ == 6) & (HasHvsDpu)) sourceOdn = "(4)";
                        if ((ListServ[countServ].Serv.NzpServ == 9) & (HasGvsDpu)) sourceOdn = "(4)";
                        if ((ListServ[countServ].Serv.NzpServ == 14) & (HasGvsDpu)) sourceOdn = "(4)";
                        if ((ListServ[countServ].Serv.NzpServ == 25) & (HasElDpu)) sourceOdn = "(4)";
                        
                        dr["c_calc_odn" + numberString] = ListServ[countServ].ServOdn.CCalc.ToString("0.0000")+
                            sourceOdn;
                    }

                    if (ListServ[countServ].Serv.NzpServ == 7 )
                    {
                        if (ListServ[countServ].Serv.NzpFrm == 26907209)
                        {
                            for (int n = 0; n < ListVolume.Count; n++)
                                if (ListVolume[n].NzpServ == 7)
                                    ListVolume[n].NormaVolume = KanNormCalc;
                        }
                    }
                
 

                    if (Math.Abs(ListServ[countServ].Serv.Tarif) > 0.001m)
                    {
                        dr["tarif" + numberString] = ListServ[countServ].Serv.Tarif.ToString("0.00");
                    }

                    if (Math.Abs(ListServ[countServ].Serv.Compensation) > 0.001m)
                    {
                        dr["comp" + numberString] = ListServ[countServ].Serv.Compensation.ToString("0.00");
                    }

                    if (((ListServ[countServ].Serv.NzpServ == 6) || 
                        (ListServ[countServ].Serv.NzpServ == 7))&(ListServ[countServ].Serv.NzpMeasure !=3))
                    {
                        if (ListServ[countServ].Serv.Norma > 0.001m)
                        {
                            dr["tarif" + numberString] = (ListServ[countServ].Serv.Tarif/
                               ListServ[countServ].Serv.Norma).ToString("0.00");
                            
                        }
                        dr["measure" + numberString] = "Куб.м.";
                    }

                    if (Math.Abs(ListServ[countServ].Serv.RsumTarif -
                        ListServ[countServ].ServOdn.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif" + numberString] = (ListServ[countServ].Serv.RsumTarif -
                            ListServ[countServ].ServOdn.RsumTarif).ToString("0.00");
                    }

                    if (Math.Abs(ListServ[countServ].ServOdn.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif_odn" + numberString] = ListServ[countServ].ServOdn.RsumTarif.ToString("0.00");
                    }
                    if (Math.Abs(ListServ[countServ].Serv.RsumTarif) > 0.001m)
                    {
                        dr["rsum_tarif_all" + numberString] = ListServ[countServ].Serv.RsumTarif.ToString("0.00");
                    }

                    if (Math.Abs(ListServ[countServ].Serv.SumInsaldo -
                      ListServ[countServ].Serv.SumMoney) > 0.001m)
                    {
                        dr["sum_dolg" + numberString] = (ListServ[countServ].Serv.SumInsaldo -
                        ListServ[countServ].Serv.SumMoney).ToString("0.00");
                    }

                    if (Math.Abs(ListServ[countServ].Serv.Reval +
                        ListServ[countServ].Serv.RealCharge) > 0.001m)
                    {
                        dr["reval" + numberString] = (ListServ[countServ].Serv.Reval +
                            ListServ[countServ].Serv.RealCharge).ToString("0.00");
                    }

                    dr["sum_lgota" + numberString] = "";

                    if (Math.Abs(ListServ[countServ].Serv.SumCharge) > 0.001m)
                    {
                        dr["sum_charge_all" + numberString] = ListServ[countServ].Serv.SumCharge.ToString("0.00");
                    }

                    if (Math.Abs(ListServ[countServ].Serv.SumCharge -
                            ListServ[countServ].ServOdn.SumCharge) > 0.001m)
                    {
                        dr["sum_charge" + numberString.ToString()] = (ListServ[countServ].Serv.SumCharge -
                            ListServ[countServ].ServOdn.SumCharge).ToString("0.00");
                        //dr["sum_charge" + numberString] = "";
                    }
                    if (Math.Abs(ListServ[countServ].ServOdn.SumCharge) > 0.001m)
                    {
                        dr["sum_charge_odn" + numberString.ToString()] = ListServ[countServ].ServOdn.SumCharge.ToString("0.00");
                        //dr["sum_charge_odn" + numberString] = "";
                    }


                    FillServiceVolume(dr, numberString, ListServ[countServ].Serv.NzpServ);

                    numberString++;
                }
            }
           
            #endregion

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
            table.Columns.Add("poluch", typeof(string));
            table.Columns.Add("poluch_adres", typeof(string));
            table.Columns.Add("poluch_phone", typeof(string));
            table.Columns.Add("poluch_bank", typeof(string));
            table.Columns.Add("poluch_rs", typeof(string));
            table.Columns.Add("poluch_ks", typeof(string));
            table.Columns.Add("poluch_bik", typeof(string));
            table.Columns.Add("poluch_inn", typeof(string));
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
            table.Columns.Add("type_pl", typeof(string));
            table.Columns.Add("priv", typeof(string));
            table.Columns.Add("kolgil2", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("kolgil_time", typeof(string));
            table.Columns.Add("ls", typeof(string));
            table.Columns.Add("ngeu", typeof(string));
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("geu_remark", typeof(string));
            table.Columns.Add("area_remark", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("rsum_tarif_all", typeof(string));
            table.Columns.Add("rsum_tarif_all_all", typeof(string));
            table.Columns.Add("rsum_tarif_odn", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("sum_charge_odn", typeof(string));
            table.Columns.Add("sum_avans", typeof(string));
            table.Columns.Add("sum_dolg", typeof(string));
            table.Columns.Add("sum_dolg_", typeof(string));
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
            table.Columns.Add("areaName", typeof(string));
            table.Columns.Add("ukls", typeof(string));
            table.Columns.Add("areaAdr", typeof(string));
            table.Columns.Add("areaPhone", typeof(string));
            table.Columns.Add("areaFax", typeof(string));
            table.Columns.Add("areaEmail", typeof(string));
            table.Columns.Add("areaWeb", typeof(string));
            table.Columns.Add("worktime", typeof(string));
            table.Columns.Add("params", typeof(string));
            table.Columns.Add("dat_opl", typeof(string));
            table.Columns.Add("sum_last_opl", typeof(string));
            table.Columns.Add("comp", typeof(string));
            table.Columns.Add("countGil", typeof(string));
            table.Columns.Add("countDepartureGil", typeof(string));
            table.Columns.Add("countArriveGil", typeof(string));

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
                table.Columns.Add("comp" + i, typeof(string));
                table.Columns.Add("sum_lgota" + i, typeof(string));
                table.Columns.Add("sum_charge_all" + i, typeof(string));
                table.Columns.Add("sum_charge" + i, typeof(string));
                table.Columns.Add("sum_charge_odn" + i, typeof(string));
                table.Columns.Add("sum_nedop" + i, typeof(string));
                table.Columns.Add("sum_dolg" + i, typeof(string));
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

            for (int i = 1; i < 7; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
            }


            for (int i = 1; i < 8; i++)
            {
                table.Columns.Add("lsserv" + i, typeof(string));
                table.Columns.Add("lsnumcnt" + i, typeof(string));
                table.Columns.Add("lsdatuchet1_" + i, typeof(string));
                table.Columns.Add("lsdatuchet2_" + i, typeof(string));
                table.Columns.Add("lsvalcnt1_" + i, typeof(string));
                table.Columns.Add("lsvalcnt2_" + i, typeof(string));
            }

            for (int i = 1; i < 10; i++)
            {
                table.Columns.Add("supp_serv" + i, typeof(string));
                table.Columns.Add("supp_name" + i, typeof(string));
                table.Columns.Add("supp_inn" + i, typeof(string));
                table.Columns.Add("supp_summ" + i, typeof(string));
            }

            for (int i = 1; i <= 5; i++)
            {
                table.Columns.Add("domserv" + i, typeof(string));
                table.Columns.Add("domnumcnt" + i, typeof(string));
                table.Columns.Add("domdatuchet1_" + i, typeof(string));
                table.Columns.Add("domdatuchet2_" + i, typeof(string));
                table.Columns.Add("domvalcnt1_" + i, typeof(string));
                table.Columns.Add("domvalcnt2_" + i, typeof(string));
            }

            return table;

        }

        /// <summary>
        /// Заполнение таблицы по поставщикам
        /// </summary>
        /// <param name="dr"></param>
        private void FillSupplierGrid(DataRow dr)
        {
            int index = 1;
            foreach (BaseServ t in ListSupp)
            {
                if (IsShowServInGrid(t) && t.Serv.SumOutsaldo > 0)
                {
                    if (index < 9)
                    {
                        //Если выбран дом с непосредственным управлением или управление не выбрано, то услуга значится как "Содержание и текущий ремонт многоквартирного дома" 
                        if (NzpArea == 1003 || NzpArea == 1004 || NzpArea == 1016)
                            t.Serv.NameServ =
                                t.Serv.NameServ.Replace("Содерж. и ремонт жилого помещения, управление МКД",
                                    "Содержание и текущий ремонт многоквартирного дома");
                        dr["supp_serv" + index] = t.Serv.NameServ.TrimEnd(',');
                        dr["supp_name" + index] = t.Serv.NameSupp;
                        dr["supp_inn" + index] = t.Serv.SuppRekv;
                        dr["supp_summ" + index] = t.Serv.SumOutsaldo.ToString("0.00");
                    }

                    index++;
                }
            }
        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillSummuryBill(DataRow dr)
        {
            if (dr == null) return false;
            dr["rsum_tarif_all"] = (SummaryServ.Serv.RsumTarif).ToString("0.00");
            dr["rsum_tarif_all_all"] = (SummaryServ.Serv.RsumTarif + SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["rsum_tarif_odn"] = SummaryServ.ServOdn.RsumTarif.ToString("0.00");
            dr["rsum_tarif"] = (SummaryServ.Serv.RsumTarif - SummaryServ.ServOdn.RsumTarif).ToString("0.00");
            dr["sum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["reval"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["comp"] = SummaryServ.Serv.Compensation.ToString("0.00");
            //dr["reval_charge"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["real_charge"] = SummaryServ.Serv.RealCharge.ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            //dr["sum_charge"] = (SummaryServ.Serv.SumCharge - SummaryServ.ServOdn.SumCharge).ToString("0.00");
            //dr["sum_charge_odn"] = SummaryServ.ServOdn.SumCharge.ToString("0.00");
            dr["sum_charge"] = "";
            dr["sum_charge_odn"] = "";
        
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00"); 
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");
            if (SummaryServ.Serv.SumInsaldo > 0)
            {
                dr["sum_dolg"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
                dr["sum_avans"] = "0.00";
            }
            else
            {
                dr["sum_dolg"] = "0.00";
                dr["sum_avans"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            }
            dr["sum_dolg_"] = (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney).ToString("0.00");

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
            dr["poluch2"] = Rekvizit.poluch2;
            dr["poluch2_adres"] = Rekvizit.adres2;
            dr["poluch2_rs"] = Rekvizit.rschet2;
            dr["poluch2_bank"] = Rekvizit.bank2;
            dr["poluch2_inn"] = Rekvizit.inn2;
            dr["poluch2_ks"] = Rekvizit.korr_schet2;
            dr["poluch2_phone"] = Rekvizit.phone2;

            dr["poluch"] = Rekvizit.poluch;
            dr["poluch_adres"] = Rekvizit.adres;
            dr["poluch_rs"] = Rekvizit.rschet;
            dr["poluch_bank"] = Rekvizit.bank;
            dr["poluch_inn"] = Rekvizit.inn;
            dr["poluch_ks"] = Rekvizit.korr_schet;
            dr["poluch_phone"] = Rekvizit.phone;
            dr["ukls"] = Rekvizit.code_uk;


            dr["Data_dolg"] = "01." + Month.ToString("00") + "." + Year;
            dr["month_"] = Month;
            dr["year_"] = Year;
            dr["months"] = FullMonthName;

            if (NumberFlat.IndexOf("комн.", StringComparison.Ordinal) >= 0)
            {
                dr["kvnum"] = NumberFlat.Replace("комн.", "(") + ")";
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
            List<Counters> sortList = new List<Counters>();
            sortList.AddRange(ListCounters.FindAll(c => c.NzpServ == 6));
            sortList.AddRange(ListCounters.FindAll(c => c.NzpServ == 9));
            sortList.AddRange(ListCounters.FindAll(c => c.NzpServ == 25));
            sortList.AddRange(ListCounters.FindAll(c => (c.NzpServ != 6 && c.NzpServ != 9 && c.NzpServ != 25)));
            for (int i = 0; i < Math.Min(8, sortList.Count); i++)
            {
                switch (sortList[i].NzpServ)
                {
                    case 6: dr["lsserv" + countersIndex] = countersIndex + ". х/в " + sortList[i].Place.Trim(); break;
                    case 9: dr["lsserv" + countersIndex] = countersIndex + ". г/в " + sortList[i].Place.Trim(); break;
                    case 25: dr["lsserv" + countersIndex] = countersIndex + ". Эл.снаб. " + sortList[i].Place.Trim(); break;
                    case 8: dr["lsserv" + countersIndex] = countersIndex + ". Отопл. " + sortList[i].Place.Trim(); break;
                    case 210: dr["lsserv" + countersIndex] = countersIndex + ". Ноч.Эл. " + sortList[i].Place.Trim(); break;
                    default: dr["lsserv" + countersIndex] = countersIndex + ". " + sortList[i].ServiceName; break;
                }
                dr["lsnumcnt" + countersIndex] = sortList[i].NumCounters;
                dr["lsdatuchet1_" + countersIndex] = sortList[i].DatUchetPred.ToShortDateString();
                dr["lsdatuchet2_" + countersIndex] = sortList[i].DatUchet.ToShortDateString();
                dr["lsvalcnt1_" + countersIndex] = sortList[i].ValuePred;
                dr["lsvalcnt2_" + countersIndex] = sortList[i].Value;
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
            List<Counters> sortList = new List<Counters>();
            sortList.AddRange(ListDomCounters.FindAll(c => c.NzpServ == 6));
            sortList.AddRange(ListDomCounters.FindAll(c => c.NzpServ == 9));
            sortList.AddRange(ListDomCounters.FindAll(c => c.NzpServ == 25));
            sortList.AddRange(ListDomCounters.FindAll(c => (c.NzpServ != 6 && c.NzpServ != 9 && c.NzpServ != 25)));
            for (int i = 0; i < Math.Min(8, sortList.Count); i++)
            {
                
                switch (sortList[i].NzpServ)
                {
                    case 6: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Хол. вода"; break;
                    case 9: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Гор. вода"; break;
                    case 25: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Эл.снаб."; break;
                    case 8: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Отопл."; break;
                    case 210: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Ноч.Эл.."; break;
                    default: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". " + sortList[i].ServiceName; break;
                }
                if (ListVolume.Find(x => x.NzpServ == sortList[i].NzpServ) != null)
                {
                    if (((ListVolume.Find(x => x.NzpServ == sortList[i].NzpServ).NzpServ == 25) & (HasElDpu)) ||
                        ((ListVolume.Find(x => x.NzpServ == sortList[i].NzpServ).NzpServ == 6) & (HasHvsDpu)) ||
                        ((ListVolume.Find(x => x.NzpServ == sortList[i].NzpServ).NzpServ == 9) & (HasGvsDpu)) ||
                        ((ListVolume.Find(x => x.NzpServ == sortList[i].NzpServ).NzpServ == 10) & (HasGazDpu)) ||
                        ((ListVolume.Find(x => x.NzpServ == sortList[i].NzpServ).NzpServ == 8) & (HasOtopDpu)))
                    {
                        int numRec = ListVolume.IndexOf(ListVolume.Find(x => x.NzpServ == sortList[i].NzpServ));
                        if (numRec > -1)
                        {
                            FillGoodServVolume(dr, ListVolume[numRec].AllLsVolume,
                                "rash_dpu" + countersIndex);

                            FillGoodServVolume(dr, ListVolume[numRec].DomVolume, "rash_dpu_pu" + countersIndex);
                            FillGoodServVolume(dr, ListVolume[numRec].OdnDomVolume, "rash_dpu_odn" + countersIndex);
                        }
                    }
                }

                //dr["domserv" + countersIndex] = listDomCounters[i].serviceName;
                dr["domnumcnt" + countersIndex] = sortList[i].NumCounters;
                dr["domdatuchet1_" + countersIndex] = sortList[i].DatUchetPred.ToShortDateString();
                dr["domdatuchet2_" + countersIndex] = sortList[i].DatUchet.ToShortDateString();
                dr["domvalcnt1_" + countersIndex] = sortList[i].ValuePred.ToString("0.00##");
                dr["domvalcnt2_" + countersIndex] = sortList[i].Value.ToString("0.00##");
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
        /// Заполнение реквизитов территории
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public override bool FillAreaData(DataRow dr)
        {
            if (dr == null) return false;
            dr["areaName"] = AreaName ?? "";
            dr["areaAdr"] = AreaAdr ?? "";
            dr["areaEmail"] = AreaEmail ?? "";
            dr["areaWeb"] = AreaWeb ?? "";
            dr["areaPhone"] = AreaPhone ?? "";
            dr["areaFax"] = AreaFax ?? "";
            dr["remark"] = AreaRemark ?? "";
            dr["worktime"] = AreaWorkTime ?? "";
            return true;
        }

        /// <summary>
        /// дата последней оплаты по счету
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillDatOpl(DataRow dr)
        {
            if (dr == null) return false;

            if (DateOplat != "")
            {
                dr["dat_opl"] = DateOplat + "г, ";
                dr["sum_last_opl"] = LastSumOplat + " руб.";
            }
            return true;
        }


        public KalugaFaktura()
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
            FakturaBlocks.HasCalcGil = true;
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
            FakturaBlocks.HasAreaDataBlock = true;
            FakturaBlocks.HasDatOplBlock = true;
            FakturaBlocks.HasRTCountersDoubleBlock = false;
            FakturaBlocks.HasRTCountersDoubleDomBlock = true;

            Clear();
            Kfodnhvs = "0.03";
        }

    }


      
   
}

