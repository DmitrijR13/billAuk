using System;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.EXCHANGE
{
    public class UnloadUtils : DataBaseHeadServer
    {
        /// <summary>
        /// Создание индексов
        /// </summary>
        /// <param name="fullTableNameWithPref"></param>
        /// <param name="indexDictionary"></param>
        private void CreateIndex(string fullTableNameWithPref, Dictionary<string, List<string>> indexDictionary)
        {
            string sql;
            string col = "";
            try
            {
                foreach (KeyValuePair<string, List<string>> kvp in indexDictionary)
                {
                    foreach (string colunm in kvp.Value)
                    {
                        col += colunm + ",";
                    }
                    //убираем последнюю запятую
                    col = col.Substring(0, col.Length - 1);
                    try
                    {
                        sql = "create index " + kvp.Key + " on " + fullTableNameWithPref + " (" + col + ")";
                        ExecSQL(sql, false);
                    }
                    catch
                    {
                    }
                }
                ExecSQL(sUpdStat + " " + fullTableNameWithPref);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CreateOneIndex : " + ex.Message + Environment.NewLine + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
        }

        /// <summary>
        /// Проверка и создание таблицы при отсутствии
        /// </summary>
        /// <param name="tblName"></param>
        /// <param name="columnNames"></param>
        private void CreateTable(string tblName, string columnNames)
        {
            Returns ret = new Returns();
            string sql = 
                "SELECT " +
                (DBManager.tableDelimiter == ":" ? " FIRST 1 " : "" ) +
                " * FROM " + tblName + 
                (DBManager.tableDelimiter == "." ? " LIMIT 1 " : "" ) +
                "";
            if (!ExecSQL(sql, false).result)
            {
                ret.result = false;
                ret = ExecSQL("CREATE TABLE " + tblName + " ( " + columnNames + " ) ");
                if (!ret.result)
                {
                    throw new Exception("Ошибка выполнения процедуры CreateTable при созданиии таблицы '" + tblName +
                                        "': " + ret.text);
                }
            }
            ExecSQL(sUpdStat + " " + tblName);
        }

        /// <summary>
        /// Создание отсутствующих таблиц в БД, необходимых для выгрузки
        /// </summary>
        /// <param name="baseName"></param>
        /// <param name="tblName"></param>
        /// <returns></returns>
        public Returns CheckTablesForUnl()
        {
            Returns ret = new Returns();
            Dictionary<string, List<string>> index = new Dictionary<string, List<string>>();
            string columnNames = "";

            #region Проверка на существование БД fXXX_upload

            string sql = "";
#if PG
            sql = " SET search_path TO  '" + Points.Pref + "_upload" + "'";
#else
            sql = "DATABASE " + Points.Pref + "_upload" + "; ";
#endif

            if (!ExecSQL(sql).result)
            {
                ret.result = false;
                ret.text = "Ошибка работы с БД. Возможно, отсутствует схема (БД) для загрузки. Смотрите журнал ошибок.";
                ret.tag = -1;
                return ret;
            }

            #endregion Проверка на существование БД fXXX_upload

            #region unl_head

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER NOT NULL," +
                "org_name CHAR(40) NOT NULL," +
                "branch_name CHAR(40) NOT NULL," +
                "inn CHAR(20) NOT NULL," +
                "kpp CHAR(20) NOT NULL," +
                "unl_no INTEGER NOT NULL," +
                "unl_date DATE NOT NULL," +
                "sender_phone CHAR(20) NOT NULL," +
                "sender_fio CHAR(80) NOT NULL," +
                "calc_date DATE NOT NULL," +
                "row_number INTEGER NOT NULL";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_head", columnNames);

            #endregion unl_head

            #region unl_area

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER NOT NULL," +
                "area char(40)," +
                "jur_address char(100)," +
                "fact_address CHAR(100)," +
                "inn CHAR(20)," +
                "kpp CHAR(20)," +
                "rs CHAR(20)," +
                "bank CHAR(100)," +
                "bik CHAR(20)," +
                "ks CHAR(20)," +
                "nzp_area INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_area", columnNames);

            index.Clear();
            index.Add("ix1_unl_area", new List<string>() {"id"});
            index.Add("i1_unl_area", new List<string>() {"nzp_file", "id", "nzp_area"});
            index.Add("ix2_unl_area", new List<string>() {"nzp_file", "nzp_area"});
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_area", index);

            #endregion unl_area

            #region unl_urlic

            columnNames =
                "id SERIAL NOT NULL," +
                "nzp_file   INTEGER NOT NULL," +
                "urlic_id INTEGER," +
                "urlic_name CHAR(100) NOT NULL," +
                "urlic_name_s CHAR(10) NOT NULL," +
                "jur_address CHAR(100)," +
                "fact_address CHAR(100)," +
                "inn CHAR(20)," +
                "kpp CHAR(20)," +
                "rs CHAR(20)," +
                "ks CHAR(20)," +
                "bik CHAR(9)," +
                "tel_chief CHAR(20)," +
                "tel_b CHAR(20)," +
                "chief_name CHAR(100)," +
                "chief_post CHAR(40)," +
                "b_name CHAR(100)," +
                "okonh1 CHAR(20)," +
                "okonh2 CHAR(20)," +
                "okpo CHAR(20)," +
                "post_and_name CHAR(200)," +
                "is_area INTEGER," +
                "is_supp INTEGER," +
                "is_arendator INTEGER, " +
                "is_rc INTEGER," +
                "is_rso INTEGER," +
                "is_agent INTEGER," +
                "is_subabonent INTEGER," +
                "is_bank INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_urlic", columnNames);

            index.Clear();
            index.Add("ix1_unl_urlic", new List<string>() { "nzp_file"});
            index.Add("ix2_unl_urlic", new List<string>() { "nzp_file", "urlic_id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_urlic", index);

            #endregion unl_urlic

            #region unl_dom

            columnNames =
                "id SERIAL NOT NULL," +
                "nzp_file  INTEGER NOT NULL," +
                "nzp_dom INTEGER," +
                "town CHAR(30)," +
                "rajon CHAR(30)," +
                "ulica CHAR(40)," +
                "ndom CHAR(10)," +
                "nkor CHAR(3)," +
                "nzp_area INTEGER," +
                "cat_blago CHAR(30)," +
                "etazh INTEGER ," +
                "build_year DATE," +
                "total_square DECIMAL(14,2) ," +
                "mop_square DECIMAL(14,2)," +
                "useful_square DECIMAL(14,2)," +
                "mo_id DECIMAL(13,0)," +
                "params CHAR(250)," +
                "ls_row_number INTEGER ," +
                "odpu_row_number INTEGER ," +
                "nzp_ul INTEGER," +
                "nzp_raj INTEGER," +
                "nzp_town INTEGER," +
                "nzp_geu INTEGER," +
                "uch INTEGER," +
                "kod_kladr CHAR(30), " +
                "kod_fias CHAR(30)";

            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_dom", columnNames);

            index.Clear();
            index.Add("ix1_unl_dom", new List<string>() {"nzp_file"});
            index.Add("ix2_unl_dom", new List<string>() {"nzp_file", "id"});
            index.Add("ix3_unl_dom", new List<string>() {"nzp_file", "nzp_dom"});
            index.Add("ix4_unl_dom", new List<string>() { "nzp_file", "nzp_area", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_dom", index);

            #endregion unl_dom

            #region unl_kvar

            columnNames =
                "id SERIAL NOT NULL," +
                "nzp_file INTEGER NOT NULL," +
                "nzp_kvar INTEGER NOT NULL," +
                "nzp_dom INTEGER NOT NULL," +
                "typek INTEGER NOT NULL," +
                "fio CHAR(120)," +
                "fam CHAR(40)," +
                "ima CHAR(40)," +
                "otch CHAR(40)," +
                "birth_date DATE," +
                "nkvar CHAR(10)," +
                "nkvar_n CHAR(3)," +
                "open_date DATE," +
                "opening_osnov CHAR(100)," +
                "close_date DATE," +
                "closing_osnov CHAR(100)," +
                "kol_gil INTEGER ," +
                "kol_vrem_prib INTEGER ," +
                "kol_vrem_ub INTEGER ," +
                "room_number INTEGER ," +
                "total_square DECIMAL(14,2) ," +
                "living_square DECIMAL(14,2)," +
                "otapl_square DECIMAL(14,2)," +
                "naim_square DECIMAL(14,2)," +
                "is_communal INTEGER," +
                "is_el_plita INTEGER," +
                "is_gas_plita INTEGER," +
                "is_gas_colonka INTEGER," +
                "is_fire_plita INTEGER," +
                "gas_type INTEGER," +
                "water_type INTEGER," +
                "hotwater_type INTEGER," +
                "canalization_type INTEGER," +
                "is_open_otopl INTEGER," +
                "params CHAR(250)," +
                "service_row_number INTEGER," +
                "reval_params_row_number INTEGER," +
                "ipu_row_number INTEGER," +
                "comment CHAR(250)," +
                "num_ls INTEGER," +
                "id_urlic CHAR(20)," +
                "type_owner CHAR(30)," +
                "nzp_gil INTEGER," +
                "nzp_geu INTEGER," +
                "uch char(60)," +
                "nzp_area INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_kvar", columnNames);

            index.Clear();
            index.Add("ix1_unl_kvar", new List<string>() { "id" });
            index.Add("ix2_unl_kvar", new List<string>() { "nzp_file", "id" });
            index.Add("ix3_unl_kvar", new List<string>() { "nzp_file", "nzp_kvar" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_kvar", index);

            #endregion unl_kvar

            #region unl_supp

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER NOT NULL," +
                "supp_id DECIMAL(18,0) NOT NULL," +
                "supp_name CHAR(25) NOT NULL," +
                "jur_address CHAR(100)," +
                "fact_address CHAR(100)," +
                "inn CHAR(20)," +
                "kpp CHAR(20)," +
                "rs CHAR(20)," +
                "bank CHAR(100)," +
                "bik CHAR(20)," +
                "ks CHAR(20)," +
                "nzp_supp INTEGER";

            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_supp", columnNames);

            index.Clear();
            index.Add("ix1_unl_supp", new List<string>() { "nzp_file", "nzp_supp" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_supp", index);

            #endregion unl_supp

            #region unl_dog

            columnNames =
                "id SERIAL NOT NULL," +
                "nzp_file INTEGER," +
                "nzp_supp INTEGER," +
                "nzp_payer_agent INTEGER," +
                "nzp_payer_princip INTEGER," +
                "nzp_payer_supp INTEGER," +
                "name_supp char(100)," +
                "num_dog char(20)," +
                "dat_dog date";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_dog", columnNames);

            index.Clear();
            index.Add("ix1_unl_dog", new List<string>() { "nzp_file", "nzp_supp" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_dog", index);

            #endregion unl_dog
            
            #region unl_serv

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER NOT NULL," +
                "nzp_kvar CHAR(20) NOT NULL," +
                "nzp_supp DECIMAL(18,0)," +
                "nzp_serv INTEGER NOT NULL," +
                "sum_insaldo DECIMAL(14,2) NOT NULL," +
                "eot DECIMAL(14,3) NOT NULL," +
                "reg_tarif_percent DECIMAL(14,3) NOT NULL," +
                "reg_tarif DECIMAL(14,3) NOT NULL," +
                "nzp_measure INTEGER," +
                "fact_rashod DECIMAL(18,7) NOT NULL," +
                "norm_rashod DECIMAL(18,7) NOT NULL," +
                "is_pu_calc INTEGER NOT NULL," +
                "sum_nach DECIMAL(14,2) NOT NULL," +
                "sum_reval DECIMAL(14,2) NOT NULL," +
                "sum_subsidy DECIMAL(14,2) NOT NULL," +
                "sum_subsidyp DECIMAL(14,2) NOT NULL," +
                "sum_lgota DECIMAL(14,2) NOT NULL," +
                "sum_lgotap DECIMAL(14,2)," +
                "sum_smo DECIMAL(14,2) NOT NULL," +
                "sum_smop DECIMAL(14,2)," +
                "sum_money DECIMAL(14,2) NOT NULL," +
                "is_del INTEGER," +
                "sum_outsaldo DECIMAL(14,2) NOT NULL," +
                "servp_row_number INTEGER," +
                "met_calc INTEGER," +
                "pkod DECIMAL(18,0)";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_serv", columnNames);

            index.Clear();
            index.Add("ix1_unl_serv", new List<string>() { "nzp_file", "nzp_serv", "nzp_measure", "nzp_kvar" });
            index.Add("ix2_unl_serv", new List<string>() { "nzp_file", "nzp_kvar", "nzp_serv", "nzp_measure", "id" });
            index.Add("ix3_unl_serv", new List<string>() { "nzp_file", "nzp_kvar", "nzp_serv" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_serv", index);

            #endregion unl_serv

            #region unl_odpu

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER NOT NULL, " +
                "nzp_counter INTEGER," +
                "nzp_dom INTEGER," +
                "nzp_serv INTEGER," +
                "serv_type INTEGER," +
                "nzp_cnttype INTEGER, " +
                "counter_type CHAR(25)," +
                "cnt_stage INTEGER," +
                "mmnog INTEGER," +
                "num_cnt CHAR(20)," +
                "nzp_measure INTEGER," +
                "dat_prov DATE," +
                "dat_provnext DATE," +
                "doppar CHAR(250)," +
                "rashod_type INTEGER," +
                "dat_uchet DATE," +
                "val_cnt FLOAT";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_odpu", columnNames);

            index.Clear();
            index.Add("ix1_unl_odpu", new List<string>() { "nzp_file", "nzp_counter" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_odpu", index);

            #endregion unl_odpu

            #region unl_ipu

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER NOT NULL, " +
                "nzp_counter INTEGER," +
                "nzp_kvar INTEGER," +
                "nzp_serv INTEGER," +
                "serv_type INTEGER," +
                "nzp_cnttype INTEGER, " +
                "counter_type CHAR(25)," +
                "cnt_stage INTEGER," +
                "mmnog INTEGER," +
                "num_cnt CHAR(20)," +
                "nzp_measure INTEGER," +
                "dat_prov DATE," +
                "dat_provnext DATE," +
                "doppar CHAR(250)," +
                "rashod_type INTEGER," +
                "dat_uchet DATE," +
                "val_cnt FLOAT";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_ipu", columnNames);

            index.Clear();
            index.Add("ix1_unl_ipu", new List<string>() { "nzp_file", "nzp_counter" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_ipu", index);

            #endregion unl_ipu


            #region unl_mo
            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file INTEGER," +
                "nzp_vill "+ DBManager.sDecimalType + "(13,0)," +
                "vill_name CHAR(60)," +
                "rajon CHAR(60)," +
                "nzp_raj INTEGER," +
                "kod_kladr CHAR(30)";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_mo", columnNames);

            index.Clear();
            index.Add("ix1_unl_mo", new List<string>() { "nzp_file", "nzp_vill" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_mo", index);

            #endregion unl_mo



            #region unl_gilec

            columnNames =
                  "id SERIAL  NOT NULL," +
                " nzp_file INTEGER," +
                "nzp_kvar INTEGER," +
                "nzp_gil INTEGER," +
                "nzp_kart INTEGER," +
                "nzp_tkrt INTEGER," +
                "fam CHAR(40)," +
                "ima CHAR(40)," +
                "otch CHAR(40)," +
                "dat_rog DATE," +
                "fam_c CHAR(40)," +
                "ima_c CHAR(40)," +
                "otch_c CHAR(40)," +
                "dat_rog_c DATE," +
                "gender CHAR(1)," +
                "nzp_dok INTEGER," +
                "serij CHAR(10)," +
                "nomer CHAR(7)," +
                "vid_dat DATE," +
                "vid_mes CHAR(70)," +
                "kod_podrazd CHAR(7)," +
                "strana_mr CHAR(40)," +
                "region_mr CHAR(40)," +
                "okrug_mr CHAR(40)," +
                "gorod_mr CHAR(40)," +
                "npunkt_mr CHAR(40)," +
                "rem_mr CHAR(180)," +
                "strana_op CHAR(40)," +
                "region_op CHAR(40)," +
                "okrug_op CHAR(40)," +
                "gorod_op CHAR(40)," +
                "npunkt_op CHAR(40)," +
                "rem_op CHAR(180)," +
                "strana_ku CHAR(40)," +
                "region_ku CHAR(40)," +
                "okrug_ku CHAR(40)," +
                "gorod_ku CHAR(40)," +
                "npunkt_ku CHAR(40)," +
                "rem_ku CHAR(180)," +
                "rem_p CHAR(40)," +
                "tprp CHAR(1)," +
                "dat_prop DATE," +
                "dat_oprp DATE, " +
                "dat_pvu DATE," +
                "who_pvu CHAR(100)," +
                "dat_svu DATE," +
                "namereg CHAR(100)," +
                "kod_namereg CHAR(7)," +
                "nzp_rod INTEGER, " + 
                "rod CHAR(30)," +
                "nzp_celp INTEGER," +
                "nzp_celu INTEGER," +
                "dat_sost DATE," +
                "dat_ofor DATE," +
                "comment CHAR(40)";

            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_gilec", columnNames);

            index.Clear();
            index.Add("ix1_unl_gilec", new List<string>() { "nzp_file", "nzp_kvar" });
            index.Add("ix2_unl_gilec", new List<string>() { "nzp_file", "nzp_kvar", "nzp_gil" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_gilec", index);

            #endregion unl_gilec


            /*
            #region unl_kvarp

            columnNames =
                "id CHAR(20) NOT NULL," +
                "reval_month DATE," +
                "nzp_file INTEGER NOT NULL," +
                "fam CHAR(40)," +
                "ima CHAR(40)," +
                "otch CHAR(40)," +
                "birth_date DATE," +
                "nkvar CHAR(10)," +
                "nkvar_n CHAR(3)," +
                "open_date DATE," +
                "opening_osnov CHAR(100)," +
                "close_date DATE," +
                "closing_osnov CHAR(100)," +
                "kol_gil INTEGER NOT NULL," +
                "kol_vrem_prib INTEGER NOT NULL," +
                "kol_vrem_ub INTEGER NOT NULL," +
                "room_number INTEGER NOT NULL," +
                "total_square DECIMAL(14,2) NOT NULL," +
                "living_square DECIMAL(14,2)," +
                "otapl_square DECIMAL(14,2)," +
                "naim_square DECIMAL(14,2)," +
                "is_communal INTEGER NOT NULL," +
                "is_el_plita INTEGER," +
                "is_gas_plita INTEGER," +
                "is_gas_colonka INTEGER," +
                "is_fire_plita INTEGER," +
                "gas_type INTEGER," +
                "water_type INTEGER," +
                "hotwater_type INTEGER," +
                "canalization_type INTEGER," +
                "is_open_otopl INTEGER," +
                "params CHAR(250)," +
                "nzp_dom INTEGER," +
                "nzp_kvar INTEGER," +
                "comment CHAR(250)," +
                "nzp_status INTEGER," +
                "local_id CHAR(20)";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_kvarp", columnNames);

            #endregion unl_kvarp

            #region unl_servp

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER NOT NULL," +
                "reval_month DATE," +
                "ls_id CHAR(20) NOT NULL," +
                "supp_id DECIMAL(18,0)," +
                "nzp_serv INTEGER NOT NULL," +
                "eot DECIMAL(14,3) NOT NULL," +
                "reg_tarif_percent DECIMAL(14,3) NOT NULL," +
                "reg_tarif DECIMAL(14,3) NOT NULL," +
                "nzp_measure INTEGER NOT NULL," +
                "fact_rashod DECIMAL(18,7) NOT NULL," +
                "norm_rashod DECIMAL(18,7) NOT NULL," +
                "is_pu_calc INTEGER NOT NULL," +
                "sum_reval DECIMAL(14,2) NOT NULL," +
                "sum_subsidyp DECIMAL(14,2) NOT NULL," +
                "sum_lgotap DECIMAL(14,2) NOT NULL," +
                "sum_smop DECIMAL(14,2) NOT NULL," +
                "nzp_kvar INTEGER," +
                "nzp_supp INTEGER," +
                "dog_id INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_servp", columnNames);

            index.Clear();
            index.Add("ix1_unl_servp", new List<string>() { "ls_id", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_servp", index);

            #endregion unl_servp
            */
           
            /*
            #region unl_odpu_p

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER," +
                "id_odpu CHAR(20)," +
                "rashod_type INTEGER," +
                "dat_uchet DATE," +
                "val_cnt FLOAT," +
                "id_ipu INTEGER," +
                "kod_serv DECIMAL(10,0)";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_odpu_p", columnNames);

            index.Clear();
            index.Add("ix1_unl_odpu_p", new List<string>() { "id_odpu", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_odpu_p", index);

            #endregion unl_odpu_p

            #region unl_ipu

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER NOT NULL," +
                "ls_id CHAR(20)," +
                "nzp_serv INTEGER," +
                "rashod_type INTEGER," +
                "serv_type INTEGER," +
                "counter_type CHAR(25)," +
                "cnt_stage INTEGER," +
                "mmnog INTEGER," +
                "num_cnt CHAR(20)," +
                "dat_uchet DATE," +
                "val_cnt FLOAT," +
                "nzp_measure INTEGER," +
                "dat_prov DATE," +
                "dat_provnext DATE," +
                "nzp_kvar INTEGER," +
                "nzp_counter INTEGER," +
                "local_id CHAR(20)," +
                "kod_serv CHAR(20)," +
                "doppar CHAR(25)";

            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_ipu", columnNames);

            index.Clear();
            index.Add("ix1_unl_ipu", new List<string>() { "local_id", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_ipu", index);

            #endregion unl_ipu

            #region unl_ipu_p

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER," +
                "id_ipu CHAR(20)," +
                "rashod_type INTEGER," +
                "dat_uchet DATE," +
                "val_cnt FLOAT," +
                "kod_serv INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_ipu_p", columnNames);

            index.Clear();
            index.Add("ix1_unl_ipu_p", new List<string>() { "id_ipu", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_area", index);

            #endregion unl_ipu_p

            #region unl_services

            columnNames =
                "id SERIAL  NOT NULL," +
                "id_serv INTEGER," +
                "service CHAR(100)," +
                "service2 CHAR(100)," +
                "nzp_file INTEGER," +
                "nzp_measure INTEGER," +
                "ed_izmer CHAR(30)," +
                "type_serv INTEGER," +
                "nzp_serv INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_services", columnNames);

            index.Clear();
            index.Add("ix1_unl_services", new List<string>() { "id_serv", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_services", index);

            #endregion unl_services

            #region unl_typeparams

            columnNames =
                "id SERIAL  NOT NULL," +
                "id_prm INTEGER," +
                "prm_name CHAR(100) default '1'," +
                "level_ INTEGER default 28," +
                "type_prm INTEGER default 2002," +
                "nzp_file INTEGER," +
                "nzp_prm INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_typeparams", columnNames);

            #endregion unl_typeparams

            #region unl_gaz

            columnNames =
                "id SERIAL NOT NULL," +
                "id_prm INTEGER," +
                "name CHAR(100)," +
                "nzp_file INTEGER," +
                "nzp_prm INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_gaz", columnNames);

            index.Clear();
            index.Add("ix1_unl_gaz", new List<string>() {"id_prm", "id"});
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_gaz", index);

            #endregion unl_gaz

            #region unl_voda

            columnNames =
                "id SERIAL  NOT NULL," +
                "id_prm INTEGER," +
                "name CHAR(100)," +
                "nzp_file INTEGER," +
                "nzp_prm INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_voda", columnNames);

            index.Clear();
            index.Add("ix1_unl_voda", new List<string>() { "id_prm", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_voda", index);

            #endregion unl_voda

            #region unl_blag

            columnNames =
                "id SERIAL NOT NULL," +
                "id_prm INTEGER," +
                "name CHAR(100)," +
                "nzp_file INTEGER," +
                "nzp_prm INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_blag", columnNames);

            #endregion unl_blag

            #region unl_paramsdom

            columnNames =
                "id SERIAL  NOT NULL," +
                "id_dom CHAR(20)," +
                "id_prm INTEGER," +
                "val_prm CHAR(100)," +
                "nzp_dom INTEGER," +
                "nzp_file INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_paramsdom", columnNames);

            index.Clear();
            index.Add("ix1_unl_paramsdom", new List<string>() { "id_dom", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_paramsdom", index);

            #endregion unl_paramsdom

            #region unl_paramsls

            columnNames =
                "id SERIAL  NOT NULL," +
                "ls_id CHAR(20)," +
                "id_prm INTEGER," +
                "val_prm CHAR(100)," +
                "num_ls INTEGER," +
                "nzp_file INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_paramsls", columnNames);

            index.Clear();
            index.Add("ix1_unl_paramsls", new List<string>() { "ls_id", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_paramsls", index);

            #endregion unl_paramsls
            
            #region unl_oplats

            columnNames =
                "id SERIAL  NOT NULL," +
                "ls_id CHAR(20)," +
                "type_oper INTEGER," +
                "numplat CHAR(80)," +
                "dat_opl DATE," +
                "dat_uchet DATE," +
                "dat_izm DATE," +
                "sum_oplat DECIMAL(14,2)," +
                "ist_opl CHAR(80)," +
                "mes_oplat DATE," +
                "nzp_file INTEGER," +
                "nzp_pack INTEGER," +
                "id_serv INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_oplats", columnNames);

            index.Clear();
            index.Add("ix1_unl_oplats", new List<string>() { "ls_id", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_oplats", index);

            #endregion unl_oplats

            #region unl_nedopost

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER," +
                "ls_id CHAR(20)," +
                "id_serv CHAR(20)," +
                "type_ned DECIMAL(10,0)," +
                "temper INTEGER," +
                "dat_nedstart DATE," +
                "dat_nedstop DATE," +
                "sum_ned DECIMAL(10,2)";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_nedopost", columnNames);

            index.Clear();
            index.Add("ix1_unl_nedopost", new List<string>() { "type_ned", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_nedopost", index);

            #endregion unl_nedopost
            
            #region unl_typenedopost

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER," +
                "type_ned DECIMAL(10,0)," +
                "ned_name CHAR(100)";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_typenedopost", columnNames);

            index.Clear();
            index.Add("ix1_unl_typenedopost", new List<string>() { "type_ned", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_typenedopost", index);

            #endregion unl_typenedopost
            
            #region unl_servls

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file INTEGER," +
                "ls_id DECIMAL(14,0)," +
                "id_serv CHAR(100)," +
                "dat_start DATE," +
                "dat_stop DATE," +
                "supp_id DECIMAL(18,0)";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_servls", columnNames);

            index.Clear();
            index.Add("ix1_unl_servls", new List<string>() { "ls_id", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_servls", index);

            #endregion unl_servls
            
            #region unl_pack

            columnNames =
                "id INTEGER," +
                "nzp_file INTEGER," +
                "dat_plat DATE," +
                "num_plat CHAR(20)," +
                "sum_plat DECIMAL(14,2)," +
                "kol_plat INTEGER";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_pack", columnNames);

            index.Clear();
            index.Add("ix1_unl_pack", new List<string>() { "num_plat", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_pack", index);

            #endregion unl_pack

            #region unl_reestr_ls

            columnNames =
                " nzp_file INTEGER," +
                "ls_id_supp INTEGER," +
                "ls_id_ns char(20)," +
                "ls_pkod char(20)," +
                "dat_open date," +
                "open_osnov char(100)," +
                "dat_close date," +
                "close_osnov char(100)," +
                "ls_id_sz char(20) ";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_reestr_ls", columnNames);

            #endregion unl_reestr_ls

            #region unl_vrub

            columnNames =
                "id SERIAL  NOT NULL," +
                "nzp_file   INTEGER," +
                "ls_id CHAR(20)," +
                "gil_id INTEGER," +
                "dat_vrvib DATE," +
                "dat_end DATE";
            CreateTable(Points.Pref + DBManager.sUploadAliasRest + "unl_vrub", columnNames);

            index.Clear();
            index.Add("ix1_unl_vrub", new List<string>() { "ls_id", "id" });
            CreateIndex(Points.Pref + DBManager.sUploadAliasRest + "unl_vrub", index);

            #endregion unl_vrub

          */
            ret.result = true;
            ret.text = "Таблицы для выгрузки успешно проверены";
            MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, true);
            return ret;
        }

    }
}
