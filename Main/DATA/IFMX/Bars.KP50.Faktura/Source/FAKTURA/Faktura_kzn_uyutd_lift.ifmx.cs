

using System;
using System.Data;
using System.Globalization;
using Bars.KP50.Faktura.Source.Base;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Faktura.Source.FAKTURA
{
    public class KznUyutdLiftNewFaktura : BaseFactura2
    {
        public override string Code { get { return "1101"; } }

        public override string Name
        {
            get { return "Казань Лифтерки  "; }
        }

        public override string FileName { get { return "kzn_uyutd_lift.frx"; } }

        public override DataTable MakeTable()
        {
            var table = new DataTable { TableName = "Q_master" };

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
            table.Columns.Add("dom_remark", typeof(string));
            table.Columns.Add("datedestv", typeof(string));
            table.Columns.Add("sum_imu", typeof(string));
            table.Columns.Add("sum_komm", typeof(string));
            table.Columns.Add("numkvit", typeof(string));
            table.Columns.Add("sum_propislift", typeof(string));
            table.Columns.Add("num_arend", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("dat_arend", typeof(string));
            table.Columns.Add("inn", typeof(string));
            table.Columns.Add("poluch", typeof(string));
            table.Columns.Add("bank", typeof(string));
            table.Columns.Add("bik", typeof(string));
            table.Columns.Add("ks", typeof(string));
            table.Columns.Add("rs", typeof(string));
            table.Columns.Add("gen_dir", typeof(string));
            return table;

        }


        protected override bool FillRekvizit()
        {

            if (Dr == null) return false;
            _Rekvizit rekvizit = Rekvizits.GetRekvizit(NzpArea, NzpGeu, Pref);
       
            Dr["poluch"] = rekvizit.poluch;
            Dr["inn"] = rekvizit.inn;
            Dr["bik"] = rekvizit.bik;
            Dr["bank"] = rekvizit.bank;
            Dr["ks"] = rekvizit.korr_schet;
            Dr["rs"] = rekvizit.rschet;
            Dr["remark"] = Area.AreaRemark;
            Dr["dom_remark"] = Dom.DomRemark;          
            Arendator.LoadArendPrm(Pref, NzpArea, NzpKvar);
            Dr["num_arend"] = Arendator.ArendNumAct;
            Dr["dat_arend"] = Arendator.ArendDatDog;
            Dr["gen_dir"] = Area.AreaDirectorFio;
            Dr["months"] = FullMonthName;

            //Dr["poluch2_rs"] = rekvizit.rschet2;
            //Dr["poluch2_bank"] = rekvizit.bank2;
            //Dr["poluch2_inn"] = rekvizit.inn;
            //Dr["poluch2_ks"] = rekvizit.korr_schet2;
            //Dr["poluch2_phone"] = rekvizit.phone2;



            //Dr["Data_dolg"] = "01." + Month.ToString("00") + "." + Year;
            //Dr["month_"] = Month;
            //Dr["year_"] = Year;
           

            //if (NumberFlat.IndexOf("комн.", StringComparison.Ordinal) >= 0)
            //{
            //    Dr["kvnum"] = NumberFlat.Replace("комн.", "(") + ")";
            //}

            return true;

        }

        public string IntToWord64(Int64 k)
        {
            //для Inttoworda64
            const long max3 = 5; //Максимальное количество триад
            string[] a11 = { "одна ", "две " };
            string[] a1 = {"", "один ", "два ","три ", "четыре ", "пять ", "шесть ", "семь ", "восемь ", 
                      "девять ", "десять ", "одиннадцать ", "двенадцать ", "тринадцать ", 
                      "четырнадцать ", "пятнадцать ", "шестнадцать ", "семнадцать ", 
                      "восемнадцать ", "девятнадцать "};
            string[] a10 = { "", "десять ", "двадцать ", "тридцать ", "сорок ", "пятьдесят ", "шестьдесят ", "семьдесят ", "восемьдесят ", "девяносто " };
            string[] a100 = { "", "сто ", "двести ", "триста ", "четыреста ", "пятьсот ", "шестьсот ", "семьсот ", "восемьсот ", "девятьсот " };
            //четвертым параметром является обозначение мужского или женского рода,
            //где "0" - женский род;
            //"1" - мужской род.
            //Это дает возможность, например, изменяя "рубли" на "штуки" просто поменять "1" на "0" не меняя кода.}
            var a0 = new[,]{{"рубль", "рубля", "рублей", "1"},
    {"тысяча", "тысячи", "тысяч", "0"},
    {"миллион", "миллиона", "миллионов", "1"},
            {"миллиард", "миллиарда", "миллиардов", "1"},
                {"триллион", "триллиона", "триллионов", "1"}};


            string res;

            try
            {
                res = "";
                Int64 value = k;
                Int64 n = 0;
                //Обработка отрицательного значения и нуля
                if (value < 0)
                {
                    value = -value;
                    res = "минус";
                }
                else if (value == 0) res = "ноль" + res;


                do
                {
                    //Разбивка на триады с конца
                    Int64 v = value % 1000;
                    //value = (int)Decimal.Truncate(value / 1000);
                    value = value / 1000;
                    //Обработка
                    if ((v > 0) || (n == 0))
                    {
                        //Int64 i100 = (int)Decimal.Truncate(v / 100);
                        Int64 i100 = v / 100;
                        v = v - (i100 * 100);
                        Int64 i10;
                        Int64 i1;
                        if (v >= 20)
                        {
                            i1 = v % 10;
                            //v = (int)Decimal.Truncate(v / 10);
                            v = v / 10;
                            i10 = v % 10;
                        }
                        else
                        {
                            i1 = v;
                            i10 = 0;
                        }

                        Int64 p;
                        switch (i1)
                        {
                            case 1: p = 0; break;
                            case 2: p = 1; break;
                            case 3: p = 1; break;
                            case 4: p = 1; break;
                            default: p = 2; break;
                        }

                        //Изменение в зависимости от женского или мужского рода
                        if (k >= 1)
                        {
                            if ((a0[n, 3] == "0") & ((i1 == 1) || (i1 == 2)))
                                res = a100[i100] + a10[i10] + a11[i1 - 1] + a0[n, p] + " " + res;
                            else res = a100[i100] + a10[i10] + a1[i1] + a0[n, p] + " " + res;
                        }
                        else
                        {
                            if ((a0[n, 3] == "0") & ((i1 == 1) || (i1 == 2)))
                                res = res + ' ' + a100[i100] + a10[i10] + a11[i1 - 1] + a0[n, p];
                            else res = res + ' ' + a100[i100] + a10[i10] + a1[i1] + a0[n, p];
                        }



                    }
                    n++;
                    //Проверка выхода за максимальное количество триад
                    if (n > max3) return "";
                } while ((value > 0));
                //until Value <= 0;
            }
            catch
            {
                res = "Ошибка";
            }
            return res;


        }

        public string FormatKop(int n)
        {
            string[] kopeek =
                {"копеек","копейка","копейки","копейки","копейки","копеек",
       "копеек","копеек","копеек","копеек"};


            string num = n.ToString("00");
            if ((num.Length >= 2) & (num[num.Length - 2].ToString(CultureInfo.InvariantCulture) == "1"))
                num = num + " " + "копеек";
            else
                num = num + " " + kopeek[Int32.Parse(num[num.Length - 1].ToString(CultureInfo.InvariantCulture))];
            return num;
        }



        /// <summary>
        /// Заполнение квартирных параметров
        /// </summary>
        /// <returns></returns>
        protected override bool FillKvarPrm()
        {
            if (Dr == null) return false;

            Dr["kv_pl"] = Kvar.FullSquare.ToString("0.00");

            return true;
        }

        /// <summary>
        /// Заполнение данных таблицы начислений
        /// </summary>
        /// <returns></returns>
        protected override bool FillMainChargeGrid()
        {
            if (Dr == null) return false;

            decimal sumImu = 0;
            decimal sumKomm = 0;

            foreach (var serv in Charge.ListServ)
            {
                if (serv.Value.KommServ)
                {
                    sumKomm += serv.Value.Serv.SumCharge;
                }
                else
                {
                    sumImu += serv.Value.Serv.SumCharge;
                }

            }


            Dr["sum_imu"] = sumImu.ToString("0.00");
            Dr["sum_komm"] = sumKomm.ToString("0.00");
            Dr["sum_charge"] = (sumKomm + sumImu).ToString("0.00");
            int kop = Convert.ToInt32(((sumKomm + sumImu) - (int)(sumKomm + sumImu))*100);
            Dr["sum_propislift"] = IntToWord64((Int64)Decimal.Truncate(sumKomm + sumImu)) + FormatKop(kop);




            return true;
        }



        /// <summary>
        /// Заполнение строки Адреса
        /// </summary>
        /// <returns></returns>
        protected override bool FillAdr()
        {
            if (Dr == null) return false;
            Dr["fio"] = Kvar.PayerFio;
            Dr["adress"] = Ulica + " д. " + NumberDom + " кв." + NumberFlat;
            if (Pkod.IndexOf(".", StringComparison.Ordinal) > -1)
                Dr["pkod"] = Pkod.Substring(0, Pkod.IndexOf(".", StringComparison.Ordinal));
            else
                Dr["pkod"] = "111";
            Dr["numdom"] = NumberDom;
            Dr["kvnum"] = NumberFlat;
            Dr["ulica"] = Ulica;
            return true;
        }



        /// <summary>
        /// Заполнение итоговой строки по счету
        /// </summary>
        /// <returns></returns>
        protected override bool FillSummuryBill()
        {
            if (Dr == null) return false;
            return true;
        }

        /// <summary>
        /// Формирование суммы к оплате в счете
        /// </summary>
        /// <param name="finder"></param>
        public override void FinalPass(STCLINE.KP50.Interfaces.Faktura finder)
        {
            SumTicket = Charge.SummaryServ.Serv.SumCharge > 0.001m
                ? Charge.SummaryServ.Serv.SumCharge
                : 0;
        }


    }

}

