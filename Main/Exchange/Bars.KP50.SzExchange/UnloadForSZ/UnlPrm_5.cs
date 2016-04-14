using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    internal class UnlPrm_5 : BaseUnloadClass
    {
        public override int Code
        {
            get
            {
                return 1005;
            }
        }

        public override string Name
        {
            get
            {
                return "UnlPrm_5";
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
                " nzp_key INTEGER, " +
                " nzp INTEGER, " +
                " nzp_prm INTEGER, " +
                " dat_s DATE, " +
                " dat_po DATE, " +
                " val_prm CHARACTER(20), " +
                " is_actual INTEGER, " +
                " nzp_user INTEGER," +
                " dat_when DATE " +
                ")";
            ExecSQL(sql);
        }

        public void WriteInPrm_5(FilesImported finder)
        {
            string sql;
            string pref = finder.bank;

            DateTime dats = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month), 1); //1й день месяца выгрузки
            DateTime datpo = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month),
                DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)));      //последний день месяца выгрузки

            sql = " INSERT INTO " + Name +
                  " SELECT " +
                  " nzp_key, nzp, nzp_prm, " +
                  " dat_s, dat_po, val_prm, " +
                  " is_actual, nzp_user, dat_when" +
                  " FROM " + pref + "_data.prm_5" +
                  " WHERE is_actual <> 100 " +
                  " AND dat_s <= '" + datpo + "'" +
                  " AND dat_po >= '" + dats + "'";
            ExecSQL(sql);
        }

        public override void Start()
        {
            throw new NotImplementedException();
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
                WriteInPrm_5(finder); //запись во временную таблицу

                //выборка данных из временной таблицы 
                sql =
                    " SELECT * FROM " + Name;
                foreach (DataRow rr in ExecSQLToTable(sql).Rows)
                {
                    //формирование строки 1005(prm_5)
                    str = Code + sep + rr["nzp_key"].ToString().Trim() + sep + rr["nzp"].ToString().Trim() + sep +
                          rr["nzp_prm"].ToString().Trim() + sep +
                          ConvertToDate(rr["dat_s"]) + sep + ConvertToDate(rr["dat_po"]) + sep +
                          rr["val_prm"].ToString().Trim() + sep +
                          rr["is_actual"].ToString().Trim() + sep + rr["nzp_user"].ToString().Trim() + sep +
                          ConvertToDate(rr["dat_when"]) + sep;
                    //запись в файл
                    w.Filing(str, finder.saved_name);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnlPrm_5.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message,
                    MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }
        }

        private string ConvertToDate(object obj)
        {
            if (String.IsNullOrEmpty(Convert.ToString(obj)))
            {
                return "";
            }
            else
            {
                DateTime date = Convert.ToDateTime(obj);
                return date.ToString("dd.MM.yyyy");
            }
        }
    }
}
