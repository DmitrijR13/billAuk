using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    class UnlListOfSupp: BaseUnloadClass
    {
        public override int Code
        {
            get
            {
                return 1003;
            }
        }

        public override string Name
        {
            get
            {
                return "UnlListOfSupp";
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
                " nzp_supp INTEGER, " +
                " name_supp CHARACTER(100), " +
                " adres_supp CHARACTER(100), " +
                " phone_supp CHARACTER(20), " +
                " geton_plat CHARACTER(2), " +
                " have_proc INTEGER, " +
                " kod_supp CHARACTER(20) " +
                ")";
            ExecSQL(sql);
        }

        public void WriteInListOfSupp(FilesImported finder)
        {
            string sql;
            string pref = finder.bank;

            sql = " INSERT INTO " + Name +
                  " SELECT " +
                  " nzp_supp, name_supp, adres_supp, " +
                  " phone_supp, geton_plat, have_proc, " +
                  " kod_supp" +
                  " FROM " + pref + "_kernel.supplier";
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
                WriteInListOfSupp(finder); //запись во временную таблицу
                
                //выборка данных из временной таблицы 
                sql =
                    " SELECT * FROM " + Name;
                foreach (DataRow rr in ExecSQLToTable(sql).Rows)
                {
                    //формирование строки Список поставщиков
                    str = Code + sep + rr["nzp_supp"].ToString().Trim() + sep + rr["name_supp"].ToString().Trim() + sep + rr["adres_supp"].ToString().Trim() + sep +
                          rr["phone_supp"].ToString().Trim() + sep + rr["geton_plat"].ToString().Trim() + sep + rr["have_proc"].ToString().Trim() + sep +
                          rr["kod_supp"].ToString().Trim() + sep;
                    //запись в файл
                    w.Filing(str, finder.saved_name);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnlListOfSupp.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }
        }
    }
}
