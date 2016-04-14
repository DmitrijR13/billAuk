using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;

namespace STCLINE.KP50.REPORT
{
    public partial class ReportGen
    {
       
        //Отчет: получение начислений по дому
        public Returns GetDomCalcs(string Nzp_user, string mm, string yy, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            ExcelLoader ExcelL = new ExcelLoader();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetCalcs(out ret, Nzp_user, mm, yy);

            if (DT != null)
            {
                //собрать заголовки колонок
                string[] col_names = new string[] { "Адрес","Услуга","Ед.измерения", "Нормативный расход", "Расход по КПУ", "Расход по ДПУ", "Площадь всех лс", 
                                                    "Площадь всех лс без КПУ","Кол-во жильцов", "Кол-во жильцов с учетом временных выбывших", 
                                                    "Распределенный расход", "коэффициент П307"};
                //MonitorLog.WriteLog(DT.Rows.Count.ToString(), MonitorLog.typelog.Info, true);


                //собрать типы для колонок
                string[] TypeHeader = new string[] { "", "", "", "float", "float", "float", "float", "float", "int", "int", "float", "float" };

                //процедура формирования файла Excel
                //==========================================================================================================================
                //создание Excel
                ret = ExcelL.Load_to_Excel(1, DT, col_names, null, TypeHeader);

                try
                {
                    if (ret.result)
                    {
                        //применение объединения
                        ExcelFormater eformat = new ExcelFormater();
                        ret = eformat.MergeCells("A", DT.Rows.Count + 1, ref ExcelL.ExlWs);

                        //Сохранение                                                         
                        fileName = "Calcs_" + Nzp_user + ".xls";
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
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = ex.Message;
                }
                finally
                {
                    //удаление объекта
                    ExcelL.DeleteObject();
                }

                //==========================================================================================================================

                //ret = ExcelL.CreateExcelReport(1, DT, col_names, null, Constants.ExcelDir, Nzp_user, TypeHeader);                
            }
            else
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            return ret;

        }





        //==================================Обертки для процедур(потоки)================================================

      




        //Отчет: получение начислений по дому(перед пуском ИСПРАВИТЬ в IFMX)
        public void GetDomCalcs(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ////путь, по которому скачивается файл
            //string path = "";
            ////время записи в БД
            //string time = "";
            string fileName = "";

            ////запись в БД о старте выгрузки
            //ret = ExcelRep.AddToPoolThread(cont.nzp_user, "empty", 2,ref time);

            //Старт формирования
            ret = this.GetDomCalcs(cont.nzp_user.ToString(), cont.mm, cont.yy, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                ////Запись в БД об успешном формировании
                //ret = ExcelRep.MarkPoolThread(cont.nzp_user, "1", 2, path + fileName, "",time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                ////запись об неудачном формировании
                //ret = ExcelRep.MarkPoolThread(cont.nzp_user, "-1", 2,"",ret.text,time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }



        //Отчет: Расшифровка по домам начислено
        public void GetDomNach(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Расшифровка по домам - общая\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Расшифровка по домам - общая\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetDomNach(cont.listprm, cont.nzp_user.ToString(), ref fileName);
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



        //Отчет: Расшифровка по домам начислено
        public void GetDomNachPere(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Расшифровка по домам - начислено\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Расшифровка по домам - начислено\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetDomNachPere(cont.listprm, cont.nzp_user.ToString(), ref fileName);
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

 


    }
}
