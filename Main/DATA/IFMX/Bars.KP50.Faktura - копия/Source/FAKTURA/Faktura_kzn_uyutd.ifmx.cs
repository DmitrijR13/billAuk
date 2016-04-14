
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


    public class KznUyutdFaktura : BaseFactura
    {

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
            table.Columns.Add("remark", typeof(string));
            table.Columns.Add("datedestv", typeof(string));
            table.Columns.Add("kommItogIndex", typeof(int));
            table.Columns.Add("gilItogIndex", typeof(int));

            table.Columns.Add("areaAdsPhone", typeof(string));
            table.Columns.Add("areaAdr", typeof(string));
            table.Columns.Add("areaDirectorFio", typeof(string));
            table.Columns.Add("areaDirectorPost", typeof(string));
            table.Columns.Add("areaEmail", typeof(string));
            table.Columns.Add("areaWeb", typeof(string));
            table.Columns.Add("areaPhone", typeof(string));

            table.Columns.Add("geuPhone", typeof(string));
            table.Columns.Add("geuAdr", typeof(string));
            table.Columns.Add("geuName", typeof(string));
            table.Columns.Add("geuPref", typeof(string));
            table.Columns.Add("geuDatPlat", typeof(string));
            table.Columns.Add("upravDom", typeof(string));

            for (int i = 1; i < 16; i++)
                table.Columns.Add("st_" + i, typeof(string));

            table.Columns.Add("sum_insaldo", typeof(string));
            table.Columns.Add("sum_ito", typeof(string));
            table.Columns.Add("reval_charge", typeof(string));
            table.Columns.Add("sum_money", typeof(string));
            table.Columns.Add("sum_ticket", typeof(string));
            table.Columns.Add("sum_outsaldo", typeof(string));
            table.Columns.Add("rsum_tarif_all", typeof(string));
            table.Columns.Add("sum_sn", typeof(string));
            table.Columns.Add("rsum_tarif", typeof(string));
            table.Columns.Add("rsum_tarif_odn", typeof(string));
            table.Columns.Add("sum_nedop", typeof(string));
            table.Columns.Add("sum_charge_all", typeof(string));
            table.Columns.Add("vars", typeof(string));
            table.Columns.Add("vars2", typeof(string));


            for (int i = 1; i < 30; i++)
            {
                table.Columns.Add("num" + i, typeof(string));
                table.Columns.Add("name_serv" + i, typeof(string));
                table.Columns.Add("measure" + i, typeof(string));
                table.Columns.Add("tarif" + i, typeof(string));
                table.Columns.Add("sum_dolg" + i, typeof(string));
                table.Columns.Add("sum_money" + i, typeof(string));
                table.Columns.Add("rsum_tarif" + i, typeof(string));
                table.Columns.Add("rsum_tarif_all" + i, typeof(string));
                table.Columns.Add("rsum_tarif_odn" + i, typeof(string));
                table.Columns.Add("c_calc" + i, typeof(string));
                table.Columns.Add("c_calc_all" + i, typeof(string));
                table.Columns.Add("c_calc_odn" + i, typeof(string));
                table.Columns.Add("reval" + i, typeof(string));
                table.Columns.Add("sum_nedop" + i, typeof(string));
                table.Columns.Add("sum_charge_all" + i, typeof(string));
                table.Columns.Add("sum_sn" + i, typeof(string));
                table.Columns.Add("day_nedop" + i, typeof(string));
                table.Columns.Add("sum_ito" + i, typeof(string));
            }


            for (int i = 1; i < 13; i++)
            {
                table.Columns.Add("domserv" + i, typeof(string));
                table.Columns.Add("domnumcnt" + i, typeof(string));
                table.Columns.Add("dommeasure" + i, typeof(string));
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

            for (int i = 1; i < 7; i++)
            {
                table.Columns.Add("serv_pere" + i.ToString(), typeof(string));
                table.Columns.Add("osn_pere" + i.ToString(), typeof(string));
                table.Columns.Add("sum_pere" + i.ToString(), typeof(string));
                table.Columns.Add("period_pere" + i.ToString(), typeof(string));
            }

            table.Columns.Add("el_dpu", typeof(string));
            table.Columns.Add("el_dpu_odn", typeof(string));
            table.Columns.Add("el_arend", typeof(string));

            table.Columns.Add("ni_dpu", typeof(string));
            table.Columns.Add("ni_dpu_odn", typeof(string));
            table.Columns.Add("ni_arend", typeof(string));

            table.Columns.Add("hv_dpu", typeof(string));
            table.Columns.Add("hv_dpu_odn", typeof(string));
            table.Columns.Add("hv_arend", typeof(string));

            table.Columns.Add("gv_dpu", typeof(string));
            table.Columns.Add("gv_dpu_odn", typeof(string));
            table.Columns.Add("gv_arend", typeof(string));

            table.Columns.Add("otop_dpu", typeof(string));
            table.Columns.Add("otop_dpu_odn", typeof(string));
            table.Columns.Add("otop_arend", typeof(string));

            table.Columns.Add("gv_dpu_gkal", typeof(string));

            table.Columns.Add("pref_podr", typeof(string));
            table.Columns.Add("mdom", typeof(string));
            table.Columns.Add("name_podr", typeof(string));
            table.Columns.Add("k_el", typeof(string));
            table.Columns.Add("k_hv", typeof(string));
            table.Columns.Add("k_gv", typeof(string));

            table.Columns.Add("months_sz", typeof(string));
            table.Columns.Add("ls_lgota", typeof(string));
            table.Columns.Add("ls_edv", typeof(string));
            table.Columns.Add("ls_tepl", typeof(string));
            table.Columns.Add("ls_smo", typeof(string));
            table.Columns.Add("numkvit", typeof(string));
            
         


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
            dr["kv_pl"] = FullSquare.ToString("0.00");

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


            dr["poluch"] = Rekvizit.poluch;
            dr["poluch_adres"] = Rekvizit.adres;
            dr["poluch_rs"] = Rekvizit.rschet;
            dr["poluch_bank"] = Rekvizit.bank;
            dr["poluch_inn"] = Rekvizit.inn;
            dr["poluch_ks"] = Rekvizit.korr_schet;
            dr["poluch_phone"] = Rekvizit.phone;
            dr["pm_note"] = Rekvizit.pm_note;

            dr["poluch2"] = Rekvizit.poluch2;
            dr["poluch2_adres"] = Rekvizit.adres2;
            dr["poluch2_rs"] = Rekvizit.rschet2;
            dr["poluch2_bank"] = Rekvizit.bank2;
            dr["poluch2_inn"] = Rekvizit.inn2;
            dr["poluch2_ks"] = Rekvizit.korr_schet2;
            dr["poluch2_phone"] = Rekvizit.phone2;

            dr["months"] = FullMonthName;
            dr["date_print"] = DateTime.Now.ToShortDateString();
            dr["datedestv"] = DateTime.Now.ToString("28.MM.yyyy");
            dr["vars2"] = GeuKodErc;
            return true;
        }

        /// <summary>
        /// Заполнение одной строки в таблице начислений
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="stringIndex">Номер строки</param>
        /// <param name="bs">Услуга</param>
        /// <returns></returns>
        protected override bool FillOneRowInChargeGrid(DataRow dr, int stringIndex, BaseServ bs, string numSt)
        {
            if (bs == null)
            {
                dr["name_serv" + stringIndex] = "";
                dr["measure" + stringIndex] = "";
                dr["c_calc_all" + stringIndex] = "";
                dr["c_calc_odn" + stringIndex] = "";
                dr["tarif" + stringIndex] = "";
                dr["rsum_tarif_all" + stringIndex] = "";
                dr["rsum_tarif_odn" + stringIndex] = "";
                dr["reval" + stringIndex] = "";
                dr["sum_nedop" + stringIndex] = "";
                dr["sum_ito" + stringIndex] = "";
                dr["sum_dolg" + stringIndex] = "";
                dr["sum_sn" + stringIndex] = "";
                dr["sum_charge_all" + stringIndex] = "";
                dr["num" + stringIndex] = "";
            }
            else
            {
                dr["num" + stringIndex] = numSt;
                dr["name_serv" + stringIndex] = bs.Serv.NameServ;
                dr["measure" + stringIndex] = bs.Serv.Measure;
                dr["c_calc_all" + stringIndex] = bs.Serv.CCalc.ToString("0.00");
                dr["c_calc_odn" + stringIndex] = bs.ServOdn.CCalc.ToString("0.00");
                dr["tarif" + stringIndex] = bs.Serv.Tarif.ToString("0.00");
                dr["sum_dolg" + stringIndex] = bs.Serv.SumPere.ToString("0.00");
                dr["sum_money" + stringIndex] = bs.Serv.SumMoney.ToString("0.00");
                dr["sum_sn" + stringIndex] = bs.Serv.SumSn.ToString("0.00");
                dr["rsum_tarif_all" + stringIndex] = bs.Serv.RsumTarif.ToString("0.00");
                dr["sum_charge_all" + stringIndex] = bs.Serv.SumCharge.ToString("0.00");
                dr["rsum_tarif_odn" + stringIndex] = bs.ServOdn.RsumTarif.ToString("0.00");
                dr["reval" + stringIndex] = (bs.Serv.RealCharge + bs.Serv.Reval).ToString("0.00");
                dr["sum_nedop" + stringIndex] = bs.Serv.SumNedop.ToString("0.00");
                dr["day_nedop" + stringIndex] = (DateTime.DaysInMonth(Year, Month) - bs.Serv.COkaz).ToString("0.00");;
                dr["sum_ito" + stringIndex] = (bs.Serv.RsumTarif -
                    bs.Serv.SumNedop + bs.Serv.Reval + bs.Serv.RealCharge).ToString("0.00");
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

            decimal sum_charge = 0;
            decimal rsum_tarif = 0;
            decimal rsum_tarif_odn = 0;
            decimal reval_charge = 0;
            decimal sum_nedop = 0;
            decimal sum_sn = 0; 
            decimal sum_pere = 0;
            int num_st = 0;
            dr["kommItogIndex"] = 0;
            dr["gilItogIndex"] = 0;

            #region Коммунальные услуги
            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (ListServ[countServ].KommServ)
                {
                    if (IsShowServInGrid(ListServ[countServ]))
                    {
                        

                        rsum_tarif = rsum_tarif + ListServ[countServ].Serv.RsumTarif;
                        reval_charge = reval_charge + ListServ[countServ].Serv.Reval +
                            ListServ[countServ].Serv.RealCharge;
                        sum_nedop = sum_nedop + ListServ[countServ].Serv.SumNedop;
                        rsum_tarif_odn = rsum_tarif_odn + ListServ[countServ].ServOdn.RsumTarif;
                        sum_sn = sum_sn + ListServ[countServ].Serv.SumSn;
                        sum_charge = sum_charge + ListServ[countServ].Serv.SumCharge;
                        num_st++;
                        FillOneRowInChargeGrid(dr, stIndex, ListServ[countServ], num_st.ToString());
                        stIndex++;

                    }
                }
            }
            if (stIndex > 1)
            {
                dr["name_serv" + stIndex] = "<b>Итого по коммунальным услугам</b>";
                dr["measure" + stIndex] = "";
                dr["c_calc_all" + stIndex] = "";
                dr["c_calc_odn" + stIndex] = "";
                dr["tarif" + stIndex] = "";
                dr["rsum_tarif_all" + stIndex] = "<b>" + rsum_tarif.ToString("0.00") + "</b>";
                dr["rsum_tarif_odn" + stIndex] = "<b>" + rsum_tarif_odn.ToString("0.00") + "</b>";
                dr["reval" + stIndex] = "<b>" + reval_charge.ToString("0.00") + "</b>";
                dr["sum_charge_all" + stIndex] = "<b>" + sum_charge.ToString("0.00") + "</b>";
                dr["sum_nedop" + stIndex] = "<b>" + sum_nedop.ToString("0.00") + "</b>";
                dr["kommItogIndex"] = stIndex;
                stIndex++;
            }

            #endregion

            sum_charge = 0;
            rsum_tarif = 0;
            rsum_tarif_odn = 0;
            reval_charge = 0;
            sum_nedop = 0;
            sum_sn = 0;

            #region Жилищные услуги
            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (!ListServ[countServ].KommServ)
                {
                    if (IsShowServInGrid(ListServ[countServ]))
                    {

                        rsum_tarif = rsum_tarif + ListServ[countServ].Serv.RsumTarif;
                        reval_charge = reval_charge + ListServ[countServ].Serv.Reval +
                            ListServ[countServ].Serv.RealCharge;
                        sum_nedop = sum_nedop + ListServ[countServ].Serv.SumNedop;
                        rsum_tarif_odn = rsum_tarif_odn + ListServ[countServ].ServOdn.RsumTarif;
                        sum_sn = sum_sn + ListServ[countServ].Serv.SumSn;
                        sum_charge = sum_charge + ListServ[countServ].Serv.SumCharge;
                        sum_pere = sum_pere + ListServ[countServ].Serv.SumPere;
                        num_st++;
                        FillOneRowInChargeGrid(dr, stIndex, ListServ[countServ], num_st.ToString());
                        stIndex++;
                    }
                }
            }

            if ((rsum_tarif + System.Math.Abs(reval_charge) + System.Math.Abs(sum_charge) + 
                System.Math.Abs(sum_pere) > 0.001m))
            {
                dr["name_serv" + stIndex] = "<b>Итого по жилищным услугам</b>";
                dr["measure" + stIndex] = "";
                dr["c_calc_all" + stIndex] = "";
                dr["c_calc_odn" + stIndex] = "";
                dr["tarif" + stIndex] = "";
                dr["rsum_tarif_all" + stIndex] = "<b>" + rsum_tarif.ToString("0.00") + "</b>";
                dr["rsum_tarif_odn" + stIndex] = "0.00";
                dr["reval" + stIndex] = "<b>" + reval_charge.ToString("0.00") + "</b>"; ;
                dr["sum_nedop" + stIndex] = "<b>" + sum_nedop.ToString("0.00") + "</b>"; ;
                dr["sum_charge_all" + stIndex] = "<b>" + (sum_charge).ToString("0.00") + "</b>"; 
                dr["gilItogIndex"] = stIndex;
                stIndex++;
            }

            #endregion



            for (int i = stIndex; i < 30; i++)
            {
                FillOneRowInChargeGrid(dr, stIndex, null, "");
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
            for (int i = 0; i < Math.Min(12,ListCounters.Count); i++)
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
                dr["lsdatuchet1_" + countersIndex] = ListCounters[i].DatUchetPred.ToShortDateString();
                dr["lsdatuchet2_" + countersIndex] = ListCounters[i].DatUchet.ToShortDateString(); 
                dr["lsvalcnt1_" + countersIndex] = ListCounters[i].ValuePred;
                dr["lsvalcnt2_" + countersIndex] = ListCounters[i].Value;
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
            decimal gkalRashod = 0;
            for (int i = 0; i < Math.Min(12,ListDomCounters.Count); i++)
            {
                switch (ListDomCounters[i].NzpServ)
                {
                    case 6: dr["domserv" + countersIndex] = countersIndex + ". Хол. вода"; break;
                    case 9:
                        {
                            dr["domserv" + countersIndex] = countersIndex + ". Гор. вода";
                            if (ListDomCounters[i].IsGkal)
                                gkalRashod += ListDomCounters[i].Value - ListDomCounters[i].ValuePred;
                        } break;
                    case 8: dr["domserv" + countersIndex] = countersIndex + ". Отопл."; break;
                    case 25: dr["domserv" + countersIndex] = countersIndex + ". Эл.снаб."; break;
                    case 210: dr["domserv" + countersIndex] = countersIndex + ". Ноч.Эл.."; break;
                    default: dr["domserv" + countersIndex] = countersIndex + ". " + ListDomCounters[i].ServiceName; break;
                }

                dr["domserv" + countersIndex] = ListDomCounters[i].ServiceName;
                dr["dommeasure" + countersIndex] = ListDomCounters[i].Measure;
                dr["domnumcnt" + countersIndex] = ListDomCounters[i].NumCounters;
                dr["domdatuchet1_" + countersIndex] = ListDomCounters[i].DatUchetPred.ToShortDateString();
                dr["domdatuchet2_" + countersIndex] = ListDomCounters[i].DatUchet.ToShortDateString();
                dr["domvalcnt1_" + countersIndex] = ListDomCounters[i].ValuePred;
                dr["domvalcnt2_" + countersIndex] = ListDomCounters[i].Value;
                countersIndex++;
            }

            if (gkalRashod > 0m)
            {
                dr["gv_dpu_gkal"] = gkalRashod;
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
                dr["pkod"] = "98";
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

            dr["months_sz"] = MonthPredlog + " " + Year.ToString("0000") + " г.";
            dr["ls_lgota"] = SumLgota;
            dr["ls_edv"] = SumEdv;
            dr["ls_tepl"] = SumTepl;
            dr["ls_smo"] = SumSmo;
            return true;
        }


        protected override bool FillServiceVolume(DataRow dr)
        {
            return FillDomRashod(dr);
        }

        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillSummuryBill(DataRow dr)
        {
            if (dr == null) return false;
            dr["rsum_tarif"] = SummaryServ.Serv.SumTarif.ToString("0.00");
            dr["reval_charge"] = (SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge).ToString("0.00");
            dr["rsum_tarif_odn"] = SummaryServ.ServOdn.RsumTarif.ToString("0.00");
            dr["rsum_tarif_all"] = SummaryServ.Serv.RsumTarif.ToString("0.00");
            dr["sum_nedop"] = SummaryServ.Serv.SumNedop.ToString("0.00");
            dr["sum_sn"] = SummaryServ.Serv.SumSn.ToString("0.00");
            dr["sum_charge_all"] = SummaryServ.Serv.SumCharge.ToString("0.00");
            dr["sum_ito"] = (SummaryServ.Serv.RsumTarif + SummaryServ.Serv.Reval+
            SummaryServ.Serv.RealCharge - SummaryServ.Serv.SumNedop).ToString("0.00");
            dr["sum_insaldo"] = SummaryServ.Serv.SumInsaldo.ToString("0.00");
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_outsaldo"] = SummaryServ.Serv.SumOutsaldo.ToString("0.00");
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
                (System.Math.Abs(aServ.Serv.SumNedop) < 0.001m)
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


        public KznUyutdFaktura() : 
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
            FakturaBlocks.HasRassrochka = false;
            FakturaBlocks.HasCountersBlock = false;
            FakturaBlocks.HasCountersDoubleBlock = true;
            FakturaBlocks.HasCountersDoubleDomBlock = true;
            FakturaBlocks.HasNormblock = false;
            FakturaBlocks.HasSzBlock = true;
            FakturaBlocks.HasAreaDataBlock = true;
            FakturaBlocks.HasGeuDataBlock = true;
            FakturaBlocks.HasUpravDomBlock = true;
            FakturaBlocks.HasDomRashodBlock = true;
            //fakturaBlocks.hasRemarkblock = true;
            Clear();
    
        }
    }
   
}

