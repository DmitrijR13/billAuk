using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class LoadAsRepair : DbAdminClient
    {
        #region Функция добавления признака "Загрузить как кап.ремонт"
        /// <summary>
        /// Функция добавления признака "Загрузить как кап.ремонт"
        /// </summary>
        /// <returns></returns>
        public Returns Run(IDbConnection conn_db, FilesDisassemble finder, DateTime date_s)
        {
            Returns ret = Utils.InitReturns();
            string sql = "";
            try
            {
                #region убрано из общего разбора

                //nzp_kvar
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                    " SET nzp_kvar = " +
                    " ((select max(nzp) from " + finder.bank + "_data" + tableDelimiter + "prm_1 " +
                    " where nzp_prm = 866 and cast (val_prm as integer) = " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.ukas)) " +
                    " where " + sNvlWord + "(ukas, 0) > 0 and nzp_kvar is null " +
                    " and nzp_file=" + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                    " SET ukas = null " +
                    " where nzp_kvar  is null and nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
                    " SET num_ls= " +
                    " (SELECT num_ls " +
                    " FROM " + finder.bank + "_data" + tableDelimiter + "kvar a, " +
                    Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk" +
                    " where fk.id = " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls.ls_id and " +
                    " fk.nzp_kvar is not null and fk.nzp_kvar= a.nzp_kvar and fk.nzp_file = " + finder.nzp_file + " ) " +
                    " WHERE " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls.nzp_file = " +
                    finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);


                sql =
                    " update  " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar set nzp_dom =" +
                    "(" +
                    " select max(nzp_dom ) from " + Points.Pref + DBManager.sUploadAliasRest + " file_dom " +
                    " where id =" + Points.Pref + DBManager.sUploadAliasRest +
                    "file_kvar.dom_id and nzp_file=" + finder.nzp_file +
                    ") " +
                    " where nzp_file= " + finder.nzp_file + " and nzp_dom is null";
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                #endregion

                sql =
                    " UPDATE " + Points.Pref + "_data" + tableDelimiter + "kvar set typek = " +
                    " (" +
                    "   SELECT ls_type FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk " +
                    "   WHERE cast( fk.id as integer)=" + Points.Pref + "_data" + tableDelimiter +
                    "kvar.nzp_kvar " +
                    "   AND nzp_file = " + finder.nzp_file +
                    " ) " +
                    " WHERE EXISTS " +
                    "    ( SELECT 1 FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk" +
                    "      WHERE nzp_file = " + finder.nzp_file +
                    "      AND cast (fk.id as integer)= " + Points.Pref + "_data" + tableDelimiter +
                    "kvar.nzp_kvar " +
                    "     )";
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                sql =
                    " UPDATE " + finder.bank + "_data" + tableDelimiter + "kvar set typek = " +
                    " (" +
                    "   SELECT ls_type FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk " +
                    "   WHERE cast( fk.id as integer)=" + finder.bank + "_data" + tableDelimiter +
                    "kvar.nzp_kvar " +
                    "   AND nzp_file = " + finder.nzp_file +
                    " ) " +
                    " WHERE EXISTS " +
                    "    ( SELECT 1 FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk" +
                    "      WHERE nzp_file = " + finder.nzp_file +
                    "      AND cast (fk.id as integer)= " + finder.bank + "_data" + tableDelimiter +
                    "kvar.nzp_kvar " +
                    "     )";
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                #region закрываем предыдущие тарифы
               
                //закрываем предыдущие тарифы
                sql =
                    " UPDATE " + finder.bank + "_data" + tableDelimiter + "tarif SET is_actual = 100 " +
                    " WHERE nzp_kvar in " +
                    "      (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar WHERE nzp_file = " + finder.nzp_file + ")";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                sql =
                   " UPDATE " + finder.bank + "_data" + tableDelimiter + "tarif SET dat_when = " + sCurDate +
                   " WHERE nzp_kvar in " +
                   "      (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar WHERE nzp_file = " + finder.nzp_file + ")";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);


                //закрываем предыдущие тарифы
                sql =
                    " UPDATE " + Points.Pref + "_data" + tableDelimiter + "tarif SET is_actual = 100 " +
                    " WHERE nzp_kvar in " +
                    "      (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar WHERE nzp_file = " + finder.nzp_file + ")";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                sql =
                   " UPDATE " + Points.Pref + "_data" + tableDelimiter + "tarif SET dat_when = " + sCurDate +
                   " WHERE nzp_kvar in " +
                   "      (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar WHERE nzp_file = " + finder.nzp_file + ")";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                #endregion закрываем предыдущие тарифы

                # region добавляем услугу кап.ремонт в нижний и верхний банки

                //в нижний банк
                sql =
                    "INSERT INTO " + finder.bank + "_data" + tableDelimiter + "tarif " +
                    "     (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, " +
                    "       dat_s, dat_po, is_actual, nzp_user, dat_when, user_del ) " +
                    " SELECT DISTINCT nzp_kvar, num_ls, 206, 1, 527, " +
                    "       cast ('" + date_s.ToShortDateString() + "' as date), cast ('01.01.3000' as date), 1," + finder.nzp_user + ", " + sCurDate + ", " + finder.nzp_file +
                    " FROM " + Points.Pref + "_data" + tableDelimiter + "kvar " +
                    " WHERE nzp_kvar IN ( SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar WHERE nzp_file = " + finder.nzp_file + " ) ";


                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                //в верхний банк
                sql =
                    "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tarif " +
                    "     (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, " +
                    "       dat_s, dat_po, is_actual, nzp_user, dat_when, user_del ) " +
                    " SELECT DISTINCT nzp_kvar, num_ls, 206, 1, 527, " +
                    "       cast ('" + date_s.ToShortDateString() + "' as date), cast ('01.01.3000' as date), 1," + finder.nzp_user + ", " + sCurDate + ", " + finder.nzp_file +
                    " FROM " + Points.Pref + "_data" + tableDelimiter + "kvar " +
                    " WHERE nzp_kvar IN ( SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar WHERE nzp_file = " + finder.nzp_file + " ) ";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion добавляем услугу кап.ремонт для л/с в нижний и верхний банки

                #region закомментировано
                /*
                #region закрываем период действия параметра 'общая площадь' для загруженных л/с
                sql =
                   " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_1 SET dat_po = " +
#if PG
                   " cast ('" + date_s.ToShortDateString() + "' as date) - INTERVAL '1 day' " +
#else
                   " cast ('" + date_s.ToShortDateString() + "' as date) - 1 day UNITS " +
#endif
                   " WHERE nzp IN " +
                   "      (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar "+
                   "         WHERE nzp_file = " + finder.nzp_file + " ) "+
                   " AND nzp_prm = 4 "+
                   " AND cast ('" + date_s.ToShortDateString() + "' as date) < dat_po ";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                #endregion закрываем период действия параметра 'общая площадь' для загруженных л/с

                #region вставка новых значений параметра 'общая площадь' для загруженных л/с 
                sql =
                    " INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_1 " +
                    "  (nzp, nzp_prm, "+
                    "   dat_s, dat_po, "+
                    "   val_prm, is_actual, cur_unl, nzp_user, dat_when, user_del ) " +
                    " SELECT DISTINCT "+
                    "   nzp_kvar, 4, "+
                    "   cast ('" + date_s.ToShortDateString() + "' as date), cast ('01.01.3000' as date), "+
                    "   total_square, 1, 1," + finder.nzp_user + ", "+ sCurDate + ", " + finder.nzp_file +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                    " WHERE nzp_kvar IN "+
                    "       ( SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar "+
                    "         WHERE nzp_file = " + finder.nzp_file + " ) " +
                    " AND nzp_file = " + finder.nzp_file ;

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion вставка новых значений параметра 'общая площадь' для загруженных л/с
                

                #region закрываем период действия параметра 'жилая площадь' для загруженных л/с
                sql =
                   " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_1 SET dat_po = " +
#if PG
                   " cast ('" + date_s.ToShortDateString() + "' as date) - INTERVAL '1 day' " +
#else
                   " cast ('" + date_s.ToShortDateString() + "' as date) - 1 day UNITS " +
#endif
                   " WHERE nzp IN " +
                   "      (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                   "         WHERE nzp_file = " + finder.nzp_file + " ) " +
                   " AND nzp_prm = 6 "+
                   " AND cast('" + date_s.ToShortDateString() + "' as date) < dat_po "; 
                
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion закрываем период действия параметра 'жилая площадь' для загруженных л/с

                #region вставка новых значений параметра 'жилая площадь' для загруженных л/с 
                sql =
                    " INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_1 " +
                    "  (nzp, nzp_prm, " +
                    "   dat_s, dat_po, " +
                    "   val_prm, is_actual, cur_unl, nzp_user, dat_when, user_del ) " +
                    " SELECT DISTINCT " +
                    "   nzp_kvar, 6, " +
                    "   cast ('" + date_s.ToShortDateString() + "' as date), cast ('01.01.3000' as date), " +
                    "   living_square, 1, 1," + finder.nzp_user + ", " + sCurDate + ", " + finder.nzp_file +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                    " WHERE nzp_kvar IN " +
                    "       ( SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                    "         WHERE nzp_file = " + finder.nzp_file + " ) " +
                    " AND nzp_file = " + finder.nzp_file ;
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion вставка новых значений параметра 'жилая площадь' для загруженных л/с
                */
                #endregion

                #region закрываем период действия параметра 'общая площадь' для загруженных л/с
                sql =
                   " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_1 SET dat_po = " +
