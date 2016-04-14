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


namespace Bars.KP50.DB.Faktura
{


    public class KznUyutdArendFaktura : BaseFactura
    {
        decimal sum_peni = 0;

        public override DataTable MakeTable()
        {
            DataTable table = new DataTable();
            table.TableName = "Q_master";

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
            table.Columns.Add("sum_propis", typeof(string));
            table.Columns.Add("sum_propis_wpeni", typeof(string));
            table.Columns.Add("num_arend", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("dat_arend", typeof(string));

            for (int i = 1; i < 30; i++)
            {
                table.Columns.Add("name_serv", typeof(string));
                table.Columns.Add("sum_money", typeof(string));
                table.Columns.Add("reval", typeof(string));
                table.Columns.Add("sum_dolg", typeof(string));
                table.Columns.Add("measure", typeof(string));
                table.Columns.Add("tarif", typeof(string));
                table.Columns.Add("c_calc_all", typeof(string));
                table.Columns.Add("sum_tarif_all", typeof(string));
                table.Columns.Add("sum_charge_all", typeof(string));
                table.Columns.Add("sum_ito", typeof(string));
            }
            return table;

        }


        protected override bool FillRekvizit(DataRow dr)
        {
            return true;

        }

        public string IntToWord64(Int64 k)
        {
            //для Inttoworda64
            Int64 MAX3 = 5; //Максимальное количество триад
            string[] A11 = { "одна ", "две " };
            string[] A1 = {"", "один ", "два ","три ", "четыре ", "пять ", "шесть ", "семь ", "восемь ", 
                      "девять ", "десять ", "одиннадцать ", "двенадцать ", "тринадцать ", 
                      "четырнадцать ", "пятнадцать ", "шестнадцать ", "семнадцать ", 
                      "восемнадцать ", "девятнадцать "};
            string[] A10 = { "", "десять ", "двадцать ", "тридцать ", "сорок ", "пятьдесят ", "шестьдесят ", "семьдесят ", "восемьдесят ", "девяносто " };
            string[] A100 = { "", "сто ", "двести ", "триста ", "четыреста ", "пятьсот ", "шестьсот ", "семьсот ", "восемьсот ", "девятьсот " };
            //четвертым параметром является обозначение мужского или женского рода,
            //где "0" - женский род;
            //"1" - мужской род.
            //Это дает возможность, например, изменяя "рубли" на "штуки" просто поменять "1" на "0" не меняя кода.}
            string[,] A0 = new string[5, 4]{{"рубль", "рубля", "рублей", "1"},
    {"тысяча", "тысячи", "тысяч", "0"},
    {"миллион", "миллиона", "миллионов", "1"},
            {"миллиард", "миллиарда", "миллиардов", "1"},
                {"триллион", "триллиона", "триллионов", "1"}};




            Int64 Value = 0;
            Int64 i100;
            Int64 i10;
            Int64 i1;
            Int64 V;
            Int64 p;
            Int64 n;
            string res;

            try
            {
                res = "";
                Value = k;
                n = 0;
                //Обработка отрицательного значения и нуля
                if (Value < 0)
                {
                    Value = -Value;
                    res = "минус";
                }
                else if (Value == 0) res = "ноль" + res;


                do
                {
                    //Разбивка на триады с конца
                    V = Value % 1000;
                    Value = (int)Decimal.Truncate(Value / 1000);
                    //Обработка
                    if ((V > 0) || (n == 0))
                    {
                        i100 = (int)Decimal.Truncate(V / 100);
                        V = V - (i100 * 100);
                        if (V >= 20)
                        {
                            i1 = V % 10;
                            V = (int)Decimal.Truncate(V / 10);
                            i10 = V % 10;
                        }
                        else
                        {
                            i1 = V;
                            i10 = 0;
                        }

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
                            if ((A0[n, 3] == "0") & ((i1 == 1) || (i1 == 2)))
                                res = A100[i100] + A10[i10] + A11[i1-1] + A0[n, p] + " " + res;
                            else res = A100[i100] + A10[i10] + A1[i1] + A0[n, p] + " " + res;
                        }
                        else
                        {
                            if ((A0[n, 3] == "0") & ((i1 == 1) || (i1 == 2)))
                                res = res + ' ' + A100[i100] + A10[i10] + A11[i1-1] + A0[n, p];
                            else res = res + ' ' + A100[i100] + A10[i10] + A1[i1] + A0[n, p];
                        }



                    }
                    n++;
                    //Проверка выхода за максимальное количество триад
                    if (n > MAX3) return "";
                } while ((Value > 0));
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


            string num;

            num = n.ToString("00");
            if ((num.Length >= 2) & (num[num.Length - 2].ToString() == "1"))
                num = num + " " + "копеек";
            else
                num = num + " " + kopeek[Int32.Parse(num[num.Length-1].ToString())];
            return num;
        }

        public override void Clear()
        {

            FullSquare = 0;

            PayerFio = "";

            Month = System.DateTime.Now.Month;
            Year = System.DateTime.Now.Year;
            FullMonthName = "";
            Pkod = "";
            Geu = "";
            Ulica = "";
            NumberDom = "";
            NumberFlat = "";
            PrefixUk = "";
            CodeUk = "";

            NzpArea = 0;
            NzpGeu = 0;


            SummaryServ.Clear();


        }

        /// <summary>
        /// Заполнение квартирных параметров
        /// </summary>
        /// <param name="dr">Строка таблицы</param>
        /// <returns></returns>
        protected override bool FillKvarPrm(DataRow dr)
        {
            if (dr == null) return false;

            dr["kv_pl"] = FullSquare.ToString("0.00");

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
      
            BaseServ p;

            for (int countServ = 0; countServ < Math.Min(30,ListServ.Count); countServ++)
            {

                p = ListServ[countServ];

                if (p.Serv.NzpServ > 0)
                {
                    dr["name_serv" + countServ] = p.Serv.NameServ;
                    dr["sum_money" + countServ] = p.Serv.SumMoney;
                    dr["reval" + countServ] = p.Serv.Reval + p.Serv.RealCharge;
                    dr["sum_dolg" + countServ] = p.Serv.SumPere;
                    dr["measure"] = p.Serv.Measure;
                    dr["tarif" + countServ] = p.Serv.Tarif;
                    dr["c_calc_all" + countServ] = p.Serv.CCalc;
                    dr["sum_tarif_all" + countServ] = p.Serv.SumTarif;
                    dr["sum_charge_all" + countServ] = p.Serv.SumCharge;
                    dr["sum_ito" + countServ] = p.Serv.RsumTarif + p.Serv.RealCharge + p.Serv.Reval;
                    if (p.Serv.NzpServ == 500) sum_peni = p.Serv.RsumTarif + p.Serv.RealCharge + p.Serv.Reval;
                }
                else
                {

                    dr["name_serv" + countServ] = "x x x x x ";
                    dr["sum_money" + countServ] = "x x x x x ";
                    dr["reval" + countServ] = "x x x x x ";
                    dr["sum_dolg" + countServ] = "x x x x x ";
                    dr["measure"] = p.Serv.Measure;
                    dr["tarif" + countServ] = "x x x x x ";
                    dr["c_calc_all" + countServ] = "x x x x x ";
                    dr["sum_tarif_all" + countServ] = "x x x x x ";
                    dr["sum_charge_all" + countServ] = "x x x x x ";
                    dr["sum_ito" + countServ] = " х х х х ";
                }

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
                dr["pkod"] = "101";
            dr["numdom"] = NumberDom;
            dr["kvnum"] = NumberFlat;
            dr["ulica"] = Ulica;
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
            dr["sum_charge"] = SummaryServ.Serv.SumCharge.ToString("0.00");

            decimal sum_propis = SummaryServ.Serv.SumTarif + SummaryServ.Serv.Reval + SummaryServ.Serv.RealCharge;

            dr["sum_propis"] = IntToWord64((Int64)Decimal.Truncate(sum_propis)) +
                FormatKop((int)(sum_propis - (Int64)Decimal.Truncate(sum_propis)) * 100);

            sum_propis -= sum_peni;
            dr["sum_propis_wpeni"] = IntToWord64((Int64)Decimal.Truncate(sum_propis)) +
                FormatKop((int)(sum_propis - (Int64)Decimal.Truncate(sum_propis)) * 100);

            return true;
        }

        /// <summary>
        /// Формирование суммы к оплате в счете
        /// </summary>
        /// <param name="finder"></param>
        public override void FinalPass( STCLINE.KP50.Interfaces.Faktura finder)
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


        public KznUyutdArendFaktura() : 
            base()
        {
            Rekvizit = new _Rekvizit();

            SummaryServ = new BaseServ(false);
            FakturaBlocks.HasAdrBlock = true;
            FakturaBlocks.HasKvarPrmBlock = true;
            FakturaBlocks.HasSummuryBillBlock = true;
            FakturaBlocks.HasMainChargeGridBlock = true;
            FakturaBlocks.HasArendBlock = true;
            Clear();
    
        }
    }
   
}

