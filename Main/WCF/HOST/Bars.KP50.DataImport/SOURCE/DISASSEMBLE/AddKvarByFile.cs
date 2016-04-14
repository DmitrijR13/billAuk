using System;
using System.Data;
using System.Deployment.Internal.Isolation;
using System.Text;
using System.Threading;
using Bars.KP50.DataImport.SOURCE.DISASSEMBLE;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase 
{
    public class AddKvarByFile : DbAdminClient
    {   
        /// <summary>
        /// Функция добавления лиц счетов и квартирных параметров
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nzp_file"></param>
        /// <param name="date_s"></param>
        /// <param name="finder"></param>
        /// <param name="dat_po"></param>
        /// <param name="disassLog"></param>
        /// <returns></returns>
        public ReturnsType Run(IDbConnection conn_db, int nzp_file, DateTime date_s, FilesDisassemble finder,
            string dat_po)
        {
            int affectedRowsCount = -100;
            MonitorLog.WriteLog("Старт разбора квартирных параметров (AddKvarByFile) ", MonitorLog.typelog.Info, true);

            int commandTime = 3600;
            ReturnsType ret = new ReturnsType();

            //изменение статуса загрузки
            string sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                " SET diss_status = 'Загрузка квартирных параметров'" +
                " WHERE nzp_file = " + nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            //если укас уже есть
            SetNzpKvarByUKAS(conn_db, finder);

            SetNzpDomInFileKvar(conn_db, nzp_file);

            #region Обработать лицевые счета по банкам данных

            sql =
                " SELECT DISTINCT d.pref  " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                finder.bank + sDataAliasRest + "dom d, " +
                Points.Pref + sKernelAliasRest + "s_point p " +
                " WHERE fk.nzp_dom = d.nzp_dom " +
                " AND p.bd_kernel = d.pref " +
                " AND fk.nzp_file = " + nzp_file +
                " AND d.pref ='" + finder.bank + "'";

            IDbCommand IDbCommand = DBManager.newDbCommand(sql, conn_db);
            var readerPref = IDbCommand.ExecuteReader();


            string qreaderPref = "";

            #endregion Обработать лицевые счета по банкам данных

            //Выбрать префикс базы данных и выполнять пока не конец
            while (readerPref.Read())
            {
                if (readerPref["pref"] != DBNull.Value) qreaderPref = readerPref["pref"].ToString().Trim();
                if (qreaderPref.Length > 0) // Перебор по базам данных в выгрузке
                {
                    #region Вставить данные в локальный банк данных

                    //Подправить пустые данные в стандартные пустышки типа "-"
                    SetValueInFileKvarWhereNull(conn_db, nzp_file, qreaderPref);


                    if (finder.use_local_num) // Если выставлено свойство сохранить локальную нумерацию 
                    {
                        MonitorLog.WriteLog("Выбрано 'Использовать локальную нумерацию'", MonitorLog.typelog.Info, true);

                        #region Использование локальной нумерации

                        #region Проверить если есть уже квартира с нужным номером то просто выставить номер квартиры

                        sql =
                            " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                            " SET nzp_kvar = " +
                            "   (SELECT MIN(nzp_kvar " + sConvToInt + ") " +
                            "   FROM " + qreaderPref + "_data" + tableDelimiter + "kvar fk " +
                            "   WHERE  fk.nzp_kvar  = (" + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.id" +
                            sConvToInt + ") ) " +
                            " WHERE nzp_file = " + nzp_file;

                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);
                        sql =
                            " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                            " SET nzp_kvar = " +
                            "   (SELECT MIN(nzp_kvar)  " +
                            "   FROM " + qreaderPref + sDataAliasRest + "kvar fk " +
                            "   WHERE fk.nzp_dom =" + Points.Pref + sUploadAliasRest + "file_kvar.nzp_dom " +
                            "   AND fk.nkvar=" + Points.Pref + sUploadAliasRest + "file_kvar.nkvar " +
                            "   AND case when fk.nkvar_n='-' then '1' else fk.nkvar_n end = " + Points.Pref +
                            sUploadAliasRest + "file_kvar.nkvar_n ) " +
                            " WHERE nzp_file = " + nzp_file +
                            " AND nzp_kvar IS NULL ";
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        // Обновить старые свойства в квар и 
                        // удалить старые свойства квартиры Prm_1 Prm_3

                        #endregion Проверить если есть уже квартира с нужным номером то просто выставить номер квартиры

                        #region Добавить не хватающие квартиры

                        sql =
                            " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                            " SET nzp_kvar = 0 " +
                            " WHERE nzp_kvar IS NULL" +
                            " AND nzp_file=  " + nzp_file + " ";
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        sql =
                            " INSERT INTO " + qreaderPref + sDataAliasRest + "kvar " +
                            " (nzp_kvar , nzp_dom, nkvar, nkvar_n, pkod10,  num_ls,  fio )  " +
                            " SELECT DISTINCT cast (fk.id  as integer), fk.nzp_dom, fk.nkvar, fk.nkvar_n , 0, " +
                            " cast (fk.id  as integer), trim(trim(nvl(fk.fam,''))||' '||trim( nvl(fk.ima,''))||' '||trim( nvl(fk.otch,''))) " +
                            " DROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                            Points.Pref + sDataAliasRest + "dom d, " +
                            Points.Pref + sKernelAliasRest + "s_point p " +
                            " WHERE  fk.nzp_dom = d.nzp_dom " +
                            " AND p.bd_kernel = d.pref " +
                            " AND fk.nzp_kvar = 0 " +
                            " AND fk.nzp_file = " + nzp_file;
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        // в верхний банк тоже 

                        sql = " INSERT INTO " + Points.Pref + sDataAliasRest + "kvar" +
                              " (nzp_kvar , nzp_dom, nkvar, nkvar_n, pkod10,  num_ls,  fio )  " +
                              " SELECT DISTINCT  (fk.id " + sConvToInt +
                              "),fk.nzp_dom, fk.nkvar, fk.nkvar_n , 0 ,  (fk.id " + sConvToInt + ") ," +
                              " trim(trim(nvl(fk.fam,''))||' '||trim( nvl(fk.ima,''))||' '||trim( nvl(fk.otch,''))) " +
                              " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                              Points.Pref + "_data" + tableDelimiter + "dom d, " +
                              Points.Pref + "_kernel" + tableDelimiter + "s_point p" +
                              " WHERE fk.nzp_dom = d.nzp_dom AND p.bd_kernel = d.pref AND fk.nzp_kvar =0 AND fk.nzp_file = " +
                              nzp_file;

                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);
                        MonitorLog.WriteLog("Новые квартиры добавлены.", MonitorLog.typelog.Info, true);

                        #endregion Добавить не хватающие квартиры

                        #region Выставить выставленный nzp_kvar в file_kvar

                        sql =
                            " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                            " SET nzp_kvar = " +
                            "   (SELECT nzp_kvar " +
                            "   FROM " + qreaderPref + sDataAliasRest + "kvar" +
                            "   WHERE num_ls =(" + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.id" +
                            sConvToInt + ")" +
                            "   AND nzp_file= " + nzp_file + ")" +
                            " WHERE nzp_file= " + nzp_file;

                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);
                        // Делаем УКАС равным nzp_kvar 
                        sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                              " SET ukas = nzp_kvar" +
                              " WHERE nzp_file= " + nzp_file;
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        #endregion Выставить выставленный nzp_kvar в загружаемом файле

                        #endregion Использование локальной нумерации
                    }
                    else
                    {
                        #region Добавляем лицевые счета согласно sequence

                        MonitorLog.WriteLog("Старт добавления лицевых счетов согласно sequence", MonitorLog.typelog.Info,
                            true);

                        sql =
                            " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                            " SET nzp_kvar=(  " +
                            "   SELECT nzp_kvar  " +
                            "   FROM " + qreaderPref + sDataAliasRest + "kvar fk " +
                            "   WHERE fk.nzp_dom =" + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.nzp_dom" +
                            "   AND trim(fk.nkvar)=trim(" + Points.Pref + DBManager.sUploadAliasRest +
                            "file_kvar.nkvar) AND " +
                            "   trim(fk.remark) = trim(" + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.id) ) " +
                            "WHERE nzp_file = " + nzp_file + " AND " + sNvlWord + "(nzp_kvar,0) = 0 ";
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);
                        sql =
                            " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                            " SET nzp_kvar = 0 " +
                            " WHERE nzp_kvar IS NULL AND nzp_file=  " + nzp_file + " ";
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        #region добавить записи в нижний и верхний банк

                        MonitorLog.WriteLog("Старт получения значений по умолчанию для ЖЭУ и УК",
                            MonitorLog.typelog.Info, true);
                        int noArea = DbDisUtils.GetNoAreaKod(conn_db, finder);
                        int noGeu = DbDisUtils.GetNoGeuKod(conn_db, finder);
                        MonitorLog.WriteLog("Завершено получение значений по умолчанию для ЖЭУ и УК",
                            MonitorLog.typelog.Info, true);

                        string seq = Points.Pref + sDataAliasRest + "kvar_nzp_kvar_seq";
                        string strNzp_kvar;
#if PG
                        strNzp_kvar = " nextval('" + seq + "') ";
#else
                        strNzp_kvar = seq + ".nextval ";
#endif

                        //   seq = Points.Pref + sDataAliasRest + "kvar_num_ls_seq";

                        string strNum_ls;
                        string sort_schema = qreaderPref + sDataAliasRest;
#if PG
                        strNum_ls = " currval('" + seq + "') ";
                        sort_schema = pgDefaultSchema + tableDelimiter;
#else
                        strNum_ls = seq + ".nextval ";
#endif

                        sql =
                            " INSERT INTO " + Points.Pref + sDataAliasRest + "kvar " +
                            "( nzp_kvar ,nzp_area, nzp_geu, nzp_dom, nkvar," +
                            " ikvar,  nkvar_n, pkod10,  num_ls, " +
                            " remark, " +
                            " typek, pref," +
                            " nzp_wp, is_open ) " +

                            " SELECT " + strNzp_kvar + " ," + noArea + ", " + noGeu + ", fk.nzp_dom, fk.nkvar, " +
                            sort_schema + " sortnum(fk.nkvar), fk.nkvar_n ,0 , " + strNum_ls + " , " +
                            " fk.id, " +
                            " ls_type, '" + finder.bank + "'," +
                            " (SELECT nzp_wp FROM " + Points.Pref + sKernelAliasRest + "s_point WHERE bd_kernel = '" +
                            finder.bank + "'), 1 " +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                            Points.Pref + sDataAliasRest + "dom d, " +
                            Points.Pref + sKernelAliasRest + "s_point p, " +
                            Points.Pref + sUploadAliasRest + "file_dom fd, " +
                            Points.Pref + sKernelAliasRest + "s_point  sp " +
                            " WHERE fk.nzp_dom = d.nzp_dom AND p.bd_kernel = d.pref AND fk.nzp_kvar = 0 " +
                            " AND fk.dom_id = fd.id AND fk.nzp_file = fd.nzp_file " +
                            " AND fk.nzp_file = " + nzp_file +
                            " AND sp.bd_kernel = '" + qreaderPref + "'";
                        DBManager.ExecSQL(conn_db, null, sql, true, out affectedRowsCount, commandTime);

                        MonitorLog.WriteLog("В верхний банк добавлены новые квартиры в кол-ве " + affectedRowsCount, MonitorLog.typelog.Info, true);

                        SetNzpAreaGeuKvar(conn_db, finder, Points.Pref);

                        sql = "CREATE INDEX ix_ins_kvar_12 ON " + qreaderPref + "_data" + tableDelimiter +
                              "kvar (nzp_kvar)";
                        DBManager.ExecSQL(conn_db, null, sql, false, commandTime);

                        sql = "CREATE INDEX ix_ins_kvar_13 ON " + Points.Pref + "_data" + tableDelimiter +
                              "kvar (nzp_kvar)";
                        DBManager.ExecSQL(conn_db, null, sql, false, commandTime);


                        MonitorLog.WriteLog("Старт спуска ЛС из верхнего банка в нижний банк).", MonitorLog.typelog.Info,
                            true);
                        // добавить в нижний банк тоже 
                        sql =
                            " INSERT INTO " + qreaderPref + "_data" + tableDelimiter + "kvar " +
                            " (nzp_kvar , nzp_geu, nzp_area, nzp_dom, nkvar, ikvar, nkvar_n, pkod10,  num_ls, remark )  " +
                            " SELECT nzp_kvar, nzp_geu ,nzp_area, nzp_dom, nkvar, ikvar, nkvar_n, 0,  num_ls, remark " +
                            " FROM " + Points.Pref + "_data" + tableDelimiter + " kvar k " +
                            " WHERE not exists (SELECT 1 FROM " + qreaderPref + "_data" + tableDelimiter +
                            "kvar a WHERE a.nzp_kvar=k.nzp_kvar ) " +
                            " AND pref = '" + finder.bank + "' ";
                        DBManager.ExecSQL(conn_db, null, sql, true, out affectedRowsCount, commandTime);

                        MonitorLog.WriteLog("В нижний банк добавлены новые квартиры в кол-ве " + affectedRowsCount, MonitorLog.typelog.Info, true);


                        #endregion добавить записи в нижний и верхний банк

                        #region Выставить выставленный nzp_kvar в загружаемом файле

                        sql = "CREATE INDEX i_fk_nzp ON " + Points.Pref + DBManager.sUploadAliasRest +
                              "file_kvar (nzp_kvar, nzp_file, id)";
                        DBManager.ExecSQL(conn_db, null, sql, false, commandTime);
                        sql = "CREATE INDEX i_set_kv_remark ON " + Points.Pref + "_data" + tableDelimiter +
                              "kvar (remark)";
                        DBManager.ExecSQL(conn_db, null, sql, false, commandTime);

                        MonitorLog.WriteLog("Старт выставление nzp_kvar в file_kvar", MonitorLog.typelog.Info, true);
                        sql =
                            " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar SET nzp_kvar = " +
                            "(SELECT max(nzp_kvar)  " +
                            " FROM " + qreaderPref + "_data" + tableDelimiter + "kvar " +
                            " WHERE remark =" + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.id ) " +
                            " WHERE nzp_file= " + nzp_file + " AND nzp_kvar = 0 ";
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        sql =
                           " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvarp SET nzp_kvar =0 ";
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        sql =
                           " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvarp SET nzp_kvar = " +
                           "(SELECT max(nzp_kvar)  " +
                           " FROM " + qreaderPref + "_data" + tableDelimiter + "kvar " +
                           " WHERE remark =" + Points.Pref + DBManager.sUploadAliasRest + "file_kvarp.id ) " +
                           " WHERE nzp_file= " + nzp_file + " AND nzp_kvar = 0 ";
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        #endregion Выставить выставленный nzp_kvar в загружаемом файле

                        sql = "CREATE INDEX i_fk_ukas ON " + Points.Pref + DBManager.sUploadAliasRest +
                              "file_kvar (ukas, nzp_file, nzp_kvar)";

                        DBManager.ExecSQL(conn_db, null, sql, false, commandTime);


                        // Делаем УКАС равным nzp_kvar 
                        sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar set ukas = nzp_kvar " +
                              " WHERE nzp_file= " + nzp_file + " AND ukas is null AND " + sNvlWord +
                              "(nzp_kvar, 0) > 0 ";
                        DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        //MonitorLog.WriteLog("Старт добавления ukas в prm_1", MonitorLog.typelog.Info, true);
                        ////добавляем ukas в prm_1
                        //sql =
                        //    " DELETE FROM " + qreaderPref + "_data" + tableDelimiter + "prm_4 " +
                        //    " WHERE nzp_prm = 866 AND nzp in " +
                        //    " (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                        //    " WHERE nzp_file = " + nzp_file + " )";
                        //DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

                        //WriteOneParamToPrmFromDom(conn_db, finder, 4, date_s, "01.01.3000", "ukas", 866, "");

                        #endregion Добавляем лицевые счета согласно sequence
                    }

                    #endregion Вставить данные в локальный банк данных

                    MonitorLog.WriteLog("Старт AddKvarByFile:2 ", MonitorLog.typelog.Info, 1, 2, true);

                    //Выставить фио
                    SetFio(conn_db, nzp_file, qreaderPref, finder, date_s, dat_po);

                    //сохранение параметров
                    WriteKvarParam(conn_db, nzp_file, date_s, finder, dat_po);
                }
            }
            readerPref.Dispose();
            
            if (!finder.repair)
            {
                //перезапись file_paramsls в prm_1
                ParamslsCompare(conn_db, finder, dat_po);
            }
            
            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        private static void SetNzpDomInFileKvar(IDbConnection conn_db, int nzp_file)
        {
            MonitorLog.WriteLog("Старт установки nzp_dom в таблице file_kvar ", MonitorLog.typelog.Info, true);
            string sql =
                " UPDATE  " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                " SET nzp_dom =" +
                "   (SELECT MAX(nzp_dom )" +
                "   FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_dom " +
                "   WHERE id =" + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.dom_id" +
                "   AND nzp_file=" + nzp_file + ") " +
                " WHERE nzp_file= " + nzp_file + " AND nzp_dom IS NULL";
            DBManager.ExecSQL(conn_db, null, sql, true);
        }

        private static void SetNzpKvarByUKAS(IDbConnection conn_db, FilesDisassemble finder)
        {
            
            int commandTime = 3600;
            string sql;
            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                " SET nzp_kvar = " +
                "   ((SELECT max(nzp) FROM " + finder.bank + "_data" + tableDelimiter + "prm_4 " +
                "   WHERE nzp_prm = 866 and cast (val_prm as integer) = " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar.ukas)) " +
                " WHERE " + sNvlWord + "(ukas, 0) > 0 and nzp_kvar is null " +
                " and nzp_file=" + finder.nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                " SET ukas = null " +
                " WHERE nzp_kvar  is null and nzp_file = " + finder.nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
                " SET num_ls= " +
                "   (SELECT num_ls " +
                "   FROM " + finder.bank + "_data" + tableDelimiter + "kvar a, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk" +
                "   WHERE fk.id = " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls.ls_id and " +
                "   fk.nzp_kvar is not null and fk.nzp_kvar= a.nzp_kvar and fk.nzp_file = " + finder.nzp_file + " ) " +
                " WHERE " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls.nzp_file = " +
                finder.nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);
        }

        private static void SetValueInFileKvarWhereNull(IDbConnection conn_db, int nzp_file, string qreaderPref)
        {
            MonitorLog.WriteLog("Старт замены пустых данных в стандартные пустышки типа '-' ", MonitorLog.typelog.Info, true);
            int commandTime = 3600;
            string sql;
            sql = " UPDATE " + Points.Pref + sDataAliasRest + "dom " +
                  " SET pref ='" + qreaderPref + "'" +
                  " WHERE pref IS NULL ";
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                  " SET nkvar = '-'" +
                  " WHERE nkvar IS NULL AND  nzp_file=  " + nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                  " SET nzp_kvar = 0" +
                  " WHERE nzp_kvar IS NULL AND nzp_file=  " + nzp_file + " ";
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                  " SET nzp_kvar = 0" +
                  " WHERE nzp_kvar not in " +
                  "     (SELECT nzp_kvar" +
                  "     FROM  " + Points.Pref + sDataAliasRest + "kvar )" +
                  " AND nzp_file=  " + nzp_file + " ";
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);
        }

        public void WriteKvarParam(IDbConnection conn_db, int nzp_file, DateTime date_s, FilesDisassemble finder, string dat_po, string table_name = "file_kvar", string nzp_kvar = "nzp_kvar")
        {

            //int commandTime = 3600;
            string sql;            

            MonitorLog.WriteLog("Старт записи квартирных параметров (ф-ция WriteKvarParam)", MonitorLog.typelog.Info, true);

            //проверка: table_name == "file_kvar" - это флаг того, что перерасчета не происходит, т.е. 7ая секция не разбирается, иначе table_name == "file_kvarp" 
            if (table_name == "file_kvar")
            {
                #region Состояние лицевого счета (открыт , закрыт)

                sql = " CREATE INDEX ix5_file_kvar_close_date " +
                      " ON " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" + 
                      " USING btree " +
                      " (nzp_file, close_date, nzp_kvar); ";
                DBManager.ExecSQL(conn_db, sql, false);

                sql = " CREATE INDEX ix5_file_kvar_close_date " + 
                      " ON " + Points.Pref + "_upload_partition.file_kvar_" + finder.nzp_file +
                      " USING btree " +
                      " (nzp_file, close_date, nzp_kvar); ";
                DBManager.ExecSQL(conn_db, sql, false);

                // Лицевой счет закрыт
                WriteOneParamToPrmFromKvar(conn_db, finder, 3, date_s, "01.01.3000", "2", 51,
                    " AND close_date IS NOT NULL AND close_date < now() ", table_name, nzp_kvar);
                //Лицевой счет открыт
                WriteOneParamToPrmFromKvar(conn_db, finder, 3, date_s, "01.01.3000", "1", 51, " AND (close_date IS NULL OR close_date > now())",
                    table_name, nzp_kvar);

                #endregion Состояние лицевого счета
            }
            //старый номер л/c
            MonitorLog.WriteLog("Запись параметра старый номер л/c", MonitorLog.typelog.Info, true);
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "id", 2004, "", table_name, nzp_kvar);

            //если загружаем как кап.ремонт, то площади добавляются в ф-ции LoadAsRepair
            if (!finder.repair)
            {
                // общая площадь
                WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "total_square", 4, " AND total_square>0 ", table_name, nzp_kvar);
                //жилая площадь
                WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "living_square", 6, " AND living_square>0 ", table_name, nzp_kvar);
            }

            // отапливаемая площадь
            sql =
                " SELECT nzp_file" +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + table_name +
                " WHERE otapl_square > 0 AND nzp_file=" + nzp_file;
            IDbCommand IfxCommand = DBManager.newDbCommand(sql, conn_db);
            if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "otapl_square", 133, " AND otapl_square > 0 ", table_name, nzp_kvar);
            else
                // Если площади отапливаемой нет , то вставляем площадь
                WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "total_square", 133, " AND total_square > 0 ", table_name, nzp_kvar);
            IfxCommand.Dispose();

            MonitorLog.WriteLog("Запись параметров, связанных с жильцами", MonitorLog.typelog.Info, true);
            //кол-во жильцов
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "kol_gil", 5, " AND kol_gil>0", table_name, nzp_kvar);
            //кол-во жильцов (временно прибывшие)
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "kol_vrem_prib", 131, " AND kol_vrem_prib > 0", table_name, nzp_kvar);
            //кол-во жильцов (временно убывшие)
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "kol_vrem_ub", 10, " AND kol_vrem_ub > 0", table_name, nzp_kvar);
            //кол-во комнат
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "room_number", 107, " AND room_number > 0", table_name, nzp_kvar);
            //эл плита
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "is_el_plita", 19, " AND is_el_plita > 0", table_name, nzp_kvar);
            //газ плита
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "is_gas_plita", 551, " AND is_gas_plita > 0", table_name, nzp_kvar);
            //газ колонка
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "is_gas_colonka", 1, " AND is_gas_colonka > 0", table_name, nzp_kvar);
            //тип жилья по водоснабжению
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "water_type", 7, " AND water_type > 0", table_name, nzp_kvar);
            //тип жилья по гор водоснабжению
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "hotwater_type", 463, " AND hotwater_type > 0", table_name, nzp_kvar);
            //признак коммунальной квартиры
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "2", 3, " AND is_communal > 0", table_name, nzp_kvar); // призн комм квар. = комфортность 
            //наличие забора из открытой системы отопления
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "is_open_otopl", 35, " AND is_open_otopl > 0", table_name, nzp_kvar); // = открытый водозабор гор.воды 
            //площадь по найму
            WriteOneParamToPrmFromKvar(conn_db, finder, 1, date_s, dat_po, "naim_square", 314, " AND naim_square > 0", table_name, nzp_kvar);



            MonitorLog.WriteLog("Завершена запись квартирных параметров (ф-ция WriteKvarParam)", MonitorLog.typelog.Info, true);
        }

        /// <summary>
        /// Функция, выставляющая ФИО
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nzp_file"></param>
        /// <param name="pref"></param>
        /// <param name="finder"></param>
        /// <param name="dat_s"></param>
        /// <param name="dat_po"></param>
        private static void SetFio(IDbConnection conn_db, int nzp_file, string pref, FilesDisassemble finder, DateTime dat_s, string dat_po)
        {
            string sql;
            int commandTime = 3600;
            int affectedRowsCount = -100;

            MonitorLog.WriteLog("Старт выставления ФИО", MonitorLog.typelog.Info, true);

            DBManager.ExecSQL(conn_db, null, " DROP TABLE t_fio ", false);
            
            sql = " CREATE TEMP TABLE t_fio(" +
                  " fio CHAR(128)," +
                  " nzp_kvar INTEGER); ";
            DBManager.ExecSQL(conn_db, null, sql, true);
            
            // во временную таблицу t_fio ФИО кладем полностью
            sql =
                " INSERT INTO t_fio " +
                " SELECT DISTINCT  trim(" + sNvlWord + "(fam,''))||' ' ||trim(" + sNvlWord + "(ima,''))||' '||trim(" +
                sNvlWord +
                "(otch,'')) as fio, nzp_kvar " +
                " FROM  " + Points.Pref + sUploadAliasRest + "file_kvar a" +
                " WHERE a.nzp_file =" + nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            sql = " CREATE INDEX ix_t_fio ON t_fio (nzp_kvar, fio) ";
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            MonitorLog.WriteLog("Чтобы избежать дублей удаляем ФИО из " + pref + sDataAliasRest + "prm_3 по nzp_file =  " + finder.nzp_file, MonitorLog.typelog.Info, true);
            // удаляем ФИО из prm_3 по nzp_file и nzp_prm = 46, для того чтобы не было дублей при повторном разборе
            sql =
                " DELETE " +
                " FROM " + pref + sDataAliasRest + "prm_3 " +
                " WHERE nzp_prm = 46 " +
                " AND user_del = " + finder.nzp_file;
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            // выставляем is_actual = 100 по тем nzp_kvar которые уже есть в t_fio
            sql =
                " UPDATE " + pref + sDataAliasRest + "prm_3 " +
                " SET is_actual = 100 " +
                " WHERE nzp IN " +
                "   (SELECT nzp_kvar " +
                "    FROM t_fio)" +
                " AND nzp_prm = 46";
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);

            // кладем полное ФИО в таблицу prm_3   
            MonitorLog.WriteLog("Заполнение ФИО в таблицу " + pref + sDataAliasRest +  "prm_3 по nzp_file =  " + finder.nzp_file, MonitorLog.typelog.Info, true);
            sql =
                " INSERT INTO " + pref + sDataAliasRest + "prm_3 " +
                " (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, user_del) " +
                " SELECT DISTINCT nzp_kvar, 46, cast('" + dat_s + "' as date), cast('" + dat_po + "' as date), fio, 1, " + finder.nzp_file +
                " FROM  t_fio" +
                " WHERE fio IS NOT NULL";
            DBManager.ExecSQL(conn_db, null, sql, true, commandTime);         

            MonitorLog.WriteLog("Выставление ФИО в нижнем банке", MonitorLog.typelog.Info, true);
            sql =
                " UPDATE " + pref + sDataAliasRest + "kvar  set fio= " +
                "    (SELECT substring(fio from 0 for 50)" +
                "     FROM  t_fio a " +
                "     WHERE a.nzp_kvar = " + pref + sDataAliasRest + "kvar.nzp_kvar AND fio IS NOT NULL ) " +
                " WHERE EXISTS " +
                "    (SELECT 1 " +
                "     FROM  t_fio a " +
                "     WHERE a.nzp_kvar = " + pref + sDataAliasRest + "kvar.nzp_kvar AND fio IS NOT NULL" +
                " ) ";
            DBManager.ExecSQL(conn_db, null, sql, true, out affectedRowsCount, commandTime);
            MonitorLog.WriteLog("Завершено выставление ФИО в локальный банк в кол-ве: " + affectedRowsCount, MonitorLog.typelog.Info, true);

            MonitorLog.WriteLog("Выставление ФИО в верхнем банке", MonitorLog.typelog.Info, true);
            sql =
                " UPDATE " + Points.Pref + sDataAliasRest + "kvar  set fio = " +
                "    (SELECT substring(fio from 0 for 50)" +
                "     FROM  t_fio a " +
                "     WHERE a.nzp_kvar = " + Points.Pref + sDataAliasRest + "kvar.nzp_kvar and fio IS NOT NULL ) " +
                " WHERE EXISTS " +
                "    (SELECT 1 " +
                "     FROM  t_fio a " +
                "     WHERE a.nzp_kvar = " + Points.Pref + sDataAliasRest + "kvar.nzp_kvar AND fio IS NOT NULL" +
                "    ) " +
                " AND pref = '" + pref.Trim() +"'";

            DBManager.ExecSQL(conn_db, null, sql, true, out affectedRowsCount, commandTime);
            MonitorLog.WriteLog("Завершено выставление ФИО в центральный банк в кол-ве: " + affectedRowsCount, MonitorLog.typelog.Info, true);
            
        }

        private Returns ParamslsCompare(IDbConnection con_db, FilesDisassemble finder, string dat_po)
        {
            Returns ret = Utils.InitReturns();
            string sql;

            MonitorLog.WriteLog("Старт перезаписи в prm_1 (ф-ция ParamslsCompare) ", MonitorLog.typelog.Info, true);

            sql =
                " INSERT INTO " + finder.bank + sDataAliasRest + "prm_1" +
                "( nzp,       nzp_prm,         dat_s,    dat_po," +
                " val_prm, is_actual, cur_unl, nzp_wp, nzp_user," +
                " dat_when,  user_del)" +
                " SELECT DISTINCT k.nzp_kvar, p.nzp_prm, a.dats_val , cast(a.datpo_val as date) , " +
                " a.val_prm as val_prm,   1 ,   1 ,  1, " + finder.nzp_user + ", " +
                sCurDate + " , " + finder.nzp_file +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls a, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_head b,  " +
                Points.Pref + DBManager.sUploadAliasRest + "file_kvar k ," +
                Points.Pref + DBManager.sUploadAliasRest + "file_typeparams p" +
                " WHERE p.id_prm= a.id_prm AND p.nzp_prm IS NOT NULL" +
                " AND a.nzp_file = b.nzp_file AND a.ls_id = k.id AND k.nzp_file = b.nzp_file " +
                " AND p.nzp_file=a.nzp_file AND  a.nzp_file =" + finder.nzp_file +
                " AND NOT EXISTS(" +
                "   SELECT 1 " +
                "   FROM " + finder.bank + sDataAliasRest + "prm_1 pr " +
                "   WHERE pr.nzp = k.nzp_kvar " +
                "   AND pr.nzp_prm = p.nzp_prm " +
                "   AND pr.dat_s = a.dats_val " +
                "   AND pr.dat_po = cast(a.datpo_val as date) " +
                "   AND pr.is_actual = 1)";

            ret = ExecSQL(con_db, sql, true);

            sql =
                " INSERT INTO " + Points.Pref + sDataAliasRest + "prm_1" +
                "( nzp,       nzp_prm,         dat_s,    dat_po," +
                " val_prm, is_actual, cur_unl, nzp_wp, nzp_user," +
                " dat_when,  user_del)" +
                " SELECT DISTINCT k.nzp_kvar, p.nzp_prm,  a.dats_val , cast(a.datpo_val as date) , " +
                " a.val_prm as val_prm,   1 ,   1 ,  1, " + finder.nzp_user + ", " +
                sCurDate + " , " + finder.nzp_file +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls a, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_head b,  " +
                Points.Pref + DBManager.sUploadAliasRest + "file_kvar k ," +
                Points.Pref + DBManager.sUploadAliasRest + "file_typeparams p" +
                " WHERE p.id_prm= a.id_prm AND p.nzp_prm IS NOT NULL" +
                " AND a.nzp_file = b.nzp_file AND a.ls_id = k.id AND k.nzp_file = b.nzp_file " +
                " AND p.nzp_file=a.nzp_file AND  a.nzp_file =" + finder.nzp_file +
                " AND NOT EXISTS(" +
                "   SELECT 1 " +
                "   FROM " + Points.Pref + sDataAliasRest + "prm_1 pr " +
                "   WHERE pr.nzp = k.nzp_kvar " +
                "   AND pr.nzp_prm = p.nzp_prm " +
                "   AND pr.dat_s =  a.dats_val " +
                "   AND pr.dat_po = cast(a.datpo_val as date) " +
                "   AND pr.is_actual = 1)";

            //ret = ExecSQL(con_db, sql, true);            
            
            return ret;

        }

        private Returns WriteOneParamToPrmFromKvar(IDbConnection con_db, FilesDisassemble finder,
            int prm_table, DateTime date_s, string dat_po, string field, int nzp_prm, string where, string table_name = "file_kvar", string nzp_kvar = "nzp_kvar")
        {
            Returns ret = new Returns();
            int commandTime = 3600;
            string prm_t = sDataAliasRest + "prm_" + prm_table;

            //// изменить значение
            string sql = " update " + finder.bank + prm_t +
                " set  is_actual=100 " +
                " FROM " + finder.bank + prm_t + " a" +
                " ,"+Points.Pref + DBManager.sUploadAliasRest + table_name + " b " +
                " WHERE  b.nzp_file = " + finder.nzp_file + where +
                " and a.nzp_key =" + finder.bank + prm_t +".nzp_key "+
                " and  a.nzp=b." + nzp_kvar +
                " and  a.nzp_prm=" + nzp_prm +
                //" and  a.dat_po=cast('" + date_s.ToShortDateString() + "'as date) " +
                " and  a.dat_s=cast('" + date_s + "'as date) and a.is_actual=1 ";

            ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);
            // добавить справа и слева

            //sql =
            //    " INSERT INTO " + finder.bank + prm_t +
            //    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
            //    " SELECT DISTINCT " + field + "," + nzp_prm + "," + nzp_kvar + "," + "cast('" + date_s.ToShortDateString() + "'as date)," +
            //    " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
            //    " FROM " + Points.Pref + DBManager.sUploadAliasRest + table_name + " b " +
            //    " WHERE  nzp_file = " + finder.nzp_file + where +
            //    " and exists (select 1 from " + finder.bank + prm_t + " a where a.nzp=b." + nzp_kvar +
            //    " and  a.nzp_prm=" + nzp_prm +
            //    " and  a.dat_s=cast('" + date_s.ToShortDateString() + "'as date) " +
            //    " and  a.dat_po=cast('" + dat_po + "'as date) and a.is_actual=1 ) )";

            ////" and  " +finder.bank + prm_t+".dat_po<a.dat_s and a.is_actual=1 )";

            //ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);


            //sql =
            //    " INSERT INTO " + finder.bank + prm_t +
            //    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
            //    " SELECT DISTINCT " + field + "," + nzp_prm + "," + nzp_kvar + "," + "cast('" + date_s.ToShortDateString() + "'as date)," +
            //    " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
            //    " FROM " + Points.Pref + DBManager.sUploadAliasRest + table_name + " b " +
            //    " WHERE  nzp_file = " + finder.nzp_file + where +
            //    " and exists (select 1 from " + finder.bank + prm_t + " a where a.nzp=b." + nzp_kvar +
            //    " and  a.nzp_prm=" + nzp_prm +
            //    " and  a.dat_s>cast('" + dat_po + "'as date) and a.is_actual=1 ) )";
            ////" and  " +finder.bank + prm_t+".dat_po<a.dat_s and a.is_actual=1 )";

            //ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);

            //sql =
            //    " INSERT INTO " + finder.bank + prm_t +
            //    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
            //    " SELECT DISTINCT " + field + "," + nzp_prm + "," + nzp_kvar + "," + "cast('" + date_s.ToShortDateString() + "'as date)," +
            //    " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
            //    " FROM " + Points.Pref + DBManager.sUploadAliasRest + table_name + " b " +
            //    " WHERE  nzp_file = " + finder.nzp_file + where +
            //    " and exists (select 1 from " + finder.bank + prm_t + " a where a.nzp=b." + nzp_kvar +
            //    " and  a.nzp_prm=" + nzp_prm +
            //    " and  a.dat_po<cast('" + date_s.ToShortDateString() + "'as date) and a.is_actual=1 ) )";
            ////" and  " + finder.bank + prm_t + ".dat_s>a.dat_po and a.is_actual=1 )";

            //ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);


            sql =
                " INSERT INTO " + finder.bank + prm_t +
                " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
                " SELECT DISTINCT " + field + "," + nzp_prm + "," + nzp_kvar + "," + "cast('" +
                date_s.ToShortDateString() + "'as date)," +
                " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + table_name + " b " +
                " WHERE  nzp_file = " + finder.nzp_file + where+
                " and not exists (select 1 from " + finder.bank + prm_t + " a where a.nzp=b." + nzp_kvar +
                " and  a.nzp_prm=" + nzp_prm +
                " and  a.dat_po>=cast('" + dat_po + "'as date) and a.is_actual=1 " +
                " and  a.dat_s<=cast('" + date_s.ToShortDateString() + "'as date) and a.is_actual=1  )";
            //" and  " + finder.bank + prm_t + ".dat_s>a.dat_po and a.is_actual=1 )";

            ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);


            //// частичные новое шире слева меньше справа - добавить такой , сократить имеющийся по дате
            //sql =
            //    " update " + finder.bank + prm_t + " set dat_s=" + " cast('" + date_s.ToShortDateString() + "'as date)" +
            //    " from  " +

            //    "  (select nzp_key from " + finder.bank + prm_t + " a where a.nzp=b." + nzp_kvar +
            //    " and  a.nzp_prm=" + nzp_prm +
            //    " and  a.dat_po>cast('" + dat_po + "'as date) and a.is_actual=1 ) )" +
            //    " and  a.dat_s<cast('" + date_s.ToShortDateString() + "'as date) and a.is_actual=1 ) )";

            //ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);
            //sql =
            //                " update " + finder.bank + prm_t +
            //                " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
            //                " from  (" +
            //                " SELECT DISTINCT " + field + "," + nzp_prm + "," + nzp_kvar + "," +
            //                " cast('" + date_s.ToShortDateString() + "'as date)," +
            //                " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
            //                " FROM " + Points.Pref + DBManager.sUploadAliasRest + table_name +
            //                " WHERE  nzp_file = " + finder.nzp_file + where +
            //                " and exists (select 1 from " + finder.bank + prm_t + " a where " + finder.bank + prm_t + ".nzp=a.nzp " +
            //                " and  " + finder.bank + prm_t + ".nzp_prm=a.nzp_prm " +
            //                " and  " + finder.bank + prm_t + ".dat_s<a.dat_s " +
            //                " and  " + finder.bank + prm_t + ".dat_po<a.dat_po  ) )";

            //ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);
            

            //sql =
            //    " INSERT INTO " + finder.bank + prm_t +
            //    " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
            //    " SELECT DISTINCT " + field + "," + nzp_prm + "," + nzp_kvar + "," + "cast('" + date_s.ToShortDateString() + "'as date)," +
            //    " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
            //    " FROM " + Points.Pref + DBManager.sUploadAliasRest + table_name +
            //    " WHERE  nzp_file = " + finder.nzp_file + where +
            //    " and exists (select 1 from " + finder.bank + prm_t + " a where " + finder.bank + prm_t + ".nzp=a.nzp " +
            //    " and  " + finder.bank + prm_t + ".nzp_prm=a.nzp_prm " +
            //    " and  " + finder.bank + prm_t + ".dat_s>a.dat_po and a.is_actual=1 )";

            //ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);


            //sql =
            //                " update " + finder.bank + prm_t +
            //                " (val_prm, nzp_prm, nzp, dat_s, dat_po, is_actual, user_del, dat_when) " +
            //                " from  (" +
            //                " SELECT DISTINCT " + field + "," + nzp_prm + "," + nzp_kvar + "," + "cast('" + date_s.ToShortDateString() + "'as date)," +
            //                " cast('" + dat_po + "'as date),1," + finder.nzp_file + "," + sCurDate +
            //                " FROM " + Points.Pref + DBManager.sUploadAliasRest + table_name +
            //                " WHERE  nzp_file = " + finder.nzp_file + where +
            //                " and not exists (select 1 from " + finder.bank + prm_t + " a where " + finder.bank + prm_t + ".nzp=a.nzp " +
            //                " and  " + finder.bank + prm_t + ".nzp_prm=a.nzp_prm " +
            //                " and  " + finder.bank + prm_t + ".dat_s=a.dat_s " +
            //                " and  " + finder.bank + prm_t + ".dat_po=a.dat_po  ) )";

            //ret = DBManager.ExecSQL(con_db, null, sql, true, commandTime);


            return ret;
        }
        
        private static Returns SetNzpAreaGeuKvar(IDbConnection conn_db, FilesDisassemble finder, string pref)
        {
            Returns ret = new Returns(true);
            
            try
            {
                int noArea = DbDisUtils.GetNoAreaKod(conn_db, finder);
                int noGeu = DbDisUtils.GetNoGeuKod(conn_db, finder);
                string version = finder.versionFull;

                if (version == "1.2.1" || version == "1.2.2")
                {
                    //nzp_area, nzp_geu в kvar
                    string sql =
                        " UPDATE " + pref + sDataAliasRest + " kvar" +
                        " SET (nzp_area, nzp_geu) = " +
                        "   ((SELECT DISTINCT fa.nzp_area " +
                        "   FROM " + Points.Pref + sUploadAliasRest + "file_area fa," +
                        "   " + Points.Pref + sUploadAliasRest + "file_dom fd," +
                        "   " + Points.Pref + sUploadAliasRest + "file_kvar fk" +
                        "   WHERE fa.id = fd.area_id AND fk.dom_id = fd.id" +
                        "   AND fa.nzp_file = fk.nzp_file AND fd.nzp_file = fk.nzp_file " +
                        "   AND fk.nzp_file = " + finder.nzp_file + "" +
                        "   AND fk.id = remark )," + noGeu + ")" +
                        " WHERE " + sNvlWord + "(nzp_area, 0) = 0 " +
                        " AND " + sNvlWord + "(nzp_geu, " + noGeu + ") = " + noGeu;
                    DBManager.ExecSQL(conn_db, null, sql, true);
                }
                else //if (version == "1.3.2" || version == "1.3.3" || version == "1.3.4" || version == "1.3.5" || version == "1.3.6" || version == "1.3.7" || version == "1.3.8")
                {
                    MonitorLog.WriteLog("Старт обновления ссылок УК для ЛС ", MonitorLog.typelog.Info, true);
                    //nzp_area в kvar
                    string sql =
                        " UPDATE " + pref + sDataAliasRest + " kvar" +
                        " SET nzp_area = " +
                        sNvlWord + " ((SELECT DISTINCT a.nzp_area " +
                        "   FROM " + Points.Pref + sUploadAliasRest + "file_urlic fu," +
                        "   " + Points.Pref + sUploadAliasRest + "file_kvar fk," +
                        "   " + Points.Pref + sDataAliasRest + "s_area a" +
                        "   WHERE fu.urlic_id = fk.id_urlic_pass_dom AND fu.nzp_file = fk.nzp_file" +
                        "   AND fk.nzp_file = " + finder.nzp_file + "" +
                        "   AND fk.id = remark AND a.nzp_payer = fu.nzp_payer ), " + noArea + ") " +
                        " WHERE " + sNvlWord + "(nzp_area, " + noArea + ") = " + noArea;
                    DBManager.ExecSQL(conn_db, null, sql, true);

                    MonitorLog.WriteLog("Старт обновления ссылок на ЖЭУ для ЛС ", MonitorLog.typelog.Info, true);
                    //nzp_geu в kvar
                    sql =
                        " UPDATE " + pref + sDataAliasRest + " kvar" +
                        " SET nzp_geu = " +
                        "   (SELECT " + sNvlWord + "(nzp_geu, 0)" +
                        "   FROM " + Points.Pref + sDataAliasRest + "s_geu g," +
                        "   " + Points.Pref + sUploadAliasRest + "file_kvar fk" +
                        "   WHERE fk.nzp_kvar = nzp_kvar" +
                        "   AND fk.nzp_file = " + finder.nzp_file +
                        "   AND upper(trim(g.geu)) = upper(trim(fk.uch)) )" +
                        " WHERE " + sNvlWord + "(nzp_geu, 0) = 0";
                    DBManager.ExecSQL(conn_db, null, sql, true);

                    sql = 
                        " SELECT DISTINCT " + sNvlWord + "(trim(upper(uch)),'') as uch " +
                        " FROM " + Points.Pref + sUploadAliasRest + "file_kvar " +
                        " WHERE nzp_file = " + finder.nzp_file +
                        " AND uch <> '' " +
                        " AND NOT EXISTS" +
                        "   (SELECT 1 FROM " + Points.Pref + sDataAliasRest + "s_geu g" +
                        "   WHERE upper(trim(g.geu)) = upper(trim(uch)))";

                    DataTable geu = DBManager.ExecSQLToTable(conn_db, sql);
                    foreach (DataRow row in geu.Rows)
                    {
                        string seq = Points.Pref + sDataAliasRest + "s_geu_nzp_geu_seq";
#if PG
                        sql = " SELECT nextval('" + seq + "') ";
#else
                    sql = " SELECT " + seq + ".nextval FROM  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                        int newGeu =
                            Convert.ToInt32(
                                ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][0]);


                        sql = " INSERT INTO " + Points.Pref + sDataAliasRest + "s_geu" +
                              " ( nzp_geu, geu) " +
                              " VALUES " +
                              " (" + newGeu + ", '" + row["uch"].ToString().ToUpper().Trim() + "')";
                        ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                        sql = " INSERT INTO " + finder.bank + sDataAliasRest + "s_geu" +
                              " ( nzp_geu, geu) " +
                              " VALUES " +
                              " (" + newGeu + ", '" + row["uch"].ToString().ToUpper().Trim() + "')";
                        ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                        sql =
                            " UPDATE " + pref + sDataAliasRest + " kvar" +
                            " SET nzp_geu = " + newGeu +
                            " WHERE nzp_kvar in " +
                            "   (SELECT nzp_kvar " +
                            "   FROM " + Points.Pref + sUploadAliasRest + "file_kvar" +
                            "   WHERE upper(trim(uch)) = '" + row["uch"].ToString().ToUpper().Trim() +"'"+
                            "   AND nzp_file = " + finder.nzp_file +")" +
                            " AND " + sNvlWord + "(nzp_geu, 0) = 0";
                        ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                    }

                    sql =
                        " UPDATE " + pref + sDataAliasRest + " kvar" +
                        " SET nzp_geu = " + noGeu +
                        " WHERE " + sNvlWord + "(nzp_geu, 0) = 0";
                    ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции SetNzpAreaGeuKvar " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false);
            }
            MonitorLog.WriteLog("Успешно завершено обновление ссылок на ЖЭУ и УК для ЛС ", MonitorLog.typelog.Info, true);
            return ret;
        }
        
    }
}