#if PG
 " cast ('" + date_s.ToShortDateString() + "' as date) - INTERVAL '1 day' " +
#else
                   " cast ('" + date_s.ToShortDateString() + "' as date) - 1 day UNITS " +
#endif
 " WHERE nzp IN " +
                   "      (SELECT cast(ls_id as integer) FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
                   "         WHERE nzp_file = " + finder.nzp_file + " AND id_prm = 4 " +
                   "      ) " +
                   " AND nzp_prm = 4 " +
                   " AND cast ('" + date_s.ToShortDateString() + "' as date) < dat_po ";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                #endregion закрываем период действия параметра 'общая площадь' для загруженных л/с

                #region вставка новых значений параметра 'общая площадь' для загруженных л/с
                sql =
                    " INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_1 " +
                    "  (nzp, nzp_prm, " +
                    "   dat_s, dat_po, " +
                    "   val_prm, is_actual, cur_unl, nzp_user, dat_when, user_del ) " +
                    " SELECT DISTINCT " +
                    "   cast(ls_id as integer), id_prm, " +
                    "   cast ('" + date_s.ToShortDateString() + "' as date), cast ('01.01.3000' as date), " +
                    "   val_prm, 1, 1," + finder.nzp_user + ", " + sCurDate + ", " + finder.nzp_file +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
                    " WHERE ls_id IN " +
                    "       ( SELECT ls_id FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
                    "         WHERE nzp_file = " + finder.nzp_file + " AND id_prm = 4 ) " +
                    " AND id_prm = 4 " +
                    " AND nzp_file = " + finder.nzp_file;

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion вставка новых значений параметра 'общая площадь' для загруженных л/с


                #region закрываем период действия параметра 'жилая площадь' для загруженных л/с
                sql =
                    " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_1 SET dat_po = " +
