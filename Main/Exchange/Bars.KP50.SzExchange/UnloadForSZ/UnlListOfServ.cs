using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    class UnlListOfServ: BaseUnloadClass
    {
        public override int Code
        {
            get
            {
                return 1002;
            }
        }

        public override string Name
        {
            get
            {
                return "UnlListOfServ";
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
                " nzp_serv INTEGER, " +
                " service CHARACTER(100), " +
                " service_small CHARACTER(20), " +
                " service_name CHARACTER(100), " +
                " ed_izmer CHARACTER(30), " +
                " type_lgot INTEGER, " +
                " nzp_frm INTEGER," +
                " ordering INTEGER " +
                ")";
            ExecSQL(sql);
        }

        public void WriteInListOfServ(FilesImported finder)
        {
            string sql;
            string pref = finder.bank;

            sql = " INSERT INTO " + Name +
                  " SELECT " +
                  " nzp_serv, service, service_small, " +
                  " service_name, ed_izmer, type_lgot, " +
                  " nzp_frm, ordering" +
                  " FROM " + pref + "_kernel.services";
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
                WriteInListOfServ(finder); //запись во временную таблицу

                //выборка данных из временной таблицы 
                sql =
                    " SELECT * FROM " + Name;
                foreach (DataRow rr in ExecSQLToTable(sql).Rows)
                {
                    //формирование строки Список услуг
                    str = Code + sep + rr["nzp_serv"].ToString().Trim() + sep + rr["service"].ToString().Trim() + sep + rr["service_small"].ToString().Trim() + sep +
                          rr["service_name"].ToString().Trim() + sep + rr["ed_izmer"].ToString().Trim() + sep + rr["type_lgot"].ToString().Trim() + sep +
                          rr["nzp_frm"].ToString().Trim() + sep + rr["ordering"].ToString().Trim() + sep;
                    //запись в файл
                    w.Filing(str, finder.saved_name);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnlListOfServ.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }
        }
    }
}
