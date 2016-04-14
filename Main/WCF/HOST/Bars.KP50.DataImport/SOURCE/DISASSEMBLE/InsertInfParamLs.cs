using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Bars.KP50.DataImport;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    /*class InsertInfParamLs : DbAdminClient
    {
        public Returns DisassInfParamLs(IDbConnection conDb, ref StringBuilder disassLog, FilesDisassemble finder)
        {
            Returns ret = new Returns();
            string sql;
            int commandTime = 3600;

            string calc_date = "";
           
            sql =
                " SELECT MAX(calc_date) as calc_date " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_head " +
                " WHERE nzp_file = " + finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            
            sql =
                " SELECT MIN(calc_date)  as calc_date  " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_head " +
                " WHERE nzp_file = " + finder.nzp_file;
            DataTable dt1 = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows[0]["calc_date"].ToString() != dt1.Rows[0]["calc_date"].ToString())
            {
                disassLog.Append("При загрузке сальдо месяц должен быть одинаковый");
                return ret;
            }
            else
            {
                string datet = dt1.Rows[0]["calc_date"].ToString().Substring(0, 10);
                DateTime dtime =
                    new DateTime(Convert.ToInt32(datet.Substring(6, 4)),
                        Convert.ToInt32(datet.Substring(3, 2)), 1).AddMonths(-1);
                calc_date = "'" + dtime.ToString("dd.MM.yyyy") + "'";
            }
    }
    */
}
