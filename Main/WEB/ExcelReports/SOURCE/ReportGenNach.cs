using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;

namespace STCLINE.KP50.REPORT
{
    public partial class ReportGen
    {
        /// <summary>
        /// Выполняет формирование отчета Генератор по начислениям
        /// </summary>
        /// <param name="finder">Объект поиска типа Ls</param>
        /// <param name="par">Список параметров-начислений</param>
        /// <param name="month">Текущий месяц</param>
        /// <param name="year">Текущий месяц</param>
        /// <param name="comment">Комментарий</param>
        /// <returns>Объект Returns</returns>
        public Returns GetReportPrmNach(Ls finder, List<int> par, int month, int year, ref string fileName)
        {

            fileName = String.Empty;
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;



            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetReportParamNach(finder.nzp_user.ToString(), 1);
            ExR.Close();

            if (DT == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            //Делать ли лобавление пустых параметров?!



            ExcelFormater Eformat = new ExcelFormater();

            //создаем массив заголовков
            string[] HeaderData = new string[DT.Columns.Count];
            //создаем массив форматов
            string[] TypeHeader = new string[HeaderData.Length];
            //футер
            string[] footer_data = null;

            ////заполняем массивы
            //HeaderData[0] = "Лицевой счет";
            //HeaderData[1] = "Адрес";
            //int index = 2;
            //int indextype = 2;

            int index = 0;
            int indextype = 0;

            //заполнение массивов
            for (int i = 0; i < DT.Columns.Count; i++)
            {
                DataColumn dc = DT.Columns[i];

                switch (dc.ColumnName)
                {
                    case "service":
                    {
                        HeaderData[index] = "Услуга";
                        TypeHeader[indextype] = "char";
                        break;
                    }
                    case "ordering":
                    {
                        HeaderData[index] = "ordering";
                        TypeHeader[indextype] = "";
                        break;
                    }
                    case "name_supp":
                    {
                        HeaderData[index] = "Поставщик";
                        TypeHeader[indextype] = "char";
                        break;
                    }
                    case "tarif":
                    {
                        HeaderData[index] = "Тариф";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "sum_insaldo":
                    {
                        HeaderData[index] = "Вход. сальдо";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "sum_tarif":
                    {
                        HeaderData[index] = "Расчет за месяц";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "reval":
                    {
                        HeaderData[index] = "Перерасчет";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "real_charge":
                    {
                        HeaderData[index] = "Изменения";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "sum_nedop":
                    {
                        HeaderData[index] = "Недопоставка";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "sum_money":
                    {
                        HeaderData[index] = "Оплачено";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "sum_charge":
                    {
                        HeaderData[index] = "К оплате";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "sum_outsaldo":
                    {
                        HeaderData[index] = "Исход. сальдо";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "rsum_tarif":
                    {
                        HeaderData[index] = "Начисл. за месяц без недоп.";
                        TypeHeader[indextype] = "float";
                        break;
                    }
                    case "adr":
                    {
                        HeaderData[index] = "Адрес";
                        TypeHeader[indextype] = "char";
                        break;
                    }
                    case "num_ls":
                    {

                        HeaderData[index] = "Лицевой счет";
                        TypeHeader[indextype] = "char";
                        break;
                    }
                    case "pkod10":
                    {
                        HeaderData[index] = "Лицевой счет";
                        TypeHeader[indextype] = "char";
                        break;
                    }
                    case "geu":
                    {
                        HeaderData[index] = "ЖЭУ";
                        TypeHeader[indextype] = "char";
                        break;
                    }
                    case "area":
                    {
                        HeaderData[index] = "Управляющая компания";
                        TypeHeader[indextype] = "char";
                        break;
                    }

                    default:
                    {
                        HeaderData[index] = dc.ColumnName;
                        break;
                    }

                }

                index++;
                indextype++;
            }

            //формирование Excel файла                 
            try
            {
                #region Создание Excel документа

                ExcelL = new ExcelLoader();

                ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlLandscape;
                ExcelL.ExlWs.Rows.Font.Name = "Arial";
                ExcelCreater ExcelCr = new ExcelCreater();

                //создаем название отчета
                var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
                ret = ExcelCr.MakeName("Начисления по лицевым счетам за " + months[month] + " " + year.ToString() + "г.", "A1", DT.Columns.Count, 2, ref ExcelL.ExlWs);

                //Создаем шапку
                ret = ExcelCr.MakeHeader(HeaderData, 1, 0, ref ExcelL.ExlWs);


                //Создаем тело               
                if (ret.result && DT.Rows.Count < 65001)
                {
                    ret = ExcelCr.MakeBody(DT, 2, 0, ref ExcelL.ExlWs);
                }

                //Создаем подвал                
                if (footer_data != null && ret.result)
                {
                    ret = ExcelCr.MakeFooter(footer_data, DT.Rows.Count + 1, 0, ref ExcelL.ExlWs);
                }
                //стандартный формат
                if (ret.result)
                {
                    ret = ExcelL.ApplyStandartFormat(DT.Rows.Count, 3, HeaderData.Length, TypeHeader);
                }

                #endregion

                #region Надпись "Нет данных" если данных нет)
                if (DT.Rows.Count > 65000)
                {
                    ExcelCr.MakeName(" Не все версии Excel поддерживают " + Environment.NewLine +
                                     " количество строк более 65000, " + Environment.NewLine +
                                     " ограничьте выборку, " + Environment.NewLine +
                                     " количество записей " + DT.Rows.Count, "A4", DT.Columns.Count, 6, ref ExcelL.ExlWs);
                }
                if (DT.Rows.Count == 0)
                {
                    ExcelCr.MakeName("Нет данных", "A4", DT.Columns.Count, 7, ref ExcelL.ExlWs);
                }

                #endregion

                if (ret.result)
                {
                    //имя файла                                                         
                    fileName =
                        ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpLsNach" + "_" + 
                       DateTime.Now.GetHashCode() + finder.nzp_user) +
                        ".xls";

                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb,
                        ref ExcelL.ExlApp);


                
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


            if (!String.IsNullOrEmpty(fileName))
            {
                string newfileName = GetFileName(Global.Constants.ExcelDir, fileName);
                File.Copy(Global.Constants.ExcelDir + fileName, Global.Constants.ExcelDir + newfileName);
                if (InputOutput.useFtp)
                {
                    fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + newfileName);
                }
            }


            return ret;
        }

        /// <summary>
        /// Выполняет формирование отчета Генератор по начислениям (2-ой режим)
        /// </summary>
        /// <param name="finder">Объект поиска типа Ls</param>
        /// <param name="par">Список параметров-начислений</param>
        /// <param name="month">Текущий месяц</param>
        /// <param name="year">Текущий месяц</param>
        /// <param name="comment">Комментарий</param>
        /// <returns>Объект Returns</returns>
        public Returns GetReportPrmNach_2(Ls finder, List<int> par, int month, int year, ref string fileName)
        {

            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;
            fileName = String.Empty;
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetReportParamNach(finder.nzp_user.ToString(), 2);
            ExR.Close();

            if (DT == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }


            ExcelFormater Eformat = new ExcelFormater();
            //Eformat.MakeBukvaList(DT.Columns.Count);

            //создаем массив заголовков
            string[] HeaderData = new string[DT.Columns.Count];
            //создаем массив форматов
            string[] TypeHeader = new string[HeaderData.Length];

            ////заполняем массивы
            //HeaderData[0] = "Лицевой счет";
            //HeaderData[1] = "Адрес";
            //int index = 2;
            //int indextype = 2;

            int index = 0;
            int indextype = 0;

            try
            {
                //заполнение массивов
                for (int i = 0; i < DT.Columns.Count; i++)
                {
                    //название колонки-параметра (без услуги)
                    string colPureName = "";
                    //услуга
                    int colServ = -1;

                    DataColumn dc = DT.Columns[i];

                    if (dc.ColumnName.LastIndexOf('_') != -1)
                    {
                        try
                        {
                            colPureName = dc.ColumnName.Substring(0, dc.ColumnName.LastIndexOf('_'));
                            string serv = dc.ColumnName.Substring(dc.ColumnName.LastIndexOf('_') + 1);

                            if (!Int32.TryParse(serv, out colServ))
                            {
                                colPureName = dc.ColumnName;
                            }
                        }
                        catch (Exception) { }
                    }
                    else
                    {
                        colPureName = dc.ColumnName;
                    }

                    switch (colPureName)
                    {
                        case "service":
                            {
                                HeaderData[index] = "Услуга";
                                TypeHeader[indextype] = "char";
                                break;
                            }
                        case "ordering":
                            {
                                HeaderData[index] = "ordering";
                                TypeHeader[indextype] = "";
                                break;
                            }
                        case "name_supp":
                            {
                                HeaderData[index] = "Поставщик";
                                TypeHeader[indextype] = "char";
                                break;
                            }
                        case "tarif":
                            {
                                HeaderData[index] = "Тариф";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "sum_insaldo":
                            {
                                HeaderData[index] = "Вход. сальдо";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "sum_tarif":
                            {
                                HeaderData[index] = "Расчет за месяц";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "reval":
                            {
                                HeaderData[index] = "Перерасчет";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "real_charge":
                            {
                                HeaderData[index] = "Изменения";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "sum_nedop":
                            {
                                HeaderData[index] = "Недопоставка";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "sum_money":
                            {
                                HeaderData[index] = "Оплачено";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "sum_charge":
                            {
                                HeaderData[index] = "К оплате";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "sum_outsaldo":
                            {
                                HeaderData[index] = "Исход. сальдо";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "rsum_tarif":
                            {
                                HeaderData[index] = "Начисл. за месяц без недоп.";
                                TypeHeader[indextype] = "float";
                                break;
                            }
                        case "adr":
                            {
                                HeaderData[index] = "Адрес";
                                TypeHeader[indextype] = "char";
                                break;
                            }
                        case "num_ls":
                            {

                                HeaderData[index] = "Лицевой счет";
                                TypeHeader[indextype] = "char";
                                break;
                            }
                        case "pkod10":
                            {
                                HeaderData[index] = "Лицевой счет";
                                TypeHeader[indextype] = "char";
                                break;
                            }
                        case "geu":
                            {
                                HeaderData[index] = "ЖЭУ";
                                TypeHeader[indextype] = "char";
                                break;
                            }
                        case "area":
                            {
                                HeaderData[index] = "Управляющая компания";
                                TypeHeader[indextype] = "char";
                                break;
                            }

                        default:
                            {
                                HeaderData[index] = dc.ColumnName;
                                break;
                            }

                    }

                    //прибавление услуги
                    DbSprav dbSp = new DbSprav();
                    List<_Service> services = dbSp.ServiceLoad(new Finder(), out ret);
                    dbSp.Close();
                    foreach (_Service s in services)
                    {
                        if (s.nzp_serv == colServ)
                        {
                            HeaderData[index] += "[" + s.service + "]";
                            break;
                        }
                    }

                    index++;
                    indextype++;
                }
            }
            catch (Exception)
            {

            }

            //формирование Excel файла                 
            try
            {
                #region Создание Excel документа
                //высота заголовка
                int nameHeaderHeigth = 2;
                //смещение шапки отчета(строки)
                int headRowOffset = 2;
                //смещение шапки отчета(столбцы)
                int headColumnOffset = 0;



                ExcelL = new ExcelLoader();

                ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlLandscape;

                ExcelCreater ExcelCr = new ExcelCreater();

                //создаем название отчета
                ret = ExcelCr.MakeName("Начисления по лицевым счетам", "A1", DT.Columns.Count, nameHeaderHeigth, ref ExcelL.ExlWs);

                //Создаем шапку
                ret = ExcelCr.MakeHeader(HeaderData, headRowOffset, headColumnOffset, ref ExcelL.ExlWs);

                #region Объединение полей(по признаку услуга)
                try
                {
                    string startBukv = "";
                    string endBukv = "";
                    string serv = "";
                    for (int i = 0; i < DT.Columns.Count; i++)
                    {
                        //считываем ячейку
                        Microsoft.Office.Interop.Excel.Range cuurentCell = ExcelL.ExlWs.get_Range(Eformat.BukvaList[i] + (nameHeaderHeigth + headRowOffset), Type.Missing);
                        string cell = cuurentCell.Value2.ToString();
                        if (cell.Contains('[') && cell.Contains(']') && (cell.LastIndexOf('[') < cell.LastIndexOf(']')))
                        {
                            int sind = cell.LastIndexOf('[');
                            int lind = cell.LastIndexOf(']');
                            string temp = cell.Substring(sind + 1, lind - sind - 1);

                            //Инициализация начальной буквы объединения
                            if (startBukv == "")
                            {
                                //запоминаем начальную букву объединения
                                startBukv = Eformat.BukvaList[i];
                                //test mode
                                endBukv = startBukv;
                                serv = temp;
                            }

                            //новая [услуга]
                            if (temp != serv)
                            {
                                //объединяем                                
                                Microsoft.Office.Interop.Excel.Range mRange = ExcelL.ExlWs.get_Range(startBukv + (nameHeaderHeigth + 1), endBukv + (nameHeaderHeigth + 1));
                                mRange.Merge(Type.Missing);
                                mRange.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous,
                                                    Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin,
                                                    Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic,
                                                    Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);
                                mRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                mRange.Font.Bold = true;
                                mRange.Value2 = serv;

                                serv = temp;
                                startBukv = Eformat.BukvaList[i];
                                //test mode
                                endBukv = startBukv;
                            }
                            else
                            {
                                //запоминаем конечную букву
                                endBukv = Eformat.BukvaList[i];
                            }

                            //Особая ситуация - конец таблицы
                            if (i == (DT.Columns.Count - 1))
                            {
                                //объединяем                                
                                Microsoft.Office.Interop.Excel.Range mRange = ExcelL.ExlWs.get_Range(startBukv + (nameHeaderHeigth + 1), endBukv + (nameHeaderHeigth + 1));
                                mRange.Merge(Type.Missing);
                                mRange.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous,
                                                    Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin,
                                                    Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic,
                                                    Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);
                                mRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                mRange.Font.Bold = true;
                                mRange.Value2 = serv;

                                serv = temp;
                                startBukv = Eformat.BukvaList[i];
                                //test mode
                                endBukv = startBukv;
                            }

                            //убираем скобки []
                            cuurentCell.Value2 = cell.Substring(0, cell.LastIndexOf('['));

                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                catch (Exception)
                {

                }
                #endregion


                //Создаем тело               
                if (ret.result)
                {
                    ret = ExcelCr.MakeBody(DT, 3, 0, ref ExcelL.ExlWs);
                }

                ////Создаем подвал                
                //if (footer_data != null && ret.result)
                //{
                //    ret = ExcelCr.MakeFooter(footer_data, DT.Rows.Count + 1, 0, ref ExcelL.ExlWs);
                //}
                //стандартный формат
                if (ret.result)
                {
                    ret = ExcelL.ApplyStandartFormat(DT.Rows.Count, 4, HeaderData.Length, TypeHeader);
                }
                #endregion

                #region Надпись "Нет данных" если данных нет)
                if (DT.Rows.Count == 0)
                {
                    ExcelCr.MakeName("Нет данных", "A4", DT.Columns.Count, 7, ref ExcelL.ExlWs);
                }
                #endregion

                if (ret.result)
                {
                    //имя файла                                                         
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpLsNach" + "_" +
                    DateTime.Now.GetHashCode()+finder.nzp_user) + ".xls";

                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    ExcelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                    ExcelL.ExlWb = null;

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


            if (!String.IsNullOrEmpty(fileName))
            {
                string newfileName = GetFileName(Global.Constants.ExcelDir, fileName);
                File.Copy(Global.Constants.ExcelDir + fileName, Global.Constants.ExcelDir + newfileName);
                if (InputOutput.useFtp)
                {
                    fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + newfileName);
                }
            }



            return ret;
        }


        //Формирование отчета Генератор по начислениям
        public void GetReportPrmNach(object container)
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
            ret = excelRepDb.AddToPoolThread(cont.finder.nzp_user, "empty", 2, "Отчет \"Генератор по начислениям\" ", ref time, cont.comment);

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.finder.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetReportPrmNach(cont.finder, cont.parList, Convert.ToInt32(cont.mm), Convert.ToInt32(cont.yy), ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.finder.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.finder.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.finder.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Формирование отчета Генератор по начислениям 2
        public void GetReportPrmNach_2(object container)
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
            ret = excelRepDb.AddToPoolThread(cont.finder.nzp_user, "empty", 2, "Отчет \"Генератор по начислениям\" ", ref time, cont.comment);

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.finder.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetReportPrmNach_2(cont.finder, cont.parList, Convert.ToInt32(cont.mm), Convert.ToInt32(cont.yy), ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.finder.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.finder.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.finder.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }




        
        //отчет по начисления по поставщикам
        public void GetNachSupp(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Начисления по поставщикам\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.finder.nzp_user, "empty", 2, "Отчет \"Начисления по поставщикам\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.finder.nzp_user, "1", 2, "", time);


            ret = this.GetNachSupp(cont.nzp_supp, (SupgFinder)cont.finder, cont.yearr, cont.serv, ref fileName);
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

    }
}
