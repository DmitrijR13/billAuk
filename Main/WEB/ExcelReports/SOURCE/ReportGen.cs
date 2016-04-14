using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;



namespace STCLINE.KP50.REPORT
{
    //Класс генерирующий отчеты
    //содержит процедуры для генерации конкретных отчетов
    public partial class ReportGen
    {
        /// <summary>
        /// Составляет табличку из шаблона поиска для шапки отчета
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <returns></returns>
        public string SetReportConditional(int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetFindTemplate(out ret, nzp_user.ToString());
            string s_res = "";
            if (DT == null) return s_res;

            string s_head = "";
            string s_adres = "";
            string num_dom_s = "";
            string num_kvar_s = "";
            string s_sost = "";
            for (int i = 0; i < DT.Rows.Count; i++)
            {
                //if ((DT.Rows[i]["name"].ToString().Trim() == "Банк данных") ||
                if ((DT.Rows[i]["name"].ToString().Trim() == "Управляющая организация") ||
                  (DT.Rows[i]["name"].ToString().Trim() == "Отделение") ||
                  (DT.Rows[i]["name"].ToString().Trim() == "Участок"))
                    s_head = s_head + " " +
                        DT.Rows[i]["name"].ToString().Trim() + ": " + DT.Rows[i]["value"].ToString().Trim();

                if (DT.Rows[i]["name"].ToString().Trim() == "Улица")
                    s_adres = s_adres + "Улица : " + DT.Rows[i]["value"].ToString().Trim();


                if (DT.Rows[i]["name"].ToString().Trim() == "Номер дома с")
                {
                    s_adres = s_adres + " дом: " + DT.Rows[i]["value"].ToString().Trim();
                    num_dom_s = DT.Rows[i]["value"].ToString().Trim();
                }

                if (DT.Rows[i]["name"].ToString().Trim() == "Номер дома по")
                {
                    if (num_dom_s != "")
                        s_adres = s_adres + " - " + DT.Rows[i]["value"].ToString().Trim();
                    else
                        s_adres = s_adres + " дом: " + DT.Rows[i]["value"].ToString().Trim();
                }

                if (DT.Rows[i]["name"].ToString().Trim() == "Корпус")
                    s_adres = s_adres + " корп. : " + DT.Rows[i]["value"].ToString().Trim();

                if (DT.Rows[i]["name"].ToString().Trim() == "Подъезд")
                    s_adres = s_adres + " подъезд: " + DT.Rows[i]["value"].ToString().Trim();


                if (DT.Rows[i]["name"].ToString().Trim() == "Квартира с")
                {
                    s_adres = s_adres + " кв.: " + DT.Rows[i]["value"].ToString().Trim();
                    num_kvar_s = DT.Rows[i]["value"].ToString().Trim();
                }

                if (DT.Rows[i]["name"].ToString().Trim() == "Квартира по")
                {
                    if (num_kvar_s != "")
                        s_adres = s_adres + " - " + DT.Rows[i]["value"].ToString().Trim();
                    else
                        s_adres = s_adres + " кв.: " + DT.Rows[i]["value"].ToString().Trim();
                }
                if (DT.Rows[i]["name"].ToString().Trim() == "Комната")
                    s_adres = s_adres + " комн.: " + DT.Rows[i]["value"].ToString().Trim();

                if (DT.Rows[i]["name"].ToString().Trim() == "Комната")
                    s_adres = s_adres + " комн.: " + DT.Rows[i]["value"].ToString().Trim();

                if (DT.Rows[i]["name"].ToString().Trim() == "Лицевой счет")
                    s_adres = s_adres + " ЛС: " + DT.Rows[i]["value"].ToString().Trim();

                if (DT.Rows[i]["name"].ToString().Trim() == "Платежный код")
                    s_adres = s_adres + " ЛС: " + DT.Rows[i]["value"].ToString().Trim();

                if (DT.Rows[i]["name"].ToString().Trim() == "Квартиросъемщик")
                    s_adres = s_adres + " ФИО: " + DT.Rows[i]["value"].ToString().Trim();

                if (DT.Rows[i]["name"].ToString().Trim() == "Состояние")
                    s_sost = s_sost + " Состояние: " + DT.Rows[i]["value"].ToString().Trim();

                //if (DT.Rows[i]["name"].ToString().Trim() == "Тип счета")
                //    s_sost = s_sost + " Тип счета: " + DT.Rows[i]["value"].ToString().Trim();

            }
            s_res = s_head;
            if (s_adres != "") s_res = s_res + System.Convert.ToChar(10) + System.Convert.ToChar(13) + s_adres;
            if (s_sost != "") s_res = s_res + System.Convert.ToChar(10) + System.Convert.ToChar(13) + s_sost;
            return s_res;

        }




