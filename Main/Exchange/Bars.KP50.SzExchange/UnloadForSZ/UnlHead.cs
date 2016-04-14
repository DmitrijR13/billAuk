using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    public class UnlHead : BaseUnloadClass
    {
        public override int Code
        {
            get
            {
                return 1;
            }
        }

        public override string Name
        {
            get { return "UnlHead"; }
        }

        public override string NameText
        {
            get { return "Заголовок"; }
        }


        public override void Start(FilesImported finder)
        {
            string sql;
            string str;
            string sep = "|";

            OpenConnection();
            CreateTempTable();

            WriteInFile w = new WriteInFile();

            try
            {
                WriteInHead(finder); //1-запись во временную таблицу
                //2-выборка данных из временной таблицы 
                sql =
                    " SELECT * FROM " + Name;
                foreach (DataRow rr in ExecSQLToTable(sql).Rows)
                {
                    str = Code + sep + Convert.ToString(rr["file_type"]).Trim() + sep + Convert.ToString(rr["name_org"]).Trim() + sep + Convert.ToString(rr["partition_org"]).Trim() + sep +
                        Convert.ToString(rr["file_number"]).Trim() + sep + Convert.ToString(rr["file_date"]).Substring(0,10).Trim() + sep + Convert.ToString(rr["phone_number"]).Trim() + sep + Convert.ToString(rr["fio"]).Trim() + sep +
                        Convert.ToString(rr["date_nach"]).Substring(0, 10).Trim() + sep + Convert.ToString(rr["household_number"]).Trim() + sep + Convert.ToString(rr["is_close"]).Trim() + sep + Convert.ToString(rr["format_version"]).Trim() + sep;
                    //! w.Filing(str);  //запись в файл
                    w.Filing(str, finder.saved_name);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnlHead.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }
        }

        public override void Start()
        {
            
        }

        public override void CreateTempTable()
        {
            string sql;

            sql = "DROP TABLE " + Name;
            ExecSQL(sql, false);

            sql =
                " CREATE TEMP TABLE " + Name + "(" +
                " file_type CHAR(20), " +
                " name_org CHAR(40), " +
                " partition_org CHAR(40), " +
                " file_number INTEGER, " +
                " file_date DATE, " +
                " phone_number CHAR(40), " +
                " fio CHAR(80), " +
                " date_nach DATE, " +
                " household_number INTEGER, " +
                " is_close INTEGER, " +
                " format_version CHAR(10))";
            ExecSQL(sql);
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        /// <param name="pref"></param>
        public void WriteInHead(FilesImported finder)
        {
            string sql;
            DateTime nach_date = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month), 1);

            string pref = finder.bank;
            int nzp_user = finder.nzp_user;

            #region Выборка данных из БД

            //Заполняем поле: Тип файла(фиксированное значение)
            sql =
                " INSERT INTO " + Name +
                " SELECT " +
                " 'Параметры ИРЦ', '', '', " +
                " 0, '" + DateTime.Now.ToShortDateString() + "', '', " +
                " '', '" + DateTime.Now.ToShortDateString() + "', 0, " +
                " 0, '2.2'";
            ExecSQL(sql);

            //Заполняем поле: Наименование организации отправителя
            sql = 
                " UPDATE " + Name + 
                " SET name_org = ( " +
                "  SELECT MAX(val_prm) " + 
                "  FROM " + pref + "_data.prm_10 " + 
                "  WHERE nzp_prm = 80)";
            ExecSQL(sql);

            //Заполняем поле: Подразделение организации отправителя   
            sql =
                " UPDATE " + Name +
                " SET partition_org = '" + pref + "_kernel/' || (" +
                "  SELECT catalog_name  " +
                "  FROM INFORMATION_SCHEMA.SCHEMATA " +
                "  WHERE schema_name = '" + pref + "_kernel')";
            ExecSQL(sql);

            //Заполняем поле: Номер файла  
            sql = 
                " UPDATE " + Name + 
                " SET file_number = " + finder.nzp_exc;
            ExecSQL(sql);

            //Заполняем поле: Дата файла  
            sql  =
                " UPDATE " + Name +
                " SET file_date = (" +
                "  SELECT dat_start " +
                "  FROM public.excel_utility " +
                "  WHERE nzp_exc = " + finder.nzp_exc + ")";
            ExecSQL(sql);

            //Заполняем поле: Телефон отправителя  
            sql =
                " UPDATE " + Name +
                " SET phone_number = ( " +
                "  SELECT MAX(val_prm) " +
                "  FROM " + pref + "_data.prm_10" +
                "  WHERE nzp_prm = 96)";
            ExecSQL(sql);

            //Заполняем поле: ФИО отправителя
            sql =
                " UPDATE " + Name +
                " SET fio = ( " +
                "  SELECT uname " +
                "  FROM public.users " +
                "  WHERE nzp_user = " + nzp_user + ")";
            ExecSQL(sql);

            //Заполняем поле: Месяц и год начислений  
            sql =
                " UPDATE " + Name +
                " SET date_nach = '" + nach_date.ToShortDateString() + "'";
            ExecSQL(sql);

            //Заполняем поле: Количество домохозяйств 
            sql =
                " SELECT count(DISTINCT nzp_kvar) as count " +
                " FROM " + pref + "_data.kvar k, " + pref + "_data.dom d " + 
                " WHERE k.nzp_dom = d.nzp_dom " +
                " AND d.nzp_dom IS NOT NULL";
            DataTable dt = ExecSQLToTable(sql);
            int count = Convert.ToInt32(dt.Rows[0]["count"].ToString());
            
            sql =
                " UPDATE " + Name +
                " SET household_number = " + count;
            ExecSQL(sql);

            //Заполняем поле: Признак закрытия месяца  
            sql =
                " UPDATE " + Name +
                " SET is_close = (" +
                "  SELECT iscurrent " +
                "  FROM " + pref + "_data.saldo_date " +
                "  WHERE month_ = " + finder.month +
                "  AND yearr = " + finder.year + ")";
            ExecSQL(sql);

            //Заполняем поле: Версия формата(фиксированное значение)
            sql = 
                " UPDATE " + Name + 
                " SET format_version = '12'";
            ExecSQL(sql);

            #endregion
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        public void WriteInHead()
        {

        }
    }
}
