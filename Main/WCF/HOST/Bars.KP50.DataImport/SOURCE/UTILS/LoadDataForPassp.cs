

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.DataBase
{
    public class DBLoadDataForPassportistka : DbAdminClient
    {
        private decimal nzp_load;
        /// <summary>
        /// Загрузка Паспортистки
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns Run(FilesImported finder)
        {

            Returns ret = Utils.InitReturns();
            try
            {
                //директория файла
                string fDirectory = Constants.Directories.ImportDir.Replace("/", "\\");
                // fDirectory = "D:\\work.php\\KOMPLAT.50\\WEB\\WebKomplat5\\ExcelReport\\import\\";

                //имя файла
                string fileName = Path.Combine(fDirectory, finder.saved_name);

                if (InputOutput.useFtp) InputOutput.DownloadFile(finder.saved_name, fileName);

                //версия файла
                //int nzp_version = -1;
                // FileInfo[] files = new FileInfo[1];

                #region Разархивация файла

                string exDirectory = Path.Combine(Directory.GetDirectoryRoot(finder.saved_name),
                    Path.GetFileNameWithoutExtension(finder.saved_name));
                string[] archFiles = Archive.GetInstance(fileName).Decompress(
                    fileName, exDirectory);
                if (archFiles.Count(file => Path.GetExtension(file) == ".mdb") == 0)
                    return new Returns(false, "Архив пустой", -1);
                List<FileInfo> lst = new List<FileInfo>();
                archFiles.ForEach(file => lst.Add(new FileInfo(file)));
                var files = lst.ToArray();
                /*
                using (SevenZipExtractor extractor = new SevenZipExtractor(fileName))
                {
                    //создание папки с тем же именем
                    DirectoryInfo exDirectorey =
                        Directory.CreateDirectory(Path.Combine(fDirectory,
                            finder.saved_name.Substring(0, finder.saved_name.LastIndexOf('.'))));
                    extractor.ExtractArchive(exDirectorey.FullName);
                    //change
                    files = exDirectorey.GetFiles("*.mdb");
                    //files = exDirectorey.GetFiles("*.dbf");
                    if (files.Length == 0)
                    {
                        ret.result = false;
                        ret.text = "Архив пустой";
                        ret.tag = -1;
                        return ret;
                    }
                }
                  */

                #endregion

                //List<string> sqlStr = new List<string>();
                //StringBuilder err = new StringBuilder();
                //string commStr = "";

                IDbConnection con_db = GetConnection(Constants.cons_Kernel);
                
                ret = OpenDb(con_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует  подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                string sql =
                    " INSERT INTO "+Points.Pref+"_data"+tableDelimiter+"parus_load_status (nzp_load) " +
#if PG
 " VALUES (DEFAULT)";
#else
" VALUES (0)";
#endif
                nzp_load = ClassDBUtils.ExecSQL(sql, con_db, true).GetID();

                if (nzp_load == 0)
                {
                    MonitorLog.WriteLog("Ошибка! nzp_load == 0. ", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }


                DataTable dataFromFile = ReadFileIntoDataTable(files, ref fileName);

                //создание временных таблиц для вставки данных из dataFromFile
                //CreateTempTables(con_db);

                //запись данных из  dataFromFile во временные таблицы
                ReadIntoTempTable(dataFromFile, con_db, fileName);

                //cопоставление с данными из нашей базы (по адресу, по ФИО) 
                //CompareData(con_db);
                ret.text = "Ошибка при cопоставлении с данными из нашей базы.";

                //проверка полей во временных таблицах(в соответствии с форматом)
                //CheckFields(con_db);
                //ret.text = "Ошибка при проверке типов из файла.";


                //Обработка пустых полей во временных таблицах
                //ret = FillZeroFields(con_db);
                ret.text = "Ошибка при обработке пустых полей во временных таблицах.";

                //добавление недостающих колонок и заполнение их данными
                //используется в случае недостатка полей в соответсвии с форматом
                //ret = AddMissingColumns (con_db);
                //    ret.text = "Ошибка при добавлении недостающих колонок и заполнении их данными.";

                

                //добавление новых жильцов (которые не сопоставлялись) 
                //ret = AddGilec(con_db);
                ret.text = "Ошибка при добавлении новых жильцов.";

            }

            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка выполнения процедуры DBLoadDataForPassportistka : " + ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            ret.result = true;
            ret.text = "Файл успешно загружен.";
            ret.tag = -1;

            return ret;
        }

        private static DataTable ReadFileIntoDataTable(FileInfo[] files, ref string fileName)
        {

            //Returns ret;
            DataTable dataFromFile = new DataTable();

            #region Считываем файл в dataFromFile

            fileName = files[0].FullName;

            var mdbBase = System.IO.Path.GetFileNameWithoutExtension(fileName);

            if (System.IO.File.Exists(fileName) == false)
            {
                throw new Exception("Файл отсутствует по указанному пути");
            }

            try
            {
                OleDbConnection oleDbConnection = new OleDbConnection();
                var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                         "Data Source=" + fileName + ";Jet OLEDB:Database Password=password;";
                oleDbConnection.ConnectionString = myConnectionString;
                oleDbConnection.Open();

                OleDbCommand oleDbCommand = new OleDbCommand();
                oleDbCommand.CommandText = "select * from [" + mdbBase + "]";
                oleDbCommand.Connection = oleDbConnection;

                // Адаптер данных
                OleDbDataAdapter dataAdapter = new OleDbDataAdapter();
                dataAdapter.SelectCommand = oleDbCommand;
                // Заполняем объект данными
                dataAdapter.Fill(dataFromFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Не удалось считать файл " + fileName + Environment.NewLine + ex.Message);
            }

            return dataFromFile;

            #endregion
        }

        private void ReadIntoTempTable(DataTable dataFromFile, IDbConnection con_db, string fileName)
        {
            try
            {
                SetLoadStat(con_db, "ReadIntoTempTable", "Старт");


                if (System.IO.Path.GetFileNameWithoutExtension(fileName) == "person_dbf")
                {
                    SetLoadStat(con_db, "ReadPerson", "Старт");
                    ReadPerson(dataFromFile, con_db);

                    SetLoadStat(con_db, "ReadIntoTempTable", "Успешно завершено");
                    return;
                }

                if (System.IO.Path.GetFileNameWithoutExtension(fileName) == "pvsarriv_dbf")
                {
                    SetLoadStat(con_db, "ReadPvsArriv", "Старт");
                    ReadPvsArriv(dataFromFile, con_db);

                    SetLoadStat(con_db, "ReadIntoTempTable", "Успешно завершено");
                    return;
                }

                if (System.IO.Path.GetFileNameWithoutExtension(fileName) == "pvsdepar_dbf")
                {
                    SetLoadStat(con_db, "ReadPvsDepar", "Старт");
                    ReadPvsDepar(dataFromFile, con_db);

                    SetLoadStat(con_db, "ReadIntoTempTable", "Успешно завершено");
                    return;
                }

                if (System.IO.Path.GetFileNameWithoutExtension(fileName) == "faceacce_dbf")
                {
                    SetLoadStat(con_db, "ReadFaceAcce", "Старт");
                    ReadFaceAcce(dataFromFile, con_db);

                    SetLoadStat(con_db, "ReadIntoTempTable", "Успешно завершено");
                    return;
                }

                //загрузка адресного пространства 
                if (System.IO.Path.GetFileNameWithoutExtension(fileName) == "addrdict")
                {
                    SetLoadStat(con_db, "ReadAddrDict", "Старт");
                    ReadAddrDict(dataFromFile, con_db);

                    SetLoadStat(con_db, "ReadIntoTempTable", "Успешно завершено");
                    return;
                }
                if (System.IO.Path.GetFileNameWithoutExtension(fileName) == "addrtype")
                {
                    SetLoadStat(con_db, "ReadAddrType", "Старт");
                    ReadAddrType(dataFromFile, con_db);

                    SetLoadStat(con_db, "ReadIntoTempTable", "Успешно завершено");
                    return;
                }
                if (System.IO.Path.GetFileNameWithoutExtension(fileName) == "country")
                {
                    SetLoadStat(con_db, "ReadCountry", "Старт");
                    ReadCountry(dataFromFile, con_db);

                    SetLoadStat(con_db, "ReadIntoTempTable", "Успешно завершено");
                    return;
                }
                if (System.IO.Path.GetFileNameWithoutExtension(fileName) == "region")
                {
                    SetLoadStat(con_db, "ReadRegion", "Старт");
                    ReadRegion(dataFromFile, con_db);

                    SetLoadStat(con_db, "ReadIntoTempTable", "Успешно завершено");
                    return;
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка в ф-ции ReadIntoTempTable." + Environment.NewLine + ex.Message);
            }
        }

        private void ReadPerson(DataTable dataFromFile, IDbConnection con_db)
        {
            string sql = "";
            string nzp_dok = "";

            
            foreach (DataRow row in dataFromFile.Rows)
            {
                if ((row["PASSPORT_R"] != DBNull.Value) && (row["PASSPORT_R"].ToString().Trim() != ""))
                    nzp_dok = row["PASSPORT_R"].ToString().Trim();
                else nzp_dok = " null ";
                if (!String.IsNullOrEmpty(nzp_dok))
                {
                    nzp_dok = CompareNzp_dok(nzp_dok);
                }

                //получаем nzp_gil
                sql = "INSERT INTO "+Points.Pref+"_data"+tableDelimiter+"gilec (nzp_gil) " +
#if PG
 " VALUES (DEFAULT)";
#else
                      " VALUES (0)";
#endif

                decimal nzp_gil = ClassDBUtils.ExecSQL(sql, con_db, true).GetID();

                sql =
                    " INSERT INTO "+Points.Pref+"_data"+tableDelimiter+"parus_kart " +
                    "( " +
                    " nzp_gil, " +
                    " orbase_rn, " +
                    " isactual, " +
                    " fam, ima, otch, " +
                    " dat_rog,"+
                //"dat_smert, " +
                    //" fam_c ," +
                    //" ima_c ," +
                    //" otch_c ," +
                    //" dat_rog_c ," +
                    //" dat_fio_c ," +
                    " gender , " +
                    " nzp_user , " +
                    " nation_rn, "+
                    " nzp_dok ," +
                    " serij, nomer, " +
                    " vid_mes, " +
                    " vid_dat , " +
                    " kod_podrazd, " +
                    " BH_COUNTRY, BH_REGION_, BH_REGION, BH_CITY, " +
                    " cur_unl " +
                    ") " +
                    " VALUES ( " +
                    nzp_gil+", ";
                if ((row["ORBASE_RN"] != DBNull.Value) && (row["ORBASE_RN"].ToString().Trim() != ""))
                    sql += "'" + row["ORBASE_RN"].ToString().Trim() + "', ";
                else sql += " null , ";

                sql += " 1, "; //isactual

                if ((row["SURNAME"] != DBNull.Value) && (row["SURNAME"].ToString().Trim() != ""))
                    sql += "'" + row["SURNAME"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["FIRSTNAME"] != DBNull.Value) && (row["FIRSTNAME"].ToString().Trim() != ""))
                    sql += "'" + row["FIRSTNAME"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["SECONDNAME"] != DBNull.Value) && (row["SECONDNAME"].ToString().Trim() != ""))
                    sql += "'" + row["SECONDNAME"].ToString().Trim() + "', ";
                else sql += " null, ";

                if (row["BIRTHDAY"] != DBNull.Value)
                    sql += "'" + Convert.ToDateTime(row["BIRTHDAY"]).ToShortDateString().Trim() + "', ";
                else sql += " null, ";
                //if (row["DEATHDATE"] != DBNull.Value)
                //    sql += "'" + Convert.ToDateTime(row["DEATHDATE"]).ToShortDateString().Trim() + "', ";
                //else sql += " null, ";

                if ((row["SEX"] != DBNull.Value) && (row["SEX"].ToString().Trim() != ""))
                    sql += "'" + row["SEX"].ToString().Trim() + "', ";
                else sql += " null, ";

                sql += "1, ";   //nzp_user
                if ((row["NATION_RN"] != DBNull.Value) && (row["NATION_RN"].ToString().Trim() != ""))
                    sql += "'" + row["NATION_RN"].ToString().Trim() + "', ";
                else sql += " null, ";

                sql += nzp_dok + ", ";

                if ((row["DOCSERIAL"] != DBNull.Value) && (row["DOCSERIAL"].ToString().Trim() != ""))
                    sql += "'" + row["DOCSERIAL"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["DOCNUMBER"] != DBNull.Value) && (row["DOCNUMBER"].ToString().Trim() != ""))
                    sql += "'" + row["DOCNUMBER"].ToString().Trim() + "', ";
                else sql += " null, ";

                sql += " ";
                if ((row["DOCDEALER"] != DBNull.Value) && (row["DOCDEALER"].ToString().Trim() != ""))
                    sql += "'" + row["DOCDEALER"].ToString().Trim() + "', ";
                else sql += " null, ";

                if (row["DOCDATE"] != DBNull.Value)
                    sql += "'" + Convert.ToDateTime(row["DOCDATE"]).ToShortDateString().Trim() + "', ";
                else sql += " null,  ";
                if ((row["DOCSUBCODE"] != DBNull.Value) && (row["DOCSUBCODE"].ToString().Trim() != ""))
                    sql += "'" + row["DOCSUBCODE"].ToString().Trim() + "', ";
                else sql += " null, ";


                if ((row["BH_COUNTRY"] != DBNull.Value) && (row["BH_COUNTRY"].ToString().Trim() != ""))
                    sql += "'" + row["BH_COUNTRY"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["BH_REGION_"] != DBNull.Value) && (row["BH_REGION_"].ToString().Trim() != ""))
                    sql += "'" + row["BH_REGION_"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["BH_REGION"] != DBNull.Value) && (row["BH_REGION"].ToString().Trim() != ""))
                    sql += "'" + row["BH_REGION"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["BH_CITY"] != DBNull.Value) && (row["BH_CITY"].ToString().Trim() != ""))
                    sql += "'" + row["BH_CITY"].ToString().Trim() + "', ";
                else sql += " null, ";

                sql += "2) "; //номер выгрузки
                
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
                
            }

            SetLoadStat(con_db, "ReadPerson", "Успешно завершено");
        }

        private void ReadPvsArriv(DataTable dataFromFile, IDbConnection con_db)
        {
            string sql = "";
            foreach (DataRow row in dataFromFile.Rows)
            {
                //tprp - Тип регистрации (П-постоянная В-временная)
                //dat_prop - Дата первичной регистрации
                //dat_oprp - Дата окончания временной регистрации

                sql =
                    //"UPDATE "+ Points.Pref + "_data" + tableDelimiter +"kart_from_parus " +
                    "UPDATE "+Points.Pref+"_data"+tableDelimiter+"parus_kart " +
                    " SET " +
                    "( " +
                    " faceacc_rn, tprp, dat_prop, dat_oprp, nzp_tkrt " +
                    //откуда прибыл
                    //" FRCOUNTRY_, FRREGION_R, FRAREA_RN," +
                    //" FRCITY_RN, FRTOWN_RN, FRSTREET_R, " +
                    //" FRHOUSE, FRBUILDING, FRFLAT, " +
                    ////куда прибыл
                    //" TOCOUNTRY_, TOREGION_R, TOAREA_RN," +
                    //" TOCITY_RN, TOTOWN_RN, TOSTREET_R, " +
                    //" TOHOUSE, TOBUILDING, TOFLAT " +
                    " " +
                    ") = " +
                    " ( ";

                if ((row["FACEACC_RN"] != DBNull.Value) && (row["FACEACC_RN"].ToString().Trim() != ""))
                    sql += "'" + row["FACEACC_RN"].ToString().Trim() + "', ";
                else sql += " null, ";

                if ((row["REG_TYPE"] != DBNull.Value) && (row["REG_TYPE"].ToString().Trim() != ""))
                {
                    if (row["REG_TYPE"].ToString().Trim() == "2")
                    {
                        sql += "'П', "; //постоянная
                    }
                    else
                    {
                        sql += "'В', "; //временная
                    }
                }
                else sql += " null, ";


                if (row["REG_DATE"] != DBNull.Value)
                    sql += "'" + Convert.ToDateTime(row["REG_DATE"]).ToShortDateString().Trim() + "', ";
                else sql += " null, ";

                if (row["REG_DATE_T"] != DBNull.Value)
                    sql += "'" + Convert.ToDateTime(row["REG_DATE_T"]).ToShortDateString().Trim() + "', ";
                else sql += " null, ";

                //тип карточки - Прибытие
                sql += "1) ";

                ////откуда прибыл
                //if ((row["FRCOUNTRY_"] != DBNull.Value) && (row["FRCOUNTRY_"].ToString().Trim() != ""))
                //    sql += "'" + row["FRCOUNTRY_"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["FRREGION_R"] != DBNull.Value) && (row["FRREGION_R"].ToString().Trim() != ""))
                //    sql += "'" + row["FRREGION_R"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["FRAREA_RN"] != DBNull.Value) && (row["FRAREA_RN"].ToString().Trim() != ""))
                //    sql += "'" + row["FRAREA_RN"].ToString().Trim() + "', ";
                //else sql += " null, ";

                //if ((row["FRCITY_RN"] != DBNull.Value) && (row["FRCITY_RN"].ToString().Trim() != ""))
                //    sql += "'" + row["FRCITY_RN"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["FRTOWN_RN"] != DBNull.Value) && (row["FRTOWN_RN"].ToString().Trim() != ""))
                //    sql += "'" + row["FRTOWN_RN"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["FRSTREET_R"] != DBNull.Value) && (row["FRSTREET_R"].ToString().Trim() != ""))
                //    sql += "'" + row["FRSTREET_R"].ToString().Trim() + "', ";
                //else sql += " null, ";

                //if ((row["FRHOUSE"] != DBNull.Value) && (row["FRHOUSE"].ToString().Trim() != ""))
                //    sql += "'" + row["FRHOUSE"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["FRBUILDING"] != DBNull.Value) && (row["FRBUILDING"].ToString().Trim() != ""))
                //    sql += "'" + row["FRBUILDING"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["FRFLAT"] != DBNull.Value) && (row["FRFLAT"].ToString().Trim() != ""))
                //    sql += "'" + row["FRFLAT"].ToString().Trim() + "', ";
                //else sql += " null, ";


                ////куда прибыл
                //if ((row["TOCOUNTRY_"] != DBNull.Value) && (row["TOCOUNTRY_"].ToString().Trim() != ""))
                //    sql += "'" + row["TOCOUNTRY_"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["TOREGION_R"] != DBNull.Value) && (row["TOREGION_R"].ToString().Trim() != ""))
                //    sql += "'" + row["TOREGION_R"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["TOAREA_RN"] != DBNull.Value) && (row["TOAREA_RN"].ToString().Trim() != ""))
                //    sql += "'" + row["TOAREA_RN"].ToString().Trim() + "', ";
                //else sql += " null, ";

                //if ((row["TOCITY_RN"] != DBNull.Value) && (row["TOCITY_RN"].ToString().Trim() != ""))
                //    sql += "'" + row["TOCITY_RN"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["TOTOWN_RN"] != DBNull.Value) && (row["TOTOWN_RN"].ToString().Trim() != ""))
                //    sql += "'" + row["TOTOWN_RN"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["TOSTREET_R"] != DBNull.Value) && (row["TOSTREET_R"].ToString().Trim() != ""))
                //    sql += "'" + row["TOSTREET_R"].ToString().Trim() + "', ";
                //else sql += " null, ";

                //if ((row["TOHOUSE"] != DBNull.Value) && (row["TOHOUSE"].ToString().Trim() != ""))
                //    sql += "'" + row["TOHOUSE"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["TOBUILDING"] != DBNull.Value) && (row["TOBUILDING"].ToString().Trim() != ""))
                //    sql += "'" + row["TOBUILDING"].ToString().Trim() + "', ";
                //else sql += " null, ";
                //if ((row["TOFLAT"] != DBNull.Value) && (row["TOFLAT"].ToString().Trim() != ""))
                //    sql += "'" + row["TOFLAT"].ToString().Trim() + "') ";
                //else sql += " null) ";

                sql +=
                    " WHERE "+Points.Pref+"_data"+tableDelimiter+"parus_kart.orbase_rn = ";
                if ((row["PERSON_RN"] != DBNull.Value) && (row["PERSON_RN"].ToString().Trim() != ""))
                    sql += "'" + row["PERSON_RN"].ToString().Trim() + "' ";
                else sql += " null ";

                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
            }
            SetLoadStat(con_db, "ReadPvsArriv", "Успешно завершено");
        }



        private void ReadPvsDepar(DataTable dataFromFile, IDbConnection con_db)
        {
            //string sql = "";
            //foreach (DataRow row in dataFromFile.Rows)
            //{
                //
            //}
            SetLoadStat(con_db, "ReadPvsDepar", "Успешно завершено");
        }

        private
            void ReadFaceAcce(DataTable dataFromFile, IDbConnection con_db)
        {
            string sql = "";
            int number;
            bool result;

            foreach (DataRow row in dataFromFile.Rows)
            {
                //TODO date_begin, dat_close
                sql =
                    "UPDATE "+Points.Pref+"_data"+tableDelimiter+"parus_kart " +
                    " SET nzp_kvar = ";

                if ((row["NUM"] != DBNull.Value) && (row["NUM"].ToString().Trim() != ""))
                {
                    result = Int32.TryParse(Convert.ToString(row["NUM"]), out number);
                    if (result)
                    {
                        sql += "'" + row["NUM"].ToString().Trim() + "' ";
                    }
                    else sql += " null ";
                }
                else sql += " null ";

                sql +=
                    " WHERE "+Points.Pref+"_data"+tableDelimiter+"parus_kart.faceacc_rn = ";
                if ((row["RN"] != DBNull.Value) && (row["RN"].ToString().Trim() != ""))
                    sql += "'" + row["RN"].ToString().Trim() + "' ";
                else sql += " null ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
            }

            //sql =
            //    "DELETE  FROM " + Points.Pref + "_data" + tableDelimiter + "parus_kart " +
            //    "WHERE " + Points.Pref + "_data" + tableDelimiter + "parus_kart.nzp_kvar NOT IN ( select nzp_kvar from " + Points.Pref + "_data" + tableDelimiter + "kvar)";
            //ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);

            //sql =
            //    "DELETE  FROM " + Points.Pref + "_data" + tableDelimiter + "parus_kart " +
            //    "WHERE nzp_kvar IS NULL";
            //ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);


            SetLoadStat(con_db, "ReadFaceAcce", "Успешно завершено");
        }


        /// <summary>
        /// Считывания справочника адресного пространства
        /// </summary>
        /// <param name="dataFromFile"></param>
        /// <param name="con_db"></param>
        private void ReadAddrDict(DataTable dataFromFile, IDbConnection con_db)
        {
                //DROP TABLE IF EXISTS ADDRDICT_PARUS;
                // CREATE TABLE ADDRDICT_PARUS (
                //ADDR_RN                          Character(4), 
                //COUNTRY_RN                       Character(4), 
                //REGION_RN                        Character(4), 
                //AREA_RN                          Character(4), 
                //CITY_RN                          Character(4), 
                //TOWN_RN                          Character(4), 
                //ADDRTYPE_R                       Character(4), 
            //ADDRTYPE                       Character(60), 
            //NAME                             Character(40));

            string sql = "";
            foreach (DataRow row in dataFromFile.Rows)
            {
                sql = "INSERT INTO "+Points.Pref+"_data"+tableDelimiter+"parus_addrdict " +
                      "( " +
                      " ADDR_RN, LEVEL, COUNTRY_RN, REGION_RN, AREA_RN," +
                      " CITY_RN, TOWN_RN, ADDRTYPE_R, NAME " +
                      ") " +
                      "VALUES " +
                      "(";
                if ((row["ADDR_RN"] != DBNull.Value) && (row["ADDR_RN"].ToString().Trim() != ""))
                    sql += "'" + row["ADDR_RN"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["LEVEL"] != DBNull.Value) && (row["LEVEL"].ToString().Trim() != ""))
                    sql += "'" + row["LEVEL"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["COUNTRY_RN"] != DBNull.Value) && (row["COUNTRY_RN"].ToString().Trim() != ""))
                    sql += "'" + row["COUNTRY_RN"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["REGION_RN"] != DBNull.Value) && (row["REGION_RN"].ToString().Trim() != ""))
                    sql += "'" + row["REGION_RN"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["AREA_RN"] != DBNull.Value) && (row["AREA_RN"].ToString().Trim() != ""))
                    sql += "'" + row["AREA_RN"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["CITY_RN"] != DBNull.Value) && (row["CITY_RN"].ToString().Trim() != ""))
                    sql += "'" + row["CITY_RN"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["TOWN_RN"] != DBNull.Value) && (row["TOWN_RN"].ToString().Trim() != ""))
                    sql += "'" + row["TOWN_RN"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["ADDRTYPE_R"] != DBNull.Value) && (row["ADDRTYPE_R"].ToString().Trim() != ""))
                    sql += "'" + row["ADDRTYPE_R"].ToString().Trim() + "', ";
                else sql += " null, ";
                if ((row["NAME"] != DBNull.Value) && (row["NAME"].ToString().Trim() != ""))
                    sql += "'" + row["NAME"].ToString().Trim() + "') ";
                else sql += " null) ";


                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
                //SetLoadStat(con_db, "ReadAddrDict", "Выполняется");
            }

            SetLoadStat(con_db, "ReadAddrDict", "Успешно завершено");
        }

        /// <summary>
        /// Считывания справочника типов адресов
        /// </summary>
        /// <param name="dataFromFile"></param>
        /// <param name="con_db"></param>
        private void ReadAddrType(DataTable dataFromFile, IDbConnection con_db)
        {
             string sql = "";
            foreach (DataRow row in dataFromFile.Rows)
            {
                sql =
                    " UPDATE "+Points.Pref+"_data"+tableDelimiter+"parus_addrdict SET addrtype = ";
                if ((row["CODE"] != DBNull.Value) && (row["CODE"].ToString().Trim() != ""))
                    sql += "'" + row["CODE"].ToString().Trim() + "' ";
                else sql += " null ";

                sql += 
                    " WHERE addrtype_r = ";
                if ((row["ADDRTYPE_R"] != DBNull.Value) && (row["ADDRTYPE_R"].ToString().Trim() != ""))
                    sql += "'" + row["ADDRTYPE_R"].ToString().Trim() + "' ";
                else sql += " null ";
                    
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
                SetLoadStat(con_db, "ReadAddrType", "Выполняется");
            }
            SetLoadStat(con_db, "ReadAddrType", "Успешно завершено");
        }

        private void ReadCountry(DataTable dataFromFile, IDbConnection con_db)
        {
            string sql = "";
            foreach (DataRow row in dataFromFile.Rows)
            {
                sql =
                    " UPDATE  "+Points.Pref+"_data"+tableDelimiter+"parus_kart SET strana_mr = ";
                if ((row["CODE"] != DBNull.Value) && (row["CODE"].ToString().Trim() != ""))
                    sql += "'" + row["CODE"].ToString().Trim() + "' ";
                else sql += " null ";
                sql +=
                    " WHERE bh_country = ";
                if ((row["country_rn"] != DBNull.Value) && (row["country_rn"].ToString().Trim() != ""))
                    sql += "'" + row["country_rn"].ToString().Trim() + "' ";
                else sql += " null ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);

                sql =
                    " UPDATE  "+Points.Pref+"_data"+tableDelimiter+"parus_kart SET strana_op = ";
                if ((row["CODE"] != DBNull.Value) && (row["CODE"].ToString().Trim() != ""))
                    sql += "'" + row["CODE"].ToString().Trim() + "' ";
                else sql += " null ";
                sql +=
                    " WHERE frcountry_ = ";
                if ((row["country_rn"] != DBNull.Value) && (row["country_rn"].ToString().Trim() != ""))
                    sql += "'" + row["country_rn"].ToString().Trim() + "' ";
                else sql += " null ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);

                sql =
                    " UPDATE  "+Points.Pref+"_data"+tableDelimiter+"parus_kart SET strana_ku = ";
                if ((row["CODE"] != DBNull.Value) && (row["CODE"].ToString().Trim() != ""))
                    sql += "'" + row["CODE"].ToString().Trim() + "' ";
                else sql += " null ";
                sql +=
                    " WHERE tocountry_ = ";
                if ((row["country_rn"] != DBNull.Value) && (row["country_rn"].ToString().Trim() != ""))
                    sql += "'" + row["country_rn"].ToString().Trim() + "' ";
                else sql += " null ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
                SetLoadStat(con_db, "ReadCountry", "Выполняется");
            }
            SetLoadStat(con_db, "ReadCountry", "Успешно завершено");
        }

        private void ReadRegion(DataTable dataFromFile, IDbConnection con_db)
        {
            string sql = "";
            foreach (DataRow row in dataFromFile.Rows)
            {

                sql =
                    " UPDATE  "+Points.Pref+"_data"+tableDelimiter+"parus_kart SET region_mr = ";
                if ((row["CODE"] != DBNull.Value) && (row["CODE"].ToString().Trim() != ""))
                    sql += "'" + row["CODE"].ToString().Trim() + "' ";
                else sql += " null ";
                sql +=
                    " WHERE bh_region_ = ";

                if ((row["region_rn"] != DBNull.Value) && (row["region_rn"].ToString().Trim() != ""))
                    sql += "'" + row["region_rn"].ToString().Trim() + "' ";
                else sql += " null ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);


                sql =
                    " UPDATE  "+Points.Pref+"_data"+tableDelimiter+"parus_kart SET region_op = ";
                if ((row["CODE"] != DBNull.Value) && (row["CODE"].ToString().Trim() != ""))
                    sql += "'" + row["CODE"].ToString().Trim() + "' ";
                else sql += " null ";
                sql +=
                    " WHERE frregion_r = ";

                if ((row["region_rn"] != DBNull.Value) && (row["region_rn"].ToString().Trim() != ""))
                    sql += "'" + row["region_rn"].ToString().Trim() + "' ";
                else sql += " null ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);

                sql =
                    " UPDATE  "+Points.Pref+"_data"+tableDelimiter+"parus_kart SET region_ku = ";
                if ((row["CODE"] != DBNull.Value) && (row["CODE"].ToString().Trim() != ""))
                    sql += "'" + row["CODE"].ToString().Trim() + "' ";
                else sql += " null ";
                sql +=
                    " WHERE frregion_r = ";

                if ((row["region_rn"] != DBNull.Value) && (row["region_rn"].ToString().Trim() != ""))
                    sql += "'" + row["region_rn"].ToString().Trim() + "' ";
                else sql += " null ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
                //SetLoadStat(con_db, "ReadRegion", "Выполняется");
            }
            SetLoadStat(con_db, "ReadRegion", "Успешно завершено");
        }
        private string CompareNzp_dok(string nzp_dok)
        {
            //паспорт гражданина СССР
            if (nzp_dok == "000D")
            {
                return  "1";
            }

            //Свидетельство о рождении
            if (nzp_dok == "000I")
            {
                return "2";
            }

            //военный билет солдата         
            if (nzp_dok == "0003")
            {
                return "4";
            }

            //паспорт РФ
            if (nzp_dok == "000C")
            {
                return "10";
            }

            //пенсионное удостоверение
            if (nzp_dok == "000E")
            {
                return "20";
            }

            //удостоверение участника ВОВ
            if (nzp_dok == "000L")
            {
                return "26";
            }

            //уд-ние инвалида
            if (nzp_dok == "000M")
            {
                return "5";
            }

            //уд-ние ликвидатора аварии на ЧАЭС
            if (nzp_dok == "000N")
            {
                return "29";
            }

            //по умолчанию ставим признак 'другой документ'
            return "22";
        }

        private void CompareData( IDbConnection con_db)
        {
            SetLoadStat(con_db, "CompareData", "Старт");
            string sql = "пустой запрос";


            ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
            SetLoadStat(con_db, "CompareData", "Успешно завершено");
        }


        private void SetLoadStat(IDbConnection con_db, string funcName, string status)
        {
            string sql =
               " UPDATE "+Points.Pref+"_data"+tableDelimiter+"parus_load_status SET " + funcName + " = '" + status + "'" +
               " WHERE nzp_load = " + nzp_load;
            ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
        }

        public void CreateTempTables(IDbConnection con_db)
        {
          
        }


       }


}
