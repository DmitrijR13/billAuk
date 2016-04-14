using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.DataImport.SOURCE.LOAD;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    public class DbDisUtils : DataBaseHeadServer
    {

        private FilesDisassemble _finder = null;

        public DbDisUtils(FilesDisassemble finder)
        {
            this._finder = finder;
        }

        /// <summary>
        /// создание индексов
        /// </summary>
        /// <param name="conn_db"></param>
        public Returns CreateIndexForFileTables(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            IDbConnection _conDb = ServerConnection;

            try
            {
                Dictionary<string, List<string>> index = new Dictionary<string, List<string>>();
                DataImportUtils d = new DataImportUtils();

                #region file_area

                index.Clear();
                index.Add("ix2_file_area", new List<string>() {"nzp_file", "id", "nzp_area"});
                index.Add("ix3_file_area", new List<string>() {"nzp_file", "nzp_area"});

                d.CreateOneIndex(_conDb, Points.Pref + DBManager.sUploadAliasRest + "file_area", index);

                #endregion

                #region file_dom

                index.Clear();
                index.Add("ix2_file_dom", new List<string>() {"nzp_file", "nzp_dom"});

                d.CreateOneIndex(_conDb, Points.Pref + DBManager.sUploadAliasRest + "file_dom", index);

                #endregion

                #region file_gilec

                index.Clear();
                index.Add("ix2_file_gilec", new List<string>() {"nzp_file", "id", "comment"});

                d.CreateOneIndex(_conDb, Points.Pref + DBManager.sUploadAliasRest + "file_gilec", index);

                #endregion

                #region file_kvar                
                index.Clear();
                index.Add("ix2_file_kvar", new List<string>() {"nzp_file", "id"});
                index.Add("ix3_file_kvar", new List<string>() {"nzp_file", "nzp_kvar", "id"});
                index.Add("ix4_file_kvar", new List<string>() {"nzp_file", "id", "nzp_kvar"});

                d.CreateOneIndex(_conDb, Points.Pref + DBManager.sUploadAliasRest + "file_kvar", index);

                #endregion 

                #region file_serv

                index.Clear();
                index.Add("ix2_file_serv", new List<string>() {"nzp_file", "ls_id", "nzp_serv"});

                d.CreateOneIndex(_conDb, Points.Pref + DBManager.sUploadAliasRest + "file_serv", index);

                #endregion

                #region files_imported

                index.Clear();
                index.Add("ix2_files_imported", new List<string>() {"nzp_file"});

                d.CreateOneIndex(_conDb, Points.Pref + DBManager.sUploadAliasRest + "files_imported", index);

                #endregion

                //создаются в миграторе
                #region kvar в верхнем банке
                /*
                index.Clear();
                d.CreateUniqueIndex(_conDb, Points.Pref + DBManager.sDataAliasRest + "kvar", "ix202_1",
                    new List<string>() {"nzp_kvar"});

                index.Add("ix_kvar1", new List<string>() {"nkvar", "nkvar_n"});
                index.Add("ix_pkod", new List<string>() {"pkod"});
                index.Add("x_typek", new List<string>() {"typek"});
                index.Add("ix101_2", new List<string>() {"nzp_area"});
                index.Add("ix101_3", new List<string>() {"nzp_geu"});
                index.Add("ix161_2", new List<string>() {"nzp_dom"});
                index.Add("ix161_9", new List<string>() {"num_ls"});
                index.Add("ix207_23", new List<string>() {"ikvar"});
                index.Add("ixz_kv01", new List<string>() {"nzp_dom", "num_ls"});
                index.Add("kv_uch", new List<string>() {"uch"});
                index.Add("ik2", new List<string>() {"nzp_kvar", "remark"});

                d.CreateOneIndex(_conDb, Points.Pref + DBManager.sDataAliasRest + "kvar", index);
                */
                #endregion
                //создаются в миграторе
                #region kvar в нижних банках
                /*
                string pref = finder.bank;

                index.Clear();
                d.CreateUniqueIndex(_conDb, pref + DBManager.sDataAliasRest + "kvar", "ix202_1",
                    new List<string>() {"nzp_kvar"});

                index.Add("ix_kvar1", new List<string>() {"nkvar", "nkvar_n"});
                index.Add("ix_pkod", new List<string>() {"pkod"});
                index.Add("x_typek", new List<string>() {"typek"});
                index.Add("ix101_2", new List<string>() {"nzp_area"});
                index.Add("ix101_3", new List<string>() {"nzp_geu"});
                index.Add("ix161_2", new List<string>() {"nzp_dom"});
                index.Add("ix161_9", new List<string>() {"num_ls"});
                index.Add("ix207_23", new List<string>() {"ikvar"});
                index.Add("ixz_kv01", new List<string>() {"nzp_dom", "num_ls"});
                index.Add("kv_uch", new List<string>() {"uch"});
                index.Add("ik2", new List<string>() {"nzp_kvar", "remark"});

                d.CreateOneIndex(_conDb, pref + DBManager.sDataAliasRest + "kvar", index);
                */
                #endregion
            }
            catch
            {
                ret.result = false;
                ret.text = "Ошибка создания индексов";
                return ret;
            }

            return ret;
        }
        /// <summary>
        /// Функция формирования сальдо предыдущего месяца
        /// </summary>
        /// <param name="conDb"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns FormPrevMonthSaldo(IDbConnection conDb, FilesDisassemble finder)
        {
            Returns ret = new Returns();
            string sql;
            MonitorLog.WriteLog("Выбрано: 'Сформировать сальдо предыдущего месяца.'", MonitorLog.typelog.Info, true);
            MonitorLog.WriteLog("Старт формировнаия сальдо предыдущего месяца", MonitorLog.typelog.Info, true);

            //получаем  загружаемый месяц
            sql =
                    " SELECT h.calc_date as calc_date " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_head h " +
                    " WHERE h.nzp_file = " + finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();

            //получаем предыдущий месяц 
            DateTime prev_date = dt.Rows[0]["calc_date"].ToDateTime().AddMonths(-1);
            string year = prev_date.ToString().Substring(8, 2);
            string prev_month = prev_date.ToString().Substring(3, 2);

            try
            {
                //переносим данные из загружаемого месяца в предыдущий
                sql =
                    " INSERT INTO " + finder.bank + "_charge_" + year + 
                    tableDelimiter + "charge_" + prev_month +
                    " SELECT * " +
                    " FROM " + finder.bank + "_charge_" + (finder.year - 2000).ToString() +
                    tableDelimiter + "charge_" + finder.month.ToString("00") +
                    " WHERE order_print = " + finder.nzp_file;
                DBManager.ExecSQL(conDb, null, sql, true);

                sql =
                    " UPDATE " + finder.bank + "_charge_" + year +
                    tableDelimiter + "charge_" + prev_month +
                    " SET sum_outsaldo = sum_insaldo, " +
                    " real_charge = sum_insaldo" +
                    " WHERE real_charge <> 0 " +
                    " AND order_print = " + finder.nzp_file;
                DBManager.ExecSQL(conDb, null, sql, true);

                sql =
                    " INSERT INTO " + finder.bank + "_charge_" + year +
                    tableDelimiter + "perekidka" +
                    " (nzp_kvar, num_ls, nzp_serv, " +
                    "nzp_supp, type_rcl, date_rcl, " +
                    "tarif, volum, sum_rcl, " +
                    "month_, comment, nzp_user, nzp_reestr) " +
                    " SELECT nzp_kvar, num_ls ,nzp_serv, " +
                    "nzp_supp, -1 , cast( '" + prev_date + "' as date), " + 
                    "tarif, 1, real_charge, " +
                    prev_month + ", 'перекидка', " + finder.nzp_user + ", -2000" +
                    " FROM " + finder.bank + "_charge_" + year +
                    tableDelimiter + "charge_" + prev_month +
                    " WHERE real_charge <> 0 " +
                    " AND nzp_serv > 1" +
                    " AND order_print = " + finder.nzp_file;
                DBManager.ExecSQL(conDb, null, sql, true);
            }
            catch(Exception ex)
            {
                return new Returns(false, "Ошибка в функции FormPrevMonthSaldo: " + ex.Message);
            }

            ret.result = true;
            MonitorLog.WriteLog("Успешно выполнено формирование сальдо предыдущего месяца", MonitorLog.typelog.Info, true);
            return ret;
        }

        /// <summary>
        /// Установка значений переменным для разбора
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="disassLog"></param>
        /// <returns></returns>
        public Returns SetDisassemleVars(ref FilesDisassemble finder)
        {
            Returns ret = new Returns();
            IDbConnection _conDb = ServerConnection;
            MonitorLog.WriteLog("Старт установки значений переменным для разбора", MonitorLog.typelog.Info, true);
            try
            {
                string sql =
                    " SELECT nzp_file, h.calc_date as calc_date " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_head h " +
                    " WHERE h.nzp_file = " + finder.nzp_file;
                DataTable dt = ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).GetData();

                if (dt.Rows.Count != 1)
                {
                    return new Returns(false, "Для разбора должен быть выбран один файл! Выбрано:" + dt.Rows.Count, -1);
                }

                //Выставить сальдовый месяц
                string calcDate = "";
                if (dt.Rows[0]["calc_date"] != DBNull.Value)
                {
                    calcDate = dt.Rows[0]["calc_date"].ToString();
                    finder.month = Convert.ToInt32(calcDate.Substring(3, 2));
                    finder.year = Convert.ToInt32(calcDate.Substring(6, 4));
                }
                else
                {
                    return new Returns(false,
                        "В загружаемом файле не заполнено поле 'Месяц и год начислений' в 1 секции!", -1);
                }

                if(finder.prev_month_saldo)
                    MonitorLog.WriteLog("Выбран режим 'Сформировать сальдо предыдущего месяца.'", MonitorLog.typelog.Info, true);

                if (finder.is_last_month)
                {
                    finder.dat_po = "01.01.3000";
                    MonitorLog.WriteLog("Выбрано 'Последний загружаемый месяц.'",MonitorLog.typelog.Info, true);
                }
                else
                    finder.dat_po =
                        DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month))
                            .ToString()
                            .PadLeft(2, '0') + "." + finder.month.ToString().PadLeft(2, '0') + "." +
                        finder.year;

                MonitorLog.WriteLog("Выбран конец действия услуг и параметров (dat_po) = " + finder.dat_po, MonitorLog.typelog.Info, true);

                finder.nzp_file = Convert.ToInt32(dt.Rows[0]["nzp_file"]);
                MonitorLog.WriteLog("Выбран номер файла для разбора: " + finder.nzp_file, MonitorLog.typelog.Info, true);

                MonitorLog.WriteLog("Выбранный банк данных: " + finder.bank, MonitorLog.typelog.Info, true);

                sql =
                    " SELECT trim(version_name)" +
                    " FROM " + Points.Pref + sUploadAliasRest + "file_versions" +
                    " WHERE nzp_version = " +
                    " (SELECT nzp_version" +
                    "  FROM " + Points.Pref + sUploadAliasRest + "files_imported" +
                    "  WHERE nzp_file = " + finder.nzp_file + ")";
                finder.versionFull = DBManager.ExecScalar(ServerConnection, sql, out ret, true).ToString();
                MonitorLog.WriteLog("Версия файла: " + finder.versionFull, MonitorLog.typelog.Info, true);
                
                if (finder.delete_all_data)
                    DeleteAllData(finder, calcDate.Substring(0, 10));
                else if (!finder.reloaded_file)
                    //если не удаляем информацию, то обрезаем параметры и тарифы по ЛС из разбираемого файла
                    CutPrms(finder, calcDate.Substring(0, 10));
            }
            catch
            {
                return new Returns(false, "Ошибка установки значений переменным для разбора");
            }

            ret.result = true;
            MonitorLog.WriteLog("Успешно выполнено установка значений переменным для разбора", MonitorLog.typelog.Info, true); 
            return ret;
        }

        private void DeleteAllData(FilesDisassemble finder,string calcDate)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Выбрано: 'Удалить все данные перед разбором' ", MonitorLog.typelog.Info, true);
            MonitorLog.WriteLog("Старт удаления предыдущих данных перед разбором", MonitorLog.typelog.Info, true);
            
            //изменение статуса загрузки
            SetDissStatus(finder, "Удаление предыдущих данных");

            // Удалить данные  которые были загружены ранее из верхнего банка
            ret = DeleteFromOneTable(Points.Pref + sDataAliasRest + "prm_1", "user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(Points.Pref + sDataAliasRest + "prm_2", "user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(Points.Pref + sDataAliasRest + "prm_3", " nzp_prm = 51 AND user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(Points.Pref + sDataAliasRest + "prm_9", " nzp_prm = 51 AND user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(Points.Pref + sDataAliasRest + "counters", "user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(Points.Pref + sDataAliasRest + "counters_spis", "user_block = " + finder.nzp_file);
            ret = DeleteFromOneTable(Points.Pref + sDataAliasRest + "tarif", "user_del = " + finder.nzp_file);

            // Удалить данные  которые были загружены ранее из нижнего банка данных 
            ret = DeleteFromOneTable(finder.bank + sDataAliasRest + "prm_1", "user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(finder.bank + sDataAliasRest + "prm_2", "user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(finder.bank + sDataAliasRest + "prm_3", " nzp_prm = 51 AND user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(finder.bank + sDataAliasRest + "prm_9", " nzp_prm = 51 AND user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(finder.bank + sDataAliasRest + "counters", "user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(finder.bank + sDataAliasRest + "counters_spis", "user_block = " + finder.nzp_file);
            ret = DeleteFromOneTable(finder.bank + sDataAliasRest + "counters_dom", "user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(finder.bank + sDataAliasRest + "tarif", "user_del = " + finder.nzp_file);
            ret = DeleteFromOneTable(finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter + "charge_" +
                  finder.month.ToString("00"), "order_print = " + finder.nzp_file);

            CutPrms(finder, calcDate);
            MonitorLog.WriteLog("Успешное завершение удаления предыдущих данных ", MonitorLog.typelog.Info, true);
        }

        private Returns DeleteFromOneTable(string table, string where)
        {
            string sql = 
                " DELETE FROM " + table + 
                " WHERE " + where;
            return ExecSQL(sql);
        }

        private void CutPrms(FilesDisassemble finder, string calcDate)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт обрезки параметров и тарифов по ЛС", MonitorLog.typelog.Info, true);
            string str_minus_1_day;
            string str_public;
            string sql;

#if PG
            str_minus_1_day = " interval '1 day' ";
            str_public = " public.";
#else
            str_minus_1_day = " 1 units day ";
            str_public = " ";
#endif

            //prm_1
            CutOnePrm(finder, calcDate, "prm_1", "nzp_kvar", "file_kvar");
            //prm_2
            CutOnePrm(finder, calcDate, "prm_2", "nzp_dom", "file_dom");
            //prm_3
            CutOnePrm(finder, calcDate, "prm_3", "nzp_kvar", "file_kvar");
            //prm_9
            CutOnePrm(finder, calcDate, "prm_9", "nzp_payer", "file_urlic");
            //prm_11
            CutOnePrm(finder, calcDate, "prm_11", "nzp_supp", "file_supp");

            //tarif
            sql =
                "update " + Points.Pref + sDataAliasRest + "tarif set dat_po = " +
                "cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " where nzp_kvar in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ") " +
                "and dat_s < cast('" + calcDate + "' as date)" +
                " and dat_po = " + str_public + "MDY(1,1,3000)";
            ret = ExecSQL(sql, true);

            sql =
                " update " + finder.bank + sDataAliasRest + "tarif set dat_po = " +
                " cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " where nzp_kvar in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ") " +
                " and dat_s < cast('" + calcDate + "' as date)" +
                " and dat_po = " + str_public + "MDY(1,1,3000)";
            ret = ExecSQL(sql, true);

            string tarif_table_up = Points.Pref + sDataAliasRest + "tarif ";

            sql =
                " update " + tarif_table_up + 
                " set is_actual = 100 " +
                " where dat_s = '" + calcDate + "'" +
                " and exists " +
                " (select fk.nzp_kvar " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_serv fs," +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_services s, " +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_dog d " +
                " WHERE fs.ls_id= fk.id and fs.nzp_file = fk.nzp_file AND s.id_serv = fs.nzp_serv" +
                " and s.nzp_file = fs.nzp_file and d.dog_id=fs.dog_id and d.nzp_file=fs.nzp_file and d.nzp_supp>0 " +
                " AND fk.nzp_kvar = " + tarif_table_up + ".nzp_kvar  AND s.nzp_serv = " + tarif_table_up + ".nzp_serv " +
                " AND d.nzp_supp = " + tarif_table_up + ".nzp_supp " +
                " and fk.nzp_file = " + finder.nzp_file + " )";
            ret = ExecSQL(sql, true);


            string tarif_table_low = finder.bank + sDataAliasRest + "tarif ";

            sql =
                " update " + tarif_table_low +
                " set is_actual = 100 " +
                " where dat_s = '" + calcDate + "'" +
                " and exists " +
                " (select fk.nzp_kvar " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_serv fs," +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_services s, " +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_dog d " +
                " WHERE fs.ls_id= fk.id and fs.nzp_file = fk.nzp_file AND s.id_serv = fs.nzp_serv" +
                " and s.nzp_file = fs.nzp_file and d.dog_id=fs.dog_id and d.nzp_file=fs.nzp_file and d.nzp_supp>0 " +
                " AND fk.nzp_kvar = " + tarif_table_low + ".nzp_kvar  AND s.nzp_serv = " + tarif_table_low + ".nzp_serv " +
                " AND d.nzp_supp = " + tarif_table_low + ".nzp_supp " +
                " and fk.nzp_file = " + finder.nzp_file + " )";
            ret = ExecSQL(sql, true);

            //charge_mm

            string charge_table = finder.bank + "_charge_" + (finder.year - 2000) +
                                  tableDelimiter + "charge_" + finder.month.ToString("00");

            sql =
                " delete from  " + charge_table +
                " where exists " +
                " (select fk.nzp_kvar " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_serv fs," +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_services s, " +
                " " + Points.Pref + DBManager.sUploadAliasRest + "file_dog d " +
                " WHERE fs.ls_id= fk.id and fs.nzp_file = fk.nzp_file AND s.id_serv = fs.nzp_serv" +
                " and s.nzp_file = fs.nzp_file and d.dog_id=fs.dog_id and d.nzp_file=fs.nzp_file and d.nzp_supp>0 " +
                " AND fk.nzp_kvar = " + charge_table + ".nzp_kvar  AND s.nzp_serv = " + charge_table + ".nzp_serv " +
                " AND d.nzp_supp = " + charge_table + ".nzp_supp " +
                " and fk.nzp_file = " + finder.nzp_file + " )";
            ret = ExecSQL(sql);
        }
        
        private Returns CutOnePrm(FilesDisassemble finder, string calcDate, string del_table, string nzp_field, string nzp_table )
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт обрезки параметров в таблице " + del_table, MonitorLog.typelog.Info, true);

#if PG
            string str_minus_1_day = " interval '1 day'" ;
            string str_public = " public.";
#else
            string str_minus_1_day = " 1 units day ";
            string str_public = " ";
#endif

            string sql =
               "update " + finder.bank + sDataAliasRest + del_table + " set dat_po = " +
               "cast('" + calcDate + "' as date) -" +
               str_minus_1_day +
               " where nzp in" +
               " (select " + nzp_field + " from " + Points.Pref + DBManager.sUploadAliasRest + nzp_table +
               " where nzp_file = " + finder.nzp_file + ")" +
               " and user_del not in (" + finder.nzp_file + ") " +
               " and dat_s < cast('" + calcDate + "' as date)" +
               " and dat_po = " + str_public + "MDY(1,1,3000)";
            ExecSQL(sql);

            sql =
                "update " + Points.Pref + sDataAliasRest + del_table + " set dat_po = " +
                "cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " where nzp in" +
                " (select " + nzp_field + " from " + Points.Pref + DBManager.sUploadAliasRest + nzp_table +
                " where nzp_file = " + finder.nzp_file + ")" +
                " and user_del not in (" + finder.nzp_file + ") " +
                " and dat_s < cast('" + calcDate + "' as date)" +
                " and dat_po = " + str_public + "MDY(1,1,3000)";
            ExecSQL(sql);

            sql =
                " delete from  " + Points.Pref + sDataAliasRest + del_table +
                " where dat_s = '" + calcDate + "'" +
                " and nzp in " +
                " (select " + nzp_field + " from " + Points.Pref + DBManager.sUploadAliasRest + nzp_table +
                " where nzp_file = " + finder.nzp_file + ")";
            ExecSQL(sql);

            sql =
                " delete from  " + finder.bank + sDataAliasRest + del_table +
                " where dat_s = '" + calcDate + "'" +
                " and nzp in" +
                " (select " + nzp_field + " from " + Points.Pref + DBManager.sUploadAliasRest + nzp_table +
                " where nzp_file = " + finder.nzp_file + ")";
            ExecSQL(sql);
            return ret;
        }

        public Returns CheckDisHouse(FilesDisassemble finder, StringBuilder disassLog)
        {
            Returns ret = new Returns();
            IDbConnection _conDb = ServerConnection;
            string sql =
                    " select count(*)  as kol_notdis " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom a " +
                    " where nzp_dom is null and a.nzp_file= " + finder.nzp_file;
            int kol_notdis =
                Convert.ToInt32(
                    ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                        "kol_notdis"]);
            if (kol_notdis > 0)
            {
                disassLog.Append("Ошибка разбора! " + ret.text + Environment.NewLine);
                return new Returns(false, " Согласованы не все дома! Кол-во несогласованных: " + kol_notdis, -1);
            }

            return new Returns(true);
        }

        public Returns CheckDisKvar(FilesDisassemble finder, StringBuilder disassLog)
        {
            Returns ret = new Returns(true);
            try
            {
                string sql =
                    " select count(a.nzp_file)   as kol_notdis " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar a " +
                    " where a.nzp_kvar is null and a.nzp_file = " + finder.nzp_file;
                int kol_notdis =
                    Convert.ToInt32(
                        ClassDBUtils.OpenSQL(sql, ServerConnection, ClassDBUtils.ExecMode.Exception).
                        GetData().Rows[0]["kol_notdis"]);

                if (kol_notdis > 0)
                {
                    MonitorLog.WriteLog(
                        "Ошибка при разборе file_kvar. " + ret.text + " из файла nzp_file=" + finder.nzp_file +
                        "; процедура DisassembleFileVers132.", MonitorLog.typelog.Error, true);
                    return new Returns(false, " Согласованы не все лицевые счета! Кол-во несогласованных: " + kol_notdis, -1);
                }
            }
            catch
            {
                MonitorLog.WriteLog(" Ошибка проверки согласованности ЛС ", MonitorLog.typelog.Error, true);
                return new Returns(false, " Ошибка проверки согласованности ЛС ", -1);
            }

            return ret;
        }

        public Returns SetDissStatus(FilesDisassemble finder, string status)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog(status, MonitorLog.typelog.Info, true);
            string sql = 
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest +"files_imported" +
                " SET diss_status = '" + status + "' " +
                " WHERE nzp_file = " + finder.nzp_file;
            ret = ExecSQL(sql);
            return ret;
        }

        public Returns SettingsForGKU(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт тонкой настройки (ф-ция SettingsForGKU) ", MonitorLog.typelog.Info, true);
            string sql;
            string charge_XX = finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter +
                "charge_" + finder.month.ToString("00");
            string calc_gku_XX = finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter +
                "calc_gku_" + finder.month.ToString("00");
            string gil_XX = finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter +
                "gil_" + finder.month.ToString("00");

            SetDissStatus(finder, "Тонкая настройка");

            try
            {
                sql =
                    " DROP TABLE t_settings_gku_tarif_1";
                ret = ExecSQL(sql, false);
            }
            catch { }
            sql =
                " CREATE TEMP TABLE t_settings_gku_tarif_1 ( " +
                " nzp_kvar INTEGER," +
                " nzp_supp INTEGER," +
                " nzp_serv INTEGER," +
                " nzp_frm INTEGER)";
            ret = ExecSQL(sql);

            sql =
                " INSERT INTO t_settings_gku_tarif_1" +
                " (nzp_kvar, nzp_supp, nzp_serv, nzp_frm)" +
                " SELECT DISTINCT nzp_kvar, nzp_supp, nzp_serv, nzp_frm" +
                " FROM " + finder.bank + sDataAliasRest + "tarif" +
                " WHERE dat_s <= '01." + finder.month.ToString("00") + "." + finder.year + "' " +
                " AND dat_po> '01." + finder.month.ToString("00") + "." + finder.year + "'" +
                " AND is_actual <> 100";
            ret = ExecSQL(sql);

            sql =
                " CREATE INDEX inx_settings_gku_tarif_1 " +
                " ON t_settings_gku_tarif_1(nzp_kvar, nzp_supp, nzp_serv)";
            ret = ExecSQL(sql);

            sql = sUpdStat + " t_settings_gku_tarif_1";
            ret = ExecSQL(sql);

            sql =
                " UPDATE " + charge_XX +
                " SET nzp_frm =" +
                "   ((SELECT MAX(t.nzp_frm)" +
                "   FROM t_settings_gku_tarif_1 t " +
                "   WHERE t.nzp_kvar = nzp_kvar AND t.nzp_serv = nzp_serv AND t.nzp_supp = nzp_supp ))" +
                " WHERE nzp_frm is null";
            ret = ExecSQL(sql);

            sql =
                " UPDATE " + charge_XX +
                " SET gsum_tarif = rsum_tarif" +
                " WHERE gsum_tarif <> rsum_tarif";
            ret = ExecSQL(sql);


            sql = sUpdStat + " " + charge_XX;
            ret = ExecSQL(sql);

            sql =
                " ANALYZE " + calc_gku_XX;
            ExecSQL(sql);

            sql =
                " INSERT INTO " + calc_gku_XX +
                " (nzp_dom, nzp_kvar, nzp_serv, nzp_supp, nzp_frm, nzp_prm_tarif," +
                " nzp_prm_rashod, tarif, rashod, rashod_norm, rashod_g, gil," +
                " gil_g, squ, trf1, trf2, trf3, trf4, rsh1, rsh2, rsh3, nzp_prm_trf_dt," +
                " tarif_f, rash_norm_one, valm, dop87, dlt_reval, is_device, nzp_frm_typ," +
                " nzp_frm_typrs)" +
                " SELECT k.nzp_dom ,k.nzp_kvar, nzp_serv, nzp_supp, nzp_frm, 0," +
                " 0, tarif, c_calc, 0, c_calc, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0," +
                " 0, 0, c_calc, 0, 0, 0, 0, 0" +
                " FROM " + charge_XX + " a, " +
                finder.bank + sDataAliasRest + "kvar k" +
                " WHERE a.nzp_kvar = k.nzp_kvar AND tarif > 0;";
            ret = ExecSQL(sql);


            sql = "drop table _t_gku";
            ret = ExecSQL(sql);

            sql = "select nzp_kvar, nzp_serv ,nzp_supp, (select nzp_serv_link from " + finder.bank + sKernelAliasRest + ".serv_odn a where a.nzp_serv=b.nzp_serv) nzp_serv_link,"+
                  " sum(rashod) rashod  into temp _t_gku  " +
                  " from " + calc_gku_XX + " b  where nzp_serv in  (select nzp_serv from " + finder.bank + sKernelAliasRest + ".serv_odn) group by 1,2,3,4 ";
            ret = ExecSQL(sql);

            sql =
               " update " + calc_gku_XX + " set dop87 =coalesce((select sum(rashod) from _t_gku a   " +
               " where " + calc_gku_XX + ".nzp_serv=a.nzp_serv_link  and a.nzp_kvar =" + calc_gku_XX + ".nzp_kvar " +
               " and a.nzp_serv =" + calc_gku_XX + ".nzp_serv and a.nzp_supp=" + calc_gku_XX + ".nzp_supp ),0) " +
               " where nzp_serv in (select nzp_serv_link from " + finder.bank + sKernelAliasRest + ".serv_odn) and " +
               " exists (select 1 from _t_gku a where " + calc_gku_XX + ".nzp_serv=a.nzp_serv_link  and a.nzp_kvar =" +
                calc_gku_XX + ".nzp_kvar " +
               " and a.nzp_serv =" + calc_gku_XX + ".nzp_serv and a.nzp_supp=" + calc_gku_XX + ".nzp_supp)";

            ret = ExecSQL(sql);
          

            try
            {
                sql =
                    " DROP TABLE t_settings_gku_prm_1";
                ret = ExecSQL(sql, false);
            }
            catch { }
            sql =
                " CREATE TEMP TABLE t_settings_gku_prm_1 ( " +
                " nzp_kvar INTEGER," +
                " nzp_prm INTEGER," +
                " val_prm CHAR(20) )";
            ret = ExecSQL(sql);

            sql =
                " INSERT INTO t_settings_gku_prm_1 " +
                " (nzp_kvar, nzp_prm, val_prm)" +
                " SELECT DISTINCT nzp, nzp_prm, val_prm " +
                " FROM " + finder.bank + sDataAliasRest + "prm_1" +
                " WHERE dat_s <= '01." + finder.month.ToString("00") + "." + finder.year + "' " +
                " AND dat_po> '01." + finder.month.ToString("00") + "." + finder.year + "'" +
                " AND is_actual <> 100" +
                " AND nzp_prm in (4, 5)"; 
            ret = ExecSQL(sql);

            sql =
                " CREATE INDEX inx_settings_gku_prm_1 " +
                " ON t_settings_gku_prm_1(nzp_kvar, nzp_prm)";
            ret = ExecSQL(sql);

            sql = sUpdStat + " t_settings_gku_prm_1";
            ret = ExecSQL(sql);

            sql =
                " UPDATE " + calc_gku_XX +
                " SET gil = " + sNvlWord +
                "   ((SELECT MAX(val_prm " + DBManager.sConvToInt + ")" +
                "   FROM t_settings_gku_prm_1 p" +
                "   WHERE " + calc_gku_XX + ".nzp_kvar=p.nzp_kvar" +
                "   AND p.nzp_prm = 5 ),0);";
            ret = ExecSQL(sql);

            sql =
                " UPDATE " + calc_gku_XX +
                " SET squ = " + sNvlWord +
                "   ((select max(val_prm " + sConvToNum + " )" +
                "   FROM t_settings_gku_prm_1 p" +
                "   WHERE " + calc_gku_XX + ".nzp_kvar = p.nzp_kvar" +
                "   AND p.nzp_prm=4 ),0);";
            ret = ExecSQL(sql);

            sql =
                " UPDATE " + calc_gku_XX +
                " SET gil_g=gil ";
            ret = ExecSQL(sql);

            sql =
                " DELETE FROM " + gil_XX;
            ret = ExecSQL(sql);

            sql =
                " INSERT INTO " + gil_XX +
                " ( nzp_dom, nzp_kvar, cur_zap, nzp_gil, " +
                " dat_s, dat_po, stek," +
                " cnt1, cnt2, cnt3, val1, val2, val3, val4, val5, kod_info)" +
                " SELECT  nzp_dom, nzp_kvar,  0, 0, " +
                " '01." + finder.month.ToString("00") + "." + finder.year + "', " +
                " '" + DateTime.DaysInMonth(finder.year, finder.month) + "." + finder.month.ToString("00") + "." +
                finder.year + "', 3, " +
                " max(gil), max(gil), 0, max(gil), max(gil), '0', '0', '0', 0" +
                " FROM " + calc_gku_XX +
                " GROUP BY 1,2;";
            ret = ExecSQL(sql);
            return ret;
        }

        /// <summary>
        /// Заполнение таблицы l_foss уникальным значением из таблицы tarif
        /// </summary>
        /// <param name="finder"></param>
        public void LoadFromTarifToL_foss(FilesDisassemble finder)
        {
            try
            {
                string sql;

                sql = " INSERT INTO " + finder.bank + sKernelAliasRest +
                      " l_foss (nzp_serv, nzp_supp, nzp_frm, dat_s, dat_po) " +
                      " SELECT nzp_serv, nzp_supp, nzp_frm, '01.01.2000', '01.01.3000' " +
                      " FROM " + finder.bank + sDataAliasRest + " tarif t " +
                      " WHERE NOT EXISTS (" +
                      " SELECT 1  FROM " + finder.bank + sKernelAliasRest + "l_foss l " +
                      " WHERE t.nzp_serv = l.nzp_serv" +
                      " AND t.nzp_supp = l.nzp_supp " +
                      " AND t.nzp_frm = l.nzp_frm)" +
                      " AND nzp_kvar in (" +
                      " SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar WHERE nzp_file = " + finder.nzp_file + ")" +
                      " GROUP BY 1, 2, 3";
                ExecSQL(sql);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при заполнении таблицы l_foss уникальным сочетанием из таблицы tarif.\n :" + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

        public Returns InsertNedop(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            string sql;

            //изменение статуса загрузки
            SetDissStatus(finder, "Загрузка недопоставок");

            try
            {
                sql = 
                    " INSERT INTO " + finder.bank + sDataAliasRest + "nedop_kvar" +
                    " (nzp_kvar, nzp_serv, nzp_supp, dat_s, dat_po, is_actual, nzp_user," +
                    " dat_when, nzp_kind, cur_unl, month_calc, user_del, tn)" +
                    " SELECT fk.nzp_kvar, fs.nzp_serv, 1, fn.dat_nedstart," +
                    " fn.dat_nedstop, 1, " + finder.nzp_user + "," +
                     sCurDate + ",  fn.type_ned, 1, fh.calc_date, fn.nzp_file, fn.percent" +
                     " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost fn, " +
                     Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                     Points.Pref + DBManager.sUploadAliasRest + "file_services fs, " +
                     Points.Pref + DBManager.sUploadAliasRest + "file_head fh " +
                     " WHERE fn.nzp_file = fk.nzp_file AND fk.nzp_file = fs.nzp_file " +
                     " AND fs.nzp_file = fh.nzp_file AND fn.nzp_file =" + finder.nzp_file +
                     " AND cast (fn.id_serv as integer) = fs.id_serv " +
                     " AND fk.id = fn.ls_id ";
                ret = ExecSQL(sql);
                if (ret.result)
                {
                    sql = 
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "nedop_kvar" +
                        " (nzp_kvar, nzp_serv, nzp_supp, dat_s, " +
                        " dat_po, tn, comment, is_actual, nzp_user," +
                        " dat_when, act_no, nzp_kind)" +
                        " SELECT nzp_kvar, nzp_serv, nzp_supp, dat_s, " +
                        " dat_po, tn, comment, is_actual, nzp_user," +
                        " dat_when, act_no, nzp_kind" +
                        " FROM " + finder.bank + sDataAliasRest + "nedop_kvar " +
                        " WHERE user_del = " + finder.nzp_file;
                    ret = ExecSQL(sql);
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры insertNedop " + ret.text, MonitorLog.typelog.Error, true);
                    return new Returns(false, "Ошибка выполнения процедуры insertNedop ", -1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка выполнения процедуры insertNedop " + "\n" + ex.Message + "\n" + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка выполнения процедуры insertNedop ", -1);
            }
            return new Returns(true, "Данные сохранены ", 1);
        }

        /// <summary>
        /// Ф-ция проверки колонки на наличие пустых значений
        /// </summary>
        /// <param name="columnName">Название колонки для проверки</param>
        /// <param name="tblName">Таблица, в которой проверяем</param>
        /// <param name="message">Сбщ, выводимое в лог </param>
        public Returns CheckColumnOnEmptiness(string columnName, string tblName, string message, MonitorLog.typelog typeLog = MonitorLog.typelog.Error)
        {
            Returns ret = new Returns();

            MonitorLog.WriteLog("Запущена проверка '" + columnName + "' в таблице '" + tblName + "' на наличие пустых значений", MonitorLog.typelog.Info, true);
            
            int emptyRecordsCnt = 0;
            string sql =
                " SELECT COUNT(*) as count FROM " + Points.Pref + DBManager.sUploadAliasRest + tblName + " " +
                " WHERE nzp_file = " + _finder.nzp_file +
                " AND " + columnName + " IS NULL ";
            emptyRecordsCnt = Convert.ToInt32(OpenSQL(sql).resultData.Rows[0]["count"]);
            if (emptyRecordsCnt > 0)
            {
                MonitorLog.WriteLog("[!] Имеются " + message + " в кол-ве: " + emptyRecordsCnt, typeLog, true);
                ret.text = "[!] Имеются " + message + " в кол-ве: " + emptyRecordsCnt;
                ret.result = false;
                return ret;
            }
            ret.result = true;
            return ret;
        }

        public static int GetNoGeuKod(IDbConnection conn_db, FilesDisassemble finder)
        {
            Returns ret = new Returns();
            int noGeu;
            string sql;
            //смотрим в таблице значений по умолчанию
            sql = " DELETE FROM " + Points.Pref + sUploadAliasRest + "default_values" +
                  " WHERE id = 2  AND upper(trim(field_name)) = 'КОД ЖЭУ'" +
                  " AND field_value NOT IN" +
                  " (SELECT nzp_geu FROM " + Points.Pref + sDataAliasRest + "s_geu )";
            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

            sql = " DELETE FROM " + Points.Pref + sUploadAliasRest + "default_values" +
                  " WHERE id = 2  AND upper(trim(field_name)) = 'КОД ЖЭУ'" +
                  " AND field_value NOT IN" +
                  " (SELECT nzp_geu FROM " + finder.bank + sDataAliasRest + "s_geu )";
            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

            sql =
                " SELECT " + sNvlWord + "(field_value, 0) as field_value " +
                " FROM " + Points.Pref + sUploadAliasRest + "default_values" +
                " WHERE id = 2  AND upper(trim(field_name)) = 'КОД ЖЭУ'";
            if (ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count > 0)
                noGeu =
                    Convert.ToInt32(
                        ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                            "field_value"]);
            else noGeu = 0;

            if (noGeu == 0)
            {
                //ищем УК 'НЕТ СВЕДЕНИЙ ОБ УК' в верхнем банке
                sql =
                    " SELECT nzp_geu " +
                    " FROM " + Points.Pref + sDataAliasRest + "s_geu " +
                    " WHERE upper(trim(geu)) = 'НЕТ СВЕДЕНИЙ О ЖЭУ'";
                if (ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count > 0)
                {
                    noGeu =
                        Convert.ToInt32(
                            ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                                "nzp_geu"]);
                    sql =
                        " INSERT INTO " + finder.bank + sDataAliasRest + "s_geu ( nzp_geu, geu) " +
                        " SELECT nzp_geu, geu FROM " + Points.Pref + sDataAliasRest + "s_geu " +
                        " WHERE upper(trim(geu)) = 'НЕТ СВЕДЕНИЙ О ЖЭУ' ";
                    DBManager.ExecSQL(conn_db, sql, false);
                }
                if (noGeu <= 0)
                {
                    //добавление  УК 'НЕТ СВЕДЕНИЙ О ЖЭУ' по sequence в верхний и нижний банки, в таблицу значений по умолчанию
                    string seq = Points.Pref + sDataAliasRest + "s_geu_nzp_geu_seq";
#if PG
                    sql = " SELECT nextval('" + seq + "') ";
#else
                    sql = " SELECT " + seq + ".nextval FROM  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                    noGeu =
                        Convert.ToInt32(ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][0]);


                    sql = " INSERT INTO " + Points.Pref + sDataAliasRest + "s_geu" +
                          " ( nzp_geu, geu) " +
                          " VALUES " +
                          " (" + noGeu + ", 'НЕТ СВЕДЕНИЙ О ЖЭУ')";
                    ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                    sql = " INSERT INTO " + finder.bank + sDataAliasRest + "s_geu" +
                          " ( nzp_geu, geu) " +
                          " VALUES " +
                          " (" + noGeu + ", 'НЕТ СВЕДЕНИЙ О ЖЭУ')";
                    ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                }

                //добавление в таблицу по умолчанию
                sql =
                    " INSERT INTO " + Points.Pref + sUploadAliasRest + "default_values" +
                    " (id, field_name, field_value) " +
                    " VALUES" +
                    " (2, 'КОД ЖЭУ', " + noGeu + ")";
                ret = DBManager.ExecSQL(conn_db, null, sql, true);
            }

            return noGeu;
        }

   

        public static int GetNoAreaKod(IDbConnection conn_db, FilesDisassemble finder)
        {
            Returns ret = new Returns();
            int noArea;
            int nzp_payer = 0;
            string sql;
            //смотрим в таблице значений по умолчанию
            sql = " DELETE FROM " + Points.Pref + sUploadAliasRest + "default_values" +
                  " WHERE id = 1 AND upper(trim(field_name)) = 'КОД УК'" +
                  " AND field_value NOT IN" +
                  " (SELECT nzp_area FROM " + Points.Pref + sDataAliasRest + "s_area )";
            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

            sql = " DELETE FROM " + Points.Pref + sUploadAliasRest + "default_values" +
                  " WHERE id = 1 AND upper(trim(field_name)) = 'КОД УК'" +
                  " AND field_value NOT IN" +
                  " (SELECT nzp_area FROM " + finder.bank + sDataAliasRest + "s_area )";
            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

            sql =
                " SELECT " + sNvlWord + "(field_value, 0) as field_value " +
                " FROM " + Points.Pref + sUploadAliasRest + "default_values" +
                " WHERE id = 1 AND upper(trim(field_name)) = 'КОД УК'";
            if (ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count > 0)
            {
                noArea =
                    Convert.ToInt32(
                        ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                            "field_value"]);
            }
            else noArea = 0;

            //определение фиктивного контрагента
            sql =
                        " SELECT MIN(nzp_payer) as nzp_payer " +
                        " FROM " + Points.Pref + sKernelAliasRest + "s_payer " +
                        " WHERE lower(replace(payer, ' ', '')) like '%фиктивный%';";
            if (ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count > 0 &&
                ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                    "nzp_payer"] != DBNull.Value)
            {
                nzp_payer = Convert.ToInt32(
                    ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                        "nzp_payer"]);
            }
            else
            {
                sql =
                    " INSERT INTO " + Points.Pref + sKernelAliasRest + "s_payer (payer, npayer, is_erc)" +
                    "  VALUES ('Фиктивный контрагент', 'Фиктивный контрагент', 0)";
                DBManager.ExecSQL(conn_db, sql, true);
                nzp_payer = DBManager.GetSerialValue(conn_db);
            }
           

            if (noArea == 0)
            {
                

                //ищем УК 'НЕТ СВЕДЕНИЙ ОБ УК' в верхнем банке
                sql =
                    " SELECT MIN(nzp_area) as nzp_area " +
                    " FROM " + Points.Pref + sDataAliasRest + "s_area " +
                    " WHERE upper(trim(area)) like '%НЕТ СВЕДЕНИЙ%'";
                if (ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows.Count > 0 && ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                                "nzp_area"] != DBNull.Value)
                {
                    noArea =
                        Convert.ToInt32(
                            ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                                "nzp_area"]);
                    

                    sql =
                        " INSERT INTO " + finder.bank + sDataAliasRest + "s_area (nzp_area, area, nzp_payer) " +
                        " SELECT nzp_area, area," + nzp_payer + " FROM " + Points.Pref + sDataAliasRest + "s_area " +
                        " WHERE upper(trim(area)) = 'НЕТ СВЕДЕНИЙ ОБ УК";
                    DBManager.ExecSQL(conn_db, sql, false);
                }
                if (noArea <= 0)
                {
                    //добавление  УК 'НЕТ СВЕДЕНИЙ ОБ УК' по sequence в верхний и нижний банки, в таблицу значений по умолчанию
                    string seq = Points.Pref + sDataAliasRest + "s_area_nzp_area_seq";
#if PG
                    sql = " SELECT nextval('" + seq + "') ";
#else
                    sql = " SELECT " + seq + ".nextval FROM  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                    noArea =
                        Convert.ToInt32(ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][0]);


                    sql = " INSERT INTO " + Points.Pref + sDataAliasRest + "s_area" +
                          " ( nzp_area, area, nzp_payer) " +
                          " VALUES " +
                          " (" + noArea + ", 'НЕТ СВЕДЕНИЙ ОБ УК', " + nzp_payer + ")";
                    ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);

                    sql = " INSERT INTO " + finder.bank + sDataAliasRest + "s_area" +
                          " ( nzp_area, area, nzp_payer) " +
                          " VALUES " +
                          " (" + noArea + ", 'НЕТ СВЕДЕНИЙ ОБ УК', " + nzp_payer + ")";
                    ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                }

                //добавление в таблицу по умолчанию
                sql =
                    " INSERT INTO " + Points.Pref + sUploadAliasRest + "default_values" +
                    " (id, field_name, field_value) " +
                    " VALUES" +
                    " (1, 'КОД УК', " + noArea + ")";
                ret = DBManager.ExecSQL(conn_db, null, sql, true);
            }

            return noArea;
        }

        public void Test()
        {
            Parameter p = Parameter.GetInstance(ParameterTypes.Int32);
            p.AddFilter(ParameterFilters.IsNull, false);
            p.Check();
            var log = p.ToString();
        }

        /// <summary>
        /// Функция вставки признака перерасчета в must_calc
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db">Соединение</param>
        /// <param name="dat_uchet_p">Дата учета показания</param>
        /// <param name="pnzp_counter">Код счетчика</param>
        /// <param name="nzp_kvar">Код квартиры</param>
        /// <param name="nzp_serv">Код услуги</param>   
        /// <param name="nzp_type">Код типа счетчика</param>  
        /// <param name="nzp_dom">Код дома</param>
        /// <param name="table">Наим. таблицы с показаниями</param>
        public void InsertIntoMustCalc(FilesDisassemble finder, IDbConnection conn_db, string dat_uchet_p, int nzp_counter, int nzp_kvar, int nzp_serv, int nzp_type, int nzp_dom, string table)
        {
            string sql;
            IDataReader reader;
            DateTime? DatS = null;
            DateTime? DatPo = null;
            DateTime dat_uchet = Convert.ToDateTime(dat_uchet_p);
            RecordMonth calcMonth = Points.GetCalcMonth(new CalcMonthParams(finder.pref));

            //Определяем дату начала перерасчета
            sql =
                " SELECT MAX(dat_uchet) as dat_uchet " +
                " FROM " + finder.bank + "_data" + tableDelimiter + table + 
                " WHERE nzp_counter = " + nzp_counter +
                " AND dat_uchet < " + STCLINE.KP50.Global.Utils.EStrNull(dat_uchet.ToShortDateString()) +
                " AND is_actual <> 100";
            ExecRead(conn_db, out reader, sql, true);
            if (reader.Read())
            {
                if (reader["dat_uchet"] != DBNull.Value) DatS = Convert.ToDateTime(reader["dat_uchet"]);
            }
            reader.Close();
            reader.Dispose();
            if (DatS == null) DatS = dat_uchet;

            //Определим дату окончания перерасчета
            //если месяц начала перерасчета предшествует текущему расчетному месяцу, то определим месяц окончания перерасчета
            if (DatS < new DateTime(calcMonth.year_, calcMonth.month_, 1))
            {
                //Определим последующее показание, чтобы установить месяц окончания перерасчета
                sql =
                    " SELECT MIN(dat_uchet) as dat_uchet " +
                    " FROM " + finder.bank + "_data" + tableDelimiter + table + 
                    " WHERE nzp_counter = " + nzp_counter +
                    " AND dat_uchet > " + STCLINE.KP50.Global.Utils.EStrNull(dat_uchet.ToShortDateString()) +
                    " AND is_actual <> 100 ";
                ExecRead(conn_db, out reader, sql, true);
                if (reader.Read())
                {
                    if (reader["dat_uchet"] != DBNull.Value)
                        DatPo = Convert.ToDateTime(reader["dat_uchet"]);
                }
                reader.Close();
                reader.Dispose();

                //если последующее показание отсутствует, то считаем месяцем окончания перерасчета месяц, за который вносится показание
                if (DatPo == null) DatPo = dat_uchet.AddDays(-1);

                //если месяц окончания перерасчета превышает или равен текущему расчетному месяцу, то устанавливаем его равным предыдущему расчетному месяцу
                if (DatPo.Value >= new DateTime(calcMonth.year_, calcMonth.month_, 1))
                    DatPo = new DateTime(calcMonth.year_, calcMonth.month_, 1).AddDays(-1);
            }
            else DatPo = null;

            //Вставка признака перерасчета
            if (DatS < new DateTime(calcMonth.year_, calcMonth.month_, 1))
            {
                MustCalcTable must_c = new MustCalcTable();
                must_c.NzpKvar = nzp_kvar;
                must_c.NzpServ = nzp_serv;
                must_c.NzpSupp = 0;
                must_c.Month = calcMonth.month_;
                must_c.Year = calcMonth.year_;
                must_c.NzpUser = finder.nzp_user;
                must_c.Kod2 = 0;

                if (nzp_type == (int)CounterKinds.Dom) 
                    must_c.Reason = MustCalcReasons.DomCounter;
                else must_c.Reason = MustCalcReasons.Counter;

                DbMustCalcNew db = new DbMustCalcNew(conn_db);

                if (nzp_type == (int)CounterKinds.Kvar)
                {
                    db.InsertReason(finder.bank + "_data", must_c);
                }
                else if (nzp_type == (int)CounterKinds.Dom)
                {
                    db.InsertReasonDomCounter(finder.bank + "_data", must_c, nzp_dom);
                }
                else
                {
                    db.InsertReasonGroupCounter(finder.bank + "_data", must_c, nzp_counter);
                }
            }
        }
    }
}
