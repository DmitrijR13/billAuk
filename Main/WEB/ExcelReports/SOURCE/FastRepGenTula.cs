using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using FastReport;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;



namespace STCLINE.KP50.REPORT
{
    //Класс генерирующий отчеты
    //содержит процедуры для генерации конкретных отчетов
    public partial class ReportGen
    {

        //Отчет:  Сводный отчет по начислениям для Тулы 
        public Returns GetServSuppNach(ReportPrm prm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetServSuppNach(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "tula_1.frx";
            
            rep.Load(PathHelper.GetReportTemplatePath(template));
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;

            //установка параметров отчета
            rep.SetParameterValue("month", prm.month.ToString("00"));
            rep.SetParameterValue("year", prm.year);
            rep.SetParameterValue("reportHeader", prm.reportDopParams["Заголовок отчета"]);
            rep.SetParameterValue("sumHeader", prm.reportDopParams["Вид начислено"]);
            rep.SetParameterValue("ercName", prm.reportDopParams["ЕРЦ"]);

            
            

            //Определение принциала
            string principal = "";
            if (prm.reportAreaList.Count > 0)
            {

                foreach (KeyValuePair<string, string> kp in prm.reportAreaList)
                    principal += kp.Value + ", ";
            }

            if (principal == "")
            {
                if (prm.reportSuppList.Count > 0)
                {
                    foreach (KeyValuePair<string, string> kp in prm.reportSuppList)
                        principal += kp.Value + ", ";
                }
            }

            if (principal != "") principal = principal.Substring(0, principal.Length - 2);

            rep.SetParameterValue("principal", principal);
            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован " + prm.reportName;
                ret.tag = rep.Report.PreparedPages.Count;


                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    prm.reportName.Replace(" ","_")+"_" + prm.month.ToString("00") + ".20" + prm.year.ToString("00") + ".xlsx";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован "+prm.reportName;
                MonitorLog.WriteLog("Ошибка формирования отчета " +prm.reportName+" "+
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }


        //Отчет:  Сводный отчет по начислениям для Тулы 
        public void GetServSuppNach(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            //string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.reportPrm.nzp_user, "empty", 2,
                "Отчет \"" + cont.reportPrm.reportName + "\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.reportPrm.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetServSuppNach(cont.reportPrm, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.reportPrm.nzp_user 
                    + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, Path.GetFileName(fileName), time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.reportPrm.nzp_user
                    + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.reportPrm.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }



        //Отчет:  Сводный отчет по поступлениям для Тулы 
        public Returns GetServSuppMoney(ReportPrm prm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetServSuppMoney(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "tula_2.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;

            //установка параметров отчета
            rep.SetParameterValue("dateBegin", prm.reportDatBegin);
            rep.SetParameterValue("dateEnd", prm.reportDatEnd);
            //rep.SetParameterValue("principal", prm.remark);

            //Dictionary<int, string> st;

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован " + prm.reportName;
                ret.tag = rep.Report.PreparedPages.Count;


                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    prm.reportName.Replace(" ","_") + DateTime.Now.ToShortDateString()+ ".xlsx";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован "+prm.reportName;
                MonitorLog.WriteLog("Ошибка формирования отчета  " +prm.reportName+" "+
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }


        //Отчет:  Сводный отчет по поступлениям для Тулы 
        public void GetServSuppMoney(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            //string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            
            
            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.reportPrm.nzp_user, "empty", 2, "Отчет \"" + 
                cont.reportPrm.reportName + "\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.reportPrm.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetServSuppMoney(cont.reportPrm, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + 
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, Path.GetFileName(fileName), time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.reportPrm.nzp_user 
                    + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.reportPrm.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }


        //Отчет: Сводный отчет по принятым и перечисленным средствам для Тулы
        public Returns GetServSuppMoney2(ReportPrm prm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetServSuppMoney2(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "tula_4.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;

            //установка параметров отчета
            rep.SetParameterValue("reportHeader", "3.73 Сводный отчет по принятым и перечисленным средствам для Тулы"); 
            rep.SetParameterValue("dateBegin", prm.reportDatBegin);
            rep.SetParameterValue("dateEnd", prm.reportDatEnd);
            //rep.SetParameterValue("principal", prm.remark);

            //Dictionary<int, string> st;

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован " + prm.reportName;
                ret.tag = rep.Report.PreparedPages.Count;


                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    prm.reportName.Replace(" ", "_") + DateTime.Now.ToShortDateString() + ".xlsx";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован " + prm.reportName;
                MonitorLog.WriteLog("Ошибка формирования отчета  " + prm.reportName + " " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }


        //Отчет:  Сводный отчет по принятым и перечисленным средствам для Тулы
        public void GetServSuppMoney2(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            //string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";



            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.reportPrm.nzp_user, "empty", 2, "Отчет \"" +
                cont.reportPrm.reportName + "\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.reportPrm.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetServSuppMoney2(cont.reportPrm, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, Path.GetFileName(fileName), time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.reportPrm.nzp_user
                    + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.reportPrm.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }



        //Отчет: Справка по должникам по Туле
        public Returns GetSpravDolgTula(ReportPrm prm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSpravDolgTula(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "tula_6.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;

            //установка параметров отчета
            rep.SetParameterValue("dateBegin", prm.reportDatBegin);
            rep.SetParameterValue("dateEnd", prm.reportDatEnd);
            //rep.SetParameterValue("principal", prm.remark);

            //Dictionary<int, string> st;

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован " + prm.reportName;
                ret.tag = rep.Report.PreparedPages.Count;


                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    prm.reportName.Replace(" ", "_") + DateTime.Now.ToShortDateString() + ".xlsx";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован " + prm.reportName;
                MonitorLog.WriteLog("Ошибка формирования отчета  " + prm.reportName + " " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }

        //Отчет:  Справка по должникам по Туле
        public void GetSpravDolgTula(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            //string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";



            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.reportPrm.nzp_user, "empty", 2, "Отчет \"" +
                cont.reportPrm.reportName + "\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.reportPrm.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSpravDolgTula(cont.reportPrm, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, Path.GetFileName(fileName), time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.reportPrm.nzp_user
                    + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.reportPrm.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }



        // Отчет по должникам по Туле
        public Returns GetListDolgTula(ReportPrm prm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetListDolgTula(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "tula_5.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;

            //установка параметров отчета
            rep.SetParameterValue("day", "01");
            rep.SetParameterValue("month", prm.month);
            rep.SetParameterValue("year", prm.year);

            rep.SetParameterValue("supplier", "");
            //rep.SetParameterValue("principal", prm.remark);

            //Dictionary<int, string> st;

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован " + prm.reportName;
                ret.tag = rep.Report.PreparedPages.Count;


                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    prm.reportName.Replace(" ", "_") + DateTime.Now.ToShortDateString() + ".xlsx";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован " + prm.reportName;
                MonitorLog.WriteLog("Ошибка формирования отчета  " + prm.reportName + " " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }

        // Отчет по должникам по Туле
        public void GetListDolgTula(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            //string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";



            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.reportPrm.nzp_user, "empty", 2, "Отчет \"" +
                cont.reportPrm.reportName + "\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.reportPrm.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetListDolgTula(cont.reportPrm, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, Path.GetFileName(fileName), time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.reportPrm.nzp_user
                    + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.reportPrm.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }


        //Отчет справка по поставщикам Тула
        public Returns GetSpravSuppTula(ReportPrm prm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSpravSuppTula(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "tula_7.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            rep.SetParameterValue("director", "");
            rep.SetParameterValue("director_fio", "");
            rep.SetParameterValue("day_s", Convert.ToDateTime(prm.reportDatBegin).Day);
            rep.SetParameterValue("month_s", Convert.ToDateTime(prm.reportDatBegin).Month);
            rep.SetParameterValue("year_s", Convert.ToDateTime(prm.reportDatBegin).Year);

            rep.SetParameterValue("day_po", Convert.ToDateTime(prm.reportDatEnd).Day);
            rep.SetParameterValue("month_po", Convert.ToDateTime(prm.reportDatEnd).Month);
            rep.SetParameterValue("year_po", Convert.ToDateTime(prm.reportDatEnd).Year);

            rep.SetParameterValue("town", "");
            rep.SetParameterValue("ulica", "");
            rep.SetParameterValue("ndom", "");
            rep.SetParameterValue("nkvar", "");
            rep.SetParameterValue("fio", "");
            //Dictionary<int, string> st;

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован " + prm.reportName;
                ret.tag = rep.Report.PreparedPages.Count;


                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    prm.reportName.Replace(" ", "_") + DateTime.Now.ToShortDateString() + ".xlsx";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован " + prm.reportName;
                MonitorLog.WriteLog("Ошибка формирования отчета  " + prm.reportName + " " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }

        // Отчет справка по поставщикам Тула
        public void GetSpravSuppTula(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            //string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";



            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.reportPrm.nzp_user, "empty", 2, "Отчет \"" +
                cont.reportPrm.reportName + "\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.reportPrm.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSpravSuppTula(cont.reportPrm, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, Path.GetFileName(fileName), time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.reportPrm.nzp_user
                    + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.reportPrm.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

    }
}
