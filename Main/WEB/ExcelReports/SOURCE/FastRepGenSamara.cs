using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using FastReport;
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
        public Returns GetListDomFaktura(ReportPrm prm, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetListDomFaktura(prm, out ret);
            if (DT == null)
            {
                ret.result = false;
                return ret;
            }

            DataSet fDataSet = new DataSet();

            fDataSet.Tables.Add(DT);
            FastReport.Report rep = new Report();
            string template = "samara_2.frx";

            rep.Load(System.IO.Directory.GetCurrentDirectory() + @"\Template\" + template);
                    
    
            rep.RegisterData(DT,"Q_master");
            rep.GetDataSource("Q_master").Enabled = true;
            
            
            //установка параметров отчета
            rep.SetParameterValue("month", prm.month.ToString("00"));
            rep.SetParameterValue("year", prm.year);
            rep.SetParameterValue("Erc_name", "ГУП Самарской области 'ЕИРРЦ'");

            try
            {
                rep.Prepare();
                ret.text = "Отчет сформирован " + prm.reportName;
                ret.tag = rep.Report.PreparedPages.Count;


                fileName = STCLINE.KP50.Global.Constants.ExcelDir +
                    "List_count_ls_" + prm.month.ToString("00") + ".20" + prm.year.ToString("00") + ".xlsx";
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
                MonitorLog.WriteLog("Ошибка формирования отчета " + prm.reportName + " " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            rep.Dispose();

            return ret;
        }


        //Отчет:  Сводный отчет по начислениям для Тулы 
        public void GetListDomFaktura(object container)
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
            ret = this.GetListDomFaktura(cont.reportPrm, ref fileName);
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

      
   
    }
}
