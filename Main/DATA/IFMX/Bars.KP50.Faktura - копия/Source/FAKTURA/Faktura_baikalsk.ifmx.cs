using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bars.KP50.Faktura.Source.Base;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Faktura
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;

    public sealed class BaikalskFaktura : BaseFactura
    {
        public string MessageForLs { get; set; }
        public string ContractContentLs { get; set; }
        public string ContractFooterLs { get; set; }

        /// <summary>
        /// Заполнение квартирных парметров
        /// </summary>
        /// <param name="dr"></param>
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
            dr["kolgil2"] = CountRegisterGil;
            dr["kolgil"] = CountGil;
            dr["ls"] = Pkod.Substring(5, 5);
            if (NzpGeu > 100)
                dr["ngeu"] = NzpGeu.ToString(CultureInfo.InvariantCulture).Substring(1, 2);
            else
                dr["ngeu"] = Pkod.Substring(3, 2);
            dr["ud"] = Ud;
            dr["pkod"] = Pkod;
            dr["pl_dom"] = DomSquare.ToString("0.00");
            dr["pl_mop"] = MopSquare.ToString("0.00");
            dr["dom_gil"] = CountDomGil.ToString(CultureInfo.InvariantCulture);
            dr["area_adr"] = Rekvizit.adres2;
            dr["area_phone"] = Rekvizit.phone2;
            return true;
        }

        /// <summary>
        /// Формирование двумерного штрих-кода (QR)
        /// </summary>
        /// <returns>Готовый штрих-код</returns>
        protected override bool FillBarcode(DataRow dr)
        {
            if (Pkod.Length > 13)
                Pkod = Pkod.Substring(0, 13);
            else
                Pkod = Pkod.PadLeft(13, '0');

            var fio = PayerFio.Split(' ');
            var lastName = (fio.Length >= 1)?fio[0]:"";
            var firstName = (fio.Length >= 2) ? fio[1] : "";
            var middleName = (fio.Length >= 3) ? fio[2] : "";
            var index = (Indecs != "" && Indecs != "-" ? Indecs + ", " : "");
            var street = (Ulica != "-" && Ulica != "" ? Ulica + ", " : Ulica);
            var district = (Rajon != "" && Rajon != "-" && Rajon != null ? Rajon + ", " : "");
            var city = (Town != "" && Town != "-" && Town != null ? Town + ", " : "");
            var flat = (NumberFlat != "" && NumberFlat != "-" && NumberFlat != null ? " кв." + NumberFlat : "");
            var room = (NumberRoom != "" && NumberRoom != "-" && NumberRoom != null ? " ком." + NumberRoom : "");

            var textData =
                "ST00011|Name=ОАО «Управление жилищно-коммунальными системами»|PersonalAcc=40702810900021330601|BankName=ОАО «ВостСибтранскомбанк»|BIC=042520849|CorrespAcc=30101810700000000849|" +
                //                "ST00011|Name=ОАО Управление жилищно-коммунальными системами|PersonalAcc=40702810900021330601|BankName=ОАО ВостСибтранскомбанк|BIC=042520849|CorrespAcc=30101810700000000849|" +
                "Sum=" + (Math.Max(0, SumTicket) * 100).ToString("00000000") + "|" +
                "Purpose=Оплата за ЖКУ|" +
                "PayeeINN=3848006291|" +
                "lastName=" + lastName + "|" +
                "firstName=" + firstName + "|" +
                "middleName=" + middleName + "|" +
                "payerAddress=" + index + city + district + street + " д. " + NumberDom + flat + room + "|" +
                "persAcc=" + LicSchet + "|" +
                "paymPeriod=" + Month.ToString("D2") + Year;

            QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.L);
            QrCode qrCode = qrEncoder.Encode(textData);
            GraphicsRenderer renderer = new GraphicsRenderer(new FixedModuleSize(4, QuietZoneModules.Two), Brushes.Black, Brushes.White);

            using (var stream = new MemoryStream())
            {
                renderer.WriteToStream(qrCode.Matrix, ImageFormat.Bmp, stream);
                byte[] bufferBytes = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(bufferBytes, 0, bufferBytes.Length);

                dr["vars"] = bufferBytes;
            }

            return true;
        }

        /// <summary>
        /// Признак отображения услуги в таблице начислений
        /// </summary>
        /// <param name="aServ"></param>
        /// <returns></returns>
        public override bool IsShowServInGrid(BaseServ aServ)
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
        public override void FinalPass(Faktura finder)
        {
            base.FinalPass(finder);

            if (SummaryServ.Serv.SumCharge < 0) SummaryServ.Serv.SumCharge = 0;
            SumTicket = SummaryServ.Serv.SumCharge;

            SumTicket = SummaryServ.Serv.SumCharge;
            if (finder.newSumOpl > 0.001m)
                SumTicket = finder.newSumOpl;

        }

        /// <summary>
        /// Заполнение адреса
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        protected override bool FillAdr(DataRow dr)
        {
            if (dr == null)
                return false;
            dr["fio"] = PayerFio;
            dr["rajon"] = Rajon;
            dr["ulica"] = Ulica;
            dr["numdom"] = NumberDom;
            if (!string.IsNullOrEmpty(NumberFlat))
            {
                dr["kvnum"] = ", кв. " + NumberFlat;
            }
            dr["kv_pl"] = FullSquare.ToString("0.00");
            dr["type_pl"] = "общая";
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

        /// <summary>
        /// Форматирования значения для вывода объема по услуге на экран
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="aValue"></param>
        /// <param name="colName"></param>
        protected override void FillGoodServVolume(DataRow dr, decimal aValue, string colName)
        {
            if (Math.Abs(aValue) < 0.001m)
            {
                dr[colName] = string.Empty;
            }
            else
            {
                if (colName.IndexOf("rash_pu", StringComparison.Ordinal) > -1)
                    dr[colName] = aValue.ToString("0.00");
                else
                    dr[colName] = aValue.ToString("0.00##");
            }
        }

        /// <summary>
        /// Заполнение расходов по услугам
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <param name="index"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        private void FillServiceVolume(DataRow dr, int index, int nzpServ)
        {
            var anzpServ = nzpServ;
            var numRec = -1;
            for (var i = 0; i < ListVolume.Count; i++)
            {
                if (ListVolume[i].NzpServ == anzpServ)
                {
                    numRec = i;
                }
            }
            if (numRec == -1) return;

            var hasOdn = ListServ.Any(s => s.Serv.NzpServ == nzpServ && s.ServOdn.IsOdn);
            if (hasOdn)
            {
                if ((anzpServ == 25) && !string.IsNullOrEmpty(KfodnEl))
                {
                    dr["rash_norm_odn" + index] = KfodnEl;
                }

                if ((anzpServ == 6) && !string.IsNullOrEmpty(Kfodnhvs))
                {
                    dr["rash_norm_odn" + index] = Kfodnhvs;
                }

                if ((anzpServ == 9) && !string.IsNullOrEmpty(Kfodngvs))
                {
                    dr["rash_norm_odn" + index] = Kfodngvs;
                }
            }

            if (anzpServ == 25 ||
                anzpServ == 6 ||
                anzpServ == 9)
            {
                FillGoodServVolume(dr, ListVolume[numRec].DomVolume + ListVolume[numRec].OdnDomVolume, "rash_dpu" + index);
                FillGoodServVolume(dr, ListVolume[numRec].DomVolume, "rash_dpu_pu" + index);
                if (hasOdn)
                {
                    FillGoodServVolume(dr, ListVolume[numRec].OdnDomVolume, "rash_dpu_odn" + index);
                }
            }

            var normalVolume = ListVolume[numRec].NormaVolume;
            if (anzpServ == 324) // очистка стоков
            {
                var kanServiceVolume = ListVolume.FirstOrDefault(lv => lv.NzpServ == 7 && lv.NormaVolume > 0);
                if (kanServiceVolume != null)
                {
                    normalVolume = kanServiceVolume.NormaVolume;
                }
            }

            FillGoodServVolume(dr, normalVolume, "rash_norm" + index);


            FillCounterValueForService(dr, ListCounters, anzpServ, "rash_pu" + index);
            FillCounterValueForService(dr, ListDomCounters, anzpServ, "rash_pu_odn" + index);
        }

        private void FillCounterValueForService(DataRow dr, IEnumerable<Counters> counters, int serviceId, string parameterName)
        {
            FillGoodServVolume(dr, counters.Where(c => c.NzpServ == serviceId).Sum(c => c.Value), parameterName);
        }

        /// <summary>
        /// Отображение особых услуг в заголовке таблицы
        /// </summary>
        /// <param name="nzpServ"></param>
        /// <param name="dr"></param>
        /// <param name="index"></param>
        /// <param name="defaultName"></param>
        private void ShowSpecServ(int nzpServ, DataRow dr, int index, string defaultName = null)
        {
            //dr["name_serv" + index] = defaultName;
            foreach (BaseServ t in ListServ)
            {
                if (t.Serv.NzpServ == nzpServ)
                {
                    dr["name_serv" + index] = string.IsNullOrEmpty(defaultName) ? t.Serv.NameServ : defaultName;
                    dr["rsum_tarif_all" + index] = t.Serv.RsumTarif.ToString("0.00");

                    if (Math.Abs(t.Serv.Tarif) > 0.001m)
                    {
                        dr["tarif" + index] = t.Serv.Tarif.ToString("0.00");
                    }

                    dr["sum_dolg" + index] = (t.Serv.SumInsaldo - t.Serv.SumMoney).ToString("0.00");
                    dr["reval" + index] = (t.Serv.Reval + t.Serv.RealCharge).ToString("0.00");
                    dr["sum_charge_all" + index] = t.Serv.SumCharge.ToString("0.00");
                    dr["measure" + index] = t.Serv.Measure.Trim();
                }
            }
        }

        /// <summary>
        /// Заполнение таблицы по поставщикам
        /// </summary>
        /// <param name="dr"></param>
        private void FillSupplierGrid(DataRow dr)
        {
            var servicesBySupplier = new List<BaseServ>();

            foreach (var service in ListSupp)
            {
                var oldService = servicesBySupplier.FirstOrDefault(s => s.Serv.NzpSupp == service.Serv.NzpSupp);
                if (oldService != null)
                {
                    oldService.AddSum(service.Serv);
                    oldService.Serv.NameServ += ", " + service.Serv.NameServ.Trim();
                }
                else
                {
                    var newServ = new BaseServ(false);
                    newServ.AddSum(service.Serv);
                    newServ.Serv.NameSupp = service.Serv.NameSupp;
                    newServ.Serv.SuppRekv = service.Serv.SuppRekv;
                    newServ.Serv.NameServ = service.Serv.NameServ.Trim();
                    servicesBySupplier.Add(newServ);
                }
            }

            var index = 1;
            foreach (var t in servicesBySupplier)
            {
                if (IsShowServInGrid(t))
                {
                    if (index < 10)
                    {
                        dr["supp_serv" + index] = t.Serv.NameServ.TrimEnd(',');
                        dr["supp_name" + index] = t.Serv.NameSupp;
                        dr["supp_inn" + index] = t.Serv.SuppRekv;
                        dr["supp_summ" + index] = t.Serv.SumOutsaldo.ToString("0.00");
                    }

                    index++;
                }
            }
        }

        private void AddServiceToList(BaseServ service, List<BaseServ> services)
        {
            var mainServ = CUnionServ.GetMainServBySlave(service.Serv.NzpServ);
            //Определяем к какой услуге добавить
            if (mainServ == null) //услуга не объединяемая
            {
                var findServ = false;
                foreach (var t in services)
                {
                    if (t.Serv.NzpServ == service.Serv.NzpServ) //если такая услуга уже есть то добавляем к ней
                    {
                        t.AddSum(service.Serv);
                        if (string.IsNullOrEmpty(t.Serv.NameSupp))
                        {
                            t.Serv.NameSupp = service.Serv.NameSupp;
                            t.Serv.SuppRekv = service.Serv.SuppRekv;
                        }
                        else
                        {
                            if (service.Serv.Tarif > 0)
                            {
                                t.Serv.NameSupp = service.Serv.NameSupp;
                                t.Serv.SuppRekv = service.Serv.SuppRekv;
                                if (service.Serv.IsOdn == false)
                                    t.Serv.NameServ = service.Serv.NameServ;
                            }
                        }
                        findServ = true;
                    }
                }
                if (!findServ) services.Add(service); //иначе добавляем новую
            }
            else //если услуга объединяемая
            {
                var findServ = false;
                foreach (var t in services)
                {
                    if (t.Serv.NzpServ == mainServ.Serv.NzpServ) //если мастер услуга уже присутствует, то добавляем к ней
                    {
                        if (service.Serv.IsOdn)
                            service.Serv.CanAddTarif = false;
                        else
                            service.Serv.CanAddVolume = false;

                        t.AddSum(service.Serv);
                        findServ = true;
                    }
                }

                if (!findServ) //Не найдена объединенная услуга, добавляем её
                {
                    var newMainServ = (BaseServ)mainServ.Clone();
                    newMainServ.KommServ = service.KommServ;

                    if (service.Serv.IsOdn) service.Serv.CanAddTarif = false;

                    if (service.Serv.Tarif > 0)
                    {
                        newMainServ.Serv.NameSupp = service.Serv.NameSupp;
                        newMainServ.Serv.SuppRekv = service.Serv.SuppRekv;
                        if (service.Serv.IsOdn == false)
                        {
                            newMainServ.Serv.NameServ = service.Serv.NameServ;
                            newMainServ.Serv.IsDevice = service.Serv.IsDevice;
                        }
                    }

                    newMainServ.Serv.Measure = service.Serv.Measure;
                    newMainServ.AddSum(service.Serv);
                    services.Add(newMainServ);
                }
            }
        }

        public override void AddSupp(BaseServ aServ)
        {
            //Проверка на пустоту добавляемой услуги
            if (aServ.Empty()) return;

            AddServiceToList(aServ, ListSupp);
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

            AddServiceToList(aServ, ListServ);
        }

        public override void AddReasonReval(int nzpServ, string reason, string period)
        {
            var serviceId = nzpServ;
            var baseServ = CUnionServ.GetMainServBySlave(nzpServ);
            if (baseServ != null)
            {
                serviceId = baseServ.Serv.NzpServ;
            }

            var revalIndex = ListReval.FindIndex(r => r.NzpServ == serviceId);
            if (revalIndex != -1)
            {
                var aReval = ListReval[revalIndex];
                if (!string.IsNullOrEmpty(aReval.Reason))
                {
                    if (!string.IsNullOrEmpty(reason) && !aReval.Reason.Contains(reason))
                    {
                        aReval.Reason += ", " + reason;
                    }
                    if (!string.IsNullOrEmpty(period) && !aReval.ReasonPeriod.Contains(period))
                    {
                        aReval.ReasonPeriod += ", " + period;
                    }
                }
                else
                {
                    aReval.Reason = reason;
                    aReval.ReasonPeriod = period;
                }
                ListReval[revalIndex] = aReval;
            }
        }

        public override void AddReval(SumServ aServ)
        {
            ServReval aReval;

            var serviceId = aServ.NzpServ;
            var serviceName = aServ.NameServ;
            var baseServ = CUnionServ.GetMainServBySlave(aServ.NzpServ);
            if (baseServ != null)
            {
                serviceId = baseServ.Serv.NzpServ;
                serviceName = baseServ.Serv.NameServ;
            }

            // здесь структура, поэтому null не дождёшься
            var revalIndex = ListReval.FindIndex(r => r.NzpServ == serviceId);
            if (revalIndex == -1)
            {
                aReval = new ServReval
                    {
                        NzpServ = serviceId,
                        ServiceName = serviceName,
                    };
            }
            else
            {
                aReval = ListReval[revalIndex];
            }

            aReval.SumReval += aServ.Reval + aServ.RealCharge;
            aReval.CReval += aServ.CReval;
            aReval.SumGilReval += aServ.RevalGil;

            if (revalIndex == -1)
            {
                ListReval.Add(aReval);
            }
            else
            {
                ListReval[revalIndex] = aReval;
            }
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
            if (GvsNormGkal == 0) GvsNormGkal = 0.0611m;
            SetServRashod();
            ListServ.Sort();

            // Содержание жилья
            ShowSpecServ(2, dr, 1, "Содержание жилья");

            //Вывоз ТБО
            ShowSpecServ(16, dr, 2);

            //Капитальный ремонт
            ShowSpecServ(206, dr, 3);

            //Найм
            ShowSpecServ(15, dr, 4);

            int numberString = 5;

            foreach (BaseServ t in ListServ)
            {
                if ((IsShowServInGrid(t)) & (
                    (t.Serv.NzpServ != 2) &
                    (t.Serv.NzpServ != 206) &
                    (t.Serv.NzpServ != 16) &
                    (t.Serv.NzpServ != 15)))
                {
                    dr["name_serv" + numberString] = t.Serv.NameServ.Trim();

                    dr["measure" + numberString] = t.Serv.Measure.Trim();



                    if ((Math.Abs(t.Serv.CCalc) > 0.001m) && (t.Serv.IsOdn == false))
                    {
                        if (!((t.Serv.RsumTarif ==
                               t.ServOdn.RsumTarif) && (t.Serv.RsumTarif > 0.001m)))
                        {
                            if (Math.Abs(t.Serv.RsumTarif) > 0.001m)
                                dr["c_calc" + numberString] = t.Serv.CCalc.ToString("0.00##");
                        }
                    }


                    if (Math.Abs(t.ServOdn.CCalc) > 0.001m)
                    {
                        if (t.Serv.NzpServ == 7)
                            dr["c_calc_odn" + numberString] = "x";
                        else
                            dr["c_calc_odn" + numberString] = t.ServOdn.CCalc.ToString("0.00##");
                    }





                    if (Math.Abs(t.Serv.Tarif) > 0.001m)
                    {
                        dr["tarif" + numberString] = t.Serv.Tarif.ToString("0.00");
                    }

                    if (((t.Serv.NzpServ == 6) ||
                         (t.Serv.NzpServ == 7)) & (t.Serv.NzpMeasure != 3))
                    {
                        if (t.Serv.Norma > 0.001m)
                        {
                            dr["tarif" + numberString] = (t.Serv.Tarif /
                                                                     t.Serv.Norma).ToString("0.00");

                        }
                        dr["measure" + numberString] = "Куб.м.";
                    }

                    dr["rsum_tarif" + numberString] = (t.Serv.RsumTarif - t.ServOdn.RsumTarif).ToString("0.00");
                    dr["rsum_tarif_odn" + numberString] = t.ServOdn.RsumTarif.ToString("0.00");
                    dr["rsum_tarif_all" + numberString] = t.Serv.RsumTarif.ToString("0.00");

                    decimal revalX = t.Serv.Reval + t.Serv.RealCharge;
                    dr["reval" + numberString] = revalX.ToString("0.00");

                    decimal revalOdn = t.ServOdn.Reval + t.ServOdn.RealCharge;
                    dr["reval_odn" + numberString] = revalOdn.ToString("0.00");

                    dr["sum_lgota" + numberString] = "";

                    dr["sum_charge_all" + numberString] = t.Serv.SumCharge.ToString("0.00");


                    dr["sum_dolg" + numberString] =
                            (t.Serv.SumInsaldo -
                             t.Serv.SumMoney).ToString("0.00");

                    dr["sum_charge" + numberString] = (t.Serv.SumCharge - t.ServOdn.SumCharge).ToString("0.00");

                    dr["sum_charge_odn" + numberString] = t.ServOdn.SumCharge.ToString("0.00");

                    FillServiceVolume(dr, numberString, t.Serv.NzpServ);

                    numberString++;
                }
            }
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
                dr["reval_odn" + i] = "";
                dr["sum_lgota" + i] = "";
                dr["sum_charge_all" + i] = "";
                dr["sum_charge" + i] = "";
                dr["sum_charge_odn" + i] = "";
                dr["sum_nedop" + i] = "";
                dr["sum_sn" + i] = "";
                dr["sum_outsaldo" + i] = "";
                dr["real_charge" + i] = "";

            }
            dr["ls_msg"] = MessageForLs;
            dr["contract_content"] = ContractContentLs;
            dr["contract_footer"] = ContractFooterLs;
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
            table.Columns.Add("vars", typeof(byte[]));
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
            table.Columns.Add("area_adr", typeof(string));
            table.Columns.Add("area_phone", typeof(string));
            table.Columns.Add("vib_kolgil", typeof(string));
            table.Columns.Add("countGil", typeof(string));
            table.Columns.Add("countDepartureGil", typeof(string));
            table.Columns.Add("countArriveGil", typeof(string));
            table.Columns.Add("Avans", typeof(string));
            table.Columns.Add("Dolgnik", typeof(string));
            table.Columns.Add("sum_oplat_lgot", typeof(string));
            table.Columns.Add("sum_oplat_subs", typeof(string));

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
                table.Columns.Add("reval_odn" + i, typeof(string));
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


            for (int i = 1; i < 10; i++)
            {
                table.Columns.Add("supp_serv" + i, typeof(string));
                table.Columns.Add("supp_name" + i, typeof(string));
                table.Columns.Add("supp_inn" + i, typeof(string));
                table.Columns.Add("supp_summ" + i, typeof(string));

            }

            for (int i = 1; i < 9; i++)
            {
                table.Columns.Add("serv_pere" + i, typeof(string));
                table.Columns.Add("period_pere" + i, typeof(string));
                table.Columns.Add("osn_pere" + i, typeof(string));
                table.Columns.Add("sum_pere" + i, typeof(string));
            }

            //todo 8
            for (int i = 1; i <= 16; i++)
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
            table.Columns.Add("ls_msg", typeof(string));
            table.Columns.Add("contract_content", typeof(string));
            table.Columns.Add("contract_footer", typeof(string));

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
//            if (SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney > 0)
            if (SummaryServ.Serv.SumInsaldo > 0)
            {
                dr["sum_dolg"] = (SummaryServ.Serv.SumInsaldo).ToString("0.00");
                dr["sum_avans"] = "0.00";
            }
            else
            {
                dr["sum_dolg"] = "0.00";
                dr["sum_avans"] = (SummaryServ.Serv.SumInsaldo).ToString("0.00");
            }
            dr["sum_money"] = SummaryServ.Serv.SumMoney.ToString("0.00");
            dr["sum_peni"] = "0.00";
            dr["sum_ticket"] = SumTicket.ToString("0.00");
            dr["sum_rub"] = Decimal.Truncate(SumTicket).ToString("0");
            dr["sum_kop"] = ((SumTicket % 1) * 100).ToString("0");

            if (SummaryServ.Serv.SumOutsaldo < 0)
            {
                dr["sum_next_avans"] = SummaryServ.Serv.SumOutsaldo;
            }
            else
            {
                dr["sum_next_avans"] = "0.00";
            }

            if (((SummaryServ.Serv.SumInsaldo - SummaryServ.Serv.SumMoney) > SummaryServ.Serv.RsumTarif * 3)
                && (SummaryServ.Serv.RsumTarif > 0))
            {
                dr["Dolgnik"] = "1";
            }
            else
            {
                dr["Dolgnik"] = "0";
            }
            var conDb = DBManager.GetConnection(Constants.cons_Kernel);
            var conRet = DBManager.OpenDb(conDb, false);
            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendFormat(@"
select (select sum(s.sum_prih)
from {0}_charge_{1}.fn_supplier{2} s, {3}_fin_{1}.pack_ls pl, {3}_fin_{1}.pack p
where s.num_ls = {4}
and s.nzp_pack_ls = pl.nzp_pack_ls
and p.nzp_pack = pl.nzp_pack and p.nzp_bank = 80025) as lgot,
(select sum(s.sum_prih)
from {0}_charge_{1}.fn_supplier{2} s, {3}_fin_{1}.pack_ls pl, {3}_fin_{1}.pack p
where s.num_ls = {4}
and s.nzp_pack_ls = pl.nzp_pack_ls
and p.nzp_pack = pl.nzp_pack and p.nzp_bank = 80026) as subs
            ", Pref, Year.ToString().Substring(2), Month.ToString().PadLeft(2, '0'), Points.Pref, LicSchet);

            MyDataReader goodreader;
            var ret = DBManager.ExecRead(conDb, out goodreader, sqlBuilder.ToString(), true);
            if (ret.result)
            {
                if (goodreader.Read())
                {
                    if (goodreader["lgot"] != DBNull.Value)
                        dr["sum_oplat_lgot"] = goodreader["lgot"].ToString().Trim();
                    if (goodreader["subs"] != DBNull.Value)
                        dr["sum_oplat_subs"] = goodreader["subs"].ToString().Trim();
                }
            }
            goodreader.Close();
            conDb.Close();

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
                dr["dat_opl"] = DateOplat;
                dr["sum_last_opl"] = LastSumOplat;
            }
            return true;
        }


        /// <summary>
        /// Заполнение причин перерасчета
        /// </summary>
        /// <param name="dr">Строка в результирующей таблице</param>
        /// <returns></returns>
        protected override bool FillRevalReason(DataRow dr)
        {
            if (dr == null) return false;

            for (var i = 0; i < 6; i++)
            {
                if (ListReval.Count > i)
                {
                    dr["serv_pere" + (i + 1)] = ListReval[i].ServiceName;

                    if (string.IsNullOrEmpty(ListReval[i].Reason))
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
                                 (ListReval[i].NzpServ == 518)) && !string.IsNullOrEmpty(GilPeriods))
                            {

                                dr["period_pere" + (i + 1)] = GilPeriods;
                                dr["osn_pere" + (i + 1)] = "Врем. выбытие жильца";
                            }
                        }
                        else
                        {
                            dr["osn_pere" + (i + 1)] = "Изм. расхода по услуге";
                        }
                    }
                    else
                    {
                        dr["period_pere" + (i + 1)] = ListReval[i].ReasonPeriod.TrimStart(',').Trim();
                        dr["osn_pere" + (i + 1)] = ListReval[i].Reason;
                    }

                    dr["sum_pere" + (i + 1)] = ListReval[i].SumReval;
                }
                else
                {
                    dr["serv_pere" + (i + 1)] = string.Empty;
                    dr["osn_pere" + (i + 1)] = string.Empty;
                    dr["sum_pere" + (i + 1)] = string.Empty;
                    dr["period_pere" + (i + 1)] = string.Empty;
                }
            }

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
            for (int i = 0; i < Math.Min(8, ListCounters.Count); i++)
            {
                switch (ListCounters[i].NzpServ)
                {
                    case 6: dr["lsserv" + countersIndex] = countersIndex + ". х/в " + ListCounters[i].Place.Trim(); break;
                    case 9: dr["lsserv" + countersIndex] = countersIndex + ". г/в " + ListCounters[i].Place.Trim(); break;
                    case 8: dr["lsserv" + countersIndex] = countersIndex + ". Отопл. " + ListCounters[i].Place.Trim(); break;
                    case 25: dr["lsserv" + countersIndex] = countersIndex + ". Эл.снаб. " + ListCounters[i].Place.Trim(); break;
                    case 210: dr["lsserv" + countersIndex] = countersIndex + ". Ноч.Эл. " + ListCounters[i].Place.Trim(); break;
                    default: dr["lsserv" + countersIndex] = countersIndex + ". " + ListCounters[i].ServiceName; break;
                }
                dr["lsnumcnt" + countersIndex] = ListCounters[i].NumCounters;
                dr["lsdatuchet2_" + countersIndex] = ListCounters[i].DatUchet.ToShortDateString();
                dr["lsvalcnt2_" + countersIndex] = ListCounters[i].Value.ToString("0.00");
                dr["lsvalcnt1_" + countersIndex] = ListCounters[i].ValuePred.ToString("0.00");
                countersIndex++;
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
            for (int i = 0; i < Math.Min(8, ListDomCounters.Count); i++)
            {
                switch (ListDomCounters[i].NzpServ)
                {
                    case 6: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Хол. вода"; break;
                    case 9: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Гор. вода"; break;
                    case 8: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Отопл."; break;
                    case 25: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Эл.снаб."; break;
                    case 210: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". Ноч.Эл.."; break;
                    default: dr["domserv" + countersIndex] = "ОПУ " + countersIndex + ". " + ListDomCounters[i].ServiceName; break;
                }

                //dr["domserv" + countersIndex] = listDomCounters[i].serviceName;
                dr["domnumcnt" + countersIndex] = ListDomCounters[i].NumCounters;
                dr["domdatuchet1_" + countersIndex] = ListDomCounters[i].DatUchetPred.ToShortDateString();
                dr["domdatuchet2_" + countersIndex] = ListDomCounters[i].DatUchet.ToShortDateString();
                dr["domvalcnt1_" + countersIndex] = ListDomCounters[i].ValuePred.ToString("0.00##");
                dr["domvalcnt2_" + countersIndex] = ListDomCounters[i].Value.ToString("0.00##");
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

        public override void OrderPrint(int nzpKvar)
        {
            LsFaktura listLsServ = new LsFaktura(nzpKvar);
            int order_print = 1;
            for (int countServ = 0; countServ < ListServ.Count; countServ++)
            {
                if (IsShowServInGrid(ListServ[countServ]))
                {
                    listLsServ.AddServ(ListServ[countServ].Serv.NzpServ, order_print);
                    order_print++;
                }
            }
            if (listLsServ.ListServ.Count > 0)
                DbfakturaOrdering.AddFaktura(listLsServ);
        }

        public BaikalskFaktura()
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
            FakturaBlocks.HasCountersDoubleBlock = true;
            FakturaBlocks.HasCountersDoubleDomBlock = true;
            FakturaBlocks.HasOdnBlock = true;
            FakturaBlocks.HasRdnBlock = true;
            FakturaBlocks.HasPerekidkiSamaraBlock = true;
            FakturaBlocks.HasRemarkblock = true;
            FakturaBlocks.HasGilPeriodsBlock = true;
            FakturaBlocks.HasCountersSpisBlock = false;
            FakturaBlocks.HasDatOplBlock = true;
            FakturaBlocks.HasCalcGil = true;
            FakturaBlocks.HasGilPeriodsBlock = true;
            FakturaBlocks.HasPrintOrdering = true;
            FakturaBlocks.HasNewCountersBlock = true;

            Clear();
        }

    }


}

