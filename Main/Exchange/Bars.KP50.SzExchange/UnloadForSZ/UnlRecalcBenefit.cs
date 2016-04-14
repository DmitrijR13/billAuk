using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    /// <summary>
    /// Выгрузка перерасчетов по льготам
    /// </summary>
    public class UnlRecalcBenefit: BaseUnloadClass
    {
        public override int Code
        {
            get
            {
                return 7;
            }
        }

        public override string Name
        {
            get { return "UnlRecalcBenefit"; }
        }

        public override string NameText
        {
            get { return "Перерасчет льготы"; }
        }

        public override void Start()
        {

        }

        public override void Start(FilesImported finder)
        {
            OpenConnection();
            CreateTempTable();

            try
            {
                WriteInRecalcBenefit(finder.bank); //запись во временную таблицу

                //выборка данных из временной таблицы 

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SumNachServ.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }

        }

        public override void CreateTempTable()
        {
            string sql;

            sql =
                " DROP TABLE " + Name;
            ExecSQL(sql, false);

            sql = 
                " CREATE TEMP TABLE " + Name + "(" +
                " month_and_year DATE , " +
                " count_lg INTEGER , " +
                " number_gil INTEGER , " +
                " sum_pere_lg DECIMAL , " +
                " sum_pere_gil DECIMAL  , " +
                " nzp_budget INTEGER , " +
                " nzp_law INTEGER" +
                ")";
            ExecSQL(sql);
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        /// <param name="pref"></param>
        public void WriteInRecalcBenefit(string pref)
        {

        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        public void WriteInRecalcBenefit()
        {

        }
    }
}
