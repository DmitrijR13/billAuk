
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.EXCHANGE
{
    public class UnlFormat_1_3_2 : DbDataUnload
    {
        public Returns UnlSection02_132_Urlic()
        {
            Returns ret = new Returns();
            DataTable tbl = new DataTable();

            ret = FillUrlicInfo();
            if (!ret.result)
            {
                //Todo сообщить об ошибке и продолжить работу
            }

            string sql =
                " SELECT  urlic_id, urlic_name, urlic_name_s," +
                " jur_address, fact_address, inn, kpp, " +
                " tel_chief, tel_b, chief_name, chief_post, b_name, " +
                " okonh1, okonh2, okpo, " +
                " post_and_name, is_area, is_supp, is_arendator, is_rc, " +
                " is_rso, is_agent, is_subabonent " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_urlic " +
                " WHERE nzp_file = " + NzpFile +
                " ORDER BY urlic_id";
            ClassDBUtils.OpenSQL(sql, tbl, ServerConnection, null);

            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                    "2" + Delimiter +
                    r["urlic_id"].ToString().Trim() + Delimiter +
                    r["urlic_name"].ToString().Trim() + Delimiter +
                    r["urlic_name_s"].ToString().Trim() + Delimiter +
                    r["jur_address"].ToString().Trim() + Delimiter +
                    r["fact_address"].ToString().Trim() + Delimiter +
                    r["inn"].ToString().Trim() + Delimiter +
                    r["kpp"].ToString().Trim() + Delimiter +
                    r["tel_chief"].ToString().Trim() + Delimiter +
                    r["tel_b"].ToString().Trim() + Delimiter +
                    r["chief_name"].ToString().Trim() + Delimiter +
                    r["chief_post"].ToString().Trim() + Delimiter +
                    r["b_name"].ToString().Trim() + Delimiter +
                    r["okonh1"].ToString().Trim() + Delimiter +
                    r["okonh2"].ToString().Trim() + Delimiter +
                    r["okpo"].ToString().Trim() + Delimiter +
                    r["post_and_name"].ToString().Trim() + Delimiter +
                    r["is_area"].ToString().Trim() + Delimiter +
                    r["is_supp"].ToString().Trim() + Delimiter +
                    r["is_arendator"].ToString().Trim() + Delimiter +
                    r["is_rc"].ToString().Trim() + Delimiter +
                    r["is_rso"].ToString().Trim() + Delimiter +
                    r["is_agent"].ToString().Trim() + Delimiter +
                    r["is_subabonent"].ToString().Trim() + Delimiter +
                    Environment.NewLine
                    );
            }

            return ret;
        }

        public Returns UnlSection03_132_Houses()
        {
            Returns ret = new Returns();
            
            FillHousesInfo();
            if (!ret.result)
            {
                //Todo сообщить об ошибке и продолжить работу
            }
            try
            {
                string sql =
                    " SELECT " +
                    " nzp_dom, town, rajon, ulica, ndom, nkor, nzp_area," +
                    " cat_blago, etazh, build_year, total_square, mop_square, useful_square, " +
                    " mo_id, params, ls_row_number, odpu_row_number, kod_kladr, kod_fias " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom a " +
                    " WHERE nzp_file = " + NzpFile +
                " ORDER BY nzp_dom";
                DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

                foreach (DataRow r in tbl.Rows)
                {
                    InfoText.Append(
                        "3" + Delimiter +
                        /*УКДС пока не записываем*/ Delimiter +
                        r["nzp_dom"].ToString().Trim() + Delimiter +
                        r["town"].ToString().Trim() + Delimiter +
                        r["rajon"].ToString().Trim() + Delimiter +
                        r["ulica"].ToString().Trim() + Delimiter +
                        r["ndom"].ToString().Trim() + Delimiter +
                        r["nkor"].ToString().Trim() + Delimiter +
                        r["nzp_area"].ToString().Trim() + Delimiter +
                        r["cat_blago"].ToString().Trim() + Delimiter +
                        r["etazh"].ToString().Trim() + Delimiter +
                        r["build_year"].ToString().Trim() + Delimiter +
                        r["total_square"].ToString().Trim() + Delimiter +
                        r["mop_square"].ToString().Trim() + Delimiter +
                        r["useful_square"].ToString().Trim() + Delimiter +
                        r["mo_id"].ToString().Trim() + Delimiter +
                        r["params"].ToString().Trim() + Delimiter +
                        r["ls_row_number"].ToString().Trim() + Delimiter +
                        r["odpu_row_number"].ToString().Trim() + Delimiter +
                        r["kod_kladr"].ToString().Trim() + Delimiter +
                        r["kod_fias"].ToString().Trim() + Delimiter +
                        Environment.NewLine
                        );
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка выполнения процедуры UnlSection03_132_Houses" + ex.Message);
            }

            return ret;
        }

        public Returns UnlSection04_132_Ls()
        {
            Returns ret = new Returns();

            FillLsInfo();

            string sql =
                 "SELECT nzp_kvar, nzp_dom, typek, fam, ima, otch, birth_date, " +
                 " nkvar, nkvar_n, open_date, opening_osnov, close_date, closing_osnov, " +
                 " kol_gil, kol_vrem_prib, kol_vrem_ub, room_number, " +
                 " total_square, living_square, otapl_square, naim_square, " +
                 " is_communal, is_el_plita, is_gas_plita, is_gas_colonka, is_fire_plita, " +
                 " gas_type, water_type, hotwater_type, canalization_type, is_open_otopl, " +
                 " service_row_number, reval_params_row_number, ipu_row_number, id_urlic," +
                 " type_owner, nzp_gil, uch, nzp_area " +
                 " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar " +
                 " WHERE nzp_file = " + NzpFile +
                " ORDER BY nzp_kvar";

            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                    "4" + Delimiter +
                    /*УКАС пока не записываем*/ Delimiter +
                    r["nzp_dom"].ToString().Trim() + Delimiter +
                    r["nzp_kvar"].ToString().Trim() + Delimiter +
                    r["typek"].ToString().Trim() + Delimiter +
                    r["fam"].ToString().Trim() + Delimiter +
                    r["ima"].ToString().Trim() + Delimiter +
                    r["otch"].ToString().Trim() + Delimiter +
                    r["birth_date"].ToString().Trim() + Delimiter +
                    r["nkvar"].ToString().Trim() + Delimiter + //10
                    r["nkvar_n"].ToString().Trim() + Delimiter +
                    r["open_date"].ToString().Trim() + Delimiter +
                    r["opening_osnov"].ToString().Trim() + Delimiter +
                    r["close_date"].ToString().Trim() + Delimiter +
                    r["closing_osnov"].ToString().Trim() + Delimiter +
                    r["kol_gil"].ToString().Trim() + Delimiter +
                    r["kol_vrem_prib"].ToString().Trim() + Delimiter +
                    r["kol_vrem_ub"].ToString().Trim() + Delimiter +
                    r["room_number"].ToString().Trim() + Delimiter +
                    r["total_square"].ToString().Trim() + Delimiter + //20
                    r["living_square"].ToString().Trim() + Delimiter +
                    r["otapl_square"].ToString().Trim() + Delimiter +
                    r["naim_square"].ToString().Trim() + Delimiter +
                    r["is_communal"].ToString().Trim() + Delimiter +
                    r["is_el_plita"].ToString().Trim() + Delimiter +
                    r["is_gas_plita"].ToString().Trim() + Delimiter +
                    r["is_gas_colonka"].ToString().Trim() + Delimiter +
                    r["is_fire_plita"].ToString().Trim() + Delimiter +
                    r["gas_type"].ToString().Trim() + Delimiter +
                    r["water_type"].ToString().Trim() + Delimiter + //30
                    r["hotwater_type"].ToString().Trim() + Delimiter +
                    r["canalization_type"].ToString().Trim() + Delimiter +
                    r["is_open_otopl"].ToString().Trim() + Delimiter +
                    Delimiter +
                    r["service_row_number"].ToString().Trim() + Delimiter +
                    r["reval_params_row_number"].ToString().Trim() + Delimiter +
                    r["ipu_row_number"].ToString().Trim() + Delimiter +
                    r["id_urlic"].ToString().Trim() + Delimiter +
                    r["type_owner"].ToString().Trim() + Delimiter +
                    r["nzp_gil"].ToString().Trim() + Delimiter + //40
                    r["uch"].ToString().Trim() + Delimiter +
                    r["nzp_area"].ToString().Trim() + Delimiter +
                    Environment.NewLine);
            }

            return ret;
        }

        public Returns UnlSection05_132_Contract()
        {
            Returns ret = new Returns();

            FillContractInfo();

            string sql =
                " SELECT nzp_supp, nzp_payer_agent, " +
                " nzp_payer_princip, nzp_payer_supp, name_supp, num_dog, dat_dog " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_dog " +
                " WHERE nzp_file = " + NzpFile +
                " ORDER BY nzp_supp";
            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                    "5" + Delimiter +
                    r["nzp_supp"].ToString().Trim() + Delimiter +
                    r["nzp_payer_agent"].ToString().Trim() + Delimiter +
                    r["nzp_payer_princip"].ToString().Trim() + Delimiter +
                    r["nzp_payer_supp"].ToString().Trim() + Delimiter +
                    r["name_supp"].ToString().Trim() + Delimiter +
                    r["num_dog"].ToString().Trim() + Delimiter +
                    r["dat_dog"].ToString().Trim() + Delimiter +
                    /*Комментарий*/ Delimiter +
                    Environment.NewLine);
            }
            return ret;
        }

        public Returns UnlSection06_132_Serv()
        {
            Returns ret = new Returns();
            
            FillServicesInfo();

            string sql =
                " SELECT nzp_kvar, nzp_supp, nzp_serv, sum_insaldo, eot, " +
                " reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod, norm_rashod, " +
                " is_pu_calc, sum_nach, sum_reval, sum_subsidy, sum_subsidyp, sum_lgota, " +
                " sum_lgotap, sum_smo, sum_smop, sum_money, is_del, sum_outsaldo, " +
                " servp_row_number, met_calc, pkod " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv " +
                " WHERE nzp_file = " + NzpFile +
                " ORDER BY nzp_kvar";
            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                        "6" + Delimiter +
                        r["nzp_kvar"].ToString().Trim() + Delimiter +
                        r["nzp_supp"].ToString().Trim() + Delimiter +
                        r["nzp_serv"].ToString().Trim() + Delimiter +
                        r["sum_insaldo"].ToString().Trim() + Delimiter +
                        r["eot"].ToString().Trim() + Delimiter +
                        r["reg_tarif_percent"].ToString().Trim() + Delimiter +
                        r["reg_tarif"].ToString().Trim() + Delimiter +
                        r["nzp_measure"].ToString().Trim() + Delimiter +
                        r["fact_rashod"].ToString().Trim() + Delimiter +
                        r["norm_rashod"].ToString().Trim() + Delimiter +
                        r["is_pu_calc"].ToString().Trim() + Delimiter +
                        r["sum_nach"].ToString().Trim() + Delimiter +
                        r["sum_reval"].ToString().Trim() + Delimiter +
                        r["sum_subsidy"].ToString().Trim() + Delimiter +
                        r["sum_subsidyp"].ToString().Trim() + Delimiter +
                        r["sum_lgota"].ToString().Trim() + Delimiter +
                        r["sum_lgotap"].ToString().Trim() + Delimiter +
                        r["sum_smo"].ToString().Trim() + Delimiter +
                        r["sum_smop"].ToString().Trim() + Delimiter +
                        r["sum_money"].ToString().Trim() + Delimiter +
                        r["is_del"].ToString().Trim() + Delimiter +
                        r["sum_outsaldo"].ToString().Trim() + Delimiter +
                        r["servp_row_number"].ToString().Trim() + Delimiter +
                        r["met_calc"].ToString().Trim() + Delimiter +
                        r["pkod"].ToString().Trim() + Delimiter +
                        Environment.NewLine);
            }
            return ret;
        }

        public Returns UnlSection09_Odpu()
        {
            Returns ret = new Returns();

            FillOdpuInfo();
            string sql =
               " SELECT nzp_dom, nzp_counter, nzp_serv, serv_type, counter_type, cnt_stage, mmnog, num_cnt, nzp_measure, dat_prov, dat_provnext, doppar " +
               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_odpu " +
               " WHERE nzp_file = " + NzpFile +
               " ORDER BY nzp_dom";
            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                        "9" + Delimiter +
                        r["nzp_dom"].ToString().Trim() + Delimiter +
                        r["nzp_counter"].ToString().Trim() + Delimiter +
                        r["nzp_serv"].ToString().Trim() + Delimiter +
                        r["serv_type"].ToString().Trim() + Delimiter +
                        r["counter_type"].ToString().Trim() + Delimiter +
                        r["cnt_stage"].ToString().Trim() + Delimiter +
                        r["mmnog"].ToString().Trim() + Delimiter +
                        r["num_cnt"].ToString().Trim() + Delimiter +
                        r["nzp_measure"].ToString().Trim() + Delimiter +
                        r["dat_prov"].ToString().Trim() + Delimiter +
                        r["dat_provnext"].ToString().Trim() + Delimiter +
                        r["doppar"].ToString().Trim() + Delimiter +
                        Environment.NewLine);
            }

            return ret;
        }

        public Returns UnlSection10_Pokazania_Odpu()
        {
            Returns ret = new Returns();

            string sql =
               " SELECT nzp_counter, rashod_type, dat_uchet, val_cnt " +
               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_odpu " +
               " WHERE nzp_file = " + NzpFile +
               " ORDER BY nzp_counter";
            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                        "10" + Delimiter +
                        r["nzp_counter"].ToString().Trim() + Delimiter +
                        r["rashod_type"].ToString().Trim() + Delimiter +
                        r["dat_uchet"].ToString().Trim() + Delimiter +
                        r["val_cnt"].ToString().Trim() + Delimiter +
                        Environment.NewLine);
            }

            return ret;
        }

        public Returns UnlSection11_Ipu()
        {
            Returns ret = new Returns();

            FillIpuInfo();

            string sql =
               " SELECT nzp_kvar, nzp_counter, nzp_serv, serv_type, counter_type, cnt_stage, mmnog, num_cnt, nzp_measure, dat_prov, dat_provnext, doppar " +
               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu " +
               " WHERE nzp_file = " + NzpFile +
               " ORDER BY nzp_kvar";
            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                        "11" + Delimiter +
                        r["nzp_kvar"].ToString().Trim() + Delimiter +
                        r["nzp_counter"].ToString().Trim() + Delimiter +
                        r["nzp_serv"].ToString().Trim() + Delimiter +
                        r["serv_type"].ToString().Trim() + Delimiter +
                        r["counter_type"].ToString().Trim() + Delimiter +
                        r["cnt_stage"].ToString().Trim() + Delimiter +
                        r["mmnog"].ToString().Trim() + Delimiter +
                        r["num_cnt"].ToString().Trim() + Delimiter +
                        r["nzp_measure"].ToString().Trim() + Delimiter +
                        r["dat_prov"].ToString().Trim() + Delimiter +
                        r["dat_provnext"].ToString().Trim() + Delimiter +
                        r["doppar"].ToString().Trim() + Delimiter +
                        Environment.NewLine);
            }

            return ret;
        }

        public Returns UnlSection12_Pokazania_Ipu()
        {
            Returns ret = new Returns();

            string sql =
               " SELECT nzp_counter, rashod_type, dat_uchet, val_cnt, nzp_serv " +
               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu " +
               " WHERE nzp_file = " + NzpFile +
               " ORDER BY nzp_counter";
            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                        "12" + Delimiter +
                        r["nzp_counter"].ToString().Trim() + Delimiter +
                        r["rashod_type"].ToString().Trim() + Delimiter +
                        r["dat_uchet"].ToString().Trim() + Delimiter +
                        r["val_cnt"].ToString().Trim() + Delimiter +
                        r["nzp_serv"].ToString().Trim() + Delimiter +
                        Environment.NewLine);
            }

            return ret;
        }

        public Returns UnlSection14_MoInfo()
        {
            Returns ret = new Returns();

            FillMoInfo();

            string sql =
               " SELECT nzp_vill, vill_name, rajon, nzp_raj, kod_kladr " +
               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_mo " +
               " WHERE nzp_file = " + NzpFile +
               " ORDER BY nzp_vill";
            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                        "14" + Delimiter +
                        r["nzp_vill"].ToString().Trim() + Delimiter +
                        r["vill_name"].ToString().Trim() + Delimiter +
                        r["rajon"].ToString().Trim() + Delimiter +
                        r["nzp_raj"].ToString().Trim() + Delimiter +
                        r["kod_kladr"].ToString().Trim() + Delimiter +
                        Environment.NewLine);
            }

            return ret;
        }

        public Returns UnlSection15_GilecInfo()
        {
            Returns ret = new Returns();

            FillGilecInfo();

            string sql =
               " SELECT nzp_kvar, nzp_gil, nzp_kart, nzp_tkrt, fam, ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, gender, nzp_dok, serij, nomer, " +
               " vid_dat, vid_mes, kod_podrazd, strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, strana_op, region_op, okrug_op, gorod_op, npunkt_op, rem_op, " +
               " strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp, dat_prop, dat_oprp, dat_pvu, who_pvu, dat_svu, namereg, kod_namereg, " +
               " rod, nzp_celp, nzp_celu, dat_sost, dat_ofor " +
               " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_gilec " +
               " WHERE nzp_file = " + NzpFile +
               " ORDER BY nzp_kvar";
            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                        "15" + Delimiter +
                        r["nzp_kvar"].ToString().Trim() + Delimiter +
                        r["nzp_gil"].ToString().Trim() + Delimiter +
                        r["nzp_kart"].ToString().Trim() + Delimiter +
                        r["nzp_tkrt"].ToString().Trim() + Delimiter +
                        r["fam"].ToString().Trim() + Delimiter +
                        r["ima"].ToString().Trim() + Delimiter +
                        r["otch"].ToString().Trim() + Delimiter +
                        r["dat_rog"].ToString().Trim() + Delimiter +
                        r["fam_c"].ToString().Trim() + Delimiter +
                        r["ima_c"].ToString().Trim() + Delimiter +
                        r["otch_c"].ToString().Trim() + Delimiter +
                        r["dat_rog_c"].ToString().Trim() + Delimiter +
                        r["gender"].ToString().Trim() + Delimiter +
                        r["nzp_dok"].ToString().Trim() + Delimiter +
                        r["serij"].ToString().Trim() + Delimiter +
                        r["nomer"].ToString().Trim() + Delimiter +
                        r["vid_dat"].ToString().Trim() + Delimiter +
                        r["vid_mes"].ToString().Trim() + Delimiter +
                        r["kod_podrazd"].ToString().Trim() + Delimiter +
                        r["strana_mr"].ToString().Trim() + Delimiter +
                        r["region_mr"].ToString().Trim() + Delimiter +
                        r["okrug_mr"].ToString().Trim() + Delimiter +
                        r["gorod_mr"].ToString().Trim() + Delimiter +
                        r["npunkt_mr"].ToString().Trim() + Delimiter +
                        r["strana_op"].ToString().Trim() + Delimiter +
                        r["region_op"].ToString().Trim() + Delimiter +
                        r["okrug_op"].ToString().Trim() + Delimiter +
                        r["gorod_op"].ToString().Trim() + Delimiter +
                        r["npunkt_op"].ToString().Trim() + Delimiter +
                        r["rem_op"].ToString().Trim() + Delimiter +
                        r["strana_ku"].ToString().Trim() + Delimiter +
                        r["region_ku"].ToString().Trim() + Delimiter +
                        r["okrug_ku"].ToString().Trim() + Delimiter +
                        r["gorod_ku"].ToString().Trim() + Delimiter +
                        r["npunkt_ku"].ToString().Trim() + Delimiter +
                        r["rem_ku"].ToString().Trim() + Delimiter +
                        r["rem_p"].ToString().Trim() + Delimiter +
                        r["tprp"].ToString().Trim() + Delimiter +
                        r["dat_prop"].ToString().Trim() + Delimiter +
                        r["dat_oprp"].ToString().Trim() + Delimiter +
                        r["dat_pvu"].ToString().Trim() + Delimiter +
                        r["who_pvu"].ToString().Trim() + Delimiter +
                        r["dat_svu"].ToString().Trim() + Delimiter +
                        r["namereg"].ToString().Trim() + Delimiter +
                        r["kod_namereg"].ToString().Trim() + Delimiter +
                        r["rod"].ToString().Trim() + Delimiter +
                        r["nzp_celp"].ToString().Trim() + Delimiter +
                        r["nzp_celu"].ToString().Trim() + Delimiter +
                        r["dat_sost"].ToString().Trim() + Delimiter +
                        r["dat_ofor"].ToString().Trim() + Delimiter +
                        Environment.NewLine);
            }

            return ret;
        }


        public override Returns Run(FilesExchange finder)
        {
            Returns ret = new Returns();
            UnloadUtils exchUtils = new UnloadUtils();
            try
            {
                ret = exchUtils.CheckTablesForUnl();
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка проверки таблиц для выгрузки: " + ret.text, MonitorLog.typelog.Error,
                        true);
                    ret.text = "Ошибка проверки таблиц для выгрузки!";
                    ret.tag = -1;
                }

                #region заполнение полей класса из finder'a

                NzpVersion = finder.nzp_version;
                NzpUser = finder.nzp_user;
                SelectedSections = finder.selectedSections;
                NzpUser = finder.nzp_user;
                BankPref = finder.bankPref;
                VersionName = finder.versionName;
                FileExstention = finder.fileExtension;
                SelectedDate = finder.selectedDate;
                FullUnlFileName = finder.fullFileName;

                #endregion заполнение полей класса из finder'a


                //Здесь заполняется NzpFile
                GetNzpFile(VersionName);

                MonitorLog.WriteLog("Старт выгрузки 'Хар-ки ЖФ и начисления ЖКУ'" + Environment.NewLine +
                                    "Формат '" + VersionName + "', " + Environment.NewLine +
                                    "Выгружаемая дата: " + SelectedDate + Environment.NewLine +
                                    "Банк данных: " + BankPref + "; " + Environment.NewLine +
                                    "Расширение выгружаемого файла: '" + FileExstention + "'" + Environment.NewLine +
                                    "Разделитель: " + Delimiter + Environment.NewLine +
                                    "Уникальный код файла: " + NzpFile + Environment.NewLine, MonitorLog.typelog.Info,
                    true);

                #region заполнение dictionary ссылками на вызываемые методы

                SectionMethods.Clear();
                SectionMethods.Add("Urlic132",      UnlSection02_132_Urlic);
                SectionMethods.Add("Houses132",     UnlSection03_132_Houses);
                SectionMethods.Add("Ls132",         UnlSection04_132_Ls);
                SectionMethods.Add("Supp132",       UnlSection05_132_Contract);
                SectionMethods.Add("Serv132",       UnlSection06_132_Serv);
                SectionMethods.Add("Odpu",          UnlSection09_Odpu);
                SectionMethods.Add("PokazaniaOdpu", UnlSection10_Pokazania_Odpu);
                SectionMethods.Add("Ipu",           UnlSection11_Ipu);
                SectionMethods.Add("PokazaniaIpu",  UnlSection12_Pokazania_Ipu);
                SectionMethods.Add("MoInfo",        UnlSection14_MoInfo);
                SectionMethods.Add("GilecInfo",     UnlSection15_GilecInfo);
                
                #endregion заполнение dictionary ссылками на вызываемые методы

                //создание шапки файла
                FillFileHeadInfo();

                foreach (var item in SelectedSections)
                {
                    if (item.Value)
                    {
                        //TODO: добавить try-catch
                        SectionMethods[item.Key]();
                    }
                }
                
                FullUnlFileName = InputOutput.GetOutputDir() + @"export/Unl_" + VersionName + "_" + SelectedDate.ToShortDateString() + "_(" +
                               DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") +")" + FileExstention;

                Dictionary<StringBuilder, string> files = new Dictionary<StringBuilder, string>();

                files.Add(InfoText, FullUnlFileName);
                files.Add(LogInfo, FullUnlFileName.Replace(FileExstention, "_log.log"));
                Compress(files);
                
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выгрузки! " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                MonitorLog.WriteLog("Ошибка при выполнении выгрузки!", MonitorLog.typelog.Info, true);
                ret.text = "Ошибка при выполнении выгрузки! Смотрите лог ошибок";
                ret.tag = -1;
                ret.result = false;
                return ret;

            }

            ret.text = "Успешно выгружено";
            ret.tag = -1;
            ret.result = true;
            return ret;
        }
    }

    public class vodok : DbDataUnload
    {
        public override Returns Run(FilesExchange s)
        {
            return new Returns();
        }
    }
}
