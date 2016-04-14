using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Bars.KP50.DataImport.SOURCE.LOADER;
using Bars.KP50.Gubkin;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;
using Bars.KP50.Utils;


namespace STCLINE.KP50.REPORT
{
    //Класс генерирующий отчеты
    //содержит процедуры для генерации конкретных отчетов
    public partial class ReportGen
    {
        //Отчет: Сверка поступлений за день/период
        public void GetSverkaDay(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret;
            ParamContainer cont = (ParamContainer)container;

            ExcelRepClient erc = new ExcelRepClient();
            string repName = "Сверка поступлений за " + cont.listsverkaperiod[0].dat_s + (cont.listsverkaperiod[0].dat_po != "" ? " - " + cont.listsverkaperiod[0].dat_po : "");
            ret = erc.AddMyFile(new ExcelUtility()
            {
                nzp_user = cont.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = repName
            });
            int nzpExc = 0;
            if (ret.result) nzpExc = ret.tag;

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                ret = erc.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Failed,
                });
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + ". Ошибка формирования отчета " + repName + ".\nОтсутствие установленной версии Excel", MonitorLog.typelog.Error, true);
                return;
            }
            #endregion

            //Имя файла отчета
            string fileName = "";

            ExcelRep excelRepDb = new ExcelRep();

            //Старт формирования
            ret = GetSverkaDay(cont.listsverkaperiod[0], cont.nzp_user.ToString(), ref fileName);

            if (ret.result)
            {
                ret = erc.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Success,
                    exc_path = fileName
                });
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + ". Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
            }
            else
            {
                ret = erc.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Failed
                });
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + ". Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Сверка поступлений за месяц
        public void GetSverkaMonth(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Сверка поступлений за месяц\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Сверка поступлений за месяц\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSverkaMonth(cont.listsverkaperiod[0], cont.nzp_user.ToString(), ref fileName);
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

        public void GetDataSaldoPoPerechisl(object container)
        {
            Returns ret;
            ParamContainer cont = (ParamContainer)container;

            ExcelRepClient erc = new ExcelRepClient();
            string repName = "Сальдо по перечислениям за " + cont.MoneyDistrib.dat_oper + (cont.MoneyDistrib.dat_oper_po != "" ? " - " + cont.MoneyDistrib.dat_oper_po : "");
            ret = erc.AddMyFile(new ExcelUtility()
            {
                nzp_user = cont.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = repName
            });
            int nzpExc = 0;
            if (ret.result) nzpExc = ret.tag;

            #region Проверка на наличие Excel
            /* if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                ret = erc.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Failed,
                });
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + ". Ошибка формирования отчета " + repName + ".\nОтсутствие установленной версии Excel", MonitorLog.typelog.Error, true);
                return;
            }*/
            #endregion

            //Имя файла отчета
            string fileName = "";

            ExcelRep excelRepDb = new ExcelRep();

            //Старт формирования
            ret = GetDataSaldoPoPerechisl(cont.MoneyDistrib, cont.nzp_user.ToString(), ref fileName);

            if (ret.result)
            {
                ret = erc.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Success,
                    exc_path = fileName
                });
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + ". Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
            }
            else
            {
                ret = erc.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Failed
                });
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + ". Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
            }
        }

        public Returns GetSverkaDay(ExcelSverkaPeriod prm_, string Nzp_user, ref string fileName)
        {
            Returns ret;

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSverkaDay(prm_, out ret, Nzp_user);
            ExR.Close();

            if (!ret.result) return ret;

            ExcelLoader ExcelL = null;

            try
            {
                if (DT != null)
                {
                    ExcelL = new ExcelLoader();

                    //ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    //спустили шапку
                    excells1 = ExcelL.ExlWs.get_Range("A1", "J1");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сверка поступлений из банка";
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A2", "J2");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;

                    string pr = "";
                    if (prm_.search_date == 1) pr = " (опер. день) ";
                    string s = " за " + prm_.dat_s;
                    if (prm_.dat_po != "") s += " - " + prm_.dat_po;
                    string s_name = SetReportHeader();
                    //excells1.FormulaR1C1 = s+pr+" МП городского округа Самара \"ЕИРЦ\"";
                    excells1.FormulaR1C1 = s + pr + s_name;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A3", "A3");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.ColumnWidth = 3;

                    string col = "";
                    excells1 = ExcelL.ExlWs.get_Range("A5", "A7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Место формирования";//"Номер пачки";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.5;

                    //prm_.dat_po == "" - отчет за день
                    if (prm_.dat_po == "") col = "B"; else col = "D";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Предприятие";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;

                    if (prm_.dat_po == "") col = "D"; else col = "B";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Р/счет";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 4;

                    if (prm_.dat_po != "")
                    {
                        excells1 = ExcelL.ExlWs.get_Range("C5", "C7");
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "Дата пачки";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excells1.ColumnWidth = 6;
                    }

                    if (prm_.dat_po == "") col = "E"; else col = "F";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Количество";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 3;

                    if (prm_.dat_po == "") col = "C"; else col = "E";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Территория";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 10;

                    if (prm_.dat_po == "") col = "F"; else col = "G";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Оплачено всего";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 8;

                    if (prm_.dat_po == "") col = "G"; else col = "H";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = " Оплачено (в т.ч. юр. лицами)";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 7;

                    if (prm_.dat_po == "") col = "H"; else col = "I";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Пеня";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 6;

                    if (prm_.dat_po == "") col = "I"; else col = "J";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Комис. сбор";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 6;

                    if (prm_.dat_po == "") col = "J"; else col = "K";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Всего";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 8;
                    #endregion

                    if (DT.Rows.Count > 0)
                    {
                        string rw;
                        if (prm_.dat_po == "") rw = "J";
                        else rw = "K";


                        #region Пишем тело

                        int ExcelRow = 8;
                        int i = 0;

                        int icount_kvit = 0, count_kvit = 0, scount_kvit = 0, acount_kvit = 0;
                        decimal ig_sum_ls = 0, g_sum_ls = 0, sg_sum_ls = 0, ag_sum_ls = 0;
                        decimal isum_ur = 0, sum_ur = 0, ssum_ur = 0, asum_ur = 0;
                        decimal ipenya = 0, penya = 0, spenya = 0, apenya = 0;
                        decimal ikomiss = 0, komiss = 0, skomiss = 0, akomiss = 0;
                        decimal itot = 0, tot = 0, stot = 0, atot = 0;

                        string old_geu = "", old_payer = "", old_p = "", old_a = "", old_g = "", old_area = "";
                        string old_prefix_ls = "";
                        bool key = false;
                        List<ExcelSverka> list = new List<ExcelSverka>();
                        ExcelSverka sv;

                        while (i < DT.Rows.Count)
                        {
                            if (list.Count == 0)
                            {
                                sv = new ExcelSverka();
                                sv.area = DT.Rows[i]["area"].ToString().Trim();
                                sv.nzp_area = Int32.Parse(DT.Rows[i]["nzp_area"].ToString());
                                list.Add(sv);
                            }


                            if (prm_.dat_po == "")//первый отчет
                            {
                                if (old_geu == "") old_geu = DT.Rows[i]["geu"].ToString().Trim();
                                if (old_area == "") old_area = DT.Rows[i]["area"].ToString().Trim();
                                if (old_payer == "") old_payer = DT.Rows[i]["payer"].ToString().Trim();



                                //Пишем Итого по ЖЭУ
                                #region Итого по ЖЭУ
                                if (old_geu != DT.Rows[i]["geu"].ToString().Trim())
                                {
                                    excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 4]);
                                    excells1.Merge(Type.Missing);
                                    excells1.FormulaR1C1 = "Итого по ЖЭУ (" + old_geu + ")";
                                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                                    ExcelL.ExlWs.Cells[ExcelRow, 5] = count_kvit;
                                    ExcelL.ExlWs.Cells[ExcelRow, 6] = g_sum_ls;
                                    ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_ur;
                                    ExcelL.ExlWs.Cells[ExcelRow, 8] = penya;
                                    ExcelL.ExlWs.Cells[ExcelRow, 9] = komiss;
                                    ExcelL.ExlWs.Cells[ExcelRow, 10] = tot;
                                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                                    excells1.Font.Bold = true;
                                    ExcelRow++;

                                    old_geu = DT.Rows[i]["geu"].ToString().Trim();

                                    count_kvit = 0;
                                    g_sum_ls = 0;
                                    sum_ur = 0;
                                    penya = 0;
                                    komiss = 0;
                                    tot = 0;
                                }
                                #endregion

                                #region Итого по Управляющая организация
                                if (old_area != DT.Rows[i]["area"].ToString().Trim() || old_payer != DT.Rows[i]["payer"].ToString().Trim())
                                {
                                    excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 4]);
                                    excells1.Merge(Type.Missing);
                                    excells1.FormulaR1C1 = "Итого по " + old_area;
                                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                                    ExcelL.ExlWs.Cells[ExcelRow, 5] = acount_kvit;
                                    ExcelL.ExlWs.Cells[ExcelRow, 6] = ag_sum_ls;
                                    ExcelL.ExlWs.Cells[ExcelRow, 7] = asum_ur;
                                    ExcelL.ExlWs.Cells[ExcelRow, 8] = apenya;
                                    ExcelL.ExlWs.Cells[ExcelRow, 9] = akomiss;
                                    ExcelL.ExlWs.Cells[ExcelRow, 10] = atot;
                                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                                    excells1.Font.Bold = true;
                                    ExcelRow++;

                                    old_area = DT.Rows[i]["area"].ToString().Trim();

                                    key = true;
                                    for (int cnt = 0; cnt < list.Count; cnt++)
                                        if (list[cnt].nzp_area == Int32.Parse(DT.Rows[i]["nzp_area"].ToString()))
                                        {
                                            key = false;
                                            break;
                                        }

                                    if (key)
                                    {
                                        sv = new ExcelSverka();
                                        sv.area = DT.Rows[i]["area"].ToString().Trim();
                                        sv.nzp_area = Int32.Parse(DT.Rows[i]["nzp_area"].ToString().Trim());
                                        list.Add(sv);
                                    }
                                    acount_kvit = 0;
                                    ag_sum_ls = 0;
                                    asum_ur = 0;
                                    apenya = 0;
                                    akomiss = 0;
                                    atot = 0;
                                }
                                #endregion

                                #region Итого по пачке
                                if (old_payer != DT.Rows[i]["payer"].ToString().Trim())
                                {
                                    excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 4]);
                                    excells1.Merge(Type.Missing);
                                    excells1.FormulaR1C1 = "Итого по пачке (" + old_payer + ")";
                                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                    ExcelL.ExlWs.Cells[ExcelRow, 5] = scount_kvit;
                                    ExcelL.ExlWs.Cells[ExcelRow, 6] = sg_sum_ls;
                                    ExcelL.ExlWs.Cells[ExcelRow, 7] = ssum_ur;
                                    ExcelL.ExlWs.Cells[ExcelRow, 8] = spenya;
                                    ExcelL.ExlWs.Cells[ExcelRow, 9] = skomiss;
                                    ExcelL.ExlWs.Cells[ExcelRow, 10] = stot;
                                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                                    excells1.Font.Bold = true;

                                    ExcelRow++;

                                    old_payer = DT.Rows[i]["payer"].ToString().Trim();

                                    scount_kvit = 0;
                                    sg_sum_ls = 0;
                                    ssum_ur = 0;
                                    spenya = 0;
                                    skomiss = 0;
                                    stot = 0;
                                }
                                #endregion

                                count_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                                g_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                                sum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                                penya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                                komiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                                tot += Decimal.Parse(DT.Rows[i]["tot"].ToString());

                                scount_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                                sg_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                                ssum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                                spenya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                                skomiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                                stot += Decimal.Parse(DT.Rows[i]["tot"].ToString());

                                acount_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                                ag_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                                asum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                                apenya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                                akomiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                                atot += Decimal.Parse(DT.Rows[i]["tot"].ToString());
                            }
                            else //второй отчет
                            {
                                if (old_prefix_ls == "") old_prefix_ls = DT.Rows[i]["prefix_ls"].ToString().Trim();
                                if (old_area == "") old_area = DT.Rows[i]["area"].ToString().Trim();

                                #region Итого по Управляющая организация
                                if (old_area != DT.Rows[i]["area"].ToString().Trim())
                                {
                                    excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 5]);
                                    excells1.Merge(Type.Missing);
                                    excells1.FormulaR1C1 = "Итого по " + old_area;
                                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                                    ExcelL.ExlWs.Cells[ExcelRow, 6] = acount_kvit;
                                    ExcelL.ExlWs.Cells[ExcelRow, 7] = ag_sum_ls;
                                    ExcelL.ExlWs.Cells[ExcelRow, 8] = asum_ur;
                                    ExcelL.ExlWs.Cells[ExcelRow, 9] = apenya;
                                    ExcelL.ExlWs.Cells[ExcelRow, 10] = akomiss;
                                    ExcelL.ExlWs.Cells[ExcelRow, 11] = atot;
                                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                                    excells1.Font.Bold = true;
                                    ExcelRow++;

                                    old_area = DT.Rows[i]["area"].ToString().Trim();

                                    key = true;
                                    for (int cnt = 0; cnt < list.Count; cnt++)
                                        if (list[cnt].nzp_area == Int32.Parse(DT.Rows[i]["nzp_area"].ToString()))
                                        {
                                            key = false;
                                            break;
                                        }

                                    if (key)
                                    {
                                        sv = new ExcelSverka();
                                        sv.area = DT.Rows[i]["area"].ToString().Trim();
                                        sv.nzp_area = Int32.Parse(DT.Rows[i]["nzp_area"].ToString().Trim());
                                        list.Add(sv);
                                    }

                                    acount_kvit = 0;
                                    ag_sum_ls = 0;
                                    asum_ur = 0;
                                    apenya = 0;
                                    akomiss = 0;
                                    atot = 0;
                                }
                                #endregion

                                //Пишем Итого по р/счету
                                #region Итого по р/счету
                                if (old_prefix_ls != DT.Rows[i]["prefix_ls"].ToString().Trim())
                                {
                                    excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 5]);
                                    excells1.Merge(Type.Missing);
                                    excells1.FormulaR1C1 = "Итого (" + old_prefix_ls + ")";
                                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                                    ExcelL.ExlWs.Cells[ExcelRow, 6] = count_kvit;
                                    ExcelL.ExlWs.Cells[ExcelRow, 7] = g_sum_ls;
                                    ExcelL.ExlWs.Cells[ExcelRow, 8] = sum_ur;
                                    ExcelL.ExlWs.Cells[ExcelRow, 9] = penya;
                                    ExcelL.ExlWs.Cells[ExcelRow, 10] = komiss;
                                    ExcelL.ExlWs.Cells[ExcelRow, 11] = tot;
                                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                                    excells1.Font.Bold = true;
                                    ExcelRow++;

                                    old_prefix_ls = DT.Rows[i]["prefix_ls"].ToString().Trim();

                                    count_kvit = 0;
                                    g_sum_ls = 0;
                                    sum_ur = 0;
                                    penya = 0;
                                    komiss = 0;
                                    tot = 0;
                                }
                                #endregion

                                count_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                                g_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                                sum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                                penya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                                komiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                                tot += Decimal.Parse(DT.Rows[i]["tot"].ToString());

                                acount_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                                ag_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                                asum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                                apenya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                                akomiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                                atot += Decimal.Parse(DT.Rows[i]["tot"].ToString());
                            }

                            for (int cnt = 0; cnt < list.Count; cnt++)
                            {
                                if (list[cnt].nzp_area == Int32.Parse(DT.Rows[i]["nzp_area"].ToString()))
                                {
                                    list[cnt].count_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                                    list[cnt].g_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                                    list[cnt].sum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                                    list[cnt].penya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                                    list[cnt].komiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                                    list[cnt].tot += Decimal.Parse(DT.Rows[i]["tot"].ToString());
                                    break;
                                }
                            }
                            #region строки
                            //место формирования
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                            //if (old_p != DT.Rows[i]["payer"].ToString().Trim()) excells1.FormulaR1C1 = DT.Rows[i]["payer"].ToString().Trim();
                            //else excells1.FormulaR1C1 = "";
                            excells1.FormulaR1C1 = DT.Rows[i]["payer"].ToString().Trim();

                            if (prm_.dat_po != "") col = "B"; else col = "D";
                            //Р/счет
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            //   excells1.FormulaR1C1 = Int32.Parse(DT.Rows[i]["prefix_ls"].ToString());
                            if (DT.Rows[i]["prefix_ls"].ToString() == "" || DT.Rows[i]["prefix_ls"].ToString() == "0")
                                excells1.FormulaR1C1 = "Р/с не определен";
                            else excells1.FormulaR1C1 = DT.Rows[i]["prefix_ls"].ToString().Trim();

                            if (prm_.dat_po != "")
                            {
                                //дата пачки
                                excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                                string d = DT.Rows[i]["dat_pack"].ToString().Trim();
                                DateTime dt;
                                DateTime.TryParse(d, out dt);
                                excells1.FormulaR1C1 = dt.ToShortDateString();
                            }

                            if (prm_.dat_po != "") col = "D"; else col = "B";
                            //Предприятие
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            //if (old_a != DT.Rows[i]["area"].ToString().Trim() || old_p != DT.Rows[i]["payer"].ToString().Trim())
                            //    excells1.FormulaR1C1 = DT.Rows[i]["area"].ToString().Trim();
                            //else excells1.FormulaR1C1 = "";
                            excells1.FormulaR1C1 = DT.Rows[i]["area"].ToString().Trim();

                            if (prm_.dat_po != "") col = "E"; else col = "C";
                            //ЖЭУ
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            //if (old_g != DT.Rows[i]["geu"].ToString().Trim())
                            //    excells1.FormulaR1C1 = DT.Rows[i]["geu"].ToString().Trim();
                            //else excells1.FormulaR1C1 = "";
                            excells1.FormulaR1C1 = DT.Rows[i]["geu"].ToString().Trim();


                            if (prm_.dat_po != "") col = "F"; else col = "E";
                            //Количество
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                            icount_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());

                            if (prm_.dat_po != "") col = "G"; else col = "F";
                            //оплачено всего
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                            ig_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());

                            if (prm_.dat_po != "") col = "H"; else col = "G";
                            //Оплачено (всего в т.ч. юридическими лицами)
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                            isum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());

                            if (prm_.dat_po != "") col = "I"; else col = "H";
                            //пеня
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["penya"].ToString());
                            ipenya += Decimal.Parse(DT.Rows[i]["penya"].ToString());

                            if (prm_.dat_po != "") col = "J"; else col = "I";
                            //комис. сбор
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                            ikomiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());

                            if (prm_.dat_po != "") col = "K"; else col = "J";
                            //всего
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["tot"].ToString());
                            itot += Decimal.Parse(DT.Rows[i]["tot"].ToString());

                            ExcelRow++;
                            old_p = DT.Rows[i]["payer"].ToString().Trim();
                            old_a = DT.Rows[i]["area"].ToString().Trim();
                            old_g = DT.Rows[i]["geu"].ToString().Trim();
                            i++;
                            #endregion
                        }
                        #endregion

                        if (prm_.dat_po == "")
                        {
                            #region Итого
                            if (ExcelRow > 8)
                            {


                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 4]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого по ЖЭУ (" + old_geu + ")";
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                ExcelL.ExlWs.Cells[ExcelRow, 5] = count_kvit;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = g_sum_ls;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_ur;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = penya;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = komiss;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = tot;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;

                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 4]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого по " + old_area;
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                ExcelL.ExlWs.Cells[ExcelRow, 5] = acount_kvit;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = ag_sum_ls;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = asum_ur;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = apenya;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = akomiss;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = atot;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;

                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 4]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого по пачке (" + old_payer + ")";
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                ExcelL.ExlWs.Cells[ExcelRow, 5] = scount_kvit;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = sg_sum_ls;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = ssum_ur;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = spenya;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = skomiss;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = stot;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;

                                /*ExcelL.ExlWs.Cells[ExcelRow, 1] = "Итого по пачке ("+old_payer+")";
                                ExcelL.ExlWs.Cells[ExcelRow, 2] = "";
                                ExcelL.ExlWs.Cells[ExcelRow, 3] = "";*/


                            }
                            #endregion
                        }
                        else
                        {
                            #region Итого
                            if (ExcelRow > 8)
                            {
                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 5]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого по " + old_area;
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = acount_kvit;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = ag_sum_ls;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = asum_ur;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = apenya;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = akomiss;
                                ExcelL.ExlWs.Cells[ExcelRow, 11] = atot;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;

                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 5]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого (" + old_prefix_ls + ")";
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = count_kvit;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = g_sum_ls;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = sum_ur;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = penya;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = komiss;
                                ExcelL.ExlWs.Cells[ExcelRow, 11] = tot;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                                excells1.Font.Bold = true;
                                ExcelRow++;
                            }
                            #endregion
                        }

                        #region Пишем подвал
                        if (ExcelRow > 8)
                        {
                            int cl = 1;
                            for (int cnt = 0; cnt < list.Count; cnt++)
                            {
                                if (prm_.dat_po == "") cl = 4;
                                else cl = 5;
                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, cl]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого " + list[cnt].area;
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                cl++;
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = list[cnt].count_kvit; cl++;
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = list[cnt].g_sum_ls; cl++;
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = list[cnt].sum_ur; cl++;
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = list[cnt].penya; cl++;
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = list[cnt].komiss; cl++;
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = list[cnt].tot; cl++;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), rw + ExcelRow.ToString());
                                excells1.Font.Bold = true;
                                ExcelRow++;
                            }

                            cl = 1;
                            if (prm_.dat_po != "")
                            {
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = ""; cl++;
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = ""; cl++;
                            }
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = ""; cl++;
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = ""; cl++;
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = "Итого "; cl++;
                            if (prm_.dat_po == "")
                            {
                                ExcelL.ExlWs.Cells[ExcelRow, cl] = ""; cl++;
                            }
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = icount_kvit; cl++;
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = ig_sum_ls; cl++;
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = isum_ur; cl++;
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = ipenya; cl++;
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = ikomiss; cl++;
                            ExcelL.ExlWs.Cells[ExcelRow, cl] = itot;

                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), rw + ExcelRow.ToString());
                            excells1.Font.Bold = true;
                        }

                        excells1 = ExcelL.ExlWs.get_Range("A5", rw + ExcelRow.ToString());
                        excells1.Font.Size = 6;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        if (prm_.dat_po == "") excells1 = ExcelL.ExlWs.get_Range("F8", rw + ExcelRow.ToString());
                        else excells1 = ExcelL.ExlWs.get_Range("G8", rw + ExcelRow.ToString());
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlPortrait;
                        #endregion
                    }
                    else
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A5", "I5");
                        excells1.EntireColumn.AutoFit();
                        ExcelL.ExlWs.Cells[10, 2] = "Данные не найдены";
                    }

                    string dir = STCLINE.KP50.Global.Constants.ExcelDir;// InputOutput.GetOutputDir();
                    fileName = "SverkaDay_" + prm_.year_ + "_" + prm_.month_.ToString("00") + "_" + Nzp_user + ".xls";
                    string filePath = STCLINE.KP50.Global.Constants.ExcelDir + fileName;
                    
                    
                    ret = ExcelL.SaveFile(filePath, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    
                    if (ret.result && InputOutput.useFtp)
                    {
                        ExcelL.DeleteObject();
                        ExcelL = null;

                        fileName = InputOutput.SaveOutputFile(filePath);
                    }

                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSverkaDay DataTable : ОШИБКА!";
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формирование GetSverkaDay " + ex.Message, MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание GetSverkaDay DataTable : ОШИБКА!" + ex.Message;
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

        public Returns GetSverkaMonth(ExcelSverkaPeriod prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            ExcelLoader ExcelL = null;

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSverkaMonth(prm_, out ret, Nzp_user);
            ExR.Close();

            try
            {
                ExcelL = new ExcelLoader();

                if (DT != null)
                {
                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    //спустили шапку 
                    #region заголовок
                    string pr = "";
                    if (prm_.search_date == 1) pr = " (опер. день) ";

                    string month = "";
                    switch (Convert.ToDateTime(prm_.dat_s).Month)
                    {
                        case 1: month = "январь " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 2: month = "февраль " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 3: month = "март " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 4: month = "апрель " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 5: month = "май " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 6: month = "июнь " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 7: month = "июль " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 8: month = "август " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 9: month = "сентябрь " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 10: month = "октябрь " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 11: month = "ноябрь " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                        case 12: month = "декабрь " + Convert.ToDateTime(prm_.dat_s).Year + " г."; break;
                    }

                    string s = " за " + month;
                    #endregion

                    excells1 = ExcelL.ExlWs.get_Range("A1", "J1");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сверка поступлений по банку " + s + pr;
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A2", "J2");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;

                    string s_name = SetReportHeader();

                    excells1.FormulaR1C1 = s_name;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A3", "A3");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.ColumnWidth = 3;

                    string col = "";
                    excells1 = ExcelL.ExlWs.get_Range("A5", "A7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Вид перечислений";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 11;


                    col = "B";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Р/счет";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 8;

                    col = "C";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Дата пл/пор";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 6;


                    if (prm_.add_geu_column == 1)
                    {
                        col = "D";
                        excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "ЖЭУ";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excells1.ColumnWidth = 10;
                    }


                    if (prm_.add_geu_column == 1) col = "E"; else col = "D";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Количество";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 3;

                    if (prm_.add_geu_column == 1) col = "F"; else col = "E";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Оплачено всего";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 8;

                    if (prm_.add_geu_column == 1) col = "G"; else col = "F";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = " Оплачено (в т.ч. юр. лицами)";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 7;

                    if (prm_.add_geu_column == 1) col = "H"; else col = "G";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Пеня";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 6;

                    if (prm_.add_geu_column == 1) col = "I"; else col = "H";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Комис. сбор";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 6;

                    if (prm_.add_geu_column == 1) col = "J"; else col = "I";
                    excells1 = ExcelL.ExlWs.get_Range(col + "5", col + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Оплачено (всего)";
                    excells1.WrapText = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 8;
                    #endregion

                    if (DT.Rows.Count > 0)
                    {

                        string rw = "I";
                        if (prm_.add_geu_column == 1) rw = "J";

                        #region Пишем тело
                        int ExcelRow = 7;
                        int i = 0;

                        string old_date = "", old_prefixls = "", old_payer = "";

                        int count_kvit = 0, pcount_kvit = 0, scount_kvit = 0, icount_kvit = 0;
                        decimal g_sum_ls = 0, sum_ur = 0, penya = 0, komiss = 0, tot = 0;
                        decimal pg_sum_ls = 0, psum_ur = 0, ppenya = 0, pkomiss = 0, ptot = 0;
                        decimal sg_sum_ls = 0, ssum_ur = 0, spenya = 0, skomiss = 0, stot = 0;
                        decimal ig_sum_ls = 0, isum_ur = 0, ipenya = 0, ikomiss = 0, itot = 0;

                        #region цикл
                        while (i < DT.Rows.Count)
                        {
                            ExcelRow++;

                            if (old_date == "") old_date = Convert.ToDateTime(DT.Rows[i]["dat_"]).ToShortDateString();
                            if (old_prefixls == "") old_prefixls = DT.Rows[i]["prefix_ls"].ToString().Trim();
                            if (old_payer == "") old_payer = DT.Rows[i]["payer"].ToString().Trim();

                            if (prm_.add_geu_column == 1)
                            {
                                #region Итого по дате
                                if (old_date != Convert.ToDateTime(DT.Rows[i]["dat_"]).ToShortDateString() ||
                                    old_prefixls != DT.Rows[i]["prefix_ls"].ToString().Trim() ||
                                    old_payer != DT.Rows[i]["payer"].ToString().Trim())
                                {
                                    excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 4]);
                                    excells1.Merge(Type.Missing);
                                    excells1.FormulaR1C1 = "Итого за день";
                                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                                    ExcelL.ExlWs.Cells[ExcelRow, 5] = count_kvit;
                                    ExcelL.ExlWs.Cells[ExcelRow, 6] = g_sum_ls;
                                    ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_ur;
                                    ExcelL.ExlWs.Cells[ExcelRow, 8] = penya;
                                    ExcelL.ExlWs.Cells[ExcelRow, 9] = komiss;
                                    ExcelL.ExlWs.Cells[ExcelRow, 10] = tot;
                                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                                    excells1.Font.Bold = true;
                                    ExcelRow++;

                                    old_date = Convert.ToDateTime(DT.Rows[i]["dat_"]).ToShortDateString();

                                    count_kvit = 0;
                                    g_sum_ls = 0;
                                    sum_ur = 0;
                                    penya = 0;
                                    komiss = 0;
                                    tot = 0;
                                }
                                #endregion
                            }

                            #region Итого по р/счету
                            if (old_prefixls != DT.Rows[i]["prefix_ls"].ToString().Trim() ||
                                old_payer != DT.Rows[i]["payer"].ToString().Trim())
                            {
                                int cl = 3;
                                if (prm_.add_geu_column == 1) cl = 4;
                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, cl]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого по " + old_prefixls;
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = pcount_kvit;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = pg_sum_ls;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = psum_ur;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ppenya;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = pkomiss;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ptot;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                                excells1.Font.Bold = true;
                                ExcelRow++;

                                old_prefixls = DT.Rows[i]["prefix_ls"].ToString().Trim();

                                pcount_kvit = 0;
                                pg_sum_ls = 0;
                                psum_ur = 0;
                                ppenya = 0;
                                pkomiss = 0;
                                ptot = 0;
                            }
                            #endregion

                            #region Итого по пачке
                            if (old_payer != DT.Rows[i]["payer"].ToString().Trim())
                            {
                                int cl = 3;
                                if (prm_.add_geu_column == 1) cl = 4;
                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, cl]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого по пачке (" + old_payer + ")";
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = scount_kvit;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = sg_sum_ls;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ssum_ur;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = spenya;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = skomiss;
                                cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = stot;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                                excells1.Font.Bold = true;

                                ExcelRow++;

                                old_payer = DT.Rows[i]["payer"].ToString().Trim();

                                scount_kvit = 0;
                                sg_sum_ls = 0;
                                ssum_ur = 0;
                                spenya = 0;
                                skomiss = 0;
                                stot = 0;
                            }
                            #endregion

                            #region подсчет
                            count_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                            g_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                            sum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                            penya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                            komiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                            tot += Decimal.Parse(DT.Rows[i]["tot"].ToString());

                            pcount_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                            pg_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                            psum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                            ppenya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                            pkomiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                            ptot += Decimal.Parse(DT.Rows[i]["tot"].ToString());

                            scount_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                            sg_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                            ssum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                            spenya += Decimal.Parse(DT.Rows[i]["penya"].ToString());
                            skomiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                            stot += Decimal.Parse(DT.Rows[i]["tot"].ToString());
                            #endregion

                            #region строки
                            //вид перечислений
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["payer"].ToString().Trim();

                            col = "B";
                            //Р/счет
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            if (DT.Rows[i]["prefix_ls"].ToString() == "" || DT.Rows[i]["prefix_ls"].ToString() == "0")
                                excells1.FormulaR1C1 = "Р/с не определен";
                            else excells1.FormulaR1C1 = DT.Rows[i]["prefix_ls"].ToString().Trim();

                            col = "C";
                            //Дата пл/пор
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Convert.ToDateTime(DT.Rows[i]["dat_"]).ToShortDateString();

                            if (prm_.add_geu_column == 1)
                            {
                                col = "D";
                                //ЖЭУ
                                excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                                excells1.FormulaR1C1 = DT.Rows[i]["geu"].ToString().Trim();
                            }

                            if (prm_.add_geu_column == 1) col = "E"; else col = "D";
                            //Количество
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Int32.Parse(DT.Rows[i]["count_kvit"].ToString());
                            icount_kvit += Int32.Parse(DT.Rows[i]["count_kvit"].ToString());

                            if (prm_.add_geu_column == 1) col = "F"; else col = "E";
                            //оплачено всего
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());
                            ig_sum_ls += Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString());

                            if (prm_.add_geu_column == 1) col = "G"; else col = "F";
                            //Оплачено (всего в т.ч. юридическими лицами)
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());
                            isum_ur += Decimal.Parse(DT.Rows[i]["sum_ur"].ToString());

                            if (prm_.add_geu_column == 1) col = "H"; else col = "G";
                            //пеня
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["penya"].ToString());
                            ipenya += Decimal.Parse(DT.Rows[i]["penya"].ToString());

                            if (prm_.add_geu_column == 1) col = "I"; else col = "H";
                            //комис. сбор
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["komiss"].ToString());
                            ikomiss += Decimal.Parse(DT.Rows[i]["komiss"].ToString());

                            if (prm_.add_geu_column == 1) col = "J"; else col = "I";
                            //всего
                            excells1 = ExcelL.ExlWs.get_Range(col + ExcelRow.ToString(), col + ExcelRow.ToString());
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["tot"].ToString());
                            itot += Decimal.Parse(DT.Rows[i]["tot"].ToString());

                            i++;
                            #endregion
                        }
                        #endregion
                        #endregion

                        #region Итого
                        if (ExcelRow >= 8)
                        {
                            if (prm_.add_geu_column == 1)
                            {
                                ExcelRow++;
                                #region итого за день
                                excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, 4]);
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = "Итого за день ";
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                                ExcelL.ExlWs.Cells[ExcelRow, 5] = count_kvit;
                                ExcelL.ExlWs.Cells[ExcelRow, 6] = g_sum_ls;
                                ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_ur;
                                ExcelL.ExlWs.Cells[ExcelRow, 8] = penya;
                                ExcelL.ExlWs.Cells[ExcelRow, 9] = komiss;
                                ExcelL.ExlWs.Cells[ExcelRow, 10] = tot;
                                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                                excells1.Font.Bold = true;
                                #endregion
                            }

                            ExcelRow++;

                            #region итого по р/счету
                            int cl = 3;
                            if (prm_.add_geu_column == 1) cl = 4;
                            excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, cl]);
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Итого по " + old_prefixls;
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = pcount_kvit;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = pg_sum_ls;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = psum_ur;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ppenya;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = pkomiss;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ptot;
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                            excells1.Font.Bold = true;
                            #endregion

                            ExcelRow++;

                            #region итого по пачке
                            cl = 3;
                            if (prm_.add_geu_column == 1) cl = 4;
                            excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, cl]);
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Итого по пачке (" + old_payer + ")";
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = scount_kvit;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = sg_sum_ls;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ssum_ur;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = spenya;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = skomiss;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = stot;
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                            excells1.Font.Bold = true;
                            #endregion

                            ExcelRow++;
                        }
                        #endregion

                        #region Пишем подвал
                        if (ExcelRow >= 8)
                        {
                            int cl = 3;
                            if (prm_.add_geu_column == 1) cl = 4;
                            excells1 = ExcelL.ExlWs.get_Range(ExcelL.ExlWs.Cells[ExcelRow, 1], ExcelL.ExlWs.Cells[ExcelRow, cl]);
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Итого";

                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = icount_kvit;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ig_sum_ls;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = isum_ur;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ipenya;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = ikomiss;
                            cl++; ExcelL.ExlWs.Cells[ExcelRow, cl] = itot;

                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), rw + ExcelRow.ToString());
                            excells1.Font.Bold = true;
                        }
                        #endregion

                        #region оформление
                        excells1 = ExcelL.ExlWs.get_Range("A5", rw + ExcelRow.ToString());
                        excells1.Font.Size = 6;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excells1 = ExcelL.ExlWs.get_Range("F8", rw + ExcelRow.ToString());
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlPortrait;
                        #endregion
                    }
                    else
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A5", "I5");
                        excells1.EntireColumn.AutoFit();
                        ExcelL.ExlWs.Cells[10, 2] = "Данные не найдены";
                    }

                    #region Сохраняем файл
                    try
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SverkaMonth_" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        string filePath = STCLINE.KP50.Global.Constants.ExcelDir + fileName;

                        ret = ExcelL.SaveFile(filePath, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);

                        if (ret.result && InputOutput.useFtp)
                        {
                            ExcelL.DeleteObject();
                            ExcelL = null;

                            fileName = InputOutput.SaveOutputFile(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }
                    finally
                    {
                        ////удаление объекта
                        if (ExcelL != null)
                        {
                            ExcelL.DeleteObject();
                        }
                    }
                    #endregion
                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSverkaMonth DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формирование GetSverkaMonth " + ex.Message, MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание GetSverkaMonth DataTable : ОШИБКА!" + ex.Message;
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

        public Returns GetDataSaldoPoPerechisl(MoneyDistrib prm_, string Nzp_user, ref string fileName)
        {
            Returns ret;

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDataSaldoPoPerechisl(prm_, out ret, Nzp_user);
            ExR.Close();

            if (!ret.result) return ret;

            ExcelLoader ExcelL = null;

            try
            {
                if (DT != null)
                {
                    string[] cols = new string[17] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q" };
                    ExcelL = new ExcelLoader();

                    //ExcelCreater ExcelCr = new ExcelCreater();
                    int number_date, number_area, number_bank, number_payer, number_serv, number_dom;
                    if (!Utils.GetParams(prm_.groupby, STCLINE.KP50.Global.Constants.act_groupby_date.ToString(), out number_date)) number_date = -1;
                    if (!Utils.GetParams(prm_.groupby, STCLINE.KP50.Global.Constants.act_groupby_area.ToString(), out number_area)) number_area = -1;
                    if (!Utils.GetParams(prm_.groupby, STCLINE.KP50.Global.Constants.act_groupby_bank.ToString(), out number_bank)) number_bank = -1;
                    if (!Utils.GetParams(prm_.groupby, STCLINE.KP50.Global.Constants.act_groupby_payer.ToString(), out number_payer)) number_payer = -1;
                    if (!Utils.GetParams(prm_.groupby, STCLINE.KP50.Global.Constants.act_groupby_service.ToString(), out number_serv)) number_serv = -1;
                    if (!Utils.GetParams(prm_.groupby, STCLINE.KP50.Global.Constants.act_groupby_dom.ToString(), out number_dom)) number_dom = -1;
                    int icol = 1;
                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    //спустили шапку
                    excells1 = ExcelL.ExlWs.get_Range(cols[1] + "1", cols[10] + "1");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сальдо по перечислениям";
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range(cols[1] + "2", cols[10] + "2");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    string s = " за " + prm_.dat_oper;
                    if (prm_.dat_oper_po != "") s += " - " + prm_.dat_oper_po;
                    excells1.FormulaR1C1 = s;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range(cols[1] + "3", cols[1] + "3");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.ColumnWidth = 3;

                    excells1 = ExcelL.ExlWs.get_Range(cols[1] + "5", cols[1] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "№ п/п";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 9.5;


                    for (int i = 0; i < 6; i++)
                    {
                        if (number_date == i)
                        {
                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Дата";
                            excells1.ColumnWidth = 10;
                        }
                        if (number_area == i)
                        {
                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Управляющая организация";
                            excells1.ColumnWidth = 30;
                        }
                        else if (number_bank == i)
                        {
                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Банк";
                            excells1.ColumnWidth = 30;
                        }
                        else if (number_payer == i)
                        {
                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Подрядчик";
                            excells1.ColumnWidth = 30;
                        }
                        else if (number_serv == i)
                        {
                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Услуга";
                            excells1.ColumnWidth = 30;
                        }
                        else if (number_dom == i)
                        {
                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = "Адрес дома";
                            excells1.ColumnWidth = 30;
                        }
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excells1.WrapText = true;
                    }

                    icol++;
                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сальдо начальное";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;

                    icol++;
                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Распределено";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;

                    icol++;
                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Следует удержать за обслуживание";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;

                    icol++;
                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Начислено за обслуживание";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;

                    icol++;
                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Изменение";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;

                    icol++;
                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Зачислено";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;

                    icol++;
                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Перечислить";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;

                    icol++;
                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + "5", cols[icol] + "7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сальдо конечное";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excells1.ColumnWidth = 10;
                    #endregion

                    if (DT.Rows.Count > 0)
                    {
                        #region Пишем тело

                        int ExcelRow = 8;
                        int i = 0;

                        while (i < DT.Rows.Count)
                        {
                            icol = 1;
                            #region строки
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["num"].ToString().Trim();

                            for (int ind = 0; ind < 6; ind++)
                            {
                                if (number_date == ind)
                                {
                                    icol++;
                                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                                    excells1.FormulaR1C1 = DT.Rows[i]["dat_oper"].ToString().Trim();
                                }
                                if (number_area == ind)
                                {
                                    icol++;
                                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                                    excells1.FormulaR1C1 = DT.Rows[i]["area"].ToString().Trim();
                                }
                                else if (number_bank == ind)
                                {
                                    icol++;
                                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                                    excells1.FormulaR1C1 = DT.Rows[i]["bank"].ToString().Trim();
                                }
                                else if (number_payer == ind)
                                {
                                    icol++;
                                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                                    excells1.FormulaR1C1 = DT.Rows[i]["payer"].ToString().Trim();
                                }
                                else if (number_serv == ind)
                                {
                                    icol++;
                                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                                    excells1.FormulaR1C1 = DT.Rows[i]["service"].ToString().Trim();
                                }
                                else if (number_dom == ind)
                                {
                                    icol++;
                                    excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                                    excells1.FormulaR1C1 = DT.Rows[i]["adr"].ToString().Trim();
                                }
                            }

                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_in"].ToString().Trim();

                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_rasp"].ToString().Trim();

                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_ud"].ToString().Trim();

                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_naud"].ToString().Trim();

                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_reval"].ToString().Trim();

                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_charge"].ToString().Trim();

                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_send"].ToString().Trim();

                            icol++;
                            excells1 = ExcelL.ExlWs.get_Range(cols[icol] + ExcelRow.ToString(), cols[icol] + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_out"].ToString().Trim();

                            ExcelRow++;
                            i++;
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A5", "I5");
                        excells1.EntireColumn.AutoFit();
                        ExcelL.ExlWs.Cells[10, 2] = "Данные не найдены";
                    }

                    string dir = InputOutput.GetOutputDir();
                    fileName = GetFileName(dir, "SaldoPoPerechisl_" + prm_.dat_oper + "_" + prm_.dat_oper_po + "_" + Nzp_user + ".xls");
                    ret = ExcelL.SaveFile(dir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);

                    if (ret.result && InputOutput.useFtp)
                    {
                        ExcelL.DeleteObject();
                        ExcelL = null;

                        fileName = InputOutput.SaveOutputFile(dir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание SaldoPoPerechisl DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формирование SaldoPoPerechisl " + ex.Message, MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание SaldoPoPerechisl DataTable : ОШИБКА!" + ex.Message;
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

        /// <summary>
        /// Выгрузка файла обмена
        /// </summary>
        /// <returns>true/false</returns>   
        public void UploadKLADRAddrSpace(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = UploadKLADRAddrSpace(out ret, cont.KLADRfinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns UploadKLADRAddrSpace(out Returns ret, KLADRFinder finder)
        {
            #region Подключение к БД
            IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
            ret = DBManager.OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции UploadKLADRAddrSpace ", MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при открытии соединения.";
                ret.result = false;
                ret.tag = -1;
                return ret;
            }
            #endregion Подключение к БД

            DbKladr db = new DbKladr(con_db);
            ret = Utils.InitReturns();
            ret = db.UploadKLADRAddrSpace(out ret, finder);
            return ret;
        }

        /// <summary>
        /// Выгрузка файла обмена
        /// </summary>
        /// <returns>true/false</returns>   
        public void GenerateExchange(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = GenerateExchange(out ret, cont.supgfinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GenerateExchange(out Returns ret, SupgFinder finder)
        {
            //ExcelRep ExR = new ExcelRep();
            USP ExR = new USP();
            ret = Utils.InitReturns();
            ret = ExR.GenerateExchange(out ret, finder);
            return ret;
        }

        /// <summary>
        /// Выгрузка начислений УЭС
        /// </summary>
        /// <returns>true/false</returns>   
        public void GenerateUESVigr(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = GenerateUESVigr(out ret, cont.supgfinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GenerateUESVigr(out Returns ret, SupgFinder finder)
        {
            //ExcelRep ExR = new ExcelRep();
            DbUES ExR = new DbUES();

            ret = Utils.InitReturns();
            ret = ExR.GenerateUESVigr(out ret, finder);
            return ret;
        }

        /// <summary>
        /// Загрузка файла
        /// </summary>
        /// <returns>true/false</returns>   
        public void LoadHarGilFondGKU(object container)
        {
            ParamContainer cont = (ParamContainer)container;
            //Returns ret = LoadHarGilFondGKU(out ret, cont.filesImportedFinder);
            Returns ret = FileLoader(out ret, cont.filesImportedFinder);
        }



        public void LoadOneTime(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            //Returns ret = LoadHarGilFondGKU(out ret, cont.filesImportedFinder);
            Returns ret = LoadOneTime(out ret, cont.filesImportedFinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }
        public Returns LoadOneTime(out Returns ret, FilesImported finder)
        {

            IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
            ret = DBManager.OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции LoadFile", MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                ret.tag = -1;
                return ret;
            }
            try
            {
                DBLoadOneTime fl = new DBLoadOneTime(con_db);
                ret = Utils.InitReturns();
                ret = fl.LoadOneTime(finder);
            }
            finally
            {
                con_db.Close();
            }
            return ret;
        }

        public Returns FileLoader(out Returns ret, FilesImported finder)
        {

            IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
            ret = DBManager.OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции LoadFile", MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                ret.tag = -1;
                return ret;
            }
            try
            {
                DbFileLoader fl = new DbFileLoader(con_db);
                ret = Utils.InitReturns();
                ret = fl.LoadFile(finder, ref ret);

                //DBLoadDataForPassportistka loadPasp = new DBLoadDataForPassportistka();
                //ret = loadPasp.Run(finder);
            }
            finally
            {
                con_db.Close();
            }
            return ret;
        }


        /// <summary>
        /// Выгрузка оплат МУРЦ
        /// </summary>
        /// <returns>true/false</returns>   
        public void GenerateMURCVigr(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = GenerateMURCVigr(out ret, cont.supgfinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GenerateMURCVigr(out Returns ret, SupgFinder finder)
        {
            //ExcelRep ExR = new ExcelRep();
            DbMURC ExR = new DbMURC();
            ret = Utils.InitReturns();
            ret = ExR.GenerateMURCVigr(out ret, finder);
            return ret;
        }


        /// <summary>
        /// Смена УК для ЛС
        /// </summary>
        /// <returns>true/false</returns>  
        public void ChangeArea(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = ChangeArea(out ret, cont.ChangeAreaFinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns ChangeArea(out Returns ret, FinderChangeArea finder)
        {
            ExcelRep ExR = new ExcelRep();
            ret = Utils.InitReturns();
            ret = ExR.ChangeArea(finder);
            return ret;
        }


        public void GetSaldo_5_10(object container)
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
            ret = excelRepDb.AddToPoolThread(cont.ChargeFind.nzp_user, "empty", 2,
                "Отчет \"Сальдовая оборотная ведомость 5.10\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.ChargeFind.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSaldo_5_10(out ret, cont.ChargeFind, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.ChargeFind.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.ChargeFind.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.ChargeFind.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.ChargeFind.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        /// <summary>
        /// Выгрузка в кассу 3.0
        /// </summary>
        /// <returns>true/false</returns>      

        public Returns GetSaldo_5_10(out Returns ret, ChargeFind finder, ref string path)
        {
            DbCharge DbCharge = new DbCharge();
            ret = Utils.InitReturns();
            List<SaldoRep> res = new List<SaldoRep>();
            try
            {
                res = DbCharge.FillRep_5_10(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка выгрузка в кассу 3.0(GetSaldo_5_10)", ex);
                ret.result = false;
                return ret;
            }
            #region Запонение таблицы для отчета
            DataSet FDataSet = new DataSet();

            System.Data.DataTable table = new System.Data.DataTable();
            table.TableName = "Q_master";
            FDataSet.Tables.Add(table);

            table.Columns.Add("service", typeof(string));
            table.Columns.Add("sum_insaldo_k", typeof(decimal));
            table.Columns.Add("sum_insaldo_d", typeof(decimal));
            table.Columns.Add("sum_insaldo", typeof(decimal));
            table.Columns.Add("reval", typeof(decimal));
            table.Columns.Add("real_charge", typeof(decimal));
            table.Columns.Add("sum_real", typeof(decimal));
            table.Columns.Add("sum_money", typeof(decimal));
            table.Columns.Add("sum_outsaldo_k", typeof(decimal));
            table.Columns.Add("sum_outsaldo_d", typeof(decimal));
            table.Columns.Add("sum_outsaldo", typeof(decimal));

            #endregion


            //готовый список для вывода

            for (int i = 0; i < res.Count; i++)
            {
                table.Rows.Add(res[i].service, res[i].sum_insaldo_k,
                    res[i].sum_insaldo_d, res[i].sum_insaldo,
                    res[i].reval, res[i].real_charge,
                    res[i].sum_real, res[i].sum_money,
                    res[i].sum_outsaldo_d, res[i].sum_outsaldo_k,
                    res[i].sum_outsaldo);
            }


            FastReport.Report rep = new FastReport.Report();
            string template = "Web_saldo_rep5_10.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));

            rep.RegisterData(FDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            rep.SetParameterValue("month_", finder.month_);
            rep.SetParameterValue("year_", finder.year_);
            rep.Prepare();

            FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

            string fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "Saldo_5_10" +
                            finder.year_.ToString() + "_" +
                            finder.month_.ToString() + "_" + finder.nzp_user) + ".xls";
            path = Path.Combine(STCLINE.KP50.Global.Constants.ExcelDir, fileName);
            export_xls.ShowProgress = false;
            export_xls.Export(rep, path);

            return ret;
        }

        /// <summary>
        /// Выгрузка для обмена с соц.защитой (Тула)
        /// </summary>
        /// <returns>true/false</returns>  
        public void GetExchangeSZ(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = GetExchangeSZ(cont.Finder, cont.yy, cont.mm, cont.nzp,cont.serv);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GetExchangeSZ(Finder finder, string year, string month, int nzp_ex_sz, bool isPkodInLs)
        {
            ExcelRep ExR = new ExcelRep();
            Returns ret = Utils.InitReturns();
            ret = ExR.GetExchangeSZ(finder, year, month, nzp_ex_sz, isPkodInLs);
            return ret;
        }

        public void GetUploadExchangeSZ(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (ParamContainer)container;
            Returns ret = GetUploadExchangeSZ(cont.Finder, cont.yy, cont.from_yy, cont.mm, cont.parList);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GetUploadExchangeSZ(Finder finder, string fileName, string fileNameFull, string encodingValue, List<int> listWP)
        {
            Returns ret;
            using (var exR = new DbExchange())
            {
                ret = exR.GetUploadExchangeSZ(finder, fileName, fileNameFull, encodingValue, listWP);
            }
            return ret;
        }


        public void UploadReestrInFon(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = UploadReestrInFon(cont.filesImportedFinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns UploadReestrInFon(FilesImported finder)
        {
            var ExR = new StartLoadPackFromBank();
            Returns ret = Utils.InitReturns();
            ret = ExR.UploadReestrInFon(finder);
            return ret;
        }

        /// <summary>
        /// протокол для загрузки оплат от ВТБ24
        /// </summary>
        /// <param name="container"></param>
        public void GetProtocolVTB24(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();
            DbExchange exchange = new DbExchange();
            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";
            int nzp_exc = 0;
            ////запись в БД о постановки в поток(статус 0)
            //ret = excelRepDb.AddToPoolThread(cont.ExFinder.nzp_user, "empty", 2,
            //    "Отчет \"Протокол о загрузке оплат от ВТБ24\" ", ref time, "");


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = cont.ExFinder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Протокол загрузки оплат от ВТБ24 №" + cont.ExFinder.VTB24ReestrID,
                is_shared = 1
            });
            if (!ret.result) return;

            nzp_exc = ret.tag;

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.ExFinder.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetProtocolVTB24(out ret, cont.ExFinder, ref fileName);
            if (ret.result)
            {
                //MonitorLog.WriteLog("Пользователь: " + cont.ExFinder.nzp_user +
                //    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                ////Запись в БД об успешном формировании
                //ret = excelRepDb.MarkPoolThread(cont.ExFinder.nzp_user, "2", 2, path + fileName, time);

                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzp_exc, progress = 1 });
                excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzp_exc, status = ExcelUtility.Statuses.Success, exc_path = path + fileName });

                ExFinder finder = cont.ExFinder;
                //передаем ключ-nzp_exc для протокола
                ret = exchange.UpdateExcVTB24(finder, nzp_exc);
            }
            else
            {
                //MonitorLog.WriteLog("Пользователь: " + cont.ExFinder.nzp_user +
                //    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                ////запись об неудачном формировании
                //ret = excelRepDb.MarkPoolThread(cont.ExFinder.nzp_user, "-1", 2, "", time);
                excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzp_exc, status = ExcelUtility.Statuses.Failed, exc_path = "" });
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }


        /// <summary>
        /// протокол для ВТБ24
        /// </summary>
        /// <returns>true/false</returns>      

        public Returns GetProtocolVTB24(out Returns ret, ExFinder finder, ref string fileName)
        {
            var DbExchange = new DbExchange();
            ret = Utils.InitReturns();
            var res = new ProtocolVTB24();
            var rep = new FastReport.Report();
            var template = "protocol_VTB24.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));

            if (finder.Status == 0)
            {
                var user_name = DbExchange.GetUserName(finder, out ret);
                rep.SetParameterValue("file_name", finder.fileName);
                rep.SetParameterValue("status", finder.status);
                rep.SetParameterValue("user_name", user_name);
                rep.SetParameterValue("download_date", DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"));
                rep.SetParameterValue("text", finder.errors);
   
                rep.SetParameterValue("file_id", "");
                rep.SetParameterValue("message_date", "");
                rep.SetParameterValue("count_oper", "");
                rep.SetParameterValue("compared_count", "");
                rep.SetParameterValue("receiver", "");
                rep.SetParameterValue("total_amount", "");
                rep.SetParameterValue("compared_amount", "");

                DataSet FDataSet = new DataSet();
                System.Data.DataTable table = new System.Data.DataTable();
                table.TableName = "ls";
                FDataSet.Tables.Add(table);

                table.Columns.Add("account", typeof(decimal));
                table.Columns.Add("operation_uni", typeof(string));
                rep.RegisterData(FDataSet);
                rep.GetDataSource("ls").Enabled = false;
            }
            else
            {
                res = DbExchange.GetProtocolVTB24(finder, out ret);
                #region Заполнение таблицы для отчета
                DataSet FDataSet = new DataSet();

                System.Data.DataTable table = new System.Data.DataTable();
                table.TableName = "ls";
                FDataSet.Tables.Add(table);

                table.Columns.Add("account", typeof(decimal));
                table.Columns.Add("operation_uni", typeof(string));

                #endregion
                //готовый список для вывода
                if (res.Rows != null)
                    for (int i = 0; i < res.Rows.Count; i++)
                    {
                        table.Rows.Add(res.Rows[i].account, res.Rows[i].operation_uni);
                    }

                rep.RegisterData(FDataSet);
                rep.GetDataSource("ls").Enabled = true;
                rep.SetParameterValue("file_name", res.file_name);
                rep.SetParameterValue("file_id", res.file_id);
                rep.SetParameterValue("message_date", res.message_date.ToString("dd.MM.yyyy"));
                rep.SetParameterValue("user_name", res.user_name);
                rep.SetParameterValue("download_date", res.download_date.ToString("yyyy.MM.dd HH:mm:ss"));
                rep.SetParameterValue("count_oper", res.count_oper);
                rep.SetParameterValue("compared_count", res.compared_count);
                rep.SetParameterValue("receiver", res.receiver);
                rep.SetParameterValue("total_amount", res.total_amount);
                rep.SetParameterValue("compared_amount", res.compared_amount);
                try
                {
                    if (res.compared_amount != res.total_amount || res.compared_count != res.count_oper)
                    {
                        res.errors += "\n Сопоставлены не все лицевые счета.";
                        finder.Status = (int)StatusVTB24.WithErrors;
                        ret = DbExchange.UpdateStatusReestr(null, null, finder);
                    }
                    else
                    {
                        finder.Status = (int)StatusVTB24.Success;
                        ret = DbExchange.UpdateStatusReestr(null, null, finder);
                    }
                }
                catch
                {
                    ret.result = false;
                    return ret;
                }
                VTB24Info inf = new VTB24Info();
                rep.SetParameterValue("status", inf.getNameStatusVTB24(finder.Status));
                rep.SetParameterValue("text", res.errors);
            }
            rep.Prepare();

            FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

            fileName = "PROTOCOl_VTB24_NUM_" + finder.VTB24ReestrID + ".xls";
            string path = Path.Combine(InputOutput.GetInputDir(), fileName);
            export_xls.ShowProgress = false;
            export_xls.Export(rep, path);

            //перенос  на ftp сервер
            path = InputOutput.SaveOutputFile(path);

            return ret;
        }


        #region Обмен со сторонними поставщиками

        /// <summary>
        /// Выгрузка файла синхронизации
        /// </summary>
        /// <returns>true/false</returns>  
        public void FileSyncLS(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = FileSyncLS(cont.ExFinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns FileSyncLS(ExFinder finder)
        {
            DbExchange ExR = new DbExchange();
            Returns ret = Utils.InitReturns();
            ret = ExR.FileSyncLS(finder);
            return ret;
        }


        /// <summary>
        /// Выгрузка файла изменений параметров ЛС
        /// </summary>
        /// <returns>true/false</returns>  
        public void FileChangeLS(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = FileChangeLS(cont.ExFinder);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns FileChangeLS(ExFinder finder)
        {
            DbExchange ExR = new DbExchange();
            Returns ret = Utils.InitReturns();
            ret = ExR.FileChangeLS(finder);
            return ret;
        }
        #endregion


        public void StartTransfer(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (TransferParams)container;
            var db = new DbExchange();
            db.StartTransfer(cont);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        #region Загрузка оплат от ВТБ24
        public void UploadVTB24(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            var cont = (FilesImported)container;
            var finder = (FilesImported)cont;
            var exFinder = new ExFinder
            {
                nzp_user = finder.nzp_user,
                nzp_role = finder.nzp_role,
                RolesVal = finder.RolesVal,
                webLogin = finder.webLogin,
                webUname = finder.webUname,
                remoteLogin = finder.remoteLogin,
                fileName = finder.loaded_name
            };
            var db = new DbExchange();
            var ret = db.UploadVTB24(cont);
            exFinder.VTB24ReestrID = ret.tag;
            exFinder.errors = ret.text;
            exFinder.Status = Convert.ToInt32(ret.result);
            exFinder.status = !ret.result ? "Ошибка при загрузке" : "Успешно загружен";

            var exCont = new ParamContainer { ExFinder = exFinder };
            GetProtocolVTB24(exCont);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }
        #endregion
    }
}
