using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;
using DataTable = System.Data.DataTable;
using System.Data;

namespace STCLINE.KP50.REPORT
{
    public class MakeSpravSoderg6
    {
        #region Адресные поля
        string oldAdres = "";
        string oldUlica = "";
        string oldDom = "";
        string ulica = "";
        string dom = "";
        string litera = "";
        string geu = "";
        #endregion

        string serviceName = "";

        List<Decimal> calcMas ; //Массив расходов
        List<Decimal> gilMas ; //Массив проживающих
        List<Decimal> plMas ; //Массив площадей
        List<Decimal> odnMas ; //Массив расходов ОДН

        ExcelLoader excelL;

        private List<string> rowHeaderList;
        private List<string> fieldList;


        /// <summary>
        /// Сформировать справку по начислению по виду услуг ХВ
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="nzpUser"></param>
        /// <param name="fileName"></param>
        /// <param name="repGen"></param>
        /// <returns></returns>
        public Returns GetSpravSoderg6(Prm prm, string nzpUser, ref string fileName, ReportGen repGen)
        {
            Returns ret;
            string nameSupp = "";
            
            System.Data.DataTable dt;

            //создание dataTable
            using (var exR = new ExcelRep())
            {
                dt = exR.GetSpravSoderg(prm, out ret, nzpUser);
            }

            if (dt == null)
            {
                MonitorLog.WriteLog("Пустая таблица с данными в функции GetSpravSoderg6," +
                                    " нет данных по начислениям по услуге ХВ ", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                return ret;
            }



            try
            {


                excelL = new ExcelLoader();



                #region Создаем шапку

                var dic = new Dictionary<string, string>
                {
                    {"ERCName", repGen.SetReportHeader()},
                    {"month", Utils.GetMonthName(prm.month_)},
                    {"year", prm.year_.ToString(CultureInfo.InvariantCulture)},
                    {"DATE", DateTime.Now.ToShortDateString()}
                };
                if (prm.has_pu == "1")
                {
                    dic.Add("ODPU", "(все дома)");
                }
                else if (prm.has_pu == "2")
                {
                    dic.Add("ODPU", "(дома с ОДПУ)");
                }
                else
                {
                    dic.Add("ODPU", "(дома без ОДПУ)");
                }

                serviceName = GetRepServName(prm.nzp_serv);
                dic.Add("SERVICE", serviceName);
                dic.Add("NAMESUPP", prm.nzp_key > -1 ? prm.name_prm : "Все поставщики");


                // Массив заголовков
                var columnHeaderList = new List<string>();
                columnHeaderList.Add("Начислено (тариф*кол-во), руб.");
                columnHeaderList.Add("Постоянное начисление, руб.");
                columnHeaderList.Add("Начислено ОДН, руб.");
                columnHeaderList.Add("Возврат за услуги, руб.");
                columnHeaderList.Add("Начислено раз. красным, руб.");
                columnHeaderList.Add("Начислено раз черным, руб.");
                columnHeaderList.Add("Итого к оплате, руб.");
                columnHeaderList.Add("Тариф поставщика, руб.");

                rowHeaderList = new List<string> {"жильцов, чел."};

                rowHeaderList.Add("Объем л/счетов, м3");
                rowHeaderList.Add("Объем ОДН, м3");
                rowHeaderList.Add("площадь, м2");

                fieldList = new List<string> {"rsum_tarif"};

                fieldList.Add("rsum_tarif_wodn");
                fieldList.Add("sum_odn");

                fieldList.Add("sum_nedop");
                fieldList.Add("reval_k");
                fieldList.Add("reval_d");
                fieldList.Add("sum_charge");
                fieldList.Add("tarif");

                fieldList.Add("c_nedop");
                fieldList.Add("c_reval_k");
                fieldList.Add("c_reval_d");



                var domMas = new decimal[fieldList.Count];
                for (int j = 0; j < fieldList.Count; j++) domMas[j] = 0;

                var itoMas = new decimal[fieldList.Count];
                for (int j = 0; j < fieldList.Count; j++) itoMas[j] = 0;


                #endregion






                excelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg.xls"), dic);
                var exform = new ExcelFormater();
                Range excells1 = excelL.ExlWs.Range["D3", "L4"];
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = repGen.SetReportConditional(Convert.ToInt32(nzpUser));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Font.Size = 8;
                excells1.RowHeight = 23;

                #region Пишем тело

                //Слепляем заголовок колонки параметров дома
                excells1 = excelL.ExlWs.Range["F6", "F7"];
                excells1.Merge(Type.Missing);
                excells1.Font.Size = 6;
                excells1.FormulaR1C1 = "";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excells1.WrapText = true;

                int excelRow = 8; //Начальная строка для вывода первого адреса

                int excelCol = 7; //Начальный столбец для вывода формул
                int countDom = 0;
                int maxExcelCol = 7;

                decimal revalOdndom = 0;
                decimal revalOdnall = 0;

                calcMas = new List<Decimal>(); //Массив расходов
                gilMas = new List<Decimal>(); //Массив проживающих
                plMas = new List<Decimal>(); //Массив площадей
                odnMas = new List<Decimal>(); //Массив расходов ОДН


                int i = 0;
                while (i < dt.Rows.Count)
                {

                    var adres = SetDomAdres(dt, i);

                    #region Записываем Итого по дому

                    if (oldAdres != adres)
                    {
                        countDom++;

                        FillDomSummary(excelRow, countDom, dt.Rows[i]["geu"].ToString().Trim(),
                            domMas, excelCol, exform);


                        for (int j = 0; j < fieldList.Count; j++) domMas[j] = 0;
                        revalOdndom = 0;

                        excelCol = 7;
                        oldAdres = adres;
                        oldUlica = ulica;
                        oldDom = dom;
                        excelRow += rowHeaderList.Count;


                    }

                    #endregion

                    //Проживающие
                    //Добавление в массив 0-х элементов, если размерность менее необходимой
                    if (gilMas.Count < excelCol + 1)
                    {
                        for (int k = gilMas.Count; k < excelCol + 1; k++) gilMas.Add(0);
                    }
                    gilMas[excelCol] = gilMas[excelCol] +
                                       GetGilCount(dt.Rows[i]["count_gil"].ToString(), excelRow, excelCol);

                    //Объем
                    if (calcMas.Count < excelCol + 1)
                    {
                        for (int k = calcMas.Count; k < excelCol + 1; k++) calcMas.Add(0);
                    }
                    calcMas[excelCol] = calcMas[excelCol] +
                                        GetServVolume(dt.Rows[i]["volume"].ToString(), excelRow, excelCol);

                    //ОДН
                    if (odnMas.Count < excelCol + 1)
                    {
                        for (int k = odnMas.Count; k < excelCol + 2; k++) odnMas.Add(0);
                    }
                    odnMas[excelCol] = odnMas[excelCol] +
                                       GetODNvolume(dt.Rows[i]["c_calc_odn"].ToString(), excelRow, excelCol);

                    //площадь
                    if (plMas.Count < excelCol + 1)
                    {
                        for (int k = plMas.Count; k < excelCol + 2; k++) plMas.Add(0);
                    }
                    plMas[excelCol] = plMas[excelCol] +
                                      GetServSquare(dt.Rows[i]["pl_kvar"].ToString(), excelRow, excelCol);

                    #region Пишем заголовок формул

                    if (excelRow == 8)
                    {
                        excelL.ExlWs.Cells[7, excelCol] = dt.Rows[i]["name_frm"].ToString();
                        excells1 =
                            excelL.ExlWs.Range[
                                exform.BukvaList[excelCol - 1] + "7", exform.BukvaList[excelCol - 1] + "7"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                        excells1.WrapText = true;
                        excells1.ColumnWidth = 10;
                        excells1.Font.Size = 6;
                    }

                    #endregion


                    AddtoDomMas(domMas, dt.Rows[i]);

                    AddToSummaryMas(itoMas, dt.Rows[i]);

                    revalOdnall = revalOdnall + Decimal.Parse(dt.Rows[i]["c_reval_odn"].ToString());
                    revalOdndom = revalOdndom + Decimal.Parse(dt.Rows[i]["c_reval_odn"].ToString());

                    excelCol++;
                    i++;

                }

             

                countDom++;
                if (dt.Rows.Count > 0) //Если в отчете есть данные, то дописываем последнюю строку
                {

                    oldUlica = ulica;
                    oldDom = dom;

                    FillDomSummary(excelRow, countDom, geu, domMas, excelCol, exform);

                    maxExcelCol = excelCol;

                    #region Пишем заголовки колонок

                    for (int k = 0; k < columnHeaderList.Count; k++)
                    {
                        excells1 = excelL.ExlWs.Range[exform.BukvaList[excelCol - 1 + k] + "6",
                            exform.BukvaList[excelCol - 1 + k] + "7"];
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = columnHeaderList[k];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                        excells1.WrapText = true;
                        excells1.ColumnWidth = 10;
                    }

                    #endregion

                    excells1 = excelL.ExlWs.Range["G6", exform.BukvaList[excelCol - 2] + "6"];
                    excells1.Merge(Type.Missing);
                    excells1.Font.Size = 6;
                    excells1.FormulaR1C1 = "Количество жильцов/нат.показатель";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.WrapText = true;

                }

                #endregion
                excelRow += rowHeaderList.Count;
                if (dt.Rows.Count > 0)
                {
                    excelRow = WriteReportSummary(excelRow, maxExcelCol, itoMas, excelCol, exform);

                    excelRow += rowHeaderList.Count - 1;

                    #region форматируем

                    excells1 =
                        excelL.ExlWs.Range["A6", exform.BukvaList[excelCol + columnHeaderList.Count - 2] + excelRow];
                    excells1.Font.Size = 6;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                 
                    excells1 =
                        excelL.ExlWs.Range["G8", exform.BukvaList[excelCol + columnHeaderList.Count - 2] + excelRow];
                    excells1.EntireColumn.NumberFormat = "# ##0.00#";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                    //excells1 =
                    //excelL.ExlWs.Range[exform.BukvaList[excelCol + columnHeaderList.Count - 2]+8
                    //, exform.BukvaList[excelCol + columnHeaderList.Count - 2] + excelRow];
                    //excells1.EntireColumn.NumberFormat = "# ##0,00#";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                    #endregion
                }

                repGen.SetReportSing(excelL, excelRow + 3, true);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.ToString(), MonitorLog.typelog.Error, true);
            }
            finally
            {
                //удаление объекта
                if (excelL != null)
                {
                    SaveReportToFile(prm, nzpUser, ref fileName, nameSupp, out ret);
                    excelL.DeleteObject();
                }
            }

            return ret;

        }

        private static string GetNameSupp(Prm prm, string nameSupp, string serviceName)
        {
            nameSupp = serviceName + "_" + nameSupp;
            if (prm.nzp_key != -1)
            {
                nameSupp = nameSupp + prm.name_prm;
            }
            nameSupp = nameSupp.Replace(">", " ").Replace("<", " ").Replace("?", " ").Replace("[", " ");
            nameSupp = nameSupp.Replace("]", " ")
                .Replace(":", " ")
                .Replace("|", " ")
                .Replace("*", " ")
                .Replace(@"\", " ");


            nameSupp = nameSupp.Replace(@"/", " ").Replace(" ", "_");
            return nameSupp;
        }


        /// <summary>
        /// Сохранение отчета в файл
        /// </summary>
        /// <param name="month_"></param>
        /// <param name="nzpUser"></param>
        /// <param name="fileName"></param>
        /// <param name="nameSupp"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private void SaveReportToFile(Prm prm, string nzpUser, ref string fileName, string nameSupp, out Returns ret)
        {
            GetNameSupp(prm, nameSupp, serviceName);
            fileName = prm.month_ + "_Дислокация_" + nameSupp + "_" + nzpUser;
            try
            {

                fileName = ReportGen.GetFileName(Global.Constants.ExcelDir, fileName) + ".xls";

                ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb,
                    ref excelL.ExlApp);

                if (InputOutput.useFtp)
                {
                    fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.ToString(), MonitorLog.typelog.Error, true);
                ret = new Returns(false);
            }
        }


        /// <summary>
        /// Пишем итоговую сумму по отчету 
        /// </summary>
        /// <param name="excelRow">Строка, с которой пишутся итоговые данные</param>
        /// <param name="maxExcelCol"></param>
        /// <param name="itoMas"></param>
        /// <param name="excelCol"></param>
        /// <param name="exform"></param>
        /// <returns></returns>
        private int WriteReportSummary(int excelRow, int maxExcelCol, decimal[] itoMas, int excelCol, ExcelFormater exform)
        {
            Range excells1;


      


            for (int j = 4; j < maxExcelCol; j++)
                excelL.ExlWs.Cells[excelRow, j] = gilMas[j];

            for (int j = 4; j < maxExcelCol; j++)
                excelL.ExlWs.Cells[excelRow + 1, j] = calcMas[j];

            for (int j = 4; j < maxExcelCol; j++)
                excelL.ExlWs.Cells[excelRow + 2, j] = odnMas[j];

            for (int j = 4; j < maxExcelCol; j++)
                excelL.ExlWs.Cells[excelRow + 3, j] = plMas[j];


            for (int j = 0; j < fieldList.Count - 4; j++)
            {
                excelL.ExlWs.Cells[excelRow, maxExcelCol + j] = itoMas[j];
            }

            //Объем по недопоставке
            int exCol = excelCol + fieldList.Count - 8;
            excelL.ExlWs.Cells[excelRow + 1, exCol] = itoMas[fieldList.Count - 3];


            //Объем по перерасчету -
            exCol = excelCol + fieldList.Count - 7;
            excelL.ExlWs.Cells[excelRow + 1, exCol] = itoMas[fieldList.Count - 2];


            //Объем по перерасчету +
            exCol = excelCol + fieldList.Count - 6;
            excelL.ExlWs.Cells[excelRow + 1, exCol] = itoMas[fieldList.Count - 1];

            for (int j = 0; j < fieldList.Count - 3; j++)
            {
                exCol = excelCol + j - 1;
                int exRow;
                if ((fieldList[j] == "sum_nedop") || (fieldList[j] == "reval_k") ||
                    (fieldList[j] == "reval_d"))
                    exRow = excelRow + 1;
                else exRow = excelRow;

                excells1 = excelL.ExlWs.Range[exform.BukvaList[exCol] +
                                              (exRow),
                    exform.BukvaList[exCol] +
                    (excelRow + rowHeaderList.Count - 1)];
                excells1.Merge(Type.Missing);
                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
            }


            excells1 = excelL.ExlWs.Range["A" + excelRow,
                "E" + (excelRow + rowHeaderList.Count - 1)];
            excells1.Merge(Type.Missing);
            excells1.Font.Size = 6;
            excells1.FormulaR1C1 = "Итого";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

            for (int k = 0; k < rowHeaderList.Count; k++)
            {
                excells1 = excelL.ExlWs.Range["F" + (excelRow + k), "F" + (excelRow + k)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.FormulaR1C1 = rowHeaderList[k];
                excells1.Font.Size = 6;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            }


            return excelRow;
        }


        /// <summary>
        /// Накапливает данные в итоговом массиве по отчету
        /// </summary>
        /// <param name="itoMas">Итоговый массив</param>
        /// <param name="dr">Строка таблицы данных</param>
        private void AddToSummaryMas(decimal[] itoMas, DataRow dr)
        {
            for (int k = 0; k < fieldList.Count; k++)
            {
                if (k != 7)
                {
                    itoMas[k] = itoMas[k] + Decimal.Parse(dr[fieldList[k]].ToString());
                }
                else
                {
                    if (Decimal.Parse(dr[fieldList[k]].ToString()) > 0)
                    {
                        itoMas[k] = Decimal.Parse(dr[fieldList[k]].ToString());
                    }
                }
            }
        }


        /// <summary>
        /// Добавляет значения в массив Итого по дому
        /// </summary>
        /// <param name="domMas">Массив значений, в котором считается итого по дому</param>
        /// <param name="dr">Строка из таблицы данных</param>
        private void AddtoDomMas(decimal[] domMas, DataRow dr)
        {
            for (int k = 0; k < fieldList.Count; k++)
            {
                if (k != 7) //Седьмая колонка Тариф, ее нельзя суммировать
                {
                    domMas[k] = domMas[k] + Decimal.Parse(dr[fieldList[k]].ToString());
                }
                else
                {
                    if (Decimal.Parse(dr[fieldList[k]].ToString()) > 0)
                    {
                        domMas[k] = Decimal.Parse(dr[fieldList[k]].ToString());
                    }
                }
            }
        }


        /// <summary>
        /// Получить объем ОДН по услуге
        /// </summary>
        /// <param name="plKvar">Площадь квартир по услуге</param>
        /// <param name="excelRow">Строка, в которой вывести площадь услуги</param>
        /// <param name="excelCol">Столбец, в котором вывести площадь услуги</param>
        private decimal GetServSquare(string plKvar, int excelRow, int excelCol)
        {
            decimal squareKvar = Decimal.Parse(plKvar);
            if (Math.Abs(squareKvar) > 0)
                excelL.ExlWs.Cells[excelRow + 3, excelCol] = squareKvar;
          
            return squareKvar;
        }


        /// <summary>
        /// Получить объем ОДН по услуге
        /// </summary>
        /// <param name="cCalcOdn">Объем по услуге ОДН</param>
        /// <param name="excelRow">Строка, в которой вывести объем услуги</param>
        /// <param name="excelCol">Столбец, в котором вывести объем услуги</param>
        private decimal GetODNvolume(string cCalcOdn, int excelRow, int excelCol)
        {
            decimal odnVolume = Decimal.Parse(cCalcOdn);
            if (Math.Abs(odnVolume) > 0)
                excelL.ExlWs.Cells[excelRow + 2, excelCol] = odnVolume;
            
            return odnVolume;
        }


        /// <summary>
        /// Получить объем по основной услуги 
        /// </summary>
        /// <param name="volume">Объем по основной услуге</param>
        /// <param name="excelRow">Строка, в которой вывести объем услуги</param>
        /// <param name="excelCol">Столбец, в котором вывести объем услуги</param>
        /// <returns></returns>
        private decimal GetServVolume(string volume, int excelRow, int excelCol)
        {
            decimal vol = Decimal.Parse(volume);
          
            if (volume.Trim() != "")
            {
                if (Math.Abs(vol) > 0.001m)
                {
                    excelL.ExlWs.Cells[excelRow + 1, excelCol] = vol;
                     
                }
            }
            return vol;
        }


        /// <summary>
        /// Получить количество жильцов в доме
        /// </summary>
        /// <param name="countGil">Количество проживающих</param>
        /// <param name="excelRow">Строка в которой вывести количество проживающих</param>
        /// <param name="excelCol">Столбец в котором вывести количество проживающих</param>
        /// <returns></returns>
        private Int32 GetGilCount(string countGil, int excelRow, int excelCol)
        {
            int cntGil = Int32.Parse(countGil);

            if (cntGil > 0)
                excelL.ExlWs.Cells[excelRow, excelCol] = countGil;
            
         
            return cntGil;
        }


        /// <summary>
        /// Заполнить итоговую строку по дому 
        /// </summary>
        /// <param name="excelRow">Строка, начала печати дома</param>
        /// <param name="countDom">Номер дома по порядку</param>
        /// <param name="geu">ЖЭУ</param>
        /// <param name="domMas">Массив итоговых данных для дома</param>
        /// <param name="excelCol">Столбец, с которого пишутся данные</param>
        /// <param name="exform"></param>
        private void FillDomSummary( int excelRow,  int countDom, string geu,
             decimal[] domMas, int excelCol, ExcelFormater exform)
        {
            Range excells1 = excelL.ExlWs.Range["A" + excelRow, "A" + (excelRow + rowHeaderList.Count - 1)];
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = countDom;


            excells1 = excelL.ExlWs.Range["B" + excelRow, "B" + (excelRow + rowHeaderList.Count - 1)];
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = geu;


            excells1 = excelL.ExlWs.Range["C" + excelRow, "C" + (excelRow + rowHeaderList.Count - 1)];
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = oldUlica;

            excells1 = excelL.ExlWs.Range["D" + excelRow, "D" + (excelRow + rowHeaderList.Count - 1)];
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = oldDom;

            excells1 = excelL.ExlWs.Range["E" + excelRow, "E" + (excelRow + rowHeaderList.Count - 1)];
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = litera;

            SetDomParams(excelRow);

            //Денежные суммы
            for (int j = 0; j < fieldList.Count - 3; j++) //////!!!!!!!!!!!!
            {
                if (Math.Abs(domMas[j]) > 0.001m)
                    excelL.ExlWs.Cells[excelRow, excelCol + j] = domMas[j];
            }

            //Объем по недопоставке
            int exCol = excelCol + fieldList.Count - 8;
            if (Math.Abs(domMas[fieldList.Count - 3]) > 0.001m)
                excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 3];


            //Объем по перерасчету -
            exCol = excelCol + fieldList.Count - 7;
            if (Math.Abs(domMas[fieldList.Count - 2]) > 0.001m)
                excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 2];


            //Объем по перерасчету +
            exCol = excelCol + fieldList.Count - 6;
            if (Math.Abs(domMas[fieldList.Count - 1]) > 0.001m)
                excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 1];


