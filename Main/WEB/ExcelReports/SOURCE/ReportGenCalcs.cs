using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;

namespace STCLINE.KP50.REPORT
{
    public partial class ReportGen
    {


        /// <summary>
        /// Отчет: сверка расчетов с жильцом по состоянию
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="yy_from"></param>
        /// <param name="mm_from"></param>
        /// <param name="yy_to"></param>
        /// <param name="mm_to"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Returns GetVerifCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            DBCalcs calc = new DBCalcs();
            System.Data.DataTable DT_Body = calc.VerificationCalcs(finder, yy_from, mm_from, yy_to, mm_to, out ret);
            StreamWriter sw = new StreamWriter(@"C:\Temp\VerifCalc.txt", true);
            sw.WriteLine("00001");
            MonitorLog.WriteLog("Запуск процедуры формирвования тела отчета VerificationCalcs", MonitorLog.typelog.Info, true);
            try
            {

                if (DT_Body != null)
                {
                    sw.WriteLine("001");
                    #region Получение колонки даты

                    for (int g = 0; g < DT_Body.Rows.Count; g++)
                    {
                        sw.WriteLine("111 = " + DT_Body.Rows[g][1].ToString());
                        string date_mm = DT_Body.Rows[g][0].ToString();
                        string date_yy = DT_Body.Rows[g][1].ToString();
                        sw.WriteLine(date_mm);
                        sw.WriteLine(date_yy);
                        string date = date_mm + date_yy;
                        sw.WriteLine(date);
                        DT_Body.Rows[g][0] = date;
                    }
                    DT_Body.Columns.RemoveAt(1);
                    sw.WriteLine("002");
                    #endregion

                    #region Определение заголовков и типов
                    sw.WriteLine("003");
                    //собрать заголовки футера
                    string[] footer_name_1 = new string[] { "Итого:", "", "", "", "", "" };
                    string[] footer_name_2 = new string[] { "", "", "", "Сальдо на", "", "" };

                    sw.WriteLine("004");
                    //собрать типы для колонок
                    string[] TypeFooter_1 = new string[] { "char", "float", "", "float", "float", "float" };
                    string[] TypeFooter_2 = new string[] { "", "", "", "", "", "float" };

                    #endregion



                    try
                    {
                        //-----------------------------------создание Excel---------------------------------
                        sw.WriteLine("005");
                        #region Заполняем и объединяем ячейки

                        int temp_y = Convert.ToInt32("20" + yy_from.ToString());
                        sw.WriteLine("006");
                        //Проверка периода выгрузки
                        if (STCLINE.KP50.Interfaces.Points.BeginCalc.year_ > temp_y ||
                            (STCLINE.KP50.Interfaces.Points.BeginCalc.year_ == temp_y && STCLINE.KP50.Interfaces.Points.BeginCalc.month_ > Convert.ToInt32(mm_from)))
                        {
                            mm_from = STCLINE.KP50.Interfaces.Points.BeginCalc.month_.ToString("00");
                            string year_f = STCLINE.KP50.Interfaces.Points.BeginCalc.year_.ToString().Substring(STCLINE.KP50.Interfaces.Points.BeginCalc.year_.ToString().Length - 2, 2);
                            yy_from = year_f;
                        }
                        sw.WriteLine("007");
                        int date = DateTime.DaysInMonth(Convert.ToInt32("20" + yy_to), Convert.ToInt32(mm_to));
                        sw.WriteLine("008");
                        #endregion

                        Dictionary<string, string> Dic = new Dictionary<string, string>();

                        Dic.Add("date1", DateTime.Now.ToString("d"));
                        Dic.Add("date2", "c " + "01." + mm_from + ".20" + yy_from + " по " + date.ToString() + "." + mm_to + ".20" + yy_to);
                        Dic.Add("date3", "");
                        Dic.Add("fio", finder.fio);
                        Dic.Add("pkod", finder.num_ls.ToString());
                        Dic.Add("addr", finder.adr);
                        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("sverka_raschet.xls"), Dic);
                        sw.WriteLine("009");

                        ExcelCreater ExcelCr = new ExcelCreater();
                        Microsoft.Office.Interop.Excel.Range excells3;

                        #region Создание тела

                        MonitorLog.WriteLog("Запуск создание тела отчета VerificationCalcs", MonitorLog.typelog.Info, true);
                        sw.WriteLine("1");
                        if (DT_Body.Rows.Count != 0)
                        {
                            string a = DT_Body.Rows[0][0].ToString();
                            string _month = a.Substring(0, a.Length - 4);
                            string _year = a.Substring(a.Length - 2, 2);
                            sw.WriteLine("2");

                            excells3 = ExcelL.ExlWs.get_Range("C10", Type.Missing);
                            excells3.Value2 = "01." + _month.PadLeft(2, '0') + ".20" + _year;
                            excells3.NumberFormat = "дд.мм.гггг";
                            excells3.EntireRow.AutoFit();
                            sw.WriteLine("3");
                            excells3 = ExcelL.ExlWs.get_Range("D10", Type.Missing);
                            excells3.Value2 = Decimal.Parse(ret.sql_error);
                            excells3.NumberFormat = "0.00";
                            excells3.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            sw.WriteLine("4");
                            if (ret.result)
                            {
                                DT_Body.Columns.RemoveAt(1);
                                ret = ExcelCr.MakeBody(DT_Body, 12, 0, ref ExcelL.ExlWs);
                            }
                            sw.WriteLine("5");
                            excells3 = ExcelL.ExlWs.get_Range("A" + Convert.ToString(DT_Body.Rows.Count + 13), Type.Missing);
                            excells3.Font.Bold = true;
                            excells3.Value2 = "Итого: ";
                            excells3.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            sw.WriteLine("6");
                            excells3 = ExcelL.ExlWs.get_Range("E" + Convert.ToString(DT_Body.Rows.Count + 15), "F" + Convert.ToString(DT_Body.Rows.Count + 15));
                            excells3.Merge(Type.Missing);
                            excells3.Value2 = "Сальдо на " + date.ToString() + "." + mm_to + ".20" + yy_to;
                            excells3.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            excells3.Font.Bold = true;
                            sw.WriteLine("7");
                            excells3 = ExcelL.ExlWs.get_Range("A13", "G" + Convert.ToString(DT_Body.Rows.Count + 15));
                            excells3.Font.Size = 8;

                            //вывод Месяц/год
                            for (int i = 0; i < DT_Body.Rows.Count; i++)
                            {
                                excells3 = ExcelL.ExlWs.get_Range("A" + Convert.ToString(13 + i), Type.Missing);
                                excells3.NumberFormat = "@";
                                if (excells3.Value2 != null)
                                {
                                    int temp_month = Convert.ToInt32(excells3.Value2.ToString().Substring(0, excells3.Value2.ToString().Length - 4));
                                    string temp = String.Format("{0:00}", temp_month);
                                    excells3.Value2 = temp + "/" + excells3.Value2.ToString().Substring(excells3.Value2.ToString().Length - 4, 4);
                                    excells3.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                }
                            }
                            sw.WriteLine("8");
                            ExcelFormater exform = new ExcelFormater();
                            for (int r = 0; r < 6; r++)
                            {
                                double sum = 0.0;
                                if (r != 0 && r != 2 && r != 5)
                                {
                                    for (int h = 0; h < DT_Body.Rows.Count; h++)
                                    {
                                        excells3 = ExcelL.ExlWs.get_Range(exform.BukvaList[r].ToString() + Convert.ToString(13 + h), Type.Missing);
                                        sum += Convert.ToDouble(excells3.Value2);
                                    }
                                    excells3 = ExcelL.ExlWs.get_Range(exform.BukvaList[r].ToString() + Convert.ToString(DT_Body.Rows.Count + 13), Type.Missing);
                                    excells3.NumberFormat = "0.00";
                                    excells3.Font.Bold = true;
                                    excells3.Value2 = sum;
                                }
                            }
                            sw.WriteLine("9");
                            excells3 = ExcelL.ExlWs.get_Range("C" + 13, "C" + Convert.ToString(DT_Body.Rows.Count + 13));
                            excells3.NumberFormat = "дд.мм.гггг";


                            excells3 = ExcelL.ExlWs.get_Range("F" + Convert.ToString(DT_Body.Rows.Count + 12), Type.Missing);
                            Microsoft.Office.Interop.Excel.Range excells4;
                            excells4 = ExcelL.ExlWs.get_Range("F" + Convert.ToString(DT_Body.Rows.Count + 13), Type.Missing);
                            excells4.Value2 = excells3.Value2;
                            excells4.Font.Bold = true;
                            excells4.NumberFormat = "0.00";
                            sw.WriteLine("10");
                            //сальдо нарастающим итогом
                            excells3 = ExcelL.ExlWs.get_Range("F" + 13, "F" + Convert.ToString(DT_Body.Rows.Count + 13));
                            excells3.NumberFormat = "0.00";

                            for (int r = 0; r < 7; r++)
                            {
                                if (r != 0 & r != 2)
                                {
                                    for (int h = 0; h < DT_Body.Rows.Count; h++)
                                    {
                                        excells3 = ExcelL.ExlWs.get_Range(exform.BukvaList[r].ToString() + Convert.ToString(13 + h), Type.Missing);
                                        excells3.NumberFormat = "0.00";
                                    }
                                }
                            }

                            sw.WriteLine("11");
                            Microsoft.Office.Interop.Excel.Range excells5;
                            excells5 = ExcelL.ExlWs.get_Range("F" + Convert.ToString(DT_Body.Rows.Count + 13), Type.Missing);
                            excells3 = ExcelL.ExlWs.get_Range("F" + Convert.ToString(DT_Body.Rows.Count + 15), Type.Missing);
                            excells3.Value2 = excells5.Value2;
                            excells3.NumberFormat = "0.00";
                            excells3.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            excells3.Font.Bold = true;

                            excells3 = ExcelL.ExlWs.get_Range("B3", "D3");
                            excells3.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                            sw.WriteLine("12");
                            for (int k = 0; k < 4; k++)
                            {
                                excells3 = ExcelL.ExlWs.get_Range("C" + Convert.ToString(5 + k), Type.Missing);
                                excells3.Font.Bold = true;
                                excells3.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            }

                            excells3 = ExcelL.ExlWs.get_Range("B3", "E3");
                            excells3.Font.Size = "11";
                            excells3 = ExcelL.ExlWs.get_Range("E3", Type.Missing);
                            excells3.Font.Size = "11";
                            sw.WriteLine("13");

                            #endregion

                            #region заполнение параметров подписи отчета
                            int indexer = DT_Body.Rows.Count + 17,
                                fontSize = 10;
                            string fontName = "Calibri";

                            /* 579 - Наименование должности бухгалтера
                               1047 - ФИО руководителя ПУС 
                               1048 - Должность руководителя ПУС */
                            var finderPrm = new Prm
                            {
                                nzp_user = finder.nzp_user,
                                pref = finder.pref,
                                prm_num = 10,
                                spis_prm = "579, 1047, 1048",
                                is_actual = 1,
                                date_begin = DateTime.Now.ToShortDateString()
                            };

                            var parPod = new DbParameters();
                            parPod.FindPrm(finderPrm, out ret);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog("class ReportGen, метод GetVerifCalcs \n " +
                                                    " Ошибка формирования списка параметров \"подписи\" :" + ret.text, MonitorLog.typelog.Error, true);
                                return ret;
                            }
                            List<Prm> listPrms = parPod.GetPrm(finderPrm, out ret);
                            if (!ret.result)
                            {
                                MonitorLog.WriteLog("class ReportGen, метод GetVerifCalcs \n " +
                                                    " Ошибка формирования списка параметров \"подписи\" :" + ret.text, MonitorLog.typelog.Error, true);
                                return ret;
                            }
                            string pasportDol = listPrms.Find(x => x.nzp_prm == 579) != null
                                    ? listPrms.Find(x => x.nzp_prm == 579).val_prm
                                    : string.Empty,
                                nachalFio = listPrms.Find(x => x.nzp_prm == 1047) != null
                                    ? listPrms.Find(x => x.nzp_prm == 1047).val_prm
                                    : string.Empty,
                                nachalDol = listPrms.Find(x => x.nzp_prm == 1048) != null
                                    ? listPrms.Find(x => x.nzp_prm == 1048).val_prm
                                    : string.Empty,
                                pasportFio = finder.webUname;


                            excells3 = ExcelL.ExlWs.Range["A" + (indexer + 2), "B" + (indexer + 2)];
                            excells3.Merge(Type.Missing);
                            excells3.Font.Name = fontName;
                            excells3.Font.Size = fontSize;
                            excells3.WrapText = true;
                            excells3.Value2 = nachalDol;

                            excells3 = ExcelL.ExlWs.Range["C" + (indexer + 2), Type.Missing];
                            excells3.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;


                            excells3 = ExcelL.ExlWs.Range["D" + (indexer + 2), Type.Missing];
                            excells3.Font.Name = fontName;
                            excells3.Font.Size = fontSize;
                            excells3.Value2 = Utils.GetCorrectFIO(nachalFio);

                            excells3 = ExcelL.ExlWs.Range["A" + (indexer + 4), "B" + (indexer + 4)];
                            excells3.Merge(Type.Missing);
                            excells3.Font.Name = fontName;
                            excells3.Font.Size = fontSize;
                            excells3.WrapText = true;
                            excells3.Value2 = pasportDol;

                            excells3 = ExcelL.ExlWs.Range["C" + (indexer + 4), Type.Missing];
                            excells3.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;

                            excells3 = ExcelL.ExlWs.Range["D" + (indexer + 4), Type.Missing];
                            excells3.Font.Name = fontName;
                            excells3.Font.Size = fontSize;
                            excells3.Value2 = pasportFio;
                            #endregion
                        }
                        else
                        {
                            ExcelCr.MakeName("Нет данных", "A13", 7, 4, ref ExcelL.ExlWs);
                        }
                        //----------------------------------------------------------------------------------
                        if (ret.result)
                        {
                            //Сохранение 

                            fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "Sverka_" + finder.nzp_user) + ".xls";//"SpLs_" + nzp_user + ".xlsx";
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
                }
                else
                {
                    sw.WriteLine("1124235236436");
                    MonitorLog.WriteLog("Ошибка при формировании Excel отчета", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при формировании Excel отчета";
                    ret.result = false;
                }
            }
            catch(Exception e)
            {
                sw.WriteLine(e.ToString());
            }
            sw.Close();
            //==========================================================================================================================

