
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



    public class ZelFaktura : BaseFactura
    {
        private int maxNachStringCount = 32;
        private int maxCountersCount = 8;
        private int maxLgotStringCount = 5;

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

                for (int i = 1; i < 33; i++)
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
        protected override bool FillKvarPrm(DataRow dr)
        {
            if (dr == null) return false;

            dr["kolgil"] = CountGil;
            dr["kolvgil"] = CountDepartureGil;
            dr["kv_pl"] = FullSquare.ToString("0.00");
            dr["et"] = Stage;
            if (IsolateFlat)
                dr["komf"] = "изолированное";
            else
                dr["komf"] = "коммунальное";


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

            dr["sb1"] = Rekvizit.poluch;
            dr["sb2"] = Rekvizit.bank;
            dr["sb3"] = Rekvizit.rschet;
            dr["sb4"] = Rekvizit.korr_schet;
            dr["sb5"] = Rekvizit.bik;
            dr["sb6"] = Rekvizit.inn;
            dr["sb7"] = Rekvizit.phone;
            dr["sb8"] = Rekvizit.adres;
            dr["sb9"] = Rekvizit.pm_note;
            dr["sb10"] = Rekvizit.poluch2;
            dr["sb11"] = Rekvizit.bank2;
            dr["sb12"] = Rekvizit.rschet2;
            dr["sb13"] = Rekvizit.korr_schet2;
            dr["sb14"] = Rekvizit.bik2;
            dr["sb15"] = Rekvizit.inn2;

            dr["poluch"] = Rekvizit.ercName;

         

            dr["months"] = FullMonthName;
            dr["date_print"] = DateTime.Now.ToShortDateString();
            dr["datedestv"] = DateTime.Now.ToString("28.MM.yyyy");
            dr["vars2"] = GeuKodErc;
            return true;
        }



        /// <summary>
        /// Добавление услуги в счета
        /// </summary>
        /// <param name="cServ">Правила объединения услуг</param>
        /// <param name="aServ">Услуга</param>
        public override void AddServ(BaseServ aServ)
        {

            //Проверка на пустоту добавляемой услуги
            if (aServ.Empty()) return;

            for (int i = 0; i < ListKommServ.Count; i++)
            {
                if (ListKommServ[i].Serv.NzpServ == aServ.Serv.NzpServ)
                {
                    aServ.KommServ = true;
                }
            }

            SummaryServ.AddSum(aServ.Serv, false); //Подсчитываем Итого
            if ((System.Math.Abs(aServ.Serv.Reval) > 0.001m) || (System.Math.Abs(aServ.Serv.RealCharge) > 0.001m))
            {
                AddReval(aServ.Serv);
            }
            BaseServ mainServ = CUnionServ.GetMainServBySlave(aServ.Serv.NzpServ);
            //Определяем к какой услуге добавить
            if (mainServ == null) //услуга не объединяемая
            {
                bool findServ = false;
                for (int i = 0; i < ListServ.Count; i++)
                {
                    if (ListServ[i].Serv.NzpServ == aServ.Serv.NzpServ) //если такая услуга уже есть то добавляем к ней
                    {
                        ListServ[i].AddSum(aServ.Serv);
                        //listServ[i].serv.nameServ = aServ.serv.nameServ;
                        findServ = true;
                    }
                }
                if (!findServ) ListServ.Add(aServ); //иначе добавляем новую
            }
            else //если услуга объединяемая
            {
                bool findServ = false;
                for (int i = 0; i < ListServ.Count; i++)
                {
                    if (ListServ[i].Serv.NzpServ == mainServ.Serv.NzpServ) //если мастер услуга уже присутствует, то добавляем к ней
                    {
                        if (aServ.Serv.IsOdn) aServ.Serv.CanAddTarif = false;
                 
                        ListServ[i].AddSum(aServ.Serv);

                        if (aServ.Serv.NzpServ == 513) aServ.Serv.NzpServ = 9;
                        if (aServ.Serv.NzpServ == 514) aServ.Serv.NzpServ = 14;
                        //если услуга 14 или 9 то добавляем услуги
                        if ((aServ.Serv.NzpServ == 9) ||
                            (aServ.Serv.NzpServ == 14))

                        {
                            ListServ[i].AddSlave(aServ.Serv);
                        }

                        findServ = true;
                    }
                }
                if (!findServ) //Не найдена объединенная услуга, добавляем её
                {
                    BaseServ newMainServ = (BaseServ)mainServ.Clone();
                    newMainServ.KommServ = aServ.KommServ;
                    if (aServ.Serv.IsOdn) aServ.Serv.CanAddTarif = false;
                    newMainServ.AddSum(aServ.Serv);

                    if (aServ.Serv.NzpServ == 513) aServ.Serv.NzpServ = 9;
                    if (aServ.Serv.NzpServ == 514) aServ.Serv.NzpServ = 14;

                    if ((aServ.Serv.NzpServ == 9) ||
                           (aServ.Serv.NzpServ == 14))
                    {
                        newMainServ.AddSlave(aServ.Serv);
                    }
                    ListServ.Add(newMainServ);
                }
            }

          

        }

        /// <summary>
        /// Заполнение одной строки в таблице начислений
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="stringIndex">Номер строки</param>
        /// <param name="bs">Услуга</param>
        /// <returns></returns>
        protected virtual bool FillOneRowInChargeGrid(DataRow dr,ref int stringIndex, BaseServ bs, string num_st)
        {
            //Если в наборе больше строк чем в счете, то не пытаемся печатать лишние строки
            if (stringIndex > maxNachStringCount) return true;
            if (bs != null)
            {
                dr["num" + stringIndex] = num_st;
                if (bs.Serv.NzpServ == 9)
                    dr["name_serv" + stringIndex] = "Горячее водоснабжение";
                else
                    dr["name_serv" + stringIndex] = bs.Serv.NameServ;
                dr["measure" + stringIndex] = bs.Serv.Measure;
                dr["tarif" + stringIndex] = bs.Serv.Tarif.ToString("0.00");
                dr["sum_dolg" + stringIndex] = bs.Serv.SumInsaldo.ToString("0.00");
                dr["sum_money" + stringIndex] = bs.Serv.SumMoney.ToString("0.00");
                dr["rsum_tarif_all" + stringIndex] = bs.Serv.RsumTarif.ToString("0.00");
                dr["sum_tarif" + stringIndex] = bs.Serv.SumTarif.ToString("0.00");
                dr["sum_charge_all" + stringIndex] = bs.Serv.SumCharge.ToString("0.00");
                dr["reval" + stringIndex] = (bs.Serv.RealCharge + bs.Serv.Reval).ToString("0.00");
                dr["sum_nedop" + stringIndex] = bs.Serv.SumNedop.ToString("0.00");
                stringIndex++;
                foreach (SumServ ss in bs.SlaveServ)
                {
                    dr["name_serv" + stringIndex] = "в т.ч." + ss.NameServ;
                    dr["measure" + stringIndex] = ss.Measure;
                    dr["tarif" + stringIndex] = ss.Tarif.ToString("0.00");
                    dr["sum_dolg" + stringIndex] = ss.SumInsaldo.ToString("0.00");
                    dr["sum_money" + stringIndex] = ss.SumMoney.ToString("0.00");
                    dr["rsum_tarif_all" + stringIndex] = ss.RsumTarif.ToString("0.00");
                    dr["sum_tarif" + stringIndex] = ss.SumTarif.ToString("0.00");
                    dr["sum_charge_all" + stringIndex] = ss.SumCharge.ToString("0.00");
                    dr["reval" + stringIndex] = (ss.RealCharge + ss.Reval).ToString("0.00");
                    dr["sum_nedop" + stringIndex] = ss.SumNedop.ToString("0.00");
                    stringIndex++;
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
            int stIndex = 1;

          
            int num_st = 0;
            BaseServ itogServ = new BaseServ(false);

            dr["kommItogIndex"] = 0;
            dr["gilItogIndex"] = 0;

            #region Коммунальные услуги
            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (ListServ[countServ].KommServ)
                {
                    if (IsShowServInGrid(ListServ[countServ]))
                    {
                        itogServ.Serv.AddSum(ListServ[countServ].Serv);
                        num_st++;
                        FillOneRowInChargeGrid(dr,ref stIndex, ListServ[countServ], num_st.ToString());
                       
                    }
                }
            }
            if ((stIndex > 1) & (stIndex <= maxNachStringCount))
            {
                dr["name_serv" + stIndex] = "<b>Итого по коммунальным услугам</b>";
                dr["measure" + stIndex] = "";
                dr["tarif" + stIndex] = "";
                dr["sum_dolg" + stIndex] = "<b>" + itogServ.Serv.SumInsaldo.ToString("0.00") + "</b>";
                dr["rsum_tarif_all" + stIndex] = "<b>" + itogServ.Serv.RsumTarif.ToString("0.00") + "</b>";
                dr["sum_tarif" + stIndex] = "<b>" + itogServ.Serv.SumTarif.ToString("0.00") + "</b>";
                dr["reval" + stIndex] = "<b>" + (itogServ.Serv.Reval +
                itogServ.Serv.RealCharge).ToString("0.00") + "</b>";
                dr["sum_charge_all" + stIndex] = "<b>" + itogServ.Serv.SumCharge.ToString("0.00") + "</b>";
                dr["sum_nedop" + stIndex] = "<b>" + itogServ.Serv.SumNedop.ToString("0.00") + "</b>";
                dr["kommItogIndex"] = stIndex;
                stIndex++;
            }

            #endregion
            itogServ.Serv.Clear();

            #region Жилищные услуги
            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (!ListServ[countServ].KommServ)
                {
                    if (IsShowServInGrid(ListServ[countServ]))
                    {
                        itogServ.Serv.AddSum(ListServ[countServ].Serv);
                        num_st++;
                        FillOneRowInChargeGrid(dr,ref stIndex, ListServ[countServ], num_st.ToString());
                    }
                }
            }

            if (((itogServ.Serv.RsumTarif + System.Math.Abs(itogServ.Serv.RealCharge) +
                System.Math.Abs(itogServ.Serv.SumCharge) +
                System.Math.Abs(itogServ.Serv.SumPere) > 0.001m)) & (stIndex <= maxNachStringCount))
            {
                dr["name_serv" + stIndex] = "<b>Итого по жилищным услугам</b>";
                dr["measure" + stIndex] = "";
                dr["tarif" + stIndex] = "";
                dr["sum_dolg" + stIndex] = "<b>" + itogServ.Serv.SumInsaldo.ToString("0.00") + "</b>";
                dr["rsum_tarif_all" + stIndex] = "<b>" + itogServ.Serv.RsumTarif.ToString("0.00") + "</b>";
                dr["sum_tarif" + stIndex] = "<b>" + itogServ.Serv.SumTarif.ToString("0.00") + "</b>";
                dr["reval" + stIndex] = "<b>" + (itogServ.Serv.Reval +
                itogServ.Serv.RealCharge).ToString("0.00") + "</b>";
                dr["sum_charge_all" + stIndex] = "<b>" + itogServ.Serv.SumCharge.ToString("0.00") + "</b>";
                dr["sum_nedop" + stIndex] = "<b>" + itogServ.Serv.SumNedop.ToString("0.00") + "</b>";
                dr["gilItogIndex"] = stIndex;
                stIndex++;
            }

            #endregion
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
            for (int i = 0; i < Math.Min(maxCountersCount,NewlistCounters.Count); i++)
            {
                switch (NewlistCounters[i].NzpServ)
                {
                    case 6: dr["lsserv" + countersIndex] = countersIndex + ". Хол. вода"; break;
                    case 9: dr["lsserv" + countersIndex] = countersIndex + ". Гор. вода"; break;
                    case 8: dr["lsserv" + countersIndex] = countersIndex + ". Отопл."; break;
                    case 25: dr["lsserv" + countersIndex] = countersIndex + ". Эл.снаб."; break;
                    case 210: dr["lsserv" + countersIndex] = countersIndex + ". Ноч.Эл.."; break;
                    default: dr["lsserv" + countersIndex] = countersIndex + ". " + ListCounters[i].ServiceName; break;
                }
                dr["lsnumcnt" + countersIndex] = NewlistCounters[i].NumCounter;
                dr["lsdatuchet2_" + countersIndex] = NewlistCounters[i].DatUchet.ToShortDateString();
                dr["lsvalcnt2_" + countersIndex] = NewlistCounters[i].Value;
                dr["srokslugb" + countersIndex] = 
                    NewlistCounters[i].IsProv(new DateTime(Year,Month,1)) == 1 ? "": "Срок службы истек "+
                    NewlistCounters[i].DatProv;
                countersIndex++;
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
                dr["pkod"] = "234";
            dr["numdom"] = NumberDom;
            dr["kvnum"] = NumberFlat;
            dr["ulica"] = Ulica;
            return true;
        }

        /// <summary>
        /// Загрузка информации от СЗ
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillSzInf(DataRow dr)
        {
            if (dr == null) return false;
            int gilIndex = 1;
            for (int i = 0; i < Math.Min(maxLgotStringCount, SzInformation.ListGilec.Count); i++)
            {
                dr["lgfio" + gilIndex] = SzInformation.ListGilec[i].FIO;
                dr["lglgota" + gilIndex] = SzInformation.ListGilec[i].SumLgota;
                dr["lgsubs" + gilIndex] = SzInformation.ListGilec[i].SumSubs + SzInformation.ListGilec[i].SumTepl;
                dr["lgedv" + gilIndex] = SzInformation.ListGilec[i].SumEdv;
                dr["lgsv" + gilIndex] = SzInformation.ListGilec[i].SumSv;
                gilIndex++;
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
            if (dr == null) return false;
            foreach (ServVolume sv in ListVolume)
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
                    if (sv.NzpServ == 8)
                    {
                        dr[servPref + "_kv"] = sv.IsPu == 1 ? sv.PUVolume.ToString("0.000####") : sv.NormaFullVolume.ToString("0.000####");
                    }
                    else if (sv.NzpServ == 7)
                    {
                        if (Math.Abs(sv.PUVolume + sv.NormaFullVolume) > 0.001m)
                        {
                            dr[servPref + "_kv"] = sv.IsPu == 1 ? sv.PUVolume.ToString("0.000") : sv.NormaFullVolume.ToString("0.000");
                            dr[servPref + "_norm"] = sv.NormaFullVolume.ToString("0.00");
                            dr[servPref + "_odn"] = (sv.OdnFlatPuVolume + sv.OdnFlatNormVolume).ToString("0.000");
                            dr[servPref] = (sv.OdnFlatPuVolume + sv.OdnFlatNormVolume +
                                sv.IsPu == 1 ? sv.PUVolume : sv.NormaFullVolume).ToString("0.000");

                        }
                    }
                    else
                    {
                        dr[servPref + "_kv"] = sv.IsPu == 1 ? sv.PUVolume.ToString("0.000") : sv.NormaFullVolume.ToString("0.000");
                        dr[servPref + "_norm"] = sv.NormaFullVolume.ToString("0.00");

                        dr[servPref + "_odn"] = (sv.OdnFlatPuVolume + sv.OdnFlatNormVolume).ToString("0.000");
                        dr[servPref] = (sv.OdnFlatPuVolume + sv.OdnFlatNormVolume +
                            sv.IsPu == 1 ? sv.PUVolume : sv.NormaFullVolume).ToString("0.000");
                    }
                }
            }

            FillDomRashod(dr);
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
            dr["reval"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["sum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["rsum_tarif_all"] = SummaryServ.Serv.RsumTarif.ToString("0.00");
            dr["sum_nedop"] = SummaryServ.Serv.SumNedop.ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_dolg"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            
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
        public override bool IsShowServInGrid(BaseServ aServ)
        {
            if ((System.Math.Abs(aServ.Serv.Tarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RsumTarif) < 0.001m) &
                (System.Math.Abs(aServ.Serv.Reval) < 0.001m) &
                (System.Math.Abs(aServ.Serv.RealCharge) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumNedop) < 0.001m) &
                (System.Math.Abs(aServ.Serv.SumCharge) < 0.001m)
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


        public override bool FillGeuData(DataRow dr)
        {
            if (dr == null) return false;
            dr["vars2"] = GeuKodErc;
            return true;
        }

        public ZelFaktura() : 
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
            FakturaBlocks.HasServiceVolumeBlock = true;
            FakturaBlocks.HasRassrochka = false;
            FakturaBlocks.HasCountersBlock = false;
            FakturaBlocks.HasNewCountersBlock = true;
            FakturaBlocks.HasCountersDoubleBlock = false;
            FakturaBlocks.HasCountersDoubleDomBlock = false;
            FakturaBlocks.HasNormblock = true;
            FakturaBlocks.HasSzBlock = true;
            FakturaBlocks.HasAreaDataBlock = false;
            FakturaBlocks.HasGeuDataBlock = true;
            FakturaBlocks.HasDomRashodBlock = true;
            

            Clear();
    
        }
    }
   
}

