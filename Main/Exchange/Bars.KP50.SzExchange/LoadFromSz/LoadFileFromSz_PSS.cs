using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
//using Castle.Components.DictionaryAdapter;
//using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Класс для взаимодействия с СЗ (загрузка/выгрузка)
    /// </summary>
    public class DbLoadFileFromSZpss : DataBaseHeadServer
    {
        private struct MyStruct
        {
            public int type_string;
            public string type_file;
            public string name_supp;
            public string podr_supp;
            public int number_file;
            public string dat_load_file;
            public string phone_number;
            public string fio;
            public int count_household;
            public long code_mistake;
            public string text_mistake;

        }

        private struct MyStructLoadPss
        {
            public int type_string;
            public long number_personal_account;
            public string nzp_kvar;
            public string number_household;
            public string dat_s_for_ls;
            public string dat_po_for_ls;
            public string fam;
            public string ima;
            public string otch;
            public string date_of_birth;
            public string town;
            public string selo;
            public string ulica;
            public string dom;
            public string korpus;
            public string kvar;
            public string room;
            public string nzp_vill;
            public long code_answer;
            public string text_answer;

        }

        //контейнер параметров
        private FilesImported _finder;

        //StringBuilder для записи текста ошибок
        private StringBuilder err = new StringBuilder();

        //вспомогательная структура для формирования протокола
        private MyStruct _fileHeadStruct;
        //вспомогательная структура для получения nzp_kvar
        private MyStructLoadPss _fileHouseholdStruct;

        /// <summary>
        /// Ф-ция загрузки файла из СЗ
        /// </summary>
        /// <param name="finder">контейнер входящих пеерменных</param>
        /// <param name="ret">переменная для возврата результата работы ф-ции</param>
        /// <returns></returns>
        public Returns LoadFileFromSz_PSS(FilesImported finder, ref Returns ret, IDbConnection conn_db)
        {
            _finder = finder;

            //записываем в "Мои файлы"
            int nzpExc = DbFileLoader.AddMyFile("Загрузка из СЗ", _finder);

            //разархивируем файл
            string _fullFileName = DbFileLoader.DecompressionFile(_finder.saved_name, InputOutput.GetInputDir(), ".txt", ref ret);


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
            ret = Run(DbFileLoader.ReadFile(_fullFileName, 866), finder, conn_db);


            return ret;
        }

        /// <summary>
        /// Ф-ция обработки
        /// </summary>
        /// <param name="fileStrings"></param>
        /// <returns></returns>
        private Returns Run(string[] fileStrings, FilesImported finder, IDbConnection conn_db)
        {
            Returns ret = new Returns();
            ret = Utils.InitReturns();
            string[] liststr;
            string[] CheckString;

            //CheckString = fileStrings[0].Split(new char[] { '|' }, StringSplitOptions.None);

            try
            {
                CreateTableHead();
                CreateTableHousehold();
                CreateTableHouseholdMistake();

                //PreWork(finder.bank, finder, conn_db);

                foreach (string str in fileStrings)
                {
                    //защита от пустых строк(пустые строки для сохранения нумерации)
                    if (str.Trim() == "")
                    {
                        continue;
                    }

                    //массив значений строки
                    liststr = str.Split(new char[] {'|'}, StringSplitOptions.None);
                    // Array.ForEach(liststr.vals, x => x = x.Trim());
                    switch (liststr[0])
                    {
                        case "1":
                            AddHead(ref finder, liststr);
                            PreWork(finder.bank, finder, conn_db);
                            break;
                        case "2":
                            AddHousehold(liststr, finder, conn_db);
                            DisassemblePSS(finder.bank, finder, conn_db);
                            break;
                        case "3":
                            AddMistake_for_household(liststr);
                            DisassemblePSS(finder.bank, finder, conn_db);
                            break;
                    }
                }

                LoadPss_SaveProtocol(finder.bank, finder, conn_db);

            }

            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка загрузки файла ответ-поставщику" + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropHead();
                DropHousehold();
                DropHouseholdMistake();
            }

            return ret;
        }

        protected virtual DataTable ExecSQLToTable(string sql, IDbConnection conn_db)
        {
            return DBManager.ExecSQLToTable(conn_db, sql);
        }
        
        public void AddHead(ref FilesImported finder, string[] liststr)
        {
            try
            {
                string sql;

                sql =
                    " INSERT INTO Head ( " +
                    " type_string, " +
                    " type_file, " +
                    " name_supp, " +
                    " podr_supp, " +
                    " number_file, " +
                    " dat_load_file, " +
                    " phone_number, " +
                    " fio, " +
                    " count_household, " +
                    " code_mistake, " +
                    " text_mistake ) " +
                    " VALUES (" +
                    " '1', 'Ответ поставщику', '" +
                    liststr[2] + "','" + liststr[3] + "','" + liststr[4] + "','" + liststr[5] + "','" + liststr[6] + "','" +
                    liststr[7] + "'," + ConvertValue(liststr[8], EColumnTypes.Int.GetHashCode()) + "," + ConvertValue(liststr[9], EColumnTypes.Int.GetHashCode()) + ",'" + liststr[10] + "')";
                ExecSQL(sql);

                //_fileHeadStruct = new MyStruct();
                
                    _fileHeadStruct.type_string = 1;
                    _fileHeadStruct.type_file = "Ответ поставщику";
                    _fileHeadStruct.name_supp = liststr[2];
                    _fileHeadStruct.podr_supp = liststr[3];
                    _fileHeadStruct.number_file = String.IsNullOrEmpty(liststr[4]) ? 0 : Convert.ToInt32(liststr[4]);
                    _fileHeadStruct.dat_load_file = liststr[5];
                    _fileHeadStruct.phone_number = liststr[6];
                    _fileHeadStruct.fio = liststr[7];
                    _fileHeadStruct.count_household = String.IsNullOrEmpty(liststr[8]) ? 0 : Convert.ToInt32(liststr[8]);
                    _fileHeadStruct.code_mistake = String.IsNullOrEmpty(liststr[9]) ? 0 : Convert.ToInt32(liststr[9]);
                    _fileHeadStruct.text_mistake = liststr[10];




                    finder.bank = _fileHeadStruct.podr_supp.ToLower().Split(new string[] { "_kernel" }, StringSplitOptions.None)[0].ToLower();

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления заголовка" + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                string sql = " DROP TABLE HEAD ";
                ExecSQL(sql);
            }

        }

        public void AddHousehold(string[] liststr, FilesImported finder, IDbConnection conn_db)
        {
            try
            {
                string sql;

                sql =
                    " INSERT INTO Household ( " +
                    " type_string, " +
                    " number_personal_account, " +
                    " number_household, " +
                    " dat_s_for_ls, " +
                    " dat_po_for_ls, " +
                    " fam, " +
                    " ima, " +
                    " otch, " +
                    " date_of_birth, " +
                    " town, " +
                    " selo, " +
                    " ulica, " +
                    " dom, " +
                    " korpus, " +
                    " kvar, " +
                    " room, " +
                    " nzp_vill, " +
                    " code_answer, " +
                    " text_answer) " +
                    " VALUES (" +
                    " '2', " +
                    ConvertValue(liststr[1], EColumnTypes.Int.GetHashCode()) + ",'" + liststr[2] + "'," + ConvertValue(liststr[3], EColumnTypes.Date.GetHashCode()) + "," + ConvertValue(liststr[4], EColumnTypes.Date.GetHashCode()) + ",'" + liststr[5] + "','" + liststr[6] + "','" +
                    liststr[7] + "'," + ConvertValue(liststr[8], EColumnTypes.Date.GetHashCode()) + ",'" + liststr[9] + "','" + liststr[10] + "','" + liststr[11] + "','" + liststr[12] + "','" + liststr[13] + "','" + liststr[14] + "','" +
                    liststr[15] + "','" + liststr[16] + "'," + ConvertValue(liststr[17], EColumnTypes.Int.GetHashCode()) + ",'" + liststr[18] + "')";
                ExecSQL(sql);


                _fileHouseholdStruct.number_personal_account = String.IsNullOrEmpty(liststr[1]) ? 0 : Convert.ToInt64(liststr[1]);
                _fileHouseholdStruct.number_household = Convert.ToString(String.IsNullOrEmpty(liststr[2]) ? 0 : Convert.ToInt32(liststr[2]));
                _fileHouseholdStruct.dat_s_for_ls = liststr[3];
                _fileHouseholdStruct.dat_po_for_ls = liststr[4];
                //..проверить, если такой nzp_kvar, иначе-выход
                _fileHouseholdStruct.nzp_kvar = GetNzpKvar(finder.bank, conn_db);
                _fileHouseholdStruct.code_answer = String.IsNullOrEmpty(liststr[17]) ? 0 : Convert.ToInt64(liststr[17]);

            }
            catch (Exception ex)
            {

                MonitorLog.WriteLog("Ошибка добавления поля Домохозяйство: " + String.Join("#",liststr)+
                    ex.Message + 
                    ex.StackTrace, MonitorLog.typelog.Error, true);
                string sql = " DROP TABLE HOUSEHOLD ";
                ExecSQL(sql);
            }
        }
        public void AddMistake_for_household(string[] liststr)
        {
            try
            {
                string sql;
                
                sql =
                    " INSERT INTO Household ( " +
                    " type_string, " +
                    " code_mistake, " +
                    " text_mistake) " +
                    " VALUES (" +
                    " '3' ," +
                    ConvertValue(liststr[1], EColumnTypes.Int.GetHashCode()) + ",'" + liststr[2] + "')";
                ExecSQL(sql);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления поля Домохозяйство" + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                string sql = " DROP TABLE HOUSEHOLD ";
                ExecSQL(sql);
            }
        }

        private void DropHead()
        {
            string sql;

            sql =
                " DROP TABLE Head ";
            ExecSQL(sql);
        }

        private void DropHousehold()
        {
            string sql;

            sql =
                " DROP TABLE Household ";
            ExecSQL(sql);
        }

        private void DropHouseholdMistake()
        {
            string sql;

            sql =
                " DROP TABLE MISTAKE_FOR_HOUSEHOLD ";
            ExecSQL(sql);
        }

        private void CreateTableHead()
        {
            string sql;
            sql =
                " CREATE TABLE Head( " +
                " type_string INTEGER, " +
                " type_file CHAR (16), " +
                " name_supp CHAR (40), " +
                " podr_supp CHAR (40), " +
                " number_file INTEGER, " +
                " dat_load_file DATE, " +
                " phone_number CHAR (10), " +
                " fio CHAR (80), " +
                " count_household INTEGER, " +
                " code_mistake BIGINT, " +
                " text_mistake CHAR (80) ) ";
            ExecSQL(sql);
        }

        private string GetNzpKvar(string pref, IDbConnection conn_db)
        {
            string sql;

            sql =
                " SELECT nzp_kvar FROM selected_kvar " +
                " WHERE num_ls = " +
                Convert.ToString(String.IsNullOrEmpty(_fileHouseholdStruct.number_household)
                    ? 0
                    : Convert.ToInt32(_fileHouseholdStruct.number_household));
            DataTable dt1 = ExecSQLToTable(sql, conn_db);
            sql =
                " SELECT nzp_kvar FROM " + pref + DBManager.sDataAliasRest + "kvar k," + pref + DBManager.sDataAliasRest +
                "dom d, " +
                pref + DBManager.sDataAliasRest + "s_ulica s, " + pref + DBManager.sDataAliasRest + "s_rajon r, " +
                pref + DBManager.sDataAliasRest + "s_town t " +
                " WHERE k.nzp_dom=d.nzp_dom AND d.nzp_ul=s.nzp_ul AND " +
                " s.nzp_raj=r.nzp_raj AND r.nzp_town=t.nzp_town " +
                " AND num_ls = " +
                Convert.ToString(String.IsNullOrEmpty(_fileHouseholdStruct.number_household)
                    ? 0
                    : Convert.ToInt32(_fileHouseholdStruct.number_household));
            //DataTable dt1 = ExecSQLToTable(sql, conn_db);

            if (dt1.Rows.Count != 0)
            {
                return Convert.ToString(dt1.Rows[0]["nzp_kvar"]);
            }
            else
            {
                return Constants._ZERO_.ToString();
            }
        }


        private void CreateTableHousehold()
        {
            string sql;

            sql =
                " CREATE TABLE Household( " +
                " type_string INTEGER, " +
                " number_personal_account BIGINT, " +
                " number_household CHAR (20), " +
                " dat_s_for_ls DATE, " +
                " dat_po_for_ls DATE, " +
                " fam CHAR (20), " +
                " ima CHAR (20), " +
                " otch CHAR (20), " +
                " date_of_birth DATE, " +
                " town CHAR (30), " +
                " selo CHAR (30), " +
                " ulica CHAR (40), " +
                " dom CHAR (10), " +
                " korpus CHAR (3), " +
                " kvar CHAR (10), " +
                " room CHAR (3) , " +
                " nzp_vill CHAR (30), " +
                " code_answer BIGINT, " +
                " text_answer CHAR (80) ) ";
            ExecSQL(sql);

        }

        private void CreateTableHouseholdMistake()
        {
            string sql;

            sql =
                " CREATE TABLE MISTAKE_FOR_HOUSEHOLD( " +
                " type_string INTEGER, " +
                " code_mistake BIGINT, " +
                " text_mistake CHAR (80) ) ";
            ExecSQL(sql);
        }



        public void PreWork(string pref, FilesImported finder, IDbConnection conn_db)
        {
            string sql;
            try
            {
                sql =
                    " SELECT num_ls,nzp_kvar,Upper(ndom) as ndom, " +
                    " Upper(nkor) as nkor,Upper(nkvar) as nkvar,Upper(nkvar_n) as nkvar_n, " +
                    " Upper(ulica) as ulica,Upper(rajon) as rajon,Upper(town) as town  " +
                    " INTO TEMP selected_kvar " +
                    " FROM " + pref + DBManager.sDataAliasRest + "kvar k," + pref + DBManager.sDataAliasRest + "dom d, " +
                    pref + DBManager.sDataAliasRest + "s_ulica s, " + pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    pref + DBManager.sDataAliasRest + "s_town t " +
                    " WHERE k.nzp_dom=d.nzp_dom AND d.nzp_ul=s.nzp_ul AND " +
                    " s.nzp_raj=r.nzp_raj AND r.nzp_town=t.nzp_town ";
                ExecSQL(conn_db, sql);

                sql =
                    " CREATE INDEX selected_kvar_num_ls_idx ON selected_kvar(num_ls)";
                ExecSQL(conn_db, sql);

                sql =
                    " CREATE INDEX selected_kvar_nzp_kvar_idx ON selected_kvar(nzp_kvar)";
                ExecSQL(conn_db, sql);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки адресного пространства" + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                sql =
                    " DROP TABLE selected_kvar";
                ExecSQL(sql);
            }
        }
        /// <summary>
        /// Сохранение информации о файле загрузки
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        public void LoadPss_SaveProtocol(string pref, FilesImported finder, IDbConnection conn_db)
        {
            int nzp_prot;
            string nzp_wp;
            string sql;
            try
            {
                if (Points.Pref + sDataAliasRest == "nko_data." || Points.Pref + sDataAliasRest == "nch_data." ||
                    Points.Pref + sDataAliasRest == "kama_data.")
                {
                    sql =
                        " SELECT nzp_wp FROM s_point " +
                        " WHERE Upper(bd_kernel) = '" + pref.Trim() + "'";
                    DataTable dt = ExecSQLToTable(sql, conn_db);

                    if (dt.Rows.Count != 1)
                    {
                        nzp_wp = "";
                    }
                    else
                    {
                        nzp_wp = Convert.ToString(dt.Rows[0]["nzp_wp"]);

                        sql =
                            " INSERT INTO " + Points.Pref + sDataAliasRest + "in_prot (" +
                            " is_loaded, nzp_reg, id_file, file_name, nzp_user, dat_save, nzp_wp) " +
                            " VALUES ( " +
                            " 0, -4, '" + _fileHeadStruct.number_file + "****" + DateTime.Now + "','" +
                            finder.loaded_name + "'," +
                            finder.nzp_user + ", '" + Convert.ToString(DateTime.Now).Substring(0, 10) + "'," + nzp_wp +
                            " ) ";
                        ExecSQL(sql);
                    }

                }
                else
                {
                    sql =
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "in_prot (" +
                        " is_loaded, nzp_reg, id_file, file_name, nzp_user, dat_save) " +
                        " VALUES ( " +
                        " 0, -4, '" + _fileHeadStruct.number_file + "****" + DateTime.Now + "', '" +
                        finder.loaded_name + "'," +
                        finder.nzp_user + ", '" + Convert.ToString(DateTime.Now).Substring(0, 10) + "' ) ";
                    ExecSQL(sql);

                    nzp_prot = DBManager.GetSerialValue(conn_db);

                    //Номер файла
                    sql =
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "in_prot_prm ( " +
                        " nzp_prot, nzp_param, val_param) " +
                        " values ( " +
                        Convert.ToString(nzp_prot) + ", 1,'" + _fileHeadStruct.number_file + "')";
                    ExecSQL(sql);

                    //Дата начала выгрузки
                    sql =
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "in_prot_prm ( " +
                        " nzp_prot, nzp_param, val_param) " +
                        " values ( " +
                        Convert.ToString(nzp_prot) + ", 2,'" + _fileHeadStruct.dat_load_file + "')";
                    ExecSQL(sql);

                    //Дата окончания выргузки
                    sql =
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "in_prot_prm ( " +
                        " nzp_prot, nzp_param, val_param) " +
                        " values ( " +
                        Convert.ToString(nzp_prot) + ", 3,'" + DateTime.Now + "')";
                    ExecSQL(sql);

                    //Наименование отправителя
                    sql =
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "in_prot_prm ( " +
                        " nzp_prot, nzp_param, val_param) " +
                        " values ( " +
                        Convert.ToString(nzp_prot) + ", 6,'" + _fileHeadStruct.name_supp + _fileHeadStruct.podr_supp +
                        "')";
                    ExecSQL(sql);

                    //Телефон отправителя
                    sql =
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "in_prot_prm ( " +
                        " nzp_prot, nzp_param, val_param) " +
                        " values ( " +
                        Convert.ToString(nzp_prot) + ", 7,'" + _fileHeadStruct.phone_number + "')";
                    ExecSQL(sql);

                    //ФИО отправителя
                    sql =
                        " INSERT INTO " + Points.Pref + sDataAliasRest + "in_prot_prm ( " +
                        " nzp_prot, nzp_param, val_param) " +
                        " values ( " +
                        Convert.ToString(nzp_prot) + ", 8,'" + _fileHeadStruct.fio + "')";
                    ExecSQL(sql);


                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при добавлении информации о загрузке файла" + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }


        }

        public void InsertPss(string pref, FilesImported finder, IDbConnection conn_db)
        {
            string sql;
            //int val_prm = 0;

            try
            {
                sql =
                    " SELECT val_prm, dat_s, dat_po FROM " + pref + DBManager.sDataAliasRest + " prm_15 " +
                    " WHERE is_actual <> 100 " +
                    " AND nzp_prm = 162 " +
                    " AND nzp = " + Convert.ToString(String.IsNullOrEmpty(_fileHouseholdStruct.nzp_kvar) ? 0 : Convert.ToInt32(_fileHouseholdStruct.nzp_kvar));
                DataTable dt3 = ExecSQLToTable(sql, conn_db);

                if (dt3.Rows.Count == 0)
                {
                    sql =
                        " INSERT INTO " + pref + sDataAliasRest + " prm_15 ( " +
                        " nzp_prm, nzp, val_prm, is_actual, dat_s, dat_po, nzp_user, dat_when ) " +
                        " VALUES ( " +
                        " 162, " + Convert.ToString(String.IsNullOrEmpty(_fileHouseholdStruct.nzp_kvar) ? 0 : Convert.ToInt32(_fileHouseholdStruct.nzp_kvar)) + ",'" + _fileHouseholdStruct.number_personal_account +
                        "', 1, " +
                        ConvertValue(_fileHouseholdStruct.dat_s_for_ls, EColumnTypes.Date.GetHashCode()) + ", " + ConvertValue(_fileHouseholdStruct.dat_po_for_ls, EColumnTypes.Date.GetHashCode()) + "," +
                        Convert.ToString(finder.nzp_user) + ",'" +
                        Convert.ToString(DateTime.Now).Substring(0, 10) + "' )";
                    ExecSQL(sql);
                }
                else
                {
                    if (Convert.ToString(dt3.Rows[0]["val_prm"]).Trim() == Convert.ToString(_fileHouseholdStruct.number_personal_account).Trim()) 
                    {
                        if (Convert.ToString(dt3.Rows[0]["dat_s"]).Trim() != Convert.ToString(_fileHouseholdStruct.dat_s_for_ls).Trim() ||
                            Convert.ToString(dt3.Rows[0]["dat_po"]).Trim() != Convert.ToString(_fileHouseholdStruct.dat_po_for_ls).Trim())
                        {
                            sql =
                                " UPDATE " + pref + DBManager.sDataAliasRest + " prm_15 " +
                                " SET (dat_s, dat_po) = ('" +
                                _fileHouseholdStruct.dat_s_for_ls + " ', ' " +
                                _fileHouseholdStruct.dat_po_for_ls + " ' ) " +
                                " WHERE nzp_prm = 162 " +
                                " AND is_actual <> 100 " +
                                " AND nzp = " + _fileHouseholdStruct.nzp_kvar;
                            ExecSQL(sql);
                        }
                        else
                        {
                            MonitorLog.WriteLog("Другой ПСС по лицевому счету", MonitorLog.typelog.Error, true);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
              MonitorLog.WriteLog("Ошибка добавления ПСС в базу данных" + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
        }

        public bool CheckPss (IDbConnection conn_db)
        {
           long res;
           string pss = Convert.ToString(_fileHouseholdStruct.number_personal_account).Trim();
            string sql;
            if (!Int64.TryParse(pss, out res) || pss.Length > 20)
            {
                MonitorLog.WriteLog("Ошибка в написании ПСС", MonitorLog.typelog.Error, true);
            }

            try
            {
                if (_fileHouseholdStruct.date_of_birth != "")
                {
                    Convert.ToDateTime(_fileHouseholdStruct.date_of_birth);
                    Convert.ToInt64(_fileHouseholdStruct.code_answer);
                }

                if (Convert.ToString(_fileHouseholdStruct.dat_s_for_ls).Trim() == "")
                {
                    _fileHouseholdStruct.dat_s_for_ls = "01.01.1900";
                }
                else
                {
                    Convert.ToDateTime(_fileHouseholdStruct.dat_s_for_ls);
                }

                if (Convert.ToString(_fileHouseholdStruct.dat_po_for_ls).Trim() == "")
                {
                    _fileHouseholdStruct.dat_po_for_ls = "01.01.3000";
                }
                else
                {
                    Convert.ToDateTime(_fileHouseholdStruct.dat_po_for_ls);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в написании дат" + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                return false;
            }

            sql =
                " SELECT * FROM selected_kvar " +
                " WHERE num_ls = " + _fileHouseholdStruct.number_household;
            DataTable dt4 = ExecSQLToTable(sql, conn_db);

            if (dt4.Rows.Count == 0)
            {
                MonitorLog.WriteLog("Нет такого лицевого счета с таким адресом " + _fileHouseholdStruct.number_household, MonitorLog.typelog.Error, true);
                return true;
            }

            return true; //CheckSzDom
        }



        public void DisassemblePSS(string pref, FilesImported finder, IDbConnection conn_db)
        {
            //if (_fileHouseholdStruct.code_answer != 0)
            //{
            //    is_del_pss(finder.bank, finder, conn_db);
            //}
            //else
            //{
            if (CheckPss(conn_db))
            {
                //InsertPss(finder.bank, finder, conn_db);
                InsertPssRso(finder.bank, finder, conn_db);
            }
            //}
        }

        public void InsertPssRso( string pref,FilesImported finder, IDbConnection conn_db)
        {
            string sql;
            string kod_tosz= "";
            string pss_in_bd;

            try
            {
                sql =
                    " SELECT * FROM " + pref + DBManager.sDataAliasRest + " prm_15 " +
                    " WHERE nzp_prm = 162 " +
                    " AND is_actual <> 100 " +
                    " AND nzp = " + _fileHouseholdStruct.nzp_kvar;
                DataTable dt1 = ExecSQLToTable(sql, conn_db);

                // нет ПСС в БД
                if (dt1.Rows.Count == 0)
                {
                    //вставим, если присвоение
                    if (_fileHouseholdStruct.code_answer == 0)
                    {
                        sql =
                            " INSERT INTO " + pref + DBManager.sDataAliasRest + " prm_15 ( " +
                            " nzp_prm, nzp, val_prm, is_actual, dat_s, dat_po, nzp_user, dat_when ) " +
                            " VALUES ( " +
                            " 162, " + _fileHouseholdStruct.nzp_kvar + " , '" +
                            _fileHouseholdStruct.number_personal_account + "' , " +
                            " 1, '" + _fileHouseholdStruct.dat_s_for_ls + "' , '" + _fileHouseholdStruct.dat_po_for_ls +
                            "' ," + Convert.ToString(finder.nzp_user) + ", '" +
                            Convert.ToString(DateTime.Now).Substring(0, 10) + " ')";
                        ExecSQL(sql);
                    }
                }

                //есть ПСС в БД

                else
                {
                    if (Convert.ToString(_fileHouseholdStruct.number_personal_account).Trim().Length > 10 &
                        _fileHouseholdStruct.code_answer == 0)
                    {
                        kod_tosz = Convert.ToString(_fileHouseholdStruct.number_personal_account)
                            .Substring(0,
                                Convert.ToInt32(
                                    Convert.ToString(_fileHouseholdStruct.number_personal_account).Trim().Length - 10));
                    }

                    pss_in_bd = Convert.ToString(dt1.Rows[0]["val_prm"]);

                    if (pss_in_bd.Trim().Substring(0, Convert.ToInt32(
                                    Convert.ToString(pss_in_bd).Trim().Length - 10)) == kod_tosz) // ПСС в БД от той же ТОСЗ
                    {
                        if (_fileHouseholdStruct.code_answer == 0)
                        {
                            sql =
                                " UPDATE " + pref + DBManager.sDataAliasRest + " prm_15 SET " +
                                " val_prm = '" + _fileHouseholdStruct.number_personal_account + "'," +
                                " dat_s = '" + _fileHouseholdStruct.dat_s_for_ls + "'," +
                                " dat_po = '" + _fileHouseholdStruct.dat_po_for_ls + "'" +
                                " WHERE nzp_prm = 162 " +
                                " AND is_actual <> 100 " +
                                " AND nzp = " + _fileHouseholdStruct.nzp_kvar;
                            ExecSQL(sql);
                        }
                        else
                        {
                            sql =
                                " DELETE FROM " + pref + DBManager.sDataAliasRest + " prm_15 " +
                                " WHERE nzp_prm = 162 " +
                                " AND nzp = " + _fileHouseholdStruct.nzp_kvar;
                            ExecSQL(sql);
                        }
                    }
                    else
                    {
                        if (_fileHouseholdStruct.code_answer == 0)
                        {
                            MonitorLog.WriteLog("Другой ПСС от другого отдела СЗ по данному лицевому счету", MonitorLog.typelog.Error, true);
                        }
                    }
                }

                

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при разборе ПСС Осетия" + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
        }

        public bool is_del_pss(string pref, FilesImported finder, IDbConnection conn_db)
        {
            string sql;

            sql =
                " SELECT * FROM " + pref + DBManager.sDataAliasRest + " prm_15 " +
                " WHERE is_actual <> 100 " +
                " AND nzp = " + _fileHouseholdStruct.nzp_kvar;
            DataTable dt2 = ExecSQLToTable(sql, conn_db);

            return dt2.Rows.Count == 0;
        }

        private Returns Check()
        {
            return new Returns(true, "Результат проверки");
        }
        private Returns CheckInputPrms()
        {
            return new Returns(true, "Результат проверки");
        }

        public static string ConvertValue(string liststr, int type)
        {

            switch (type)
            {
                case 2:
                case 3:
                case 4:
                    {
                        if (String.IsNullOrEmpty(liststr))
                        {
                            return "null";
                        }
                    }
                    break;
            }
            return "'" + liststr.Trim() + "'";
        }

        private enum EColumnTypes
        {
            Char = 1,
            Int = 2,
            Decimal = 3,
            Date = 4
        }
    }
}
