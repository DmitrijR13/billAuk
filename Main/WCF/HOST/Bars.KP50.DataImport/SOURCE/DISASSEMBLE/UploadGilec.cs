using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Bars.KP50.DataImport.SOURCE;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase 
{
    public class UploadGilec : DbAdminClient
    {
        /// <summary>
        /// Функция загрузки паспортистки
        /// </summary>
        /// <param name="nzp_file"></param>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public Returns Run(FilesDisassemble finder, IDbConnection conn_db)
        {
            
            var ret = Utils.InitReturns();
            string sql;

            sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest +" files_imported" +
                    " SET diss_status = 'Разбор жильцов' " +
                    " WHERE nzp_file=" + finder.nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, 3600);

            #region чистим, если уже разбирали этот файл, nzp_bank = nzp_file 
            sql = " DELETE FROM " + finder.bank + "_data" + tableDelimiter + "kart " +
                  " WHERE nzp_bank = " + finder.nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, 3600);
            #endregion

            #region заполняем таблицу gilec
            sql = " INSERT INTO " + finder.bank + "_data" + tableDelimiter + " gilec (nzp_gil) " +
                  " SELECT DISTINCT nzp_gil FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec " +
                  " WHERE nzp_gil NOT IN" +
                        " (SELECT nzp_gil FROM " + finder.bank + "_data" + tableDelimiter + " gilec)" +
                  " AND nzp_file =" + finder.nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, 3600);
            #endregion

            #region создаем временную таблицу
            try
            {
                sql = "DROP TABLE temp_table_gilec";
                ExecSQL(conn_db, sql, false);
            }
            catch 
            {}

            sql = " CREATE TEMP TABLE temp_table_gilec " +
                  " (id SERIAL NOT NULL," +
                  " nzp_file INTEGER," +
                  " num_ls char(20)," +
                  " nzp_gil INTEGER," +
                  " nzp_kart INTEGER," +
                  " nzp_tkrt INTEGER," +
                  " fam CHAR(40)," +
                  " ima CHAR(40)," +
                  " otch CHAR(40)," +
                  " dat_rog DATE," +
                  " fam_c CHAR(40)," +
                  " ima_c CHAR(40)," +
                  " otch_c CHAR(40)," +
                  " dat_rog_c DATE," +
                  " gender CHAR(1)," +
                  " nzp_dok INTEGER," +
                  " serij CHAR(10)," +
                  " nomer CHAR(7)," +
                  " vid_dat DATE," +
                  " vid_mes CHAR(70)," +
                  " kod_podrazd CHAR(7)," +
                  " strana_mr CHAR(30)," +
                  " region_mr CHAR(30)," +
                  " okrug_mr CHAR(30)," +
                  " gorod_mr CHAR(30)," +
                  " npunkt_mr CHAR(30)," +
                  " rem_mr CHAR(180)," +
                  " strana_op CHAR(30)," +
                  " region_op CHAR(30)," +
                  " okrug_op CHAR(30)," +
                  " gorod_op CHAR(30)," +
                  " npunkt_op CHAR(30)," +
                  " rem_op CHAR(180)," +
                  " strana_ku CHAR(30)," +
                  " region_ku CHAR(30)," +
                  " okrug_ku CHAR(30)," +
                  " gorod_ku CHAR(30)," +
                  " npunkt_ku CHAR(30)," +
                  " rem_ku CHAR(180)," +
                  " rem_p CHAR(40)," +
                  " tprp CHAR(1)," +
                  " dat_prop DATE," +
                  " dat_oprp DATE," +
                  " dat_pvu DATE," +
                  " who_pvu CHAR(40)," +
                  " dat_svu DATE," +
                  " namereg CHAR(80)," +
                  " kod_namereg CHAR(7)," +
                  " rod CHAR(30)," +
                  " nzp_celp INTEGER," +
                  " nzp_celu INTEGER," +
                  " dat_sost DATE," +
                  " dat_ofor DATE," +
                  " nzp_kvar INTEGER," +
                  " pref CHAR(7)," +
                  " nzp_rod INTEGER," +
                  " comment CHAR(40))" + sUnlogTempTable;
            ret = ExecSQL(conn_db, sql, true);
            #endregion


            sql = " INSERT INTO temp_table_gilec" +
                  " (nzp_file, num_ls, nzp_gil, nzp_kart, nzp_tkrt, fam, ima, otch, dat_rog, fam_c," +
                  " ima_c, otch_c, dat_rog_c, gender, nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd," +
                  " strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, rem_mr, strana_op, region_op, okrug_op, gorod_op," +
                  " npunkt_op, rem_op, strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp," +
                  " dat_prop, dat_oprp, dat_pvu, who_pvu, dat_svu, namereg, kod_namereg, rod, nzp_celp, nzp_celu," +
                  " dat_sost, dat_ofor, comment) " +

                  " SELECT " +
                  " nzp_file, num_ls, nzp_gil, nzp_kart, nzp_tkrt, fam, ima, otch, dat_rog, fam_c," +
                  " ima_c, otch_c, dat_rog_c, gender, nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd," +
                  " strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, rem_mr, strana_op, region_op, okrug_op, gorod_op," +
                  " npunkt_op, rem_op, strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp," +
                  " dat_prop, dat_oprp, dat_pvu, who_pvu, dat_svu, namereg, kod_namereg, upper(rod), nzp_celp, nzp_celu," +
                  " dat_sost, dat_ofor, comment " + 
                  " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec " +
                  " WHERE " + sNvlWord + "(comment,'') = '' " +
                  " AND nzp_gil NOT IN" +
                        " (SELECT nzp_gil FROM " + finder.bank + "_data" + tableDelimiter + "kart)" +
                  " AND nzp_file = " + finder.nzp_file;
            ret = ExecSQL(conn_db, sql, true);

            sql = " UPDATE temp_table_gilec " +
                  " SET nzp_kvar = " +
                      " (SELECT k.nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar k " +
                      " WHERE  k.id  = temp_table_gilec.num_ls AND k.nzp_file = " + finder.nzp_file + ")";
            ret = ExecSQL(conn_db, sql, true);

            sql = " UPDATE temp_table_gilec " +
                  " SET pref = " +
                      " (SELECT k.pref FROM " + Points.Pref + "_data" + tableDelimiter + "kvar k " +
                      " WHERE k.nzp_kvar = temp_table_gilec.nzp_kvar)";
            ret = ExecSQL(conn_db, sql, true);

            sql = " UPDATE temp_table_gilec " +
                  " SET nzp_rod = " +
                      " ((SELECT r.nzp_rod FROM " + Points.Pref + "_data" + tableDelimiter + "s_rod r " +
                      " WHERE upper(rod) " + DataImportUtils.plike +  "  temp_table_gilec.rod)) " +
                  " WHERE rod IS NOT NULL";
            ret = ExecSQL(conn_db, sql, true);

            //тип документа паспотр в формате 1, а в справочнике 10, поэтому меняем
            sql = " UPDATE temp_table_gilec " +
                 " SET nzp_dok = 10" +
                 " WHERE nzp_dok = 1";
            ret = ExecSQL(conn_db, sql, true);

            sql = " SELECT count(*) AS kol FROM temp_table_gilec ";
            int kol_srt = Convert.ToInt32( DBManager.ExecScalar(conn_db, sql, out ret, true));

            int i = 0;
            while (i <= kol_srt)
            {

                sql = " INSERT INTO " + finder.bank + "_data" + tableDelimiter + "kart " +
                      " (nzp_gil, isactual, fam, ima, otch, dat_rog, fam_c, ima_c, otch_c , dat_rog_c, " +
                      " gender, tprp, dat_pvu, who_pvu, dat_svu, nzp_user, nzp_tkrt, nzp_kvar, " +
                      " rem_p, namereg,  nzp_rod, nzp_dok, serij, nomer,  vid_mes, vid_dat," +
                      " strana_mr, region_mr, gorod_mr, npunkt_mr, rem_mr, " +
                      " strana_op,  region_op, gorod_op, npunkt_op, rem_op," +
                      " strana_ku, region_ku, gorod_ku, npunkt_ku, rem_ku, " +
                      " is_unl, dat_sost, dat_ofor, kod_podrazd, nzp_bank, dat_izm )" +
                      " SELECT " +
                      " nzp_gil, 1, fam, ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c," +
                      " gender, tprp, dat_pvu, who_pvu, dat_svu, " + finder.nzp_user + ", nzp_tkrt,  nzp_kvar," +
                      " rem_p, namereg,  nzp_rod, nzp_dok,  upper(serij), upper(nomer), upper(vid_mes), vid_dat," +
                      " strana_mr , region_mr,  gorod_mr, npunkt_mr, rem_mr, " +
                      " strana_op, region_op, gorod_op, npunkt_op, rem_op," +
                      " strana_ku, region_ku, gorod_ku,  npunkt_ku, rem_ku, " +
                      " 0, dat_sost, dat_ofor, kod_podrazd, " + finder.nzp_file + "," + sCurDate +
                      " FROM temp_table_gilec" +
                      " WHERE nzp_kvar IS NOT NULL AND pref IS NOT NULL AND" +
                      " id >= " + i + " AND id <" + (i + 500);
                ret = ExecSQL(conn_db, sql, true);

                i += 500;
            }


            sql = sUpdStat +" "+ finder.bank + DBManager.sDataAliasRest + "kart ";
            ret = ExecSQL(conn_db, sql, true);

            return ret;
        }
    }
}