            //ret = ExcelL.CreateExcelReport(1, DT, col_names, null, Constants.ExcelDir, Nzp_user, TypeHeader);                

            return ret;
        }

        //Отчет: справка для предъявления в суд
        public Returns GetDebtCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ArrayList services = new ArrayList();
            DBCalcs calc = new DBCalcs();
            List<System.Data.DataTable> DT_Body = calc.DebtCalcs(finder, yy_from, mm_from, yy_to, mm_to, out ret);

            if (DT_Body != null)
            {
                #region Определение заголовков и типов

                //string ERC_name = "";

                //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
                //    ERC_name = "Н-ска";
                //else
                //    ERC_name = "Самары";
                string s_name = SetReportHeader();

                string[] head_up = new string[] { s_name, 
                                              "Дана для предъявления в суд", 
                                              "Квартиросъемщик (собственник)", 
                                              "Проживающий по адресу", 
                                              "Квартира", 
                                              "Общая площадь",
                                              "Количество зарегистрированных", 
                                              "Количество проживающих", 
                                              "Льготная площадь", 
                                              "Код льгот", 
                                              "Задолженность на" };

                string[] head = new string[] { "Месяц/ год", 
                                              "Всего", 
                                              "в том числе по услугам", 
                                              "Списание долга", 
                                              "Оплачено", 
                                              "Дата оплаты", 
                                              "Перечисления", 
                                              "Долг на конец месяца" };

                #endregion

                #region Создание шапки

                try
                {
                    ExcelCreater ExcelCr = new ExcelCreater();
                    ExcelFormater exform = new ExcelFormater();
                    Microsoft.Office.Interop.Excel.Range excell;
                    excell = ExcelL.ExlWs.get_Range("B3", "I3");
                    excell.Merge(Type.Missing);
                    excell.Value2 = head_up[0];
                    excell.Font.Bold = true;
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("B5", "F5");
                    excell.Merge(Type.Missing);
                    excell.Value2 = head_up[1];
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                    excell = ExcelL.ExlWs.get_Range("E7", "H7");
                    excell.Merge(Type.Missing);
                    excell.Value2 = finder.fio;
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                    excell = ExcelL.ExlWs.get_Range("E8", "H8");
                    excell.Merge(Type.Missing);
                    excell.Value2 = finder.adr;
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("F10", "G10");
                    excell.Merge(Type.Missing);
                    excell.Value2 = "Жилая площадь:";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                    excell = ExcelL.ExlWs.get_Range("E11", "E12");
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                    excell = ExcelL.ExlWs.get_Range("E7", "I16");
                    excell.Font.Bold = true;

                    excell = ExcelL.ExlWs.get_Range("F10", Type.Missing);
                    excell.Font.Bold = false;


                    for (int i = 0; i < 8; i++)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + (i + 7).ToString(), "D" + (i + 7).ToString());
                        excell.Merge(Type.Missing);
                        excell.Value2 = head_up[i + 2];
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    }

