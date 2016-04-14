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
      
        //Отчет: Сальдовая оборотная ведомость
        public void GetSaldoRep10_14_3(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Сальдовая оборотная ведомость (10.14.3)\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Сальдовая оборотная ведомость (10.14.3)\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSaldoRep10_14_3(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
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

        //Отчет: Сальдовая оборотная ведомость
        public void GetSaldoRep10_14_1(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Сальдовая оборотная ведомость (10.14.1)\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Сальдовая оборотная ведомость (10.14.1)\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSaldoRep10_14_1(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
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


        /// <summary>
        /// Выгрузка сальдо в банк
        /// </summary>
        /// <returns>true/false</returns>   
        public void GetSaldo_v_bank(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            ParamContainer cont = (ParamContainer)container;
            Returns ret = GetSaldo_v_bank(out ret, cont.supgfinder, cont.yy, cont.mm);
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        public Returns GetSaldo_v_bank(out Returns ret, SupgFinder finder, string year, string month)
        {
            ExcelRep ExR = new ExcelRep();
            ret = Utils.InitReturns();
            ret = ExR.GetSaldo_v_bank(out ret, finder, year, month);
            return ret;
        }


       

        public void GetSaldoServices(object container)
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
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Сальдовая оборотная ведомость по услугам для всех поставщиков\"", ref time2, cont.comment);
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
            ret = excelRepDb.AddToPoolThread(cont.finder.nzp_user, "empty", 2, "Отчет \"Сальдовая оборотная ведомость по услугам для всех поставщиков\"", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.finder.nzp_user, "1", 2, "", time);


            ret = this.GetSaldoServices((SupgFinder)cont.finder, cont.nzp_supp, ref  fileName);
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
