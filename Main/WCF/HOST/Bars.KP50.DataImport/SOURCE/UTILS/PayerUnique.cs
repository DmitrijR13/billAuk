using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.SOURCE
{
    public class PayerUnique : SelectedFiles
    {
        public Returns MakeUniquePayer(List<int> selectedFiles)
        {
            Returns ret = new Returns();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    string add_symbols = "( )";

                    string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic set " + 
                        " urlic_name = trim(urlic_name) || ' (' || urlic_id || ')' " +
                        " where length(trim(urlic_name)) + length(trim(urlic_id::text)) + " + add_symbols.Length + " <= 100 " +
                        "   and strpos(trim(urlic_name), ' (' || urlic_id || ')') = 0 " +
                        WhereNzpFile(selectedFiles);

                    ExecSQLWE(con_db, sql);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры MakeUniquePayer : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                }
            }

            return ret;
        }

        public Returns MakeNonUniquePayer(List<int> selectedFiles)
        {
            Returns ret = new Returns();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic set " + 
                        " urlic_name = replace(urlic_name, ' (' || urlic_id || ')', '') " +
                        " WHERE 1=1" + WhereNzpFile(selectedFiles);

                    ExecSQLWE(con_db, sql);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры MakeUniquePayer : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                }
            }

            return ret;
        }
    }
}
