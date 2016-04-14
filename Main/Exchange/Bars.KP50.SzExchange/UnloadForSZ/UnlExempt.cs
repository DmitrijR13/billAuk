using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    /// <summary>
    /// Выгрузка льготников
    /// </summary>
    public class UnlExempt : BaseUnloadClass
    {
        public override int Code
        {
            get
            {
                return 3;
            }
        }

        public override string Name
        {
            get { return "UnlExempt"; }
        }

        public override string NameText
        {
            get { return "Льготник"; }
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
                //WriteInExempt(pref); //запись во временную таблицу

                //выборка данных из временной таблицы 

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SumNachGil.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
                " code_lg_kategor INTEGER , " +
                " number_lg INTEGER , " +
                " fam CHAR (40) , " +
                " ima CHAR (40) , " +
                " otch CHAR (40) , " +
                " date_of_birth DATE , " +
                " doc_type CHAR (30) , " +
                " serij CHAR (10) , " +
                " number CHAR (7) , " +
                " data_vid DATE , " +
                " data_s DATE , " +
                " data_po DATE , " +
                " count_gil UNTEGER " +
                ")";
            ExecSQL(sql);
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        /// <param name="pref"></param>
        public void WriteInExempt(string pref)
        {

        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        public void WriteInExempt()
        {

        }
    }
}