        public Returns GetSaldoRep10_14_1(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            ExcelLoader ExcelL = null;

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSaldoReport10_14_1(prm_, out ret, Nzp_user);

            try
            {

                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelL.ExlWs.Rows.Font.Name = "Arial";
                    //ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку

                    #region Получаем название месяца
                    string strMonthName = "";
                    string strMonthName1 = "";

                    switch (Convert.ToInt32(prm_.month_))
                    {
                        case 1:
                            {
                                strMonthName = "ЯНВАРЬ";
                                strMonthName1 = "ЯНВАРЕ";
                            }
                            break;
                        case 2:
                            {
                                strMonthName = "ФЕВРАЛЬ";
                                strMonthName1 = "ФЕВРАЛЕ";
                            }
                            break;
                        case 3:
                            {
                                strMonthName = "МАРТ";
                                strMonthName1 = "МАРТЕ";
                            }
                            break;
                        case 4:
                            {
                                strMonthName = "АПРЕЛЬ";
                                strMonthName1 = "АПРЕЛЕ";
                            }
                            break;
                        case 5:
                            {
                                strMonthName = "МАЙ";
                                strMonthName1 = "МАЕ";
                            }
                            break;
                        case 6:
                            {
                                strMonthName = "ИЮНЬ";
                                strMonthName1 = "ИЮНЕ";
                            }
                            break;
                        case 7:
                            {
                                strMonthName = "ИЮЛЬ";
                                strMonthName1 = "ИЮЛЕ";
                            }
                            break;
                        case 8:
                            {
                                strMonthName = "АВГУСТ";
                                strMonthName1 = "АВГУСТЕ";
                            }
                            break;
                        case 9:
                            {
                                strMonthName = "СЕНТЯБРЬ";
                                strMonthName = "СЕНТЯБРЕ";
                            }
                            break;
                        case 10:
                            {
                                strMonthName = "ОКТЯБРЬ";
                                strMonthName = "ОКТЯБРЕ";
                            }
                            break;
                        case 11:
                            {
                                strMonthName = "НОЯБРЬ";
                                strMonthName = "НОЯБРЕ";
                            }
                            break;
                        case 12:
                            {
                                strMonthName = "ДЕКАБРЬ";
                                strMonthName = "ДЕКАБРЕ";
                            }
                            break;
                        default:
                            break;
                    }
                    #endregion
                    Microsoft.Office.Interop.Excel.Range excells1;

                    //спустили шапку
                    excells1 = ExcelL.ExlWs.get_Range("A1", "M1");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "10.14.1 Сальдовая оборотная ведомость начислений и оплат по услугам и поставщикам ";
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A2", "M2");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;

                    excells1.FormulaR1C1 = " за " +
                        strMonthName + " " + prm_.year_.ToString() + "г.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("B3", "B3");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.ColumnWidth = 3;

                    excells1 = ExcelL.ExlWs.get_Range("C3", "M4");
                    excells1.Font.Bold = true;
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.RowHeight = 23;

                    excells1 = ExcelL.ExlWs.get_Range("A5", "A6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "№ п/п";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("B5", "B6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Поставщик /услуга";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 23;

                    excells1 = ExcelL.ExlWs.get_Range("C5", "C6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Входящее сальдо";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 9.3;


                    excells1 = ExcelL.ExlWs.get_Range("D5", "D6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Рассчитано по тарифу";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 9.3;


                    excells1 = ExcelL.ExlWs.get_Range("E5", "E6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Скидка по льготе";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 7;

                    excells1 = ExcelL.ExlWs.get_Range("F5", "F6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = " Сумма недопоставки в расчетном месяце";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.3;

                    excells1 = ExcelL.ExlWs.get_Range("G5", "G6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Рассчитано с учетом льгот и недопоставок";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.3;

                    excells1 = ExcelL.ExlWs.get_Range("H5", "H6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Изменения и перерасчет предыдущего периода без уч. недопоставок";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.4;

                    excells1 = ExcelL.ExlWs.get_Range("I5", "I6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сумма перерасч. недопоставки. предыдущего периода";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 8.6;

                    excells1 = ExcelL.ExlWs.get_Range("J5", "J6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Оплата произведенная в " + strMonthName1 + " " + prm_.year_.ToString() +
                        " предыдущий месяц ";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.3;

                    excells1 = ExcelL.ExlWs.get_Range("K5", "L5");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "в т.ч.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("K6", "K6");
                    excells1.FormulaR1C1 = "Оплата поставщиков";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 5.3;

                    excells1 = ExcelL.ExlWs.get_Range("L6", "L6");
                    excells1.FormulaR1C1 = "Перерасчет между поставщиками";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 6.6;

                    excells1 = ExcelL.ExlWs.get_Range("M5", "M6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Исходящее сальдо";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 9.3;
                    #endregion
                    int ExcelRow = 7;
                    if (DT.Rows.Count > 0)
                    {


                        #region Пишем тело


                        int i = 0;
                        int num_str = 1;
                        decimal sum_insaldo = 0;
                        decimal rsum_tarif = 0;
                        decimal rsum_lgota = 0;
                        decimal sum_nedop = 0;
                        decimal sum_real = 0;
                        decimal sum_last_nedop = 0;
                        decimal reval = 0;
                        decimal sum_money = 0;
                        decimal money_from = 0;
                        decimal money_del = 0;
                        decimal sum_outsaldo = 0;

                        decimal isum_insaldo = 0;
                        decimal irsum_tarif = 0;
                        decimal irsum_lgota = 0;
                        decimal isum_nedop = 0;
                        decimal isum_real = 0;
                        decimal isum_last_nedop = 0;
                        decimal ireval = 0;
                        decimal isum_money = 0;
                        decimal imoney_from = 0;
                        decimal imoney_del = 0;
                        decimal isum_outsaldo = 0;


                        decimal ssum_insaldo = 0;
                        decimal srsum_tarif = 0;
                        decimal srsum_lgota = 0;
                        decimal ssum_nedop = 0;
                        decimal ssum_real = 0;
                        decimal ssum_last_nedop = 0;
                        decimal sreval = 0;
                        decimal ssum_money = 0;
                        decimal smoney_from = 0;
                        decimal smoney_del = 0;
                        decimal ssum_outsaldo = 0;

                        string old_area = "";
                        string old_serv = "";
                        while (i < DT.Rows.Count)
                        {

                            if (old_area == "")
                            {
                                old_area = DT.Rows[i]["area"].ToString().Trim();
                                old_serv = DT.Rows[i]["service"].ToString().Trim();
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = old_area;
                                ExcelRow++;
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = old_serv;
                                ExcelRow++;

                            }

                            //Пишем Итого по Территории
                            #region Итого по территории
                            if (old_area != DT.Rows[i]["area"].ToString().Trim())
                            {
                                ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого " + old_serv;
                                ExcelL.ExlWs.Cells[ExcelRow, 3] = ssum_insaldo;
                                ExcelL.ExlWs.Cells[ExcelRow, 4] = srsum_tarif;
                                ExcelL.ExlWs.Cells[ExcelRow, 5] = srsum_lgota;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = ssum_nedop;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = ssum_real;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = sreval;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = ssum_last_nedop;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = ssum_money;
                                ExcelL.ExlWs.Cells[ExcelRow, 11] = smoney_from;
                                ExcelL.ExlWs.Cells[ExcelRow, 12] = smoney_del;
                                ExcelL.ExlWs.Cells[ExcelRow, 13] = ssum_outsaldo;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                                "M" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;

                                ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого " + old_area;
                                ExcelL.ExlWs.Cells[ExcelRow, 3] = sum_insaldo;
                                ExcelL.ExlWs.Cells[ExcelRow, 4] = rsum_tarif;
                                ExcelL.ExlWs.Cells[ExcelRow, 5] = rsum_lgota;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = sum_nedop;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_real;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = reval;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = sum_last_nedop;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = sum_money;
                                ExcelL.ExlWs.Cells[ExcelRow, 11] = money_from;
                                ExcelL.ExlWs.Cells[ExcelRow, 12] = money_del;
                                ExcelL.ExlWs.Cells[ExcelRow, 13] = sum_outsaldo;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                                "M" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;
                                old_area = DT.Rows[i]["area"].ToString().Trim();
                                old_serv = DT.Rows[i]["service"].ToString().Trim();
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = old_area;
                                ExcelRow++;
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = old_serv;
                                ExcelRow++;
                                num_str = 1;

                                sum_insaldo = 0;
                                rsum_tarif = 0;
                                rsum_lgota = 0;
                                sum_nedop = 0;
                                sum_real = 0;
                                reval = 0;
                                sum_last_nedop = 0;
                                sum_money = 0;
                                money_from = 0;
                                money_del = 0;
                                sum_outsaldo = 0;

                                ssum_insaldo = 0;
                                srsum_tarif = 0;
                                srsum_lgota = 0;
                                ssum_nedop = 0;
                                ssum_real = 0;
                                sreval = 0;
                                ssum_last_nedop = 0;
                                ssum_money = 0;
                                smoney_from = 0;
                                smoney_del = 0;
                                ssum_outsaldo = 0;

                            }
                            #endregion


                            #region Итого по услуге
                            if (old_serv != DT.Rows[i]["service"].ToString().Trim())
                            {
                                ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого " + old_serv;
                                ExcelL.ExlWs.Cells[ExcelRow, 3] = ssum_insaldo;
                                ExcelL.ExlWs.Cells[ExcelRow, 4] = srsum_tarif;
                                ExcelL.ExlWs.Cells[ExcelRow, 5] = srsum_lgota;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = ssum_nedop;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = ssum_real;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = sreval;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = ssum_last_nedop;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = ssum_money;
                                ExcelL.ExlWs.Cells[ExcelRow, 11] = smoney_from;
                                ExcelL.ExlWs.Cells[ExcelRow, 12] = smoney_del;
                                ExcelL.ExlWs.Cells[ExcelRow, 13] = ssum_outsaldo;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                                "M" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;

                                old_serv = DT.Rows[i]["service"].ToString().Trim();
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = old_serv;
                                ExcelRow++;
                                num_str = 1;

                                ssum_insaldo = 0;
                                srsum_tarif = 0;
                                srsum_lgota = 0;
                                ssum_nedop = 0;
                                ssum_real = 0;
                                sreval = 0;
                                ssum_last_nedop = 0;
                                ssum_money = 0;
                                smoney_from = 0;
                                smoney_del = 0;
                                ssum_outsaldo = 0;

                            }
                            #endregion


                            sum_insaldo = sum_insaldo + Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                            rsum_tarif = rsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                            rsum_lgota = rsum_lgota + Decimal.Parse(DT.Rows[i]["rsum_lgota"].ToString());
                            sum_nedop = sum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                            sum_real = sum_real + Decimal.Parse(DT.Rows[i]["sum_real"].ToString());
                            reval = reval + Decimal.Parse(DT.Rows[i]["reval"].ToString());
                            sum_last_nedop = sum_last_nedop + Decimal.Parse(DT.Rows[i]["sum_last_nedop"].ToString());
                            sum_money = sum_money + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                            money_from = money_from + Decimal.Parse(DT.Rows[i]["money_from"].ToString());
                            money_del = money_del + Decimal.Parse(DT.Rows[i]["money_del"].ToString());
                            sum_outsaldo = sum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());

                            isum_insaldo = isum_insaldo + Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                            irsum_tarif = irsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                            irsum_lgota = irsum_lgota + Decimal.Parse(DT.Rows[i]["rsum_lgota"].ToString());
                            isum_nedop = isum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                            isum_real = isum_real + Decimal.Parse(DT.Rows[i]["sum_real"].ToString());
                            ireval = ireval + Decimal.Parse(DT.Rows[i]["reval"].ToString());
                            isum_last_nedop = isum_last_nedop + Decimal.Parse(DT.Rows[i]["sum_last_nedop"].ToString());
                            isum_money = isum_money + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                            imoney_from = imoney_from + Decimal.Parse(DT.Rows[i]["money_from"].ToString());
                            imoney_del = imoney_del + Decimal.Parse(DT.Rows[i]["money_del"].ToString());
                            isum_outsaldo = isum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());

                            ssum_insaldo = ssum_insaldo + Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                            srsum_tarif = srsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                            srsum_lgota = srsum_lgota + Decimal.Parse(DT.Rows[i]["rsum_lgota"].ToString());
                            ssum_nedop = ssum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                            ssum_real = ssum_real + Decimal.Parse(DT.Rows[i]["sum_real"].ToString());
                            sreval = sreval + Decimal.Parse(DT.Rows[i]["reval"].ToString());
                            ssum_last_nedop = ssum_last_nedop + Decimal.Parse(DT.Rows[i]["sum_last_nedop"].ToString());
                            ssum_money = ssum_money + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                            smoney_from = smoney_from + Decimal.Parse(DT.Rows[i]["money_from"].ToString());
                            smoney_del = smoney_del + Decimal.Parse(DT.Rows[i]["money_del"].ToString());
                            ssum_outsaldo = ssum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());



                            //Номер п/п
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = num_str.ToString();
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;


                            //Услуга/Территория
                            excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["name_supp"].ToString().Trim();

                            //Входящее сальдо
                            excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());

                            //Рассчитано по тарифу
                            excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());

                            //Скидка по льготе
                            excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow.ToString(), "E" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["rsum_lgota"].ToString());

                            //Сумма недопоставки в расчетном месяце
                            excells1 = ExcelL.ExlWs.get_Range("F" + ExcelRow.ToString(), "F" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());


                            //Расчитано с учетом льгот и недопост.
                            excells1 = ExcelL.ExlWs.get_Range("G" + ExcelRow.ToString(), "G" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_real"].ToString());


                            //Изменения и перерасчет предыдущего периода без уч. недопоставок
                            excells1 = ExcelL.ExlWs.get_Range("H" + ExcelRow.ToString(), "H" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["reval"].ToString());


                            //Сумма перерасч. недопоставки. предыдущего периода
                            excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow.ToString(), "I" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_last_nedop"].ToString());


                            //Оплата произведенная в [monthtext1] [yearr] за [monthtext2] [year2]
                            excells1 = ExcelL.ExlWs.get_Range("J" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_money"].ToString());

                            //Оплата поставщиков
                            excells1 = ExcelL.ExlWs.get_Range("K" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["money_from"].ToString());

                            //Перерасчет между поставщиками
                            excells1 = ExcelL.ExlWs.get_Range("L" + ExcelRow.ToString(), "L" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["money_del"].ToString());


                            //Исходящее сальдо
                            excells1 = ExcelL.ExlWs.get_Range("M" + ExcelRow.ToString(), "M" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());
                            ExcelRow++;
                            num_str++;
                            i++;

                        }

                        //Пишем Итого по Территории
                        #region Итого по территории
                        if (ExcelRow > 7)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого " + old_serv;
                            ExcelL.ExlWs.Cells[ExcelRow, 3] = ssum_insaldo;
                            ExcelL.ExlWs.Cells[ExcelRow, 4] = srsum_tarif;
                            ExcelL.ExlWs.Cells[ExcelRow, 5] = srsum_lgota;
                            ExcelL.ExlWs.Cells[ExcelRow, 6] = ssum_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = ssum_real;
                            ExcelL.ExlWs.Cells[ExcelRow, 8] = sreval;
                            ExcelL.ExlWs.Cells[ExcelRow, 9] = ssum_last_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 10] = ssum_money;
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = smoney_from;
                            ExcelL.ExlWs.Cells[ExcelRow, 12] = smoney_del;
                            ExcelL.ExlWs.Cells[ExcelRow, 13] = ssum_outsaldo;
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                            "M" + ExcelRow.ToString());
                            excells1.Font.Bold = true;

                            ExcelRow++;

                            ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого " + old_area;
                            ExcelL.ExlWs.Cells[ExcelRow, 3] = sum_insaldo;
                            ExcelL.ExlWs.Cells[ExcelRow, 4] = rsum_tarif;
                            ExcelL.ExlWs.Cells[ExcelRow, 5] = rsum_lgota;
                            ExcelL.ExlWs.Cells[ExcelRow, 6] = sum_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_real;
                            ExcelL.ExlWs.Cells[ExcelRow, 8] = reval;
                            ExcelL.ExlWs.Cells[ExcelRow, 9] = sum_last_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 10] = sum_money;
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = money_from;
                            ExcelL.ExlWs.Cells[ExcelRow, 12] = money_del;
                            ExcelL.ExlWs.Cells[ExcelRow, 13] = sum_outsaldo;
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                            "M" + ExcelRow.ToString());
                            excells1.Font.Bold = true;

                            ExcelRow++;

                        }
                        #endregion

                        #endregion

                        #region Пишем подвал
                        if (ExcelRow > 7)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого ";
                            ExcelL.ExlWs.Cells[ExcelRow, 3] = isum_insaldo;
                            ExcelL.ExlWs.Cells[ExcelRow, 4] = irsum_tarif;
                            ExcelL.ExlWs.Cells[ExcelRow, 5] = irsum_lgota;
                            ExcelL.ExlWs.Cells[ExcelRow, 6] = isum_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = isum_real;
                            ExcelL.ExlWs.Cells[ExcelRow, 8] = ireval;
                            ExcelL.ExlWs.Cells[ExcelRow, 9] = isum_last_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 10] = isum_money;
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = imoney_from;
                            ExcelL.ExlWs.Cells[ExcelRow, 12] = imoney_del;
                            ExcelL.ExlWs.Cells[ExcelRow, 13] = isum_outsaldo;
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                            "M" + ExcelRow.ToString());
                            excells1.Font.Bold = true;

                        }



                        excells1 = ExcelL.ExlWs.get_Range("A5", "M" + ExcelRow.ToString());
                        excells1.Font.Size = 7;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excells1 = ExcelL.ExlWs.get_Range("C7", "M" + ExcelRow.ToString());
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlLandscape;
                        //excells1.Columns.AutoFit();


                        #endregion
                    }
                    else
                    {

                        excells1 = ExcelL.ExlWs.get_Range("A5", "I5");
                        excells1.EntireColumn.AutoFit();
                        ExcelL.ExlWs.Cells[10, 2] = "Статистика по домам по начислениям не подсчитана";
                    }
                    SetReportSing(ExcelL, ExcelRow + 3, false);


                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SaldoRep10_14_1" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        ExcelL.ExlWb.Close();
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }
                    finally
                    {
                        ////удаление объекта
                        //ExcelL.DeleteObject();
                    }
                    #endregion

                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSaldoRep10_14_1 DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формирование GetSaldoRep10_14_1 " + ex.Message, MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание GetSaldoRep10_14_1 DataTable : ОШИБКА!" + ex.Message;
                ////удаление объекта
                //ExcelL.DeleteObject();
                return ret;

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }
            return ret;

        }



        public Returns GetSaldoRep10_14_3(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            ExcelLoader ExcelL = null;

           

            try
            {

                //создание dataTable
                ExcelRep ExR = new ExcelRep();
                System.Data.DataTable DT = ExR.GetSaldoReport10_14_3(prm_, out ret, Nzp_user);

                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelL.ExlWs.Rows.Font.Name = "Arial";
                    //ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку

                    #region Получаем название месяца
                    string strMonthName = "";
                    string strMonthName1 = "";

                    switch (Convert.ToInt32(prm_.month_))
                    {
                        case 1:
                            {
                                strMonthName = "ЯНВАРЬ";
                                strMonthName1 = "ЯНВАРЕ";
                            }
                            break;
                        case 2:
                            {
                                strMonthName = "ФЕВРАЛЬ";
                                strMonthName1 = "ФЕВРАЛЕ";
                            }
                            break;
                        case 3:
                            {
                                strMonthName = "МАРТ";
                                strMonthName1 = "МАРТЕ";
                            }
                            break;
                        case 4:
                            {
                                strMonthName = "АПРЕЛЬ";
                                strMonthName1 = "АПРЕЛЕ";
                            }
                            break;
                        case 5:
                            {
                                strMonthName = "МАЙ";
                                strMonthName1 = "МАЕ";
                            }
                            break;
                        case 6:
                            {
                                strMonthName = "ИЮНЬ";
                                strMonthName1 = "ИЮНЕ";
                            }
                            break;
                        case 7:
                            {
                                strMonthName = "ИЮЛЬ";
                                strMonthName1 = "ИЮЛЕ";
                            }
                            break;
                        case 8:
                            {
                                strMonthName = "АВГУСТ";
                                strMonthName1 = "АВГУСТЕ";
                            }
                            break;
                        case 9:
                            {
                                strMonthName = "СЕНТЯБРЬ";
                                strMonthName = "СЕНТЯБРЕ";
                            }
                            break;
                        case 10:
                            {
                                strMonthName = "ОКТЯБРЬ";
                                strMonthName = "ОКТЯБРЕ";
                            }
                            break;
                        case 11:
                            {
                                strMonthName = "НОЯБРЬ";
                                strMonthName = "НОЯБРЕ";
                            }
                            break;
                        case 12:
                            {
                                strMonthName = "ДЕКАБРЬ";
                                strMonthName = "ДЕКАБРЕ";
                            }
                            break;
                        default:
                            break;
                    }
                    #endregion
                    Microsoft.Office.Interop.Excel.Range excells1;

                    //спустили шапку
                    excells1 = ExcelL.ExlWs.get_Range("A1", "M1");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "10.14.3 Сальдовая оборотная ведомость начислений и оплат по услугам ";
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A2", "M2");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;

                    excells1.FormulaR1C1 = " за " +
                        strMonthName + " " + prm_.year_.ToString() + "г.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("B3", "B3");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.ColumnWidth = 3;

                    excells1 = ExcelL.ExlWs.get_Range("C3", "M4");
                    excells1.Font.Bold = true;
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.RowHeight = 23;

                    excells1 = ExcelL.ExlWs.get_Range("A5", "A6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "№ п/п";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("B5", "B6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Услуга";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 23;

                    excells1 = ExcelL.ExlWs.get_Range("C5", "C6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Входящее сальдо";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 9.3;


                    excells1 = ExcelL.ExlWs.get_Range("D5", "D6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Рассчитано по тарифу";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 9.3;


                    excells1 = ExcelL.ExlWs.get_Range("E5", "E6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Скидка по льготе";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 7;

                    excells1 = ExcelL.ExlWs.get_Range("F5", "F6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = " Сумма недопоставки в расчетном месяце";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.3;

                    excells1 = ExcelL.ExlWs.get_Range("G5", "G6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Рассчитано с учетом льгот и недопоставок";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.3;

                    excells1 = ExcelL.ExlWs.get_Range("H5", "H6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Изменения и перерасчет предыдущего периода без уч. недопоставок";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.4;

                    excells1 = ExcelL.ExlWs.get_Range("I5", "I6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сумма перерасч. недопоставки. предыдущего периода";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 8.6;

                    excells1 = ExcelL.ExlWs.get_Range("J5", "J6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Оплата произведенная в " + strMonthName1 + " " + prm_.year_.ToString() +
                        " предыдущий месяц ";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.3;

                    excells1 = ExcelL.ExlWs.get_Range("K5", "L5");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "в т.ч.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("K6", "K6");
                    excells1.FormulaR1C1 = "Оплата поставщиков";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 5.3;

                    excells1 = ExcelL.ExlWs.get_Range("L6", "L6");
                    excells1.FormulaR1C1 = "Перерасчет между поставщиками";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 6.6;

                    excells1 = ExcelL.ExlWs.get_Range("M5", "M6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Исходящее сальдо";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 9.3;
                    #endregion
                    int ExcelRow = 7;
                    if (DT.Rows.Count > 0)
                    {


                        #region Пишем тело


                        int i = 0;
                        int num_str = 1;
                        decimal sum_insaldo = 0;
                        decimal rsum_tarif = 0;
                        decimal rsum_lgota = 0;
                        decimal sum_nedop = 0;
                        decimal sum_real = 0;
                        decimal sum_last_nedop = 0;
                        decimal reval = 0;
                        decimal sum_money = 0;
                        decimal money_from = 0;
                        decimal money_del = 0;
                        decimal sum_outsaldo = 0;

                        decimal isum_insaldo = 0;
                        decimal irsum_tarif = 0;
                        decimal irsum_lgota = 0;
                        decimal isum_nedop = 0;
                        decimal isum_real = 0;
                        decimal isum_last_nedop = 0;
                        decimal ireval = 0;
                        decimal isum_money = 0;
                        decimal imoney_from = 0;
                        decimal imoney_del = 0;
                        decimal isum_outsaldo = 0;

                        string old_area = "";
                        while (i < DT.Rows.Count)
                        {

                            if (old_area == "")
                            {
                                old_area = DT.Rows[i]["area"].ToString().Trim();
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = old_area;
                                ExcelRow++;
                            }

                            //Пишем Итого по Управляющая организация
                            if (old_area != DT.Rows[i]["area"].ToString().Trim())
                            {
                                ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого " + old_area;
                                ExcelL.ExlWs.Cells[ExcelRow, 3] = sum_insaldo;
                                ExcelL.ExlWs.Cells[ExcelRow, 4] = rsum_tarif;
                                ExcelL.ExlWs.Cells[ExcelRow, 5] = rsum_lgota;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = sum_nedop;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_real;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = reval;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = sum_last_nedop;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = sum_money;
                                ExcelL.ExlWs.Cells[ExcelRow, 11] = money_from;
                                ExcelL.ExlWs.Cells[ExcelRow, 12] = money_del;
                                ExcelL.ExlWs.Cells[ExcelRow, 13] = sum_outsaldo;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                                "M" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;
                                old_area = DT.Rows[i]["area"].ToString().Trim();
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = old_area;
                                ExcelRow++;
                                num_str = 1;

                                sum_insaldo = 0;
                                rsum_tarif = 0;
                                rsum_lgota = 0;
                                sum_nedop = 0;
                                sum_real = 0;
                                reval = 0;
                                sum_last_nedop = 0;
                                sum_money = 0;
                                money_from = 0;
                                money_del = 0;
                                sum_outsaldo = 0;

                            }
                            sum_insaldo = sum_insaldo + Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                            rsum_tarif = rsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                            rsum_lgota = rsum_lgota + Decimal.Parse(DT.Rows[i]["rsum_lgota"].ToString());
                            sum_nedop = sum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                            sum_real = sum_real + Decimal.Parse(DT.Rows[i]["sum_real"].ToString());
                            reval = reval + Decimal.Parse(DT.Rows[i]["reval"].ToString());
                            sum_last_nedop = sum_last_nedop + Decimal.Parse(DT.Rows[i]["sum_last_nedop"].ToString());
                            sum_money = sum_money + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                            money_from = money_from + Decimal.Parse(DT.Rows[i]["money_from"].ToString());
                            money_del = money_del + Decimal.Parse(DT.Rows[i]["money_del"].ToString());
                            sum_outsaldo = sum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());

                            isum_insaldo = isum_insaldo + Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                            irsum_tarif = irsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                            irsum_lgota = irsum_lgota + Decimal.Parse(DT.Rows[i]["rsum_lgota"].ToString());
                            isum_nedop = isum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                            isum_real = isum_real + Decimal.Parse(DT.Rows[i]["sum_real"].ToString());
                            ireval = ireval + Decimal.Parse(DT.Rows[i]["reval"].ToString());
                            isum_last_nedop = isum_last_nedop + Decimal.Parse(DT.Rows[i]["sum_last_nedop"].ToString());
                            isum_money = isum_money + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                            imoney_from = imoney_from + Decimal.Parse(DT.Rows[i]["money_from"].ToString());
                            imoney_del = imoney_del + Decimal.Parse(DT.Rows[i]["money_del"].ToString());
                            isum_outsaldo = isum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());


                            //Номер п/п
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = num_str.ToString();
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;


                            //Услуга/Территория
                            excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["service"].ToString().Trim();

                            //Входящее сальдо
                            excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());

                            //Рассчитано по тарифу
                            excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());

                            //Скидка по льготе
                            excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow.ToString(), "E" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["rsum_lgota"].ToString());

                            //Сумма недопоставки в расчетном месяце
                            excells1 = ExcelL.ExlWs.get_Range("F" + ExcelRow.ToString(), "F" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());


                            //Расчитано с учетом льгот и недопост.
                            excells1 = ExcelL.ExlWs.get_Range("G" + ExcelRow.ToString(), "G" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_real"].ToString());


                            //Изменения и перерасчет предыдущего периода без уч. недопоставок
                            excells1 = ExcelL.ExlWs.get_Range("H" + ExcelRow.ToString(), "H" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["reval"].ToString());


                            //Сумма перерасч. недопоставки. предыдущего периода
                            excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow.ToString(), "I" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_last_nedop"].ToString());


                            //Оплата произведенная в [monthtext1] [yearr] за [monthtext2] [year2]
                            excells1 = ExcelL.ExlWs.get_Range("J" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_money"].ToString());

                            //Оплата поставщиков
                            excells1 = ExcelL.ExlWs.get_Range("K" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["money_from"].ToString());

                            //Перерасчет между поставщиками
                            excells1 = ExcelL.ExlWs.get_Range("L" + ExcelRow.ToString(), "L" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["money_del"].ToString());


                            //Исходящее сальдо
                            excells1 = ExcelL.ExlWs.get_Range("M" + ExcelRow.ToString(), "M" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());
                            ExcelRow++;
                            num_str++;
                            i++;

                        }

                        //Пишем Итого по Территории
                        if (num_str > 1)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого " + old_area;
                            ExcelL.ExlWs.Cells[ExcelRow, 3] = sum_insaldo;
                            ExcelL.ExlWs.Cells[ExcelRow, 4] = rsum_tarif;
                            ExcelL.ExlWs.Cells[ExcelRow, 5] = rsum_lgota;
                            ExcelL.ExlWs.Cells[ExcelRow, 6] = sum_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_real;
                            ExcelL.ExlWs.Cells[ExcelRow, 8] = reval;
                            ExcelL.ExlWs.Cells[ExcelRow, 9] = sum_last_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 10] = sum_money;
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = money_from;
                            ExcelL.ExlWs.Cells[ExcelRow, 12] = money_del;
                            ExcelL.ExlWs.Cells[ExcelRow, 13] = sum_outsaldo;
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                            "M" + ExcelRow.ToString());
                            excells1.Font.Bold = true;

                            ExcelRow++;

                            sum_insaldo = 0;
                            rsum_tarif = 0;
                            rsum_lgota = 0;
                            sum_nedop = 0;
                            sum_real = 0;
                            reval = 0;
                            sum_last_nedop = 0;
                            sum_money = 0;
                            money_from = 0;
                            money_del = 0;
                            sum_outsaldo = 0;

                        }

                        #endregion

                        #region Пишем подвал
                        if (ExcelRow > 7)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 1] = "";
                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого ";
                            ExcelL.ExlWs.Cells[ExcelRow, 3] = isum_insaldo;
                            ExcelL.ExlWs.Cells[ExcelRow, 4] = irsum_tarif;
                            ExcelL.ExlWs.Cells[ExcelRow, 5] = irsum_lgota;
                            ExcelL.ExlWs.Cells[ExcelRow, 6] = isum_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = isum_real;
                            ExcelL.ExlWs.Cells[ExcelRow, 8] = ireval;
                            ExcelL.ExlWs.Cells[ExcelRow, 9] = isum_last_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 10] = isum_money;
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = imoney_from;
                            ExcelL.ExlWs.Cells[ExcelRow, 12] = imoney_del;
                            ExcelL.ExlWs.Cells[ExcelRow, 13] = isum_outsaldo;
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(),
                            "M" + ExcelRow.ToString());
                            excells1.Font.Bold = true;

                        }



                        excells1 = ExcelL.ExlWs.get_Range("A5", "M" + ExcelRow.ToString());
                        excells1.Font.Size = 7;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excells1 = ExcelL.ExlWs.get_Range("C7", "M" + ExcelRow.ToString());
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlLandscape;
                        //excells1.Columns.AutoFit();


                        #endregion
                    }
                    else
                    {

                        excells1 = ExcelL.ExlWs.get_Range("A5", "I5");
                        excells1.EntireColumn.AutoFit();
                        ExcelL.ExlWs.Cells[10, 2] = "Статистика по домам по начислениям не подсчитана";
                    }

                    SetReportSing(ExcelL, ExcelRow + 3, false);

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SaldoRep10_14_3" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        ExcelL.ExlWb.Close();
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }
                    finally
                    {
                        ////удаление объекта
                        //ExcelL.DeleteObject();
                    }
                    #endregion

                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSaldoRep10_14_3 DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формирование GetSaldoRep10_14_3 " + ex.Message, MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание GetSaldoRep10_14_3 DataTable : ОШИБКА!" + ex.Message;
                ////удаление объекта
                //ExcelL.DeleteObject();
                return ret;

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }
            return ret;

        }


        public string SetReportHeader()
        {
            if (STCLINE.KP50.Interfaces.Points.IsSmr)
            {
                return "ООО \"УК \"Ассоциация Управляющих Компаний\" САМАРА Г";
            }
            else if (STCLINE.KP50.Interfaces.Points.Pref.IndexOf("sah") > -1)
            {
                return "";
            }
            else
            {
                return "";
            }
        }

        //Отчет: отчет по заявкам
        public Returns GetOrderList(int nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            Supg sg = new Supg();
            System.Data.DataTable DT_Body = sg.OrderList(nzp_user, out ret);
            System.Data.DataTable DT_Info = sg.GetReportInfo(nzp_user, out ret);

            Dictionary<string, string> Dic = new Dictionary<string, string>();
            string info = "";
            for (int h = 0; h < DT_Info.Rows.Count; h++)
            {
                info += DT_Info.Rows[h][0].ToString().Trim() + ": " + DT_Info.Rows[h][1].ToString().Trim() + "; ";
            }
            Dic.Add("str_date", "Дата печати: " + System.DateTime.Today.ToShortDateString());
            Dic.Add("str_filter", info);
            Dic.Add("ERCName", SetReportHeader());
            try
            {
                int indexer = 7;

                ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("listzvk_r1.xls"), Dic);

                if (DT_Body.Rows.Count != 0)
                {
                    for (int r = 0; r < DT_Body.Rows.Count; r++)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 5;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.WrapText = true;
                        excell.Value2 = (r + 1).ToString();

                        excell = ExcelL.ExlWs.get_Range("B" + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 17;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.NumberFormat = "@";
                        excell.Value2 = DT_Body.Rows[r][2].ToString().Trim() + "\r\n" + "№ " + DT_Body.Rows[r][1].ToString().Trim();

                        excell = ExcelL.ExlWs.get_Range("C" + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 21;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.NumberFormat = "@";
                        excell.WrapText = true;
                        excell.Value2 = DT_Body.Rows[r][3].ToString().Trim();

                        excell = ExcelL.ExlWs.get_Range("D" + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 25;
                        excell.WrapText = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = DT_Body.Rows[r][4].ToString().Trim();


                        excell = ExcelL.ExlWs.get_Range("E" + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 38;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.WrapText = true;
                        excell.Value2 = DT_Body.Rows[r][6].ToString().Trim();


                        excell = ExcelL.ExlWs.get_Range("F" + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 20;
                        excell.NumberFormat = "@";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = DT_Body.Rows[r][5].ToString().Trim();

                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + r).ToString(), "F" + (indexer + r).ToString());
                        excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;

                        excell.Borders[XlBordersIndex.xlInsideHorizontal].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlLineStyleNone;
                        excell.Borders[XlBordersIndex.xlInsideVertical].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlLineStyleNone;
                    }
                }
                else
                {
                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "F" + (indexer + 4).ToString());
                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";

                }

                if (ret.result)
                {
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "listzvk_r1" + nzp_user) + ".xls";
                    string filePath = STCLINE.KP50.Global.Constants.ExcelDir + fileName;
                    ret = ExcelL.SaveFile(filePath, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (ret.result && InputOutput.useFtp)
                    {
                        ExcelL.ExlWb.Close();
                        fileName = InputOutput.SaveOutputFile(filePath);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

            }
            finally
            {
                //удаление объекта
                ExcelL.DeleteObject();
            }
            return ret;
        }

        /// <summary>
        /// процедура для получения отчетов по плановым работам
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="en"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Returns GetPlannedWorksList(int nzp_user, enSrvOper en, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            Microsoft.Office.Interop.Excel.Range excell_begin;
            Microsoft.Office.Interop.Excel.Range excell_end;
            DbNedop nedop = new DbNedop();
            bool flag = false;
            int indexer = 0;

            if (en == enSrvOper.GetPlannedWorksNone || en == enSrvOper.GetPlannedWorksActs)
            {
                indexer = 5;
            }
            if (en == enSrvOper.GetPlannedWorksSupp)
            {
                indexer = 6;
            }


            Dictionary<string, string> Dic = new Dictionary<string, string>();
            string info = "";

            List<NedopInfo> Body = nedop.PlannedWorksList(nzp_user, en, out ret);//данные для шапки
            System.Data.DataTable Info = nedop.GetReportInfo(nzp_user, out ret);//данные для шапки отчета

            try
            {
                #region отчет по сведениям по отключениям услуг по поставщикам

                if (en == enSrvOper.GetPlannedWorksSupp)
                {
                    if (Body.Count != 0)
                    {
                        ArrayList name_supp = new ArrayList();//подрядчики
                        Microsoft.Office.Interop.Excel.Range excell_template = null;//для шапок других страниц

                        #region узнаем подрядчиков

                        name_supp.Add(Body[0].name_supp.Trim());
                        for (int i = 1; i < Body.Count; i++)
                        {
                            if (!name_supp.Contains(Body[i].name_supp.Trim()))
                            {
                                name_supp.Add(Body[i].name_supp.Trim());
                            }
                        }

                        #endregion

                        for (int k = 0; k < name_supp.Count; k++)
                        {
                            #region для каждой страницы отдельно
                            // исключить одинаковое название страницы
                            int j = 0;
                            string curname_supp = name_supp[k].ToString().Trim();
                            if (curname_supp.Length >= 31) curname_supp = curname_supp.Substring(0, 28);
                            for (int i = 0; i < k; i++)
                            {
                                string checkname_supp = name_supp[i].ToString().Trim();
                                if (checkname_supp.Length >= 31) checkname_supp = checkname_supp.Substring(0, 28);
                                if (checkname_supp == curname_supp) j += 1;
                            }
                            if (j > 0) curname_supp = curname_supp.Substring(0, 28) + "_" + (j + 1).ToString();
                            ExcelL.GetWs(k + 1, curname_supp);

                            if (k == 0)
                            {
                                for (int h = 0; h < Info.Rows.Count; h++)
                                {
                                    info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                                }
                                Dic.Add("str_date", "Дата печати: " + System.DateTime.Today.ToShortDateString());
                                Dic.Add("name_supp", name_supp[k].ToString());
                                Dic.Add("str_filter", info);
                                ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("Plan_31.xls"), Dic);
                                ExcelL.ExlWs.Name = curname_supp; //name_supp[k].ToString().Trim();
                                //шапка для других страниц отчета
                                excell_template = ExcelL.ExlWs.get_Range("A1", "H5");
                            }
                            else
                            {
                                excell = ExcelL.ExlWs.get_Range("A1", "H5");
                                excell_template.Copy(Type.Missing);
                                //вставка шапки
                                excell.PasteSpecial(XlPasteType.xlPasteAll, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);
                                excell = ExcelL.ExlWs.get_Range("A4", "H4");
                                excell.Value2 = curname_supp; //name_supp[k].ToString();
                            }
                            #endregion

                            #region вставка данных

                            int counter = 0;
                            indexer = 6;
                            int begin = 0;
                            int end = 0;

                            for (int i = 0; i < Body.Count; i++)
                            {
                                if (name_supp[k].ToString().Trim() == Body[i].name_supp.Trim())
                                {
                                    //номер
                                    counter++;
                                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 5.3;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.Value2 = counter.ToString();

                                    //характер аварии
                                    excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 26.4;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    if (Body[i].plan_date != null && !String.IsNullOrEmpty(Body[i].plan_date.ToString()))
                                    {
                                        excell.Value2 = "Документ №" + Body[i].plan_number + " от " + Body[i].plan_date + " " + Body[i].comment;
                                    }
                                    else
                                    {
                                        excell.Value2 = "Документ №" + Body[i].plan_number + " " + Body[i].comment;
                                    }

                                    //дата, время отключения
                                    excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 11.3;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    excell.Value2 = Body[i].disconnect_date;

                                    //дата, время подключения
                                    excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 12.4;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    excell.Value2 = Body[i].connect_date;

                                    //ответственный
                                    excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), Type.Missing);
                                    excell.WrapText = true;
                                    excell.ColumnWidth = 13.1;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.Value2 = Body[i].officer;

                                    excell_begin = null;

                                    for (int l = 0; l < Body[i].geu_list.Count; l++)
                                    {
                                        //ЖЭУ
                                        excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                        excell.ColumnWidth = 11.7;
                                        excell.WrapText = true;
                                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                        excell.NumberFormat = "@";
                                        excell.Value2 = Body[i].geu_list[l].ToString();

                                        //список адресов
                                        excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), Type.Missing);
                                        excell.ColumnWidth = 29.3;
                                        excell.WrapText = true;
                                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                        excell.NumberFormat = "@";
                                        excell.Value2 = Body[i].address_list[l].ToString();

                                        if (l == 0)
                                        {
                                            excell_begin = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                            begin = indexer;
                                        }
                                        else
                                        {
                                            excell_end = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                            end = indexer;

                                            if (excell_begin.Value2.ToString() != excell_end.Value2.ToString() || (l == Body[i].geu_list.Count - 1))
                                            {
                                                if (l != Body[i].geu_list.Count - 1)
                                                {
                                                    excell_end = ExcelL.ExlWs.get_Range("B" + begin.ToString(), "B" + (end - 1).ToString());
                                                }
                                                else
                                                {
                                                    excell_end = ExcelL.ExlWs.get_Range("B" + begin.ToString(), "B" + end.ToString());
                                                }

                                                excell_end.Merge(Type.Missing);
                                                excell_end.WrapText = true;

                                                excell_begin = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                                begin = indexer;
                                            }
                                            else
                                            {

                                            }
                                        }
                                        indexer++;
                                    }

                                    //объединяем поля: номер, характер аварии, обе даты и ответственный
                                    excell = ExcelL.ExlWs.get_Range("A" + (indexer - Body[i].geu_list.Count).ToString(), "A" + (indexer - 1).ToString());
                                    excell.Merge(Type.Missing);
                                    excell = ExcelL.ExlWs.get_Range("E" + (indexer - Body[i].geu_list.Count).ToString(), "E" + (indexer - 1).ToString());
                                    excell.Merge(Type.Missing);
                                    excell = ExcelL.ExlWs.get_Range("F" + (indexer - Body[i].geu_list.Count).ToString(), "F" + (indexer - 1).ToString());
                                    excell.Merge(Type.Missing);
                                    excell = ExcelL.ExlWs.get_Range("G" + (indexer - Body[i].geu_list.Count).ToString(), "G" + (indexer - 1).ToString());
                                    excell.Merge(Type.Missing);
                                    excell = ExcelL.ExlWs.get_Range("H" + (indexer - Body[i].geu_list.Count).ToString(), "H" + (indexer - 1).ToString());
                                    excell.Merge(Type.Missing);

                                    excell = ExcelL.ExlWs.get_Range("A" + (indexer - Body[i].geu_list.Count).ToString(), "H" + (indexer - 1).ToString());
                                    excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                                }
                            }

                            #endregion
                        }

                        //сортируем страницы книги по названию
                        ExcelL.SortWs();
                    }
                    else
                    {
                        flag = true;
                    }
                }

                #endregion

                #region отчет сведения по отключениям услуг

                if (en == enSrvOper.GetPlannedWorksNone)
                {
                    for (int h = 0; h < Info.Rows.Count; h++)
                    {
                        info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                    }
                    Dic.Add("str_date", "Дата печати: " + System.DateTime.Today.ToShortDateString());
                    Dic.Add("str_filter", info);
                    ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("Plan_32.xls"), Dic);

                    if (Body.Count != 0)
                    {
                        for (int r = 0; r < Body.Count; r++)
                        {
                            #region вывод данных
                            //номер
                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 5.3;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.WrapText = true;
                            excell.Value2 = (r + 1).ToString();

                            //№ док-та, дата
                            excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 11.7;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.NumberFormat = "@";
                            excell.Value2 = "№" + Body[r].plan_number + " от " + Body[r].plan_date;

                            //поставщик
                            excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 21.4;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.NumberFormat = "@";
                            excell.WrapText = true;
                            excell.Value2 = Body[r].name_supp;

                            excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 20.4;

                            excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 11.4;

                            //цель отключения
                            excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), "F" + indexer.ToString());
                            excell.Merge(Type.Missing);
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.WrapText = true;
                            excell.Value2 = Body[r].comment;

                            //Дата, время отключения
                            excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 12.1;
                            excell.WrapText = true;
                            excell.NumberFormat = "@";
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = Body[r].disconnect_date;


                            //Дата, время подключения
                            excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), Type.Missing);
                            excell.WrapText = true;
                            excell.ColumnWidth = 11.9;
                            excell.NumberFormat = "@";
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = Body[r].disconnect_date;


                            //отключаемые объекты
                            for (int i = 0; i < Body[r].address_list.Count; i++)
                            {
                                excell = ExcelL.ExlWs.get_Range("D" + (indexer + i).ToString(), Type.Missing);
                                excell.ColumnWidth = 31.4;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = Body[r].address_list[i].Trim();
                            }

                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "A" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), "B" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), "C" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), "F" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);


                            excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), "G" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);


                            excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), "H" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            if (Body[r].address_list.Count > 1)
                            {
                                indexer += Body[r].address_list.Count;
                            }
                            else
                            {
                                indexer += 1;
                            }

                            #endregion
                        }
                    }
                    else
                    {
                        flag = true;
                    }
                }

                #endregion

                #region отчет акты по отключениям услуг

                if (en == enSrvOper.GetPlannedWorksActs)
                {
                    for (int h = 0; h < Info.Rows.Count; h++)
                    {
                        info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                    }
                    Dic.Add("str_date", "Дата печати: " + System.DateTime.Today.ToShortDateString());
                    Dic.Add("str_filter", info);
                    ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("Plan_33.xls"), Dic);

                    if (Body.Count != 0)
                    {
                        for (int r = 0; r < Body.Count; r++)
                        {
                            #region вывод данных
                            //номер
                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 5.3;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.WrapText = true;
                            excell.Value2 = (r + 1).ToString();

                            //№ акта, дата
                            excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 11.7;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.NumberFormat = "@";
                            if (!String.IsNullOrEmpty(Body[r].number) || !String.IsNullOrEmpty(Body[r].act_date))
                            {
                                if (!String.IsNullOrEmpty(Body[r].act_date))
                                {
                                    excell.Value2 = "№" + Body[r].number + " от " + Body[r].act_date;
                                }
                                else
                                {
                                    excell.Value2 = "№" + Body[r].number;
                                }
                            }

                            //№ документа, дата
                            excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 11.8;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.NumberFormat = "@";
                            excell.Value2 = "№" + Body[r].plan_number + " от " + Body[r].plan_date;

                            //услуга, тип недопоставки
                            excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 32.6;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = Body[r].name_nedop;

                            //температура
                            excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 7.3;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.WrapText = true;
                            excell.Value2 = Body[r].tn;

                            //дата, время отключения
                            excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 12;
                            excell.WrapText = true;
                            excell.NumberFormat = "@";
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = Body[r].disconnect_date;

                            //дата, время подключения
                            excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 15.7;
                            excell.WrapText = true;
                            excell.NumberFormat = "@";
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = Body[r].connect_date;

                            //адреса недопоставки
                            for (int i = 0; i < Body[r].address_list.Count; i++)
                            {
                                excell = ExcelL.ExlWs.get_Range("F" + (indexer + i).ToString(), Type.Missing);
                                excell.ColumnWidth = 31.4;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = Body[r].address_list[i].Trim();
                            }

                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "A" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), "B" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), "C" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), "D" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), "E" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), "G" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), "H" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Merge(Type.Missing);

                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (indexer + Body[r].address_list.Count - 1).ToString());
                            excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            if (Body[r].address_list.Count > 1)
                            {
                                indexer += Body[r].address_list.Count;
                            }
                            else
                            {
                                indexer += 1;
                            }

                            #endregion
                        }
                    }
                    else
                    {
                        flag = true;
                    }
                }

                #endregion

                #region нет данных

                if (flag)
                {
                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (indexer + 4).ToString());
                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";
                }

                #endregion

                if (ret.result)
                {
                    if (en == enSrvOper.GetPlannedWorksSupp)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "Plan_31" + nzp_user) + ".xls";
                    }
                    if (en == enSrvOper.GetPlannedWorksNone)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "Plan_32" + nzp_user) + ".xls";
                    }
                    if (en == enSrvOper.GetPlannedWorksActs)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "Plan_33" + nzp_user) + ".xls";
                    }
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

            }
            finally
            {
                //удаление объекта
                ExcelL.DeleteObject();
            }
            return ret;
        }

        /// <summary>
        /// процедура для получения отчетов по списку заявок
        /// </summary>
        /// <returns></returns>
        public Returns GetSupgReports(SupgFinder finder, enSrvOper en, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            Supg supg = new Supg();
            List<ZvkFinder> Data = null;
            bool flag = false;
            int indexer = 0;

            Dictionary<string, string> Dic = new Dictionary<string, string>();
            string info = "";

            //1.1
            #region отчет: информация, полученная ОДДС

            if (en == enSrvOper.GetInfoFromService)
            {
                Data = supg.GetInfoFromService(finder, en, out ret);
            }

            #endregion

            //1.2
            #region отчет: приложение к информации, полученной ОДДС
            if (en == enSrvOper.GetAppInfoFromService)
            {
                Data = supg.GetAppInfoFromService(finder, en, out ret);
            }
            #endregion

            //1.4
            #region отчет: список невыполненных нарядов-заказов к концу

            if (en == enSrvOper.GetJoborderPeriodOutstand)
            {
                Data = supg.GetJoborderPeriodOutstand(finder, en, out ret);
            }

            #endregion

            //2.4
            #region отчет: количество переадресаций заявок, принятых ОДДС

            if (en == enSrvOper.GetCountOrderReadres)
            {
                Data = supg.GetCountOrderReadres(finder, en, out ret);
            }

            #endregion

            //1.3.1
            #region отчет: cписок сообщений, зарегестрированных ОДДС

            if (en == enSrvOper.GetMessageList)
            {
                Data = supg.GetMessageList(finder, en, out ret);
            }

            #endregion

            //1.3.2
            #region отчет: список сообщений, зарегестрированных ОДДС(опрос)

            if (en == enSrvOper.GetMessageQuestList)
            {
                Data = supg.GetMessageList(finder, en, out ret);
            }

            #endregion

            System.Data.DataTable Info = supg.GetReportInfo(finder.nzp_user, out ret);//данные для шапки отчета

            try
            {
                //1.1
                #region отчет: количество переадресаций заявок, принятых ОДДС

                if (en == enSrvOper.GetInfoFromService)
                {
                    if (Data.Count != 0)
                    {
                        #region для шапки

                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("str_filter", info);
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("info_from_service.xls"), Dic);

                        #endregion

                        #region вставка данных

                        indexer = 7;
                        int counter = 0;
                        int sum_cnt = 0;
                        for (int i = 0; i < Data.Count; i++)
                        {
                            //номер
                            if (Data[i].type != "3")
                            {
                                counter++;
                                excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 5.7;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.Value2 = counter.ToString();
                            }

                            //виды заявок
                            excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 58.3;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.NumberFormat = "@";
                            excell.Value2 = Data[i].service;

                            //за период
                            excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 17.6;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.Value2 = Data[i].cnt;

                            if (Data[i].type != "3")
                            {
                                sum_cnt += Convert.ToInt32(Data[i].cnt);//считаем сумму
                            }

                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "C" + indexer.ToString());
                            excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            indexer++;
                        }

                        //виды заявок
                        excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                        excell.ColumnWidth = 58.3;
                        excell.Font.Bold = true;
                        excell.WrapText = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.NumberFormat = "@";
                        excell.Value2 = "Всего сообщений: ";

                        //за период
                        excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                        excell.ColumnWidth = 17.6;
                        excell.WrapText = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.NumberFormat = "0";
                        excell.Value2 = sum_cnt.ToString();

                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "C" + indexer.ToString());
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;


                        #endregion
                    }
                    else
                    {
                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("str_filter", info);
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("info_from_service.xls"), Dic);

                        flag = true;
                    }
                }

                #endregion

                //1.2
                #region отчет: приложение к информации, полученной ОДДС

                if (en == enSrvOper.GetAppInfoFromService)
                {
                    Microsoft.Office.Interop.Excel.Range excell_template = null;//для шапок других страниц

                    if (Data.Count != 0)
                    {
                        ArrayList name_supp = new ArrayList();//подрядчики


                        #region узнаем подрядчиков

                        name_supp.Add(Data[0].slug_name);
                        for (int i = 1; i < Data.Count; i++)
                        {
                            if (!name_supp.Contains(Data[i].slug_name))
                            {
                                name_supp.Add(Data[i].slug_name);
                            }
                        }

                        #endregion

                        for (int k = 0; k < name_supp.Count; k++)
                        {
                            #region для каждой страницы отдельно
                            // исключить одинаковое название страницы
                            int j = 0;
                            string curname_supp = name_supp[k].ToString().Trim();
                            if (curname_supp.Length >= 31) curname_supp = curname_supp.Substring(0, 28);
                            for (int i = 0; i < k; i++)
                            {
                                string checkname_supp = name_supp[i].ToString().Trim();
                                if (checkname_supp.Length >= 31) checkname_supp = checkname_supp.Substring(0, 28);
                                if (checkname_supp == curname_supp) j += 1;
                            }
                            if (j > 0) curname_supp = curname_supp.Substring(0, 28) + "_" + (j + 1).ToString();
                            ExcelL.GetWs(k + 1, curname_supp);

                            if (k == 0)
                            {
                                for (int h = 0; h < Info.Rows.Count; h++)
                                {
                                    info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                                }
                                Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                                Dic.Add("name_supp", name_supp[k].ToString());
                                Dic.Add("str_filter", info);
                                ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("appinfo_from_service.xls"), Dic);
                                ExcelL.ExlWs.Name = curname_supp; //name_supp[k].ToString().Trim();
                                //шапка для других страниц отчета
                                excell_template = ExcelL.ExlWs.get_Range("A1", "F6");
                            }
                            else
                            {
                                excell = ExcelL.ExlWs.get_Range("A1", "F6");
                                excell_template.Copy(Type.Missing);
                                //вставка шапки
                                excell.PasteSpecial(XlPasteType.xlPasteAll, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);
                                excell = ExcelL.ExlWs.get_Range("A5", Type.Missing);
                                excell.Merge(Type.Missing);
                                excell.Value2 = name_supp[k].ToString();
                            }
                            #endregion

                            #region вставка данных

                            indexer = 7;
                            int counter = 0;
                            for (int i = 0; i < Data.Count; i++)
                            {
                                if (name_supp[k].ToString().Trim() == Data[i].slug_name.Trim())
                                {
                                    //номер
                                    counter++;
                                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 5.7;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.Value2 = counter.ToString();

                                    //дата, время, N
                                    excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 18.7;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    excell.Value2 = Data[i].zvk_date + " N " + Data[i].nzp_zvk;

                                    //адрес, Ф.И.О., телефон
                                    excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 22.3;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    excell.Value2 = Data[i].adr + " " + Data[i].fio + " " + Data[i].phone;


                                    //содержание заявки
                                    excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 26.1;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    excell.Value2 = Data[i].comment;

                                    //принятые меры
                                    excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 22.4;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.Value2 = Data[i].r_comment;

                                    //отметка о выполнении
                                    excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 29.3;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.Value2 = Data[i].result_comment;

                                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "F" + indexer.ToString());
                                    excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                                    indexer++;
                                }
                            }

                            #endregion
                        }

                        //сортируем страницы книги по названию
                        ExcelL.SortWs();
                    }
                    else
                    {
                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("name_supp", "нет данных");
                        Dic.Add("str_filter", info);
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("appinfo_from_service.xls"), Dic);
                        ExcelL.ExlWs.Name = "нет данных".ToString().Trim();
                        //шапка для других страниц отчета
                        excell_template = ExcelL.ExlWs.get_Range("A1", "F6");

                        flag = true;
                    }
                }

                #endregion

                //1.4
                #region отчет: список невыполненных нарядов-заказов к концу периода

                if (en == enSrvOper.GetJoborderPeriodOutstand)
                {
                    Microsoft.Office.Interop.Excel.Range excell_template = null;//для шапок других страниц

                    if (Data.Count != 0)
                    {
                        ArrayList name_supp = new ArrayList();//подрядчики

                        #region узнаем подрядчиков

                        name_supp.Add(Data[0].name_supp);
                        for (int i = 1; i < Data.Count; i++)
                        {
                            if (!name_supp.Contains(Data[i].name_supp))
                            {
                                name_supp.Add(Data[i].name_supp);
                            }
                        }

                        #endregion

                        for (int k = 0; k < name_supp.Count; k++)
                        {
                            #region для каждой страницы отдельно
                            // исключить одинаковое название страницы
                            int j = 0;
                            string curname_supp = name_supp[k].ToString().Trim();
                            if (curname_supp.Length >= 31) curname_supp = curname_supp.Substring(0, 28);
                            for (int i = 0; i < k; i++)
                            {
                                string checkname_supp = name_supp[i].ToString().Trim();
                                if (checkname_supp.Length >= 31) checkname_supp = checkname_supp.Substring(0, 28);
                                if (checkname_supp == curname_supp) j += 1;
                            }
                            if (j > 0) 
                                curname_supp = curname_supp.Substring(0, 28) + "_" + (j + 1).ToString();
                            ExcelL.GetWs(k + 1, curname_supp);

                            if (k == 0)
                            {
                                for (int h = 0; h < Info.Rows.Count; h++)
                                {
                                    info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                                }
                                Dic.Add("end_date", finder._date_to);
                                Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                                Dic.Add("name_supp", name_supp[k].ToString());
                                Dic.Add("str_filter", info);
                                ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("joborder_period_outstand.xls"), Dic);
                                //заменяем недопустимые символы
                                ExcelL.ExlWs.Name =
                                    curname_supp.Replace("/", "|")
                                        .Replace("\\", "|")
                                        .Replace("[", "|")
                                        .Replace("]", "|")
                                        .Replace("*", "|")
                                        .Replace("?", "|"); //name_supp[k].ToString().Trim();

                                //шапка для других страниц отчета
                                excell_template = ExcelL.ExlWs.get_Range("A1", "L6");
                            }
                            else
                            {
                                excell = ExcelL.ExlWs.get_Range("A1", "L6");
                                excell_template.Copy(Type.Missing);
                                //вставка шапки
                                excell.PasteSpecial(XlPasteType.xlPasteAll, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);
                                excell = ExcelL.ExlWs.get_Range("A5", "E5");
                                excell.Merge(Type.Missing);
                                excell.Value2 = name_supp[k].ToString();

                            }
                            #endregion

                            #region вставка данных

                            indexer = 7;
                            int counter = 0;
                            for (int i = 0; i < Data.Count; i++)
                            {
                                if (name_supp[k].ToString().Trim() == Data[i].name_supp.Trim())
                                {
                                    //номер
                                    counter++;
                                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 5.7;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.Value2 = counter.ToString();

                                    //дата, время, N
                                    excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 20;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    excell.Value2 = Data[i].order_date;

                                    //номер наряда-заказа
                                    excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 10.57;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.Value2 = Data[i].nzp_zk;

                                    //адрес, ф.и.о., телефон
                                    excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 21.14;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    excell.Value2 = Data[i].adr + " " + Data[i].fio + " " + Data[i].phone;

                                    //претензия, услуга
                                    excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 23.7;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.Value2 = Data[i].service;

                                    //результат
                                    excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 10.7;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.Value2 = Data[i].res_name;

                                    //контрольный срок
                                    excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 15.5;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.Value2 = Data[i].control_date;


                                    //дата выполнения
                                    excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 15.5;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.Value2 = Data[i].fact_date;

                                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + indexer.ToString());
                                    excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                                    indexer++;
                                }
                            }

                            #endregion
                        }

                        //сортируем страницы книги по названию (если количество страниц больше 2х, во избежание вывода первой пустой страницы)
                        if (name_supp.Count > 2) ExcelL.SortWs();
                    }
                    else
                    {
                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("end_date", finder._date_to);
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("name_supp", "нет данных".ToString());
                        Dic.Add("str_filter", info);
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("joborder_period_outstand.xls"), Dic);
                        ExcelL.ExlWs.Name = "нет данных".ToString().Trim();
                        //шапка для других страниц отчета
                        excell_template = ExcelL.ExlWs.get_Range("A1", "L6");

                        flag = true;
                    }
                }

                #endregion

                //2.4
                #region отчет: количество переадресаций заявок, принятых ОДДС

                if (en == enSrvOper.GetCountOrderReadres)
                {
                    if (Data.Count != 0)
                    {
                        #region для шапки

                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("str_filter", info);
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("count_order_readres.xls"), Dic);

                        #endregion

                        #region обработка входных данных

                        List<string> nzp_slugs = Data.Select(x => x.nzp_slug).Distinct().ToList<string>();//получаем список уникальных служб
                        List<string> nzp_geus = Data.Select(x => x.nzp_geu).Distinct().ToList<string>();//получаем список уникальных ЖЭУ

                        ExcelFormater format = new ExcelFormater();
                        format.MakeBukvaList(nzp_geus.Count + 3);//создали массив букв для листа

                        int counter = 0;
                        bool column_flag = false;
                        indexer = 7;
                        List<List<string>> sum = new List<List<string>>();

                        foreach (string nzp_slug in nzp_slugs)
                        {
                            //номер
                            counter++;
                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 3;
                            excell.Font.Size = 8;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.Value2 = counter.ToString();
                            excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);


                            //пишем название наименования службы
                            ZvkFinder current_slug = Data.First(z => z.nzp_slug == nzp_slug);
                            excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 18;
                            excell.Font.Size = 8;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.Value2 = current_slug.slug_name;
                            excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);

                            int abc = 0;
                            List<string> row = new List<string>();
                            foreach (string nzp_geu in nzp_geus)
                            {
                                if (!column_flag)
                                {
                                    //название столбца
                                    ZvkFinder current_geu = Data.First(g => g.nzp_slug == nzp_slug && g.nzp_geu == nzp_geu);
                                    excell = ExcelL.ExlWs.get_Range(format.BukvaList[abc + 2] + "6", Type.Missing);
                                    excell.ColumnWidth = 10.3;
                                    excell.Font.Bold = true;
                                    excell.Font.Size = 8;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                    excell.Value2 = current_geu.geu;
                                    excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);
                                }
                                ZvkFinder current_value = Data.FirstOrDefault(v => v.nzp_geu == nzp_geu && v.nzp_slug == nzp_slug);

                                excell = ExcelL.ExlWs.get_Range(format.BukvaList[abc + 2] + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 10.3;
                                excell.Font.Size = 8;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.Value2 = current_value.cnt;
                                row.Add(current_value.cnt);
                                excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);
                                abc++;
                            }

                            excell = ExcelL.ExlWs.get_Range(format.BukvaList[abc + 2] + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 10.3;
                            excell.Font.Size = 8;
                            excell.Font.Bold = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.Value2 = row.Sum(item => Convert.ToInt32(item));
                            excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);

                            sum.Add(row);
                            column_flag = true;
                            indexer++;
                        }

                        #region Всего

                        //по горизонтали
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "B" + indexer.ToString());
                        excell.Merge(Type.Missing);
                        excell.Font.Size = 8;
                        excell.Font.Bold = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = "Всего";
                        excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);

                        //по вертикали
                        excell = excell = ExcelL.ExlWs.get_Range(format.BukvaList[nzp_geus.Count + 2] + "6", Type.Missing);
                        excell.Merge(Type.Missing);
                        excell.Font.Size = 8;
                        excell.Font.Bold = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = "Всего";
                        excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);



                        int rs_sum = 0;
                        for (int n = 0; n < nzp_geus.Count; n++)
                        {
                            int geu_column_sum = 0;
                            foreach (List<string> item in sum)
                            {
                                geu_column_sum += Convert.ToInt32(item[n]);
                            }
                            excell = ExcelL.ExlWs.get_Range(format.BukvaList[n + 2] + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 10.3;
                            excell.Font.Size = 8;
                            excell.Font.Bold = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.Value2 = geu_column_sum.ToString();
                            rs_sum += geu_column_sum;
                            excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);
                        }

                        excell = ExcelL.ExlWs.get_Range(format.BukvaList[nzp_geus.Count + 2] + indexer.ToString(), Type.Missing);
                        excell.ColumnWidth = 10.3;
                        excell.Font.Size = 8;
                        excell.Font.Bold = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = rs_sum.ToString();
                        excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);

                        #endregion

                        #endregion
                    }
                    else
                    {
                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("str_filter", info);
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("count_order_readres.xls"), Dic);

                        flag = true;
                    }
                }

                #endregion

                //1.3.1
                #region отчет: cписок сообщений, зарегистрированных ОДДС

                if (en == enSrvOper.GetMessageList)
                {
                    if (Data.Count != 0)
                    {
                        #region для шапки

                        //подсчет количества сообщений, заявлений
                        ArrayList message_count = new ArrayList();//количество сообщений
                        ArrayList zakaz_count = new ArrayList();//количество заявлений

                        message_count.Add(Data[0].nzp_zvk);
                        zakaz_count.Add(Data[0].nzp_zk);
                        for (int i = 1; i < Data.Count; i++)
                        {
                            if (!message_count.Contains(Data[i].nzp_zvk))
                            {
                                message_count.Add(Data[i].nzp_zvk);
                            }
                            if (!zakaz_count.Contains(Data[i].nzp_zk))
                            {
                                zakaz_count.Add(Data[i].nzp_zk);
                            }
                        }

                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("str_filter", info);
                        Dic.Add("str_count", "Количество сообщений: " + message_count.Count.ToString() + " Количество нарядов-заказов: " + zakaz_count.Count.ToString());
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("message_list.xls"), Dic);

                        #endregion

                        #region вставка данных

                        indexer = 7;
                        int counter = 0;
                        int curt_nzp_zvk = 0;
                        for (int i = 0; i < Data.Count; i++)
                        {
                            if (i == 0 || curt_nzp_zvk != Data[i].nzp_zvk)
                            {
                                curt_nzp_zvk = Data[i].nzp_zvk;
                                //номер
                                counter++;
                                excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 5.7;
                                excell.Font.Size = 9;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.Value2 = counter.ToString();

                                //дата, время, № заявки
                                excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 16.57;
                                excell.WrapText = true;
                                excell.Font.Size = 9;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.NumberFormat = "@";
                                excell.Value2 = Data[i].zvk_date + " №" + Data[i].nzp_zvk;

                                //адрес, Ф.И.О., телефон
                                excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 20.86;
                                excell.Font.Size = 9;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = Data[i].adr + " тел." + Data[i].phone + " " + Data[i].fio;

                                //содержание сообщения
                                excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 22;
                                excell.Font.Size = 9;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = Data[i].comment;

                            }

                            //претензия, услуга
                            excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 17;
                            excell.Font.Size = 9;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = Data[i].service;

                            string field_one = "";

                            if (!String.IsNullOrEmpty(Data[i].replicated))
                            {
                                if (Data[i].replicated == "1")
                                    field_one += "Повторное; ";
                                field_one += "Передан " + Data[i].name_supp + " наряд-заказ " + Data[i].nzp_zk;
                                if (!String.IsNullOrEmpty(Data[i].order_date))
                                {
                                    field_one += " от " + Convert.ToDateTime(Data[i].order_date).ToString("dd.MM.yyyy HH:mm");
                                }

                            }

                            //ЖКУ, № заявления, дата
                            excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 20;
                            excell.WrapText = true;
                            excell.Font.Size = 9;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = field_one;

                            string field_two = "";

                            if (Data[i].nzp_res == "3")
                            {
                                if (!String.IsNullOrEmpty(Data[i].fact_date))
                                    field_two = "Выполнено ";
                                if (!String.IsNullOrEmpty(Data[i].fact_date))
                                {
                                    field_two += Convert.ToDateTime(Data[i].fact_date).ToString("dd.MM.yyyy HH:mm");
                                }
                                if (!String.IsNullOrEmpty(field_two))
                                    field_two += "; ";
                            }
                            else
                            {
                                if (Data[i].nzp_res == "2")
                                {
                                    field_two = "Плановый ремонт";
                                }
                                else
                                {
                                    if (Data[i].nzp_res == "4")
                                    {
                                        field_two = "Отклонено";
                                    }
                                }
                            }

                            if (Data[i].nzp_atts == "3")
                            {
                                field_two += "Не подтверждено жильцом; повторный наряд-заказ " + Data[i].replno;
                                if (!String.IsNullOrEmpty(Data[i].order_date))
                                {
                                    field_two += " от " + Convert.ToDateTime(Data[i].order_date).ToString("dd.MM.yyyy HH:mm");
                                }
                            }
                            else
                            {
                                if (Data[i].nzp_atts == "2")
                                {
                                    field_two += "Подтверждено жильцом ";
                                }
                            }

                            //отметка о выполнении
                            excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 22.3;
                            excell.Font.Size = 9;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = field_two;

                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "G" + indexer.ToString());
                            excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            indexer++;
                        }

                        #endregion
                    }
                    else
                    {
                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("str_filter", info);
                        Dic.Add("str_count", "Количество сообщений: 0 Количество нарядов-заказов: 0");
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("message_list.xls"), Dic);


                        flag = true;
                    }
                }

                #endregion

                //1.3.2
                #region отчет: список сообщений, зарегестрированных ОДДС(опрос)

                if (en == enSrvOper.GetMessageQuestList)
                {
                    if (Data.Count != 0)
                    {
                        #region для шапки

                        //подсчет количества сообщений, заявлений
                        ArrayList message_count = new ArrayList();//количество сообщений
                        ArrayList zakaz_count = new ArrayList();//количество заявлений

                        message_count.Add(Data[0].nzp_zvk);
                        zakaz_count.Add(Data[0].nzp_zk);
                        for (int i = 1; i < Data.Count; i++)
                        {
                            if (!message_count.Contains(Data[i].nzp_zvk))
                            {
                                message_count.Add(Data[i].nzp_zvk);
                            }
                            if (!zakaz_count.Contains(Data[i].nzp_zk))
                            {
                                zakaz_count.Add(Data[i].nzp_zk);
                            }
                        }

                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("str_filter", info);
                        Dic.Add("str_count", "Количество сообщений: " + message_count.Count.ToString() + " Количество нарядов-заказов: " + zakaz_count.Count.ToString());
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("message_quest_list.xls"), Dic);

                        #endregion

                        #region вставка данных

                        indexer = 7;
                        int counter = 0;
                        int zk_counter = 0;
                        int curt_nzp_zvk = 0;
                        for (int i = 0; i < Data.Count; i++)
                        {
                            if (i == 0 || curt_nzp_zvk != Data[i].nzp_zvk)
                            {
                                //номер
                                counter++;
                                excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 3.3;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.Value2 = counter.ToString();

                                //дата, время, № сообщения
                                excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 16.29;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.NumberFormat = "@";
                                excell.Value2 = Data[i].zvk_date + " №" + Data[i].nzp_zvk;

                                //адрес, Ф.И.О., телефон
                                excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 22.3;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = Data[i].adr + " тел." + Data[i].phone + " " + Data[i].fio;

                                //содержание сообщения
                                excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 22.57;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = Data[i].comment;
                            }

                            zk_counter++;
                            //№ п.п. (заявл.)
                            excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 6.43;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.Value2 = zk_counter.ToString();

                            //претензия, услуга
                            excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 17;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = Data[i].service;

                            string field_one = "";

                            if (!String.IsNullOrEmpty(Data[i].replicated))
                            {
                                if (Data[i].replicated == "1")
                                    field_one += "Повторное; ";
                                field_one += "Передан " + Data[i].name_supp + " наряд-заказ " + Data[i].nzp_zk;
                                if (!String.IsNullOrEmpty(Data[i].order_date))
                                {
                                    field_one += " от " + Convert.ToDateTime(Data[i].order_date).ToString("dd.MM.yyyy HH:mm") + ";";
                                }
                            }

                            //ЖКУ, № заявления, дата
                            excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 20;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = field_one;

                            string field_two = "";

                            if (Data[i].nzp_res == "3")
                            {
                                if (!String.IsNullOrEmpty(Data[i].fact_date))
                                {
                                    field_two = "Выполнено ";
                                    if (!String.IsNullOrEmpty(Data[i].fact_date))
                                    {
                                        field_two += Convert.ToDateTime(Data[i].fact_date).ToString("dd.MM.yyyy HH.mm");
                                    }
                                    if (!String.IsNullOrEmpty(field_two))
                                        field_two += ";";
                                }
                            }
                            else
                            {
                                if (Data[i].nzp_res == "2")
                                {
                                    field_two = "Плановый ремонт";
                                }
                                else
                                {
                                    if (Data[i].nzp_res == "4")
                                    {
                                        field_two = "Отклонено";
                                    }
                                }
                            }

                            if (Data[i].nzp_atts == "3")
                            {
                                field_two += "Не подтверждено жильцом; повторный наряд-заказ " + Data[i].replno;
                                if (!String.IsNullOrEmpty(Data[i].order_date))
                                {
                                    field_two += " от " + Convert.ToDateTime(Data[i].order_date).ToString("dd.MM.yyyy HH.mm");
                                }
                                field_two += ";";
                            }
                            else
                            {
                                if (Data[i].nzp_atts == "2")
                                {
                                    field_two += "Подтверждено жильцом ";
                                }
                            }
                            field_two += Data[i].n_comment;

                            //отметка о выполнении
                            excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), Type.Missing);
                            excell.ColumnWidth = 16.9;
                            excell.WrapText = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = field_two;

                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + indexer.ToString());
                            excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            indexer++;
                        }

                        #endregion
                    }
                    else
                    {

                        for (int h = 0; h < Info.Rows.Count; h++)
                        {
                            info += Info.Rows[h][0].ToString().Trim() + ": " + Info.Rows[h][1].ToString().Trim() + "; ";
                        }
                        Dic.Add("str_date", System.DateTime.Today.ToShortDateString());
                        Dic.Add("str_filter", info);
                        Dic.Add("str_count", "Количество сообщений: 0 Количество нарядов-заказов: 0");
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("message_quest_list.xls"), Dic);

                        flag = true;
                    }
                }

                #endregion

                #region нет данных

                if (flag)
                {
                    excell = ExcelL.ExlWs.get_Range("A100", "C100");
                    indexer = 7;
                    //1.1
                    #region отчет: информация, полученная ОДДС

                    if (en == enSrvOper.GetInfoFromService)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "C" + (indexer + 4).ToString());
                    }

                    #endregion

                    //1.2
                    #region отчет: приложение к информации, полученной ОДДС
                    if (en == enSrvOper.GetAppInfoFromService)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "F" + (indexer + 4).ToString());
                    }
                    #endregion

                    //1.4
                    #region отчет: список невыполненных нарядов-заказов к концу

                    if (en == enSrvOper.GetJoborderPeriodOutstand)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (indexer + 4).ToString());
                    }

                    #endregion

                    //2.4
                    #region отчет: количество переадресаций заявок, принятых ОДДС

                    if (en == enSrvOper.GetCountOrderReadres)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (indexer + 4).ToString());
                    }

                    #endregion

                    //1.3.1
                    #region отчет: cписок сообщений, зарегестрированных ОДДС

                    if (en == enSrvOper.GetMessageList)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "G" + (indexer + 4).ToString());
                    }

                    #endregion

                    //1.3.2
                    #region отчет: список сообщений, зарегестрированных ОДДС(опрос)

                    if (en == enSrvOper.GetMessageQuestList)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (indexer + 4).ToString());
                    }

                    #endregion

                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";
                }

                #endregion

                if (ret.result)
                {
                    //1.1
                    #region отчет: информация, полученная ОДДС

                    if (en == enSrvOper.GetInfoFromService)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "info_from_service" + finder.nzp_user) + ".xls";
                    }

                    #endregion

                    //1.2
                    #region отчет: приложение к информации, полученной ОДДС
                    if (en == enSrvOper.GetAppInfoFromService)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "appinfo_from_service" + finder.nzp_user) + ".xls";
                    }
                    #endregion

                    //1.4
                    #region отчет: список невыполненных нарядов-заказов к концу

                    if (en == enSrvOper.GetJoborderPeriodOutstand)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "joborder_period_outstand" + finder.nzp_user) + ".xls";
                    }

                    #endregion

                    //2.4
                    #region отчет: количество переадресаций заявок, принятых ОДДС

                    if (en == enSrvOper.GetCountOrderReadres)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "count_order_readres" + finder.nzp_user) + ".xls";
                    }

                    #endregion

                    //1.3.1
                    #region отчет: cписок сообщений, зарегестрированных ОДДС

                    if (en == enSrvOper.GetMessageList)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "message_list" + finder.nzp_user) + ".xls";
                    }

                    #endregion

                    //1.3.2
                    #region отчет: список сообщений, зарегестрированных ОДДС(опрос)

                    if (en == enSrvOper.GetMessageQuestList)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "message_quest_list" + finder.nzp_user) + ".xls";
                    }

                    #endregion

                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    ExcelL.ExlWb.Close();
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

            }
            finally
            {
                //удаление объекта
                ExcelL.DeleteObject();
            }
            return ret;
        }


        //Отчет: отчет по количеству заявлений, направленных по услугам за период
        public Returns GetCountOrders(Ls finder, string _nzp, string _nzp_add, string s_date, string po_date, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            Supg sg = new Supg();
            System.Data.DataTable DT_Body = sg.GetCountOrders(finder, _nzp, _nzp_add, s_date, po_date, out ret);
            System.Data.DataTable DT_Info = sg.GetReportInfo(finder.nzp_user, out ret);

            Dictionary<string, string> Dic = new Dictionary<string, string>();
            string info = "";
            for (int h = 0; h < DT_Info.Rows.Count; h++)
            {
                info += DT_Info.Rows[h][0].ToString().Trim() + ": " + DT_Info.Rows[h][1].ToString().Trim() + "; ";
            }
            Dic.Add("p1", s_date);
            Dic.Add("p2", po_date);
            Dic.Add("str_filter", info);
            Dic.Add("date", System.DateTime.Today.ToShortDateString());

            int inexec_bf_sum = 0;
            int crt_p_sum = 0;
            int crt_pp_sum = 0;
            int otm_p_sum = 0;
            int fact_p_sum = 0;
            int fact_pp_sum = 0;
            int fact_np_sum = 0;
            int plan_p_sum = 0;

            try
            {
                int indexer = 8;
                bool repsupp = false;
                if (_nzp == "nzp_dest" && _nzp_add == "")
                {
                    ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("cntzserv_z1.xls"), Dic);
                }
                if (_nzp == "nzp_supp")
                {
                    ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("cntzsupp_z1.xls"), Dic);
                    repsupp = true;
                }
                if (_nzp == "nzp_dest" && _nzp_add != "")
                {
                    ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("cntzdest_z1.xls"), Dic);
                }

                string[] ColR = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L" };
                if (DT_Body.Rows.Count != 0)
                {
                    for (int r = 0; r < DT_Body.Rows.Count; r++)
                    {
                        int iC = 0;
                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 25;
                        excell.NumberFormat = "@";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.WrapText = true;
                        excell.Value2 = DT_Body.Rows[r][0].ToString().Trim();

                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = DT_Body.Rows[r][1];
                        inexec_bf_sum += Convert.ToInt32(DT_Body.Rows[r][1]);

                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = DT_Body.Rows[r][2];
                        crt_p_sum += Convert.ToInt32(DT_Body.Rows[r][2]);

                        if (!repsupp)
                        {
                            excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                            excell.ColumnWidth = 8;
                            excell.NumberFormat = "0";
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = DT_Body.Rows[r][3];
                            crt_pp_sum += Convert.ToInt32(DT_Body.Rows[r][3]);
                        }


                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = DT_Body.Rows[r][4];
                        otm_p_sum += Convert.ToInt32(DT_Body.Rows[r][4]);



                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = DT_Body.Rows[r][8];
                        plan_p_sum += Convert.ToInt32(DT_Body.Rows[r][8]);




                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = DT_Body.Rows[r][5];
                        fact_p_sum += Convert.ToInt32(DT_Body.Rows[r][5]);

                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = DT_Body.Rows[r][6];
                        fact_np_sum += Convert.ToInt32(DT_Body.Rows[r][6]);

                        if (!repsupp)
                        {
                            excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                            excell.ColumnWidth = 8;
                            excell.NumberFormat = "0";
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = Convert.ToInt32(DT_Body.Rows[r][5]) - Convert.ToInt32(DT_Body.Rows[r][6]) - Convert.ToInt32(DT_Body.Rows[r][7]);

                            excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                            excell.ColumnWidth = 8;
                            excell.NumberFormat = "0";
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excell.Value2 = DT_Body.Rows[r][7];
                            fact_pp_sum += Convert.ToInt32(DT_Body.Rows[r][7]);
                        }

                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        if ((Convert.ToInt32(DT_Body.Rows[r][1]) + Convert.ToInt32(DT_Body.Rows[r][2]) - Convert.ToInt32(DT_Body.Rows[r][4]) + Convert.ToInt32(DT_Body.Rows[r][7]) - Convert.ToInt32(DT_Body.Rows[r][8])) != 0)
                        {
                            excell.Value2 = Convert.ToInt32(DT_Body.Rows[r][5]) * 100 /
                                (Convert.ToInt32(DT_Body.Rows[r][1]) + Convert.ToInt32(DT_Body.Rows[r][2]) - Convert.ToInt32(DT_Body.Rows[r][4]) + Convert.ToInt32(DT_Body.Rows[r][7]) - Convert.ToInt32(DT_Body.Rows[r][8]));
                        }
                        else
                        {
                            excell.Value2 = 0;
                        }


                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + r).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = Convert.ToInt32(DT_Body.Rows[r][1]) + Convert.ToInt32(DT_Body.Rows[r][2]) - Convert.ToInt32(DT_Body.Rows[r][4]) - Convert.ToInt32(DT_Body.Rows[r][5]) - Convert.ToInt32(DT_Body.Rows[r][8]);

                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + r).ToString(), ColR[iC - 1] + (indexer + r).ToString());
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    }
                }
                else
                {
                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "L" + (indexer + 4).ToString());
                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";

                }

                if (DT_Body.Rows.Count != 0)
                {

                    #region Итого

                    int iC = 0;
                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 25;
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.WrapText = true;
                    excell.Value2 = "Итого";

                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 8;
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    excell.Value2 = inexec_bf_sum;

                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 8;
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    excell.Value2 = crt_p_sum;

                    if (!repsupp)
                    {
                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = crt_pp_sum;
                    }

                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 8;
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    excell.Value2 = otm_p_sum;


                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 8;
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    excell.Value2 = plan_p_sum;

                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 8;
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    excell.Value2 = fact_p_sum;

                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 8;
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    excell.Value2 = fact_np_sum;

                    if (!repsupp)
                    {
                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = fact_p_sum - fact_np_sum - fact_pp_sum;

                        excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                        excell.ColumnWidth = 8;
                        excell.NumberFormat = "0";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excell.Value2 = fact_pp_sum;
                    }

                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 8;
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    if ((inexec_bf_sum + crt_p_sum - otm_p_sum + fact_np_sum - plan_p_sum) != 0)
                    {
                        excell.Value2 = fact_p_sum * 100 / (inexec_bf_sum + crt_p_sum - otm_p_sum + fact_pp_sum - plan_p_sum);
                    }
                    else
                    {
                        excell.Value2 = 0;
                    }

                    excell = ExcelL.ExlWs.get_Range(ColR[iC++] + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.ColumnWidth = 8;
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    excell.Value2 = inexec_bf_sum + crt_p_sum - otm_p_sum - fact_p_sum - plan_p_sum;

                    excell = ExcelL.ExlWs.get_Range(ColR[0] + (indexer + DT_Body.Rows.Count).ToString(), ColR[iC - 1] + (indexer + DT_Body.Rows.Count).ToString());
                    excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Bold = true;

                    #endregion
                }
                if (ret.result)
                {


                    if (_nzp == "nzp_dest" && _nzp_add == "")
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "cntzserv_z1" + finder.nzp_user) + ".xls";
                    }
                    if (_nzp == "nzp_supp")
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "cntzsupp_z1" + finder.nzp_user) + ".xls";
                    }
                    if (_nzp == "nzp_dest" && _nzp_add != "")
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "cntzdest_z1" + finder.nzp_user) + ".xls";
                    }
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    ExcelL.ExlWb.Close();
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

            }
            finally
            {
                //удаление объекта
                ExcelL.DeleteObject();
            }
            return ret;
        }


        /// <summary>
        /// список нарядов-заказов для выполнения
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <returns></returns>
        public Returns GetIncomingJobOrders(int nzp_user, ref string fileName)
        {
            Supg sg = new Supg();
            Returns ret = Utils.InitReturns();
            System.Data.DataTable DT_Body = sg.GetIncomingJobOrders(nzp_user, out ret);
            System.Data.DataTable DT_Info = sg.GetReportInfo(nzp_user, out ret);
            ArrayList name_supp = new ArrayList();//подрядчики

            #region узнаем подрядчиков

            if (DT_Body != null)
            {
                string temp_name_supp = "";
                string current_name_supp = DT_Body.Rows[0][6].ToString();

                name_supp.Add(current_name_supp);
                for (int i = 1; i < DT_Body.Rows.Count / 2; i++)
                {
                    temp_name_supp = DT_Body.Rows[i * 2][6].ToString();

                    if (!name_supp.Contains(temp_name_supp))
                        name_supp.Add(temp_name_supp);
                }
            }

            #endregion


            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            Microsoft.Office.Interop.Excel.Range excell_template = null;//для шапок других страниц


            try
            {
                if (DT_Body.Rows.Count != 0)
                {
                    for (int k = 0; k < name_supp.Count; k++)
                    {
                        //для каждой страницы отдельно
                        ExcelL.GetWs(k + 1, name_supp[k].ToString().Trim());

                        if (k == 0)
                        {
                            Dictionary<string, string> Dic = new Dictionary<string, string>();
                            string info = "";
                            for (int h = 0; h < DT_Info.Rows.Count; h++)
                            {
                                info += DT_Info.Rows[h][0].ToString().Trim() + ": " + DT_Info.Rows[h][1].ToString().Trim() + "; ";
                            }
                            Dic.Add("_DateTime", "Дата печати: " + System.DateTime.Today.ToShortDateString());
                            Dic.Add("name_supp", name_supp[k].ToString());
                            Dic.Add("str_filter", info);
                            ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("listzakaz_3.xls"), Dic);
                            string nameSupp = name_supp[k].ToString().Trim();
                            ExcelL.CheckWsName(ref nameSupp);
                            ExcelL.ExlWs.Name = nameSupp;  
                            //шапка для других страниц отчета
                            excell_template = ExcelL.ExlWs.get_Range("A1", "G9");
                        }
                        else
                        {
                            excell = ExcelL.ExlWs.get_Range("A1", "G9");
                            excell_template.Copy(Type.Missing);
                            //вставка шапки
                            excell.PasteSpecial(XlPasteType.xlPasteAll, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);
                            excell = ExcelL.ExlWs.get_Range("A4", "G5");
                            excell.Value2 = "Наименование подрядчика: " + name_supp[k].ToString();
                        }

                        #region вставка данных

                        string temp_supp = "";
                        int number = 0;
                        int indexer = 10;//первая строка
                        int counter = 0;

                        for (int i = 0; i < DT_Body.Rows.Count / 2; i++)
                        {
                            temp_supp = DT_Body.Rows[number][6].ToString();//подрядчик текущей строки

                            if (name_supp[k].ToString().Trim() == temp_supp.Trim())
                            {
                                counter++;
                                excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "A" + (indexer + 2).ToString());
                                excell.Merge(Type.Missing);
                                excell.ColumnWidth = 5;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = counter.ToString();


                                excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), "B" + (indexer + 2).ToString());
                                excell.Merge(Type.Missing);
                                excell.ColumnWidth = 11;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.NumberFormat = "@";
                                excell.Value2 = DT_Body.Rows[number][0].ToString().Trim().Replace("#", "№");

                                excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), "C" + (indexer + 2).ToString());
                                excell.Merge(Type.Missing);
                                excell.ColumnWidth = 21;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.NumberFormat = "@";
                                excell.Value2 = DT_Body.Rows[number][1].ToString().Trim();

                                excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), "D" + (indexer + 2).ToString());
                                excell.Merge(Type.Missing);
                                excell.ColumnWidth = 25;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = DT_Body.Rows[number][2].ToString().Trim();


                                excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                                excell.WrapText = true;
                                excell.ColumnWidth = 33;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = DT_Body.Rows[number][3].ToString().Trim();


                                excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 20;
                                excell.NumberFormat = "@";
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = DT_Body.Rows[number][4].ToString().Trim();

                                excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), "G" + (indexer + 2).ToString());
                                excell.Merge(Type.Missing);
                                excell.WrapText = true;
                                excell.ColumnWidth = 20;
                                excell.NumberFormat = "@";
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = DT_Body.Rows[number][5].ToString().Trim();

                                excell = ExcelL.ExlWs.get_Range("E" + (indexer + 1).ToString(), Type.Missing);
                                excell.ColumnWidth = 33;
                                excell.RowHeight = 29;
                                excell.NumberFormat = "@";
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = DT_Body.Rows[number + 1][3].ToString().Trim();

                                excell = ExcelL.ExlWs.get_Range("E" + (indexer + 2).ToString(), "F" + (indexer + 2).ToString());
                                excell.Merge(Type.Missing);
                                excell.Font.Size = 7;
                                excell.RowHeight = 9;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.Value2 = "(заполняется вручную)";

                                excell = ExcelL.ExlWs.get_Range("F" + (indexer + 1).ToString(), Type.Missing);
                                excell.ColumnWidth = 20;
                                excell.NumberFormat = "@";
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.Value2 = DT_Body.Rows[number + 1][4].ToString().Trim();

                                excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "G" + (indexer + 2).ToString());
                                excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                                indexer = indexer + 3;
                            }
                            number = number + 2;
                        }

                        #endregion
                    }
                }
                else
                {
                    excell = ExcelL.ExlWs.get_Range("A10", "G18");
                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";

                }

                //сортируем страницы книги по названию
                ExcelL.SortWs();

                if (ret.result)
                {
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "listzakaz_3" + nzp_user) + ".xls";
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    ExcelL.ExlWb.Close();
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            //удаление объекта
            ExcelL.DeleteObject();
            return ret;
        }

        /// <summary>
        /// список недопоставок
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <returns></returns>
        public Returns GetRepNedopList(int nzp_user, int nzp_jrn, ref string fileName)
        {
            Supg sg = new Supg();
            DbNedop nedop = new DbNedop();
            Returns ret = Utils.InitReturns();

            List<NedopInfo> Body = nedop.GetRepNedopList(nzp_user, nzp_jrn, out ret);
            List<Journal> lj = sg.GetJournal(new Journal() { number = nzp_jrn }, out ret);

            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;

            try
            {
                if (Body.Count != 0)
                {
                    ArrayList name_serv = new ArrayList();//услуги
                    Microsoft.Office.Interop.Excel.Range excell_template = null;//для шапок других страниц

                    #region Список услуг

                    name_serv.Add(Body[0].name_serv.Trim());
                    for (int i = 1; i < Body.Count; i++)
                    {
                        if (!name_serv.Contains(Body[i].name_serv.Trim()))
                        {
                            name_serv.Add(Body[i].name_serv.Trim());
                        }
                    }

                    #endregion

                    for (int k = 0; k < name_serv.Count; k++)
                    {
                        #region страницы
                        ExcelL.GetWs(k + 1, name_serv[k].ToString().Trim());

                        if (k == 0)
                        {
                            string info = "";

                            if (lj.Count != 0)
                            {
                                int h = 0;
                                if (lj[h].is_actual != 0)
                                {
                                    if (lj[h].is_actual == 2) info += " Источник недопоставки: План.(авар.,пр.) работы;";
                                    if (lj[h].is_actual == 1) info += " Источник недопоставки: наряды-заказы;";
                                }
                                if (lj[h].doc_begin != "") info += " Начало периода рег.док-та: " + lj[h].doc_begin + ";";
                                if (lj[h].doc_end != "") info += " Конец периода рег.док-та: " + lj[h].doc_end + ";";
                                if (lj[h].d_begin != "") info += " Начало недопоставки: " + lj[h].d_begin + ";";
                                if (lj[h].d_end != "") info += " Конец недопоставки: " + lj[h].d_end + ";";
                                if (lj[h].d_when != "") info += " Дата формирования данных о недопоставке: " + lj[h].d_when + ";";
                            }
                            Dictionary<string, string> Dic = new Dictionary<string, string>();
                            Dic.Add("str_date", "Дата печати: " + System.DateTime.Today.ToShortDateString());
                            Dic.Add("name_serv", name_serv[k].ToString());
                            Dic.Add("str_filter", info);
                            ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("upg_nedoplist.xls"), Dic);
                            ExcelL.ExlWs.Name = name_serv[k].ToString().Trim();
                            //шапка для других страниц отчета
                            excell_template = ExcelL.ExlWs.get_Range("A1", "F5");
                        }
                        else
                        {
                            excell = ExcelL.ExlWs.get_Range("A1", "F5");
                            excell_template.Copy(Type.Missing);
                            //вставка шапки
                            excell.PasteSpecial(XlPasteType.xlPasteAll, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);
                            excell = ExcelL.ExlWs.get_Range("A4", "F4");
                            excell.Value2 = name_serv[k].ToString();
                        }
                        #endregion

                        #region вставка данных

                        int counter = 0;
                        int indexer = 6;

                        for (int i = 0; i < Body.Count; i++)
                        {
                            if (name_serv[k].ToString().Trim() == Body[i].name_serv.Trim())
                            {
                                //номер
                                counter++;
                                excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 5.3;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excell.Value2 = counter.ToString();

                                //Номер документа
                                excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 25.0;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.NumberFormat = "@";
                                if (Body[i].act_date != null && !String.IsNullOrEmpty(Body[i].act_date.ToString()))
                                {
                                    if (Body[i].ctp == "1")
                                    {
                                        excell.Value2 = "Н-заказ №" + Body[i].number + " от " + Body[i].act_date;
                                    }
                                    else
                                    {
                                        excell.Value2 = "Акт №" + Body[i].number + " от " + Body[i].act_date;
                                    }
                                }

                                //Тип недопоставки
                                excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 30.4;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.NumberFormat = "@";
                                excell.Value2 = Body[i].name_nedop;

                                //дата, время отключения
                                excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 15.4;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.NumberFormat = "@";
                                excell.Value2 = Body[i].disconnect_date;

                                //дата, время подключения
                                excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), Type.Missing);
                                excell.ColumnWidth = 15.4;
                                excell.WrapText = true;
                                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                excell.NumberFormat = "@";
                                excell.Value2 = Body[i].connect_date;

                                for (int l = 0; l < Body[i].geu_list.Count; l++)
                                {
                                    //список адресов
                                    excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), Type.Missing);
                                    excell.ColumnWidth = 35.3;
                                    excell.WrapText = true;
                                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                                    excell.NumberFormat = "@";
                                    excell.Value2 = Body[i].address_list[l].ToString();

                                    indexer++;
                                }

                                //объединяем поля: номер, характер аварии, обе даты и ответственный
                                excell = ExcelL.ExlWs.get_Range("A" + (indexer - Body[i].geu_list.Count).ToString(), "A" + (indexer - 1).ToString());
                                excell.Merge(Type.Missing);
                                excell = ExcelL.ExlWs.get_Range("B" + (indexer - Body[i].geu_list.Count).ToString(), "B" + (indexer - 1).ToString());
                                excell.Merge(Type.Missing);
                                excell = ExcelL.ExlWs.get_Range("C" + (indexer - Body[i].geu_list.Count).ToString(), "C" + (indexer - 1).ToString());
                                excell.Merge(Type.Missing);
                                excell = ExcelL.ExlWs.get_Range("E" + (indexer - Body[i].geu_list.Count).ToString(), "E" + (indexer - 1).ToString());
                                excell.Merge(Type.Missing);
                                excell = ExcelL.ExlWs.get_Range("F" + (indexer - Body[i].geu_list.Count).ToString(), "F" + (indexer - 1).ToString());
                                excell.Merge(Type.Missing);

                                excell = ExcelL.ExlWs.get_Range("A" + (indexer - Body[i].geu_list.Count).ToString(), "F" + (indexer - 1).ToString());
                                excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            }
                        }

                        #endregion
                    }

                    //сортируем страницы книги по названию
                    ExcelL.SortWs();
                }
                else
                {
                    excell = ExcelL.ExlWs.get_Range("A10", "G18");
                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";
                }

                fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "upg_nedop" + nzp_user) + ".xls";
                ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                ExcelL.ExlWb.Close();
                if (InputOutput.useFtp)
                {
                    fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            //удаление объекта
            ExcelL.DeleteObject();
            return ret;
        }

        //Отчет: оплата гражданами-получателями коммунальных услуг за поставленные услуги
        public Returns GetDeliveredServicesPayment(Ls finder, int nzp_supp, string yy, string mm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            DBCalcs calc = new DBCalcs();
            System.Data.DataTable data = calc.DeliveredServicesPayment(finder, nzp_supp, yy, mm, out ret);

            #region Определение заголовков и типов

            Dictionary<string, string> Dic = new Dictionary<string, string>();

            int month_count = System.DateTime.DaysInMonth(Convert.ToInt32("20" + yy), Convert.ToInt32(mm));

            string strMonthName = "";

            #region Получаем название месяца

            switch (Convert.ToInt32(mm))
            {
                case 1:
                    strMonthName = "Январь";
                    break;
                case 2:
                    strMonthName = "Февраль";
                    break;
                case 3:
                    strMonthName = "Март";
                    break;
                case 4:
                    strMonthName = "Апрель";
                    break;
                case 5:
                    strMonthName = "Май";
                    break;
                case 6:
                    strMonthName = "Июнь";
                    break;
                case 7:
                    strMonthName = "Июль";
                    break;
                case 8:
                    strMonthName = "Август";
                    break;
                case 9:
                    strMonthName = "Сентябрь";
                    break;
                case 10:
                    strMonthName = "Октябрь";
                    break;
                case 11:
                    strMonthName = "Ноябрь";
                    break;
                case 12:
                    strMonthName = "Декабрь";
                    break;
                default:
                    break;
            }
            #endregion


            Dic.Add("tdat", "   " + month_count.ToString() + "." + String.Format("{0:00}", Convert.ToInt32(mm)) + ".20" + yy + " г.");
            Dic.Add("date1", "01." + String.Format("{0:00}", Convert.ToInt32(mm)) + ".20" + yy + "г.");
            Dic.Add("date2", strMonthName + " 20" + yy + " г.");
            Dic.Add("date3", month_count.ToString() + "." + String.Format("{0:00}", Convert.ToInt32(mm)) + ".20" + yy + " г.");
            Dic.Add("date4", "01.01." + "20" + yy);

            #endregion

            try
            {
                int indexer = 11;

                ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("oplata_za_uslugi.xls"), Dic);

                if (data.Rows.Count != 0)
                {
                    #region Создание тела


                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        for (int j = 0; j < 15; j++)
                        {
                            excell = ExcelL.ExlWs.get_Range(ExcelFor.BukvaList[j + 1].ToString() + (indexer + i).ToString(), Type.Missing);
                            if (j >= 1)
                            {
                                excell.Value2 = data.Rows[i][j + 1];
                            }
                            else
                            {
                                excell.Value2 = data.Rows[i][j];
                            }
                        }
                    }

                    int number = 1;
                    string org = "";
                    for (int k = 0; k < data.Rows.Count; k++)
                    {
                        if (k == 0)
                        {
                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), Type.Missing);
                            excell.Value2 = number;
                            excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), Type.Missing);
                            org = excell.Value2.ToString().Trim();
                            excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "P" + indexer.ToString());
                            excell.Font.Bold = true;
                            excell.RowHeight = 32.25;
                        }
                        else
                        {
                            excell = ExcelL.ExlWs.get_Range("B" + (indexer + k).ToString(), Type.Missing);
                            if (org == excell.Value2.ToString().Trim())
                            {
                                excell.Clear();
                                excell.RowHeight = 32.25;
                            }
                            else
                            {
                                number++;
                                excell = ExcelL.ExlWs.get_Range("A" + (indexer + k).ToString(), Type.Missing);
                                excell.Value2 = number;
                                excell = ExcelL.ExlWs.get_Range("B" + (indexer + k).ToString(), Type.Missing);
                                org = excell.Value2.ToString().Trim();
                                excell = ExcelL.ExlWs.get_Range("A" + (indexer + k).ToString(), "P" + (indexer + k).ToString());
                                excell.Font.Bold = true;
                                excell.RowHeight = 32.25;
                            }
                        }
                    }

                    #region Выравнивание + шрифт

                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "A" + (indexer + data.Rows.Count).ToString());
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), "C" + (indexer + data.Rows.Count).ToString());
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.WrapText = true;
                    excell.NumberFormat = "@";


                    excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), "P" + (indexer + data.Rows.Count).ToString());
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.NumberFormat = "0,00";

                    excell = ExcelL.ExlWs.get_Range("A" + (indexer - 1).ToString(), "P" + (indexer + data.Rows.Count - 1).ToString());
                    excell.Font.Size = 7;
                    excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    #endregion

                    #endregion
                }
                else
                {
                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "P" + (indexer + 4).ToString());
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";

                }
                #region Сохранение


                if (ret.result)
                {
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "oplata_za_uslugi" + finder.nzp_user) + ".xls";//"SpLs_" + nzp_user + ".xlsx";
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

            }
            finally
            {
                //удаление объекта
                ExcelL.DeleteObject();
            }
            return ret;
                #endregion
        }

        //Отчет: сверка расчетов с жильцом по состоянию
        public Returns GetStateGilFond(string yy_from, string mm_from, string yy_to, string mm_to, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            //Microsoft.Office.Interop.Excel.Range excell;
            DBCalcs calc = new DBCalcs();
            System.Data.DataTable DT_Body = calc.GetStateGilFond(yy_from, mm_from, yy_to, mm_to, out ret);

            return ret;
        }

        
     
        public void GetFakeAnalisKart(int year, int month, int nzpUser, ref string fileName, int nzpExcelUtility)
        {
            IDbConnection conn_db = DBManager.GetConnection(Global.Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            Returns ret = DBManager.OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return;
            }
            var ExcelL = new IFMX.Report.SOURCE.SamaraReport.ExcelNewLoader();
            try
            {
                var samaraGroupCalcReport = new IFMX.Report.SOURCE.Samara.SamaraGroupCalcReport(conn_db,
                    year, month, nzpUser);
                samaraGroupCalcReport.PrepareTempTable();


                //Карточка Аналитического учета
                var samaraKartSaldo = new IFMX.Report.SOURCE.SamaraReport.SamaraKartSaldo(conn_db, ExcelL, month, year);
               List<string> fileList = samaraKartSaldo.GetReport(nzpExcelUtility);
                if (fileList.Count > 0) fileName = fileList[0];

                samaraGroupCalcReport.DropTempTable(false);
            }
            catch (Exception e)
            {

                MonitorLog.WriteLog("ExcelReport : Ошибка cоздания пакетного отчета " + e.Message,
                    MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_db.Close();
                ExcelL.DeleteObject();

            }

        }

        public Returns GetAnalisKart(List<Prm> listprm, string Nzp_user, ref string fileName, int nzpExcelUtility)
        {
            Returns ret = Utils.InitReturns();
            //GetFakeAnalisKart(listprm[0].year_, listprm[0].month_, 
            //     Convert.ToInt32(Nzp_user), ref fileName, nzpExcelUtility);
            //return ret;

            ExcelLoader ExcelL = null;

            try
            {
                ExcelL = new ExcelLoader();
                ExcelL.ExlWs.Rows.Font.Name = "Arial";
                //создание dataTable
                ExcelRep ExR = new ExcelRep();
                System.Data.DataTable DT = ExR.GetAnalisKartTable(listprm, out ret, Nzp_user);

                if (DT != null)
                {



                    #region Создаем шапку

                    //Microsoft.Office.Interop.Excel.Range excells1;

                    ////спустили шапку
                    //excells1 = ExcelL.ExlWs.get_Range("A1", "K1");
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "Карта аналитического учета";
                    //excells1.Font.Bold = true;
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    //excells1 = ExcelL.ExlWs.get_Range("A2", "K2");
                    //excells1.Merge(Type.Missing);
                    //excells1.Font.Bold = true;
                    //string ERC_name = "";

                    //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
                    //    ERC_name = "Н-ска";
                    //else
                    //    ERC_name = "Самары";

                    //excells1.FormulaR1C1 = "по МП городского округа " + ERC_name + " ''ЕИРЦ'' за " +
                    //    listprm[0].month_.ToString("00") + " " + listprm[0].year_.ToString() + "г.";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    //excells1 = ExcelL.ExlWs.get_Range("A3", "A3");
                    //excells1.Merge(Type.Missing);
                    //excells1.Font.Bold = true;
                    //excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                    //excells1 = ExcelL.ExlWs.get_Range("B3", "K4");
                    //excells1.Font.Bold = true;
                    //excells1.Merge(Type.Missing);
                    ////                    excells1.FormulaR1C1 = "Управляющая организация:" + listprm[0].area;
                    //excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    //excells1.RowHeight = 23;


                    ///*
                    //    excells1 = ExcelL.ExlWs.get_Range("B4", "K4");
                    //    excells1.Font.Bold = true;
                    //    excells1.Merge(Type.Missing);
                    //    excells1.FormulaR1C1 = "Отделение:" + listprm[0].geu;
                    //    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    //*/

                    //excells1 = ExcelL.ExlWs.get_Range("A5", "A6");
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "Вид услуги:";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;
                    //excells1.ColumnWidth = 36.43;

                    //excells1 = ExcelL.ExlWs.get_Range("B5", "B6");
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "Сальдо 01." + listprm[0].month_.ToString("00") + "." + listprm[0].year_.ToString() + " г.";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;
                    //excells1.WrapText = true;
                    //excells1.ColumnWidth = 11.14;


                    //excells1 = ExcelL.ExlWs.get_Range("C5", "H5");
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "Начислено";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    //excells1 = ExcelL.ExlWs.get_Range("C6", "C6");
                    //excells1.FormulaR1C1 = "постоянно";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    //excells1 = ExcelL.ExlWs.get_Range("D6", "D6");
                    //excells1.FormulaR1C1 = "кор-ка " + System.Convert.ToChar(10) + "тарифа " + System.Convert.ToChar(10) + "(МОП)";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    //excells1 = ExcelL.ExlWs.get_Range("E6", "E6");
                    //excells1.FormulaR1C1 = "возврат";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    //excells1 = ExcelL.ExlWs.get_Range("F6", "F6");
                    //excells1.FormulaR1C1 = "красные";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;


                    //excells1 = ExcelL.ExlWs.get_Range("G6", "G6");
                    //excells1.FormulaR1C1 = "черные";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    //excells1 = ExcelL.ExlWs.get_Range("H6", "H6");
                    //excells1.FormulaR1C1 = "итого";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    //excells1 = ExcelL.ExlWs.get_Range("I5", "I6");
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "к оплате";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    //excells1 = ExcelL.ExlWs.get_Range("J5", "J6");
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "оплачено";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    //excells1 = ExcelL.ExlWs.get_Range("K5", "K6");
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "Сальдо " +
                    //    System.DateTime.DaysInMonth(System.Convert.ToInt32(listprm[0].year_),
                    //    System.Convert.ToInt32(listprm[0].month_)).ToString() +
                    //    "." + listprm[0].month_.ToString("00") + "." + listprm[0].year_.ToString() + " г.";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.WrapText = true;
                    //excells1.ColumnWidth = 11.86;
                    //excells1.Font.Size = 8;
                    //excells1.Font.Bold = true;

                    #endregion

                    #region Пишем тело

                    //decimal[] sum_colls = new decimal[DT.Columns.Count];

                    //for (int i = 0; i < DT.Columns.Count; i++)
                    //    sum_colls[i] = 0;

                    //Decimal dl;

                    //#region первая страница

                    //int excelRow = 7;
                    //DataRow[] dr3 = DT.Select("tip = 1");
                    //foreach(DataRow dr in dr3)
                    //{
                    //    if ((int)dr["nzp_serv"] == 17)
                    //    {
                    //        ExcelL.ExlWs.Cells[excelRow, 1] = dr["service"].ToString().Trim();
                    //        ExcelL.ExlWs.Cells[excelRow, 3] = dr["rsum_tarif"];
                    //        excelRow++;
                    //        ExcelL.ExlWs.Cells[excelRow, 1] = "ОДН";
                    //        excells1 = ExcelL.ExlWs.get_Range("B" + excelRow.ToString(), "B" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["sum_insaldo"];
                    //        ExcelL.ExlWs.Cells[excelRow, 3] = dr["sum_odn"];

                    //        excells1 = ExcelL.ExlWs.get_Range("D" + excelRow.ToString(), "D" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["izm_tarif"];

                    //        excells1 = ExcelL.ExlWs.get_Range("E" + excelRow.ToString(), "E" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["vozv"];

                    //        excells1 = ExcelL.ExlWs.get_Range("F" + excelRow.ToString(), "F" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["reval_k"];

                    //        excells1 = ExcelL.ExlWs.get_Range("G" + excelRow.ToString(), "G" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["reval_d"];

                    //        excells1 = ExcelL.ExlWs.get_Range("H" + excelRow.ToString(), "H" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["sum_ito"];

                    //        excells1 = ExcelL.ExlWs.get_Range("I" + excelRow.ToString(), "I" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["sum_charge"];


                    //        excells1 = ExcelL.ExlWs.get_Range("J" + excelRow.ToString(), "J" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["sum_money"];

                    //        excells1 = ExcelL.ExlWs.get_Range("K" + excelRow.ToString(), "K" + (excelRow - 1).ToString());
                    //        excells1.Merge(Type.Missing);
                    //        excells1.FormulaR1C1 = dr["sum_outsaldo"];

                    //    }
                    //    else
                    //    {
                    //        ExcelL.ExlWs.Cells[excelRow, 1] = dr["service"].ToString().Trim();
                    //        ExcelL.ExlWs.Cells[excelRow, 2] = dr["sum_insaldo"];
                    //        ExcelL.ExlWs.Cells[excelRow, 3] = dr["rsum_tarif"];
                    //        ExcelL.ExlWs.Cells[excelRow, 4] = dr["izm_tarif"];
                    //        ExcelL.ExlWs.Cells[excelRow, 5] = dr["vozv"];
                    //        ExcelL.ExlWs.Cells[excelRow, 6] = dr["reval_k"];
                    //        ExcelL.ExlWs.Cells[excelRow, 7] = dr["reval_d"];
                    //        ExcelL.ExlWs.Cells[excelRow, 8] = dr["sum_ito"];
                    //        ExcelL.ExlWs.Cells[excelRow, 9] = dr["sum_charge"];
                    //        ExcelL.ExlWs.Cells[excelRow, 10] = dr["sum_money"];
                    //        ExcelL.ExlWs.Cells[excelRow, 11] = dr["sum_outsaldo"];
                    //    }
                    //    excelRow++;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["sum_insaldo"].ToString(), out dl)) sum_colls[1] = sum_colls[1] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["rsum_tarif"].ToString(), out dl)) sum_colls[2] = sum_colls[2] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["sum_odn"].ToString(), out dl)) sum_colls[2] = sum_colls[2] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["izm_tarif"].ToString(), out dl)) sum_colls[3] = sum_colls[3] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["vozv"].ToString(), out dl)) sum_colls[4] = sum_colls[4] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["reval_k"].ToString(), out dl)) sum_colls[5] = sum_colls[5] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["reval_d"].ToString(), out dl)) sum_colls[6] = sum_colls[6] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["sum_ito"].ToString(), out dl)) sum_colls[7] = sum_colls[7] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["sum_charge"].ToString(), out dl)) sum_colls[8] = sum_colls[8] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["sum_money"].ToString(), out dl)) sum_colls[9] = sum_colls[9] + dl;
                    //    dl = 0;
                    //    if (Decimal.TryParse(dr["sum_outsaldo"].ToString(), out dl)) sum_colls[10] = sum_colls[10] + dl;
                    //    dl = 0;
                    //}



                    //#region Пишем подвал

                    //int FooterRow = excelRow;


                    //ExcelL.ExlWs.Cells[FooterRow, 1] = "Итого";

                    //for (int i = 1; i < DT.Columns.Count-2; i++)
                    //{
                    //    ExcelL.ExlWs.Cells[FooterRow, i + 1] = sum_colls[i];
                    //}

                    //string[] colarray = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
                    //foreach (string ch in colarray)
                    //{
                    //    excells1 = ExcelL.ExlWs.get_Range(ch + (FooterRow).ToString(), ch + (FooterRow).ToString());
                    //    if (ch != "A")
                    //    {
                    //        excells1.EntireColumn.NumberFormat = "# ##0,00";
                    //    }
                    //    excells1.Font.Bold = true;
                    //    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    //    excells1.EntireColumn.Font.Size = 10;
                    //    if ((ch != "A") & (ch != "B") & (ch != "K"))
                    //        excells1.EntireColumn.AutoFit();

                    //    excells1 = ExcelL.ExlWs.get_Range(ch + "7", ch + (FooterRow).ToString());
                    //    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //}


                    //SetReportSing(ExcelL, FooterRow + 3);




                    //ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlLandscape;


                    //#endregion
                    //#endregion


                    #endregion


                    DataRow[] drSelect = DT.Select("tip = 1");

                    MakeListKartAnalis(drSelect, ExcelL, 1, listprm[0], Nzp_user);

                    drSelect = DT.Select("tip = 2");

                    string old_geu = "";
                    int j = 2;
                    foreach (DataRow dr in drSelect)
                    {
                        if (old_geu != dr["geu"].ToString())
                        {
                            old_geu = dr["geu"].ToString();
                            DataRow[] drSelect2 = DT.Select("geu = '" + old_geu + "'");
                            MakeListKartAnalis(drSelect2, ExcelL, j, listprm[0], Nzp_user);
                            j++;
                        }
                    }

                    ExcelL.SortWs();

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "KartAnalis_" +
                            listprm[0].nzp_area.ToString() + "_" +
                            listprm[0].nzp_geu.ToString() + "_" +
                            listprm[0].year_.ToString() + "_" +
                            listprm[0].month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        ExcelL.ExlWb.Close();
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = ex.Message;
                    }
                    finally
                    {
                        ////удаление объекта
                        //ExcelL.DeleteObject();
                    }
                    #endregion

                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetAnalisKartTable DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }

     
        public Returns GetCharges(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            ExcelLoader ExcelL = null;

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetCharges(prm_, out ret, Nzp_user);

            try
            {
                ExcelL = new ExcelLoader();
                ExcelL.ExlWs.Rows.Font.Name = "Arial";

                if (DT != null)
                {
                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;
                    string rep_name = "Начисления и оплаты по поставщикам";

                    //спустили шапку
                    excells1 = ExcelL.ExlWs.get_Range("A1", "E1");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = rep_name;
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Font.Size = 8;

                    excells1 = ExcelL.ExlWs.get_Range("A2", "E2");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    string s = " за " + prm_.dat_calc;
                    excells1.FormulaR1C1 = s;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Font.Size = 8;

                    excells1 = ExcelL.ExlWs.get_Range("A4", "A6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "№";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 5;
                    excells1.Font.Size = 8;

                    excells1 = ExcelL.ExlWs.get_Range("B4", "B6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Поставщик";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 37;
                    excells1.Font.Size = 8;

                    excells1 = ExcelL.ExlWs.get_Range("C4", "C6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Начислено за месяц";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 11;

                    excells1 = ExcelL.ExlWs.get_Range("D4", "D6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Начислено к оплате";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 11;
                    excells1.Font.Size = 8;

                    excells1 = ExcelL.ExlWs.get_Range("E4", "E6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Оплата в";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 11;
                    excells1.Font.Size = 8;
                    #endregion

                    if (DT.Rows.Count > 0)
                    {
                        #region Пишем тело

                        int ExcelRow = 7;
                        int i = 0;

                        decimal isum_real = 0;
                        decimal isum_charge = 0;
                        decimal isum_money = 0;
                        string old_typek = "";
                        int row = 0;
                        while (i < DT.Rows.Count)
                        {
                            #region Итого
                            if (old_typek != DT.Rows[i]["typek"].ToString().Trim())
                            {
                                old_typek = DT.Rows[i]["typek"].ToString().Trim();
                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 2], ExcelL.ExlWs.Cells[ExcelRow, 2]);
                                excells1.FormulaR1C1 = old_typek;
                                excells1.Font.Bold = true;
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                ExcelRow++;

                                if (i > 0)
                                {
                                    ExcelL.ExlWs.Cells[row, 3] = isum_real;
                                    ExcelL.ExlWs.Cells[row, 4] = isum_charge;
                                    ExcelL.ExlWs.Cells[row, 5] = isum_money;
                                    excells1 = ExcelL.ExlWs.get_Range("A" + row.ToString(), "E" + row.ToString());
                                    excells1.Font.Bold = true;

                                    isum_real = 0;
                                    isum_charge = 0;
                                    isum_money = 0;
                                }

                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 2], ExcelL.ExlWs.Cells[ExcelRow, 2]);
                                excells1.FormulaR1C1 = "Всего";
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excells1.Font.Bold = true;
                                row = ExcelRow;

                                ExcelRow++;

                            }
                            #endregion

                            if (DT.Rows[i]["sum_real"] != null && DT.Rows[i]["sum_real"].ToString() != "") isum_real += Decimal.Parse(DT.Rows[i]["sum_real"].ToString());
                            if (DT.Rows[i]["sum_charge"] != null && DT.Rows[i]["sum_charge"].ToString() != "") isum_charge += Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                            if (DT.Rows[i]["sum_money"] != null && DT.Rows[i]["sum_money"].ToString() != "") isum_money += Decimal.Parse(DT.Rows[i]["sum_money"].ToString());

                            if (i == DT.Rows.Count - 1)
                            {
                                ExcelL.ExlWs.Cells[row, 3] = isum_real;
                                ExcelL.ExlWs.Cells[row, 4] = isum_charge;
                                ExcelL.ExlWs.Cells[row, 5] = isum_money;
                                excells1 = ExcelL.ExlWs.get_Range("A" + row.ToString(), "E" + row.ToString());
                                excells1.Font.Bold = true;
                            }

                            //№
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = (i + 1).ToString().Trim();

                            //поставщик
                            excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["name_supp"].ToString().Trim();

                            //начислено
                            excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_real"].ToString().Trim();

                            //начислено к  оплате
                            excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_charge"].ToString().Trim();

                            //оплата
                            excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow.ToString(), "E" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_money"].ToString().Trim();

                            ExcelRow++;
                            i++;

                        }
                        #endregion

                        excells1 = ExcelL.ExlWs.get_Range("A4", "E" + (ExcelRow - 1).ToString());
                        excells1.Font.Size = 8;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excells1 = ExcelL.ExlWs.get_Range("C7", "E" + (ExcelRow - 1).ToString());
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlPortrait;
                    }
                    else
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A4", "E4");
                        excells1.EntireColumn.AutoFit();
                        ExcelL.ExlWs.Cells[10, 2] = "Данные не найдены";
                    }

                    #region Сохраняем файл
                    try
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "Charges_" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }
                    finally
                    {
                        ////удаление объекта
                        //ExcelL.DeleteObject();
                    }
                    #endregion

                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetCharges DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формирование GetCharges " + ex.Message, MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание GetCharges DataTable : ОШИБКА!" + ex.Message;
                ////удаление объекта
                //ExcelL.DeleteObject();
                return ret;

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }
            return ret;

        }


       
      
        //public Returns GetSpravSoderg(Prm prm_, string Nzp_user, ref string fileName)
        //{
        //    Returns ret = Utils.InitReturns();

        //    //создание dataTable
        //    ExcelRep ExR = new ExcelRep();
        //    System.Data.DataTable DT = ExR.GetSpravSoderg(prm_, out ret, Nzp_user);

        //    ExcelLoader ExcelL = null;
        //    try
        //    {
        //        ExcelL = new ExcelLoader();

        //        if (DT != null)
        //        {

        //            ExcelCreater ExcelCr = new ExcelCreater();


        //            #region Создаем шапку
        //            Microsoft.Office.Interop.Excel.Range excells1;

        //            Dictionary<string, string> Dic = new Dictionary<string, string>();
        //            Dic.Add("ERCName", "");
        //            string strMonthName = "";

        //            #region Получаем название месяца

        //            switch (prm_.month_)
        //            {
        //                case 1:
        //                    strMonthName = "ЯНВАРЬ";
        //                    break;
        //                case 2:
        //                    strMonthName = "ФЕВРАЛЬ";
        //                    break;
        //                case 3:
        //                    strMonthName = "МАРТ";
        //                    break;
        //                case 4:
        //                    strMonthName = "АПРЕЛЬ";
        //                    break;
        //                case 5:
        //                    strMonthName = "МАЙ";
        //                    break;
        //                case 6:
        //                    strMonthName = "ИЮНЬ";
        //                    break;
        //                case 7:
        //                    strMonthName = "ИЮЛЬ";
        //                    break;
        //                case 8:
        //                    strMonthName = "АВГУСТ";
        //                    break;
        //                case 9:
        //                    strMonthName = "СЕНТЯБРЬ";
        //                    break;
        //                case 10:
        //                    strMonthName = "ОКТЯБРЬ";
        //                    break;
        //                case 11:
        //                    strMonthName = "НОЯБРЬ";
        //                    break;
        //                case 12:
        //                    strMonthName = "ДЕКАБРЬ";
        //                    break;
        //                default:
        //                    break;
        //            }
        //            #endregion

        //            Dic.Add("month", strMonthName);//месяц
        //            Dic.Add("year", prm_.year_.ToString());//год
        //            Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
        //            #endregion

        //            DbSprav dbSprav = new DbSprav();
        //            Finder finder = new Finder();
        //            finder.nzp_user = prm_.nzp_serv;
        //            finder.RolesVal = new List<_RolesVal>();
        //            _RolesVal p = new _RolesVal();
        //            p.kod = STCLINE.KP50.Global.Constants.role_sql_serv;
        //            p.tip = STCLINE.KP50.Global.Constants.role_sql;
        //            p.val = prm_.nzp_serv.ToString();
        //            finder.RolesVal.Add(p);
        //            List<_Service> lserv = dbSprav.ServiceLoad(finder, out ret);
        //            if (lserv.Count > 0)
        //                Dic.Add("SERVICE", lserv[0].service);
        //            else
        //                Dic.Add("Услуга неопределена", lserv[0].service);
        //            dbSprav.Close();

        //            ExcelL.LoadTemlate(Directory.GetCurrentDirectory() + @"\Template\sprav_soderg.xls", Dic);
        //            ExcelFormater exform = new ExcelFormater();
        //            excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.Font.Size = 8;
        //            excells1.RowHeight = 23;

        //            #region Пишем тело

        //            int ExcelRow = 8;
        //            int i = 0;
        //            int ExcelCol = 5;
        //            int count_dom = 0;
        //            int maxExcelCol = 0;
        //            string oldDom = "";
        //            string adres = "";
        //            string geu = "";
        //            decimal[] domMas = new decimal[9];
        //            for (int j = 0; j < 9; j++) domMas[j] = 0;

        //            decimal[] itoMas = new decimal[9];
        //            for (int j = 0; j < 9; j++) itoMas[j] = 0;

        //            List<Decimal> calcMas = new List<Decimal>();
        //            List<Decimal> gilMas = new List<Decimal>();
        //            List<Decimal> plMas = new List<Decimal>();
        //            List<Decimal> odnMas = new List<Decimal>();





        //            while (i < DT.Rows.Count)
        //            {

        //                adres = DT.Rows[i]["ulica"].ToString().Trim() + "," +
        //                    DT.Rows[i]["ndom"].ToString().Trim() +
        //                    DT.Rows[i]["nkor"].ToString().Trim();
        //                geu = DT.Rows[i]["geu"].ToString().Trim();
        //                if (i == 0) oldDom = adres;

        //                #region Записываем Итого по дому
        //                if (oldDom != adres)
        //                {
        //                    count_dom++;

        //                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + (ExcelRow + 1).ToString());
        //                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //                    excells1.Merge(Type.Missing);
        //                    excells1.FormulaR1C1 = count_dom;


        //                    excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + (ExcelRow + 1).ToString());
        //                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //                    excells1.Merge(Type.Missing);
        //                    excells1.FormulaR1C1 = DT.Rows[i]["geu"].ToString().Trim();


        //                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + (ExcelRow + 1).ToString());
        //                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //                    excells1.Merge(Type.Missing);
        //                    excells1.FormulaR1C1 = oldDom;





        //                    for (int j = 0; j < 7; j++)
        //                    {
        //                        ExcelL.ExlWs.Cells[ExcelRow, ExcelCol + j + 1] = domMas[j];
        //                        excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol + j].ToString() +
        //                                                    ExcelRow.ToString(), exform.BukvaList[ExcelCol + j].ToString() + (ExcelRow + 3).ToString());
        //                        excells1.Merge(Type.Missing);
        //                        excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
        //                    }


        //                    excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol-1].ToString() +
        //                        ExcelRow.ToString(), exform.BukvaList[ExcelCol-1].ToString() + (ExcelRow +1 ).ToString());
        //                    excells1.Merge(Type.Missing);
        //                    excells1.FormulaR1C1 = domMas[7];
        //                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

        //                    excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol-1].ToString() +
        //                        (ExcelRow+2).ToString(), exform.BukvaList[ExcelCol-1].ToString() + (ExcelRow + 3).ToString());
        //                    excells1.Merge(Type.Missing);
        //                    excells1.FormulaR1C1 = domMas[8];
        //                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

        //                    for (int j = 0; j < 9; j++) domMas[j] = 0;


        //                    ExcelCol = 4;
        //                    oldDom = adres;
        //                    ExcelRow +=4;


        //                }
        //                #endregion

        //                #region Проживающие
        //                ExcelL.ExlWs.Cells[ExcelRow, ExcelCol] = DT.Rows[i]["count_gil"].ToString();
        //                if (gilMas.Count < ExcelCol + 1)
        //                {
        //                    for (int k = gilMas.Count; k < ExcelCol+1; k++) gilMas.Add(0);
        //                }
        //                gilMas[ExcelCol] = gilMas[ExcelCol] + Decimal.Parse(DT.Rows[i]["count_gil"].ToString());
        //                #endregion


        //                #region Объем
        //                if (calcMas.Count < ExcelCol + 1)
        //                {
        //                    for (int k = calcMas.Count; k < ExcelCol + 1; k++) calcMas.Add(0);
        //                }
        //                if (DT.Rows[i]["c_calc"].ToString().Trim() != "")
        //                    if (Decimal.Parse(DT.Rows[i]["c_calc"].ToString().Trim()) > 0)
        //                    {
        //                        ExcelL.ExlWs.Cells[ExcelRow + 1, ExcelCol] = Decimal.Parse(DT.Rows[i]["c_calc"].ToString());
        //                        calcMas[ExcelCol] = calcMas[ExcelCol] + Decimal.Parse(DT.Rows[i]["c_calc"].ToString());
        //                    }
        //                #endregion

        //                #region ОДН
        //                ExcelL.ExlWs.Cells[ExcelRow+2, ExcelCol] = DT.Rows[i]["c_calc_odn"].ToString();
        //                if (odnMas.Count < ExcelCol + 1)
        //                {
        //                    for (int k = odnMas.Count; k < ExcelCol+2; k++) odnMas.Add(0);
        //                }
        //                odnMas[ExcelCol] = odnMas[ExcelCol] + Decimal.Parse(DT.Rows[i]["c_calc_odn"].ToString());
        //                #endregion

        //                #region площадь
        //                ExcelL.ExlWs.Cells[ExcelRow + 3, ExcelCol] = DT.Rows[i]["pl_kvar"].ToString();
        //                if (plMas.Count < ExcelCol + 1)
        //                {
        //                    for (int k = plMas.Count; k < ExcelCol + 2; k++) plMas.Add(0);
        //                }
        //                plMas[ExcelCol] = plMas[ExcelCol] + Decimal.Parse(DT.Rows[i]["pl_kvar"].ToString());
        //                #endregion

        //                #region Пишем заголовок формул
        //                if (ExcelRow == 8)
        //                {
        //                    ExcelL.ExlWs.Cells[7, ExcelCol] = DT.Rows[i]["name_frm"].ToString();
        //                    excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol - 1].ToString() + "7", exform.BukvaList[ExcelCol - 1].ToString() + "7");
        //                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
        //                    excells1.WrapText = true;
        //                    excells1.ColumnWidth = 10;
        //                    excells1.Font.Size = 6;
        //                }
        //                #endregion



        //                domMas[0] = domMas[0] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
        //                domMas[1] = domMas[1] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
        //                domMas[2] = domMas[2] + Decimal.Parse(DT.Rows[i]["sum_odn"].ToString());
        //                domMas[3] = domMas[3] + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
        //                domMas[4] = domMas[4] + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
        //                domMas[5] = domMas[5] + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
        //                domMas[6] = domMas[6] + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
        //                domMas[7] = domMas[7] + Decimal.Parse(DT.Rows[i]["c_reval"].ToString());
        //                domMas[8] = domMas[8] + Decimal.Parse(DT.Rows[i]["c_reval_odn"].ToString());

        //                itoMas[0] = itoMas[0] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
        //                itoMas[1] = itoMas[1] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
        //                itoMas[2] = itoMas[2] + Decimal.Parse(DT.Rows[i]["sum_odn"].ToString());
        //                itoMas[3] = itoMas[3] + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
        //                itoMas[4] = itoMas[4] + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
        //                itoMas[5] = itoMas[5] + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
        //                itoMas[6] = itoMas[6] + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
        //                itoMas[7] = itoMas[7] + Decimal.Parse(DT.Rows[i]["c_reval"].ToString());
        //                itoMas[8] = itoMas[8] + Decimal.Parse(DT.Rows[i]["c_reval_odn"].ToString());



        //                ExcelCol++;
        //                i++;
        //            }

        //            count_dom++;

        //            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + (ExcelRow + 3).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = count_dom;

        //            excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + (ExcelRow + 3).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = geu;


        //            excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + (ExcelRow + 3).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = adres;

        //            excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow .ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "жильцов, чел.";

        //            excells1 = ExcelL.ExlWs.get_Range("D" + (ExcelRow + 1).ToString(), "D" + (ExcelRow + 1).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Объем л/счетов, м3";

        //            excells1 = ExcelL.ExlWs.get_Range("D" + (ExcelRow + 2).ToString(), "D" + (ExcelRow + 2).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Объем ОДН, м3";

        //            excells1 = ExcelL.ExlWs.get_Range("D" + (ExcelRow + 3).ToString(), "D" + (ExcelRow + 3).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "площадь, м2";


        //            for (int j = 0; j < 7; j++)
        //            {
        //                ExcelL.ExlWs.Cells[ExcelRow, ExcelCol + j + 1] = domMas[j];
        //                excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol + j].ToString() +
        //                                            ExcelRow.ToString(), exform.BukvaList[ExcelCol + j].ToString() + (ExcelRow + 3).ToString());
        //                excells1.Merge(Type.Missing);
        //                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
        //            }

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol-1].ToString() +
        //                                        ExcelRow.ToString(), exform.BukvaList[ExcelCol-1].ToString() + (ExcelRow + 1).ToString());
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = domMas[7];
        //            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol-1].ToString() +
        //                (ExcelRow + 2).ToString(), exform.BukvaList[ExcelCol-1].ToString() + (ExcelRow + 3).ToString());
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = domMas[8];
        //            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

        //            maxExcelCol = ExcelCol;

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol - 1].ToString() + "6", exform.BukvaList[ExcelCol - 1].ToString() + "7");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Объемы перерасчетов, м3, (возвраты+разовые)/тариф поставщика";

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol].ToString() + "6", exform.BukvaList[ExcelCol].ToString() + "7");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Начислено (тариф*кол-во), руб.";

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol + 1].ToString() + "6", exform.BukvaList[ExcelCol + 1].ToString() + "7");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Постоянное начисление, руб.";

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol + 2].ToString() + "6", exform.BukvaList[ExcelCol + 2].ToString() + "7");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Начислено ОДН, руб.";

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol + 3].ToString() + "6", exform.BukvaList[ExcelCol + 3].ToString() + "7");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Возврат за услуги, руб.";

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol + 4].ToString() + "6", exform.BukvaList[ExcelCol + 4].ToString() + "7");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Начислено раз. красным, руб.";

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol + 5].ToString() + "6", exform.BukvaList[ExcelCol + 5].ToString() + "7");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Начислено раз черным, руб.";

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol + 6].ToString() + "6", exform.BukvaList[ExcelCol + 6].ToString() + "7");
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = "Итого к оплате, руб.";



        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol - 1].ToString() + "6", exform.BukvaList[ExcelCol + 6].ToString() + "7");
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
        //            excells1.WrapText = true;
        //            excells1.ColumnWidth = 10;


        //            excells1 = ExcelL.ExlWs.get_Range("E6", exform.BukvaList[ExcelCol - 2].ToString() + "6");
        //            excells1.Merge(Type.Missing);
        //            excells1.Font.Size = 6;
        //            excells1.FormulaR1C1 = "Количество жильцов/нат.показатель";
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
        //            excells1.WrapText = true;

        //            excells1 = ExcelL.ExlWs.get_Range("D6", "D7");
        //            excells1.Merge(Type.Missing);
        //            excells1.Font.Size = 6;
        //            excells1.FormulaR1C1 = "";
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
        //            excells1.WrapText = true;



        //            #endregion


        //            #region Пишем итого
        //            ExcelRow +=4;


        //            for (int j = 4; j < maxExcelCol; j++)
        //                ExcelL.ExlWs.Cells[ExcelRow, j] = gilMas[j];

        //            for (int j = 4; j < maxExcelCol; j++)
        //                ExcelL.ExlWs.Cells[ExcelRow+1, j] = calcMas[j];

        //            for (int j = 4; j < maxExcelCol; j++)
        //                ExcelL.ExlWs.Cells[ExcelRow + 2, j] = odnMas[j];

        //            for (int j = 4; j < maxExcelCol; j++)
        //                ExcelL.ExlWs.Cells[ExcelRow + 3, j] = plMas[j];

        //            for (int j = 0; j < 7; j++)
        //            {
        //                ExcelL.ExlWs.Cells[ExcelRow, maxExcelCol + j + 1] = itoMas[j];
        //                excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[maxExcelCol + j].ToString() +
        //                                            ExcelRow.ToString(), exform.BukvaList[maxExcelCol + j].ToString() + (ExcelRow + 3).ToString());
        //                excells1.Merge(Type.Missing);
        //                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
        //            }

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[maxExcelCol-1].ToString() +
        //                    ExcelRow.ToString(), exform.BukvaList[maxExcelCol-1].ToString() + (ExcelRow + 1).ToString());
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = itoMas[7];
        //            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

        //            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[maxExcelCol-1].ToString() +
        //                (ExcelRow + 2).ToString(), exform.BukvaList[maxExcelCol-1].ToString() + (ExcelRow + 3).ToString());
        //            excells1.Merge(Type.Missing);
        //            excells1.FormulaR1C1 = itoMas[8];
        //            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

        //            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "C" + (ExcelRow+3).ToString());
        //            excells1.Merge(Type.Missing);
        //            excells1.Font.Size = 6;
        //            excells1.FormulaR1C1 = "Итого";
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
        //            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

        //            excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow .ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.FormulaR1C1 = "жильцов, чел.";
        //            excells1.Font.Size = 6;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

        //            excells1 = ExcelL.ExlWs.get_Range("D" + (ExcelRow + 1).ToString(), "D" + (ExcelRow + 1).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.FormulaR1C1 = "Объем л/счетов, м3";
        //            excells1.Font.Size = 6;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

        //            excells1 = ExcelL.ExlWs.get_Range("D" + (ExcelRow + 2).ToString(), "D" + (ExcelRow + 2).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.FormulaR1C1 = "Объем ОДН, м3";
        //            excells1.Font.Size = 6;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

        //            excells1 = ExcelL.ExlWs.get_Range("D" + (ExcelRow + 3).ToString(), "D" + (ExcelRow + 3).ToString());
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
        //            excells1.FormulaR1C1 = "площадь, м2";
        //            excells1.Font.Size = 6;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
        //            #endregion

        //            ExcelRow +=3;
        //            #region форматируем
        //            excells1 = ExcelL.ExlWs.get_Range("A6", exform.BukvaList[ExcelCol + 6].ToString() + ExcelRow);
        //            excells1.Font.Size = 6;
        //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

        //            excells1 = ExcelL.ExlWs.get_Range("E8", exform.BukvaList[ExcelCol + 6].ToString() + ExcelRow);
        //            excells1.EntireColumn.NumberFormat = "# ##0,00";
        //            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
        //            #endregion

        //            SetReportSing(ExcelL, ExcelRow + 3);
        //            //ExcelL.ExlWs.Cells[ExcelRow + 3, 1] = "Директор";
        //            //ExcelL.ExlWs.Cells[ExcelRow + 5, 1] = "Начальник ПЭО";
        //            //ExcelL.ExlWs.Cells[ExcelRow + 7, 1] = "Начальник ОНУПН";
        //            //ExcelL.ExlWs.Cells[ExcelRow + 3, 3] = " Мякишев М.В.";
        //            //ExcelL.ExlWs.Cells[ExcelRow + 5, 3] = " Соковых И.А.";
        //            //ExcelL.ExlWs.Cells[ExcelRow + 7, 3] = " Старкова Л.А.";


        //            //#endregion

        //            #region Сохраняем файл
        //            try
        //            {

        //                fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg" +
        //                    prm_.year_.ToString() + "_" +
        //                    prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
        //                ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);

        //            }
        //            catch (Exception ex)
        //            {
        //                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
        //            }

        //            #endregion

        //        }

        //        else
        //        {
        //            MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
        //            ret.result = false;
        //            ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

        //            if (ExcelL != null)
        //            {
        //                fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg" +
        //                    prm_.year_.ToString() + "_" +
        //                    prm_.month_.ToString()
        //                    + "_" + Nzp_user) + ".xls";
        //                ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);

        //            }

        //            //удаление объекта
        //            return ret;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
        //        try
        //        {
        //            if (ExcelL != null)
        //            {
        //                fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg" +
        //                    prm_.year_.ToString() + "_" +
        //                    prm_.month_.ToString()
        //                    + "_" + Nzp_user) + ".xls";
        //                ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);

        //            }
        //        }
        //        catch
        //        {
        //            MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravSoderg", MonitorLog.typelog.Warn, true);
        //            ret.result = false;
        //            ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
        //        }

        //    }
        //    finally
        //    {
        //        //удаление объекта
        //        if (ExcelL != null)
        //        {
        //            ExcelL.DeleteObject();
        //        }
        //    }

        //    return ret;

        //}
    
        /// <summary>
        /// Получение информации по расчетам с населением
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="nzp_supp">Номер поставщика услуг</param>
        /// <param name="month">месяц</param>
        /// <param name="year">год</param>
        /// <param name="fileName">имя файла</param>
        /// <returns>Returns</returns>
     
        /// <summary>
        /// Отчет "Контроль распределения оплат"
        /// </summary>
        public Returns GetControlDistribPayments(Payments pay, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;

            ////создание dataTable
            //PackFinder finder = new PackFinder();
            //finder.nzp_user = pay.nzp_user;
            //finder.dat_uchet = pay.dat_s;
            //finder.dat_uchet_po = pay.dat_po;
            //if (pay.points.Count > 0)
            //{
            //    if (pay.points[0].nzp_wp != 0)
            //    {
            //        finder.dopPointList = (from _Point a in pay.points select a.nzp_wp).ToList<int>();
            //    }
            //}

            //ExcelRep ExR = new ExcelRep();
            //DataSet ds = ExR.GetDistribLog(finder, out ret);
            //ExR.Close();


            //if (!ret.result)
            //{
            //    return ret;
            //}


            DataSet ds = pay.data;
            if (ds == null)
            {
                MonitorLog.WriteLog("DataSet null", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Получение DataSet : ОШИБКА!";
                return ret;
            }

            #region ИНФОРМАЦИЯ ПО ПАЧКАМ

            System.Data.DataTable pack_ls = new System.Data.DataTable();
            DataColumn c1 = new DataColumn("name");
            DataColumn c2 = new DataColumn("tot_sum");
            pack_ls.Columns.Add(c1);
            pack_ls.Columns.Add(c2);

            System.Data.DataTable temp_pack_ls = ds.Tables["pack_ls"];


            pack_ls.Rows.Add("Всего распределено в пачках (РП)", temp_pack_ls.Rows[0][0]);
            pack_ls.Rows.Add("Всего нераспределено в пачках", temp_pack_ls.Rows[0][1]);
            pack_ls.Rows.Add("Всего необработано в пачках", temp_pack_ls.Rows[0][2]);
            decimal result = Convert.ToDecimal(temp_pack_ls.Rows[0][0]) + Convert.ToDecimal(temp_pack_ls.Rows[0][1]) + Convert.ToDecimal(temp_pack_ls.Rows[0][2]);
            pack_ls.Rows.Add("ИТОГО", result);
            #endregion


            #region ИНФОРМАЦИЯ ПО ТЕРРИТОРИЯМ

            System.Data.DataTable fn_supplier = ds.Tables["fn_supplier"];
            fn_supplier.Columns.Remove("nzp_wp");
            fn_supplier.Columns.Remove("pref");

            decimal sum_tot = 0;


            foreach (DataRow r in fn_supplier.Rows)
            {
                int err_code = 0;
                Int32.TryParse(r["err_code"].ToString(), out err_code);
                if (err_code != 0)
                {
                    r["sum_prih"] = r["err_message"];
                }
                else
                {
                    decimal s_prih = 0;
                    decimal.TryParse(r["sum_prih"].ToString(), out s_prih);
                    s_prih = Math.Round(s_prih, 2);

                    sum_tot += s_prih;
                }
            }
            fn_supplier.Columns.Remove("err_code");
            fn_supplier.Columns.Remove("err_message");

            //добавляю итого
            fn_supplier.Rows.Add("ИТОГО", sum_tot);


            #endregion


            ExcelFormater Eformat = new ExcelFormater();

            //создаем массив заголовков pack_ls
            string[] HeaderData_pack_ls = new string[pack_ls.Columns.Count];
            //создаем массив форматов pack_ls
            string[] TypeHeader_pack_ls = new string[HeaderData_pack_ls.Length];

            HeaderData_pack_ls[0] = "Наименование";
            HeaderData_pack_ls[1] = "Сумма всего";

            TypeHeader_pack_ls[0] = "char";
            TypeHeader_pack_ls[1] = "char";




            //создаем массив заголовков fn_supplier
            string[] HeaderData_fn_supplier = new string[pack_ls.Columns.Count];
            //создаем массив форматов fn_supplier
            string[] TypeHeader_fn_supplier = new string[HeaderData_pack_ls.Length];

            HeaderData_fn_supplier[0] = "Наименование";
            HeaderData_fn_supplier[1] = "Сумма распределения";

            TypeHeader_fn_supplier[0] = "char";
            TypeHeader_fn_supplier[1] = "char";


            //формирование Excel файла                 
            try
            {
                #region Создание Excel документа

                ExcelL = new ExcelLoader();
                ExcelL.ExlWs.Rows.Font.Name = "Arial";
                ExcelCreater ExcelCr = new ExcelCreater();

                //создаем название отчета
                ret = ExcelCr.MakeName("Контроль распределения оплат", "A1", pack_ls.Columns.Count, 2, ref ExcelL.ExlWs);

                Range ran = ExcelL.ExlWs.get_Range("A3", "B3");
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                ran.Merge(Type.Missing);
                ran.Value2 = "Сводная информация по распределению оплат";

                ran = ExcelL.ExlWs.get_Range("A4", "B4");
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                ran.Merge(Type.Missing);
                ran.Value2 = "за период с " + pay.dat_s + " по " + pay.dat_po;

                ran = ExcelL.ExlWs.get_Range("A6", "B6");
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                ran.Merge(Type.Missing);
                ran.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                ran.Value2 = "Информация по пачкам";

                ran = ExcelL.ExlWs.get_Range("A13", "B13");
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                ran.Merge(Type.Missing);
                ran.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                ran.Value2 = "Информация по управляющим организациям";



                ran = ExcelL.ExlWs.get_Range("A1", Type.Missing);
                ran.ColumnWidth = 52;

                ran = ExcelL.ExlWs.get_Range("B1", Type.Missing);
                ran.ColumnWidth = 31;

                //Создаем шапку
                ret = ExcelCr.MakeHeader(HeaderData_pack_ls, 5, 0, ref ExcelL.ExlWs);
                //центруем шапку
                ran = ExcelL.ExlWs.get_Range("A7", "B7");
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                //Создаем тело               
                ret = ExcelCr.MakeBody(pack_ls, 6, 0, ref ExcelL.ExlWs);
                //строкчки итого - жирные
                ran = ExcelL.ExlWs.get_Range("A11", "B11");
                ran.Font.Bold = true;


                //Создаем шапку
                ret = ExcelCr.MakeHeader(HeaderData_fn_supplier, 12, 0, ref ExcelL.ExlWs);
                //центруем шапку
                ran = ExcelL.ExlWs.get_Range("A14", "B14");
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                //Создаем тело               
                ret = ExcelCr.MakeBody(fn_supplier, 13, 0, ref ExcelL.ExlWs);
                //строчки итого жирные
                ran = ExcelL.ExlWs.get_Range("A" + (14 + fn_supplier.Rows.Count), "B" + (14 + fn_supplier.Rows.Count));
                ran.Font.Bold = true;


                //строчка Сумма рассогласования
                ran = ExcelL.ExlWs.get_Range("A" + (15 + fn_supplier.Rows.Count), "B" + (15 + fn_supplier.Rows.Count));
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                ran.Merge(Type.Missing);
                decimal rp = 0;
                decimal.TryParse(pack_ls.Rows[0][1].ToString().Replace(",", "."), out rp);
                ran.Font.Bold = true;
                ran.Value2 = "Сумма рассогласования распределения (РП-ИР) " + rp + " - " + sum_tot + " = " + (rp - sum_tot);



                ran = ExcelL.ExlWs.get_Range("A" + (17 + fn_supplier.Rows.Count), "B" + (17 + fn_supplier.Rows.Count));
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                ran.Merge(Type.Missing);
                ran.Value2 = "Время формирования : " + DateTime.Now;


                ran = ExcelL.ExlWs.get_Range("A" + (18 + fn_supplier.Rows.Count), "B" + (18 + fn_supplier.Rows.Count));
                ran.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                ran.Merge(Type.Missing);
                ran.Value2 = "Пользователь : " + pay.uname;


                #endregion

                if (ret.result)
                {
                    //имя файла                                                         
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "ContDistribP" + "_" + pay.nzp_user) + ".xls";

                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }



       
        //Отчет: список заявок
        public void GetOrderList(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"1.5.Список заявок\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"1.5.Список заявок\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);


            ret = this.GetOrderList(cont.nzp_user, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчеты по спискам заявок
        public void GetSupgReports(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            SupgFinder finder = new SupgFinder();
            finder._date_to = cont.to_mm;
            finder._date_from = cont.to_mm;
            finder.nzp_user = cont.nzp_user;

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                if (cont.en == enSrvOper.GetInfoFromService)
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"1.1.Информация, полученная ОДДС\"", ref time2, cont.comment);
                }
                if (cont.en == enSrvOper.GetAppInfoFromService)
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"1.2.Приложение к информации, полученной ОДДС\"", ref time2, cont.comment);
                }
                if (cont.en == enSrvOper.GetJoborderPeriodOutstand)
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"1.4.Список невыполненных нарядов-заказов к концу периода\"", ref time2, cont.comment);
                }
                if (cont.en == enSrvOper.GetCountOrderReadres)
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"2.4.Количество переадресаций заявок, принятых ОДДС\"", ref time2, cont.comment);
                }
                if (cont.en == enSrvOper.GetMessageList)
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"1.3.1.Список сообщений, зарегестрированных ОДДС\"", ref time2, cont.comment);
                }
                if (cont.en == enSrvOper.GetMessageQuestList)
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"1.3.2.Список сообщений, зарегестрированных ОДДС(опрос)\"", ref time2, cont.comment);
                }
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            if (cont.en == enSrvOper.GetInfoFromService)
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"1.1.Информация, полученная ОДДС\"", ref time, "");
            }
            if (cont.en == enSrvOper.GetAppInfoFromService)
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"1.2.Приложение к информации, полученной ОДДС\"", ref time, "");
            }
            if (cont.en == enSrvOper.GetJoborderPeriodOutstand)
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"1.4.Список невыполненных нарядов-заказов к концу периода\"", ref time, "");
            }
            if (cont.en == enSrvOper.GetCountOrderReadres)
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"2.4.Количество переадресаций заявок, принятых ОДДС\"", ref time, "");
            }
            if (cont.en == enSrvOper.GetMessageList)
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"1.3.1.Список сообщений, зарегестрированных ОДДС\"", ref time, "");
            }
            if (cont.en == enSrvOper.GetMessageQuestList)
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"1.3.2.Список сообщений, зарегестрированных ОДДС(опрос)\"", ref time, "");
            }

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);


            ret = this.GetSupgReports(finder, cont.en, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: список плановых работ
        public void GetPlannedWorksList(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                if (cont.en == enSrvOper.GetPlannedWorksSupp)
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"3.1.Cведения по отключениям услуг по поставщикам\"", ref time2, cont.comment);
                }
                else
                {
                    if (cont.en == enSrvOper.GetPlannedWorksNone)
                    {
                        ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"3.2.Сведения по отключениям услуг\"", ref time2, cont.comment);
                    }
                    else
                    {
                        if (cont.en == enSrvOper.GetPlannedWorksActs)
                        {
                            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"3.3.Акты по отключениям услуг\"", ref time2, cont.comment);
                        }
                    }
                }
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие установленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            if (cont.en == enSrvOper.GetPlannedWorksSupp)
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"3.1.Cведения по отключениям услуг по поставщикам\"", ref time, "");
            }
            else
            {
                if (cont.en == enSrvOper.GetPlannedWorksNone)
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"3.2.Сведения по отключениям услуг\"", ref time, "");
                }
                else
                {
                    if (cont.en == enSrvOper.GetPlannedWorksActs)
                    {
                        ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"3.3.Акты по отключениям услуг\"", ref time, "");
                    }
                }
            }

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);


            ret = this.GetPlannedWorksList(cont.nzp_user, cont.en, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: отчет по количеству заявлений, направленных по услугам за период
        public void GetCountOrders(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;

            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                if (cont.finder.prms == "2.1")
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"2.1.Количество нарядов-заказов по услугам\"", ref time2, cont.comment);
                }
                if (cont.finder.prms == "2.2")
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"2.2.Количество нарядов-заказов по подрядчикам\"", ref time2, cont.comment);
                }
                if (cont.finder.prms == "2.3")
                {
                    ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"2.3.Количество нарядов-заказов по неисправностям\"", ref time2, cont.comment);
                }
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            if (cont.finder.prms == "2.1")
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"2.1.Количество нарядов-заказов по услугам\"", ref time, "");
            }
            if (cont.finder.prms == "2.2")
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"2.2.Количество нарядов-заказов по подрядчикам\"", ref time, "");
            }
            if (cont.finder.prms == "2.3")
            {
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"2.3.Количество нарядов-заказов по неисправностям\"", ref time, "");
            }

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);


            ret = this.GetCountOrders(cont.finder, cont._nzp, cont._nzp_add, cont.from_mm, cont.to_mm, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: для списока поступивших нарядов-заказов
        public void GetIncomingJobOrders(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Cписок поступивших нарядов-заказов\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Cписок поступивших нарядов-заказов\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);


            ret = this.GetIncomingJobOrders(cont.nzp_user, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: для списока поступивших нарядов-заказов
        public void GetRepNedopList(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Список недопоставок (учет претензий граждан)\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Список недопоставок (учет претензий граждан)\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);


            ret = this.GetRepNedopList(cont.nzp_user, System.Convert.ToInt32(cont._nzp), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: извещение за месяц
        public void GetDeliveredServicesPayment(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Оплата гражданами-получателями коммунальных услуг за поставленные услуги\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.finder.nzp_user, "empty", 2, "Отчет \"Оплата гражданами-получателями коммунальных услуг за поставленные услуги\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.finder.nzp_user, "1", 2, "", time);


            ret = this.GetDeliveredServicesPayment(cont.finder, cont.nzp_supp, cont.yy, cont.mm, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.finder.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.finder.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.finder.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.finder.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

      
        //Отчет: карта аналитического учета
        public void GetAnalisKart(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Карточка аналитического учета\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Карточка аналитического учета\" ", ref time, "");
            int nzpExcelUtility = ret.tag;
            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetAnalisKart(cont.listprm, cont.nzp_user.ToString(), ref fileName,  nzpExcelUtility);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        

        //Отчет: Начисления за месяц к оплате и оплаты Нижнекамск
        public void GetCharges(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();
            string rep_name = "Начисления и оплаты по поставщикам";
            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"" + rep_name + "\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"" + rep_name + "\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetCharges(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

     

        //Отчет: справка о лицевом счете
        public void GetLicSchetExcel(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по лицевому счету (Excel)\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка по лицевому счету (Excel)\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Ls finder = new Ls();
            finder.nzp_kvar = cont.nzp_kvar;
            string[] st = cont.comment.Split(' ');
            finder.pref = st[0];
            string sum_dolg = st[1];
            finder.nzp_user = cont.nzp_user;
            ret = this.GetLicSchetExcel(finder, System.Convert.ToInt32(cont.yy), System.Convert.ToInt32(cont.mm), sum_dolg, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

       
        //Отчет: Акт сверки с Энергосбытом
        public void GetEnergoActSverki(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Акт сверки с энергосбытом\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Акт сверки с энергосбытом\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetEnergoActSverki(cont.listprm[0], cont.nzp_supp, cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

      

        

        //Отчет: Список должников более трех месяцев
        public void GetDolgSpis(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Список должников с указанием срока задолженности\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            //string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Список должников с указанием срока задолженности\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetDolgSpis(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, STCLINE.KP50.Global.Constants.ExcelDir.Replace('/','\\') + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Список Рассогласований с паспортисткой
        public void GetPaspRas(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Рассогласование с паспортисткой\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Рассогласование с паспортисткой\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetPaspRas(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Список Рассогласований с паспортисткой
        public void GetPaspRasCommon(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Рассогласование с паспортисткой\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Рассогласование с паспортисткой\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetPaspRasCommon(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Состояние жилого фонда
        public void GetSostGilFond(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Отчет о жилом фонде\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion


            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Отчет о жилом фонде\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSostGilFond(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }



        //Отчет: Сводная ведомость нормативов потребления
        public void GetFakturaFiles(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Формирование квитанций\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Формирование квитанций\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetFakturaFiles(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public void GetUploadCharge(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = GetUploadCharge(out ret, cont.supgfinder, cont.yy, cont.mm);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GetUploadCharge(out Returns ret, SupgFinder finder, string year, string month)
        {
            ExcelRep ExR = new ExcelRep();
            ret = Utils.InitReturns();
            ret = ExR.GetUploadCharge(out ret, finder, year, month);
            return ret;
        }
        /// <summary>
        /// Выгрузка показаний ПУ
        /// </summary>
        /// <returns>true/false</returns>  
        public void GetUploadPU(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = GetUploadPU(out ret, cont.supgfinder, cont.yy, cont.mm);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GetUploadPU(out Returns ret, SupgFinder finder, string year, string month)
        {
            ExcelRep ExR = new ExcelRep();
            ret = Utils.InitReturns();
            ret = ExR.GetUploadPU(out ret, finder, year, month);
            return ret;
        }

        /// <summary>
        /// Выгрузка реестра для загрузки в БС
        /// </summary>
        /// <returns>true/false</returns>  
        public void GetUploadReestr(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = new Returns();
            ret = this.GetUploadReestr(out ret, cont.Finder, cont.parList, cont.unloadVersionFormat, cont.comment);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GetUploadReestr(out Returns ret, Finder finder, List<int> BanksList, string unloadVersionFormat,string statusLS)
        {
            ExcelRep ExR = new ExcelRep();
            ret = Utils.InitReturns();
            ret = ExR.GetUploadReestr(out ret, finder, BanksList, unloadVersionFormat, statusLS);
            return ret;
        }
        
        public void GetUploadKassa(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Формирование квитанций\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetUploadKassa(out ret, cont.supgfinder, cont.yy, cont.mm);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        /// <summary>
        /// Выгрузка в кассу 3.0
        /// </summary>
        /// <returns>true/false</returns>      

        public Returns GetUploadKassa(out Returns ret, SupgFinder finder, string year, string month)
        {
            ExcelRep ExR = new ExcelRep();
            ret = Utils.InitReturns();
            ret = ExR.GetUploadKassa(out ret, finder, year, month);
            return ret;
        }

       

      
        //Получение информации по расчетам с населением
        public void GetInfPoRaschetNasel(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Получение информации по расчетам с населением\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Получение информации по расчетам с населением\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetInfPoRaschetNasel(cont.finder, cont.nzp_user, cont.nzp_supp, Convert.ToInt32(cont.mm), Convert.ToInt32(cont.yy), ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

       
        /// <summary>
        /// Отчет "Контроль распределения оплат"
        /// </summary>
        public void GetControlDistribPayments(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Генератор по начислениям\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.payments.nzp_user, "empty", 2, "Отчет \"Контроль распределения оплат\" ", ref time, cont.comment);

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.payments.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetControlDistribPayments(cont.payments, ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.payments.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.payments.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.payments.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.payments.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        /// <summary>
        /// Выгрузка по принятым для перечисления денежным средствам
        /// </summary>
        /// <param name="container"></param>
        public void GetChargeUnload(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Выгрузка по принятым для перечисления денежным средствам\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetChargeUnload(out ret, cont.chargeUnloadPrm);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        /// <summary>
        /// Выгрузка по принятым для перечисления денежным средствам
        /// </summary>
        /// <returns>true/false</returns>      
        public Returns GetChargeUnload(out Returns ret, ChargeUnloadPrm finder)
        {
            ExcelRep ExR = new ExcelRep();
            ret = Utils.InitReturns();
            ret = ExR.GetChargeUnload(finder);
            return ret;
        }

        //==============================================================================================================

        /////////////////////Перенести в Глобал Утилс//////////////////////////////////////////////////
        public static string GetFileName(string directory, string sourceFilename)
        {
            try
            {
                string targetfileName = directory + System.IO.Path.GetFileName(sourceFilename);
                string fn = sourceFilename;
                int k = 0;
                while (System.IO.File.Exists(targetfileName))
                {
                    k++;
                    fn = System.IO.Path.GetFileNameWithoutExtension(sourceFilename) + "_" + k + System.IO.Path.GetExtension(sourceFilename);
                    targetfileName = directory + fn;
                }
                return fn;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message + " Но все равно будет работать!", MonitorLog.typelog.Warn, true);
                return sourceFilename + "_" + DateTime.Now.Ticks;
            }
        }


       
        public void GetRegisterCounters(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Реестр счетчиков по лицевым счетам\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Реестр счетчиков по лицевым счетам\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetRegisterCounters((SupgFinder)cont.finder, ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }



      



        ///////////////////////////////////////////////////////////////////////////////////////////////

     
    }
}