#if PG
 " cast ('" + date_s.ToShortDateString() + "' as date) - INTERVAL '1 day' " +
#else
                   " cast ('" + date_s.ToShortDateString() + "' as date) - 1 day UNITS " +
#endif
 " WHERE nzp IN " +
                   "      (SELECT cast(ls_id as integer) FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
                   "         WHERE nzp_file = " + finder.nzp_file + " AND id_prm = 6 " +
                   "      ) " +
                   " AND nzp_prm = 6 " +
                   " AND cast ('" + date_s.ToShortDateString() + "' as date) < dat_po ";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion закрываем период действия параметра 'жилая площадь' для загруженных л/с

                #region вставка новых значений параметра 'жилая площадь' для загруженных л/с
                sql =
                    " INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_1 " +
                    "  (nzp, nzp_prm, " +
                    "   dat_s, dat_po, " +
                    "   val_prm, is_actual, cur_unl, nzp_user, dat_when, user_del ) " +
                    " SELECT DISTINCT " +
                    "   cast(ls_id as integer), id_prm, " +
                    "   cast ('" + date_s.ToShortDateString() + "' as date), cast ('01.01.3000' as date), " +
                    "   val_prm, 1, 1," + finder.nzp_user + ", " + sCurDate + ", " + finder.nzp_file +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
                    " WHERE ls_id IN " +
                    "       ( SELECT ls_id FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
                    "         WHERE nzp_file = " + finder.nzp_file + " AND id_prm = 6 ) " +
                    " AND id_prm = 6 " +
                    " AND nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion вставка новых значений параметра 'жилая площадь' для загруженных л/с


                #region закомментировано
                /*
                #region закрываем период действия параметра 'общая площадь' для загруженных домов
                sql =
                   " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_2 SET dat_po = " +
#if PG
                    " cast ('" + date_s.ToShortDateString() + "' as date) - INTERVAL '1 day' " +
#else
                   " cast ('" + date_s.ToShortDateString() + "' as date) - 1 day UNITS " +
#endif
                   " WHERE nzp IN " +
                   "      (SELECT nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                   "         WHERE nzp_file = " + finder.nzp_file + " ) " +
                   " AND nzp_prm = 686 " +
                   " AND cast ('" + date_s.ToShortDateString() + "' as date) < dat_po ";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                #endregion закрываем период действия параметра 'общая площадь' для загруженных домов

                #region вставка новых значений параметра 'общая площадь' для загруженных домов
                sql =
                    " INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_2 " +
                    "  (nzp, nzp_prm, " +
                    "   dat_s, dat_po, " +
                    "   val_prm, is_actual, cur_unl, nzp_user, dat_when, user_del ) " +
                    " SELECT DISTINCT " +
                    "   nzp_dom, 686, " +
                    "   cast ('" + date_s.ToShortDateString() + "' as date), cast ('01.01.3000' as date), " +
                    "   total_square, 1, 1," + finder.nzp_user + ", " + sCurDate + ", " + finder.nzp_file +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                    " WHERE nzp_dom IN " +
                    "       ( SELECT nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                    "         WHERE nzp_file = " + finder.nzp_file + " ) " +
                    " AND nzp_file = " + finder.nzp_file;

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion вставка новых значений параметра 'общая площадь' для загруженных домов
                */
                #endregion закомментировано

                #region закрываем период действия параметра 'общая площадь' для загруженных домов
                sql =
                   " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_2 SET dat_po = " +