            //Для денежных колонок выполняем объединение строк, кроме колонок
            //с недопоставкой и корректировками, по ним деление другое, т.к. нужны объемы
            for (int j = 0; j < fieldList.Count - 3; j++)
            {
                exCol = excelCol + j - 1;
                int exRow;
                if ((fieldList[j] == "sum_nedop") || (fieldList[j] == "reval_k") ||
                    (fieldList[j] == "reval_d"))
                    exRow = excelRow + 1;
                else exRow = excelRow;

                excells1 = excelL.ExlWs.Range[exform.BukvaList[exCol] + exRow,
                    exform.BukvaList[exCol] +
                    (excelRow + rowHeaderList.Count - 1)];
                excells1.Merge(Type.Missing);
                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
            }
        }


        /// <summary>
        /// Устанавливает заголовки домовых параметров в строках
        /// </summary>
        /// <param name="excelRow"></param>
        private  void SetDomParams(int excelRow)
        {
            for (int k = 0; k < rowHeaderList.Count; k++)
            {
                Range excells1 = excelL.ExlWs.Range["F" + (excelRow + k), "F" + (excelRow + k)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = rowHeaderList[k];
            }
        }


        /// <summary>
        /// Получить адрес дома 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private string SetDomAdres(DataTable dt, int i)
        {
            string adres = dt.Rows[i]["ulica"].ToString().Trim() + "," +
                           dt.Rows[i]["ndom"].ToString().Trim() +
                           dt.Rows[i]["nkor"].ToString().Trim();
            geu = dt.Rows[i]["geu"].ToString().Trim();
            ulica = dt.Rows[i]["ulica"].ToString().Trim();
            dom = dt.Rows[i]["ndom"].ToString().Trim();
            litera = dt.Rows[i]["nkor"].ToString().Trim();

            if (i == 0)
            {
                oldAdres = adres;
                oldUlica = dt.Rows[i]["ulica"].ToString().Trim();
                oldDom = dt.Rows[i]["ndom"].ToString().Trim();
            }
            return adres;
        }


        /// <summary>
        /// Получение имени услуги по ее коду
        /// </summary>
        /// <param name="nzpServ">Услуга</param>
        /// <returns></returns>
        private string GetRepServName(int nzpServ)
        {
            string servName;
            using (var dbSprav = new DbSprav())
            {
                var finder = new Finder { nzp_user = nzpServ, RolesVal = new List<_RolesVal>() };
                var p = new _RolesVal
                {
                    kod = Global.Constants.role_sql_serv,
                    tip = Global.Constants.role_sql,
                    val = nzpServ.ToString(CultureInfo.InvariantCulture)
                };
                finder.RolesVal.Add(p);
                Returns ret;
                List<_Service> lserv = dbSprav.ServiceLoad(finder, out ret);
                servName = (lserv != null && lserv.Count > 0) ? lserv[0].service : "Услуга неопределена";
            }
            return servName;
        }
    }
}
