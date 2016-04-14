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
    internal class DBInsertParamLS : DataBaseHeadServer
    {
        private IDbConnection conn_db;

        public DBInsertParamLS(IDbConnection connection)
        {
            conn_db = connection;
        }

        public Returns InsertParamLS(FilesDisassemble finder, string dat_s)
        {
            Returns ret = new Returns();
            string sql;
            try
            {
                string ttable = "t_for_ls_prm_dis";
                try
                {
                    sql = "DROP TABLE " + ttable;
                    ExecSQL(sql, false);
                }
                catch { }
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

                //sql = 
                //    " INSERT INTO " + ttable +
                //    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, cur_unl, nzp_user)" +
                //    " SELECT DISTINCT fp.val_prm, ft.nzp_prm, fk.nzp_kvar, cast('" + dat_s + "'as date), " +
                //    " cast('" + finder.dat_po + "'as date), 1, "  + finder.nzp_file + ", 1, " + finder.nzp_user + 
                //    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk," +
                //    Points.Pref + DBManager.sUploadAliasRest + "file_paramsls fp," +
                //    Points.Pref + DBManager.sUploadAliasRest + "file_typeparams ft" +
                //    " WHERE fk.id = fp.ls_id AND fp.id_prm = ft.id_prm" +
                //    " AND fk.nzp_file = fp.nzp_file AND fp.id_prm = ft.id_prm " +
                //    " AND fk.nzp_file = ft.nzp_file " +
                //    " AND fk.nzp_file = " + finder.nzp_file;
                //ret = ExecSQL(conn_db, sql);

                sql =
                    " INSERT INTO " + ttable +
                    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, cur_unl, nzp_user)" +
                    " SELECT DISTINCT fp.val_prm, ft.nzp_prm, fk.nzp_kvar, fp.dats_val, " +
                    " fp.datpo_val, 1, " + finder.nzp_file + ", 1, " + finder.nzp_user +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_paramsls fp," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_typeparams ft, " +
                    Points.Pref + sKernelAliasRest + "prm_name p " +
                    " WHERE fk.id = fp.ls_id AND fp.id_prm = ft.id_prm" +
                    " AND p.nzp_prm = ft.nzp_prm AND p.prm_num = 1 " +
                    " AND fk.nzp_file = fp.nzp_file AND fp.id_prm = ft.id_prm " +
                    " AND fk.nzp_file = ft.nzp_file " +
                    " AND fk.nzp_file = " + finder.nzp_file;
                ret = ExecSQL(conn_db, sql);

                var lp = new LoadPrm(conn_db);
                ret = lp.SetPrm(1, ttable, finder.bank);

                sql = "DELETE FROM " + ttable + ";";
                ExecSQL(conn_db, sql);


                sql =
                    " INSERT INTO " + ttable +
                    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, cur_unl, nzp_user)" +
                    " SELECT DISTINCT fp.val_prm, ft.nzp_prm, fk.nzp_kvar, fp.dats_val, " +
                    " fp.datpo_val, 1, " + finder.nzp_file + ", 1, " + finder.nzp_user +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_paramsls fp," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_typeparams ft, " +
                    Points.Pref + sKernelAliasRest + "prm_name p " +
                    " WHERE fk.id = fp.ls_id AND fp.id_prm = ft.id_prm" +
                    " AND p.nzp_prm = ft.nzp_prm AND p.prm_num = 3 " +
                    " AND fk.nzp_file = fp.nzp_file AND fp.id_prm = ft.id_prm " +
                    " AND fk.nzp_file = ft.nzp_file " +
                    " AND fk.nzp_file = " + finder.nzp_file;
                ret = ExecSQL(conn_db, sql);

                ret = lp.SetPrm(3, ttable, finder.bank);

                sql = "DELETE FROM " + ttable + ";";
                ExecSQL(conn_db, sql);


                sql =
                    " INSERT INTO " + ttable +
                    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, cur_unl, nzp_user)" +
                    " SELECT DISTINCT fp.val_prm, ft.nzp_prm, fk.nzp_kvar, fp.dats_val, " +
                    " fp.datpo_val, 1, " + finder.nzp_file + ", 1, " + finder.nzp_user +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_paramsls fp," +
                    Points.Pref + DBManager.sUploadAliasRest + "file_typeparams ft, " +
                    Points.Pref + sKernelAliasRest + "prm_name p " +
                    " WHERE fk.id = fp.ls_id AND fp.id_prm = ft.id_prm" +
                    " AND p.nzp_prm = ft.nzp_prm AND p.prm_num = 15 " +
                    " AND fk.nzp_file = fp.nzp_file AND fp.id_prm = ft.id_prm " +
                    " AND fk.nzp_file = ft.nzp_file " +
                    " AND fk.nzp_file = " + finder.nzp_file;
                ret = ExecSQL(conn_db, sql);

                ret = lp.SetPrm(15, ttable, finder.bank);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора параметров ЛС: " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка разбора параметров ЛС");
            }
            return ret;
        } 
    }
}
