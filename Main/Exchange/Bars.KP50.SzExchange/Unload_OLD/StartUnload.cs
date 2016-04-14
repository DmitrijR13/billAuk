using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.Unload
{
    public class StartUnload : DataBaseHeadServer
    {
        /// <summary>
        /// Функция обработки нажатия кнопки "Выгрузить"
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns StartClick(FilesImported finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = Utils.InitReturns();
            //bool stop_flag = false;         //Признак остановки процесса
            //bool process_flag = true;       //Признак работы процесса
            //bool is_bad_unload = false;     // Если в ходе выгрузки произошли ошибкви в ЛС
            //string lgot_kat_spis = "";      //Категории льгот которые могут начисляться в 2005 году

            try
            {
                //удаление временных табличек
                Utilits u = new Utilits();
                u.DropTempTable(conn_db);

                //начало процесса выгрузки
                StartExport se = new StartExport();
                se.ExportProcess(finder, conn_db);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка функции StartExport: " + ret.text, MonitorLog.typelog.Error, true);
                    return ret;
                }

                u.DropTempTable(conn_db);

                //функция повторной выгрузки
                //StartDopExport(finder, conn_db);

                // нужна обработка ошибок
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выгрузки файла" + ex, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка выгрузки файла.", -1);
            }


            MonitorLog.WriteLog("Файл успешно выгружен", MonitorLog.typelog.Info, true);
            //process_flag = false;

            return ret;
        }
    }
}
