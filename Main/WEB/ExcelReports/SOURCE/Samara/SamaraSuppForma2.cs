using System;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.Interfaces;
using DataTable = System.Data.DataTable;

namespace STCLINE.KP50.REPORT
{
    public partial class ReportGen
    {


        public void MakeListSpravSuppNach(DataTable dt, ExcelLoader excelL, int numList, Prm prm, string nzpUser)
        {
            excelL.GetWs(numList, "Лист " + numList);
            #region Создаем шапку

            var exform = new ExcelFormater();

            //спустили шапку
            Range excells1 = excelL.ExlWs.Range["A1", "D1"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "СПРАВКА ПО ПОСТАВЩИКАМ КОММУНАЛЬНЫХ УСЛУГ";
            excells1.Font.Bold = true;
            excells1.HorizontalAlignment =Constants.xlCenter;

            excells1 = excelL.ExlWs.Range["A2", "D2"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            //string ERC_name = "";

            //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
            //    ERC_name = "Н-ска";
            //else
            //    ERC_name = "Самары";

            //excells1.FormulaR1C1 = "по МП городского округа " + ERC_name + " ''ЕИРЦ'' за " +
            string sName = SetReportHeader();
            excells1.FormulaR1C1 = sName + " за " +
                prm.month_.ToString("00") + " " + prm.year_ + "г.";
            excells1.HorizontalAlignment = Constants.xlCenter;

            excells1 = excelL.ExlWs.Range["A3", "A3"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            excells1.FormulaR1C1 = DateTime.Today.ToShortDateString();
            excells1.HorizontalAlignment = Constants.xlLeft;


            excells1 = excelL.ExlWs.Range["B3", "D4"];
            excells1.Font.Bold = true;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(nzpUser));
            excells1.HorizontalAlignment = Constants.xlLeft;
            excells1.RowHeight = 23;


            excells1 = excelL.ExlWs.Range["B5", "K5"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "";
            if (numList > 1)
            {
                excells1.FormulaR1C1 = "ЖЭУ: " + dt.Rows[0]["geu"];
            }
            excells1.HorizontalAlignment = Constants.xlLeft;
            excells1.Font.Bold = true;

            excells1 = excelL.ExlWs.Range["A6", "A7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Поставщик к/услуг";
            excells1.HorizontalAlignment = Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;

            excells1 = excelL.ExlWs.Range["B6", "B7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Виды услуг";
            excells1.HorizontalAlignment = Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;

            excells1 = excelL.ExlWs.Range["C6", "C7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Итого к оплате";
            excells1.HorizontalAlignment = Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;

            excells1 = excelL.ExlWs.Range["D6", "D7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Оплачено " + Convert.ToChar(10) + " жильцами";
            excells1.HorizontalAlignment = Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;

            int colIndex = 5;

            for (int j = 0; j < dt.Columns.Count; j++)
                if (dt.Columns[j].ColumnName.IndexOf("kod", StringComparison.Ordinal) > -1)
                {
                    int kod = Convert.ToInt32(dt.Columns[j].ColumnName.Replace("kod", "").Trim());
                    string schetKind = kod > 1000 ? "c/счет" : "р/счет";
                    if (kod > 1000) kod = kod / 100;
                    string nameKod;
                    switch (kod)
                    {
                        case 40: nameKod = "р/счет УК"; break;
                        case 407: nameKod = schetKind + " ЕИРЦ"; break;
                        default: nameKod = schetKind + " " + kod; break;
                    }

                    excells1 = excelL.ExlWs.Range[exform.BukvaList[colIndex - 1] + "7", exform.BukvaList[colIndex - 1] + "7"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "'" + nameKod;
                    excells1.HorizontalAlignment = Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.Font.Size = 8;
                    colIndex++;
                }

            excells1 = excelL.ExlWs.Range["E6", exform.BukvaList[colIndex - 2] + "6"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = " Расчетные счета";
            excells1.HorizontalAlignment = Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excelL.ExlWs.PageSetup.PrintTitleRows = "$6:$7";


            var sumPrih = new decimal[colIndex - 5];

            #endregion

            #region Пишем тело

            int excelRow = 8;
            int i = 0;

            while (i < dt.Rows.Count)
            {
                decimal sumCharge = 0;
                decimal sumMoney = 0;
                int startRow = excelRow;

                //if (old_supp == "") old_supp = DT.Rows[i]["name_supp"].ToString().Trim();
                string oldSupp = dt.Rows[i]["name_supp"].ToString().Trim();
                bool newSupplier = false;
                while ((i < dt.Rows.Count) & (!newSupplier))
                {
                    excelL.ExlWs.Cells[excelRow, 2] = dt.Rows[i]["service"].ToString().Trim();
                    excelL.ExlWs.Cells[excelRow, 3] = Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                    excelL.ExlWs.Cells[excelRow, 4] = Decimal.Parse(dt.Rows[i]["sum_prih"].ToString());
                    colIndex = 5;
                    for (int j = 0; j < dt.Columns.Count; j++)
                        if (dt.Columns[j].ColumnName.IndexOf("kod", StringComparison.Ordinal) > -1)
                        {
                            excelL.ExlWs.Cells[excelRow, colIndex] = dt.Rows[i][j];
                            if (dt.Rows[i][j].ToString() != "")
                                sumPrih[colIndex - 5] += (decimal)dt.Rows[i][j];
                            colIndex++;
                        }

                    sumCharge = sumCharge + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                    sumMoney = sumMoney + Decimal.Parse(dt.Rows[i]["sum_prih"].ToString());

                    excelRow++;
                    i++;
                    if (i < dt.Rows.Count) newSupplier = oldSupp != dt.Rows[i]["name_supp"].ToString().Trim();
                    else newSupplier = true;

                }

                excelL.ExlWs.Cells[excelRow, 2] = "Итого";
                excelL.ExlWs.Cells[excelRow, 3] = sumCharge;
                excelL.ExlWs.Cells[excelRow, 4] = sumMoney;
                colIndex = 5;
                for (int j = 0; j < dt.Columns.Count; j++)
                    if (dt.Columns[j].ColumnName.IndexOf("kod", StringComparison.Ordinal) > -1)
                    {
                        excelL.ExlWs.Cells[excelRow, colIndex] = sumPrih[colIndex - 5];
                        sumPrih[colIndex - 5] = 0;
                        colIndex++;
                    }
                excells1 = excelL.ExlWs.Range["B" + excelRow, exform.BukvaList[dt.Columns.Count - 3] + excelRow];
                excells1.Font.Bold = true;

                //Поставщик
                excells1 = excelL.ExlWs.Range["A" + startRow, "A" + excelRow];
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = oldSupp;
                excells1.VerticalAlignment = Constants.xlTop;
                excells1.HorizontalAlignment = Constants.xlLeft;

                excelRow++;

                //if (i == dt.Rows.Count - 1)
                //{
                //    excelL.ExlWs.Cells[excelRow, 2] = dt.Rows[i]["service"].ToString().Trim();
                //    excelL.ExlWs.Cells[excelRow, 3] = Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                //    excelL.ExlWs.Cells[excelRow, 4] = Decimal.Parse(dt.Rows[i]["sum_prih"].ToString());
                //    colIndex = 5;
                //    for (int j = 0; j < dt.Columns.Count; j++)
                //        if (dt.Columns[j].ColumnName.IndexOf("kod", StringComparison.Ordinal) > -1)
                //        {
                //            excelL.ExlWs.Cells[excelRow, colIndex] = dt.Rows[i][j];
                //            if (dt.Rows[i][j].ToString() != "")
                //                sumPrih[colIndex - 5] += (decimal)dt.Rows[i][j];
                //            colIndex++;
                //        }

                //    sumCharge = sumCharge + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                //    sumMoney = sumMoney + Decimal.Parse(dt.Rows[i]["sum_prih"].ToString());
                //    excelRow++;
                //}

              

            }
            #endregion

            #region Пишем подвал

            if (excelRow > 6)
            {
                excelRow--;
                excells1 = excelL.ExlWs.Range["A7", exform.BukvaList[colIndex - 2] + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.Font.Size = 8;
                excells1.EntireColumn.AutoFit();

                excells1 = excelL.ExlWs.Range["C7", exform.BukvaList[colIndex - 2] + excelRow];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.EntireColumn.NumberFormat = "# ##0,00";
                excells1.HorizontalAlignment = Constants.xlRight;

            }


            SetReportSing(excelL, excelRow + 3, false);

            #endregion
        }

     


    }
}