                    excell = ExcelL.ExlWs.get_Range("A16", "D16");
                    excell.Merge(Type.Missing);
                    excell.Value2 = head_up[10] + "  01/" + mm_from + "/20" + yy_from + ":";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                    double sum_debt = 0.0;
                    for (int r = 0; r < DT_Body[0].Rows.Count; r++)
                    {
                        sum_debt = sum_debt + Convert.ToDouble(DT_Body[0].Rows[r][6]);
                    }

                    excell = ExcelL.ExlWs.get_Range("E16", Type.Missing);
                    excell.Value2 = sum_debt;
                    excell.NumberFormat = "0,00";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;



                    excell = ExcelL.ExlWs.get_Range("E9", "F9");
                    excell.Merge(Type.Missing);

                    #region Квартирные параметры

                    Prm finderp = new Prm();
                    finderp.nzp_user = finder.nzp_user;//web_user.nzp_user;
                    finderp.nzp_kvar = finder.nzp_kvar;
                    finderp.pref = finder.pref;
                    finderp.prm_num = 1;
                    finderp.rows = 100;
                    finderp.month_ = Convert.ToInt32(mm_to);
                    finderp.year_ = Convert.ToInt32("20" + yy_to);

                    List<Prm> SpisPrm = new List<Prm>();
                    DbParameters dbPrm1 = new DbParameters();
                    dbPrm1.FindPrm(finderp, out ret);
                    SpisPrm = dbPrm1.GetPrm(finderp, out ret);


                    DbParameters dbPrm2 = new DbParameters();

                    dbPrm2.FindPrm(finderp, out ret);
                    SpisPrm = dbPrm2.GetPrm(finderp, out ret);

                    if (ret.result)
                    {

                        for (int i = 0; i < SpisPrm.Count; i++)
                        {
                            #region Разбираем параметры

                            switch (SpisPrm[i].nzp_prm)
                            {
                                case 3:
                                    if (SpisPrm[i].val_prm == "коммунальное")
                                    {

                                        ExcelL.ExlWs.Cells[9, 6] = "коммунальная";
                                    }
                                    else
                                    {
                                        ExcelL.ExlWs.Cells[9, 7] = "изолированная";
                                    }
                                    break;
                                case 8:
                                    if (SpisPrm[i].val_prm == "да")
                                    {
                                        ExcelL.ExlWs.Cells[9, 5] = "приватизирована";
                                    }
                                    else
                                    {
                                        ExcelL.ExlWs.Cells[9, 5] = "не приватизирована";
                                    }
                                    break;

                                case 4:
                                    {
                                        string pl = System.Convert.ToDecimal(SpisPrm[i].val_prm).ToString("f2");
                                        ExcelL.ExlWs.Cells[10, 5] = pl;
                                    }
                                    break;
                                case 6:
                                    {
                                        string pl = System.Convert.ToDecimal(SpisPrm[i].val_prm).ToString("f2");
                                        ExcelL.ExlWs.Cells[10, 9] = pl;
                                    } break;
                                case 5: ExcelL.ExlWs.Cells[11, 5] = SpisPrm[i].val_prm; break;
                                case 2005: ExcelL.ExlWs.Cells[12, 5] = SpisPrm[i].val_prm; break;
                                default:
                                    break;
                            }
                            #endregion
                        }
                    }
                    dbPrm2.Close();

                    #endregion

                    #region услуги для шапки
                    for (int g = 0; g < DT_Body.Count; )
                    {
                        int index = 0;
                        for (int j = 1; j <= DT_Body[g].Rows.Count; j++)
                        {

                            int count = 0;
                            string service = DT_Body[g].Rows[index][1].ToString();
                            if (services.Count == 0)
                                services.Add(service);
                            else
                            {
                                for (int s = 0; s < services.Count; s++)
                                {
                                    if (services[s].ToString() == service)
                                    {
                                        count++;
                                        break;
                                    }
                                }
                                if (count == 0)
                                {
                                    services.Add(service);
                                }
                            }
                            index++;
                        }
                        g = g + 2;
                    }

                    #endregion

