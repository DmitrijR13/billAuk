using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FastReport;
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


   

      
        public Returns GetSpravSuppNachHar(Prm prm, string nzpUser, ref string fileName)
        {
            Returns ret;

            //создание dataTable
            var exR = new ExcelRep();
            DataTable dt = exR.GetSpravSuppNachHar(prm, out ret, nzpUser);

            ExcelLoader excelL = null;
            try
            {
                excelL = new ExcelLoader();
                excelL.ExlWs.Rows.Font.Name = "Arial";
                // ExcelL.ExlWs.PageSetup.Zoom = "100%";

                if (dt != null)
                {

                    


                    DataRow[] drSelect = dt.Select("tip = 1");

                    if (drSelect.Any())
                    {
                        DataTable dtSelect = drSelect.CopyToDataTable();
                        if (dtSelect.Rows.Count > 0)
                            MakeListSpravSuppNachHar(dtSelect, excelL, 1, prm, nzpUser);
                        dtSelect.Clear();
                        drSelect = dt.Select("tip = 2");

                        string oldGeu = "";
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
                                    MakeListSpravSuppNachHar(dtSelect, excelL, j, prm, nzpUser);
                                    dtSelect.Clear();
                                    j++;
                                }
                            }
                        }

                    }
                    excelL.SortWs();
                    ((Worksheet)excelL.ExlWb.Sheets[1]).Select(Type.Missing);



                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNachHar" +
                            prm.nzp_area + "_" +
                            prm.nzp_geu + "_" +
                            prm.year_ + "_" +
                            prm.month_
                            + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
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
                    ret.text = "Создание GetSpravSuppNachHar DataTable : ОШИБКА!";

                    fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNach" +
                                                                      prm.nzp_area + "_" +
                                                                      prm.nzp_geu + "_" +
                                                                      prm.year_ + "_" +
                                                                      prm.month_
                                                                      + "_" + nzpUser) + ".xls";
                    ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                    excelL.ExlWb.Close();
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
                        fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNachHar" +
                            prm.nzp_area + "_" +
                            prm.nzp_geu + "_" +
                            prm.year_ + "_" +
                            prm.month_
                            + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет SpravSuppNachHar", MonitorLog.typelog.Warn, true);
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

        public Returns GetSpravHasDolg(string pref, string nzpKvar, string year, string month, string nzpUser, ref string fileName)
        {
            Returns ret;

            const string numLS = "";
            ////создание dataTable
            var exR = new ExcelRep();
            DataTable dt = exR.GetSpravHasDolg(nzpKvar, year, month, out ret, nzpUser);
            ExcelLoader excelL = null;
            try
            {
                excelL = new ExcelLoader();

                if (dt != null)
                {

                 

                    if (dt.Rows.Count > 0)
                    {
                        #region Создаем тело
                        Range excells1;

                        //спустили шапку
                        excells1 = excelL.ExlWs.get_Range("A1", "F1");
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "СПРАВКА О НАЛИЧИИ ДОЛГА";
                        excells1.Font.Bold = true;
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                        excells1 = excelL.ExlWs.get_Range("A4", "F4");
                        excells1.Merge(Type.Missing);
                        excells1.Font.Bold = true;
                        //string ERC_name = "";

                        //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
                        //    ERC_name = "Н-ска";
                        //else
                        //    ERC_name = "Самары";

                        //excells1.FormulaR1C1 = "по МП городского округа " + ERC_name + " ''ЕИРЦ''";
                        string sName = SetReportHeader();
                        excells1.FormulaR1C1 = sName;
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        excells1 = excelL.ExlWs.get_Range("A5", "F5");
                        excells1.Merge(Type.Missing);
                        excells1.Font.Bold = true;
                        excells1.FormulaR1C1 = "Лицевой счет " + dt.Rows[0]["pkod10"].ToString().Trim();
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        excells1 = excelL.ExlWs.get_Range("A7", "F7");
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "Квартиросъемщик (собственник) " + dt.Rows[0]["fio"].ToString().Trim() + ",";

                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        excells1 = excelL.ExlWs.get_Range("A8", "F8");
                        excells1.Merge(Type.Missing);
                        string adres = "ул." + dt.Rows[0]["ulica"].ToString().Trim() + " " +
                            " д." + dt.Rows[0]["ndom"].ToString().Trim();

                        if (dt.Rows[0]["nkor"].ToString().Trim() != "-")
                            adres = adres + " корп." + dt.Rows[0]["nkor"].ToString().Trim();

                        adres = adres + " кв." + dt.Rows[0]["nkvar"].ToString().Trim();

                        if (dt.Rows[0]["nkvar_n"].ToString().Trim() != "-")
                            adres = adres + " комн." + dt.Rows[0]["nkvar_n"].ToString().Trim();


                        excells1.FormulaR1C1 = "Проживающий по адресу " + adres + ",";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        excells1 = excelL.ExlWs.get_Range("A9", "F9");
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "имеет долг по оплате жилищно-коммунальных услуг на  " +
                            DateTime.Today.ToShortDateString();
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        excells1 = excelL.ExlWs.get_Range("A10", "F10");
                        excells1.Merge(Type.Missing);
                        decimal dl;

                        Decimal.TryParse(dt.Rows[0]["sum_outsaldo"].ToString(), out dl);

                        excells1.FormulaR1C1 = "В сумме  " + dl.ToString("0 руб. 00 коп.") + ".";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        excelL.ExlWs.Cells[12, 1] = "Оплачено в текущем месяце: ";
                        Decimal.TryParse(dt.Rows[0]["sum_money"].ToString(), out dl);
                        excelL.ExlWs.Cells[12, 2] = dl.ToString("0 руб. 00 коп.") + ".";
                        Decimal.TryParse(dt.Rows[0]["sum_charge"].ToString(), out dl);
                        excelL.ExlWs.Cells[13, 1] = "Начислено в текущем месяце: ";
                        excelL.ExlWs.Cells[13, 2] = dl.ToString("0 руб. 00 коп.") + ".";


                        excells1 = excelL.ExlWs.get_Range("A15", "F15");
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "Дана для предъявления по месту требования ";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        //excells1 = excelL.ExlWs.get_Range("A18", "F18");
                        //excells1.Merge(Type.Missing);
                        //excells1.FormulaR1C1 = "Бухгалтер ___________________________ ";
                        //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                        excells1 = excelL.ExlWs.get_Range("A1", "F20");
                        excells1.Font.Size = 10;
                        excells1.EntireColumn.AutoFit();
                        #endregion

                        #region --заполнение параметров подписи отчета
                        /* 579 - Наименование должности бухгалтера
                           1047 - ФИО руководителя ПУС 
                           1048 - Должность руководителя ПУС */
                        int nzp_user;
                        const int fontSize = 10, 
                            indexer = 18;
                        const string fontName = "Calibri";
                        Int32.TryParse(nzpUser, out nzp_user);
                        var finderPrm = new Prm
                        {
                            nzp_user = nzp_user,
                            pref = pref,   
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
                                : string.Empty;
                        
                        //получить пользователя
                        var client = new DbAdminClient();
                        var users = client.GetUsers(new User { nzpuser = nzp_user, nzp_user = nzp_user });
                        string pasportFio = (users != null && users.result && users.returnsData != null && users.returnsData.Count > 0)
                            ? Utils.GetCorrectFIO(users.returnsData[0].uname)
                            : string.Empty;

                        excells1 = excelL.ExlWs.Range["A" + indexer, Type.Missing];
                        excells1.Font.Name = fontName;
                        excells1.Font.Size = fontSize;
                        excells1.WrapText = true;
                        excells1.Value2 = nachalDol;

                        excells1 = excelL.ExlWs.Range["B" + indexer, Type.Missing];
                        excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;


                        excells1 = excelL.ExlWs.Range["C" + indexer, Type.Missing];
                        excells1.Font.Name = fontName;
                        excells1.Font.Size = fontSize;
                        excells1.Value2 = Utils.GetCorrectFIO(nachalFio);

                        excells1 = excelL.ExlWs.Range["A" + (indexer + 2), Type.Missing];
                        excells1.Font.Name = fontName;
                        excells1.Font.Size = fontSize;
                        excells1.WrapText = true;
                        excells1.Value2 = pasportDol;

                        excells1 = excelL.ExlWs.Range["B" + (indexer + 2), Type.Missing];
                        excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;

                        excells1 = excelL.ExlWs.Range["C" + (indexer + 2), Type.Missing];
                        excells1.Font.Name = fontName;
                        excells1.Font.Size = fontSize;
                        excells1.Value2 = pasportFio;
                        #endregion

                        #region Сохраняем файл
                        try
                        {

                            fileName = GetFileName(Global.Constants.ExcelDir, "SpravHasDolg_" +
                                numLS + "_" + year + "_" +
                                month + "_" + nzpUser) + ".xls";
                            ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
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
                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravHasDolg DataTable : ОШИБКА!";

                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return ret;
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

        public Returns GetLicSchetExcel(Ls finder, int year, int month, string sumDolg, ref string fileName)
        {
            Returns ret = Utils.InitReturns();


            ExcelLoader excelL = new ExcelLoader();
           
            try
            {
                #region Создаем тело
                Range excells1;
                excelL.ExlWs.Cells[1, 9] = DateTime.Now.ToShortDateString();


                #region Параметры ИРЦ
                var finderp = new Prm
                {
                    nzp_user = finder.nzp_user,
                    nzp_kvar = finder.nzp_kvar,
                    pref = finder.pref,
                    prm_num = 10,
                    rows = 100,
                    month_ = month,
                    year_ = year
                };
                var dbPrm1 = new DbParameters();
                dbPrm1.FindPrm(finderp, out ret);
                List<Prm> spisPrm = dbPrm1.GetPrm(finderp, out ret);
                if (ret.result)
                {

                    foreach (Prm t in spisPrm)
                    {
                        #region Разбираем параметры
                        switch (t.nzp_prm)
                        {
                                //case 88: ExcelL.ExlWs.Cells[1, 1] = SpisPrm[i].val_prm; break;
                            case 81: excelL.ExlWs.Cells[2, 1] = t.val_prm; break;
                            case 96: excelL.ExlWs.Cells[2, 5] = "тел. " + t.val_prm; break;
                        }
                        #endregion
                    }
                }
                dbPrm1.Close();
                string ercName;

                if (Interfaces.Points.IsDemo)
                    ercName = "Н-ска";
                else
                    ercName = "ГУП Самарской области \"ЕИРРЦ\"";

                excelL.ExlWs.Cells[1, 1] = ercName;
                #endregion


                #region Заполнение адреса


                var adr = new DbAdresHard();
                //List<Ls> LsInfo = new List<Ls>();
                ret = Utils.InitReturns();
                List<Ls> lsInfo = adr.LoadLs(finder, out ret);

                if (ret.result && lsInfo != null && lsInfo.Count > 0)
                {

                    string numdom;
                    string numkvar;


                    excells1 = excelL.ExlWs.get_Range("D3", "F3");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = "ЛИЦЕВОЙ СЧЕТ № " + lsInfo[0].pkod.Trim();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;


                    var months = new[] {"","Январь","Февраль",
                     "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                      "Октябрь","Ноябрь","Декабрь"};

                    excells1 = excelL.ExlWs.get_Range("G3", "I3");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Расчетный месяц " + months[month] + " " + year + " года";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


                    excelL.ExlWs.Cells[5, 1] = "ФИО: " + lsInfo[0].fio;

                    excells1 = excelL.ExlWs.get_Range("A10", "I10");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Данные для расчета:";

                    excells1.Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;


                    numdom = lsInfo[0].ndom;
                    numkvar = lsInfo[0].nkvar;

                    if (lsInfo[0].nkor != "-") numdom = numdom + "корп. " + lsInfo[0].nkor;
                    if (lsInfo[0].nkvar_n != "-") numkvar = numkvar + "комн. " + lsInfo[0].nkvar_n;

                    excells1 = excelL.ExlWs.get_Range("A4", "F4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Адрес: " + lsInfo[0].ulica.Trim() + " д. " + numdom + " кв. " + numkvar;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;


                    excelL.ExlWs.Cells[3, 1] = lsInfo[0].geu;

                }
                adr.Close();
                #endregion


                #region Заполнение Квартирных параметров
                excelL.ExlWs.Cells[11, 1] = "Площадь:";
                excelL.ExlWs.Cells[13, 1] = "Жильцы:";
                spisPrm.Clear();
                ret = Utils.InitReturns();
                Prm finderp1 = new Prm();

                finderp1.nzp_user = finder.nzp_user;//web_user.nzp_user;
                finderp1.nzp_kvar = finder.nzp_kvar;
                finderp1.pref = finder.pref;
                finderp1.prm_num = 1;
                finderp1.rows = 100;
                finderp1.month_ = month;
                finderp1.year_ = year;

                DbParameters dbPrm2 = new DbParameters();

                dbPrm2.FindPrm(finderp1, out ret);
                spisPrm = dbPrm2.GetPrm(finderp1, out ret);


                if (ret.result)
                {

                    for (int i = 0; i < spisPrm.Count; i++)
                    {
                        #region Разбираем параметры
                        switch (spisPrm[i].nzp_prm)
                        {
                            case 3:
                                if ((spisPrm[i].val_prm.Trim() == "коммунальное") || (spisPrm[i].val_prm.Trim() == "коммунальная"))
                                {

                                    excelL.ExlWs.Cells[8, 1] = "Квартира: коммунальная";
                                }
                                else
                                {
                                    excelL.ExlWs.Cells[8, 1] = "Квартира: изолированная";
                                }
                                break;
                            case 8:
                                if (spisPrm[i].val_prm.Trim() == "да")
                                {
                                    excelL.ExlWs.Cells[7, 1] = "Статус квартиры: приватизирована";
                                }
                                else
                                {
                                    excelL.ExlWs.Cells[7, 1] = "Статус квартиры: не приватизирована";
                                }
                                break;

                            case 4:
                                {
                                    string pl = Convert.ToDecimal(spisPrm[i].val_prm.Replace('.', ',')).ToString("f2");
                                    excelL.ExlWs.Cells[12, 1] = "Общая площадь: " + pl;
                                }
                                break;
                            case 6:
                                {
                                    string pl = Convert.ToDecimal(spisPrm[i].val_prm.Replace('.', ',')).ToString("f2");
                                    excelL.ExlWs.Cells[12, 3] = "Жилая площадь: " + pl;
                                } break;
                            case 2005: excelL.ExlWs.Cells[14, 1] = "Прописано " + spisPrm[i].val_prm; break;
                            case 107: excelL.ExlWs.Cells[6, 1] = "Количество комнат: " + spisPrm[i].val_prm; break;
                            case 5: excelL.ExlWs.Cells[14, 3] = "Фактически проживает " + spisPrm[i].val_prm; break;
                            
                        }
                        #endregion

                    }
                }
                dbPrm2.Close();

                #region Примечание к ЛС
                Prm finderp2 = new Prm();

                finderp2.nzp_user = finder.nzp_user;//web_user.nzp_user;
                finderp2.nzp_kvar = finder.nzp_kvar;
                finderp2.nzp = finder.nzp_kvar;
                finderp2.pref = finder.pref;
                finderp2.prm_num = 18;
                finderp2.rows = 10;
                finderp2.month_ = month;
                finderp2.year_ = year;
                finderp2.nzp_prm = 2012;

                ret = Utils.InitReturns();
                var dbPrm3 = new DbParameters();
                Prm primech = dbPrm3.FindPrmValue(finderp2, out ret);

                if (ret.result)
                {
                    excells1 = excelL.ExlWs.get_Range("A9", "I9");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Примечание: " + primech.val_prm;
                    excells1.WrapText = true;
                    excells1.Font.Size = 8;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                }
                dbPrm3.Close();
                #endregion


                #region Открыт ЛС
                var finderp3 = new Prm();
                finderp3.nzp_user = finder.nzp_user;//web_user.nzp_user;
                finderp3.nzp_kvar = finder.nzp_kvar;
                finderp3.nzp = finder.nzp_kvar;
                finderp3.pref = finder.pref;
                finderp3.prm_num = 3;
                finderp3.rows = 10;
                finderp3.month_ = month;
                finderp3.year_ = year;
                finderp3.nzp_prm = 51;

                ret = Utils.InitReturns();
                DbParameters cliPrm2 = new DbParameters();
                primech = cliPrm2.FindPrmValue(finderp3, out ret);

                if (ret.result)
                {
                    excelL.ExlWs.Cells[8, 4] = "Л/счет: " + primech.val_prm;
                }
                else
                {
                    excelL.ExlWs.Cells[8, 4] = "Л/счет: ";
                }
                #endregion


                #endregion


                #region Выборка данных
                ret = Utils.InitReturns();

                Kart finder4 = new Kart();
                finder4.nzp_user = finder.nzp_user;
                finder4.nzp_kvar = finder.nzp_kvar;
                finder4.pref = finder.pref;

                var exR = new ExcelRep();
                List<Charge> lst = exR.GetLicChetData(finder, year, month, out ret);
                #endregion



                #region Первая таблица
                int excelRow = 16;
                excells1 = excelL.ExlWs.get_Range("A" + excelRow, "A" + excelRow);
                excells1.FormulaR1C1 = "Виды услуг";

                excells1 = excelL.ExlWs.get_Range("B" + excelRow, "D" + excelRow);
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = "Описание услуги";

                excells1 = excelL.ExlWs.get_Range("E" + excelRow, "F" + excelRow);
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = "Тариф расчетный";

                excells1 = excelL.ExlWs.get_Range("G" + excelRow, "H" + excelRow);
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = "Тариф муниципальный";

                excells1 = excelL.ExlWs.get_Range("I" + excelRow, "I" + excelRow);
                excells1.FormulaR1C1 = "Тариф 100%";

                excells1 = excelL.ExlWs.get_Range("A" + excelRow, "I" + excelRow);
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                excelRow++;

                if (lst != null & ret.result)
                {
                    if (lst.Count > 0)
                    {
                        for (int i = 0; i < lst.Count; i++)
                        {

                            excells1 = excelL.ExlWs.get_Range("A" + (excelRow), "A" + (excelRow));
                            excells1.FormulaR1C1 = lst[i].service;

                            excells1 = excelL.ExlWs.get_Range("B" + (excelRow), "D" + (excelRow));
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "-";

                            excells1 = excelL.ExlWs.get_Range("E" + (excelRow), "F" + (excelRow));
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = lst[i].tarif;
                            excells1.NumberFormat = "0,00";

                            excells1 = excelL.ExlWs.get_Range("G" + (excelRow), "H" + (excelRow));
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = lst[i].tarif;
                            excells1.NumberFormat = "0,00";
                            excells1 = excelL.ExlWs.get_Range("I" + (excelRow), "I" + (excelRow));
                            excells1.FormulaR1C1 = lst[i].tarif;
                            excells1.NumberFormat = "0,00";
                            //  sum_dolg += Lst[i].sum_pere;
                            excelRow++;
                        }

                    }
                }

                #endregion


                #region Вторая таблица
                excelRow++;
                excells1 = excelL.ExlWs.get_Range("A" + excelRow, "A" + excelRow);
                excells1.FormulaR1C1 = "Виды услуг";
                excells1.EntireColumn.ColumnWidth = 13;

                excells1 = excelL.ExlWs.get_Range("B" + excelRow, "B" + excelRow);
                excells1.FormulaR1C1 = "Тариф";
                excells1.EntireColumn.ColumnWidth = 7.29;

                excells1 = excelL.ExlWs.get_Range("C" + excelRow, "C" + excelRow);
                excells1.FormulaR1C1 = "Начислено б\\учета льгот";
                excells1.WrapText = true;
                excells1.EntireColumn.ColumnWidth = 9.14;

                excells1 = excelL.ExlWs.get_Range("D" + excelRow, "D" + excelRow);
                excells1.FormulaR1C1 = "Количество льготников 100%|50%|30%";
                excells1.WrapText = true;
                excells1.EntireColumn.ColumnWidth = 16.14;

                excells1 = excelL.ExlWs.get_Range("E" + excelRow, "E" + excelRow);
                excells1.FormulaR1C1 = "С/норма";
                excells1.EntireColumn.ColumnWidth = 7;

                excells1 = excelL.ExlWs.get_Range("F" + excelRow, "F" + excelRow);
                excells1.FormulaR1C1 = "Кол-во";
                excells1.EntireColumn.ColumnWidth = 6.29;

                excells1 = excelL.ExlWs.get_Range("G" + excelRow, "G" + excelRow);
                excells1.FormulaR1C1 = "Единицы измерения";
                excells1.WrapText = true;
                excells1.EntireColumn.ColumnWidth = 8.43;

                excells1 = excelL.ExlWs.get_Range("H" + excelRow, "H" + excelRow);
                excells1.FormulaR1C1 = "Льготная  скидка";
                excells1.WrapText = true;
                excells1.EntireColumn.ColumnWidth = 6.14;

                excells1 = excelL.ExlWs.get_Range("I" + excelRow, "I" + excelRow);
                excells1.FormulaR1C1 = "Начислено к оплате";
                excells1.WrapText = true;
                excells1.EntireColumn.ColumnWidth = 8.43;

                excells1 = excelL.ExlWs.get_Range("A" + excelRow, "I" + excelRow);
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                excelRow++;
                decimal sumTarif = 0;
                decimal sumCharge = 0;
                decimal sumPere = 0;
                if (lst != null & ret.result)
                {
                    if (lst.Count > 0)
                    {
                        for (int i = 0; i < lst.Count; i++)
                        {

                            excells1 = excelL.ExlWs.get_Range("A" + excelRow, "A" + excelRow);
                            excells1.FormulaR1C1 = lst[i].service;

                            excells1 = excelL.ExlWs.get_Range("B" + excelRow, "B" + excelRow);
                            excells1.FormulaR1C1 = lst[i].tarif;
                            excells1.NumberFormat = "0,00";

                            excells1 = excelL.ExlWs.get_Range("C" + excelRow, "C" + excelRow);
                            excells1.FormulaR1C1 = lst[i].sum_tarif;
                            excells1.NumberFormat = "0,00";

                            excells1 = excelL.ExlWs.get_Range("E" + excelRow, "E" + excelRow);
                            excells1.FormulaR1C1 = lst[i].c_sn;
                            excells1.NumberFormat = "0,00";

                            excells1 = excelL.ExlWs.get_Range("F" + excelRow, "F" + excelRow);
                            excells1.FormulaR1C1 = lst[i].c_calc;
                            excells1.NumberFormat = "0,00";

                            excells1 = excelL.ExlWs.get_Range("G" + excelRow, "G" + excelRow);
                            excells1.FormulaR1C1 = lst[i].measure;

                            excells1 = excelL.ExlWs.get_Range("I" + excelRow, "I" + excelRow);
                            excells1.FormulaR1C1 = lst[i].sum_charge;
                            excells1.NumberFormat = "0,00";

                            sumTarif += lst[i].sum_tarif;
                            sumCharge += lst[i].sum_charge;
                            sumPere += lst[i].sum_pere;
                            excelRow++;
                        }

                    }
                }

                decimal dl;
                Decimal.TryParse(sumDolg, out dl);
                sumPere = sumPere - dl;

                excells1 = excelL.ExlWs.get_Range("B" + excelRow, "B" + excelRow);
                excells1.FormulaR1C1 = "Всего";
                excells1.Font.Size = 8;
                excells1.Font.Bold = true;

                excells1 = excelL.ExlWs.get_Range("C" + excelRow, "C" + excelRow);
                excells1.FormulaR1C1 = sumTarif;
                excells1.Font.Size = 8;
                excells1.Font.Bold = true;

                excells1 = excelL.ExlWs.get_Range("G" + excelRow, "I" + excelRow);
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = sumCharge;
                excells1.Font.Bold = true;

                excelRow++;
                excells1 = excelL.ExlWs.get_Range("G" + excelRow, "I" + excelRow);
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = "Долг на 01." + month.ToString("00") + "." + year + "г.      " + sumPere.ToString("f2");
                excells1.Font.Bold = true;
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                excelRow = excelRow + 3;


                excells1 = excelL.ExlWs.get_Range("A1", "I" + (excelRow + 1));
                excells1.Font.Size = 8;
                excells1.Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
                excells1.Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
                excells1.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
                excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;

                //excells1 = excelL.ExlWs.get_Range("D" + (excelRow + 3), "F" + (excelRow + 3));
                //excells1.Merge(Type.Missing);
                //excells1.FormulaR1C1 = "________________/_______________";
                #endregion

                #region --заполнение параметров подписи отчета
                /* 579 - Наименование должности бухгалтера
                   1047 - ФИО руководителя ПУС 
                   1048 - Должность руководителя ПУС */
                int fontSize = 10,
                    indexer = excelRow + 3;
                const string fontName = "Calibri";
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
                        : string.Empty;

                //получить пользователя
                var client = new DbAdminClient();
                var users = client.GetUsers(new User { nzpuser = finder.nzp_user, nzp_user = finder.nzp_user });
                string pasportFio = (users != null && users.result && users.returnsData != null && users.returnsData.Count > 0)
                    ? Utils.GetCorrectFIO(users.returnsData[0].uname)
                    : string.Empty;

                excells1 = excelL.ExlWs.Range["A" + indexer, Type.Missing];
                excells1.Merge(Type.Missing);
                excells1.Font.Name = fontName;
                excells1.Font.Size = fontSize;
                excells1.WrapText = true;
                excells1.Value2 = nachalDol;

                excells1 = excelL.ExlWs.Range["B" + indexer, Type.Missing];
                excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;


                excells1 = excelL.ExlWs.Range["C" + indexer, Type.Missing];
                excells1.Font.Name = fontName;
                excells1.Font.Size = fontSize;
                excells1.Value2 = Utils.GetCorrectFIO(nachalFio);

                excells1 = excelL.ExlWs.Range["A" + (indexer + 2), Type.Missing];
                excells1.Merge(Type.Missing);
                excells1.Font.Name = fontName;
                excells1.Font.Size = fontSize;
                excells1.WrapText = true;
                excells1.Value2 = pasportDol;

                excells1 = excelL.ExlWs.Range["B" + (indexer + 2), Type.Missing];
                excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;

                excells1 = excelL.ExlWs.Range["C" + (indexer + 2), Type.Missing];
                excells1.Font.Name = fontName;
                excells1.Font.Size = fontSize;
                excells1.Value2 = pasportFio;
                #endregion

                #endregion


                #region Сохраняем файл
                try
                {

                    fileName = GetFileName(Global.Constants.ExcelDir, "SpravLicSchet_" +
                        finder.nzp_kvar + "_" + year + "_" +
                        month + "_" + finder.nzp_user) + ".xls";
                    ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
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
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return ret;
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

        public Returns GetEnergoActSverki(Prm prm, int nzpSupp, string nzpUser, ref string fileName)
        {
            Returns ret;

            //создание dataTable
            var exR = new ExcelRep();
            DataTable dt = exR.GetEnergoActSverki(prm, out ret, nzpUser);

            ExcelLoader excelL = null;
            try
            {
                excelL = new ExcelLoader();

                if (dt != null)
                {

                    


                    #region Создаем шапку
                    Range excells1;

                    var dic = new Dictionary<string, string>
                    {
                        {"ERCName", ""},
                        {"rajon", ""},
                        {"month", Utils.GetMonthName(prm.month_)},
                        {"year", prm.year_.ToString(CultureInfo.InvariantCulture)}
                    };

                    #endregion


                    excelL.LoadTemlate(PathHelper.GetReportTemplatePath("ActSverki.xls"), dic);


                    #region Пишем тело

                    int excelRow = 12;
                    int i = 0;

                    while (i < dt.Rows.Count)
                    {

                        switch (Convert.ToInt32(dt.Rows[i]["ord2"].ToString()))
                        {
                            case 1:
                                {
                                    excelL.ExlWs.Cells[excelRow, 2] = dt.Rows[i]["area"].ToString().Trim() +
                                        ", всего в том числе:";
                                }
                                break;
                            case 2:
                                {
                                    if (Convert.ToInt32(dt.Rows[i]["typek"].ToString()) == 1) excelL.ExlWs.Cells[excelRow, 2] =
                                        " - УК жилые помещения (население)) \n всего, в том числе:";
                                    else
                                        excelL.ExlWs.Cells[excelRow, 2] =
                                            " - УК нежилые помещения (юр. лица)";
                                }
                                break;
                            case 3: excelL.ExlWs.Cells[excelRow, 2] = dt.Rows[i]["service"].ToString().Trim();
                                break;
                            case 4:
                                {
                                    excelL.ExlWs.Cells[excelRow, 2] = "Всего";
                                    excells1 = excelL.ExlWs.Range["B" + excelRow, "AG" + excelRow];
                                    excells1.Font.Bold = true;
                                }
                                break;

                        }

                        excelL.ExlWs.Cells[excelRow, 3] = "";
                        excelL.ExlWs.Cells[excelRow, 4] = Decimal.Parse(dt.Rows[i]["sum_insaldo"].ToString());
                        excelL.ExlWs.Cells[excelRow, 5] = Decimal.Parse(dt.Rows[i]["sum_tarif"].ToString());
                        excelL.ExlWs.Cells[excelRow, 6] = Decimal.Parse(dt.Rows[i]["gkal_calc"].ToString());
                        excelL.ExlWs.Cells[excelRow, 7] = Decimal.Parse(dt.Rows[i]["c_calc"].ToString());
                        excelL.ExlWs.Cells[excelRow, 8] = Decimal.Parse(dt.Rows[i]["reval"].ToString());
                        excelL.ExlWs.Cells[excelRow, 9] = Decimal.Parse(dt.Rows[i]["gkal_reval"].ToString());
                        excelL.ExlWs.Cells[excelRow, 10] = Decimal.Parse(dt.Rows[i]["sum_money"].ToString());
                        excelL.ExlWs.Cells[excelRow, 11] = Decimal.Parse(dt.Rows[i]["real_charge"].ToString());
                        excelL.ExlWs.Cells[excelRow, 12] = Decimal.Parse(dt.Rows[i]["sum_outs"].ToString());
                        excelL.ExlWs.Cells[excelRow, 13] = Decimal.Parse(dt.Rows[i]["sum_in"].ToString());
                        excelL.ExlWs.Cells[excelRow, 14] = Decimal.Parse(dt.Rows[i]["sum_rasp"].ToString());
                        excelL.ExlWs.Cells[excelRow, 15] = 0;
                        excelL.ExlWs.Cells[excelRow, 16] = Decimal.Parse(dt.Rows[i]["sum_outf"].ToString());

                        excelRow++;
                        i++;
                    }
                    #endregion

                    #region форматируем
                    if (excelRow > 12) excelRow--;



                    excells1 = excelL.ExlWs.get_Range("B12", "AG" + excelRow);
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.Font.Size = 8;
                    excells1.EntireColumn.NumberFormat = "# ##0,00";

                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir, "ActSverki" +
                            prm.nzp_area + "_" +
                            prm.nzp_geu + "_" +
                            prm.year_ + "_" +
                            prm.month_
                            + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
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
                    ret.text = "Создание GetSpravSuppNachHar DataTable : ОШИБКА!";

                    fileName = GetFileName(Global.Constants.ExcelDir, "ActSverki" +
                                                                      prm.year_ + "_" +
                                                                      prm.month_
                                                                      + "_" + nzpUser) + ".xls";
                    ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                    excelL.ExlWb.Close();
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
                        fileName = GetFileName(Global.Constants.ExcelDir, "ActSverki" +
                            prm.year_ + "_" +
                            prm.month_
                            + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет SpravSuppNachHar", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetActSverki Excel File : ОШИБКА!";
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

      

        /// <summary>
        /// Заполнение строки по одному дому
        /// </summary>
        /// <param name="excells1"></param>
        /// <param name="excelRow"></param>
        /// <param name="countDom"></param>
        /// <param name="excelL"></param>
        /// <param name="adres"></param>
        /// <param name="exform"></param>
        /// <param name="nachData"></param>
        /// <param name="gilMas"></param>
        /// <param name="kubMas"></param>
        /// <param name="gkalMas"></param>
        public void FillOneRowSpravSoderg9(Range excells1,
            int excelRow, int countDom, ExcelLoader excelL, List<string> adres,
            ExcelFormater exform, decimal[,] nachData, List<decimal> gilMas,
            List<decimal> kubMas, List<decimal> gkalMas)
        {
            if (excells1 == null) throw new ArgumentNullException("excells1");

            const int rowCount = 3;

            if (adres[0] == "Итого")
            {
                excells1 = excelL.ExlWs.get_Range("A" + excelRow, "E" + (excelRow + rowCount - 1));
                excells1.Merge(Type.Missing);
                excells1.Font.Size = 6;
                excells1.FormulaR1C1 = "Итого";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;


            }
            else
            {

                excells1 = excelL.ExlWs.get_Range("A" + excelRow, "A" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = countDom;


                excells1 = excelL.ExlWs.get_Range("B" + excelRow, "B" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[0];


                excells1 = excelL.ExlWs.get_Range("C" + excelRow, "C" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[1];

                excells1 = excelL.ExlWs.get_Range("D" + excelRow, "D" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[2];

                excells1 = excelL.ExlWs.get_Range("E" + excelRow, "E" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[3];
            }

            excells1 = excelL.ExlWs.get_Range("F" + excelRow, "F" + excelRow);
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "жильцов,чел";

            excells1 = excelL.ExlWs.get_Range("F" + (excelRow + 1), "F" + (excelRow + 1));
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Объем л/с, м3";

            excells1 = excelL.ExlWs.get_Range("F" + (excelRow + 2), "F" + (excelRow + 2));
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Объем л/с, Гкал";



            int maxExcelCol = 7;
            //Присваиваем данные по формулам
            for (int j = 0; j < gilMas.Count; j++)
            {
                if (gilMas[j] > 0m)
                {
                    excelL.ExlWs.Cells[excelRow, 7 + j] = gilMas[j];
                    excelL.ExlWs.Cells[excelRow + 1, 7 + j] = kubMas[j];
                    excells1 = excelL.ExlWs.get_Range(exform.BukvaList[7 + j - 1]
                        + (excelRow), exform.BukvaList[7 + j - 1] + (excelRow));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "0";
                }
                if (kubMas[j] > 0m)
                {
                    excelL.ExlWs.Cells[excelRow + 1, 7 + j] = kubMas[j];
                    excells1 = excelL.ExlWs.get_Range(exform.BukvaList[7 + j - 1]
                        + (excelRow + 1), exform.BukvaList[7 + j - 1] + (excelRow + 1));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";

                }

                if (gkalMas[j] > 0m)
                {
                    excelL.ExlWs.Cells[excelRow + 2, 7 + j] = gkalMas[j];

                    excells1 = excelL.ExlWs.get_Range(exform.BukvaList[7 + j - 1]
                        + (excelRow + 2), exform.BukvaList[7 + j - 1] + (excelRow + 2));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";


                }

                maxExcelCol++;
            }



            //Присваиваем данные по начислениям
            for (int j = 0; j < nachData.GetLength(1); j++)
            {
                for (int i = 0; i < 3; i++)
                {

                    if (nachData[i, j] != 0m)
                    {
                        excelL.ExlWs.Cells[excelRow + i, maxExcelCol + j] = nachData[i, j];
                    }
                }
                if ((j == 0) || (j == 1) || (j > 4))
                {
                    excells1 = excelL.ExlWs.get_Range(exform.BukvaList[maxExcelCol + j - 1] +
                    excelRow, exform.BukvaList[maxExcelCol + j - 1] + (excelRow + 2));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Merge(Type.Missing);
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

                }

            }




        }

        /// <summary>
        /// Справка по услуге ГВС 
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="nzpUser"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Returns GetSpravSoderg9(Prm prm, string nzpUser, ref string fileName)
        {
            
            return GetSpravSodergOdn9(prm, nzpUser, ref fileName);
        }


        /// <summary>
        /// Заполнение строки по одному дому
        /// </summary>
        /// <param name="excells1"></param>
        /// <param name="excelRow"></param>
        /// <param name="countDom"></param>
        /// <param name="excelL"></param>
        /// <param name="adres"></param>
        /// <param name="exform"></param>
        /// <param name="nachData"></param>
        /// <param name="gilMas"></param>
        /// <param name="kubMas"></param>
        public void FillOneRowSpravSoderg7(Range excells1,
            int excelRow, int countDom, ExcelLoader excelL, List<string> adres,
            ExcelFormater exform, decimal[,] nachData, List<decimal> gilMas,
            List<decimal> kubMas)
        {
            if (excells1 == null) throw new ArgumentNullException("excells1");

            int rowCount = 2;

            if (adres[0] == "Итого")
            {
                excells1 = excelL.ExlWs.get_Range("A" + excelRow,
                "E" + (excelRow + rowCount - 1));
                excells1.Merge(Type.Missing);
                excells1.Font.Size = 6;
                excells1.FormulaR1C1 = "Итого";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;


            }
            else
            {

                excells1 = excelL.ExlWs.get_Range("A" + excelRow, "A" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = countDom;


                excells1 = excelL.ExlWs.get_Range("B" + excelRow, "B" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[0];


                excells1 = excelL.ExlWs.get_Range("C" + excelRow, "C" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[1];

                excells1 = excelL.ExlWs.get_Range("D" + excelRow, "D" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[2];

                excells1 = excelL.ExlWs.get_Range("E" + excelRow, "E" + (excelRow + rowCount - 1));
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[3];
            }

            excells1 = excelL.ExlWs.get_Range("F" + excelRow, "F" + excelRow);
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "жильцов,чел";

            excells1 = excelL.ExlWs.get_Range("F" + (excelRow + 1), "F" + (excelRow + 1));
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Объем л/с, м3";



            int maxExcelCol = 7;
            //Присваиваем данные по формулам
            for (int j = 0; j < gilMas.Count; j++)
            {
                if (gilMas[j] > 0m)
                {
                    excelL.ExlWs.Cells[excelRow, 7 + j] = gilMas[j];
                }
                if (kubMas[j] > 0m)
                {
                    excelL.ExlWs.Cells[excelRow + 1, 7 + j] = kubMas[j];
                }


                maxExcelCol++;
            }



            //Присваиваем данные по начислениям
            for (int j = 0; j < nachData.GetLength(1); j++)
            {
                for (int i = 0; i < 2; i++)
                {

                    if (nachData[i, j] > 0m)
                    {
                        excelL.ExlWs.Cells[excelRow + i, maxExcelCol + j] = nachData[i, j];
                    }
                }
                if ((j == 0) || (j == 1) || (j > 4))
                {
                    excells1 = excelL.ExlWs.get_Range(exform.BukvaList[maxExcelCol + j - 1] +
                    excelRow, exform.BukvaList[maxExcelCol + j - 1] + (excelRow + 1));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Merge(Type.Missing);
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

                }

            }




        }

        /// <summary>
        /// Справка по услуге Водоотведение 
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="nzpUser"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Returns GetFakturaFiles(Prm prm, string nzpUser, ref string fileName)
        {
            //Returns ret = Utils.InitReturns();

            //ExcelRep ExR = new ExcelRep();
            //List<string> lisRes = ExR.GetFakturaFiles(prm_, out ret, Nzp_user);

            //SevenZipCompressor file = new SevenZipCompressor();

            //string file_n = (DateTime.Now.ToShortDateString() + "_" + DateTime.Now.ToShortTimeString()).Replace(":","_").Replace(".","_");

            //string[] fileArch = new string[lisRes.Count];
            //for (int i = 0; i<lisRes.Count; i++)
            //{
            //    fileArch[i] = lisRes[i];
            //}
            //if (lisRes.Count != 0)
            //{
            //    file.CompressFiles(STCLINE.KP50.Global.Constants.ExcelDir + @"\" + prm_.year_.ToString() + prm_.month_.ToString() + "_" + file_n + ".7z",
            //        fileArch);
            //}

            //for (int i = 0; i < lisRes.Count; i++)
            //{
            //    try
            //    {
            //        System.IO.File.Delete(lisRes[i]);
            //    }
            //    catch
            //    {
            //    }

            //}


            //fileName = prm_.year_.ToString() + prm_.month_.ToString() + "_" + file_n + ".7z";

            var exR = new ExcelRep();
            Returns ret;
            List<string> lisRes = exR.GetFakturaFiles(prm, out ret, nzpUser);
            if (lisRes != null && lisRes.Count > 0)
            {
                fileName = lisRes[0];

            }
            return ret;

        }


        

        //Отчет: справка по поставщикам коммунальных услуг
        public void GetSpravSuppNach(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по поставщикам коммунальных услуг форма №2\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка по поставщикам коммунальных услуг форма №2\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravSuppNach(cont.listprm, cont.nzp_user.ToString(CultureInfo.InvariantCulture), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: справка по поставщикам коммунальных услуг
        public void GetSpravSuppNachHar(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret;
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по поставщикам коммунальных услуг форма №1\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка по поставщикам коммунальных услуг форма №1\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            if (cont.listprm.Count > 0)
            {
                ret = GetSpravSuppNachHar(cont.listprm[0], cont.nzp_user.ToString(CultureInfo.InvariantCulture), ref fileName);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка нет передачи параметра в отчет Справка по поставщикам ЖКУ с харастеристиками",
                    MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);

            }
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        
        //Отчет: Справка по отключениям подачи коммунальных услуг
        public void GetSpravPoOtklUslug(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по отключениям подачи коммунальных услуг\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка по отключениям подачи коммунальных услуг\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravPoOtklUslug(cont.nzp_user, cont.nzp_serv, Convert.ToInt32(cont.mm), Convert.ToInt32(cont.yy), ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);

        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по домам с указанием виновника
        public void GetSpravPoOtklUslugDom(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по отключениям подачи коммунальных услуг по домам\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"правка по отключениям подачи коммунальных услуг по домам\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravPoOtklUslugDom(cont.listprm[0], cont.nzp_user, ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }

            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по домам с указанием виновника
        public void GetSpravPoOtklUslugDomVinovnik(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по отключениям подачи коммунальных услуг по домам\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка по отключениям подачи коммунальных услуг по домам\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravPoOtklUslugDomVinovnik(cont.listprm[0], cont.nzp_user, ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);

        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по ЖЭУ с указанием виновника
        public void GetSpravPoOtklUslugGeuVinovnik(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по отключениям подачи коммунальных услуг сводная по ЖЭУ\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка по отключениям подачи коммунальных услуг сводная по ЖЭУ\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravPoOtklUslugGeuVinovnik(cont.listprm[0], cont.nzp_user, ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }

            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }


        

        //Отчет: справка по поставщикам коммунальных услуг
        public void GetSpravPULs(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка о начислениях по квартирным приборам учета\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка о начислениях по квартирным приборам учета\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravPULs(cont.listprm[0], cont.nzp_user.ToString(CultureInfo.InvariantCulture), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Справка о начислении платы по виду услуги содержание жилья
        public void GetSpravSoderg(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка о начислении платы по видам услуг\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Справка о начислении платы по видам услуг\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravSoderg(cont.listprm[0], cont.nzp_user.ToString(CultureInfo.InvariantCulture), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Справка о начислении платы по виду услуги содержание жилья
        public void GetSpravSoderg2Heat(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret;
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка о начислении платы по видам услуг форма 2\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Справка о начислении платы по видам услуг форма 2\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
             excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = GetSpravSoderg2Heat(cont.listprm[0], cont.nzp_user.ToString(CultureInfo.InvariantCulture), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Справка о начислении платы по виду услуги содержание жилья
        public void GetSpravSoderg2Water(object container)
        {
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка о начислении платы по видам услуг форма 2\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Справка о начислении платы по видам услуг форма 2\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravSoderg2Water(cont.listprm[0], cont.nzp_user.ToString(CultureInfo.InvariantCulture), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
        }

        //Отчет: Справка о начислении платы по виду услуги содержание жилья
        public void GetSpravGroupSodergGil(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по услугам группы \"содержание жилья\"\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Справка по услугам группы \"содержание жилья\"\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            Returns ret = GetSpravGroupSodergGil(cont.listprm[0], cont.nzp_user.ToString(CultureInfo.InvariantCulture), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }


      
        public Returns GetSpravSuppNachHar2(Prm prm, string nzpUser, ref string fileName)
        {
            Returns ret;

            //создание dataTable
            var exR = new ExcelRep();
            DataTable dt = exR.GetSpravSuppNachHar2(prm, out ret, nzpUser);

            ExcelLoader excelL = null;
            try
            {
                excelL = new ExcelLoader();
                excelL.ExlWs.Rows.Font.Name = "Arial";
                // ExcelL.ExlWs.PageSetup.Zoom = "100%";

                if (dt != null)
                {
                    DataRow[] drSelect = dt.Select("tip = 1");

                    if (drSelect.Any())
                    {
                        DataTable dtSelect = drSelect.CopyToDataTable();
                        if (dtSelect.Rows.Count > 0)
                            MakeListSpravSuppNachHar2(dtSelect, excelL, 1, prm, nzpUser);
                        dtSelect.Clear();
                        drSelect = dt.Select("tip = 2");

                        string oldGeu = "";
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
                                    MakeListSpravSuppNachHar2(dtSelect, excelL, j, prm, nzpUser);
                                    dtSelect.Clear();
                                    j++;
                                }
                            }
                        }

                    }
                    excelL.SortWs();
                    ((Worksheet)excelL.ExlWb.Sheets[1]).Select(Type.Missing);



                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNachHar2" +
                            prm.year_ + "_" +
                            prm.month_
                            + "_" + DateTime.Now.GetHashCode() + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
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
                    ret.text = "Создание GetSpravSuppNachHar2 DataTable : ОШИБКА!";

                    fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNach2" +
                                                                      prm.nzp_area + "_" +
                                                                      prm.nzp_geu + "_" +
                                                                      prm.year_ + "_" +
                                                                      prm.month_
                                                                      + "_" + nzpUser) + ".xls";
                    ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                    excelL.ExlWb.Close();
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
                        fileName = GetFileName(Global.Constants.ExcelDir, "SpravSuppNachHar2" +
                            prm.nzp_area + "_" +
                            prm.nzp_geu + "_" +
                            prm.year_ + "_" +
                            prm.month_
                            + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет SpravSuppNachHar2", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSuppNach2 Excel File : ОШИБКА!";
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

        //Отчет: справка по поставщикам форма 3
        public void GetSpravSuppNachHar2(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret;
            var cont = (ParamContainer)container;
            var excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по поставщикам форма №3\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
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
            excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка по поставщикам форма №3\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            if (cont.listprm.Count > 0)
            {
                ret = GetSpravSuppNachHar2(cont.listprm[0], cont.nzp_user.ToString(CultureInfo.InvariantCulture), ref fileName);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка нет передачи параметра в отчет Справка по поставщикам ЖКУ с харастеристиками",
                    MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);

            }
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

    }
}
