using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.SzExchange.LoadFromSz
{
    public class LoadFileFromSz : DataBaseHeadServer
    {
        private FilesImported _finder;

        public Returns LoadFile(FilesImported finder, ref Returns ret, IDbConnection conn_db)
        {
            _finder = finder;
            string[] _fullName;

            //записываем в "Мои файлы"
            int nzpExc = DbFileLoader.AddMyFile("Загрузка из СЗ", _finder);

            //разархивируем файл
            //string _fullFileName = DbFileLoader.DecompressionFile(_finder.saved_name, InputOutput.GetInputDir(), ".txt", ref ret);
            _fullName = DecompressFile(_finder.saved_name, InputOutput.GetInputDir(), ".txt", ref ret);
            

            DbFileLoader fl = new DbFileLoader(ServerConnection)
            {
                _fDirectory = InputOutput.GetInputDir(),
                _finder = this._finder
            };

            //записываем в files_imported и получаем уникальный код загрузки
            //передаем тип файла 2 - загрузка из СЗ
            fl._finder.nzp_file = _finder.nzp_file = fl.InsertIntoFiles_imported(ref ret);

            //Выставление статуса успешной загрузки файла
            fl.SaveAndSetStat(nzpExc, ref ret);

            //считывание файла в массив строк и передача в ф-цию
            foreach (string str in _fullName)
            {
                if (str != null)
                {
                    ret = Run(DbFileLoader.ReadFile(str), finder, conn_db);
                    if (!ret.result)
                        return ret;
                }
            }
            //ret = Run(DbFileLoader.ReadFile(_fullFileName));

            return ret;
        }

        private string[] DecompressFile(string fileName, string fDirectory, string extens, ref Returns ret)
        {
            string fullFileName = Path.Combine(fDirectory, System.IO.Path.GetFileName(fileName));
            string[] fullName = new string[3];

            if (InputOutput.useFtp)
            {
                if (!InputOutput.DownloadFile(fileName, fullFileName))
                {
                    throw new Exception("Ошибка выполнения процедуры DecompressionFile: " +
                                        "Не удалось скопировать с ftp сервера файл " + fileName + " в файл " +
                                        fullFileName);
                }

            }
            try
            {
                string[] files = Archive.GetInstance(fullFileName).Decompress(fullFileName, fDirectory);
                fullName[0] = Path.Combine(fDirectory, files[0]);
                fullName[1] = Path.Combine(fDirectory, files[1]);
                fullName[2] = Path.Combine(fDirectory, files[2]);
                
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка выполнения процедуры DecompressionFile. Название файла: " +
                                    fullFileName +
                                    Environment.NewLine + ex.Message);
            }
            
            return fullName;
        }

        private Returns Run(string[] fileStrings, FilesImported finder, IDbConnection conn_db)
        {
            Returns ret = new Returns();
            ret = Utils.InitReturns();
            string[] listStr;
            string[] checkSec;

            checkSec = fileStrings[0].Split(new char[] { '|' }, StringSplitOptions.None);

            try
            {
                DropTable();

                #region Загрузка сумм субсидий и льгот по услугам

                if (checkSec[0] == "begin=Начисленные суммы субсидий и льгот по услугам ")
                {
                    CreateTableServ();

                    foreach (string str in fileStrings)
                    {
                        if (str.Trim() == "")
                        {
                            continue;
                        }

                        listStr = str.Split(new char[] {'|'}, StringSplitOptions.None);

                        if (listStr[0] == "begin=Начисленные суммы субсидий и льгот по услугам ")
                        {
                            finder.month = listStr[3].Substring(4, 2);
                            finder.year = listStr[3].Substring(9, 2);
                            WriteServHead(listStr);
                            continue;
                        }

                        if (listStr[0] == "end=Начисленные суммы субсидий и льгот по услугам ")
                        {
                            WriteServEnd(listStr);
                            continue;
                        }

                        WriteServSub(listStr, finder, conn_db);
                    }
                }

                #endregion Загрузка сумм субсидий и льгот по услугам

                #region Загрузка субсидий и льгот по жильцам

                if (checkSec[0] == "begin=Начисленные суммы субсидий и льгот по жильцам ")
                {
                    CreateTableGil();

                    foreach (string str in fileStrings)
                    {
                        if (str.Trim() == "")
                        {
                            continue;
                        }

                        listStr = str.Split(new char[] {'|'}, StringSplitOptions.None);

                        if (listStr[0] == "begin=Начисленные суммы субсидий и льгот по жильцам ")
                        {
                                finder.month = listStr[3].Substring(4, 2);
                                finder.year = listStr[3].Substring(9, 2);
                                WriteGilHead(listStr);
                                continue;                     
                        }

                        if (listStr[0] == "end=Начисленные суммы субсидий и льгот по жильцам ")
                        {    
                            WriteGilEnd(listStr);
                            continue;
                        }

                        ret = WriteGilSub(listStr, finder, conn_db);
                        if (!ret.result)
                            return ret;
                    }
                }
                #endregion Загрузка субсидий и льгот по жильцам

                #region Загрузка характеристик жилья

                if (checkSec[0] == "begin=Характеристики жилья ")
                {
                    CreateTableCharGil();

                    foreach (string str in fileStrings)
                    {
                        if (str.Trim() == "")
                        {
                            continue;
                        }

                        listStr = str.Split(new char[] {'|'}, StringSplitOptions.None);

                        if (listStr[0] == "begin=Характеристики жилья ")
                        {
                            finder.month = listStr[3].Substring(4, 2);
                            finder.year = listStr[3].Substring(9, 2);
                            WriteCharGilHead(listStr);
                            continue;
                        }

                        if (listStr[0] == "end=Характеристики жилья ")
                        {
                            WriteCharGilEnd(listStr);
                            continue;
                        }

                        WriteCharGilSub(listStr, finder, conn_db);
                    }

                }

                #endregion Загрузка характеристик жилья
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Run: ошибка  " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Run: ошибка";
                ret.result = false;
                return ret;
            }
            finally
            {
                DropTable();
            }
            return ret;  
        }

        protected virtual DataTable ExecSQLToTable(string sql, IDbConnection conn_db)
        {
            return DBManager.ExecSQLToTable(conn_db, sql);
        }

        private void CheckData()
        {
            
        }

        private string CheckOnEmpty(string data)
        {
            string ret = "'" + data + "'";

            if (ret == "'  '")
                ret = "null";

            return ret;
        }

        private void CreateTableServ()
        {
            string sql = "";
            
            sql =
                " CREATE TABLE file_serv_head(" +
                " file_type CHAR(100), " +
                " num_file_type CHAR(20), " +
                " org_unit CHAR(40), " +
                " month_and_year DATE, " +
                " file_date DATE, " +
                " unload_num INTEGER)";
            ExecSQL(sql);

            sql =
                " CREATE TABLE file_serv_end(" +
                " file_type CHAR(100), " +
                " num_file_type CHAR(20), " +
                " date DATE, " +
                " reserv CHAR(300))";
            ExecSQL(sql);

            sql =
                " CREATE TABLE file_serv_sub( " +
                " pss DECIMAL(12, 0), " +
                " num_ls INTEGER, " +
                " cod_serv INTEGER, " +
                " sum_nuch_sub DECIMAL(12, 2), " +
                " sum_nuch_edv DECIMAL(12, 2), " +
                " sum_sub_smo_all DECIMAL(12, 2), " +
                " sum_sub_smo_rf DECIMAL(12, 2), " +
                " sum_sub_smo_rt DECIMAL(12, 2), " +
                " sum_sub_otopl DECIMAL(12, 2), " +
                " sum_nuch_sub_rev DECIMAL(12, 2), " +
                " sum_nuch_edv_rev DECIMAL(12, 2), " +
                " sum_sub_smo_all_rev DECIMAL(12, 2), " +
                " sum_sub_smo_rf_rev DECIMAL(12, 2), " +
                " sum_sub_smo_rt_rev DECIMAL(12, 2), " +
                " sum_sub_otopl_rev DECIMAL(12, 2))";
            ExecSQL(sql);

        }

        private void CreateTableGil()
        {
            string sql = "";

            sql =
                " CREATE TABLE file_gil_head(" +
                " file_type CHAR(100), " +
                " num_file_type CHAR(20), " +
                " org_unit CHAR(40), " +
                " month_and_year DATE, " +
                " file_date DATE, " +
                " unload_num INTEGER)";
            ExecSQL(sql);

            sql =
                " CREATE TABLE file_gil_end(" +
                " file_type CHAR(100), " +
                " num_file_type CHAR(20), " +
                " date DATE, " +
                " reserv CHAR(300) " +
                //" num_household INTEGER, " +
                //" sum_insaldo_all DECIMAL(12, 2), " +
                //" reserv CHAR(300), " +
                //" sum_avanc_all DECIMAL(12, 2), " +
                //" sum_nach_sub_all DECIMAL(12, 2), " +
                //" sum_sub_rev_all DECIMAL(12, 2), " +
                //" reserv1 DECIMAL(12, 2), " +
                //" sum_viplat_all DECIMAL(12, 2)" +
                ")";
            ExecSQL(sql);

            sql =
                " CREATE TABLE file_gil_sub( " +
                " pss DECIMAL(12, 0), " +
                " num_ls INTEGER, " +
                " fam CHAR(30), " +
                " ima CHAR(30), " +
                " otch CHAR(30), " +
                " born_date DATE, " +
                " cod_sum INTEGER, " +
                " cod_sum_serv INTEGER, " +
                " bank_name CHAR(40), " +
                " date_start DATE, " +
                " sum_insaldo DECIMAL(12, 2), " +
                " reserv DECIMAL(12, 2), " +
                " sum_nach_sub DECIMAL(12, 2), " +
                " sum_nach_sub_rev DECIMAL(12, 2), " +
                " reserv1 DECIMAL(12, 2), " +
                " sum_avanc DECIMAL(12, 2), " +
                " sum_viplat DECIMAL(12, 2), " +
                " sum_sub_smo DECIMAL(12, 2), " +
                " sum_sub_smo_rt DECIMAL(12, 2), " +
                " sum_sub_otopl DECIMAL(12, 2), " +
                " sum_sub_smo_rev DECIMAL(12, 2), " +
                " sum_sub_smo_rt_rev DECIMAL(12, 2), " +
                " sum_sub_otopl_rev DECIMAL(12, 2), " +
                " sum_change_sub DECIMAL(12, 2), " +
                " date_finish DATE" +
                ")";
            ExecSQL(sql); 
        }

        private void CreateTableCharGil()
        {
            string sql = "";

            sql =
                " CREATE TABLE file_char_gil_head(" +
                " file_type CHAR(100), " +
                " num_file_type CHAR(20), " +
                " org_unit CHAR(40), " +
                " month_and_year DATE, " +
                " file_date DATE, " +
                " unload_num INTEGER)";
            ExecSQL(sql);

            sql =
                " CREATE TABLE file_char_gil_end(" +
                " file_type CHAR(100), " +
                " num_file_type CHAR(20), " +
                " date DATE, " +
                " reserv CHAR(300))";
            ExecSQL(sql);

            sql =
                " CREATE TABLE file_char_gil_sub( " +
                " pss DECIMAL(12, 0), " +
                " num_ls INTEGER, " +
                " gross_area DECIMAL(12, 2), " +
                " cod_mo DECIMAL(12, 2), " +
                " cod_paket_blag INTEGER, " +
                " date_finish DATE" +
                ")";
            ExecSQL(sql);
        }

        private void DropTable()
        {
            string sql = "";

            sql =
                " DROP TABLE file_serv_head";
            ExecSQL(sql);

            sql =
                " DROP TABLE file_gil_head";
            ExecSQL(sql);

            sql =
                " DROP TABLE file_char_gil_head";
            ExecSQL(sql);

            sql =
                " DROP TABLE file_serv_end";
            ExecSQL(sql);

            sql =
                " DROP TABLE file_gil_end";
            ExecSQL(sql);

            sql =
                " DROP TABLE file_char_gil_end";
            ExecSQL(sql);

            sql =
                " DROP TABLE file_serv_sub";
            ExecSQL(sql);

            sql =
                " DROP TABLE file_gil_sub";
            ExecSQL(sql);

            sql =
                " DROP TABLE file_char_gil_sub";
            ExecSQL(sql);
        }

        #region Заполнение секции  "Начисленные суммы субсидий и льгот по услугам"
        public void WriteServHead(string[] str)
        {
            string sql = "";

            sql =
            " INSERT INTO file_serv_head" +
            " (file_type, num_file_type, org_unit, month_and_year, file_date, unload_num) " +
            " VALUES ('" + str[0].Trim() + "', " + str[1] + ", " + str[2] + ", '" + str[3] + "', '" + str[4] + "', " + str[5] + ") ";
            ExecSQL(sql);
        }

        public void WriteServEnd(string[] str)
        {
            string sql = "";

            sql =
            " INSERT INTO file_serv_end" +
            " (file_type, num_file_type, date, reserv) " +
            " VALUES ('" + str[0].Trim() + "', " + str[1] + ", '" + str[2] + "', '" + str[3] + "') ";
            ExecSQL(sql);
        }

        public void WriteServSub(string[] str, FilesImported finder, IDbConnection conn_db)
        {
            string sql;
            string bank = finder.bank;
            string month = finder.month;
            string year = finder.year;
            string sTmpStr;
            bool bFlErr;

            try
            {
                //sql =
                //    " INSERT INTO file_serv_sub" +
                //    " (pss, num_ls, cod_serv, sum_nuch_sub, sum_nuch_edv, sum_sub_smo_all, " +
                //    " sum_sub_smo_rf, sum_sub_smo_rt, sum_sub_otopl, sum_nuch_sub_rev, sum_nuch_edv_rev, sum_sub_smo_all_rev, " +
                //    " sum_sub_smo_rf_rev, sum_sub_smo_rt_rev, sum_sub_otopl_rev) " +
                //    " VALUES (" + str[0] + ", " + str[1] + ", " + str[2] + ", " + str[3] + ", " + str[4] + ", " + str[5] +
                //    ", " + str[6] + ", " + str[7] + ", " + str[8] + ", " + str[9] + ", " + str[10] + ", " + str[11] + ", " +
                //    str[12] + ", " + str[13] + ", " + str[14] + ") ";
                //ExecSQL(sql);

                bFlErr = false;

                //Ищем ПСС
                sql =
                        " SELECT * " +
                        " FROM " + bank + "_data.prm_15 p, " + bank + "_data.kvar k" +
                        " WHERE k.nzp_kvar = p.nzp " +
                        " AND k.num_ls = " + str[1].Trim() +
                        " AND p.nzp_prm = 162 " +
                        " AND p.is_actual = 1 " +
                        " AND p.val_prm = '" + str[0].Trim() + "'";
                DataTable dt = ExecSQLToTable(sql, conn_db);

                if (dt.Rows.Count == 0)
                {
                    bFlErr = true;
                    sTmpStr = "ЛС: " + str[1].Trim() + " ПСС: " + str[0].Trim() + " - не найден ПСС или ЛС";
                    MonitorLog.WriteLog(sTmpStr, MonitorLog.typelog.Error, true);
                }

                if (!bFlErr)
                {
                    //Удаляем старую запись в БД
                    sql =
                        " DELETE FROM " + bank + "_charge_" + year + ".calc_sz_" + month +
                        " WHERE pss = " + str[0].Trim() +
                        " AND num_ls = " + str[1].Trim() +
                        " AND nzp_serv = " + str[2].Trim();
                    ExecSQL(sql);

                    //Запись в БД
                    sql =
                        " INSERT INTO " + bank + "_charge_" + year + ".calc_sz_" + month +
                        " (pss, num_ls, nzp_serv, " +
                        " sum_reval, sum_lgota_l, sum_edv_l, sum_subs_l, " +
                        " sum_smo_rf_l, sum_smo_rt_l, sum_smo_otop_l, sum_lgota_c, " +
                        " sum_edv_c, sum_subs_c, sum_smo_rf_c, sum_smo_rt_c, " +
                        " sum_smo_otop_c, sum_lgota_p, sum_reval_edv, sum_subs_p, " +
                        " sum_smo_rf_p, sum_smo_rt_p, sum_smo_otop_p) " +
                        " VALUES(" +
                        str[0] + ", " + str[1] + ", " + str[2] + "," +
                        " 0, 0, 0, 0, " +
                        " 0, 0, 0, " + str[3] + "," +
                        str[4] + ", " + str[5] + ", " + str[6] + ", " + str[7] + "," +
                        str[8] + ", " + str[9] + ", " + str[10] + ", " + str[11] + "," +
                        str[12] + ", " + str[13] + ", " + str[14] + ")";
                    ExecSQL(sql);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка! " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

        #endregion Заполнение секции  "Начисленные суммы субсидий и льгот по услугам"

        #region Заполнение секции  "Начисленные суммы субсидий и льгот по жильцам"
        private void WriteGilHead(string[] str)
        {
            string sql = "";

            sql =
            " INSERT INTO file_gil_head" +
            " (file_type, num_file_type, org_unit, month_and_year, file_date, unload_num) " +
            " VALUES ('" + str[0].Trim() + "', " + str[1] + ", '" + str[2] + "', '" + str[3] + "', '" + str[4] + "', " + str[5] + ") ";
            ExecSQL(sql);
        }

        private void WriteGilEnd(string[] str)
        {
            string sql = "";

            sql =
            " INSERT INTO file_gil_end" +
            " (file_type, num_file_type, date, reserv) " +
            " VALUES ('" + str[0].Trim() + "', " + str[1] + ", '" + str[2] + "', '" + str[3] + "') ";
            ExecSQL(sql);
        }

        private Returns WriteGilSub(string[] str, FilesImported finder, IDbConnection conn_db)
        {
            Returns ret = new Returns();
            ret = Utils.InitReturns();
            string sql;
            string bank = finder.bank;
            string month = finder.month;
            string year = finder.year;
            
            string sav_pss = "";
            string sav_num_ls = ""; 
            string sav_fam = "";
            string sav_ima = "";
            string sav_otch = "";
            string sav_dat_rog = "";
            string c_dat_rog = "";
            string c_fam;
            string c_ima;
            string c_otch;
            string sTmpStr;
            string sTmpStrD;
            string sTmpStrFS = "";
            string sTmpStrFSd = "";
            int sav_nzp_kvar = -1;
            int sav_nzp_gilec = -1;
            int c_nzp_gilec = 0;
            int c_nzp_kvar = 0;
            bool bIsGroup;
            bool bNeedFind = false;

            try
            {
                //Запись в промежуточную таблицу
                //sql =
                //    " INSERT INTO file_gil_sub" +
                //    " (pss, num_ls, fam, ima, otch, born_date, cod_sum, cod_sum_serv, bank_name, date_start, sum_insaldo," +
                //    " reserv, sum_nach_sub, sum_nach_sub_rev, reserv1, sum_avanc, sum_viplat, sum_sub_smo, sum_sub_smo_rt, " +
                //    " sum_sub_otopl, sum_sub_smo_rev, sum_sub_smo_rt_rev, sum_sub_otopl_rev, sum_change_sub, date_finish) " +
                //    " VALUES (" + str[0] + ", " + str[1] + ", '" + str[2] + "', '" + str[3] + "', '" + str[4] + "', '" + str[5] +
                //    "', " + str[6] + ", " + str[7] + ", '" + str[8] + "', " + CheckOnEmpty(str[9]) + ", " + str[10] + ", '" + str[11] + "', " +
                //    str[12] + ", " + str[13] + ", " + str[14] + ", '" + str[15] + "', " + str[16] + ", " + str[17] + ", " + str[18] + ", '" +
                //    str[19] + "', '" + str[20] + "', " + str[21] + ", '" + str[22] + "', '" + str[23] + "', " + CheckOnEmpty(str[24]) + ") ";
                //ExecSQL(sql);

                // Проверки

                c_fam = str[2].Trim();
                c_ima = str[3].Trim();
                c_otch = str[4].Trim();
                c_dat_rog = str[5].Trim();


                sTmpStr = "ПСС: " + str[0].Trim() + " ЛС: " + str[1].Trim() + " Фамилия: " + c_fam + " Имя: " + c_ima +
                          " Отчество: " + c_otch + " Дата рождения: " + c_dat_rog;

                bIsGroup = false;

                if (sav_pss.Trim() != str[0].Trim() || sav_num_ls.Trim() != str[1].Trim() || sav_nzp_kvar == -1)
                    bNeedFind = true;

                //Ищем ПСС
                if (bNeedFind)
                {
                    sql =
                        " SELECT * " +
                        " FROM " + bank + "_data.prm_15 p, " + bank + "_data.kvar k" +
                        " WHERE k.nzp_kvar = p.nzp " +
                        " AND k.num_ls = " + str[1].Trim() +
                        " AND p.nzp_prm = 162 " +
                        " AND p.is_actual = 1 " +
                        " AND p.val_prm = '" + str[0].Trim() + "'";
                    DataTable dt = ExecSQLToTable(sql, conn_db);

                    if (dt.Rows.Count == 0)
                    {
                        MonitorLog.WriteLog("Не найден ПСС или ЛС! " + sTmpStr, MonitorLog.typelog.Error, true);
                        c_nzp_kvar = 0;
                    }
                    else
                    {
                        foreach (DataRow rr in dt.Rows)
                        {
                            c_nzp_kvar = Convert.ToInt32(rr["nzp_kvar"]);
                        }
                        bIsGroup = true;
                    }

                    sav_nzp_kvar = c_nzp_kvar;
                    sav_pss = str[0].Trim();
                    sav_num_ls = str[1].Trim();

                    if (c_fam == "" && c_otch == "" && c_dat_rog == "")
                    {
                        MonitorLog.WriteLog("Человек не определен! " + sTmpStr, MonitorLog.typelog.Error, true);
                    }
                }
                else
                {
                    c_nzp_kvar = sav_nzp_kvar;
                }

                if (c_nzp_kvar > 0)
                {
                    bNeedFind = false;
                    if (sav_fam.Trim() != c_fam.Trim() || sav_ima.Trim() != c_ima.Trim() ||
                        sav_otch.Trim() != c_otch.Trim() || sav_dat_rog.Trim() != c_dat_rog.Trim() ||
                        sav_nzp_gilec == -1)
                        bNeedFind = true;

                    if (bNeedFind)
                    {
                        if (c_fam == "" && c_ima == "" && c_otch == "")
                        {
                            MonitorLog.WriteLog("Неполные данные о человеке! " + sTmpStr, MonitorLog.typelog.Error, true);
                        }

                        sql =
                            " SELECT nzp_gil " +
                            " FROM " + bank + "_data.kart " +
                            " WHERE UPPER(TRIM(fam)) = '" + c_fam.ToUpper() + "'" +
                            " AND UPPER(TRIM(ima)) = '" + c_ima.ToUpper() + "'" +
                            " AND UPPER(COALESCE(TRIM(otch), '')) = '" + c_otch.ToUpper() + "'" +
                            " AND dat_rog = CAST('" + c_dat_rog + "' as DATE)" +
                            " AND COALESCE(neuch, '0') <> '1' " +
                            " AND nzp_kvar = " + c_nzp_kvar;
                        DataTable dt1 = ExecSQLToTable(sql, conn_db);

                        if (dt1.Rows.Count != 0)
                        {
                            foreach (DataRow rr in dt1.Rows)
                            {
                                c_nzp_gilec = Convert.ToInt32(rr["nzp_gil"]);
                            }
                        }
                        else
                            c_nzp_gilec = 0;

                        if (c_nzp_gilec > 0)
                        {

                        }
                        else
                        {
                            MonitorLog.WriteLog("Предупреждение, не найден человек! " + sTmpStr,
                                MonitorLog.typelog.Error, true);
                        }

                        sav_nzp_gilec = c_nzp_gilec;
                        sav_fam = c_fam.Trim();
                        sav_ima = c_ima.Trim();
                        sav_otch = c_otch.Trim();
                        sav_dat_rog = c_dat_rog.Trim();
                    }
                    else
                    {
                        c_nzp_gilec = sav_nzp_gilec;
                    }

                    //Вставка в группу загруженных льготников
                    if (bIsGroup)
                    {
                        sql =
                            " INSERT INTO " + bank + "_data.link_group (" +
                            " nzp, nzp_group) " +
                            " VALUES (" + c_nzp_kvar + ", 800)";
                        ExecSQL(sql);
                    }


                    //Запись в БД

                    //Удаление старой записи 
                    sql =
                        " DELETE FROM " + bank + "_charge_" + year + ".calc_sz_fin_" + month +
                        " WHERE pss = " + str[0].Trim() +
                        " AND num_ls = " + str[1].Trim() +
                        " AND UPPER(TRIM(fam)) = '" + c_fam.ToUpper() + "'" +
                        " AND UPPER(TRIM(ima)) = '" + c_ima.ToUpper() + "'" +
                        " AND UPPER(COALESCE(TRIM(otch), '')) = '" + c_otch.ToUpper() + "'" +
                        " AND drog = CAST('" + c_dat_rog + "' as DATE)" +
                        " AND nzp_exp = " + str[6] +
                        " AND UPPER(COALESCE(TRIM(bank), '')) = '" + str[8].ToUpper() + "'" +
                        " AND nzp_serv = " + str[7];
                    ExecSQL(sql);

                    //Добавление новой записи
                    sTmpStr = "";
                    sTmpStrD = "";
                    
                    if (str[9].Trim().Length > 0)
                    {
                        sTmpStr = "start_subsidy,";
                        sTmpStrD = "'" + str[9].Trim() + "',";
                    }
                    if (str[24].Trim().Length > 0)
                    {
                        sTmpStrFS = ",finish_subsidy";
                        sTmpStrFSd = ",'" + str[24].Trim() + "'";
                    }

                    sql =
                        " INSERT INTO " + bank + "_charge_" + year + ".calc_sz_fin_" + month +
                        " (pss, num_ls, nzp_gil, " +
                        " fam, ima, otch, drog, " +
                        " nzp_exp, nzp_serv, bank, " + sTmpStr +
                        " sum_insaldo, sum_delta, sum_charge, sum_charge_p, " +
                        " sum_curf, sum_avans, sum_must, sum_smo_rf, " +
                        " sum_smo_rt, sum_smo_otop, sum_smo_rf_p, sum_smo_rt_p, " +
                        " sum_smo_otop_p, sum_pere " + sTmpStrFS + ")" +
                        " VALUES ( " +
                        str[0] + ", " + str[1] + ", " + c_nzp_gilec + ", " +
                        " '" + c_fam + "', '" + c_ima + "', '" + c_otch + "', '" + c_dat_rog + "', " +
                        str[6] + ", " + str[7] + ", '" + str[8] + "', " + sTmpStrD +
                        str[10] + ", " + str[11] + ", " + str[12] + ", " + str[13] + "," +
                        str[14] + ", " + str[15] + ", " + str[16] + ", " + str[17] + "," +
                        str[18] + ", " + str[19] + ", " + str[20] + ", " + str[21] + "," +
                        str[22] + ", " + str[23] + sTmpStrFSd +
                        " )";
                    ExecSQL(sql);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("WriteGilSub: ошибка  " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "WriteGilSub: ошибка";
                ret.result = false;
                return ret;
            }
            return ret;
        }

        #endregion Заполнение секции  "Начисленные суммы субсидий и льгот по жильцам"

        #region Заполнение секции  "Характеристики жилья"
        private void WriteCharGilHead(string[] str)
        {
            string sql = "";

            sql =
            " INSERT INTO file_char_gil_head" +
            " (file_type, num_file_type, org_unit, month_and_year, file_date, unload_num) " +
            " VALUES ('" + str[0].Trim() + "', '" + str[1] + "', '" + str[2] + "', '" + str[3] + "', '" + str[4] + "', " +
            str[5] + ") ";
            ExecSQL(sql);
        }
        private void WriteCharGilEnd(string[] str)
        {
            string sql = "";

            sql =
            " INSERT INTO file_char_gil_end" +
            " (file_type, num_file_type, date, reserv) " +
            " VALUES ('" + str[0].Trim() + "', " + str[1] + ", '" + str[2] + "', '" + str[3] + "') ";
            ExecSQL(sql);
        }
        private void WriteCharGilSub(string[] str, FilesImported finder, IDbConnection conn_db)
        {
            string sql;
            string bank = finder.bank;
            string month = finder.month;
            string year = finder.year;
            string sTmpStr;
            int sMonth;
            int c_nzp_kvar = 0;

            try
            {
                //Запись в промежуточную таблицу
                //sql =
                //    " INSERT INTO file_char_gil_sub" +
                //    " (pss, num_ls, gross_area, cod_mo, cod_paket_blag, date_finish) " +
                //    " VALUES (" + str[0] + ", " + str[1] + ", " + str[2] + ", " + str[3] + ", " + str[4] + ", " + str[5] + ", '" + str[6] + "') ";
                //ExecSQL(sql);

                sTmpStr = "ПСС: " + str[0].Trim() + " ЛС: " + str[1].Trim();

                //Ищем ПСС
                sql =
                    " SELECT * " +
                    " FROM " + bank + "_data.prm_15 p, " + bank + "_data.kvar k " +
                    " WHERE k.nzp_kvar = p.nzp " +
                    " AND k.num_ls = " + str[1].Trim() +
                    " AND p.nzp_prm = 162 " +
                    " AND p.is_actual = 1 " +
                    " AND p.val_prm = '" + str[0].Trim() + "'";
                DataTable dt = ExecSQLToTable(sql, conn_db);

                if (dt.Rows.Count == 0)
                {
                    MonitorLog.WriteLog("Не найден ПСС или ЛС! " + sTmpStr, MonitorLog.typelog.Error, true);
                    c_nzp_kvar = 0;
                }
                else
                {
                    foreach (DataRow rr in dt.Rows)
                    {
                        c_nzp_kvar = Convert.ToInt32(rr["nzp_kvar"]);
                    }
                }

                if (c_nzp_kvar > 0)
                {
                    sMonth = Convert.ToInt32(month);
                    //Удаляем старую запись из БД
                    sql =
                        " DELETE FROM " + bank + "_charge_" + year + ".calc_sz_har " +
                        " WHERE pss = " + str[0].Trim() +
                        " AND num_ls = " + str[1].Trim() +
                        " AND month_ = " + sMonth;
                    ExecSQL(sql);

                    //Запись в БД
                    sTmpStr = "'" + str[6] + "'";
                    if (str[6].Trim() == "")
                        sTmpStr = "null";

                    sql =
                        " INSERT INTO " + bank + "_charge_" + year + ".calc_sz_har " +
                        " (pss, num_ls, month_, " +
                        " cnt_gil, s_ob, nzp_vill, nzp_packet, finish_subsidy) " +
                        " VALUES( " +
                        str[0] + ", " + str[1] + ", " + sMonth + ", " +
                        str[2] + ", " + str[3] + ", " + str[4] + ", " + str[5] + ", " + sTmpStr + ")";
                    ExecSQL(sql);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка! " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

        #endregion Заполнение секции  "Характеристики жилья"
    }
}