                    for (int f = 0; f < head.Length; f++)
                    {
                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[f] + "18", Type.Missing);
                        excell.Value2 = head[f];
                    }

                    if (services.Count != 0)
                    {
                        for (int d = 0; d < 5; d++)
                        {
                            excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 - d] + "18", Type.Missing);
                            string value1 = excell.Value2.ToString();
                            excell.Clear();
                            excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 1 - d] + "18", exform.BukvaList[7 + services.Count - 1 - d] + "19");
                            excell.Merge(Type.Missing);
                            excell.Value2 = value1;
                            excell.WrapText = true;
                        }
                        excell = ExcelL.ExlWs.get_Range("A18", exform.BukvaList[7 + services.Count - 1] + "19");
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        exform.AllColumnsAutoFit(7 + services.Count - 2, 17, 2, ref ExcelL.ExlWs);

                        for (int a = 0; a < services.Count; a++)//пишем виды услуг в шапку
                        {
                            excell = ExcelL.ExlWs.get_Range(exform.BukvaList[2 + a] + "19", Type.Missing);
                            excell.ColumnWidth = 8.7;
                            excell.WrapText = true;
                            excell.Value2 = services[a];
                            excell.Font.Size = 7;
                        }

                        excell = ExcelL.ExlWs.get_Range("C18", exform.BukvaList[7 + services.Count - 6] + "18");//объединяем "в том числе по услугам"
                        excell.Merge(Type.Missing);

                        excell = ExcelL.ExlWs.get_Range("A1", exform.BukvaList[7 + services.Count - 1] + "18");//объединяем "в том числе по услугам"
                        excell.Font.Size = 8;

                #endregion

                        #region Создание тела



                        int indexer = 20;//текущее состояние в excel
                        double[] sum_serv;
                        sum_serv = new double[services.Count];
                        System.Data.DataTable table_service = new System.Data.DataTable();
                        System.Data.DataTable table_payment = new System.Data.DataTable();
                        int table_index = 0;
                        for (int f = 0; f < DT_Body.Count / 2; )
                        {
                            table_service = DT_Body[table_index * 2];//таблица сервисы
                            table_payment = DT_Body[table_index * 2 + 1];//таблица оплат

                            double sum = 0.0;
                            double sum_outsaldo = 0.0;
                            double sum_izmsaldo = 0.0;

                            for (int d = 0; d < table_service.Rows.Count; d++)
                            {
                                object service_item = table_service.Rows[d][1];
                                object tarif_item = table_service.Rows[d][2];

                                for (int k = 0; k < services.Count; k++)
                                {
                                    if (services[k].ToString() == service_item.ToString())
                                    {
                                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[2 + k] + Convert.ToString(indexer), Type.Missing);

                                        excell.Value2 = table_service.Rows[d][3];//charges

                                        sum = sum + Convert.ToDouble(table_service.Rows[d][3]);

                                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[2 + d] + Convert.ToString(indexer + 1), Type.Missing);
                                        excell.Value2 = tarif_item;//tarif

                                        break;
                                    }
                                }
                                sum_izmsaldo = sum_izmsaldo + Convert.ToDouble(table_service.Rows[d][4]);
                                sum_outsaldo = sum_outsaldo + Convert.ToDouble(table_service.Rows[d][5]);//sum_outsaldo
                            }

                            //загружаем данные с другой таблицы

                            for (int d = 0; d < table_payment.Rows.Count; d++)
                            {
                                excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 4] + Convert.ToString(indexer + 1), Type.Missing);
                                excell.Value2 = table_payment.Rows[d][3];

                                excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 3] + Convert.ToString(indexer + 1), Type.Missing);
                                excell.NumberFormat = "дд.мм.гггг";
                                excell.Value2 = table_payment.Rows[d][1].ToString();

                                excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 2] + Convert.ToString(indexer + 1), Type.Missing);
                                excell.NumberFormat = "@";
                                excell.Value2 = table_payment.Rows[d][2].ToString().Trim();

                                indexer++;
                            }

                            if (table_service.Rows.Count != 0)
                            {
                                if (table_payment.Rows.Count == 0)
                                {
                                    excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 1] + Convert.ToString(indexer + 1), Type.Missing);
                                    excell.Value2 = sum_outsaldo;//вывод sum_outsaldo

                                    excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 5] + Convert.ToString(indexer + 1), Type.Missing);
                                    excell.Value2 = sum_izmsaldo;//вывод sum_izmsaldo
                                }
                                else
                                {
                                    excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 1] + Convert.ToString(indexer), Type.Missing);
                                    excell.Value2 = sum_outsaldo;//вывод sum_outsaldo

                                    excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 5] + Convert.ToString(indexer), Type.Missing);
                                    excell.Value2 = sum_izmsaldo;//вывод sum_izmsaldo
                                }
                                excell = ExcelL.ExlWs.get_Range("B" + Convert.ToString(indexer - table_payment.Rows.Count), Type.Missing);
                                excell.Value2 = sum;//сумма по начислениям для одного месяца
                            }
                            else
                            {
                                if (table_payment.Rows.Count != 0)
                                {
                                    excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 1] + Convert.ToString(indexer), Type.Missing);
                                    excell.Value2 = sum_outsaldo;//вывод sum_outsaldo

                                    excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 5] + Convert.ToString(indexer), Type.Missing);
                                    excell.Value2 = sum_izmsaldo;//вывод sum_izmsaldo

                                    excell = ExcelL.ExlWs.get_Range("B" + Convert.ToString(indexer - table_payment.Rows.Count), Type.Missing);
                                    excell.Value2 = sum;//сумма по начислениям для одного месяца
                                }
                            }

                            for (int h = 0; h < services.Count; h++)
                            {
                                excell = ExcelL.ExlWs.get_Range(exform.BukvaList[2 + h] + Convert.ToString(indexer - table_payment.Rows.Count), Type.Missing);
                                sum_serv[h] = Convert.ToDouble(sum_serv[h]) + Convert.ToDouble(excell.Value2);
                            }

                            if (table_service.Rows.Count != 0)//если по начислениям все пусто
                            {
                                excell = ExcelL.ExlWs.get_Range("A" + Convert.ToString(indexer - table_payment.Rows.Count), Type.Missing);
                                excell.NumberFormat = "@";
                                excell.Value2 = table_service.Rows[0][0].ToString().Substring(0, table_service.Rows[0][0].ToString().Length - 2) + "/20" + table_service.Rows[0][0].ToString().Substring(table_service.Rows[0][0].ToString().Length - 2, 2);

                                excell = ExcelL.ExlWs.get_Range("A" + Convert.ToString(indexer + 1 - table_payment.Rows.Count), Type.Missing);
                                excell.NumberFormat = "@";
                                excell.Value2 = "тариф";
                            }
                            else
                            {
                                if (table_payment.Rows.Count != 0)
                                {
                                    excell = ExcelL.ExlWs.get_Range("A" + Convert.ToString(indexer - table_payment.Rows.Count), Type.Missing);
                                    excell.NumberFormat = "@";
                                    excell.Value2 = table_payment.Rows[0][0].ToString().Substring(0, table_payment.Rows[0][0].ToString().Length - 2) + "/20" + table_payment.Rows[0][0].ToString().Substring(table_payment.Rows[0][0].ToString().Length - 2, 2);

                                    excell = ExcelL.ExlWs.get_Range("A" + Convert.ToString(indexer + 1 - table_payment.Rows.Count), Type.Missing);
                                    excell.NumberFormat = "@";
                                    excell.Value2 = "тариф";
                                }
                            }
                            //if (table_payment.Rows.Count == 0 && f == DT_Body.Count / 2 - 1)
                            //    indexer = indexer - 1;

                            if (table_payment.Rows.Count == 0 && table_service.Rows.Count == 0)
                            { }

                            else
                            {
                                if (table_payment.Rows.Count == 0)
                                    indexer = indexer + 2;
                                else
                                    indexer = indexer + 1;//счетчик
                            }
                            f = f + 1;
                            table_index++;
                        }
                        #region Форматирование тела

                        excell = ExcelL.ExlWs.get_Range("A20", exform.BukvaList[7 + services.Count - 1] + Convert.ToString(indexer - 1));
                        excell.Font.Size = 8;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                        excell = ExcelL.ExlWs.get_Range("A18", exform.BukvaList[7 + services.Count - 1] + Convert.ToString(indexer - 1));
                        excell.Font.Size = 5.5;

                        #region задаем ширину каждой колонке

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 1] + "18", exform.BukvaList[7 + services.Count - 1] + "19");
                        excell.ColumnWidth = 5;

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 2] + "18", exform.BukvaList[7 + services.Count - 2] + "19");
                        excell.ColumnWidth = 4.9;

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 3] + "18", exform.BukvaList[7 + services.Count - 3] + "19");
                        excell.ColumnWidth = 4.9;

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 4] + "18", exform.BukvaList[7 + services.Count - 4] + "19");
                        excell.ColumnWidth = 5;

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 5] + "18", exform.BukvaList[7 + services.Count - 5] + "19");
                        excell.ColumnWidth = 4;

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[2] + "19", exform.BukvaList[7 + services.Count - 6] + "18");
                        excell.ColumnWidth = 5;

                        #endregion

                        excell = ExcelL.ExlWs.get_Range("A18", "A19");
                        excell.Merge(Type.Missing);
                        excell.WrapText = true;

                        excell = ExcelL.ExlWs.get_Range("B18", "B19");
                        excell.Merge(Type.Missing);

                        excell = ExcelL.ExlWs.get_Range("C18", exform.BukvaList[7 + services.Count - 6] + "18");
                        excell.Merge(Type.Missing);

                        excell = ExcelL.ExlWs.get_Range("B20", exform.BukvaList[7 + services.Count - 4] + (indexer + 1).ToString());
                        excell.NumberFormat = "0,00";

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 1] + "20", exform.BukvaList[7 + services.Count - 1] + (indexer + 1).ToString());
                        excell.NumberFormat = "0,00";

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 3] + "20", exform.BukvaList[7 + services.Count - 3] + (indexer + 1).ToString());
                        excell.NumberFormat = "дд.мм.гггг";

                        #endregion

                        #endregion

                        #region Создание футора

                        excell = ExcelL.ExlWs.get_Range("A" + (indexer).ToString(), Type.Missing);
                        excell.Value2 = "Итого:";
                        excell.Font.Size = 8;
                        excell.Font.Bold = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                        double sum_all = 0.0;
                        for (int h = 0; h < (indexer + 1 - 20); h++)
                        {
                            excell = ExcelL.ExlWs.get_Range("B" + (20 + h).ToString(), Type.Missing);
                            sum_all = sum_all + Convert.ToDouble(excell.Value2);
                        }

                        excell = ExcelL.ExlWs.get_Range("B" + (indexer).ToString(), Type.Missing);
                        excell.Font.Size = 8;
                        excell.Font.Bold = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = sum_all;

                        for (int s = 0; s < sum_serv.Length; s++)
                        {
                            excell = ExcelL.ExlWs.get_Range(exform.BukvaList[2 + s] + (indexer).ToString(), Type.Missing);
                            excell.Font.Size = 8;
                            excell.Font.Bold = true;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.Value2 = sum_serv[s];
                        }

                        double sum_sl = 0.0;
                        for (int a = 0; a < (indexer + 1 - 20); a++)
                        {
                            excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 4] + (20 + a).ToString(), Type.Missing);
                            sum_sl = sum_sl + Convert.ToDouble(excell.Value2);
                        }

                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 4] + (indexer).ToString(), Type.Missing);
                        excell.Font.Size = 8;
                        excell.Font.Bold = true;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = sum_sl;



                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + 3).ToString(), "B" + (indexer + 3).ToString());
                        excell.Merge(Type.Missing);
                        excell.RowHeight = 28.5;
                        int days = DateTime.DaysInMonth(Convert.ToInt32("20" + yy_to), Convert.ToInt32(mm_to));
                        string date = days.ToString() + "/" + mm_to + "/" + yy_to;
                        excell.Value2 = "Задолженность на " + date + ":";
                        excell.WrapText = true;
                        excell.Font.Size = 8;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                        excell = ExcelL.ExlWs.get_Range(exform.BukvaList[7 + services.Count - 1] + (indexer - 1).ToString(), Type.Missing);
                        double debt = Convert.ToDouble(excell.Value2);
                        excell = ExcelL.ExlWs.get_Range("C" + (indexer + 3).ToString(), "D" + (indexer + 3).ToString());
                        excell.Merge(Type.Missing);
                        excell.Font.Size = 8;
                        excell.NumberFormat = "0,00";
                        excell.Font.Bold = true;
                        excell.Value2 = debt;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                        excell = ExcelL.ExlWs.get_Range("C" + (indexer + 4).ToString(), Type.Missing);
                        excell.Font.Size = 8;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = "Дата";

                        excell = ExcelL.ExlWs.get_Range("D" + (indexer + 4).ToString(), "E" + (indexer + 4).ToString());//объединяем "в том числе по услугам"
                        excell.Merge(Type.Missing);
                        excell.Value2 = DateTime.Now.ToString("dd/MM/yy");

                        excell.Font.Size = 8;
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                        #region Старый вариант
                        //excell = ExcelL.ExlWs.get_Range("F" + (indexer + 3).ToString(), "G" + (indexer + 4).ToString());
                        //excell.Merge(Type.Missing);
                        //excell.Value2 = "Директор";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                        //excell = ExcelL.ExlWs.get_Range("E" + (indexer + 5).ToString(), "G" + (indexer + 6).ToString());
                        //excell.Merge(Type.Missing);
                        //excell.Value2 = "Начальник ПЭО";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                        //excell = ExcelL.ExlWs.get_Range("E" + (indexer + 7).ToString(), "G" + (indexer + 8).ToString());
                        //excell.Merge(Type.Missing);
                        ////excell.Value2 = "Начальник ОНУПН";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                        //excell = ExcelL.ExlWs.get_Range("H" + (indexer + 3).ToString(), "I" + (indexer + 4).ToString());
                        //excell.Merge(Type.Missing);
                        //excell.Value2 = "____________";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                        //excell = ExcelL.ExlWs.get_Range("H" + (indexer + 5).ToString(), "I" + (indexer + 6).ToString());
                        //excell.Merge(Type.Missing);
                        //excell.Value2 = "____________";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                        //excell = ExcelL.ExlWs.get_Range("H" + (indexer + 7).ToString(), "I" + (indexer + 8).ToString());
                        //excell.Merge(Type.Missing);
                        //excell.Value2 = "____________";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                        //excell = ExcelL.ExlWs.get_Range("J" + (indexer + 3).ToString(), "K" + (indexer + 4).ToString());
                        //excell.Merge(Type.Missing);
                        //excell.Value2 = "Чернышов М.Г. ";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                        //excell = ExcelL.ExlWs.get_Range("J" + (indexer + 5).ToString(), "K" + (indexer + 6).ToString());
                        //excell.Merge(Type.Missing);
                        //excell.Value2 = "Соковых И.А. ";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                        //excell = ExcelL.ExlWs.get_Range("J" + (indexer + 7).ToString(), "K" + (indexer + 8).ToString());
                        //excell.Merge(Type.Missing);
                        //excell.Value2 = "Старкова Л.А.";
                        //excell.Font.Size = 8;
                        //excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        //excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        #endregion

                        indexer = indexer + 6;

                        #region --заполнение параметров подписи отчета
                        /* 579 - Наименование должности бухгалтера
                           1047 - ФИО руководителя ПУС 
                           1048 - Должность руководителя ПУС */
                        var finderPrm = new Prm
                        {
                            nzp_user = finder.nzp_user,
                            pref = finder.pref,
                            prm_num = 10,
                            spis_prm = "579, 1047, 1048",
                            is_actual = 1,
                            date_begin = DateTime.Now.ToShortDateString()
                        };

                        var parPod = new DbParameters();
                        parPod.FindPrm(finderPrm, out ret);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("class LsPuVipiskaControl, метод prepareReport \n " +
                                                " Ошибка формирования списка параметров \"подписи\" :" + ret.text, MonitorLog.typelog.Error, true);
                            return ret;
                        }
                        List<Prm> listPrms = parPod.GetPrm(finderPrm, out ret);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("class LsPuVipiskaControl, метод prepareReport \n " +
                                                " Ошибка формирования списка параметров \"подписи\" :" + ret.text, MonitorLog.typelog.Error, true);
                            return ret;
                        }
                        string pasportDol = listPrms.Find(x => x.nzp_prm == 579) != null
                                ? listPrms.Find(x => x.nzp_prm == 579).val_prm
                                : string.Empty,
                            nachalFio = listPrms.Find(x => x.nzp_prm == 1047) != null
                                ? listPrms.Find(x => x.nzp_prm == 1047).val_prm
                                : string.Empty,
                            nachalDol = listPrms.Find(x => x.nzp_prm == 1048) != null
                                ? listPrms.Find(x => x.nzp_prm == 1048).val_prm
                                : string.Empty,
                            pasportFio = finder.webUname;


                        excell = ExcelL.ExlWs.Range["A" + (indexer + 2), "B" + (indexer + 2)];
                        excell.Merge(Type.Missing);
                        excell.Font.Name = "Calibri";
                        excell.Font.Size = 8;
                        excell.WrapText = true;
                        excell.Value2 = nachalDol;

                        excell = ExcelL.ExlWs.Range["C" + (indexer + 2), Type.Missing];
                        excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;


                        excell = ExcelL.ExlWs.Range["D" + (indexer + 2), Type.Missing];
                        excell.Font.Name = "Calibri";
                        excell.Font.Size = 8;
                        excell.Value2 = Utils.GetCorrectFIO(nachalFio);

                        excell = ExcelL.ExlWs.Range["A" + (indexer + 4), "B" + (indexer + 4)];
                        excell.Merge(Type.Missing);
                        excell.Font.Name = "Calibri";
                        excell.Font.Size = 8;
                        excell.WrapText = true;
                        excell.Value2 = pasportDol;

                        excell = ExcelL.ExlWs.Range["C" + (indexer + 4), Type.Missing];
                        excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;

                        excell = ExcelL.ExlWs.Range["D" + (indexer + 4), Type.Missing];
                        excell.Font.Name = "Calibri";
                        excell.Font.Size = 8;
                        excell.Value2 = pasportFio; 
                        #endregion

                    }
                    else
                    {
                        excell = ExcelL.ExlWs.get_Range("A1", "I22");
                        excell.Font.Size = 8;
                        exform.AllColumnsAutoFit(8, 22, ref ExcelL.ExlWs);

                        excell = ExcelL.ExlWs.get_Range("A18", "H18");
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                        exform.SetBorder(ref ExcelL.ExlWs, "A19", "H22");
                        excell = ExcelL.ExlWs.get_Range("A19", "H22");
                        excell.Merge(Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = "нет данных";

                    }
                        #endregion

                    #region Сохранение

                    if (ret.result)
                    {
                        fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "Spravka_v_sud" + finder.nzp_user) + ".xls";//"SpLs_" + nzp_user + ".xlsx";
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
            else
            {
                return ret;
            }
        }

        //Отчет: Извещение за месяц
        public Returns GetNoticeCalcs(Ls finder, string yy, string mm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            DBCalcs calc = new DBCalcs();
            List<System.Data.DataTable> DT_Body = calc.NoticeCalcs(finder, yy, mm, out ret);

            #region Определение заголовков и типов


            Dictionary<string, double> footer_one = new Dictionary<string, double>();


            Dictionary<string, string> Dic = new Dictionary<string, string>();
            Dic.Add("l/s", finder.num_ls.ToString());//лиц счет
            Dic.Add("jeu", finder.geu);//жэу
            string strMonthName = "";

            #region Получаем название месяца

            switch (Convert.ToInt32(mm))
            {
                case 1:
                    strMonthName = "ЯНВАРЬ";
                    break;
                case 2:
                    strMonthName = "ФЕВРАЛЬ";
                    break;
                case 3:
                    strMonthName = "МАРТ";
                    break;
                case 4:
                    strMonthName = "АПРЕЛЬ";
                    break;
                case 5:
                    strMonthName = "МАЙ";
                    break;
                case 6:
                    strMonthName = "ИЮНЬ";
                    break;
                case 7:
                    strMonthName = "ИЮЛЬ";
                    break;
                case 8:
                    strMonthName = "АВГУСТ";
                    break;
                case 9:
                    strMonthName = "СЕНТЯБРЬ";
                    break;
                case 10:
                    strMonthName = "ОКТЯБРЬ";
                    break;
                case 11:
                    strMonthName = "НОЯБРЬ";
                    break;
                case 12:
                    strMonthName = "ДЕКАБРЬ";
                    break;
                default:
                    break;
            }
            #endregion

            Dic.Add("month", strMonthName);//месяц
            Dic.Add("year", "20" + yy);//год
            Dic.Add("fio", finder.fio);//фио
            Dic.Add("adr", finder.adr);//адрес

            #region Квартирные параметры

            string[] kv_params = new string[6];
            Prm finderp = new Prm();
            finderp.nzp_user = finder.nzp_user;//web_user.nzp_user;
            finderp.nzp_kvar = finder.nzp_kvar;
            finderp.pref = finder.pref;
            finderp.prm_num = 1;
            finderp.rows = 100;
            finderp.month_ = Convert.ToInt32(mm);
            finderp.year_ = Convert.ToInt32("20" + yy);

            List<Prm> SpisPrm = new List<Prm>();

            DbParameters dbPrm2 = new DbParameters();

            dbPrm2.FindPrm(finderp, out ret);
            SpisPrm = dbPrm2.GetPrm(finderp, out ret);

            finderp.prm_num = 10;
            DbParameters dbPrm1 = new DbParameters();
            dbPrm1.FindPrm(finderp, out ret);
            SpisPrm.AddRange(dbPrm1.GetPrm(finderp, out ret));

            if (ret.result)
            {

                for (int i = 0; i < SpisPrm.Count; i++)
                {
                    #region Разбираем параметры

                    switch (SpisPrm[i].nzp_prm)
                    {
                        case 8:
                            if (SpisPrm[i].val_prm == "да")
                            {
                                kv_params[0] = "приватизирована";
                            }
                            else
                            {
                                kv_params[0] = "не приватизирована";
                            }
                            break;
                        case 4:
                            {
                                string pl = System.Convert.ToDecimal(SpisPrm[i].val_prm).ToString("f2");
                                kv_params[1] = pl;
                            }
                            break;
                        case 5: kv_params[2] = SpisPrm[i].val_prm.ToString(); break;
                        case 2005: kv_params[3] = SpisPrm[i].val_prm.ToString(); break;
                        case 10: kv_params[4] = SpisPrm[i].val_prm.ToString(); break;
                        case 107: kv_params[5] = SpisPrm[i].val_prm.ToString(); break;
                        case 88: Dic.Add("ERCName", SpisPrm[i].val_prm.ToString()); break;
                        default:
                            break;
                    }
                    #endregion
                }
            }
            dbPrm2.Close();

            for (int g = 0; g < kv_params.Length; g++)
            {
                if (g == 4 && kv_params[g] == null)
                {
                    kv_params[g] = "0";
                }
            }

            #endregion


            if (kv_params[1] != null)
                Dic.Add("o/p", kv_params[1].ToString());//общая площадь
            if (kv_params[0] != null)
                Dic.Add("tips", kv_params[0].ToString());//тип собственности
            if (kv_params[5] != null)
                Dic.Add("kolkom", kv_params[5].ToString());//тип собственности
            string prms = "";
            if (kv_params[2] != null)
                prms += kv_params[2].ToString();
            if (kv_params[3] != null)
                prms += "/" + kv_params[3].ToString();
            if (kv_params[4] != null)
                prms += "/" + kv_params[4].ToString();
            Dic.Add("pfo", prms);//Прописано/факт/отсутствует
          
            #endregion

            try
            {
                int indexer = 9;

                ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("izvechenie_za_mesyac.xls"), Dic);
                                                                           
                if (DT_Body[0].Rows.Count != 0)
                {
                    #region Создание тела

                    #region Первая таблица

                    double sum_nedop = 0.0;
                    double reds = 0.0;
                    double blacks = 0.0;
                    double r_sum_tarif = 0.0;
                    double sum_charge = 0.0;

                    for (int r = 0; r < DT_Body[0].Rows.Count; r++)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + r).ToString(), Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.WrapText = true;
                        excell.Value2 = DT_Body[0].Rows[r][0].ToString();

                        excell = ExcelL.ExlWs.get_Range("B" + (indexer + r).ToString(), Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.NumberFormat = "0,00";
                        excell.Value2 = DT_Body[0].Rows[r][1];

                        excell = ExcelL.ExlWs.get_Range("C" + (indexer + r).ToString(), Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.NumberFormat = "0,00";
                        excell.Value2 = DT_Body[0].Rows[r][2];
                        r_sum_tarif = r_sum_tarif + Convert.ToDouble(DT_Body[0].Rows[r][2]);


                        excell = ExcelL.ExlWs.get_Range("E" + (indexer + r).ToString(), Type.Missing);
                        excell.NumberFormat = "0,00";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = DT_Body[0].Rows[r][3];


                        excell = ExcelL.ExlWs.get_Range("F" + (indexer + r).ToString(), Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = DT_Body[0].Rows[r][4].ToString();


                        excell = ExcelL.ExlWs.get_Range("H" + (indexer + r).ToString(), Type.Missing);
                        excell.NumberFormat = "0,00";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = DT_Body[0].Rows[r][5];
                        sum_charge = sum_charge + Convert.ToDouble(DT_Body[0].Rows[r][5]);

                        sum_nedop = sum_nedop + Convert.ToDouble(DT_Body[0].Rows[r][6]);
                        reds = reds + Convert.ToDouble(DT_Body[0].Rows[r][7]);
                        blacks = blacks + Convert.ToDouble(DT_Body[0].Rows[r][8]);
                    }


                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (DT_Body[0].Rows.Count + indexer - 1).ToString());
                    excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlDot, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);

                    excell = ExcelL.ExlWs.get_Range("B" + (indexer + DT_Body[0].Rows.Count).ToString(), Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Value2 = "Всего:";

                    excell = ExcelL.ExlWs.get_Range("C" + (DT_Body[0].Rows.Count + indexer).ToString(), Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Value2 = r_sum_tarif;

                    excell = ExcelL.ExlWs.get_Range("H" + (DT_Body[0].Rows.Count + indexer).ToString(), Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Value2 = sum_charge;

                    excell = ExcelL.ExlWs.get_Range("G" + (DT_Body[0].Rows.Count + indexer).ToString(), Type.Missing);
                    excell.NumberFormat = "0,00";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Value2 = 0.00;


                    footer_one.Add("Начислен возврат за непредоставленные к/услуги:", sum_nedop);
                    footer_one.Add("Начислено разовым красным:", reds);
                    footer_one.Add("Начислено разовым черным:", blacks);
                    footer_one.Add("Итого к оплате:", sum_charge);



                    int i = 2;
                    foreach (KeyValuePair<string, double> a in footer_one)
                    {
                        excell = ExcelL.ExlWs.get_Range("C" + (DT_Body[0].Rows.Count + indexer + i).ToString(), "G" + (DT_Body[0].Rows.Count + indexer + i).ToString());
                        excell.Merge(Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = a.Key;

                        excell = ExcelL.ExlWs.get_Range("H" + (DT_Body[0].Rows.Count + indexer + i).ToString(), Type.Missing);
                        excell.Merge(Type.Missing);
                        excell.NumberFormat = "0,00";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Value2 = a.Value;
                        i++;
                    }



                    excell = ExcelL.ExlWs.get_Range("C" + (DT_Body[0].Rows.Count + indexer + 1 + footer_one.Count).ToString(), "H" + (DT_Body[0].Rows.Count + indexer + 1 + footer_one.Count).ToString());
                    excell.Font.Bold = true;
                    indexer = DT_Body[0].Rows.Count + indexer + 1 + footer_one.Count;
                    #endregion

                    if (DT_Body[1].Rows.Count != 0)
                    {
                        #region Вторая таблица

                        #region Шапка второй таблицы

                        //indexer = DT_Body[0].Rows.Count + indexer + 1 + footer_one.Count;

                        excell = ExcelL.ExlWs.get_Range("B" + (indexer + 2).ToString(), "F" + (indexer + 2).ToString());
                        excell.Merge(Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Font.Bold = true;
                        excell.Font.Size = 11;
                        excell.Value2 = "Калькуляция начислений по видам услуг";

                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + 3).ToString(), Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Font.Bold = true;
                        excell.Font.Size = 10;
                        excell.Value2 = "содерж. жилья";


                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + 4).ToString(), "H" + (indexer + 4).ToString());
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlDot;
                        excell.WrapText = true;

                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + 4).ToString(), "C" + (indexer + 4).ToString());
                        excell.Merge(Type.Missing);
                        excell.Value2 = "Статья затрат";

                        excell = ExcelL.ExlWs.get_Range("D" + (indexer + 4).ToString(), Type.Missing);
                        excell.Value2 = "Тариф для расчета";

                        excell = ExcelL.ExlWs.get_Range("E" + (indexer + 4).ToString(), Type.Missing);
                        excell.Value2 = "Тариф для льгот";

                        excell = ExcelL.ExlWs.get_Range("F" + (indexer + 4).ToString(), Type.Missing);
                        excell.Merge(Type.Missing);
                        excell.Value2 = "Начислено без учета льгот";

                        excell = ExcelL.ExlWs.get_Range("G" + (indexer + 4).ToString(), Type.Missing);
                        excell.Merge(Type.Missing);
                        excell.Value2 = "Льготная скидка";

                        excell = ExcelL.ExlWs.get_Range("H" + (indexer + 4).ToString(), Type.Missing);
                        excell.Value2 = "Начислено к оплате";

                        #endregion

                        #region Выгрузка второй таблицы

                        double rsum_ = 0.0;
                        double sumcharge_ = 0.0;

                        for (int p = 0; p < DT_Body[1].Rows.Count; p++)
                        {
                            excell = ExcelL.ExlWs.get_Range("A" + (indexer + 5 + p).ToString(), "C" + (indexer + 5 + p).ToString());
                            excell.Merge(Type.Missing);
                            excell.Font.Size = 9;
                            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excell.Value2 = DT_Body[1].Rows[p][0];

                            excell = ExcelL.ExlWs.get_Range("D" + (indexer + 5 + p).ToString(), Type.Missing);
                            excell.Value2 = DT_Body[1].Rows[p][1];

                            excell = ExcelL.ExlWs.get_Range("E" + (indexer + 5 + p).ToString(), Type.Missing);
                            excell.Value2 = DT_Body[1].Rows[p][2];

                            excell = ExcelL.ExlWs.get_Range("F" + (indexer + 5 + p).ToString(), Type.Missing);
                            excell.Value2 = DT_Body[1].Rows[p][3];
                            rsum_ = rsum_ + Convert.ToDouble(DT_Body[1].Rows[p][3]);

                            excell = ExcelL.ExlWs.get_Range("G" + (indexer + 5 + p).ToString(), Type.Missing);
                            excell.Value2 = 0.00;

                            excell = ExcelL.ExlWs.get_Range("H" + (indexer + 5 + p).ToString(), Type.Missing);
                            excell.Value2 = DT_Body[1].Rows[p][4];
                            sumcharge_ = sumcharge_ + Convert.ToDouble(DT_Body[1].Rows[p][4]);
                        }

                        excell = ExcelL.ExlWs.get_Range("D" + (indexer + 5).ToString(), "H" + (indexer + 5 + DT_Body[1].Rows.Count).ToString());
                        excell.Font.Size = 9;
                        excell.NumberFormat = "0,00";

                        excell = ExcelL.ExlWs.get_Range("F" + (indexer + 5 + DT_Body[1].Rows.Count).ToString(), Type.Missing);
                        excell.NumberFormat = "0,00";
                        excell.Font.Size = 9;
                        excell.Font.Bold = true;
                        excell.Value2 = rsum_;

                        excell = ExcelL.ExlWs.get_Range("G" + (indexer + 5 + DT_Body[1].Rows.Count).ToString(), Type.Missing);
                        excell.NumberFormat = "0,00";
                        excell.Font.Size = 9;
                        excell.Font.Bold = true;
                        excell.Value2 = 0.00;

                        excell = ExcelL.ExlWs.get_Range("H" + (indexer + 5 + DT_Body[1].Rows.Count).ToString(), Type.Missing);
                        excell.NumberFormat = "0,00";
                        excell.Font.Size = 9;
                        excell.Font.Bold = true;
                        excell.Value2 = sumcharge_;

                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + 5).ToString(), "H" + (indexer + DT_Body[1].Rows.Count + 4).ToString());
                        excell.BorderAround(Microsoft.Office.Interop.Excel.XlLineStyle.xlDot, Microsoft.Office.Interop.Excel.XlBorderWeight.xlThin, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic, Microsoft.Office.Interop.Excel.XlColorIndex.xlColorIndexAutomatic);


                        excell = ExcelL.ExlWs.get_Range("A1", "H" + (indexer + 5 + DT_Body[1].Rows.Count).ToString());
                        excell.Font.Name = "Times New Roman";

                        excell = ExcelL.ExlWs.get_Range("F1", Type.Missing);
                        excell.ColumnWidth = 8.14;

                        indexer = indexer + 2 + DT_Body[1].Rows.Count;

                        #endregion

                        #endregion
                    }

                    #endregion
                    #region заполнение параметров подписи отчета
                    /* 579 - Наименование должности бухгалтера
                       1047 - ФИО руководителя ПУС 
                       1048 - Должность руководителя ПУС */
                    var finderPrm = new Prm
                    {
                        nzp_user = finder.nzp_user,
                        pref = finder.pref,
                        prm_num = 10,
                        spis_prm = "579, 1047, 1048",
                        is_actual = 1,
                        date_begin = DateTime.Now.ToShortDateString()
                    };

                    var parPod = new DbParameters();
                    parPod.FindPrm(finderPrm, out ret);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("class LsPuVipiskaControl, метод prepareReport \n " +
                                            " Ошибка формирования списка параметров \"подписи\" :" + ret.text, MonitorLog.typelog.Error, true);
                        return ret;
                    }
                    List<Prm> listPrms = parPod.GetPrm(finderPrm, out ret);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("class LsPuVipiskaControl, метод prepareReport \n " +
                                            " Ошибка формирования списка параметров \"подписи\" :" + ret.text, MonitorLog.typelog.Error, true);
                        return ret;
                    }
                    string pasportDol = listPrms.Find(x => x.nzp_prm == 579) != null
                            ? listPrms.Find(x => x.nzp_prm == 579).val_prm
                            : string.Empty,
                        nachalFio = listPrms.Find(x => x.nzp_prm == 1047) != null
                            ? listPrms.Find(x => x.nzp_prm == 1047).val_prm
                            : string.Empty,
                        nachalDol = listPrms.Find(x => x.nzp_prm == 1048) != null
                            ? listPrms.Find(x => x.nzp_prm == 1048).val_prm
                            : string.Empty,
                        pasportFio = finder.webUname;


                    excell = ExcelL.ExlWs.Range["A" + (indexer + 2), "B" + (indexer + 2)];
                    excell.Merge(Type.Missing);
                    excell.Font.Name = "Times New Roman";
                    excell.WrapText = true;
                    excell.Value2 = nachalDol;
                    
                    excell = ExcelL.ExlWs.Range["C" + (indexer + 2), Type.Missing];
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
                        

                    excell = ExcelL.ExlWs.Range["D" + (indexer + 2), Type.Missing];
                    excell.Font.Name = "Times New Roman";
                    excell.Value2 = Utils.GetCorrectFIO(nachalFio);

                    excell = ExcelL.ExlWs.Range["A" + (indexer + 4), "B" + (indexer + 4)];
                    excell.Merge(Type.Missing);
                    excell.Font.Name = "Times New Roman";
                    excell.WrapText = true;
                    excell.Value2 = pasportDol;

                    excell = ExcelL.ExlWs.Range["C" + (indexer + 4), Type.Missing];
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;

                    excell = ExcelL.ExlWs.Range["D" + (indexer + 4), Type.Missing];
                    excell.Font.Name = "Times New Roman";
                    excell.Value2 = pasportFio;
                    #endregion
                }
                else
                {
                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (indexer + 4).ToString());
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
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "izvechenie_za_mesyac" + finder.nzp_user) + ".xls";//"SpLs_" + nzp_user + ".xlsx";
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


    
     
      

        //Отчет: Сверка расчетов с жильцом по состоянию на
        public void GetVerifCalcs(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Сверка расчетов с жильцом\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.finder.nzp_user, "empty", 2, "Отчет \"Сверка расчетов с жильцом\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.finder.nzp_user, "1", 2, "", time);


            ret = this.GetVerifCalcs(cont.finder, cont.from_yy, cont.from_mm, cont.to_yy, cont.to_mm, ref fileName);
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

        //Отчет: справка для предъявления в суд
        public void GetDebtCalcs(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка для предъвления в суд\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.finder.nzp_user, "empty", 2, "Отчет \"Справка для предъвления в суд\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.finder.nzp_user, "1", 2, "", time);


            ret = this.GetDebtCalcs(cont.finder, cont.from_yy, cont.from_mm, cont.to_yy, cont.to_mm, ref fileName);
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



        //Отчет: извещение за месяц
        public void GetNoticeCalcs(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Извещение за месяц\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.finder.nzp_user, "empty", 2, "Отчет \"Извещение за месяц\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.finder.nzp_user, "1", 2, "", time);


            ret = this.GetNoticeCalcs(cont.finder, cont.yy, cont.mm, ref fileName);
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
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }


        //Отчет: калькуляция тарифа содержание жилья
        public void GetCalcTarif(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Карточка аналитического учета по услуге \"содержание жилья\"\" ", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Карточка аналитического учета по услуге \"содержание жилья\"\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetCalcTarif(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
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




        //Отчет: Протокол расчета значений коэффициента ОДН
        public void GetProtCalcOdn(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Протокол расчитанных занчений ОДН\"", ref time2, cont.comment);
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
                "Отчет \"Протокол рассчитанных значений ОДН\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetProtCalcOdn(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
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

        //Отчет: Протокол расчета значений коэффициента ОДН расширенный
        public void GetProtCalcOdn2(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Протокол расчитанных занчений ОДН расчширенный\"", ref time2, cont.comment);
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
                "Отчет \"Протокол расчета значений ОДН расширенный\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetProtCalcOdn2(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
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

    }
}
