using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.DataImport.SOURCE.UTILS;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    public class InsertParamsDom : DataBaseHeadServer
    {
        private IDbConnection conn_db;

        public InsertParamsDom(IDbConnection connection)
        {
            conn_db = connection;
        }

        public Returns InsertParamDom(FilesDisassemble finder, string dat_s)
        {
            var ret = STCLINE.KP50.Global.Utils.InitReturns();

            try
            {
                string ttable = "t_for_dom_prm_dis";

                string sql;

                sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom " +
                      " SET nzp_dom = (SELECT d.nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d " +
                      " WHERE d.local_id = " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom " + ".id_dom::char" +
                      " AND d.nzp_file = " + finder.nzp_file + ") " +
                      " WHERE EXISTS (SELECT 1 FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d " +
                      " WHERE d.local_id = " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom " + ".id_dom::char " +
                      " AND d.nzp_file = " + finder.nzp_file + " AND d.nzp_file = " + Points.Pref + DBManager.sUploadAliasRest + 
                      "file_paramsdom " + ".nzp_file)";
                ExecSQL(conn_db, sql);

                sql = "DROP TABLE " + ttable;
                ExecSQL(conn_db, sql, false);

                sql = " CREATE TEMP TABLE  " + ttable + " (" +
                             " nzp INTEGER," +
                             " nzp_prm INTEGER," +
                             " dat_po DATE," +
                             " dat_s DATE," +
                             " val_prm CHAR(200)," +
                             " is_actual INTEGER, " +
                             " user_del INTEGER," +
                             " cur_unl INTEGER," +
                             " nzp_user INTEGER)";
                ret = ExecSQL(conn_db, sql);

                //prm_2
                sql =
                    " INSERT INTO " + ttable +
                    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, cur_unl, nzp_user)" +
                    " SELECT DISTINCT fp.val_prm, ft.nzp_prm, fd.nzp_dom, fp.dats_val, " +
                    " fp.datpo_val, 1, " + finder.nzp_file + ", 1, " + finder.nzp_user +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom fp," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_typeparams ft, " +
                    Points.Pref + sKernelAliasRest + "prm_name p " +
                    " WHERE fd.nzp_dom = fp.nzp_dom AND fp.id_prm = ft.id_prm " +
                    " AND p.nzp_prm = ft.nzp_prm AND p.prm_num = 2 " +
                    " AND fd.nzp_file = fp.nzp_file AND fp.id_prm = ft.id_prm " +
                    " AND fd.nzp_file = ft.nzp_file " +
                    " AND fd.nzp_file = " + finder.nzp_file;
                ret = ExecSQL(conn_db, sql);

                var lp = new LoadPrm(conn_db);
                ret = lp.SetPrm(2, ttable, finder.bank);

                sql = "DELETE FROM " + ttable + ";";
                ExecSQL(conn_db, sql);

                //prm_4
                sql =
                    " INSERT INTO " + ttable +
                    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, cur_unl, nzp_user)" +
                    " SELECT DISTINCT fp.val_prm, ft.nzp_prm, fd.nzp_dom, fp.dats_val, " +
                    " fp.datpo_val, 1, " + finder.nzp_file + ", 1, " + finder.nzp_user +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom fp," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_typeparams ft, " +
                    Points.Pref + sKernelAliasRest + "prm_name p " +
                    " WHERE fd.nzp_dom = fp.nzp_dom AND fp.id_prm = ft.id_prm " +
                    " AND p.nzp_prm = ft.nzp_prm AND p.prm_num = 4 " +
                    " AND fd.nzp_file = fp.nzp_file AND fp.id_prm = ft.id_prm " +
                    " AND fd.nzp_file = ft.nzp_file " +
                    " AND fd.nzp_file = " + finder.nzp_file;
                ret = ExecSQL(conn_db, sql);

                
                ret = lp.SetPrm(4, ttable, finder.bank);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора параметров дома: " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка разбора параметров дома");
            }

            return ret;
        }
    }
}
