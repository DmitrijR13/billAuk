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
    public class UnlFormat_1_2_2 : DbDataUnload
    {
        public Returns UnlSection02_Uk()
        {
            Returns ret = new Returns();
            DataTable tbl = new DataTable();

            ret = FillUkInfo();

            string sql =
                " SELECT a.nzp_area, a.area, a.jur_address, a.fact_address, " +
                " a.inn, a.kpp, a.rs, a.bank, a.bik, a.ks " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_area a" +
                " WHERE nzp_file = " + NzpFile;
            ClassDBUtils.OpenSQL(sql, tbl, ServerConnection, null);

            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                    "2" + Delimiter + 
                    r["nzp_area"].ToString().Trim() + Delimiter +
                    r["area"].ToString().Trim() + Delimiter +
                    r["jur_address"].ToString().Trim() + Delimiter +
                    r["fact_address"].ToString().Trim() + Delimiter +
                    r["inn"].ToString().Trim() + Delimiter +
                    r["kpp"].ToString().Trim() + Delimiter +
                    r["rs"].ToString().Trim() + Delimiter +
                    r["bank"].ToString().Trim() + Delimiter +
                    r["bik"].ToString().Trim() + Delimiter +
                    r["ks"].ToString().Trim() + Delimiter +
                    Environment.NewLine
                    );
            }

            return ret;
        }

        public Returns UnlSection03_Houses()
        {
            Returns ret = new Returns();

            FillHousesInfo();

            try
            {
                string sql =
                    " SELECT " +
                    " nzp_dom, town, rajon, ulica, ndom, nkor, nzp_area, " +
                    " cat_blago, etazh, build_year, total_square, mop_square, useful_square, " +
                    " mo_id, params, ls_row_number, odpu_row_number, kod_kladr " +
                    " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom a " +
                    " WHERE nzp_file = " + NzpFile;
                DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

                //TODO: добавить счетчик добавленной инфы для каждой секции
                foreach (DataRow r in tbl.Rows)
                {
                    InfoText.Append(
                        "3" + Delimiter +
                        r["nzp_dom"].ToString().Trim() + Delimiter +
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
                        Environment.NewLine
                        );
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка выполнения процедуры UnloadSection_03" + ex.Message);
            }

            return ret;
        }

        public Returns UnlSection04_Ls()
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
                " service_row_number, reval_params_row_number, ipu_row_number, id_urlic " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar " +
                " WHERE nzp_file = " + NzpFile;

            DataTable tbl = ClassDBUtils.OpenSQL(sql, ServerConnection, null).resultData;

            //TODO: добавить счетчик добавленной инфы для каждой секции
            foreach (DataRow r in tbl.Rows)
            {
                InfoText.Append(
                    "4" + Delimiter + 
                    r["nzp_kvar"].ToString().Trim() + Delimiter +
                    r["nzp_dom"].ToString().Trim() + Delimiter +
                    r["nzp_kvar"].ToString().Trim() + Delimiter +
                    r["typek"].ToString().Trim() + Delimiter +
                    r["fam"].ToString().Trim() + Delimiter +
                    r["ima"].ToString().Trim() + Delimiter +
                    r["otch"].ToString().Trim() + Delimiter +
                    r["birth_date"].ToString().Trim() + Delimiter +
                    r["nkvar"].ToString().Trim() + Delimiter +
                    r["nkvar_n"].ToString().Trim() + Delimiter +
                    r["open_date"].ToString().Trim() + Delimiter +
                    r["opening_osnov"].ToString().Trim() + Delimiter +
                    r["close_date"].ToString().Trim() + Delimiter +
                    r["closing_osnov"].ToString().Trim() + Delimiter +
                    r["kol_gil"].ToString().Trim() + Delimiter +
                    r["kol_vrem_prib"].ToString().Trim() + Delimiter +
                    r["kol_vrem_ub"].ToString().Trim() + Delimiter +
                    r["room_number"].ToString().Trim() + Delimiter +
                    r["total_square"].ToString().Trim() + Delimiter +
                    r["living_square"].ToString().Trim() + Delimiter +
                    r["otapl_square"].ToString().Trim() + Delimiter +
                    r["naim_square"].ToString().Trim() + Delimiter +
                    r["is_communal"].ToString().Trim() + Delimiter +
                    r["is_el_plita"].ToString().Trim() + Delimiter +
                    r["is_gas_plita"].ToString().Trim() + Delimiter +
                    r["is_gas_colonka"].ToString().Trim() + Delimiter +
                    r["is_fire_plita"].ToString().Trim() + Delimiter +
                    r["gas_type"].ToString().Trim() + Delimiter +
                    r["water_type"].ToString().Trim() + Delimiter +
                    r["hotwater_type"].ToString().Trim() + Delimiter +
                    r["canalization_type"].ToString().Trim() + Delimiter +
                    r["is_open_otopl"].ToString().Trim() + Delimiter +
                    r["service_row_number"].ToString().Trim() + Delimiter +
                    r["reval_params_row_number"].ToString().Trim() + Delimiter +
                    r["ipu_row_number"].ToString().Trim() + Delimiter +
                    r["id_urlic"].ToString().Trim() + Delimiter +
                    Environment.NewLine);
            }

            return ret;
        }

        public Returns UnlSection05_Supp()
        {
            Returns ret = new Returns();
            return ret;
        }

        public Returns UnlSection06_Serv()
        {
            Returns ret = new Returns();
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

                MonitorLog.WriteLog("Старт выгрузки 'Хар-ки ЖФ и начисления ЖКУ'" +
                                    "Формат '" + VersionName + "', " + Environment.NewLine +
                                    "Выгружаемая дата: " + SelectedDate + Environment.NewLine +
                                    "Банк данных: " + BankPref + "; " + Environment.NewLine +
                                    "Расширение выгружаемого файла: '" + FileExstention + "'" + Environment.NewLine +
                                    "Разделитель: " + Delimiter + Environment.NewLine +
                                    "Уникальный код файла: " + NzpFile + Environment.NewLine, MonitorLog.typelog.Info,
                    true);

                #region заполнение dictionary ссылками на вызываемые методы

                SectionMethods.Clear();
                SectionMethods.Add("Uk122",     UnlSection02_Uk);
                SectionMethods.Add("Houses122", UnlSection03_Houses);
                SectionMethods.Add("Ls122",     UnlSection04_Ls);
                SectionMethods.Add("Supp122",   UnlSection05_Supp);
                SectionMethods.Add("Serv122",   UnlSection06_Serv);

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
                
                FullUnlFileName = InputOutput.GetOutputDir() + @"export/Unl_" + VersionName + "_" + SelectedDate.ToShortDateString() + "_" +
                               DateTime.Now.Ticks + FileExstention;

                Dictionary<StringBuilder, string> files = new Dictionary<StringBuilder, string>();

                files.Add(InfoText, FullUnlFileName);
                Compress(files);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выгрузки! " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                MonitorLog.WriteLog("Ошибка при выполнении выгрузки!", MonitorLog.typelog.Info, true);
                ret.text = "Ошибка при выполнении выгрузки! Смотрите лог ошибок";
                ret.tag = -1;
                ret.result = false;

            }
            
            ret.text = "Успешно выгружено";
            ret.tag = -1;
            ret.result = true;
            return ret;
        }

    }
}
