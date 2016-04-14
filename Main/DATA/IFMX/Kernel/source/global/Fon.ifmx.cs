using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbFon: DataBaseHead
    {
#if PG
        private readonly string pgDefaultDb = "public";
#else
#endif

        public bool IsNewTask(string table)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return false;

            bool b = IsNewTask(conn_web, null, table);

            conn_web.Close();
            return b;
        }

        public bool IsNewTask(IDbConnection connection, IDbTransaction transaction, string table)
        {
            if (!TempTableInWebCashe(connection, table))
            {
                return false;
            }

            string sql = "";

            #if PG
            sql = 
                " UPDATE " + pgDefaultDb + "." + table + " SET kod_info = " + TaskStates.New.GetHashCode() + 
                " WHERE kod_info = " + TaskStates.InProcess.GetHashCode() + " AND NOW() - INTERVAL '3 hours' > dat_in ";
            #else
            sql =
                " UPDATE " + table + " SET kod_info = " + TaskStates.New.GetHashCode() +
                " WHERE kod_info = " + TaskStates.InProcess.GetHashCode() + " AND CURRENT - 3 UNITS HOUR > dat_in ";
            #endif

            //конечно, надо проверять зависшие процессы и снимать их (кто выполняется больше 3 часов)
            Returns ret = ExecSQL(connection, sql, true);
            if (!ret.result) return false;

            IDataReader reader;
            ret = ExecRead(connection, out reader, " Select nzp_key From " + table + " Where kod_info = " + TaskStates.New.GetHashCode(), true);
            if (!ret.result) return false;
            
            bool b = reader.Read();
            reader.Dispose();
            reader.Close();
            return b;
        }
    }
}
