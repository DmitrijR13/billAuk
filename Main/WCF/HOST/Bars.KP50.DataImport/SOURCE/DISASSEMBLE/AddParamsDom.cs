using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Bars.KP50.DataImport.SOURCE.DISASSEMBLE;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class AddParamsDom : DbAdminClient
    {

        /// <summary>
        /// Разбор: Функция добавления домовых параметров
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nzp_file"></param>
        /// <param name="date_s"></param>
        /// <param name="finder"></param>
        /// <param name="dat_po"></param>
        /// <returns></returns>

        public ReturnsType Run(IDbConnection conn_db, int nzp_file, DateTime date_s, FilesDisassemble finder,
            string dat_po)
        {
            ReturnsType ret = new ReturnsType();
            int commandTime = 3600;
            MonitorLog.WriteLog("Старт разбора домовых параметров (шаг 1) ", MonitorLog.typelog.Info, 1, 2, true);
            //изменение статуса загрузки
            string sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                " SET diss_status = 'Загрузка домовых параметров' " +
                " WHERE nzp_file = " + nzp_file;
            IDbCommand IfxCommand = DBManager.newDbCommand(sql, conn_db);
            IfxCommand.ExecuteNonQuery();

            //Проверка ранее загруженного с этого файла если нет то добавляем
            sql = " select " + DBManager.sNvlWord + "(min(nzp_key),0) from "
                  + finder.bank + "_data" + tableDelimiter + "prm_2 where user_del = " + nzp_file + "  ";


            if (Convert.ToInt32(IfxCommand.ExecuteScalar()) == 0)
            {
                //Количество этажей
                WriteOneParamToPrmFromDom(conn_db, finder, 2, date_s, dat_po, "etazh", 37, " AND nzp_dom > 0 " +
                                                                                            " AND cast( " + DBManager.sNvlWord + " (etazh,0) as integer)>0 ");
                //Дата постройки дома
                WriteOneParamToPrmFromDom(conn_db, finder, 2, date_s, dat_po, "build_year", 150, " AND build_year is not null");
                //Общая площадь
                WriteOneParamToPrmFromDom(conn_db, finder, 2, date_s, dat_po, "total_square", 40, " AND total_square > 0");
                //Площадь МОП дома
                WriteOneParamToPrmFromDom(conn_db, finder, 2, date_s, dat_po, "mop_square", 2049, " AND mop_square > 0");
                //Полезная площадь
                WriteOneParamToPrmFromDom(conn_db, finder, 2, date_s, dat_po, "useful_square", 36, " AND useful_square > 0");
                //Категория благоустройства
                WriteOneParamToPrmFromDom(conn_db, finder, 2, date_s, dat_po, "cat_blago", 2001, " AND cat_blago > '0'");
            }

            string sort_schema = finder.bank + sDataAliasRest;
#if PG
            sort_schema = pgDefaultSchema + tableDelimiter;
#endif
            #region заполнение idom в dom
            
            MonitorLog.WriteLog("Старт разбора домовых параметров (шаг 2) ", MonitorLog.typelog.Info, 1, 2, true);
            sql =
                " UPDATE " + finder.bank + sDataAliasRest + "dom " +
                " SET idom = " + sort_schema + "sortnum(ndom ) " +
                " WHERE " + sNvlWord + "(idom, 0) = 0";
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            sql =
                " UPDATE " + Points.Pref + sDataAliasRest + "dom " +
                " SET idom = " + sort_schema + "sortnum(ndom ) " +
                " WHERE " + sNvlWord + "(idom, 0) = 0";
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            #endregion

            #region заполнение nzp_area

            MonitorLog.WriteLog("Старт разбора домовых параметров (шаг 3) ", MonitorLog.typelog.Info, 1, 2, true);
            
            SetNzpArea(conn_db, nzp_file, finder, commandTime);

            #endregion заполнение nzp_area

            #region заполнение кодов ускорения доступа к дому

            MonitorLog.WriteLog("Старт разбора домовых параметров (шаг 4) ", MonitorLog.typelog.Info, true);

            UpdDomField(conn_db, finder, "nzp_raj", "s_ulica", "nzp_ul");
            UpdDomField(conn_db, finder, "nzp_town", "s_rajon", "nzp_raj");
            UpdDomField(conn_db, finder, "nzp_stat", "s_town", "nzp_town");
            UpdDomField(conn_db, finder, "nzp_land", "s_stat", "nzp_stat");

            #endregion заполнение кодов ускорения доступа к дому

            #region

#if PG
            sql =
                " update " + Points.Pref + "_data" + tableDelimiter +
                " dom  set (nzp_area, nzp_geu, nzp_raj, nzp_town, nzp_stat, nzp_land)  = " +
                "((select nzp_area" +
                " from " + finder.bank + "_data" + tableDelimiter + "dom a where a.nzp_dom =" +
                Points.Pref + "_data" + tableDelimiter + "dom.nzp_dom)," +
                " (select  nzp_geu" +
                " from " + finder.bank + "_data" + tableDelimiter + "dom a where a.nzp_dom =" +
                Points.Pref + "_data" + tableDelimiter + "dom.nzp_dom)," +
                " (select  nzp_raj" +
                " from " + finder.bank + "_data" + tableDelimiter + "dom a where a.nzp_dom =" +
                Points.Pref + "_data" + tableDelimiter + "dom.nzp_dom)," +
                " (select  nzp_town" +
                " from " + finder.bank + "_data" + tableDelimiter + "dom a where a.nzp_dom =" +
                Points.Pref + "_data" + tableDelimiter + "dom.nzp_dom)," +
                " (select  nzp_stat" +
                " from " + finder.bank + "_data" + tableDelimiter + "dom a where a.nzp_dom =" +
                Points.Pref + "_data" + tableDelimiter + "dom.nzp_dom)," +
                " (select  nzp_land" +
                " from " + finder.bank + "_data" + tableDelimiter + "dom a where a.nzp_dom =" +
                Points.Pref + "_data" + tableDelimiter + "dom.nzp_dom))" +
                " where nzp_dom in " +
                " (select nzp_dom from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom where nzp_file =" +
                nzp_file + " and nzp_dom >0 )";
#else
            sql =
                " update " + Points.Pref + "_data" + tableDelimiter + " dom  set (nzp_area, nzp_geu, nzp_raj, nzp_town, nzp_stat, nzp_land)  = " +
                "((select nzp_area, nzp_geu, nzp_raj, nzp_town, nzp_stat, nzp_land" +
                " from " + finder.bank + "_data" + tableDelimiter + "dom a where a.nzp_dom =" +
                Points.Pref + "_data" + tableDelimiter + "dom.nzp_dom   ))" +
                " where nzp_dom in " +
                " (select nzp_dom from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom where nzp_file =" +
                nzp_file + " and nzp_dom >0 )";
#endif
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            #endregion

            MonitorLog.WriteLog("Успешно завершено 'Загрузка домовых параметров'", MonitorLog.typelog.Info, true);
            return ret;
        }

        private static void SetNzpArea(IDbConnection conn_db, int nzp_file, FilesDisassemble finder, int commandTime)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт проставления кодов ЖЭУ и УК ", MonitorLog.typelog.Info, true);
            string sql;
            string version = finder.versionFull;

            if (version == "1.2.1" || version == "1.2.2")
            {
                sql =
                    " UPDATE " + finder.bank + sDataAliasRest + " dom " +
                    " SET nzp_area = " +
                    "   (SELECT max(a.nzp_area) " +
                    "   FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_area a, " +
                    Points.Pref + DBManager.sUploadAliasRest + "file_dom b " +
                    "   WHERE a.id = b.area_id AND a.nzp_file = b.nzp_file AND a.nzp_file =" + nzp_file +
                    "   AND b.nzp_dom = " + finder.bank + sDataAliasRest + "dom.nzp_dom ) " +
                    " WHERE  nzp_dom in " +
                    "   (SELECT nzp_dom" +
                    "   FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
                    "   WHERE nzp_file =" + nzp_file + " AND nzp_dom > 0)";
                DBManager.ExecSQL(conn_db, null, sql, true, commandTime);


                //проставляем жэу
                sql =
                    " UPDATE " + finder.bank + sDataAliasRest + " dom " +
                    " SET nzp_geu =  " + DbDisUtils.GetNoGeuKod(conn_db, finder) +
                    " WHERE nzp_dom in " +
                    "    (SELECT nzp_dom" +
                    "    FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
                    "    WHERE nzp_file =" + nzp_file + " AND nzp_dom > 0)";
                DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            }
            else //if (version == "1.3.2" || version == "1.3.3" || version == "1.3.4" || version == "1.3.5" || version == "1.3.6" || version == "1.3.7" || version == "1.3.8")
            {
                ret = SetNzpAreaGeu132(conn_db, finder);
            }
        }

        private static Returns SetNzpAreaGeu132(IDbConnection conn_db, FilesDisassemble finder)
        {
            Returns ret = new Returns(true);
            
            

            string sql;
            try
            {
                int noArea = DbDisUtils.GetNoAreaKod(conn_db, finder);
                int noGeu = DbDisUtils.GetNoGeuKod(conn_db, finder);

                //nzp_area в dom
                sql =
                    " UPDATE " + finder.bank + sDataAliasRest + " dom" +
                    " SET nzp_area = " +
                    "   (SELECT MAX(" + sNvlWord + "(a.nzp_area," + noArea + ")) " +
                    "   FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic fu," +
                    "   " + Points.Pref + sDataAliasRest + "s_area a," +
                    "   " + Points.Pref + sUploadAliasRest + "file_dom fd" +
                    "   WHERE fu.urlic_id = fd.area_id AND fu.nzp_file = fd.nzp_file" +
                    "   AND a.nzp_payer = fu.nzp_payer " +
                    "   AND fd.nzp_file = " + finder.nzp_file + "" +
                    "   AND fd.nzp_dom = " + finder.bank + sDataAliasRest + "dom.nzp_dom )" +
                    " WHERE nzp_area IS NULL";
                MonitorLog.WriteLog(sql, MonitorLog.typelog.Info, true);
                DBManager.ExecSQL(conn_db, null, sql, true);

                //nzp_geu в dom
                sql =
                    " UPDATE " + finder.bank + sDataAliasRest + " dom" +
                    " SET nzp_geu = " + noGeu +
                    " WHERE nzp_geu IS NULL";
                DBManager.ExecSQL(conn_db, null, sql, true);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog( "Ошибка проставления кода УК и ЖЭУ " + ex.Message, MonitorLog.typelog.Error, true);
                return  new Returns(false, "Ошибка проставления кода УК и ЖЭУ");
            }
            return ret;
        }



        private Returns WriteOneParamToPrmFromDom(IDbConnection con_db, FilesDisassemble finder, int prm_table, DateTime date_s, string dat_po, string field, int nzp_prm, string where)
        {
            Returns ret = new Returns();
            int commandTime = 3600;
            string prm_t = sDataAliasRest + "prm_" + prm_table;

            string sql =
                " INSERT INTO " + Points.Pref + prm_t +
                " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
                " SELECT DISTINCT " + field + "," + nzp_prm + ",nzp_dom," + "cast('" + date_s.ToShortDateString() + "'as date)," +
                " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
                " WHERE  nzp_file = " + finder.nzp_file + where;

            ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);

            sql =
                " INSERT INTO " + finder.bank + prm_t +
                " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
                " SELECT DISTINCT " + field + "," + nzp_prm + ",nzp_dom," + "cast('" + date_s.ToShortDateString() + "'as date)," +
                " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
                " WHERE  nzp_file = " + finder.nzp_file + where;

            ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);

            return ret;
        }

        private Returns UpdDomField(IDbConnection con_db, FilesDisassemble finder, string upd_field, string table_from, string rel_filed)
        {
            int commandTime = 3600;
            string sql =
                " UPDATE " + finder.bank + sDataAliasRest + " dom " +
                " SET " + upd_field + " = " +
                "   (SELECT " + upd_field +
                "    FROM " + Points.Pref + sDataAliasRest + table_from +" a" +
                "    WHERE a." + rel_filed + " =" + finder.bank + sDataAliasRest + "dom." + rel_filed + " )" +
                " WHERE nzp_dom in " +
                "    (SELECT nzp_dom" +
                "    FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
                "    WHERE nzp_file =" + finder.nzp_file + " and nzp_dom >0 )";
            Returns ret =  DBManager.ExecSQL(con_db, null, sql, true, commandTime);
            return ret;
        }
    }
}
