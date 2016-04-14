using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Excel = Microsoft.Office.Interop.Excel;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections;

namespace STCLINE.KP50.REPORT
{
    //Класс для задания форматирования в Excel документе
    public class ExcelFormater
    {
        public ExcelFormater()//Excel.Worksheet ExlWs)
        {
            //this.WorkSheet = ExlWs;
            MakeBukvaList(256);
        }

        public Excel.Range ExlRange;
        //public Excel.Worksheet WorkSheet;

        public string[] BukvaList;//= new string[] {"A","B","C","D","E","F","G","H","I","J","K","L","M","N",
        // "O","P","Q","R","S","T","U","V","W","X","Y","Z","AA","AB","AC",
        // "AD","AE","AF" , "AG","AH","AI", "AJ", "AK" , "AL", "AM" , "AN",
        // "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV", "AW", "AX", "AY", "AZ",
        // "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BK",
        // "BL", "BM", "BN", "BO", "BP","BQ", "BR", "BS", "BT", "BU", "BV", "BW",
        // "BX", "BY", "BZ"
        //};

        public string[] FormatList = new string[] { "date","float", "int", "sprav", "spr", "char", "bool", 
                                                    "adr_ulica", "adr_kvar", "adr_geu", "adr_area","" };

        public static string[] months = new string[] {"","Январь","Февраль",
                                        "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                                        "Октябрь","Ноябрь","Декабрь"};

        //Форматирование данных в столбце
        //Объединить с выравниванием
        //Decemila(денежный) - 1, дата - 2, строковый - 3, числовой - 4
        public Returns AddFormatColumn(string[] Col, int rowsLength, int HeaderHeigth, int format, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                for (int i = 0; i < Col.Length; i++)
                {
                    int pos = HeaderHeigth + 1;
                    int endPos = rowsLength + HeaderHeigth;

                    Excel.Range FormatRange = CurrentWorkSheet.get_Range(Col[i] + pos, Col[i] + endPos);
                    //Формат данных в столбце
                    switch (format)
                    {
                        //date
                        case 0:
                            {
                                //FormatRange.NumberFormat = "Д ММММ, ГГГГ"; //пример: 31 Январь, 2012
                                FormatRange.NumberFormat = "дд.мм.гггг";
                                break;
                            }


                        //float
                        case 1:
                            {
                                FormatRange.NumberFormat = "0.00";
                                FormatRange.HorizontalAlignment = Excel.Constants.xlRight;
                                break;
                            }
                        //int
                        case 2:
                            {
                                FormatRange.NumberFormat = "0";
                                FormatRange.HorizontalAlignment = Excel.Constants.xlRight;
                                break;
                            }
                        //sprav
                        case 3:
                            {
                                break;
                            }
                        //spr
                        case 4:
                            {
                                break;
                            }
                        //char
                        case 5:
                            {
                                FormatRange.NumberFormat = "@";//"Общий";
                                break;
                            }
                        //bool
                        case 6:
                            {

                                break;
                            }
                        //adr_ulica
                        case 7:
                            {

                                break;
                            }
                        //adr_kvar
                        case 8:
                            {

                                break;
                            }
                        //adr_geu
                        case 9:
                            {

                                break;
                            }
                        //adr_area
                        case 10:
                            {

                                break;
                            }
                        //пустой формат
                        case 11:
                            {

                                break;
                            }
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при применении формата : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = true;

                return ret;
            }

            return ret;
        }


        //Процедура вырвнивания
        //центр - 1, левый край - 2, правый край - 3
        public Returns AddAlignment(string[] Col, int rowsLength, int alignment, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                for (int i = 0; i < Col.Length; i++)
                {
                    Excel.Range FormatRange = CurrentWorkSheet.get_Range(Col[i] + 1, Col[i] + rowsLength);
                    //Вырвниывние данных в столбце
                    switch (alignment)
                    {
                        case 1:
                            {
                                FormatRange.HorizontalAlignment = Excel.Constants.xlCenter;
                                break;
                            }
                        case 2:
                            {
                                FormatRange.HorizontalAlignment = Excel.Constants.xlLeft;
                                break;
                            }
                        case 3:
                            {
                                FormatRange.HorizontalAlignment = Excel.Constants.xlRight;
                                break;
                            }
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при применении выравнивания : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = true;

                return ret;
            }



            return ret;
        }


        //Процедура вырвнивания(всех столбцов)
        //центр - 1, левый край - 2, правый край - 3
        public Returns AddAlignment(int ColCount, int rowsLength, int alignment, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                for (int i = 0; i < ColCount; i++)
                {
                    Excel.Range FormatRange = CurrentWorkSheet.get_Range(BukvaList[i] + 1, BukvaList[i] + rowsLength);
                    //Вырвниывние данных в столбце
                    switch (alignment)
                    {
                        case 1:
                            {
                                FormatRange.HorizontalAlignment = Excel.Constants.xlCenter;
                                break;
                            }
                        case 2:
                            {
                                FormatRange.HorizontalAlignment = Excel.Constants.xlLeft;
                                break;
                            }
                        case 3:
                            {
                                FormatRange.HorizontalAlignment = Excel.Constants.xlRight;
                                break;
                            }
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при применении выравнивания : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = true;

                return ret;
            }



            return ret;
        }

        //Выравнивание всех колонок по содержимому
        public Returns AllColumnsAutoFit(int ColCount, int rowsLength, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                for (int i = 0; i < ColCount; i++)
                {
                    Excel.Range AutoFitRange = CurrentWorkSheet.get_Range(BukvaList[i] + 1, BukvaList[i] + rowsLength);
                    AutoFitRange.EntireColumn.AutoFit();
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при выравнивании : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = true;

                return ret;
            }



            return ret;
        }


        //Выравнивание всех колонок по содержимому со смещением
        public Returns AllColumnsAutoFit(int ColCount, int OffsetRow, int rowsLength, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                for (int i = 0; i < ColCount; i++)
                {
                    Excel.Range AutoFitRange = CurrentWorkSheet.get_Range(BukvaList[i] + 1, BukvaList[i] + rowsLength + OffsetRow);
                    AutoFitRange.EntireColumn.AutoFit();
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при выравнивании : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = true;

                return ret;
            }



            return ret;
        }

        //Задание рамки для таблицы(левая верхняя ячейка - правая нижняя ячейка)
        //Добавить толщину рамки!!!!!!!!!!!!
        public Returns SetBorder(ref Excel.Worksheet WorkSheet, object cell1, object cell2)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ExlRange = WorkSheet.get_Range(cell1, cell2);
                ExlRange.BorderAround(Excel.XlLineStyle.xlContinuous, Excel.XlBorderWeight.xlThin, Excel.XlColorIndex.xlColorIndexAutomatic, Excel.XlColorIndex.xlColorIndexAutomatic);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка отрисовки рамки : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = true;

                return ret;
            }

            return ret;
        }

        //Применение заливки для ячеек
        //для столбцов
        public Returns SetColrCells(int ColCount, int rowsLength, int color, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                for (int i = 0; i < ColCount; i++)
                {
                    Excel.Range FormatRange = CurrentWorkSheet.get_Range(BukvaList[i] + 1, BukvaList[i] + rowsLength);
                    if (color != -1)
                    {
                        FormatRange.Interior.Color = color;
                    }
                    else
                    {
                        FormatRange.Interior.Color = Excel.Constants.xlGray50;//color;
                    }


                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при применении выравнивания : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = true;

                return ret;
            }



            return ret;
        }

        //Создает массив букв
        public void MakeBukvaList(int ColumnCount)
        {
            ArrayList bukvList = new ArrayList();
            for (int i = 0; i < ColumnCount; i++)
            {
                string str = "";
                if (i < 26)
                {
                    str = ((char)(i + 65)).ToString();
                }
                else
                {
                    int a = i / 26;
                    int b = 0;
                    Math.DivRem(i, 26, out b);
                    str = ((char)(a + 64)).ToString() + ((char)(b + 65)).ToString();
                }
                bukvList.Add(str);
            }
            string[] temp = new string[bukvList.Count];
            this.BukvaList = bukvList.ToArray(typeof(string)) as string[];
        }

        ////Задание рамки для каждой ячейки
        //public Returns SetBorderEachCell(ref Excel.Worksheet WorkSheet)
        //{

        //}


        //Вырвнивание столбца(ов) по содержимому
        public Returns ColumnsAutoFit(string[] Col, int rowsLength ,  ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                for (int i = 0; i < Col.Length; i++)
                {
                    Excel.Range AutoFitRange = CurrentWorkSheet.get_Range(Col[i] + 1, Col[i] + rowsLength);
                    AutoFitRange.EntireColumn.AutoFit();
                }
            }
            catch(Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при выравнивании : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = true;

                return ret;
            }
            

            return ret;
        }

        //процедура получения Prm по имени столбца
        public Prm GetPrmByColName(string colName, List<Prm> lprm)
        {
            foreach (Prm p in lprm)
            {
                if (colName == "val_" + p.nzp_prm)
                {
                    return p;
                }
            }
            return null;
        }

        //процедура замены точки на запятую
        public Returns ReplaceComma(string ColName, int rowsLength, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                for (int i = 1; i <= rowsLength; i++)
                {
                    if (CurrentWorkSheet.get_Range(ColName + i, ColName + i).Value2 != null)
                    {
                        CurrentWorkSheet.get_Range(ColName + i, ColName + i).Value2 = CurrentWorkSheet.get_Range(ColName + i, ColName + i).Value2.ToString().Replace('.', ',');
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка замены точки на запятую",MonitorLog.typelog.Error,true);
                ret.result = false;
                ret.text = ex.Message;
            }

            return ret;
        }

        //Процедура объединения строк в столбце с
        // c одинаковыми записями
        public Returns MergeCells(string ColName, int rowsLength, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                for (int i = 2; i <= rowsLength; i++)
                {
                    string st = CurrentWorkSheet.get_Range(ColName + i, Type.Missing).Value2.ToString();
                    int ind = i + 1;
                    while (st == CurrentWorkSheet.get_Range(ColName + ind, Type.Missing).Value2.ToString())
                    {
                        ind++;
                        if (ind > rowsLength)
                        {
                            break;
                        }
                    }
                    ind--;

                    Excel.Range mrenge = CurrentWorkSheet.get_Range(ColName + i, ColName + ind);
                    mrenge.Clear();
                    mrenge.Merge(Type.Missing);

                    mrenge.Value2 = st;

                    mrenge.VerticalAlignment = Excel.Constants.xlCenter;
                    mrenge.HorizontalAlignment = Excel.Constants.xlCenter;
                    i = ind;

                }
            }
            catch(Exception ex)
            {
                MonitorLog.WriteLog("Ошибка объединения MergeCells : " + ex.Message,MonitorLog.typelog.Error,true);
                ret.result = false;
                ret.text = ex.Message;
            }
            
            //Отрисовка последней линии
            CurrentWorkSheet.get_Range(ColName + rowsLength,Type.Missing).BorderAround(Excel.XlLineStyle.xlContinuous, Excel.XlBorderWeight.xlThin, Excel.XlColorIndex.xlColorIndexAutomatic, Excel.XlColorIndex.xlColorIndexAutomatic);

                return ret;
        }
    }
}
