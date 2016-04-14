using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using Excel = Microsoft.Office.Interop.Excel; 
using STCLINE.KP50.Global;
using System.Runtime.InteropServices;
using System.Collections;

using STCLINE.KP50.Interfaces;
using System.Text.RegularExpressions;
using System.IO;


namespace STCLINE.KP50.REPORT
{
    //Класс отвечающий за загрузку данных в Excel
    public class ExcelLoader
    {
        private string copyiedTemplate = String.Empty;
        //Приложение Excel
        public Excel.Application ExlApp;
        //
        public Excel.Workbook ExlWb;
        //
        public Excel.Worksheet ExlWs;
        //
        object misValue = System.Reflection.Missing.Value;

        //Конструктор
        public ExcelLoader()
        {
            //Создание сервера Excel
            this.ExlApp = new Microsoft.Office.Interop.Excel.Application();
            //отключить предупреждения
            ExlApp.DisplayAlerts = false;

            //Создание рабочей книги
            this.ExlWb = this.ExlApp.Workbooks.Add(misValue);
            //Получение ссылки на лист1
            this.ExlWs = (Excel.Worksheet)this.ExlWb.Worksheets.get_Item(1);
            this.ExlWs.Visible = Excel.XlSheetVisibility.xlSheetVisible;
        }

        //обновление страницы(случай переопределения книги)
        public void RefreshWs()
        { 
            this.ExlWs = (Excel.Worksheet)this.ExlWb.Worksheets.get_Item(1);
            this.ExlWs.Visible = Excel.XlSheetVisibility.xlSheetVisible;
        }

        //обновление страницы(случай переопределения книги)
        public void GetWs(int i, string supp_name)
        {
            //if(i < 4)
            if (i <= this.ExlWb.Worksheets.Count)
            {
                this.ExlWs = (Excel.Worksheet)this.ExlWb.Worksheets[i];
                CheckWsName(ref supp_name);
                this.ExlWs.Name = supp_name;
            }
            else
            {
                //создаем новую страницу
                this.ExlWs = (Excel.Worksheet)this.ExlWb.Worksheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                this.ExlWs = (Excel.Worksheet)this.ExlWb.Worksheets[1];
                CheckWsName(ref supp_name);
                this.ExlWs.Name = supp_name;
            }
        }

        //
        public void CheckWsName(ref string ws_name)
        {
            ws_name = Regex.Replace(ws_name, @"\s+", " ");

            if (ws_name.Length >= 31)
                ws_name = ws_name.Substring(0, 28) + "...";
            ws_name = ws_name.Replace('/', '_');
            ws_name = ws_name.Replace('\'', '_');
            ws_name = ws_name.Replace('[', '_');
            ws_name = ws_name.Replace(']', '_');
            ws_name = ws_name.Replace('*', '_');
            ws_name = ws_name.Replace('?', '_');
        }

        //сортировка старниц книги по названию
        public void SortWs()
        {
            List<Excel.Worksheet> sorted = new List<Excel.Worksheet>();

            foreach (Excel.Worksheet ws in this.ExlWb.Worksheets)
            {
                sorted.Add(ws);
            }

            //сортировка имен страниц книги
            sorted.Sort(delegate(Excel.Worksheet ws1, Excel.Worksheet ws2)
            {
                return ws1.Name.CompareTo(ws2.Name);
            });

            Excel.Worksheet action, wst;

            for (int i = 0; i < sorted.Count; i++)
            {
                for (int j = 1; j <= this.ExlWb.Worksheets.Count; j++)
                {
                    action = (Excel.Worksheet)this.ExlWb.Worksheets[j];
                    if (sorted[i].Name == action.Name)
                    {
                        wst = (Excel.Worksheet)this.ExlWb.Worksheets[i + 1];//сдвигаем на нужное место
                        action.Move(wst, Type.Missing);
                    }
                }
            }
            ((Microsoft.Office.Interop.Excel.Worksheet)this.ExlWb.Sheets[1]).Select(Type.Missing); 
        }


