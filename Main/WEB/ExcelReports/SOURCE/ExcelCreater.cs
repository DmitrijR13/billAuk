using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data;

namespace STCLINE.KP50.REPORT
{
    //Класс для создания документа Excel
    public class ExcelCreater
    {
        //Конструктор
        public ExcelCreater()
        {

        }



        //Создание шапки
        public Returns MakeHeader(object[] ColumnsNames, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            //объект отвечаеющий за форматирование
            ExcelFormater excelFortmater = new ExcelFormater();//CurrentWorkSheet);

            //Ссылка на диапазон ячеек(или одну ячейку)
            Excel.Range excells;

            ////////////////////////////////////////////////
            ////////////////////////////////////////////////
            //Парсинг строк имен, опредеоение местоположения
            //управляющие символы :  '{' , '}' , '/', '|'
            ////////////////////////////////////////////////
            ////////////////////////////////////////////////

            try
            {
                //Вставка строк в ячейки
                excells = CurrentWorkSheet.get_Range("A1", "A1");

                for (int i = 0; i < ColumnsNames.Length; i++)
                {
                    excells.Value2 = ColumnsNames[i];
                    //рамка
                    excelFortmater.SetBorder(ref CurrentWorkSheet, excells, excells);
                    //жир
                    excells.Font.Bold = true;


                    excells = excells.get_Offset(0, 1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка создания шапки : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

                return ret;
            }


            return ret;
        }

        //Создание шапки(перегрузка - смещение)
        public Returns MakeHeader(object[] ColumnsNames, int BodyOffset_row, int BodyOffset_col, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            //объект отвечаеющий за форматирование
            ExcelFormater excelFortmater = new ExcelFormater();//CurrentWorkSheet);

            //Ссылка на диапазон ячеек(или одну ячейку)
            Excel.Range excells;

            ////////////////////////////////////////////////
            ////////////////////////////////////////////////
            //Парсинг строк имен, опредеоение местоположения
            //управляющие символы :  '{' , '}' , '/', '|'
            ////////////////////////////////////////////////
            ////////////////////////////////////////////////

            try
            {
                //Вставка строк в ячейки
                excells = CurrentWorkSheet.get_Range("A1", "A1");
                excells = excells.get_Offset(BodyOffset_row, BodyOffset_col);

                for (int i = 0; i < ColumnsNames.Length; i++)
                {
                    excells.Value2 = ColumnsNames[i];
                    //рамка
                    excelFortmater.SetBorder(ref CurrentWorkSheet, excells, excells);
                    //жир
                    excells.Font.Bold = true;


                    excells = excells.get_Offset(0, 1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка создания шапки : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

                return ret;
            }


            return ret;
        }

        //Создание Тела
        public Returns MakeBody(DataTable tableData, int BodyOffset_row, int BodyOffset_col, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();
            //объект отвечаеющий за форматирование
            ExcelFormater excelFortmater = new ExcelFormater();//CurrentWorkSheet);

            //Ссылка на диапазон ячеек(или одну ячейку)
            Excel.Range excells;
            Excel.Range temp_excells;

            try
            {
                //Вставка строк в ячейки
                excells = CurrentWorkSheet.get_Range("A1", "A1");
                //применение смещения
                excells = excells.get_Offset(BodyOffset_row, BodyOffset_col);
                temp_excells = excells;

                //применение культуры
                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                foreach (DataColumn dc in tableData.Columns)
                {
                    foreach (DataRow dr in tableData.Rows)
                    {
                        //if ((tableData.Rows.IndexOf(dr) == 168) && (tableData.Columns.IndexOf(dc) == 2))
                        //{
                        //Значение "Приватизировано" должно отображаться как Да/Нет
                        if (tableData.Columns[tableData.Columns.IndexOf(dc)].ColumnName == "val_8")
                        {
                            if (tableData.Rows[tableData.Rows.IndexOf(dr)][tableData.Columns.IndexOf(dc)].ToString().Trim() == "1")
                            {
                                excells.Value2 = "Да";
                            }
                            if (tableData.Rows[tableData.Rows.IndexOf(dr)][tableData.Columns.IndexOf(dc)].ToString().Trim() == "0")
                            {
                                excells.Value2 = "Нет";
                            }
                            if (tableData.Rows[tableData.Rows.IndexOf(dr)][tableData.Columns.IndexOf(dc)].ToString().Trim() == "")
                            {
                                excells.Value2 = "";
                            }
                            //excells.Value2 = tableData.Rows[tableData.Rows.IndexOf(dr)][tableData.Columns.IndexOf(dc)].ToString().Trim() == "1" ? "Да" : "Нет";
                            //рамка
                            excelFortmater.SetBorder(ref CurrentWorkSheet, excells, excells);
                            excells = excells.get_Offset(1, 0);
                            continue;
                        }

                        decimal fl;
                        bool ch = decimal.TryParse(tableData.Rows[tableData.Rows.IndexOf(dr)][tableData.Columns.IndexOf(dc)].ToString().Trim(), out fl);
                        if (ch)
                        {
                            excells.Value2 = fl;//tableData.Rows[tableData.Rows.IndexOf(dr)][tableData.Columns.IndexOf(dc)].ToString().Trim();
                        }
                        else
                        {
                            excells.Value2 = tableData.Rows[tableData.Rows.IndexOf(dr)][tableData.Columns.IndexOf(dc)].ToString().Trim();
                        }
                        //}
                        //excells.Value2 = tableData.Rows[tableData.Rows.IndexOf(dr)][tableData.Columns.IndexOf(dc)].ToString().Trim();
                        //рамка
                        excelFortmater.SetBorder(ref CurrentWorkSheet, excells, excells);
                        excells = excells.get_Offset(1, 0);
                    }
                    temp_excells = temp_excells.get_Offset(0, 1);
                    excells = temp_excells;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка создания тела : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

                return ret;
            }


            return ret;
        }

        //Создание подвала
        public Returns MakeFooter(object[] DataFooter, int BodyOffset_row, int BodyOffset_col, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();

            //объект отвечаеющий за форматирование
            ExcelFormater excelFortmater = new ExcelFormater();//CurrentWorkSheet);

            //Ссылка на диапазон ячеек(или одну ячейку)
            Excel.Range excells;

            try
            {
                //Вставка строк в ячейки
                excells = CurrentWorkSheet.get_Range("A1", "A1");
                //применение смещения
                excells = excells.get_Offset(BodyOffset_row, BodyOffset_col);

                for (int i = 0; i < DataFooter.Length; i++)
                {
                    excells.Value2 = DataFooter[i];
                    //рамка
                    excelFortmater.SetBorder(ref CurrentWorkSheet, excells, excells);

                    excells = excells.get_Offset(0, 1);
                }


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка создания подвала : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

                return ret;
            }



            return ret;
        }
        

        //создание названия отчета
        public Returns MakeName(string name, string position , int countColumn , int heigth ,  ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();

            //объект отвечаеющий за форматирование
            ExcelFormater excelFortmater = new ExcelFormater();
            try
            {
                //извлекаем цифру - смещение из позиции (например 20 из А20 )
                var num = (from c in position where c >= '0' && c <= '9' select c).ToList<char>();
                string numb = "";
                foreach (char c in num)
                {
                    numb += c;
                }
                string bukva = position.Substring(0, position.Length - numb.Length);
                int pos_bukva = Array.IndexOf(excelFortmater.BukvaList, bukva);
                //необходимое смещение
                int number = Convert.ToInt32(numb);



                //Ссылка на диапазон ячеек(или одну ячейку)
                //Excel.Range excells = CurrentWorkSheet.get_Range(position, excelFortmater.BukvaList[countColumn - 1] + heigth);
                Excel.Range excells = CurrentWorkSheet.get_Range(position, (excelFortmater.BukvaList[pos_bukva + countColumn - 1] + (heigth + number - 1)).ToString());
                excells.Merge(Type.Missing);

                excells.HorizontalAlignment = Excel.Constants.xlCenter;
                excells.VerticalAlignment = Excel.Constants.xlCenter;
                excells.Font.Size = 16;
                excells.Font.Bold = true;
                excells.BorderAround(Excel.XlLineStyle.xlContinuous, Excel.XlBorderWeight.xlMedium, Excel.XlColorIndex.xlColorIndexAutomatic, Excel.XlColorIndex.xlColorIndexAutomatic);
                excells.Value2 = name;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка создания названия отчета : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }



            return ret;
        }

        //создание названия отчета(перерузка)
        //1 - выравнивание по левому краю(по низу)
        //2 - выравнивание по ценру
        public Returns MakeName(string text, string position, int length, int heigth, bool bold, object allignment_hor, object allignment_vert, int font, ref Excel.Worksheet CurrentWorkSheet)
        {
            Returns ret = Utils.InitReturns();

            ExcelFormater excelFortmater = new ExcelFormater();
            try
            {
                //извлекаем цифру - смещение из позиции (например 20 из А20 )
                var num = (from c in position where c >= '0' && c < '9' select c).ToList<char>();
                string numb = "";
                foreach (char c in num)
                {
                    numb += c;
                }
                string bukva = position.Substring(0, position.Length - numb.Length);
                int pos_bukva = Array.IndexOf(excelFortmater.BukvaList, bukva);
                //необходимое смещение
                int number = Convert.ToInt32(numb);


                //Ссылка на диапазон ячеек(или одну ячейку)
                Excel.Range excells = CurrentWorkSheet.get_Range(position, (excelFortmater.BukvaList[pos_bukva + length - 1] + (heigth + number - 1)).ToString());
                excells.Merge(Type.Missing);
                excells.HorizontalAlignment = allignment_hor;
                excells.VerticalAlignment = allignment_vert;

                //excells.EntireColumn.AutoFit();

                excells.Font.Size = font;
                excells.Font.Bold = bold;

                excells.Value2 = text;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка создания названия отчета : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }


            return ret;
        }



    }
}
