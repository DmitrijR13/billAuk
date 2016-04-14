using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using FastReport;
using FastReport.Utils;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;
using SevenZip;
using JCS;
using System.Diagnostics;
using STCLINE.KP50.Utility;



namespace STCLINE.KP50.REPORT
{
    //Класс генерирующий отчеты
    //содержит процедуры для генерации конкретных отчетов
    public partial class ReportGen
    {
        //Отчет: Сальдовая ведомость для энергосбыта
        public void GetSaldoVedEnergo(object container) {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer) container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"50.1 Сальдовая ведомость\" ", ref time, "");
            cont.listprm[0].nzp_key = ret.tag;
            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSaldoVedEnergo(cont.listprm[0], ref fileName);
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

        //Отчет: Сальдовая ведомость для энергосбыта
        public Returns GetSaldoVedEnergo(Prm prm, ref string fileName) {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSaldoVedEnergo(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "web_324.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            //fDataSet.DataSetName = "Q_master";
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            //установка параметров отчета
            rep.SetParameterValue("month", prm.month_.ToString("00"));
            rep.SetParameterValue("year", prm.year_);
            rep.SetParameterValue("date", DateTime.Now.ToString("dd.MM.yyyy"));
            rep.SetParameterValue("services", prm.service);

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован 50.1 Сальдовая ведомость для энергосбыта";
                ret.tag = rep.Report.PreparedPages.Count;

                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    "50_1_Sald_ved_" + prm.month_.ToString("00") + ".20" + prm.year_.ToString("00") + "_" + prm.nzp_serv + ".xls";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован 50.1 Сальдовая ведомость для энергосбыта";
                MonitorLog.WriteLog("Ошибка формирования отчета 50.1 Сальдовая ведомость для Энергосбыта " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }

        //Отчет: Список временно зарегистрированных
        public void GetVremZareg(object container) {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer) container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Список временно зарегистрированных\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetVremZareg(cont.Kartfinder, ref fileName);
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

        /*  public void GetSaldoPere(object container)
          {
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
              ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Сальдо по перечислениям\" ", ref time, "");

              //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
              ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

              //Старт формирования
              ret = this.GetSaldoPere(cont.MoneyDistrib, ref fileName);
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
          }*/

        //Отчет: Список для военкомата
        public void GetVoenkomat(object container) {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer) container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Список для военкомата\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetVoenkomat(cont.Kartfinder, ref fileName);
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

        //Отчет: временно зарегистрированные для пасаортистки
        public Returns GetVremZareg(Kart finder, ref string fileName) {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetChoosenData(finder, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "vrem_zareg.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            //fDataSet.DataSetName = "Q_master";
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            //установка параметров отчета
            rep.SetParameterValue("dat_s", finder.dat_izm);
            rep.SetParameterValue("dat_po", finder.dat_izm_po);

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован Список временно зарегистрированных";
                ret.tag = rep.Report.PreparedPages.Count;

                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    "vrem_zareg" + finder.dat_izm + "_" + finder.dat_izm + ".xls";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован Список временно зарегистрированных";
                MonitorLog.WriteLog("Ошибка формирования отчета Список временно зарегистрированных " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }

        public Returns GetSaldoPere(MoneyDistrib finder, ref string fileName) {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDataSaldoPoPerechisl(finder, out ret, finder.nzp_user.ToString());
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "saldopoperechisl.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            //fDataSet.DataSetName = "Q_master";
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            //установка параметров отчета
            rep.SetParameterValue("dat_s", finder.dat_oper);
            rep.SetParameterValue("dat_po", finder.dat_oper_po);
            int number_date, number_area, number_bank, number_payer, number_serv, number_dom;
            if (!Utils.GetParams(finder.groupby, STCLINE.KP50.Global.Constants.act_groupby_date.ToString(), out number_date)) number_date = -1;
            if (!Utils.GetParams(finder.groupby, STCLINE.KP50.Global.Constants.act_groupby_area.ToString(), out number_area)) number_area = -1;
            if (!Utils.GetParams(finder.groupby, STCLINE.KP50.Global.Constants.act_groupby_bank.ToString(), out number_bank)) number_bank = -1;
            if (!Utils.GetParams(finder.groupby, STCLINE.KP50.Global.Constants.act_groupby_payer.ToString(), out number_payer)) number_payer = -1;
            if (!Utils.GetParams(finder.groupby, STCLINE.KP50.Global.Constants.act_groupby_service.ToString(), out number_serv)) number_serv = -1;
            if (!Utils.GetParams(finder.groupby, STCLINE.KP50.Global.Constants.act_groupby_dom.ToString(), out number_dom)) number_dom = -1;
            rep.SetParameterValue("number_date", number_date);
            rep.SetParameterValue("number_area", number_area);
            rep.SetParameterValue("number_bank", number_bank);
            rep.SetParameterValue("number_payer", number_payer);
            rep.SetParameterValue("number_serv", number_serv);
            rep.SetParameterValue("number_dom", number_dom);
            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован Сальдо по перечислениям";
                ret.tag = rep.Report.PreparedPages.Count;

                /*  fileName =/* STCLINE.KP50.Global.Constants.ExcelDir +*/
                /*    "saldopoperechisl" + finder.dat_oper + "_" + finder.dat_oper_po + ".fpx";
                rep.SavePrepared(fileName);*/

                var dir = "";
                if (InputOutput.useFtp) dir = InputOutput.GetOutputDir();
                else dir = STCLINE.KP50.Global.Constants.ExcelDir;

                fileName = "saldopoperechisl" + finder.dat_oper + "_" + finder.dat_oper_po + ".fpx";
                string filePath = dir + fileName;
                rep.SavePrepared(filePath);

                if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(Path.Combine(dir, filePath));
                /*    FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                    export_xls.ShowProgress = false;
                    export_xls.Export(rep, fileName);*/



                ret.text = fileName;
                ret.result = true;
                return ret;

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "";
                MonitorLog.WriteLog("Ошибка формирования отчета Сальдо по перечислениям " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            return ret;
        }

        //Отчет: для военкомата для пасаортистки
        public Returns GetVoenkomat(Kart finder, ref string fileName) {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetChoosenData(finder, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "voenkomat.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            //fDataSet.DataSetName = "Q_master";
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            //установка параметров отчета
            rep.SetParameterValue("year_gr", finder.dat_rog);
            rep.SetParameterValue("cur_year", finder.dat_pvu);
            rep.SetParameterValue("naim_organ", finder.namereg);

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован Список для военкомата";
                ret.tag = rep.Report.PreparedPages.Count;

                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    "voenkomat" + finder.dat_rog + "_" + finder.dat_pvu + ".xls";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован Список для военкомата";
                MonitorLog.WriteLog("Ошибка формирования отчета Список для военкомата" +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }

        //Отчет: Ведомость должников для энергосбыта 
        public void GetDolgSpisEnergo(object container) {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer) container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"50.2 Ведомость должников\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetDolgSpisEnergo(cont.listprm[0], ref fileName);
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

        //Отчет:  Ведомость должников для энергосбыта 
        public Returns GetDolgSpisEnergo(Prm prm, ref string fileName) {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDolgSpisEnergo(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "web_325.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;

            //установка параметров отчета
            rep.SetParameterValue("month", prm.month_.ToString("00"));
            rep.SetParameterValue("year", prm.year_);
            rep.SetParameterValue("date", DateTime.Now.ToString("dd.MM.yyyy"));
            rep.SetParameterValue("services", prm.service);

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован 50.2 Ведомость должников";
                ret.tag = rep.Report.PreparedPages.Count;


                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    "50_2_Ved_dolg_" + prm.month_.ToString("00") + ".20" + prm.year_.ToString("00") + "_" + prm.nzp_serv + ".xls";
                FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();

                export_xls.ShowProgress = false;
                export_xls.Export(rep, fileName);

                ret.result = true;
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Отчет не сформирован 50.2 Ведомость должников";
                MonitorLog.WriteLog("Ошибка формирования отчета 50.2 Ведомость должников " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }



        //Отчет: протокол сверки данных
        public void GetProtocolSverData(object container) {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer) container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"50.2 Протокол сверки данных\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetProtocolSverData(cont.Finder, ref fileName);
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

        //Отчет: протокол сверки данных 
        public Returns GetProtocolSverData(Finder finder, ref string fileName) {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            DataSet fDataSet = ExR.GetProtocolSverData(finder, out ret);
            if (fDataSet != null && ret.result)
            {
                if (fDataSet.Tables.Count <= 0)
                {
                    ret.result = false;
                    return ret;
                }
                else
                {
                    foreach (System.Data.DataTable table in fDataSet.Tables)
                    {
                        int rowsCount = table.Rows.Count;
                        int index = 0;
                        for (int k = 0; k < rowsCount; k++)
                        {
                            bool equal = true;
                            for (int i = 2; i < table.Columns.Count - 1; i += 2)
                            {
                                if (table.Rows[index][i].ToString().Trim() != table.Rows[index][i + 1].ToString().Trim())
                                {
                                    equal = false;
                                    break;
                                }
                            }

                            if (equal)
                            {
                                table.Rows.RemoveAt(index);
                            }
                            else
                            {
                                index++;
                            }
                        }

                    }
                }

                FastReport.Report rep = new Report();
                string template = "web_326.frx";
                rep.Load(PathHelper.GetReportTemplatePath(template));
                rep.RegisterData(fDataSet);
                rep.GetDataSource("Q_master1").Enabled = true;
                rep.GetDataSource("Q_master2").Enabled = true;

                try
                {
                    rep.Prepare();
                    ret.text = "Отчет сформирован: протокол сверки данных";
                    ret.tag = rep.Report.PreparedPages.Count;

                    //вышрузка в fpx файл
                    /*fileName = STCLINE.KP50.Global.Constants.ExcelDir + 
                        "50_2_Протокол_сверки_данных_" + DateTime.Now.ToShortDateString().Replace(".", "_") +
                        DateTime.Now.ToShortTimeString().Replace(":", "_") + ".fpx";
                    rep.SavePrepared(fileName);*/

                    //выгрузка в excel
                    FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();
                    fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                        "Протокол_сверки_данных_" + DateTime.Now.ToShortDateString().Replace(".", "_") +
                        DateTime.Now.ToShortTimeString().Replace(":", "_") + ".xls";
                    export_xls.ShowProgress = false;
                    export_xls.Export(rep, fileName);

                    ret.result = true;
                    return ret;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Отчет не сформирован: протокол сверки данных";
                    MonitorLog.WriteLog("Ошибка формирования отчета: протокол сверки данных" +
                        ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                rep.Dispose();
            }
            return ret;
        }

        //Отчет: протокол сверки данных лицевых счетов и домов
        public void GetProtocolSverDataLsDom(object container) {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer) container;
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"50.2 Протокол сверки данных по лицевым счетам и домам\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetProtocolSverDataLsDom(cont.listprm[0], ref fileName);
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

        //Отчет: протокол сверки данных лицевых счетов и домов 
        public Returns GetProtocolSverDataLsDom(Prm finder, ref string fileName) {
            //ReportSverType type = ReportSverType.Dom;

            //if (finder.typek == 1)
            //{
            //    type = ReportSverType.Ls;
            //}
            //else
            //{
            //    if (finder.typek == 2)
            //        type = ReportSverType.Dom;
            //    else
            //        type = ReportSverType.Service;
            //}

            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable table = ExR.GetProtocolSverDataLsDom(finder, out ret);
            if (table != null && ret.result)
            {
                if (finder.prms == "show_differences")
                {
                    int indexes;
                    if (table.TableName == "Q_master1") indexes = 2;
                    else if (table.TableName == "Q_master2") indexes = 1;
                    else indexes = 3;

                    int rowsCount = table.Rows.Count;
                    for (int k = rowsCount - 1; k >= 0; k--)
                    {
                        bool equal = true;
                        for (int i = indexes; i < table.Columns.Count - 1; i += 2)
                        {
                            if (table.Rows[k][i].ToString().Trim() != table.Rows[k][i + 1].ToString().Trim())
                            {
                                equal = false;
                                break;
                            }
                        }

                        if (equal)
                        {
                            table.Rows.RemoveAt(k);
                        }
                    }
                }

                FastReport.Report rep = new Report();
                string template_name = "";
                string template = "";
                if (table.TableName == "Q_master1")
                {
                    template = "web_327_1.frx";
                    template_name = "лиц_счетов";
                }
                else
                {
                    if (table.TableName == "Q_master2")
                    {
                        template = "web_327_2.frx";
                        template_name = "домов";
                    }
                    else
                    {
                        template = "web_327_3.frx";
                        template_name = "услуг";
                    }
                }
                rep.Load(PathHelper.GetReportTemplatePath(template));
                DataSet fDataSet = new DataSet();
                fDataSet.Tables.Add(table);
                rep.RegisterData(fDataSet);
                rep.GetDataSource(table.TableName).Enabled = true;

                rep.SetParameterValue("month", finder.month_);
                rep.SetParameterValue("year", "20" + finder.year_.ToString());
                rep.SetParameterValue("area", finder.area);
                rep.SetParameterValue("showDif", finder.prms == "show_differences");

                try
                {
                    rep.Prepare();
                    ret.text = "Отчет сформирован: протокол сверки данных лицевых счетов и домов";
                    ret.tag = rep.Report.PreparedPages.Count;

                    //вышрузка в fpx файл
                    /*fileName = STCLINE.KP50.Global.Constants.ExcelDir + 
                        "50_2_Протокол_сверки_данных_" + DateTime.Now.ToShortDateString().Replace(".", "_") +
                        DateTime.Now.ToShortTimeString().Replace(":", "_") + ".fpx";
                    rep.SavePrepared(fileName);*/


                    fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                        "Протокол_сверки_данных_" + template_name + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xls";

                    //rep.SavePrepared(fileName);

                    //выгрузка в excel
                    FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();
                    export_xls.ShowProgress = false;
                    export_xls.Export(rep, fileName);

                    ret.result = true;
                    return ret;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Отчет не сформирован: протокол сверки данных лицевых счетов, домов";
                    MonitorLog.WriteLog("Ошибка формирования отчета: протокол сверки данных по лицевым счетам и домам" +
                        ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                rep.Dispose();
            }
            return ret;
        }


        //соглашение
        public Returns GetAgreement(Agreement finder, ReportType type) {
            string fileName = "";
            string repName = "";
            string TempDir = "";
            Returns ret = Utils.InitReturns();
            ExcelRepClient excelRepDb = new ExcelRepClient();
            //bool ifdeal = false;


            TempDir = STCLINE.KP50.Global.Constants.ExcelDir;
            //ifdeal = true;
            repName = "Соглашения о рестуктуризации задолженности по оплате жилищно-коммунальных услуг №" + finder.nzp_agr;



            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = repName
            });
            int nzpExc = ret.tag;

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            List<string> resultfacturaName = new List<string>();
            System.Data.DataTable DTT = ExR.GetAgreementTable(finder, out ret);
            if (DTT == null)
            {
                ret.result = false;
                return ret;
            }

            System.Data.DataTable DTD = ExR.GetAgreementData(finder, out ret);
            if (DTD == null)
            {
                ret.result = false;
                return ret;
            }


            int index = 0;

            DataSet fDataSetT = new DataSet();
            fDataSetT.Tables.Add(DTT);

            DataSet fDataSetD = new DataSet();
            fDataSetD.Tables.Add(DTD);

            FastReport.Report rep = new Report();
            string template = "soglashenie.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            foreach (DataRow row in DTD.Rows)
            {

                rep.SetParameterValue("uk_town", row["uktown"].ToString().Trim());
                rep.SetParameterValue("uk_ul", row["street"].ToString().Trim());
                rep.SetParameterValue("uk_dom", row["dom"].ToString().Trim());
                rep.SetParameterValue("adr", row["adr"].ToString().Trim());
                rep.SetParameterValue("town", row["town"].ToString().Trim());
                rep.SetParameterValue("num", row["nomer"]);
                rep.SetParameterValue("serij", row["serij"]);
                rep.SetParameterValue("vid_mes", row["vid_mes"].ToString().Trim());
                //rep.SetParameterValue("datemonth", row["datemonth"].ToString().Trim());
                //rep.SetParameterValue("imcoming_balance", row["imcoming_balance"].ToString().Trim());
                rep.SetParameterValue("vid_mes", row["vid_mes"].ToString().Trim());
                rep.SetParameterValue("vid_mes", row["vid_mes"].ToString().Trim());
                rep.SetParameterValue("debt_money", row["debt_money"]);
                rep.SetParameterValue("agr_date", row["agr_date"]);
                //  rep.SetParameterValue("agr_m_date", Convert.ToDateTime(row["agr_date"])+Convert.ToDateTime(row["agr_month_count"]));
            }



            rep.RegisterData(fDataSetT);
            rep.GetDataSource("Q_master").Enabled = true;


            //     FastReport.Report rep = new Report();
            //   string template = "soglashenie.frx";

            //  rep.Load(System.IO.Directory.GetCurrentDirectory() + @"\Template\" + template);



            try
            {
                rep.Prepare();
                ret.text = "Соглашение сформировано";
                ret.tag = rep.Report.PreparedPages.Count;

                switch (type)
                {
                    case ReportType.Pdf:
                        fileName = Path.Combine(TempDir, "Соглашение" + (++index) + ".pdf");
                        FastReport.Export.Pdf.PDFExport export_pdf = new FastReport.Export.Pdf.PDFExport();
                        export_pdf.Compressed = false;
                        export_pdf.ShowProgress = false;
                        export_pdf.Export(rep, fileName);
                        break;
                    case ReportType.Excel:
                        fileName = Path.Combine(TempDir, "Соглашение" + (++index) + ".xls");
                        FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();
                        export_xls.ShowProgress = false;
                        export_xls.Export(rep, fileName);
                        break;
                    case ReportType.Doc:
                        fileName = Path.Combine(TempDir, "Соглашение" + (++index) + ".doc");
                        FastReport.Export.OoXML.Word2007Export export_doc = new FastReport.Export.OoXML.Word2007Export();
                        export_doc.ShowProgress = false;
                        export_doc.Export(rep, fileName);
                        break;
                    case ReportType.Jpeg:
                        fileName = Path.Combine(TempDir, "Соглашение" + (++index) + ".jpeg");
                        FastReport.Export.Image.ImageExport export_image = new FastReport.Export.Image.ImageExport();
                        export_image.ShowProgress = false;
                        export_image.Export(rep, fileName);
                        break;
                    case ReportType.Rtf:
                        fileName = Path.Combine(TempDir, "Соглашение" + (++index) + ".rtf");
                        FastReport.Export.RichText.RTFExport export_rtf = new FastReport.Export.RichText.RTFExport();
                        export_rtf.ShowProgress = false;
                        export_rtf.Export(rep, fileName);
                        break;
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Соглашение не сформировано ";
                MonitorLog.WriteLog("Ошибка формирования соглашения " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            return ret;
        }

        //Отчет: Напоминание об оплате просроченной задолжности
        public Returns GetReminderToDebitor(Deal finder, ReportType type) {

            string fileName = "";
            Returns ret;

            var resultfacturaName = new List<string>();

            var exR = new ExcelRep();
            System.Data.DataTable dt = exR.GetNoticeToDebitor(finder, out ret);
            {
                if (dt == null || dt.Rows.Count <= 0)
                {
                    ret.tag = -2;
                    ret.result = false;
                    return ret;
                }

                var excelRepDb = new ExcelRepClient();
                ret = excelRepDb.AddMyFile(new ExcelUtility
                {
                    nzp_user = finder.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Печать напоминаний"
                });
                int nzpExc = ret.tag;

                bool ifdeal = false;
                string tempDir;
                if (finder.nzp_deal != 0)
                {
                    tempDir = Global.Constants.ExcelDir;
                    ifdeal = true;
                }
                else
                {

                    tempDir = Path.Combine(Path.GetTempPath(), "group_" + finder.nzp_group + "_" + finder.nzp_user);

                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                    Directory.CreateDirectory(tempDir);

                }

                int index = 0;
                if ((dt.Rows.Count == 0))
                {
                    #region
                    var rep = new Report();
                    const string template = "Get_reminder_to_debtor.frx";
                    rep.Load(Directory.GetCurrentDirectory() + @"\Template\" + template);

                    rep.SetParameterValue("adress", "                     ");
                    rep.SetParameterValue("area", "                     ");
                    rep.SetParameterValue("date", DateTime.Now.ToShortDateString());
                    rep.SetParameterValue("dolg", "                     ");
                    rep.SetParameterValue("gkh", "                     ");
                    rep.SetParameterValue("ul", "                     ");
                    rep.SetParameterValue("dom", "   ");
                    rep.SetParameterValue("kvnum", "   ");
                    rep.SetParameterValue("tel", "                     ");
                    rep.SetParameterValue("director", "                     ");
                    rep.SetParameterValue("fio", "                     ");

                    try
                    {
                        rep.Prepare();
                        ret.text = "Отчет сформирован Напоминание об оплате";
                        ret.tag = rep.Report.PreparedPages.Count;
                        string name = "Napominanie_Ob_oplate_" +
                                      DateTime.Now.GetHashCode();
                        switch (type)
                        {
                            case ReportType.Pdf:
                                fileName = Path.Combine(tempDir, name + ".pdf");
                                var exportPDF = new FastReport.Export.Pdf.PDFExport
                                {
                                    Compressed = false,
                                    ShowProgress = false
                                };
                                exportPDF.Export(rep, fileName);
                                break;
                            case ReportType.Excel:
                                fileName = Path.Combine(tempDir, name + ".xls");
                                var exportXLS = new FastReport.Export.OoXML.Excel2007Export { ShowProgress = false };
                                exportXLS.Export(rep, fileName);
                                break;
                            case ReportType.Doc:
                                fileName = Path.Combine(tempDir, name + ".doc");
                                var exportDoc = new FastReport.Export.OoXML.Word2007Export { ShowProgress = false };
                                exportDoc.Export(rep, fileName);
                                break;
                            case ReportType.Jpeg:
                                fileName = Path.Combine(tempDir, name + ".jpeg");
                                var exportImage = new FastReport.Export.Image.ImageExport { ShowProgress = false };
                                exportImage.Export(rep, fileName);
                                break;
                            case ReportType.Rtf:
                                fileName = Path.Combine(tempDir, name + ".rtf");
                                var exportRTF = new FastReport.Export.RichText.RTFExport { ShowProgress = false };
                                exportRTF.Export(rep, fileName);
                                break;  
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Напоминания об оплате не сформирован ";
                        MonitorLog.WriteLog("Ошибка формирования напоминаний об оплате " +
                            ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    }
                    #endregion
                }
                else
                {
                    #region для нескольких

                    foreach (DataRow row in dt.Rows)
                    {
                        var rep = new Report();
                        const string template = "Get_reminder_to_debtor.frx";
                        rep.Load(Directory.GetCurrentDirectory() + @"\Template\" + template);

                        rep.SetParameterValue("adress", row["adr"]);
                        rep.SetParameterValue("area", row["area"].ToString().Trim());
                        rep.SetParameterValue("date", DateTime.Now.ToShortDateString());
                        rep.SetParameterValue("dolg", row["debt_money"]);
                        rep.SetParameterValue("gkh", row["area"].ToString().Trim());
                        rep.SetParameterValue("ul", row["street"].ToString().Trim());
                        rep.SetParameterValue("dom", row["dom"].ToString().Trim());
                        rep.SetParameterValue("kvnum", row["kvnum"].ToString().Trim());
                        rep.SetParameterValue("tel", row["phone"].ToString().Trim());
                        rep.SetParameterValue("director", row["fio_dir"]);
                        rep.SetParameterValue("fio", row["fio"]);

                        try
                        {
                            rep.Prepare();
                            ret.text = "Отчет сформирован Напоминание об оплате";
                            ret.tag = rep.Report.PreparedPages.Count;
                            string name = "Napominanie_Ob_oplate_" +
                                          DateTime.Now.GetHashCode();
                            switch (type)
                            {
                                case ReportType.Pdf:
                                    fileName = Path.Combine(tempDir, name + ".pdf");
                                    var exportPDF = new FastReport.Export.Pdf.PDFExport
                                    {
                                        Compressed = false,
                                        ShowProgress = false
                                    };
                                    exportPDF.Export(rep, fileName);
                                    break;
                                case ReportType.Excel:
                                    fileName = Path.Combine(tempDir, name + ".xls");
                                    var exportXLS = new FastReport.Export.OoXML.Excel2007Export { ShowProgress = false };
                                    exportXLS.Export(rep, fileName);
                                    break;
                                case ReportType.Doc:
                                    fileName = Path.Combine(tempDir, name + ".doc");
                                    var exportDoc = new FastReport.Export.OoXML.Word2007Export { ShowProgress = false };
                                    exportDoc.Export(rep, fileName);
                                    break;
                                case ReportType.Jpeg:
                                    fileName = Path.Combine(tempDir, name + ".jpeg");
                                    var exportImage = new FastReport.Export.Image.ImageExport { ShowProgress = false };
                                    exportImage.Export(rep, fileName);
                                    break;
                                case ReportType.Rtf:
                                    fileName = Path.Combine(tempDir, name + ".rtf");
                                    var exportRTF = new FastReport.Export.RichText.RTFExport { ShowProgress = false };
                                    exportRTF.Export(rep, fileName);
                                    break;

                            }
                            resultfacturaName.Add(fileName);
                        }
                        catch (Exception ex)
                        {
                            ret.result = false;
                            ret.text = "Напоминания об оплате не сформирован ";
                            MonitorLog.WriteLog("Ошибка формирования напоминаний об оплате " +
                                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        }
                    }
                }
                var dir = "";
                dir = InputOutput.useFtp ? InputOutput.GetOutputDir() : STCLINE.KP50.Global.Constants.ExcelDir;
                //zip
                if (!ifdeal)
                {

                    #region Архивация
                    try
                    {
                        string fileN = (DateTime.Now.ToShortDateString() + "_" +
                            DateTime.Now.ToShortTimeString()).Replace(":", "_").Replace(".", "_") +
                            Process.GetCurrentProcess().Id;

                        fileName = "Напоминания" + finder.nzp_group + "_" + fileN + ".zip";
                        if (resultfacturaName.Count != 0)
                            Archive.GetInstance().Compress(dir + @"\" + fileName, resultfacturaName.ToArray(), true);

                        if (finder.nzp_deal == 0)
                        {
                            tempDir = Path.Combine(Path.GetTempPath(), "group_" + finder.nzp_group + "_" + finder.nzp_user);

                            if (Directory.Exists(tempDir))
                            {
                                Directory.Delete(tempDir, true);
                            }
                        }

                         
                    }
                    catch (Exception ex)
                    {
                        ret.text = "Напоминания сформированы, но не могут быть добавлены в архив, возможно на сервере отсутствует архиватор ";
                        ret.result = false;
                        ret.tag = -1;
                        MonitorLog.WriteLog("Ошибка архивирования напоминаний " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                    #endregion
                }

                var debt = new Debitor();
                var find = new deal_states_history {nzp_user = finder.nzp_user};
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    find.nzp_deal = Convert.ToInt32(dt.Rows[i]["nzp_deal"]);
                    find.nzp_oper = EnumOpers.GiveNotice.GetHashCode();
                    debt.MakeOperOnDeal(find, out ret);
                }

                if (InputOutput.useFtp && File.Exists(Path.Combine(dir, fileName)))
                    fileName = InputOutput.SaveInputFile(Path.Combine(dir, fileName));
                else fileName = Path.Combine(dir, fileName);
                excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = fileName });

                ret.result = true;
                ret.text = fileName;
                    #endregion
                return ret;
            }
        }


        //public void GetNoticeToDebitor(object container)
        //{
        //    MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        //    ParamContainer cont = (ParamContainer)container;
        //    //Returns ret = GetNoticeToDebitor(cont.Deal, cont.reportType);
        //    string k = GetNoticeToDebitorPDF(cont.Deal, cont.reportType);
        //    MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        //}

        public Returns GetNoticeToDebitor(Deal finder, ReportType type) {
            string fileName = "";
            Returns ret;
            var resultfacturaName = new List<string>();
            //создание dataTable
            var exR = new ExcelRep();
            System.Data.DataTable dt = exR.GetNoticeToDebitor(finder, out ret);
            {
                if (dt == null || dt.Rows.Count <= 0)
                {
                    ret.tag = -2;
                    ret.result = false;
                    return ret;
                }

                var excelRepDb = new ExcelRepClient();
                ret = excelRepDb.AddMyFile(new ExcelUtility
                {
                    nzp_user = finder.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Печать уведомления"
                });
                int nzpExc = ret.tag;

                bool ifdeal = false;
                string tempDir;
                if (finder.nzp_deal != 0)
                {
                    tempDir = Global.Constants.ExcelDir;
                    ifdeal = true;
                }
                else
                {

                    tempDir = Path.Combine(Path.GetTempPath(), "group_" + finder.nzp_group + "_" + finder.nzp_user);

                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                    Directory.CreateDirectory(tempDir);

                }
                if ((dt.Rows.Count == 0))
                {
                    var rep = new Report();
                    const string template = "GetNoticeToDebitor.frx";
                    rep.Load(Directory.GetCurrentDirectory() + @"\Template\" + template);
                    rep.SetParameterValue("adress", "                       ");
                    rep.SetParameterValue("area", "                       ");
                    rep.SetParameterValue("date", DateTime.Now.ToShortDateString());
                    rep.SetParameterValue("dolg", "                       ");
                    rep.SetParameterValue("gkh", "                       ");
                    rep.SetParameterValue("ul", "                       ");
                    rep.SetParameterValue("dom", "   ");
                    rep.SetParameterValue("tel", "                       ");
                    rep.SetParameterValue("director", "                       ");
                    rep.SetParameterValue("fio", "                       ");
                    try
                    {
                        rep.Prepare();
                        ret.text = "Отчет сформирован уведомление об оплате";
                        ret.tag = rep.Report.PreparedPages.Count;
                        string name = "Uvedomlenie_" +
                                          DateTime.Now.GetHashCode(); 
                        #region тип отчета
                        switch (type)
                        {
                            case ReportType.Pdf:
                                fileName = Path.Combine(tempDir, name + ".pdf");
                                var exportPDF = new FastReport.Export.Pdf.PDFExport { ShowProgress = false, Compressed = false };
                                exportPDF.Export(rep, fileName);
                                break;
                            case ReportType.Excel:
                                fileName = Path.Combine(tempDir, name + ".xls");
                                var exportXLS = new FastReport.Export.OoXML.Excel2007Export { ShowProgress = false };
                                exportXLS.Export(rep, fileName);
                                break;
                            case ReportType.Doc:
                                fileName = Path.Combine(tempDir, name + ".doc");
                                var exportDoc = new FastReport.Export.OoXML.Word2007Export { ShowProgress = false };
                                exportDoc.Export(rep, fileName);
                                break;
                            case ReportType.Jpeg:
                                fileName = Path.Combine(tempDir, name + ".jpeg");
                                var exportImage = new FastReport.Export.Image.ImageExport { ShowProgress = false };
                                exportImage.Export(rep, fileName);
                                break;
                            case ReportType.Rtf:
                                fileName = Path.Combine(tempDir, name + ".rtf");
                                var exportRTF = new FastReport.Export.RichText.RTFExport { ShowProgress = false };
                                exportRTF.Export(rep, fileName);
                                break;
                        }
                        #endregion


                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "уведомление не сформировано ";
                        MonitorLog.WriteLog("Ошибка формирования отчета уведомление " +
                            ex.Message, MonitorLog.typelog.Error, 20, 201, true);

                    }

                }
                else
                {
                    #region


                    foreach (DataRow row in dt.Rows)
                    {
                        var rep = new Report();
                        const string template = "GetNoticeToDebitor.frx";
                        rep.Load(Directory.GetCurrentDirectory() + @"\Template\" + template);

                        rep.SetParameterValue("adress", row["adr"]);
                        rep.SetParameterValue("area", row["area"].ToString().Trim());
                        rep.SetParameterValue("date", DateTime.Now.ToShortDateString());
                        rep.SetParameterValue("dolg", row["debt_money"]);
                        rep.SetParameterValue("gkh", row["area"].ToString().Trim());
                        rep.SetParameterValue("ul", row["street"].ToString().Trim());
                        rep.SetParameterValue("dom", row["dom"].ToString().Trim());
                        rep.SetParameterValue("tel", row["phone"].ToString().Trim());
                        rep.SetParameterValue("director", row["fio_dir"]);
                        rep.SetParameterValue("fio", row["fio"]);

                        try
                        {
                            rep.Prepare();
                            ret.text = "Отчет сформирован уведомление об оплате";
                            ret.tag = rep.Report.PreparedPages.Count;
                            string name = "Uvedomlenie_" +
                                          DateTime.Now.GetHashCode();
                            #region тип отчета
                            switch (type)
                            {
                                case ReportType.Pdf:
                                    fileName = Path.Combine(tempDir, name + ".pdf");
                                    var exportPDF = new FastReport.Export.Pdf.PDFExport { ShowProgress = false, Compressed = false };
                                    exportPDF.Export(rep, fileName);
                                    break;
                                case ReportType.Excel:
                                    fileName = Path.Combine(tempDir, name + ".xls");
                                    var exportXLS = new FastReport.Export.OoXML.Excel2007Export { ShowProgress = false };
                                    exportXLS.Export(rep, fileName);
                                    break;
                                case ReportType.Doc:
                                    fileName = Path.Combine(tempDir, name + ".doc");
                                    var exportDoc = new FastReport.Export.OoXML.Word2007Export { ShowProgress = false };
                                    exportDoc.Export(rep, fileName);
                                    break;
                                case ReportType.Jpeg:
                                    fileName = Path.Combine(tempDir, name + ".jpeg");
                                    var exportImage = new FastReport.Export.Image.ImageExport { ShowProgress = false };
                                    exportImage.Export(rep, fileName);
                                    break;
                                case ReportType.Rtf:
                                    fileName = Path.Combine(tempDir, name + ".rtf");
                                    var exportRTF = new FastReport.Export.RichText.RTFExport { ShowProgress = false };
                                    exportRTF.Export(rep, fileName);
                                    break;
                            }
                            resultfacturaName.Add(fileName);
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            ret.result = false;
                            ret.text = "уведомление не сформировано ";
                            MonitorLog.WriteLog("Ошибка формирования отчета уведомление " +
                                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        }
                    }
                }
                var dir = "";
                dir = InputOutput.useFtp ? InputOutput.GetOutputDir() : STCLINE.KP50.Global.Constants.ExcelDir;
                //zip
                if (!ifdeal)
                {
                    #region Архивация
                    try
                    {
                        string fileN = (DateTime.Now.ToShortDateString() + "_" +
                            DateTime.Now.ToShortTimeString()).Replace(":", "_").Replace(".", "_") +
                            Process.GetCurrentProcess().Id;

                        fileName = "Уведомления" + finder.nzp_group + "_" + fileN + ".zip";
                        if (resultfacturaName.Count != 0)
                            Archive.GetInstance().Compress(dir + @"\" + fileName, resultfacturaName.ToArray(), true);

                        if (finder.nzp_deal == 0)
                        {
                            tempDir = Path.Combine(Path.GetTempPath(), "group_" + finder.nzp_group + "_" + finder.nzp_user);
                            if (Directory.Exists(tempDir))
                            {
                                Directory.Delete(tempDir, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.text = "Уведомления сформированы сформированы, но не могут быть добавлены в архив, возможно на сервере отсутствует архиватор";
                        ret.result = false;
                        ret.tag = -1;
                        MonitorLog.WriteLog("Ошибка архивирования счета " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                    #endregion
                }
                var debt = new Debitor();
                var find = new deal_states_history {nzp_user = finder.nzp_user};
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    find.nzp_deal = Convert.ToInt32(dt.Rows[i]["nzp_deal"]);
                    find.nzp_oper = EnumOpers.GiveNotification.GetHashCode();
                    debt.MakeOperOnDeal(find, out ret);
                }

                    #endregion

                if (InputOutput.useFtp && File.Exists(Path.Combine(dir, fileName)))
                    fileName = InputOutput.SaveInputFile(Path.Combine(dir, fileName));
                else fileName = Path.Combine(dir, fileName);
                excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = fileName });

                ret.result = true;
                ret.text = fileName;
                return ret;
            }
        }

        ///  <summary>Получить отчет в виде потока данных в формате PDF</summary>
        /// <param name="typeBlank">Тип бланка(напоминание,уведомление, предупреждение)</param>
        /// <param name="finder">Входной параметр с дополнительно информации</param>
        /// <param name="typeFormat">Тип формата</param>
        /// <param name="ret">Параметр - статус работы функции</param>
        public string GetBlankPDF(Deal finder, ReportType typeFormat, int typeBlank, out Returns ret) {
            ret = Utils.InitReturns();
            Utils.setCulture();
            string destinationFilename = string.Empty;
            var ms = new MemoryStream { Position = 0 };
            try
            {
                switch (typeBlank)
                {
                    case 1:
                        ret = GetNoticeToDebitor(finder, typeFormat);
                        break;
                    case 2:
                        ret = GetReminderToDebitor(finder, typeFormat);
                        break;
                    case 3:
                        ret = GetWarningToDebitor(finder, typeFormat);
                        break;
                }

                if (!ret.result) throw new Exception(ret.text);

                string localFileName = Path.Combine(Global.Constants.Directories.ReportDir, ret.text);
                if (localFileName.LastIndexOf('/') >= 0)
                {
                    string directory = localFileName.Remove(localFileName.LastIndexOf('/'));
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                }
                if (InputOutput.useFtp)
                    if (!InputOutput.DownloadFile(ret.text, localFileName))
                        throw new Exception("Не удалось загрузить файл с сервера");
                if (typeFormat == ReportType.Pdf)
                {
                    var fs = new FileStream(localFileName, FileMode.Open, FileAccess.Read) {Position = 0};
                    while (fs.Position <= fs.Length - 1)
                    {
                        ms.WriteByte((byte) fs.ReadByte());
                    }
                    destinationFilename = Convert.ToBase64String(ms.ToArray());
                    fs.Close();
                }
            }
            catch (Exception ex)
            {

                MonitorLog.WriteLog("Ошибка функции GetBlankPDF.\n" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ret.tag == -2 ? "Список дел пуст" : "Ошибка формирования бланка";
                ret.result = false;
            }
            finally
            {
                ms.Close();
            }
            return destinationFilename;
        }

        //Отчет: Предупреждение об оплате просроченной задолжности
        public Returns GetWarningToDebitor(Deal finder, ReportType type) {

            string fileName = "";
            Returns ret;

            var resultfacturaName = new List<string>();
            //создание dataTable
            var exR = new ExcelRep();
            System.Data.DataTable dt = exR.GetNoticeToDebitor(finder, out ret);
            {
                if (dt == null || dt.Rows.Count<=0)
                {
                    ret.tag = -2;
                    ret.result = false;
                    return ret;
                }

                var excelRepDb = new ExcelRepClient();
                ret = excelRepDb.AddMyFile(new ExcelUtility
                {
                    nzp_user = finder.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Печать Предупреждения"
                });
                int nzpExc = ret.tag;

                #region
                bool ifdeal = false;
                string tempDir;
                if (finder.nzp_deal != 0)
                {
                    tempDir = Global.Constants.ExcelDir;
                    ifdeal = true;
                }
                else
                {

                    tempDir = Path.Combine(Path.GetTempPath(), "group_" + finder.nzp_group + "_" + finder.nzp_user);

                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                    Directory.CreateDirectory(tempDir);

                }



                if ((dt.Rows.Count == 0))
                {
                    var rep = new Report();
                    const string template = "GetWarningToDebitor.frx";
                    rep.Load(Directory.GetCurrentDirectory() + @"\Template\" + template);

                    rep.SetParameterValue("adress", "                     ");
                    rep.SetParameterValue("area", "                     ");
                    rep.SetParameterValue("date", DateTime.Now.ToShortDateString());
                    rep.SetParameterValue("dolg", "                     ");
                    rep.SetParameterValue("gkh", "                     ");
                    rep.SetParameterValue("ul", "                     ");
                    rep.SetParameterValue("dom", "    ");
                    rep.SetParameterValue("tel", "                     ");
                    rep.SetParameterValue("director", "                     ");
                    rep.SetParameterValue("fio", "                     ");


                    try
                    {
                        rep.Prepare();
                        ret.text = "Предупреждение сформировано";
                        ret.tag = rep.Report.PreparedPages.Count;
                        string name = "Preduprjdenie_" +
                                       DateTime.Now.GetHashCode();
                        switch (type)
                        {
                            case ReportType.Pdf:
                                fileName = Path.Combine(tempDir, name + ".pdf");
                                var exportPDF = new FastReport.Export.Pdf.PDFExport { ShowProgress = false, Compressed = false };
                                exportPDF.Export(rep, fileName);
                                break;
                            case ReportType.Excel:
                                fileName = Path.Combine(tempDir, name + ".xls");
                                var exportXLS = new FastReport.Export.OoXML.Excel2007Export { ShowProgress = false };
                                exportXLS.Export(rep, fileName);
                                break;
                            case ReportType.Doc:
                                fileName = Path.Combine(tempDir, name + ".doc");
                                var exportDoc = new FastReport.Export.OoXML.Word2007Export { ShowProgress = false };
                                exportDoc.Export(rep, fileName);
                                break;
                            case ReportType.Jpeg:
                                fileName = Path.Combine(tempDir, name + ".jpeg");
                                var exportImage = new FastReport.Export.Image.ImageExport { ShowProgress = false };
                                exportImage.Export(rep, fileName);
                                break;
                            case ReportType.Rtf:
                                fileName = Path.Combine(tempDir, name + ".rtf");
                                var exportRTF = new FastReport.Export.RichText.RTFExport { ShowProgress = false };
                                exportRTF.Export(rep, fileName);
                                break;
                        }
                    }


                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Предупреждение  не сформировано ";
                        MonitorLog.WriteLog("Ошибка формирования отчета предупреждения " +
                            ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    }

                }
                else
                {

                    foreach (DataRow row in dt.Rows)
                    {
                        var rep = new Report();
                        const string template = "GetWarningToDebitor.frx";
                        rep.Load(Directory.GetCurrentDirectory() + @"\Template\" + template);

                        rep.SetParameterValue("adress", row["adr"]);
                        rep.SetParameterValue("area", row["area"].ToString().Trim());
                        rep.SetParameterValue("date", DateTime.Now.ToShortDateString());
                        rep.SetParameterValue("dolg", row["debt_money"]);
                        rep.SetParameterValue("gkh", row["area"].ToString().Trim());
                        rep.SetParameterValue("ul", row["street"].ToString().Trim());
                        rep.SetParameterValue("dom", row["dom"].ToString().Trim());
                        rep.SetParameterValue("tel", row["phone"].ToString().Trim());
                        rep.SetParameterValue("director", row["fio_dir"]);
                        rep.SetParameterValue("fio", row["fio"]);


                        try
                        {
                            rep.Prepare();
                            ret.text = "Предупреждение сформировано";
                            ret.tag = rep.Report.PreparedPages.Count;
                            string name = "Preduprjdenie_" +
                                           DateTime.Now.GetHashCode();
                            switch (type)
                            {
                                case ReportType.Pdf:
                                    fileName = Path.Combine(tempDir, name + ".pdf");
                                    var exportPDF = new FastReport.Export.Pdf.PDFExport { ShowProgress = false, Compressed = false };
                                    exportPDF.Export(rep, fileName);
                                    break;
                                case ReportType.Excel:
                                    fileName = Path.Combine(tempDir, name + ".xls");
                                    var exportXLS = new FastReport.Export.OoXML.Excel2007Export { ShowProgress = false };
                                    exportXLS.Export(rep, fileName);
                                    break;
                                case ReportType.Doc:
                                    fileName = Path.Combine(tempDir, name + ".doc");
                                    var exportDoc = new FastReport.Export.OoXML.Word2007Export { ShowProgress = false };
                                    exportDoc.Export(rep, fileName);
                                    break;
                                case ReportType.Jpeg:
                                    fileName = Path.Combine(tempDir, name + ".jpeg");
                                    var exportImage = new FastReport.Export.Image.ImageExport { ShowProgress = false };
                                    exportImage.Export(rep, fileName);
                                    break;
                                case ReportType.Rtf:
                                    fileName = Path.Combine(tempDir, name + ".rtf");
                                    var exportRTF = new FastReport.Export.RichText.RTFExport { ShowProgress = false };
                                    exportRTF.Export(rep, fileName);
                                    break;
                            }
                            resultfacturaName.Add(fileName);
                        }
                        catch (Exception ex)
                        {
                            ret.result = false;
                            ret.text = "Предупреждение  не сформировано ";
                            MonitorLog.WriteLog("Ошибка формирования отчета предупреждения " +
                                ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        }
                    }
                }
                var dir = "";
                dir = InputOutput.useFtp ? InputOutput.GetOutputDir() : STCLINE.KP50.Global.Constants.ExcelDir;

                if (!ifdeal)
                {
                    #region Архивация
                    try
                    {
                        string fileN = (DateTime.Now.ToShortDateString() + "_" +
                            DateTime.Now.ToShortTimeString()).Replace(":", "_").Replace(".", "_") +
                            Process.GetCurrentProcess().Id;

                        fileName = "Предупреждения" + finder.nzp_group + "_" + fileN + ".zip";
                        if (resultfacturaName.Count != 0)
                            Archive.GetInstance().Compress(dir + @"\" + fileName, resultfacturaName.ToArray(), true);

                        if (finder.nzp_deal == 0)
                        {
                            tempDir = Path.Combine(Path.GetTempPath(), "group_" + finder.nzp_group + "_" + finder.nzp_user);
                            if (Directory.Exists(tempDir))
                            {
                                Directory.Delete(tempDir, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.text = "Предупреждения сформированы сформированы, но не могут быть добавлены в архив, возможно на сервере отсутствует архиватор ";
                        ret.result = false;
                        ret.tag = -1;
                        MonitorLog.WriteLog("Ошибка архивирования Предупреждения " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    #endregion
                        return ret;
                    }
                #endregion
                }

                var debt = new Debitor();
                var find = new deal_states_history {nzp_user = finder.nzp_user};
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    find.nzp_deal = Convert.ToInt32(dt.Rows[i]["nzp_deal"]);
                    find.nzp_oper = EnumOpers.GiveWarning.GetHashCode();
                    debt.MakeOperOnDeal(find, out ret);
                }

                if (InputOutput.useFtp && File.Exists(Path.Combine(dir, fileName)))
                    fileName = InputOutput.SaveInputFile(Path.Combine(dir, fileName));
                else fileName = Path.Combine(dir, fileName);
                excelRepDb.SetMyFileState(new ExcelUtility { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = fileName });
      
                ret.result = true;
                ret.text = fileName;

                return ret;
            }
        }
        //Отчет: Исковое заявление 
        public Returns GetPetition(ReportType type) {
            Returns ret = Utils.InitReturns();
            ret = GetPetitionOrPrikaz(type, "pet");
            return ret;
        }
        //Отчет: Заявление о выдаче судебного приказа
        public Returns GetPrikaz(ReportType type) {
            Returns ret = Utils.InitReturns();
            ret = GetPetitionOrPrikaz(type, "prik");
            return ret;
        }

        //Отчет: Исковое заявление или о выдаче приказа 
        public Returns GetPetitionOrPrikaz(ReportType type, string kind) {
            Returns ret = Utils.InitReturns();
            string fileName = "";
            List<string> resultPetition = new List<string>();
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetPetition(out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            string TempDir = Path.Combine(Path.GetTempPath(), "group_gewg");
            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
            Directory.CreateDirectory(TempDir);
            int index = 0;


            foreach (DataRow row in DT.Rows)
            {
                FastReport.Report rep = new Report();
                string template = "";
                if (kind == "pet")
                {
                    template = "petition.frx";
                }
                else if (kind == "prik")
                {
                    template = "prikaz_sud.frx";
                }
                rep.Load(PathHelper.GetReportTemplatePath(template));

                rep.SetParameterValue("area", row["nzp_area"]);
                rep.SetParameterValue("dir", row["fio_dir"]);
                rep.SetParameterValue("street", row["street"]);
                rep.SetParameterValue("dom", row["dom"]);
                rep.SetParameterValue("kvnum", row["kvnum"]);
                rep.SetParameterValue("fio", row["fio"]);
                rep.SetParameterValue("ulica", row["ulica"]);
                rep.SetParameterValue("ndom", row["ndom"]);
                rep.SetParameterValue("nkvar", row["nkvar"]);
                rep.SetParameterValue("lawsuit_price", row["lawsuit_price"]);
                rep.SetParameterValue("tax", row["tax"]);
                rep.SetParameterValue("month", row["curmonth"]);
                rep.SetParameterValue("year", row["curyear"]);
                rep.SetParameterValue("rub", row["rub"]);
                rep.SetParameterValue("kop", row["kop"]);
                rep.SetParameterValue("presenter", row["presenter"]);
                rep.SetParameterValue("is_town", row["is_town"]);
                rep.SetParameterValue("town", row["town"]);

                #region выбор формата
                try
                {
                    rep.Prepare();
                    ret.text = "Отчет сформирован уведомление об оплате";
                    ret.tag = rep.Report.PreparedPages.Count;

                    switch (type)
                    {
                        case ReportType.Pdf:
                            if (kind == "pet")
                            {
                                fileName = Path.Combine(TempDir, "Petition_" + (++index) + ".pdf");
                            }
                            else if (kind == "prik")
                            {
                                fileName = Path.Combine(TempDir, "Prikaz_" + (++index) + ".pdf");
                            }
                            FastReport.Export.Pdf.PDFExport export_pdf = new FastReport.Export.Pdf.PDFExport();
                            export_pdf.ShowProgress = false;
                            export_pdf.Compressed = false;
                            export_pdf.Export(rep, fileName);
                            resultPetition.Add(fileName);

                            break;
                        case ReportType.Excel:
                            if (kind == "pet")
                            {
                                fileName = Path.Combine(TempDir, "Petition_" + (++index) + ".xls");
                            }
                            else if (kind == "prik")
                            {
                                fileName = Path.Combine(TempDir, "Prikaz_" + (++index) + ".xls");
                            }
                            FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();
                            export_xls.ShowProgress = false;
                            export_xls.Export(rep, fileName);
                            break;
                        case ReportType.Doc:
                            if (kind == "pet")
                            {
                                fileName = Path.Combine(TempDir, "Petition_" + (++index) + ".doc");
                            }
                            else if (kind == "prik")
                            {
                                fileName = Path.Combine(TempDir, "Prikaz_" + (++index) + ".doc");
                            }
                            FastReport.Export.OoXML.Word2007Export export_doc = new FastReport.Export.OoXML.Word2007Export();
                            export_doc.ShowProgress = false;
                            export_doc.Export(rep, fileName);
                            break;
                        case ReportType.Jpeg:
                            if (kind == "pet")
                            {
                                fileName = Path.Combine(TempDir, "Petition_" + (++index) + ".jpeg");
                            }
                            else if (kind == "prik")
                            {
                                fileName = Path.Combine(TempDir, "Prikaz_" + (++index) + ".jpeg");
                            }
                            FastReport.Export.Image.ImageExport export_image = new FastReport.Export.Image.ImageExport();
                            export_image.ShowProgress = false;
                            export_image.Export(rep, fileName);
                            break;
                        case ReportType.Rtf:
                            if (kind == "pet")
                            {
                                fileName = Path.Combine(TempDir, "Petition_" + (++index) + ".rtf");
                            }
                            else if (kind == "prik")
                            {
                                fileName = Path.Combine(TempDir, "Prikaz_" + (++index) + ".rtf");
                            }
                            FastReport.Export.RichText.RTFExport export_rtf = new FastReport.Export.RichText.RTFExport();
                            export_rtf.ShowProgress = false;
                            export_rtf.Export(rep, fileName);
                            break;
                    }

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Исковое заявление/приказ не сформировано ";
                    MonitorLog.WriteLog("Ошибка формирования отчета Исковое заявление/Приказ " +
                        ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                #endregion
            }



            #region Архивация
            try
            {
                string file_n = (DateTime.Now.ToShortDateString() + "_" +
                    DateTime.Now.ToShortTimeString()).Replace(":", "_").Replace(".", "_") +
                    Process.GetCurrentProcess().Id.ToString();

                fileName = "Исковые заявления_" + file_n + ".zip";
                if (resultPetition.Count != 0)
                    Archive.GetInstance().Compress(Global.Constants.ExcelDir + @"\" + fileName, resultPetition.ToArray(), true);
            }
            catch (Exception ex)
            {
                ret.text = "Уведомления сформированы сформированы, но не могут быть добавлены в архив, возможно на сервере отсутствует архиватор ";
                ret.result = false;
                ret.tag = -1;
                MonitorLog.WriteLog("Ошибка архивирования счета " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            #endregion
            //ExcelRepClient excelRepDb = new ExcelRepClient();
            //ret = excelRepDb.AddMyFile(new ExcelUtility()
            //{
            //    nzp_user = user,
            //    status = ExcelUtility.Statuses.Success,
            //    rep_name = "Печать искового заявления:" + area
            //});
            //int nzpExc = ret.tag;
            //excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = STCLINE.KP50.Global.Constants.ExcelDir + @"\" + fileName });

            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
            ret.result = true;
            ret.text = fileName;
            return ret;
        }



        //Отчет: Список соглашений должников 
        public Returns GetListOfAgreemants(DateTime? dat_s, DateTime? dat_po, int user, int area, ReportType type) {
            Returns ret = Utils.InitReturns();
            string fileName = "";
            List<string> resultPetition = new List<string>();
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetListOfAgreemants(dat_s, dat_po, user, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            string TempDir = Path.Combine(Path.GetTempPath(), "group_tqet4");
            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
            Directory.CreateDirectory(TempDir);
            int index = 0;

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "AllAgreement.frx";
            rep.Load(PathHelper.GetReportTemplatePath(template));
            rep.RegisterData(fDataSet);
            rep.GetDataSource("Q_master").Enabled = true;

            #region выбор формата
            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован уведомление об оплате";
                ret.tag = rep.Report.PreparedPages.Count;

                fileName = string.Format("{0}\\AllAgriments_{1}{2}_{3}.{4}", (InputOutput.useFtp) ? "reports" : STCLINE.KP50.Global.Constants.ExcelDir,
                    ++index,
                    DateTime.Now.ToShortDateString(),
                    DateTime.Now.ToShortDateString().Replace(":", "_").Replace(".", "_"),
                    Enum.GetName(typeof(ReportType), type).ToLower());
                // "reports\\AllAgriments_" + (++index) + DateTime.Now.ToShortDateString() + "_" +
                //       (DateTime.Now.ToShortTimeString()).Replace(":", "_").Replace(".", "_") + Enum.GetName(typeof(ReportType),type).ToLower();
                switch (type)
                {
                    case ReportType.Pdf:
                        FastReport.Export.Pdf.PDFExport export_pdf = new FastReport.Export.Pdf.PDFExport();
                        export_pdf.ShowProgress = false;
                        export_pdf.Compressed = false;
                        export_pdf.Export(rep, Path.Combine(STCLINE.KP50.Global.Constants.FilesDir, fileName));
                        resultPetition.Add(fileName);
                        break;
                    case ReportType.Excel:
                        FastReport.Export.OoXML.Excel2007Export export_xls = new FastReport.Export.OoXML.Excel2007Export();
                        export_xls.ShowProgress = false;
                        export_xls.Export(rep, Path.Combine(STCLINE.KP50.Global.Constants.FilesDir, fileName));
                        break;
                    case ReportType.Doc:
                        FastReport.Export.OoXML.Word2007Export export_doc = new FastReport.Export.OoXML.Word2007Export();
                        export_doc.ShowProgress = false;
                        export_doc.Export(rep, Path.Combine(STCLINE.KP50.Global.Constants.FilesDir, fileName));
                        break;
                    case ReportType.Jpeg:
                        FastReport.Export.Image.ImageExport export_image = new FastReport.Export.Image.ImageExport();
                        export_image.ShowProgress = false;
                        export_image.Export(rep, Path.Combine(STCLINE.KP50.Global.Constants.FilesDir, fileName));
                        break;
                    case ReportType.Rtf:
                        FastReport.Export.RichText.RTFExport export_rtf = new FastReport.Export.RichText.RTFExport();
                        export_rtf.ShowProgress = false;
                        export_rtf.Export(rep, Path.Combine(STCLINE.KP50.Global.Constants.FilesDir, fileName));
                        break;
                }
                if (InputOutput.useFtp && File.Exists(Path.Combine(STCLINE.KP50.Global.Constants.FilesDir, fileName)))
                    fileName = InputOutput.SaveInputFile(Path.Combine(STCLINE.KP50.Global.Constants.FilesDir, fileName));
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "наименования УК, соглашение не сформировано ";
                MonitorLog.WriteLog("Ошибка формирования отчета наименования УК, соглашение " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            #endregion

            ExcelRepClient excelRepDb = new ExcelRepClient();
            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = user,
                status = ExcelUtility.Statuses.Success,
                rep_name = "Отчет досудебной работы"//"Печать наименования УК, соглашение:" + area
            });
            int nzpExc = ret.tag;
            excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = fileName });//STCLINE.KP50.Global.Constants.ExcelDir + @"\" + fileName });

            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, true);
            }
            ret.result = true;
            ret.text = fileName;
            return ret;
        }

        public Returns GetAllAgrementsReport(DateTime? dat_s, DateTime? dat_po, int user, int area) {
            Returns ret = Utils.InitReturns();
            try
            {
                return ret = GetListOfAgreemants(dat_s, dat_po, user, area, ReportType.Pdf);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";

                if (Global.Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Global.Constants.Debug) MonitorLog.WriteLog("Ошибка GetAllAgrementsReport() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }

        }

        public void GetAllAgrementsReport(object container) {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ExcelRep excelRepDb = new ExcelRep();

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            //ret = excelRepDb.AddToPoolThread(((ParamContainer)container).nzp_user, "empty",
            //  2, "Отчет \"50.2 Протокол сверки данных\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            // ret = excelRepDb.MarkStartPoolThread(((ParamContainer)container).nzp_user, "1", 2, "", time);


            DateTime? dat_s;
            DateTime? dat_po;

            int area = ((ParamContainer) container).listprm[0].nzp_area;
            int user = ((ParamContainer) container).nzp_user;
            if (((ParamContainer) container).listprm[0].dat_s != "")
                dat_s = Convert.ToDateTime(((ParamContainer) container).listprm[0].dat_s);
            else dat_s = null;

            if (((ParamContainer) container).listprm[0].dat_po != "")
                dat_po = Convert.ToDateTime(((ParamContainer) container).listprm[0].dat_po);
            else dat_po = null;

            Returns ret = GetListOfAgreemants(dat_s, dat_po, user, area, ReportType.Pdf);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + ((ParamContainer) container).nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(((ParamContainer) container).nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + ((ParamContainer) container).nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(((ParamContainer) container).nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

    }
}