#if PG
 " cast ('" + date_s.ToShortDateString() + "' as date) - INTERVAL '1 day' " +
#else
                   " cast ('" + date_s.ToShortDateString() + "' as date) - 1 day UNITS " +
#endif
 " WHERE nzp IN " +
                   "      (SELECT cast(id_dom as integer) FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom " +
                   "         WHERE nzp_file = " + finder.nzp_file + " AND id_prm = 686 )" +
                   " AND nzp_prm = 686 " +
                   " AND cast ('" + date_s.ToShortDateString() + "' as date) < dat_po ";

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                #endregion закрываем период действия параметра 'общая площадь' для загруженных домов

                #region вставка новых значений параметра 'общая площадь' для загруженных домов
                sql =
                    " INSERT INTO " + finder.bank + "_data" + tableDelimiter + "prm_2 " +
                    "  (nzp, nzp_prm, " +
                    "   dat_s, dat_po, " +
                    "   val_prm, is_actual, cur_unl, nzp_user, dat_when, user_del ) " +
                    " SELECT DISTINCT " +
                    "   cast(id_dom as integer) , id_prm, " +
                    "   cast ('" + date_s.ToShortDateString() + "' as date), cast ('01.01.3000' as date), " +
                    "   val_prm, 1, 1," + finder.nzp_user + ", " + sCurDate + ", " + finder.nzp_file +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom " +
                    " WHERE nzp_dom IN " +
                    "       ( SELECT cast(id_dom as integer)  FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom " +
                    "         WHERE nzp_file = " + finder.nzp_file + " AND id_prm = 686 ) " +
                    " AND id_prm = 686 " +
                    " AND nzp_file = " + finder.nzp_file;

                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                #endregion вставка новых значений параметра 'общая площадь' для загруженных домов

                sql =
                    "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Разобран' " +
                    " where nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
            }
            catch (Exception ex)
            {
                sql =
                    " update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Разобран c ошибками' " +
                    " where nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                MonitorLog.WriteLog("Ошибка при добавлении признака 'Загрузить как кап.ремонт' в функции LoadAsRepair: \n" + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.tag = -1;
                return ret;
            }

            ret.result = true;
            ret.text = "Выполнено.";
            return ret;
        }
        #endregion Функция добавления признака "Загрузить как кап.ремонт"
    }
}
