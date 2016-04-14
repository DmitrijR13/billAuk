using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;
using DataTable = System.Data.DataTable;

namespace STCLINE.KP50.REPORT
{
    public partial class ReportGen
    {

        public void MakeListSpravSuppNachHar2(DataTable DT, ExcelLoader excelL, int numList, Prm prm,
            string nzpUser)
        {
            if (DT == null) throw new ArgumentNullException("DT");
            excelL.GetWs(numList, "Лист " + numList);

            #region Создаем шапку

            //спустили шапку
            Range excells1 = excelL.ExlWs.Range["D1", "M1"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "СПРАВКА ПО ПОСТАВЩИКАМ КОММУНАЛЬНЫХ УСЛУГ";
            excells1.Font.Bold = true;
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

            excells1 = excelL.ExlWs.Range["D2", "M2"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            //string ERC_name = "";

            //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
            //    ERC_name = "Н-ска";
            //else
            //    ERC_name = "Самары";

            //excells1.FormulaR1C1 = "по МП городского округа " + ERC_name + " ''ЕИРЦ'' за " +
            string sName = SetReportHeader();
            var dt = new DateTime(2001, prm.month_, 1);
            string month = dt.ToString("MMMM", CultureInfo.CurrentCulture);

            excells1.FormulaR1C1 = sName + " за " + month + " " + prm.year_ + "г.";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

            excells1 = excelL.ExlWs.Range["A3", "A3"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            excells1.FormulaR1C1 = DateTime.Today.ToShortDateString();
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

            excells1 = excelL.ExlWs.Range["B3", "G4"];
            excells1.Font.Bold = true;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(nzpUser));
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.RowHeight = 23;

            if (numList > 1)
            {
                excells1 = excelL.ExlWs.Range["A5", "R5"];
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = "";

                excells1.FormulaR1C1 = "ЖЭУ: " + DT.Rows[0]["geu"];
                excells1.Font.Bold = true;
            }


            excells1 = excelL.ExlWs.Range["A4", "A4"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            excells1.FormulaR1C1 = "Форма 3";

            excells1 = excelL.ExlWs.Range["A6", "A7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Поставщик к/услуг";

            excells1 = excelL.ExlWs.Range["B6", "B7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Виды услуг";

            excells1 = excelL.ExlWs.Range["C6", "C7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Площадь для расчетов изолированных квартир, м2";
            excells1.ColumnWidth = 13;

            excells1 = excelL.ExlWs.Range["D6", "D7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Площадь для расчетов коммунальных квартир, м2";
            excells1.ColumnWidth = 12;

            excells1 = excelL.ExlWs.Range["E6", "E7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Проживает, чел";
            excells1.ColumnWidth = 8.43;

            excells1 = excelL.ExlWs.Range["F6", "F7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Количество лицевых счетов";
            excells1.ColumnWidth = 9;

            excells1 = excelL.ExlWs.Range["G6", "G7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Ед. измерения";
            excells1.ColumnWidth = 9;

            excells1 = excelL.ExlWs.Range["H6", "H7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Натуральный показатель";
            excells1.ColumnWidth = 8.4;

            excells1 = excelL.ExlWs.Range["I6", "I7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Ед. измерения";
            excells1.ColumnWidth = 9;

            excells1 = excelL.ExlWs.Range["J6", "J7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Натуральный показатель";
            excells1.ColumnWidth = 8.4;

            excells1 = excelL.ExlWs.Range["K7", "K7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Постоянные начисления";

            excells1 = excelL.ExlWs.Range["L7", "L7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Возвраты/ доплаты";

            excells1 = excelL.ExlWs.Range["M7", "M7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Разовые корректировки ''красные''";

            excells1 = excelL.ExlWs.Range["N7", "N7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Разовые корректировки ''черные''";

            excells1 = excelL.ExlWs.Range["O7", "O7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Субсидии/ списано сальдо";

            excells1 = excelL.ExlWs.Range["P7", "P7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Итого к оплате";

            excells1 = excelL.ExlWs.Range["Q6", "Q7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Оплачено, руб";

            excells1 = excelL.ExlWs.Range["R6", "R7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "% оплаты";

            excells1 = excelL.ExlWs.Range["K6", "P6"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Начислено,руб";


            excells1 = excelL.ExlWs.Range["A6", "R7"];
            excells1.Font.Size = 8;
            excells1.WrapText = true;
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

            #endregion

            #region Пишем тело

            int excelRow = 8;
            int i = 0;

            while (i < DT.Rows.Count - 1)
            {
                decimal sumCharge = 0;
                decimal sumMoney = 0;
                decimal rsumTarif = 0;
                decimal sumNedop = 0;
                decimal revalK = 0;
                decimal revalD = 0;
       
                int startRow = excelRow;

                string oldSupp = DT.Rows[i]["name_supp"].ToString().Trim();
                string newSupp = oldSupp;
                while ((i < DT.Rows.Count) & String.Equals(oldSupp, newSupp))
                {
                    excelL.ExlWs.Cells[excelRow, 2] = DT.Rows[i]["service"].ToString().Trim();
                    if ((int) DT.Rows[i]["ord_serv"] != 515)
                    {
                        excelL.ExlWs.Cells[excelRow, 3] = Decimal.Parse(DT.Rows[i]["isol_pl"].ToString());
                        excelL.ExlWs.Cells[excelRow, 4] = Decimal.Parse(DT.Rows[i]["komm_pl"].ToString());
                        excelL.ExlWs.Cells[excelRow, 5] = Decimal.Parse(DT.Rows[i]["count_gil"].ToString());
                        //   ExcelL.ExlWs.Cells[ExcelRow, 6] = Decimal.Parse(DT.Rows[i]["count_ls"].ToString());
                        // ExcelL.ExlWs.Cells[ExcelRow, 7] = DT.Rows[i]["measure"].ToString().Trim();
                        // ExcelL.ExlWs.Cells[ExcelRow, 8] = Decimal.Parse(DT.Rows[i]["rashod"].ToString());
                    }
                    else
                    {
                        if (i > 0)
                            if ((int) DT.Rows[i - 1]["ord_serv"] == 25)
                            {
                                excells1 = excelL.ExlWs.Range["C" + (excelRow - 1), "C" + excelRow];
                                excells1.Merge(Type.Missing);
                                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1 = excelL.ExlWs.Range["D" + (excelRow - 1), "D" + excelRow];
                                excells1.Merge(Type.Missing);
                                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1 = excelL.ExlWs.Range["E" + (excelRow - 1), "E" + excelRow];
                                excells1.Merge(Type.Missing);
                                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                            }

                    }

                    excelL.ExlWs.Cells[excelRow, 6] = Decimal.Parse(DT.Rows[i]["count_ls"].ToString());
                    excelL.ExlWs.Cells[excelRow, 7] = DT.Rows[i]["measure"].ToString().Trim();
                    excelL.ExlWs.Cells[excelRow, 8] = Decimal.Parse(DT.Rows[i]["rashod"].ToString());

                    if (Decimal.Parse(DT.Rows[i]["rashod_gkal"].ToString()) != 0)
                    {
                        excelL.ExlWs.Cells[excelRow, 9] = "ГКал";
                        excelL.ExlWs.Cells[excelRow, 10] = Decimal.Parse(DT.Rows[i]["rashod_gkal"].ToString());
                    }
                    excelL.ExlWs.Cells[excelRow, 11] = Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                    excelL.ExlWs.Cells[excelRow, 12] = Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                    excelL.ExlWs.Cells[excelRow, 13] = Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                    excelL.ExlWs.Cells[excelRow, 14] = Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                    excelL.ExlWs.Cells[excelRow, 15] = 0;
                    excelL.ExlWs.Cells[excelRow, 16] = Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                    var sM = Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                    excelL.ExlWs.Cells[excelRow, 17] = sM;
                    var sumCh = (DT.Rows[i]["sum_charge"] != DBNull.Value
                        ? Convert.ToDecimal(DT.Rows[i]["sum_charge"])
                        : 0);
                    if (sumCh != 0)
                    {
                        excelL.ExlWs.Cells[excelRow, 18] = ((Convert.ToDecimal(DT.Rows[i]["sum_money"]))/sumCh*100);
                    }
                    else
                    {
                        excelL.ExlWs.Cells[excelRow, 18] = 0;
                    }

                    rsumTarif = rsumTarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                    sumNedop = sumNedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                    revalK = revalK + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                    revalD = revalD + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                    sumCharge = sumCharge + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                    sumMoney = sumMoney + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());

                    excelRow++;

                    i++;
                    newSupp = i < DT.Rows.Count ? DT.Rows[i]["name_supp"].ToString().Trim() : "";
                }

                excelL.ExlWs.Cells[excelRow, 2] = "Итого";
                excelL.ExlWs.Cells[excelRow, 11] = rsumTarif;
                excelL.ExlWs.Cells[excelRow, 12] = sumNedop;
                excelL.ExlWs.Cells[excelRow, 13] = revalK;
                excelL.ExlWs.Cells[excelRow, 14] = revalD;
                excelL.ExlWs.Cells[excelRow, 15] = 0;
                excelL.ExlWs.Cells[excelRow, 16] = sumCharge;
                excelL.ExlWs.Cells[excelRow, 17] = sumMoney;

                excells1 = excelL.ExlWs.Range["B" + excelRow, "R" + excelRow];
                excells1.Font.Bold = true;

                //Поставщик
                excells1 = excelL.ExlWs.Range["A" + startRow, "A" + excelRow];
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = oldSupp;
                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                excelRow++;

            }

            #endregion

            #region Пишем подвал

            if (excelRow > 6)
            {
                excelRow--;
                excells1 = excelL.ExlWs.Range["K7", "R" + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.Font.Size = 8;
                excells1.EntireColumn.AutoFit();

                excells1 = excelL.ExlWs.Range["A6", "R" + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;


                excells1 = excelL.ExlWs.Range["A5", "B" + excelRow];
                excells1.Font.Size = 8;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.EntireColumn.AutoFit();

                excells1 = excelL.ExlWs.Range["C7", "J" + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.Font.Size = 8;

                excells1 = excelL.ExlWs.Range["C7", "R" + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.EntireColumn.NumberFormat = "# ##0,00";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


                excells1 = excelL.ExlWs.Range["G7", "G" + excelRow];
                excells1.EntireColumn.NumberFormat = "@";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


                excells1 = excelL.ExlWs.Range["E7", "E" + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.EntireColumn.NumberFormat = "# ##0";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


                excells1 = excelL.ExlWs.Range["F7", "F" + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.EntireColumn.NumberFormat = "# ##0";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                excells1.Font.Size = 8;

                excells1 = excelL.ExlWs.Range["R7", "R" + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.EntireColumn.NumberFormat = "# ##0,00";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                excells1 = excelL.ExlWs.Range["J7", "J" + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.EntireColumn.NumberFormat = "# ##0,0000";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


            }


            SetReportSing(excelL, excelRow + 3, false);


            #endregion
        }


        public Returns GetSpravSuppNach(List<Prm> listprm, string nzpUser, ref string fileName)
        {
            Returns ret;


            //создание dataTable
            var exR = new ExcelRep();
            var dt = exR.GetSpravSuppNach(listprm, out ret, nzpUser);

            ExcelLoader excelL = null;
            try
            {
                excelL = new ExcelLoader();
                excelL.ExlWs.Rows.Font.Name = "Arial";
                if (dt != null)
                {



                    if (dt.Rows.Count > 0)
                    {
                        DataRow[] drSelect = dt.Select("tip = 1");

                        DataTable dtSelect = drSelect.CopyToDataTable();
                        if (dtSelect.Rows.Count > 0)
                            MakeListSpravSuppNach(dtSelect, excelL, 1, listprm[0], nzpUser);
                        dtSelect.Clear();
                        drSelect = dt.Select("tip = 2");

                        var oldGeu = "";
                        int j = 2;
                        foreach (DataRow dr in drSelect)
                        {
                            if (oldGeu != dr["geu"].ToString())
                            {
                                oldGeu = dr["geu"].ToString();
                                DataRow[] drSelect2 = dt.Select("geu = '" + oldGeu + "'");
                                dtSelect = drSelect2.CopyToDataTable();
                                if (dtSelect.Rows.Count > 0)
                                {
                                    MakeListSpravSuppNach(dtSelect, excelL, j, listprm[0], nzpUser);
                                    dtSelect.Clear();
                                    j++;
                                }
                            }
                        }

                        excelL.SortWs();
                        ((Worksheet)excelL.ExlWb.Sheets[1]).Select(Type.Missing);
                    }

                    #region Сохраняем файл

                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNach_" +
                                                                                       listprm[0].year_ + "_" +
                                                                                       listprm[0].month_
                                                                                       + "_" +
                                                                                       DateTime.Now.GetHashCode() +
                                                                                       nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName,
                            ref excelL.ExlWb, ref excelL.ExlApp);
                        excelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                        excelL.ExlWb = null;
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSuppNach DataTable : ОШИБКА!";

                    fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNach_" +
                                                                      listprm[0].year_ + "_" +
                                                                      listprm[0].month_
                                                                      + "_" + nzpUser) + ".xls";
                    ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName,
                        ref excelL.ExlWb, ref excelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (excelL != null)
                    {
                        fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNach" +
                                                                                       listprm[0].nzp_area +
                                                                                       "_" +
                                                                                       listprm[0].nzp_geu +
                                                                                       "_" +
                                                                                       listprm[0].year_ + "_" +
                                                                                       listprm[0].month_
                                                                                       + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName,
                            ref excelL.ExlWb, ref excelL.ExlApp);
                        excelL.ExlWb.Close();
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет SpravSuppNach", MonitorLog.typelog.Warn,
                        true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSuppNach Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (excelL != null)
                {
                    excelL.DeleteObject();
                }
            }

            return ret;

        }

    }
}