        //Загрузка DataTable в Excel
        public Returns Load_to_Excel(int NumberSheet, DataTable table_data, string[] HeaderData, object[] footer_data, string[] TypeHeader)// List<Prm> listprm)
        {
            Returns ret = Utils.InitReturns();


            //проверка на существования столбцов
            if (table_data.Columns.Count == 0)
            {
                ret.text = "Таблица не содержит столбцов!";
                ret.result = false;

                return ret;
            }

            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater Eformat = new ExcelFormater();//this.ExlWs);

            ////массив выравнивания
            //int[] AlignmentMas = null;


            //Получение страницы Excel
            try
            {
                ExlWs = (Excel.Worksheet)ExlWb.Worksheets.get_Item(NumberSheet);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("1" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

                return ret;
            }

           
            //Собрать страницу
            try
            {                              
                //Создаем шапку
                ExcelCr.MakeHeader(HeaderData, ref ExlWs);
                
                //Создаем тело               
                ExcelCr.MakeBody(table_data, 1, 0, ref ExlWs);
                
                //Создаем подвал
                if (footer_data != null)
                {
                    ExcelCr.MakeFooter(footer_data, table_data.Rows.Count + 1, 0, ref ExlWs);
                }

                //выравнивание по содержимому
                //Eformat.ColumnsAutoFit(new string[] {"A","B","C"}, table_data.Rows.Count + 2, ref ExlWs);
                Eformat.AllColumnsAutoFit(HeaderData.Length, table_data.Rows.Count + 2, ref ExlWs);

                //Выравнивание по центру                    
                Eformat.AddAlignment(HeaderData.Length, table_data.Rows.Count + 2, 1, ref ExlWs);
                
                //Применение формата                                   
                for (int i = 0; i < TypeHeader.Length; i++)
                {
                    if (TypeHeader[i] != null)
                    {
                        Eformat.AddFormatColumn(new string[] { Eformat.BukvaList[i] }, table_data.Rows.Count + 1, 1,Array.IndexOf(Eformat.FormatList, TypeHeader[i]), ref ExlWs);
                        //замена точки на запятую
                        if (TypeHeader[i] == "float")
                        {
                            //Eformat.ReplaceComma(Eformat.BukvaList[i], table_data.Rows.Count + 1, ref ExlWs);
                        }

                    }
                }

                //заливка заголовков
                Eformat.SetColrCells(HeaderData.Length, 1, 5296274, ref ExlWs);


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("2.Ошибка заполнения: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

                return ret;
            }
       
            return ret;
        }

        //Сохранение файла на диск
        public Returns SaveFile(string path, string FileName, ref Excel.Workbook CurrentWorkBook, ref Excel.Application CurrentExcellApp)
        {
            Returns ret = Utils.InitReturns();
            //Устанавливаем формат
            CurrentExcellApp.DefaultSaveFormat = Excel.XlFileFormat.xlWorkbookNormal;
            //???
            CurrentWorkBook.Saved = true;
            //Не Отображать сообщение о замене существующего
            CurrentExcellApp.DisplayAlerts = false;
            //Формат сохраняемого файла
            //CurrentExcellApp.DefaultSaveFormat = Excel.XlFileFormat.xlExcel9795;

            try
            {
                CurrentWorkBook.SaveAs(path, Excel.XlFileFormat.xlWorkbookNormal, //Excel.XlFileFormat.xlExcel9795,
                                        Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                CurrentWorkBook.Save();
            }
            catch(Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при сохранения документа : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

                return ret;
            }

            CurrentWorkBook.Close();

            return ret;
        }


        //Поцедура трансформирующая коллекцию в DataTable
        public DataTable TransformColToDT(out Returns ret, object[] InputCollect, ArrayList ColumnFilter)
        {
            ret = Utils.InitReturns();

            DataTable Dt = new DataTable();

            try
            {
                //создание колонок
                if (InputCollect.Length > 0)
                {
                    var firstElem = InputCollect[0];
                    foreach (var Prop in firstElem.GetType().GetProperties())
                    {
                        if (ColumnFilter != null)
                        {
                            if (ColumnFilter.Contains(Prop.Name))
                            {
                                Dt.Columns.Add(Prop.Name);
                            }
                        }
                        //Если Фильтр отключен
                        else
                        {
                            Dt.Columns.Add(Prop.Name);
                        }
                    }
                }
                else
                {
                    return null;
                }

                foreach (var Ob in InputCollect)
                {
                    //object[] mas = new object[Ob.GetType().GetProperties().Length];
                    object[] mas = new object[Dt.Columns.Count];
                    
                    int i = 0;
                    foreach (var Prop in Ob.GetType().GetProperties())
                    {
                        if (ColumnFilter != null)
                        {
                            if (ColumnFilter.Contains(Prop.Name) && (i == Dt.Columns.IndexOf(Prop.Name)))
                            {
                                mas[i] = Prop.GetValue(Ob, null);
                                i++;
                            }
                        }
                            //Если Фильтр отключен
                        else
                        {
                            if (i == Dt.Columns.IndexOf(Prop.Name))
                            {
                                mas[i] = Prop.GetValue(Ob, null);
                                i++;
                            }
                        }
                    }
                    Dt.Rows.Add(mas);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка трансформации коллекции в DataTable : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

                return null;
            }


            return Dt;
        }

        //Удаление объектов
        public void DeleteObject()//(ref Excel.Application ExlApp, ref Excel.Workbook ExlWb, ref Excel.Worksheet ExlWs)
        {

            if (!String.IsNullOrEmpty(copyiedTemplate))
            {
                try
                {
                    File.Delete(copyiedTemplate);
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка удаления файла" + copyiedTemplate + " файл занят другим процессом", MonitorLog.typelog.Warn, true);
                }
            }


            try
            {
                if (ExlWb != null)
                {
                    Marshal.ReleaseComObject(ExlWs);
                    ExlWb.Close(false, Type.Missing, Type.Missing);
                    //ExlWb.Close(Type.Missing, Type.Missing, Type.Missing);
                    Marshal.ReleaseComObject(ExlWb);
                    ExlWb = null;
                }
                ///////////////////////////////
                //добавить какой нибудь рестарт
                ///////////////////////////////
                ExlApp.Quit();
                Marshal.ReleaseComObject(ExlApp);
                ExlApp = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
       
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("!!!" + ex.Message,MonitorLog.typelog.Error,true);
            }         
        }

        ////Отображение Excel
        //public Returns ShowExcel(Excel.Application ExlApp)
        //{
        //    Returns ret = Utils.InitReturns();
        //    //Отображение
        //    try
        //    {
        //        ExlApp.Visible = true;
        //        return ret;
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog("3. Не удается отобразить Excel документ: " + ex.Message, MonitorLog.typelog.Error, true);
        //        ret.text = ex.Message;
        //        ret.result = true;

        //        return ret;
        //    }
        //}

        ////Полное создание отчета
        //public Returns CreateExcelReport(int numberList, DataTable DT, string[] Header, object[] footer, string Path, string nzp_user, string[] TypeHeader)// List<Prm> listprm)
        //{
        //    Returns ret = Utils.InitReturns();
        //    string fileName = "";
        //    try
        //    {
        //        //создание Excel
        //        ret = this.Load_to_Excel(numberList, DT, Header, footer, TypeHeader);
        //        if (ret.result)
        //        {
        //            //MonitorLog.WriteLog("Док Успешно сформирован", MonitorLog.typelog.Info, true);
        //            //Сохранение                                                         
        //            //fileName = ReportGen.GetFileName(Path, "SpLs" + "_" + nzp_user) + ".xlsx";//"SpLs_" + nzp_user + ".xlsx";
        //            fileName = "SpLs_" + nzp_user + ".xlsx";
        //            ret = this.SaveFile(Path + fileName, fileName, ref this.ExlWb, ref this.ExlApp);
        //            //Удаление
        //            this.DeleteObject();
        //        }
        //        else
        //        {
        //            MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog(ex.Message,MonitorLog.typelog.Error,true);
        //        //удаление объекта
        //        this.DeleteObject();
        //    }

        //    if (ret.result)
        //    {
        //        ret.text = fileName;
        //    }

        //    return ret;
        //}

        //применение стандартного форматирования
        public Returns ApplyStandartFormat(int RowsLength, int HeigthHeader,int LengthHeader, string[] TypeHeader)
        {
            Returns ret = Utils.InitReturns();
            ExcelFormater Eformat = new ExcelFormater();


            try
            {
                 //выравнивание по содержимому
                //Eformat.ColumnsAutoFit(new string[] {"A","B","C"}, table_data.Rows.Count + 2, ref ExlWs);
                Eformat.AllColumnsAutoFit(LengthHeader, RowsLength + 1 + HeigthHeader, ref ExlWs);

                //Выравнивание по центру                    
                Eformat.AddAlignment(LengthHeader, RowsLength + 1 + HeigthHeader, 1, ref ExlWs);
                
                //Применение формата                                   
                for (int i = 0; i < TypeHeader.Length; i++)
                {
                    if (TypeHeader[i] != null)
                    {
                        Eformat.AddFormatColumn(new string[] { Eformat.BukvaList[i] }, RowsLength, HeigthHeader, Array.IndexOf(Eformat.FormatList, TypeHeader[i]), ref ExlWs);
                        ////замена точки на запятую
                        //if (TypeHeader[i] == "float")
                        //{
                        //    //Eformat.ReplaceComma(Eformat.BukvaList[i], table_data.Rows.Count + 1, ref ExlWs);
                        //}

                    }
                }

                ////заливка заголовков
                //Eformat.SetColrCells(LengthHeader, 1, -1, ref ExlWs);//5296274
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка заполнения: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;               
            }

            return ret;

        }


        private bool CheckFileAccess(string fileName)
        {
            bool result = false;
            int countDenied = 0;
            while (!result && countDenied<240)//240 раз 20 минут ждет доступа к файлу
            {
                try
                {
                    using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        result = true;
                        fs.Close();
                    }
                }
                catch 
                {
                    result = false;
                }
                Thread.Sleep(5000);
                countDenied++;

            }
            return result;
        }

        #region Загрузка шаблонов
        /// <summary>
        /// Загружает шаблон в рабочую книгу. (шаблон должен находиться на первой странице)
        /// </summary>
        /// <param name="pathTemplate">Путь к файлу шаблона</param>
        /// <param name="data">Список параметров типа ключ - значение. Ключи в шаблоне заменяются на значения.</param>
        /// <returns>тип Returns</returns>
        public Returns LoadTemlate(string pathTemplate, Dictionary<string, string> data)
        {
            Returns ret = Utils.InitReturns();


            if (!CheckFileAccess(pathTemplate))
            {
                ret.result = false;
                return ret;
            }

            try
            {
                //открыть шаблон
                this.ExlWb = this.ExlApp.Workbooks.Open(pathTemplate, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                //обновить ссылка на страницу(обязательно при смене ссылки на книгу)
                this.RefreshWs();
                //Количество строк с данными
                int iRowCount = this.ExlWs.UsedRange.Rows.Count + 2;

                //количество столбцов с данными
                int iColumnCount = this.ExlWs.UsedRange.Columns.Count;


                for (int j = 1; j <= iColumnCount; j++)
                {
                    for (int i = 1; i <= iRowCount; i++)
                    {
                        if (this.ExlWs.Cells[i, j] != null)
                        {
                            //значение ячейки 
                            Excel.Range val = (Excel.Range)this.ExlWs.Cells[i, j];
                            string valCel = val.get_Value(Type.Missing) != null ? val.get_Value(Type.Missing).ToString() : "";

                            //if (valCel.Contains('[') && valCel.Contains(']'))
                            while (valCel.Contains('[') && valCel.Contains(']'))
                            {

                                string datName = valCel.Substring(valCel.IndexOf('[') + 1, valCel.IndexOf(']') - valCel.IndexOf('[') - 1);
                                string datValue;
                                if (data.TryGetValue(datName, out datValue))
                                {
                                    string strVal = valCel.Replace(datName, datValue);
                                    //strVal = strVal.Replace('[', ' '); 
                                    //strVal = strVal.Replace(']', ' ');
                                    strVal = strVal.Remove(strVal.IndexOf('['), 1);
                                    strVal = strVal.Remove(strVal.IndexOf(']'), 1);
                                    valCel = strVal.Trim();
                                    val.Value2 = valCel;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                ////УБРАТЬ! Тестовое сохранение!
                ////сохранение
                //this.SaveFile(@"C:\123.xls", "", ref this.ExlWb, ref this.ExlApp);

                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка замены данных в шаблоне : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
            }

            return ret;
        }

        /// <summary>
        /// Загружает шаблон в рабочую книгу. (шаблон должен находиться на первой странице)
        /// </summary>
        /// <param name="pathTemplate">Путь к файлу шаблона</param>
        /// <param name="data">Список параметров типа ключ - значение. Ключи в шаблоне заменяются на значения.</param>
        /// <returns>тип Returns</returns>
        public Returns LoadTemlateNewFile(string pathTemplate, Dictionary<string, string> data)
        {
            Returns ret = Utils.InitReturns();


            if (!CheckFileAccess(pathTemplate))
            {
                ret.result = false;
                return ret;
            }

            try
            {
                copyiedTemplate  = pathTemplate.Replace(".xls", DateTime.Now.GetHashCode() + ".xls");
                File.Copy(pathTemplate, copyiedTemplate);
                ret.text = copyiedTemplate;
                //открыть шаблон
                this.ExlWb = this.ExlApp.Workbooks.Open(copyiedTemplate, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                //обновить ссылка на страницу(обязательно при смене ссылки на книгу)
                this.RefreshWs();
                //Количество строк с данными
                int iRowCount = this.ExlWs.UsedRange.Rows.Count + 2;

                //количество столбцов с данными
                int iColumnCount = this.ExlWs.UsedRange.Columns.Count;


                for (int j = 1; j <= iColumnCount; j++)
                {
                    for (int i = 1; i <= iRowCount; i++)
                    {
                        if (this.ExlWs.Cells[i, j] != null)
                        {
                            //значение ячейки 
                            Excel.Range val = (Excel.Range)this.ExlWs.Cells[i, j];
                            string valCel = val.get_Value(Type.Missing) != null ? val.get_Value(Type.Missing).ToString() : "";

                            //if (valCel.Contains('[') && valCel.Contains(']'))
                            while (valCel.Contains('[') && valCel.Contains(']'))
                            {

                                string datName = valCel.Substring(valCel.IndexOf('[') + 1, valCel.IndexOf(']') - valCel.IndexOf('[') - 1);
                                string datValue;
                                if (data.TryGetValue(datName, out datValue))
                                {
                                    string strVal = valCel.Replace(datName, datValue);
                                    //strVal = strVal.Replace('[', ' '); 
                                    //strVal = strVal.Replace(']', ' ');
                                    strVal = strVal.Remove(strVal.IndexOf('['), 1);
                                    strVal = strVal.Remove(strVal.IndexOf(']'), 1);
                                    valCel = strVal.Trim();
                                    val.Value2 = valCel;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                ////УБРАТЬ! Тестовое сохранение!
                ////сохранение
                //this.SaveFile(@"C:\123.xls", "", ref this.ExlWb, ref this.ExlApp);

                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка замены данных в шаблоне : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
            }

            return ret;
        }


        #endregion
        
    }
}
