using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    /// <summary>
    /// ВЫгрузка начисленных льгот
    /// </summary>
    public class UnlAccruedBenefit: BaseUnloadClass
    {
        public override int Code
        {
            get
            {
                return 6;
            }
        }

        public override string Name
        {
            get { return "UnlAccruedBenefit"; }
        }

        public override string NameText
        {
            get { return "Начисленная льгота"; }
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
                //WriteInAccruedBenefit(pref); //запись во временную таблицу

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
                "DROP TABLE " + Name;
            ExecSQL(sql, false);

            sql = 
                "CREATE TEMP TABLE " + Name + "(" + 
                " nzp_lgcat INTEGER , " +
                " number_lg INTEGER , " +
                " count_lg INTEGER , " +
                " count_gil INTEGER , " +
                " sum_lg DECIMAL , " +
                " sum_gil DECIMAL , " +
                " sum_pere_lg DECIMAL , " +
                " sum_pere_gil DECIMAL , " +
                " nzp_budget INTEGER , " +
                " nzp_law INTEGER , " +
                " count_string_pere_lg INTEGER" +
                ")";
            ExecSQL(sql);
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        /// <param name="pref"></param>
        public void WriteInAccruedBenefit(string pref)
        {

        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        public void WriteInAccruedBenefit()
        {

        }
    }
}
