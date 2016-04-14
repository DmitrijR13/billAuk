using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Bars.KP50.DataImport.SOURCE;
using Bars.KP50.DataImport.SOURCE.DISASSEMBLE;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbDisassembleFile : DataBaseHeadServer
    {
        private int _commandTime;
        private readonly IDbConnection _conDb;

        public DbDisassembleFile(IDbConnection con_db)
        {
            _conDb = con_db;
            _commandTime = 3600;
        }

        public Returns SelectDissMethod(FilesDisassemble finder, ref Returns ret)
        {
            try
            {
                string sql =
                    " SELECT TRIM(v.version_name) as version_name" +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_versions v, " + 
                        Points.Pref + DBManager.sUploadAliasRest + "files_imported i " +
                    " WHERE v.nzp_version = i.nzp_version " +
                    "   and i.nzp_file = " + finder.nzp_file;
                
                string versName = ClassDBUtils.OpenSQL(sql, _conDb).resultData.Rows[0]["version_name"].ToString().Trim().Substring(0, 3);

                if (versName == "1.2")
                {
                    DisassembleFile(finder, ref ret);
                }
                if (versName == "1.3")
                {
                    DbDisassembleFileVers132 disV132 = new DbDisassembleFileVers132();
                    disV132.DisassembleFile132(finder, ref ret, _conDb);
                }
                else
                {
                    ret.text = " Версия файла не найдена!";
                    ret.tag = -1;
                    ret.result = false;
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                   "Ошибка выполнения процедуры SelectDissMethod : " + ex.Message + Environment.NewLine + ex.StackTrace,
                   MonitorLog.typelog.Error, true);
                return new Returns(false, " Ошибка при получения версии файла!", -1);
            }
            return ret;
        }

        /// <summary>
        /// Разбор файла "ХАРАКТЕРИСТИКИ ЖИЛОГО ФОНДА И НАЧИСЛЕНИЯ ЖКУ"
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns DisassembleFile(FilesDisassemble finder, ref Returns ret)
        {
            ret = Utils.InitReturns();
            StringBuilder disassLog = new StringBuilder();
            try
            {
                #region Комментарий

                // Разбор 1,2,3,4,5  -считается завершенным (все адресное пространство согласовано)
                // Поставщики услуг вставлены в базу данных и согласованы
                // не заморачиваемся на счет базы данных берем из Points
                // Информация по оказываемым услугам =6|
                // Информация о параметрах лицевых счетов  в месяце перерасчета 
                // Информация о перерасчетах начислений по услугам
                // Информация о общедомовых приборах учета 
                // Информация о индивидуальных приборах учета 

                #endregion Комментарий
                MonitorLog.WriteLog("Старт разбора файла 'ХАРАКТЕРИСТИКИ ЖИЛОГО ФОНДА И НАЧИСЛЕНИЯ ЖКУ'. ID пользователя:" + finder.nzp_user, MonitorLog.typelog.Info, true);
                MonitorLog.WriteLog("Старт создания необходимых индексов ", MonitorLog.typelog.Info, true);
                CreateIndexForFileTables(finder);

                MonitorLog.WriteLog("Старт установки значений переменным для разбора", MonitorLog.typelog.Info, true);
                ret = SetDisassemleVars(ref finder, ref disassLog);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка установки значений переменным для разбора: " + ret.text , MonitorLog.typelog.Error, true);
                    return ret;
                }

                //Проверить связность адресного пространства в нижнем банке  , если есть ошибка то нужно разбираться в причинах
                using (var checkRelation = new CheckRelation())
                {
                    checkRelation.Run(_conDb, finder);
                }


                string sql;
#if PG
                sql = " set search_path to public ";
#else
                sql = " update statistics ";
#endif
                DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);
                

                using (var ins = new InsertAddrrSpaceIntoLocBanks(_conDb))
                {
                    ins.Run(finder, ref ret);
                }



                if (finder.reloaded_file)
                {
                    sql =
                        " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                        "SET nzp_dom = ukds WHERE ukds > 0 AND nzp_file = " + finder.nzp_file;
                    DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

                    sql =
                        " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom " +
                        " SET nzp_dom = (SELECT MAX(ukds) FROM " + Points.Pref + DBManager.sUploadAliasRest +
                        "file_dom A " +
                        "where a.id = cast(" + Points.Pref + DBManager.sUploadAliasRest +
                        "file_paramsdom.id_dom as " + sDecimalType + ") ) " +
                        " WHERE nzp_file = " + finder.nzp_file;
                    DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);
                }

                #region Проверка , если все данные разобраны , то переход к следующему этапу

                sql =
                    " select count(*)  as kol_notdis " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom a " +
                    " where nzp_dom is null and a.nzp_file= " + finder.nzp_file;
                int kol_notdis =
                    Convert.ToInt32(
                        ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                            "kol_notdis"]);
                if (kol_notdis > 0)
                {
                    ret.text = " Согласованы не все дома! Кол-во несогласованных: " + kol_notdis;
                    ret.result = false;
                    ret.tag = -1;
                    disassLog.Append("Ошибка разбора! " + ret.text + Environment.NewLine);
                    return ret;
                }

                #endregion Проверка , если все данные разобраны , то переход к следующему этапу

                #region Разбор file_kvar (секция 4 лицевые счета )

                if (finder.reloaded_file)
                {
                    using (var loadAsRepair = new LoadAsRepair())
                    {
                        loadAsRepair.Run(_conDb, finder,
                            Convert.ToDateTime("01." + finder.month.ToString("00") + "." + finder.year.ToString("0000")));
                        return ret;
                    }
                }

                string dat_s = "01." + finder.month.ToString("00") + "." +
                               finder.year.ToString("0000");

                using (var addParamsDom = new AddParamsDom())
                {
                    disassLog.Append("Старт ф-ции 'Загрузка домовых параметров'" +
                                     Environment.NewLine);
                    addParamsDom.Run(_conDb, finder.nzp_file, Convert.ToDateTime(dat_s), finder,
                        finder.dat_po);
                }
                using (var addKvarByFile = new AddKvarByFile())
                {
                    disassLog.Append("Старт разбора 4 секции 'Информация о лицевых счетах'" +
                                     Environment.NewLine);
                    addKvarByFile.Run(_conDb, finder.nzp_file, Convert.ToDateTime(dat_s), finder,
                        finder.dat_po);
                }

                //Проверка перезаписи квартирных и домовых параметров
                //using (var checkRewriteParams = new CheckRewriteParams())
                //{
                //    checkRewriteParams.Run(_conDb, finder);
                //}

                #endregion file_kvar (секция 4 лицевые счета )

                sql =
                    " SELECT count(a.nzp_file)   as kol_notdis " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar a " +
                    " WHERE a.nzp_kvar is null and a.nzp_file = " + finder.nzp_file;

                kol_notdis =
                    Convert.ToInt32(
                        ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).GetData().Rows[0][
                            "kol_notdis"]);

                if (kol_notdis > 0)
                {
                    ret.text = " Согласованы не все лицевые счета! Кол-во несогласованных: " + kol_notdis;
                    ret.result = false;
                    ret.tag = -1;
                    MonitorLog.WriteLog(
                        "Ошибка при разборе file_serv (секция 6 начисления текущего месяца). " +
                        ret.text + " из файла nzp_file=" + finder.nzp_file +
                        "; процедура DisassembleFile.", MonitorLog.typelog.Error, true);
                    return ret;
                }
                // начисления грузятся здесь  (не добавляются параметры тарифа)  
                // секция 6 начисления текущего месяца
                using (var insertDateFromFile = new InsertDateFromFile())
                {
                    insertDateFromFile.Run(_conDb, finder);
                }

                // Вставка ИПУ (file_ipu, file_ipu_p) - (11-12 секции)
                using (var insertIpuFromFile = new InsertIpuFromFile())
                {
                    insertIpuFromFile.Run(_conDb, finder);
                }


                // Вставка ОДПУ (file_odpu, file_odpu_p) - (9-10 секции)
                using (var insertOdpuFromFile = new InsertOdpuFromFile())
                {
                    insertOdpuFromFile.Run(_conDb, finder);
                }


                // Разбор файла жилец file_gilec (15 секция)
                using (var uploadGilec = new UploadGilec())
                {
                    uploadGilec.Run(finder, _conDb);
                }
                

                // Вставка оплат по услугам
                using (var addOplatServ = new AddOplatServ())
                {
                    addOplatServ.Run(_conDb, finder);
                }

                using (var insertParamsLs = new DBInsertParamLS(ServerConnection))
                {
                    //дополнительные параметры ЛС
                    ret = insertParamsLs.InsertParamLS(finder, dat_s);
                }

                //Разбор недопоставок (file_nedopost)
                InsertNedop(finder);
                
                //тонкая настройка calc_gku
                SettingsForGKU(finder);

                //изменение статуса загрузки
                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                    " SET diss_status = 'Разобран' " +
                    " WHERE nzp_file = " + finder.nzp_file;
                DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка выполнения процедуры DisassembleFile : " + ex.Message + Environment.NewLine + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                //изменение статуса загрузки
                string sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + " files_imported" +
                    " SET diss_status = 'Разобран c ошибками' " +
                    " WHERE nzp_file = " + finder.nzp_file;
                DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);
                ret.text = "Ошибка разбора файла.";
                ret.result = false;
                return ret;
            }
            ret.text = "Файл успешно разобран.";
            ret.result = true;
            ret.tag = -1;
            return ret;
        }


        /// <summary>
        /// Установка значений переменным для разбора
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="disassLog"></param>
        /// <returns></returns>
        private Returns SetDisassemleVars(ref FilesDisassemble finder, ref StringBuilder disassLog)
        {
            Returns ret = new Returns();
            ret = Utils.InitReturns();
            string sql =
                " SELECT h.calc_date as calc_date " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_head h " +
                " WHERE h.nzp_file = " + finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).GetData();
            
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
                ret.text = "В загружаемом файле не заполнено поле 'Месяц и год начислений' в 1 секции!";
                ret.result = false;
            ret.tag = -1;
                disassLog.Append(ret.text + Environment.NewLine);
                return ret;
            }

            if (finder.is_last_month)
            {
                finder.dat_po = "01.01.3000";
                disassLog.Append("Выбрано 'Последний загружаемый месяц.'" + Environment.NewLine);
            }
            else
                finder.dat_po =
                    DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month))
                        .ToString()
                        .PadLeft(2, '0') + "." + finder.month.ToString().PadLeft(2, '0') + "." +
                    finder.year;


            finder.nzp_file = Convert.ToInt32(dt.Rows[0]["nzp_file"]);
            disassLog.Append("Номер файла: " + finder.nzp_file + Environment.NewLine);

            sql =
                " SELECT trim(version_name)" +
                " FROM " + Points.Pref + sUploadAliasRest + "file_versions" +
                " WHERE nzp_version = " +
                " (SELECT nzp_version" +
                "  FROM " + Points.Pref + sUploadAliasRest + "files_imported" +
                "  WHERE nzp_file = " + finder.nzp_file + ")";

            finder.versionFull = DBManager.ExecScalar(ServerConnection, sql, out ret, true).ToString();

            if (finder.delete_all_data)
            {
                DeleteAllData(finder, disassLog, calcDate.Substring(0, 10));
            }
            else if (!finder.reloaded_file)
            {
                //если не удаляем информацию, то обрезаем параметры и тарифы по ЛС из разбираемого файла
                CutPrms(finder, calcDate.Substring(0, 10));
            }
            ret.result = true;
            ret.text = "Успешно выполнено!";
            return ret;
        }



        private void DeleteAllData(FilesDisassemble finder, StringBuilder disassLog, string calcDate)
        {
            string sql;
            disassLog.Append("Выбрано: 'Удалить все данные перед разбором.' " + Environment.NewLine);
            disassLog.Append("Старт удаления предыдущих данных " + Environment.NewLine);

            //изменение статуса загрузки
            sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                  " SET diss_status = 'Удаление предыдущих данных' WHERE nzp_file = " +
                  finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            // Удалить данные  которые были загружены ранее из верхнего банка
            sql = " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter +
                  "prm_1 where user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter +
                  "prm_2 where user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter +
                  "prm_3 where nzp_prm=51 and user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter +
                  "counters where user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter +
                  "counters_spis where user_block= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter +
                  "counters_dom where user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter +
                  "tarif where user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);


            // Удалить данные  которые были загружены ранее из нижнего банка данных 
            sql = " DELETE FROM  " + finder.bank + "_data" + tableDelimiter +
                  "prm_1 WHERE user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + finder.bank + "_data" + tableDelimiter +
                  "prm_2 WHERE user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + finder.bank + "_data" + tableDelimiter +
                  "prm_3 WHERE nzp_prm=51 and user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + finder.bank + "_data" + tableDelimiter +
                  "counters WHERE user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + finder.bank + "_data" + tableDelimiter +
                  "counters_spis WHERE user_block= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + finder.bank + "_data" + tableDelimiter +
                  "counters_dom WHERE user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + finder.bank + "_data" + tableDelimiter +
                  "tarif WHERE user_del= " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);
            sql = " DELETE FROM  " + finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter + "charge_" +
                  finder.month.ToString("00") +
                  " WHERE order_print=" + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, false, _commandTime);


            CutPrms(finder, calcDate);
            disassLog.Append("Успешное завершение удаления предыдущих данных " + Environment.NewLine);
        }

        /// <summary>
        /// Обрезаем параметры и тарифы по ЛС из разбираемого файла
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="calcDate"></param>
        private void CutPrms(FilesDisassemble finder, string calcDate)
        {
            string str_minus_1_day;
            string str_public;

#if PG
            str_minus_1_day = " interval '1 day' ";
            str_public = " public.";
#else
            str_minus_1_day = " 1 units day ";
            str_public = " ";
#endif

            //prm_1

            string sql =
                " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_1 set dat_po = " +
                " cast('" + calcDate + "' as date) - " +
                str_minus_1_day +
                " WHERE nzp in" +
                " (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ")" +
                " AND user_del <> (" + finder.nzp_file + ") " +
                " AND dat_s < cast('" + calcDate + "' as date)" +
                " AND dat_po = " + str_public + "MDY(1,1,3000)";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " UPDATE " + Points.Pref + "_data" + tableDelimiter + "prm_1 set dat_po = " +
                " cast('" + calcDate + "' as date) - " +
                str_minus_1_day +
                " WHERE nzp in" +
                " (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ")" +
                " AND user_del <> (" + finder.nzp_file + ") " +
                " AND dat_s < cast('" + calcDate + "' as date)" +
                " AND dat_po = " + str_public + "MDY(1,1,3000)";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter + "prm_1 " +
                " WHERE dat_s = '" + calcDate + "'" +
                " AND nzp in " +
                " (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                " WHERE nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " DELETE FROM  " + finder.bank + "_data" + tableDelimiter + "prm_1 " +
                " WHERE dat_s = '" + calcDate + "'" +
                " AND nzp in " +
                " (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar WHERE nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);


            //prm_2

            sql =
                "UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_2 set dat_po = " +
                "cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " WHERE nzp in (SELECT nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_dom WHERE nzp_file = " + finder.nzp_file + ")" +
                " AND user_del not in (" + finder.nzp_file + ") " +
                " AND dat_s < cast('" + calcDate + "' as date)" +
                " AND dat_po = " + str_public + "MDY(1,1,3000)";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                "UPDATE " + Points.Pref + "_data" + tableDelimiter + "prm_2 set dat_po = " +
                "cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " WHERE nzp in (SELECT nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_dom WHERE nzp_file = " + finder.nzp_file + ")" +
                " AND user_del not in (" + finder.nzp_file + ") " +
                " AND dat_s < cast('" + calcDate + "' as date)" +
                " AND dat_po = " + str_public + "MDY(1,1,3000)";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter + "prm_2 " +
                " WHERE dat_s = '" + calcDate + "'" +
                " AND nzp in " +
                " (SELECT nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_dom WHERE nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " DELETE FROM  " + finder.bank + "_data" + tableDelimiter + "prm_2 " +
                " WHERE dat_s = '" + calcDate + "'" +
                " AND nzp in (SELECT nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_dom WHERE nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            //prm_3

            sql =
                " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_3 set dat_po = " +
                " cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " WHERE nzp_prm = 51 and nzp in " +
                " (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar WHERE nzp_file = " + finder.nzp_file + ")" +
                " AND user_del not in (" + finder.nzp_file + ") AND dat_s < cast('" +
                calcDate + "' as date)" +
                " AND dat_po = " + str_public + "MDY(1,1,3000)";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter + "prm_3 " +
                " WHERE dat_s = '" + calcDate + "'" +
                " AND nzp in " +
                " (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar WHERE nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " DELETE FROM  " + finder.bank + "_data" + tableDelimiter + "prm_3 " +
                " WHERE dat_s = '" + calcDate + "'" +
                " AND nzp in (SELECT nzp_kvar FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar WHERE nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            //prm_11

            sql =
                " UPDATE " + finder.bank + "_data" + tableDelimiter + "prm_11 set dat_po = " +
                " cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " WHERE nzp in (SELECT nzp_supp FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_supp WHERE nzp_file = " + finder.nzp_file + ")" +
                " AND user_del NOT IN (" + finder.nzp_file + ") AND dat_s < cast('" +
                calcDate + "' as date)" +
                " AND dat_po = " + str_public + "MDY(1,1,3000)";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " DELETE FROM  " + Points.Pref + "_data" + tableDelimiter + "prm_11 " +
                " WHERE dat_s = '" + calcDate + "'" +
                " AND nzp in (SELECT nzp_supp FROM " + Points.Pref + DBManager.sUploadAliasRest +
                "file_supp WHERE nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " delete from  " + finder.bank + "_data" + tableDelimiter + "prm_11 " +
                " where dat_s = '" + calcDate + "'" +
                " and nzp in (select nzp_supp from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_supp where nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            //tarif

            sql =
                "update " + Points.Pref + "_data" + tableDelimiter + "tarif set dat_po = " +
                "cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " where nzp_kvar in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ") " +
                "and dat_s < cast('" + calcDate + "' as date)" +
                " and dat_po = " + str_public + "MDY(1,1,3000)";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " update " + finder.bank + "_data" + tableDelimiter + "tarif set dat_po = " +
                " cast('" + calcDate + "' as date) -" +
                str_minus_1_day +
                " where nzp_kvar in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ") " +
                " and dat_s < cast('" + calcDate + "' as date)" +
                " and dat_po = " + str_public + "MDY(1,1,3000)";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " delete from  " + Points.Pref + "_data" + tableDelimiter + "tarif " +
                " where dat_s = '" + calcDate + "'" +
                " and nzp_kvar in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            sql =
                " delete from  " + finder.bank + "_data" + tableDelimiter + "tarif " +
                " where dat_s = '" + calcDate + "'" +
                " and nzp_kvar in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);

            //charge_mm

            sql =
                " delete from  " + finder.bank + "_charge_" + (finder.year - 2000) +
                tableDelimiter + "charge_" + finder.month.ToString("00") +
                " where nzp_kvar in " +
                " (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                "file_kvar where nzp_file = " + finder.nzp_file + ")";
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);
        }


        /// <summary>
        /// Вспомогательная ф-ция Разбор недопоставок (file_nedopost)
        /// </summary>
        /// <param name="finder"></param>
        private void InsertNedop(FilesDisassemble finder)
        {
            string sql;
            try
            {
                //изменение статуса загрузки
                sql =
                    "update " + Points.Pref + DBManager.sUploadAliasRest +
                    "files_imported set diss_status = 'Загрузка недопоставок' " +
                    " where nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Log);

                Returns ret1 = new Returns();

                sql = "insert into " + finder.bank + sDataAliasRest + "nedop_kvar" +
                    " (nzp_kvar, nzp_serv, nzp_supp, dat_s, " +
                    " dat_po, is_actual, nzp_user," +
                    " dat_when, nzp_kind, cur_unl," +
                    " month_calc, user_del)" +
                    " select fk.nzp_kvar, fs.nzp_serv, 1, fn.dat_nedstart," +
                    " fn.dat_nedstop, 1, " + finder.nzp_user + "," +
                     sCurDate + ",  fn.type_ned, 1," +
                     " fh.calc_date, fn.nzp_file" +
                      " from " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost fn, " +
                      Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                      Points.Pref + DBManager.sUploadAliasRest + "file_services fs, " +
                      //Points.Pref + DBManager.sUploadAliasRest + "file_supp fss, " +
                      Points.Pref + DBManager.sUploadAliasRest + "file_head fh " +
                     " where fn.nzp_file = fk.nzp_file and fk.nzp_file = fs.nzp_file " +
                     " and fs.nzp_file = fh.nzp_file and " +
                     " fn.nzp_file =" + finder.nzp_file +
                     " and cast (fn.id_serv as integer) = fs.id_serv " +
                     " and fk.id = fn.ls_id ";

                ret1 = ExecSQL(_conDb, sql, true);
                if (ret1.result)
                {
                    sql = "insert into " + Points.Pref + sDataAliasRest + "nedop_kvar" +
                    " (nzp_kvar, nzp_serv, nzp_supp, dat_s, " +
                    " dat_po, tn, comment, is_actual, nzp_user," +
                    " dat_when, act_no, nzp_kind)" +
                    "select nzp_kvar, nzp_serv, nzp_supp, dat_s, " +
                    " dat_po, tn, comment, is_actual, nzp_user," +
                    " dat_when, act_no, nzp_kind" +
                    " from " + finder.bank + "_data" + tableDelimiter + "nedop_kvar " +
                    " where user_del = " + finder.nzp_file;
                    ret1 = ExecSQL(_conDb, sql, true);
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры insertNedop " + ret1.text, MonitorLog.typelog.Error,
                        true);
                    new ReturnsType(false, "Ошибка выполнения процедуры insertNedop ", -1);
                    return;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка выполнения процедуры insertNedop " + "\n" + ex.Message + "\n" + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                new ReturnsType(false, "Ошибка выполнения процедуры insertNedop ", -1);
                return;
            }
            new ReturnsType(true, "Данные сохранены ", 1);
            return;
        }

        /// <summary>
        /// создание индексов
        /// </summary>
        /// <param name="conn_db"></param>
        private void CreateIndexForFileTables(FilesDisassemble finder)
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

            #region kvar в верхнем банке

            index.Clear();
            d.CreateUniqueIndex(_conDb, Points.Pref + "_data" + tableDelimiter + "kvar", "ix202_1",
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

            d.CreateOneIndex(_conDb, Points.Pref + "_data" + tableDelimiter + "kvar", index);

            #endregion

            #region kvar в нижних банках

            string pref = finder.bank;

            index.Clear();
            d.CreateUniqueIndex(_conDb, pref + "_data" + tableDelimiter + "kvar", "ix202_1",
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

            d.CreateOneIndex(_conDb, pref + "_data" + tableDelimiter + "kvar", index);

            #endregion
        }

        /// <summary>
        /// Тонкая настройка
        /// </summary>
        /// <param name="finder"></param>
        private void SettingsForGKU(FilesDisassemble finder)
        {
            string sql;
            int commandTime = 3600;
            string charge_XX = finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter +
                "charge_" + finder.month.ToString("00");
            string calc_gku_XX = finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter +
                "calc_gku_" + finder.month.ToString("00");
            string gil_XX = finder.bank + "_charge_" + (finder.year - 2000) + tableDelimiter +
                "gil_" + finder.month.ToString("00");

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                " SET diss_status = 'Тонкая настройка'" +
                " WHERE nzp_file = " + finder.nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            try
            {
                sql =
                    " DROP TABLE t_settings_gku_tarif_1";
                DBManager.ExecSQL(_conDb, null, sql, false, commandTime);
            }
            catch { }
            sql =
                " CREATE TEMP TABLE t_settings_gku_tarif_1 ( " +
                " nzp_kvar INTEGER," +
                " nzp_supp INTEGER," +
                " nzp_serv INTEGER," +
                " nzp_frm INTEGER)";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " INSERT INTO t_settings_gku_tarif_1" +
                " (nzp_kvar, nzp_supp, nzp_serv, nzp_frm)" +
                " SELECT DISTINCT nzp_kvar, nzp_supp, nzp_serv, nzp_frm" +
                " FROM " + finder.bank + sDataAliasRest + "tarif" +
                " WHERE dat_s <= '01." + finder.month.ToString("00") + "." + finder.year + "' " +
                " AND dat_po> '01." + finder.month.ToString("00") + "." + finder.year + "'" +
                " AND is_actual <> 100";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " CREATE INDEX inx_settings_gku_tarif_1 " +
                " ON t_settings_gku_tarif_1(nzp_kvar, nzp_supp, nzp_serv)";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql = sUpdStat + " t_settings_gku_tarif_1";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " UPDATE " + charge_XX +
                " SET nzp_frm =" +
                "   ((SELECT MAX(t.nzp_frm)" +
                "   FROM t_settings_gku_tarif_1 t " +
                "   WHERE t.nzp_kvar = nzp_kvar AND t.nzp_serv = nzp_serv AND t.nzp_supp = nzp_supp ))" +
                " WHERE nzp_frm is null";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " UPDATE " + charge_XX +
                " SET gsum_tarif = rsum_tarif" +
                " WHERE gsum_tarif <> rsum_tarif";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);


            sql = sUpdStat + " " + charge_XX;
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

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
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            try
            {
                sql =
                    " DROP TABLE t_settings_gku_prm_1";
                DBManager.ExecSQL(_conDb, null, sql, false, commandTime);
            }
            catch { }
            sql =
                " CREATE TEMP TABLE t_settings_gku_prm_1 ( " +
                " nzp_kvar INTEGER," +
                " nzp_prm INTEGER," +
                " val_prm CHAR(20) )";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " INSERT INTO t_settings_gku_prm_1 " +
                " (nzp_kvar, nzp_prm, val_prm)" + 
                " SELECT DISTINCT nzp, nzp_prm, val_prm " + 
                " FROM " + finder.bank + sDataAliasRest + "prm_1" +
                " WHERE dat_s <= '01." + finder.month.ToString("00") + "." + finder.year + "' " +
                " AND dat_po> '01." + finder.month.ToString("00") + "." + finder.year + "'" +
                " AND is_actual <> 100" +
                " AND nzp_prm in (4, 5)";;
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " CREATE INDEX t_settings_gku_prm_1 " +
                " ON t_settings_gku_prm_1(nzp_kvar, nzp_prm)";
            DBManager.ExecSQL(_conDb, null, sql, false, commandTime);

            sql = sUpdStat + " t_settings_gku_prm_1";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " UPDATE " + calc_gku_XX +
                " SET gil = " + sNvlWord + 
                "   ((SELECT MAX(val_prm " + DBManager.sConvToInt + ")" +
                "   FROM t_settings_gku_prm_1 p" +
                "   WHERE " + calc_gku_XX + ".nzp_kvar=p.nzp_kvar" +
                "   AND p.nzp_prm = 5 ),0);";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " UPDATE " + calc_gku_XX +
                " SET squ = " + sNvlWord +
                "   ((select max(val_prm " + sConvToNum + " )" +
                "   FROM t_settings_gku_prm_1 p" +
                "   WHERE " + calc_gku_XX + ".nzp_kvar = p.nzp_kvar" +
                "   AND p.nzp_prm=4 ),0);";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql = 
                " UPDATE " + calc_gku_XX +
                " SET gil_g=gil ";
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

            sql =
                " DELETE FROM " + gil_XX;
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);

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
            DBManager.ExecSQL(_conDb, null, sql, true, commandTime);
        }


        /// <summary>
        /// Сохранить в базе информацию о  файле для разбора
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType DbSaveFileToDisassembly(FilesImported finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                string sql =
                    " SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " WHERE nzp_file = " + finder.nzp_file +
                    " AND nzp_user = " + finder.nzp_user +
                    " AND pref = '" + finder.bank.Trim() + "'";
                if (ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).resultData.Rows.Count == 0)
                {
                    sql =
                        " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "files_selected ( nzp_file, nzp_user, pref ) " +
                        " VALUES ( " + finder.nzp_file + " , " + finder.nzp_user + " , '" + finder.bank + "')";
                }
                else
                {
                    sql =
                        " DELETE FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                        " WHERE nzp_file = " + finder.nzp_file +
                        " AND nzp_user = " + finder.nzp_user +
                        " AND pref = '" + finder.bank + "' ";
                }
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DbSaveFileToDisassembly : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }


        public Returns GetNzpFileLoad(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            int nzp_file = 0;
            try
            {
                
                string sql =
                    " SELECT count(*) FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " WHERE  nzp_user = " + finder.nzp_user +
                    " AND pref = '" + finder.bank.Trim() + "'";

                
                object obj = DBManager.ExecScalar(_conDb, sql, out ret, true);
                if (!ret.result)
                {
                    return new Returns(false, "Ошибка при определении выбранного файла");
                }
                int count = 0;
                count = Convert.ToInt32(obj);
                if (count == 1)
                {
                    
                    sql = "SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " WHERE  nzp_user = " + finder.nzp_user +
                    " AND pref = '" + finder.bank.Trim() + "'";
                    IDataReader reader;
                    DBManager.ExecRead(_conDb, out reader, sql, true);
                    while (reader.Read())
                    {
                        nzp_file =  reader["nzp_file"] != DBNull.Value ? Convert.ToInt32(reader["nzp_file"]) : 0;
                    }
                }
                else if (count <= 0)
                {
                    return new Returns(false, "Не выбран файл из списка");
                }
                else
                {
                    return new Returns(false, "Должен быть выбран только один файл", -1);
                }
                if (nzp_file == 0)
                {
                    return new Returns(false, "Ошибка при определении выбранного файла");
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DbSaveFileToDisassembly : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = nzp_file;

            return ret;
        }

    }
}
