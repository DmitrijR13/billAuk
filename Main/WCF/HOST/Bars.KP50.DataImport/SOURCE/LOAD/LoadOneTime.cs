using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;


namespace Bars.KP50.DataImport.SOURCE.LOADER
{
    public class DBLoadOneTime : DbAdminClient
    {
        private readonly IDbConnection con_db;
        private FilesImported finder { get; set; }
        public string fDirectory { get; set; }
        public string fullFileName { get; set; }
        public StringBuilder errKvar { get; set; }

        int nzp_version = 99;

        public DBLoadOneTime(IDbConnection conDb)
        {
            con_db = conDb;
        }

        public Returns LoadOneTime(FilesImported finder)
        {
            this.finder = finder;
            Returns ret = new Returns(); 

            StringBuilder err = new StringBuilder();

            int nzpExc = DbFileLoader.AddMyFile("Разовая загрузка", this.finder);

            try
            {
                DbFileLoader fl = new DbFileLoader(con_db);
                //директория файла
                fDirectory = InputOutput.GetInputDir();

                fullFileName = DbFileLoader.DecompressionFile(finder.saved_name, InputOutput.GetInputDir(), ".csv", ref ret);

                this.finder.nzp_file = InsertIntoFiles_imported();

                string commStr =
                    " update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set nzp_exc = " + nzpExc +
                    " where nzp_file = " + finder.nzp_file;
                ExecSQL(this.con_db, commStr, true);

                string fn4 = "";
                if (InputOutput.useFtp)
                {
                    fn4 = InputOutput.SaveInputFile(String.Format("{0}{1}", fDirectory, System.IO.Path.GetFileName(finder.saved_name)));
                }

                fl.SetMyFileState (new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Success,
                    exc_path = InputOutput.useFtp ? fn4 : Path.Combine(fDirectory, System.IO.Path.GetFileName(finder.saved_name))
                });

                string[] fileStrings = DbFileLoader.ReadFile(fullFileName);

                DbValuesFromFile valuesFromFile = new DbValuesFromFile
                {
                    finder = this.finder,
                    loaded13section = false,
                    fileName = fullFileName,
                    fileStrings = fileStrings
                };

                ReadInfo(valuesFromFile);

                if (valuesFromFile.err.Length != 0)
                {
                    commStr =
                    "update " + Points.Pref + DBManager.sUploadAliasRest +
                    "files_imported set (percent, nzp_status) = (1, 3) " +
                    " where nzp_file = " + finder.nzp_file;
                    ExecSQL(con_db, commStr, true);
                }
                else
                {
                    commStr =
                           "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set (percent, nzp_status) = (1, 2)" +
                           " where nzp_file = " + valuesFromFile.finder.nzp_file;
                    ret = ExecSQL(con_db, commStr, true);

                    valuesFromFile.err.Append("Ошибок несоответствия формату не найдено");
                }

                #region архивирование лога
                int nzpExcLog = DbFileLoader.AddMyFile("Лог файла разовой загрузки", this.finder);

                    string logFileFullName = fullFileName + ".log";
                    StreamWriter sw = File.CreateText(logFileFullName);
                    sw.Write(valuesFromFile.err.ToString());
                    sw.Flush();
                    sw.Close();

                    string logArchiveFullName = fullFileName + "_LOG.zip";
                    Archive.GetInstance().Compress(logArchiveFullName, new string[] {logFileFullName});
                /*
                    SevenZipCompressor szcComperssor = new SevenZipCompressor();
                    szcComperssor.ArchiveFormat = OutArchiveFormat.Zip;

                    szcComperssor.CompressFiles(logArchiveFullName, logFileFullName);
                */

                    string fn2 = "";
                    if (InputOutput.useFtp) fn2 = InputOutput.SaveInputFile(logArchiveFullName);

                    commStr =
                    "update " + Points.Pref + DBManager.sUploadAliasRest +
                    "files_imported set (nzp_exc_log) = (" + nzpExcLog + ") " +
                    " where nzp_file = " + finder.nzp_file;
                    ExecSQL(con_db, commStr, true);

                    //fullFileName = String.Format("{0}log_{1}.zip", fDirectory, fullFileName.Replace(fDirectory, ""));
                    fullFileName = logArchiveFullName;

                    fl.SetMyFileState(new ExcelUtility()
                    {
                        nzp_exc = nzpExcLog,
                        status = ExcelUtility.Statuses.Success,
                        exc_path = InputOutput.useFtp ? fn2 : fullFileName 
                    });


                    File.Delete(logFileFullName);
                    File.Delete(fullFileName);
                #endregion


                ret.result = true;
                ret.text = "Файл успешно загружен.";
                ret.tag = -1;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при выполнении ф-ции LoadFile. \n" + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                err = new StringBuilder();
                err.Append(ret.text + ex.Message);
                return ret;
            }
            return ret;
        }


        /// <summary>
        /// Ф-ция добавления информации о файле в таблицу files_imported
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="con_db"></param>
        /// <param name="nzp_version"></param>
        /// <param name="fileName"></param>
        /// <returns>Уникальный номер файла nzp_file</returns>>
        private int InsertIntoFiles_imported()
        {
            int localUSer = finder.nzp_user;

            /*
            Returns ret;
            DbWorkUser db = new DbWorkUser();
            int localUSer = db.GetLocalUser(con_db, finder, out ret);
            if (!ret.result)
            {
                //MonitorLog.WriteLog(, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = " Ошибка определения локального пользователя. ";
                ret.tag = -1;
                throw new Exception("Ошибка определения локального пользователя.");
            }*/

            string sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                " ( nzp_version, loaded_name, saved_name, nzp_status, " +
                "    created_by, created_on, percent, pref) " +
                " VALUES (" + nzp_version + ",'" + finder.loaded_name + "',\'" + finder.saved_name + "\',1," +
                localUSer + "," + sCurDateTime + ", 0, '" + finder.bank + "')  ";
            ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
            return GetSerialValue(con_db);
        }

        public Returns ReadInfo(DbValuesFromFile valuesFromFile)
        {
            var ret = new Returns();

            #region Запросы
            try
            {
                //Создание таблиц для разовой загрузки
                CreateTableForOneTimeLoader(con_db);

                int strNum = 0;

                #region Собрать запросы
                //цикл по строкам
                foreach (string str in valuesFromFile.fileStrings)
                {
                    //чтение и парсинг строки
                    ret = ReadParseString(valuesFromFile, str);
                    strNum ++;

                    //обработка строк
                    switch (finder.type_load)
                    {
                        case 1:
                        {
                            // разовая загрузка сальдо
                            ret = LoadSaldo(valuesFromFile);

                            break;
                        }
                        case 2:
                        {
                            //Разовая загрузка показаний ИПУ
                            ret = LoadIpuValue(valuesFromFile);

                            break;
                        }
                        case 3:
                        {
                            // Загрузка параметров ЛС
                            ret = LoadParamLS(valuesFromFile);

                            break;
                        }
                        case 4:
                        {
                            //Загрузка параметров ЛС по старому ЛС
                            ret = LoadParamLSUsingOldNumLS(valuesFromFile);

                            break;
                        }
                        case 5:
                        {
                            //загрузка перекидок
                            ret = LoadPerekidka(valuesFromFile);

                            break;
                        }
                        case 6:
                        {
                            //загрузка оплат
                            ret = LoadOplata(valuesFromFile);

                            break;
                        }
                        case 7:
                        {
                            //загрузка жильцов (дата прибытия/убытия)
                            ret = LoadGilec(valuesFromFile);

                            break;
                        }
                        case 8:
                        {
                            //пропускаем заголовок
                            if(strNum == 1) break;
                            //Загрузка Таблица данных РРКЦ
                            ret = LoadRRKC(valuesFromFile);

                            break;
                        }
                        case 9:
                        {
                            //загрузка недопоставок
                            ret = LoadNedop(valuesFromFile);

                            break;
                        }

                    }
                }

                #endregion

                #region Общие запросы
                if (valuesFromFile.err.Length != 0)
                {
                    valuesFromFile.err.Append("Файл не был загружен в связи с ошибками в файле");
                    ret.text = "";
                }
                else
                {
                    switch (finder.type_load)
                    {
                        case 1:
                        {
                            //Загрузка сальдо
                            ret = DisassSaldo(valuesFromFile);

                            break;
                        }
                        case 2:
                        {
                            //Загрузка показаний ИПУ
                            ret = DisassIpuValue(valuesFromFile);

                            break;
                        }
                        case 3:
                        {
                            //Загрузка параметров ЛС
                            ret = DisassParamLS(valuesFromFile);

                            break;
                        }
                        case 4:
                        {
                            //Загрузка параметров ЛС по старому номеру ЛС
                            ret = DisassParamLSUsingOldNumLS(valuesFromFile);

                            break;
                        }
                        case 5:
                        {
                            //Загрузка перекидок
                            ret = DisassPerekidka(valuesFromFile);

                            break;
                        }
                        case 6:
                        {
                            //Загрузка оплат
                            ret = DisassOplata(valuesFromFile);

                            break;
                        }
                        case 7:
                        {
                            //Загрузка жильцов
                            ret = DisassGilec(valuesFromFile);

                            break;
                        }
                        case 8:
                        {
                            //Загрузка жильцов
                            ret = DisassRRKC(valuesFromFile);

                            break;
                        }
                        case 9:
                        {
                            //Загрузка недопоставок
                            ret = DisassNedop(valuesFromFile);

                            break;
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры ReadInfo : " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                //return ret;
            }
            //}
            #endregion

            return ret;
        }




        /// <summary>
        /// Чтение и парсинг строк из файла
        /// </summary>
        /// <param name="valuesFromFile"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        private Returns ReadParseString(DbValuesFromFile valuesFromFile, string str)
        {
            Returns ret = new Returns();
            //защита от пустых строк(пустые строки для сохранения нумерации)
            if (str.Trim() == "")
            {
                return ret;
            }

            ret = new Returns();
            valuesFromFile.sql = "";

            //массив значений строки
            valuesFromFile.vals = str.Split(new char[] {'|', ';'}, StringSplitOptions.None);
            Array.ForEach(valuesFromFile.vals, x => x = x.Trim());

            //пропуск пустой строчки
            if (valuesFromFile.vals.Length == 0)
            {
                return ret;
            }

            //номер строки в файле
            valuesFromFile.rowNumber = Environment.NewLine + " (строка " +
                                       (Array.IndexOf(valuesFromFile.fileStrings, str) + 1).ToString() + ") ";
            ret.text += valuesFromFile.rowNumber;
            valuesFromFile.rowNumber1 = Array.IndexOf(valuesFromFile.fileStrings, str) + 1;

            if (valuesFromFile.fileStrings.Length/25 != 0 &&
                valuesFromFile.rowNumber1%(valuesFromFile.fileStrings.Length/25) == 0)
            {
                string sqlPercent =
                    "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set percent = " +
                    ((decimal) valuesFromFile.rowNumber1/(decimal) valuesFromFile.fileStrings.Length) +
                    " where nzp_file = " + valuesFromFile.finder.nzp_file;
                ret = ExecSQL(con_db, sqlPercent, true);
            }
            return ret;
        }

        #region загрузка строк и проверка на корректность полей

        private Returns LoadRRKC(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            if (valuesFromFile.vals.Length < 20)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 20 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc" +
                " (ls, town, rajon, ulica, dom, korp, nkvar, nkvar_n, fio, kol_gil, ob_pl, otap_pl, etazh, tel, ind," +
                " supp_id, serv1, sum_in1, serv2, sum_in2, serv3, sum_in3, nzp_file) " +
                " VALUES (";
            int i = 0;

            string ls = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " код ЛС");
            sql += ls + ", ";
            i++;

            string town = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, " Город район ");
            sql += town + ", ";
            i++;

            string rajon = CheckType.CheckText2(valuesFromFile, i, false, 30, ref ret, " Населенный пункт ");
            sql += rajon + ", ";
            i++;

            string ulica = CheckType.CheckText2(valuesFromFile, i, true, 40, ref ret, " Улица ");
            sql += ulica + ", ";
            i++;

            string dom = CheckType.CheckText2(valuesFromFile, i, true, 10, ref ret, " Дом ");
            sql += dom + ", ";
            i++;

            string korp = CheckType.CheckText2(valuesFromFile, i, false, 3, ref ret, " Корпус ");
            sql += korp + ", ";
            i++;

            string nkvar = CheckType.CheckText2(valuesFromFile, i, false, 10, ref ret, " Квартира ");
            sql += nkvar + ", ";
            i++;

            string nkvar_n = CheckType.CheckText2(valuesFromFile, i, false, 3, ref ret, " Комната ");
            sql += nkvar_n + ", ";
            i++;

            string fio = CheckType.CheckText2(valuesFromFile, i, false, 120, ref ret, " Абонент ");
            sql += fio + ", ";
            i++;

            string kol_gil = CheckType.CheckInt(valuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                kol_gil = "0";
                //valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Количество жильцов");
            }
            sql += kol_gil + ", ";
            i++;

            string ob_pl = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Общая площадь");
            }
            sql += ob_pl + ", ";
            i++;

            string otap_pl = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Отапливаемая площадь");
            }
            sql += otap_pl + ", ";
            i++;

            string etazh = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, " Этаж");
            sql += etazh + ", ";
            i++;

            string tel = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Телефон ");
            sql += tel + ", ";
            i++;

            string ind = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Индекс ");
            sql += ind + ", ";
            i++;

            string supp_id = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, " Код поставщика ");
            sql += supp_id + ", ";
            i++;

            string serv1 = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, " Код услуги 1 ");
            sql += serv1 + ", ";
            i++;

            string sum_in1 = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Входящее сальдо 1");
            }
            sql += sum_in1 + ", ";
            i++;

            string serv2 = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, " Код услуги 2 ");
            sql += serv2 + ", ";
            i++;

            string sum_in2 = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Входящее сальдо 2");
            }
            sql += sum_in2 + ", ";
            i++;

            string serv3 = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, " Код услуги 3 ");
            sql += serv3 + ", ";
            i++;

            string sum_in3 = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Входящее сальдо 3");
            }
            sql += sum_in3 + ", ";
            i++;

            sql += valuesFromFile.finder.nzp_file + ")";

            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла " + ret.text, MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns LoadGilec(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns(); 
            string sql;

            if (valuesFromFile.vals.Length < 6)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 6 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
                " (ls, nomer, dat_ub, fio, dat_rogd, dat_pr, nzp_file) " +
                " VALUES (";
            int i = 0;

            string ls = CheckType.CheckInt(valuesFromFile.vals[i].Trim(), true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код ЛС");
            }
            sql += ls + ", ";
            i++;

            string nomer = CheckType.CheckInt(valuesFromFile.vals[i].Trim(), true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " номер");
            }
            sql += nomer + ", ";
            i++;

            string dat_ub = CheckType.CheckDateTime(valuesFromFile.vals[i].Trim(), false, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата убытия");
            }
            sql += dat_ub + ", ";
            i++;

            string fio = CheckType.CheckText(valuesFromFile.vals[i].Trim(), true, 300, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " ФИО ");
            }
            sql += fio + ", ";
            i++;

            string dat_rogd = CheckType.CheckDateTime(valuesFromFile.vals[i].Trim(), false, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата рождения ");
            }
            sql += dat_rogd + ", ";
            i++;

            string dat_pr = CheckType.CheckDateTime(valuesFromFile.vals[i].Trim(), false, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата прибытия");
            }
            sql += dat_pr + ", ";
            i++;

            sql += valuesFromFile.finder.nzp_file + ")";

            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла " + ret.text, MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns LoadOplata(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns(); 
            string sql;

            if (valuesFromFile.vals.Length < 9)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 9 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata" +
                " (date_r, date_o, num_ls, fio, adres, nzp_serv, supp_name, sum_money, num_pack, date_pack, type_pack, nzp_file) " +
                " VALUES (";
            int i = 0;

            string date_r = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата");
            }
            sql += date_r + ", ";
            i++;

            string date_o = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата");
            }
            sql += date_o + ", ";
            i++;

            string num_ls = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код ЛС (Старой системы)");
            }
            sql += num_ls + ", ";
            i++;

            string fio = CheckType.CheckText(valuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " ФИО ");
            }
            if (fio == "") fio = "''";
            sql += fio + ", ";
            i++;

            string adres = CheckType.CheckText(valuesFromFile.vals[i], true, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " адрес ");
            }
            if (adres == "") adres = "''";
            sql += adres + ", ";
            i++;

            string nzp_serv = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код услуги ");
            }
            sql += nzp_serv + ", ";
            i++;

            string supp_name = CheckType.CheckText(valuesFromFile.vals[i], true, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " наименование поставщика ");
            }
            sql += supp_name.Replace('\"', '\0') + ", ";
            i++;

            string sum_money = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null,
                ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " денюшки");
            }
            sql += sum_money + ", ";
            i++;

            string nzp_pack = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код пачки(реестра) ");
            }
            sql += nzp_pack + ", ";
            i++;

            string date_pack = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата пачки(реестра)");
            }
            sql += date_pack + ", ";
            i++;

            string type_pack = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, 2, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " тип пачки(реестра) ");
            }
            sql += type_pack + ", ";
            i++;

            sql += valuesFromFile.finder.nzp_file + ")";

            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns LoadPerekidka(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns(); 
            string sql;

            if (valuesFromFile.vals.Length < 4)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 4 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper" +
                " (nzp_kvar, nzp_serv, nzp_supp, date_rcl, sum_insaldo, comment, nzp_file ) " +
                " VALUES (";
            int i = 0;

            string nzp_kvar = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " код ЛС из биллинга");
            sql += nzp_kvar + ", ";
            i++;

            string nzp_serv = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " код услуги ");
            sql += nzp_serv + ", ";
            i++;

            string nzp_supp = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " код поставщика ");
            sql += nzp_supp + ", ";
            i++;

            string date_rcl = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " дата");
            sql += date_rcl + ", ";
            i++;

            string sum_insaldo = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null,
                ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "сумма перекидки");
            }
            sql += sum_insaldo + ", ";
            i++;

            string comment = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "комментарий");
            sql += comment + ", ";
            i++;

            sql += valuesFromFile.finder.nzp_file + ")";



            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            }

            #endregion

            return ret;
        }

        private Returns LoadParamLSUsingOldNumLS(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns(); 
            string sql;

            if (valuesFromFile.vals.Length < 5)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 5 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadlsold" +
                " (num_ls, val_prm, date_s, date_po, nzp_prm, nzp_file ) " +
                " VALUES (";
            int i = 0;

            string num_ls = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код старого ЛС");
            }
            sql += num_ls + ", ";
            i++;

            string val_prm = CheckType.CheckText(valuesFromFile.vals[i], true, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " значение параметра ");
            }
            sql += val_prm + ", ";
            i++;

            string date_s = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата c ");
            }
            sql += date_s + ", ";
            i++;

            string date_po = CheckType.CheckDateTime(valuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата по ");
            }
            if (date_po == "null") date_po = "'01.01.3000'";
            sql += date_po + ", ";
            i++;

            string nzp_prm = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " номер параметра ");
            }
            sql += nzp_prm + ", ";
            i++;


            sql += valuesFromFile.finder.nzp_file + ")";

            ret = ExecSQL(con_db, sql, true);

            #endregion

            return ret;
        }

        private Returns LoadParamLS(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns(); 
            string sql;

            if (valuesFromFile.vals.Length < 5)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 5 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadls" +
                " (nzp_kvar, val_prm, date_s, date_po, nzp_prm, nzp_file ) " +
                " VALUES (";
            int i = 0;

            string nzp_kvar = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код ЛС");
            }
            sql += nzp_kvar + ", ";
            i++;

            string val_prm = CheckType.CheckText(valuesFromFile.vals[i], true, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " значение параметра ");
            }
            sql += val_prm + ", ";
            i++;

            string date_s = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата c ");
            }
            sql += date_s + ", ";
            i++;

            string date_po = CheckType.CheckDateTime(valuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата по ");
            }
            if (date_po == "null") date_po = "'01.01.3000'";
            sql += date_po + ", ";
            i++;

            string nzp_prm = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " номер параметра ");
            }
            sql += nzp_prm + ", ";
            i++;


            sql += valuesFromFile.finder.nzp_file + ")";

            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла " + ret.text, MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns LoadIpuValue(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns(); 
            string sql;

            if (valuesFromFile.vals.Length < 5)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 5 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadipu" +
                " (nzp_kvar, kod_ipu, date_p, val_p, nzp_serv, nzp_file ) " +
                " VALUES (";
            int i = 0;

            string nzp_kvar = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код ЛС");
            }
            sql += nzp_kvar + ", ";
            i++;

            string kod_ipu = CheckType.CheckText(valuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код ИПУ ");
            }
            sql += kod_ipu + ", ";
            i++;

            string date_p = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата");
            }
            sql += date_p + ", ";
            i++;

            string val_p = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null,
                ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " показание");
            }
            sql += val_p + ", ";
            i++;

            string nzp_serv = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код услуги ");
            }
            sql += nzp_serv + ", ";
            i++;


            sql += valuesFromFile.finder.nzp_file + ")";

            ret = ExecSQL(con_db, sql, true);

            #endregion

            return ret;
        }

        private Returns LoadSaldo(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns(); 
            string sql;

            if (valuesFromFile.vals.Length < 5)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 5 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadsaldo " +
                " (nzp_kvar, sum_insaldo, nzp_serv, nzp_supp, date_rcl, type_o, nzp_file ) " +
                " VALUES (";
            int i = 0;

            string nzp_kvar = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код ЛС");
            }
            sql += nzp_kvar + ", ";
            i++;


            string sum_insaldo = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null,
                ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " сальдо");
            }
            sql += sum_insaldo + ", ";
            i++;

            string nzp_serv = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код услуги ");
            }
            sql += nzp_serv + ", ";
            i++;

            string nzp_supp = CheckType.CheckInt(valuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " код поставщика ");
            }
            sql += nzp_supp + ", ";
            i++;

            string date_rcl = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " дата");
            }
            sql += date_rcl + ", ";
            i++;

            string type_o = CheckType.CheckInt(valuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " тип оплаты ");
            }
            sql += type_o + ", ";
            i++;

            sql += valuesFromFile.finder.nzp_file + ")";

            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns LoadNedop(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            if (valuesFromFile.vals.Length < 4)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: количество полей =" +
                                          valuesFromFile.vals.Length + " вместо 4 ");
                return ret;
            }

            #region читаем строку, кладем в таблицу _onetimeload

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "_onetimenedop" +
                " (nzp_kvar, nzp_serv, nzp_supp, dat_s, dat_po, percent, nzp_file ) " +
                " VALUES (";
            int i = 0;

            string nzp_kvar = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " код ЛС из биллинга");
            sql += nzp_kvar + ", ";
            i++;

            string nzp_serv = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " код услуги ");
            sql += nzp_serv + ", ";
            i++;

            string nzp_supp = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " код поставщика ");
            sql += nzp_supp + ", ";
            i++;

            string date_s = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " дата начала");
            sql += "'" + valuesFromFile.vals[i] + "', ";
            i++;

            string date_po = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " дата окончания");
            sql += "'" + valuesFromFile.vals[i] + "', ";
            i++;

            string percent = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null,
                ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "процент");
            }
            sql += percent + ", ";
            i++;


            sql += valuesFromFile.finder.nzp_file + ")";

            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            }

            #endregion

            return ret;
        }


        #endregion


        #region разбор загруженных файлов

        private Returns DisassRRKC(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;
            
            #region 1 секция

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                " SET (nzp_version, file_type) = (3, 99)" +
                " WHERE nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 1 секция ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 1 секция ");
            }

            sql = 
                " INSERT INTO " + Points.Pref +  DBManager.sUploadAliasRest + "file_head" +
                " (nzp_file, org_name, branch_name, inn, kpp, file_no, file_date, sender_phone, sender_fio, calc_date, row_number) " +
                " VALUES (" +
                valuesFromFile.finder.nzp_file + ", 'Характеристики ЖФ', '-', '0', '0', '1', " + sCurDate + ", '', '', " +
                " (SELECT dat_saldo" +
                " FROM " + finder.bank + sDataAliasRest + "saldo_date" +
                " WHERE iscurrent = 0), " +
                " (SELECT count(*)" +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc " +
                " WHERE nzp_file = " + valuesFromFile.finder.nzp_file + "))";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 1 секция ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 1 секция ");
            }

            #endregion

            #region 2 секция 

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_area" +
                " (id, nzp_file, area, inn, kpp) " +
                " VALUES (" +
                " 1, " + valuesFromFile.finder.nzp_file + ", 'НЕТ УК', '0', '0') ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 2 секция ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 2 секция");
            }

            #endregion

            #region 3 секция

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc" +
                " SET rajon = '-'" +
                " WHERE rajon is null AND nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 3 секция", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc" +
                " SET nkvar_n = '-'" +
                " WHERE nkvar_n is null AND nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 3 секция", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc" +
                " SET dom = '-'" +
                " WHERE dom is null AND nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 3 секция", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc" +
                " SET korp = '-'" +
                " WHERE korp is null AND nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 3 секция", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc" +
                " SET nkvar = '-'" +
                " WHERE nkvar is null AND nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 3 секция", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }


            try
            {
                sql = " drop table t_rrkc_dom ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
            }
            catch{}
            sql = 
                " create temp table t_rrkc_dom (" +
                " id SERIAL," +
                " town CHAR(30)," +
                " rajon CHAR(30)," +
                " ulica CHAR(40)," +
                " dom CHAR(10)," +
                " korp CHAR(3)," +
                " etazh INTEGER) " + DBManager.sUnlogTempTable;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }

            sql =
                " INSERT INTO t_rrkc_dom (town, rajon, ulica, dom, korp, etazh)" +
                " SELECT town, rajon, ulica, dom, korp, max(etazh) " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc " +
                " WHERE nzp_file = " + valuesFromFile.finder.nzp_file + 
                " GROUP BY 1, 2, 3, 4, 5 ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 3 секция", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc " +
                " SET dom_id = " +
                " (SELECT id FROM t_rrkc_dom t " +
                " WHERE trim(t.town) = trim(" + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc.town) " +
                " AND trim(" + sNvlWord + "(t.rajon, '')) = trim(" + sNvlWord + "(" + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc.rajon,'')) " +
                " AND trim(t.ulica) = trim(" + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc.ulica) " +
                " AND trim(t.dom) = trim(" + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc.dom) " +
                " AND trim(" + sNvlWord + "(t.korp,'')) = trim(" + sNvlWord + "(" + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc.korp,'')) ) " +
                " WHERE nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 3 секция", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }

            

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
                "(id, nzp_file, town, rajon, ulica, ndom, nkor," +
                " area_id, etazh,  total_square,  ls_row_number, odpu_row_number,  local_id)" +
                " SELECT id, " + valuesFromFile.finder.nzp_file + ", trim(upper(town)), trim(upper(rajon))," +
                " trim(upper(ulica)), trim(upper(dom)), trim(upper(korp))," +
                " '1', " + sNvlWord + "(etazh, 1), '0', 0, 0, id " +
                " FROM t_rrkc_dom ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 3 секция", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 3 секция");
            }
            
            #endregion

            #region 4 секция

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                " (id, nzp_file,  dom_id, ls_type," +
                " fam, ima, otch, nkvar, nkvar_n,  kol_gil, kol_vrem_prib, kol_vrem_ub, room_number, total_square,  otapl_square, is_communal, service_row_number, reval_params_row_number, ipu_row_number, nzp_status)" +
                " SELECT DISTINCT ls, " + valuesFromFile.finder.nzp_file + ", dom_id, 1, " +
                " substr(trim(fio), 1, " + Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' )-1) as fam," +
                  " substr(replace((substr((trim(fio)||' '), " + Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' )+1, " +
                  " length(fio) - " + Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';'),1, " + 
                  Points.Pref + DBManager.sDataAliasRest + "pos(replace((substr((trim(fio)||' '), " +
                  " " + Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' )+1, length(fio) - " + 
                  Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';'), ';')-1) as ima, " +
                  sNvlWord +
                  "(substr(replace((substr((trim(fio)||' '), " + Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' )+1, " +
                  "length(fio) - " + Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';')," + 
                  Points.Pref + DBManager.sDataAliasRest + "pos(replace((substr((trim(fio)||' '), " +
                  "" + Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' )+1, length(fio) - " + 
                  Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';'), ';')+1," +
                  "length(replace((substr((trim(fio)||' '), " + Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' )+1, length(fio) - " + 
                  Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';')) -" +
                  "" + Points.Pref + DBManager.sDataAliasRest + "pos(replace((substr((trim(fio)||' '), " + 
                  Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' )+1, length(fio) - " + 
                  Points.Pref + DBManager.sDataAliasRest + "pos(replace(trim(fio),' ', ';' ),';' ))||';'), " +
                  "' ', ';'), ';')-1), '')  as otch, " +
                  " nkvar, nkvar_n, " + sNvlWord + "(kol_gil,0), 0, 0, 1, " + sNvlWord + "(ob_pl, '0'), " + sNvlWord + "(otap_pl,'0'), 0, 0, 0, 0, 2 " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc " +
                " WHERE nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 4 секция ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            #region 5 секция

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_supp " +
                " (nzp_file, supp_id, supp_name, inn, kpp) " +
                " SELECT DISTINCT " + valuesFromFile.finder.nzp_file + ", " +
                " 1, ('поставщик '||supp_id), 0, 0 " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc " +
                " WHERE nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 5 секция ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 5 секция");
            }

            #endregion

            #region 6 секция

            try
            {
                sql = " drop table t_rrkc_serv ";
                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);
            }
            catch { }
            sql =
                " CREATE TEMP TABLE t_rrkc_serv (" +
                " ls INTEGER, " +
                " serv INTEGER," +
                " sum_insaldo " + sDecimalType + "(12,2) ) " + DBManager.sUnlogTempTable;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 6 секция");
            }

            sql =
                " INSERT INTO t_rrkc_serv (ls, serv, sum_insaldo) " +
                " SELECT DISTINCT ls, serv1, sum_in1 " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc " +
                " WHERE " + sNvlWord + "(serv1, '') <> ''" +
                " AND " + sNvlWord + "(sum_in1, '') <> ''" +
                " AND nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 6 секция");
            }

            sql =
                " INSERT INTO t_rrkc_serv (ls, serv, sum_insaldo) " +
                " SELECT DISTINCT ls, serv2, sum_in2 " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc " +
                " WHERE " + sNvlWord + "(serv2, '') <> ''" +
                " AND " + sNvlWord + "(sum_in2, '') <> ''" +
                " AND nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 6 секция");
            }

            sql =
                " INSERT INTO t_rrkc_serv (ls, serv, sum_insaldo) " +
                " SELECT DISTINCT ls, serv3, sum_in3 " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc " +
                " WHERE " + sNvlWord + "(serv3, '') <> ''" +
                " AND " + sNvlWord + "(sum_in3, '') <> ''" +
                " AND nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 6 секция");
            }

            sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                " (nzp_file, ls_id, supp_id, nzp_serv, sum_insaldo," +
                " eot, reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod," +
                " norm_rashod, is_pu_calc, sum_nach, sum_reval, sum_subsidy," +
                " sum_subsidyp, sum_lgota, sum_lgotap, sum_smo, sum_smop," +
                " sum_money, is_del, sum_outsaldo, servp_row_number) " +
                " SELECT DISTINCT " + valuesFromFile.finder.nzp_file + ", ls, 1, serv, sum_insaldo," +
                " '0', '0', '0', 6, '0'," +
                " '0', '0', '0', '0', '0', " +
                " '0', '0', '0', '0', '0', " +
                " '0', '0', '0', '0' " +
                " FROM  t_rrkc_serv";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла в DisassRRKC 6 секция ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД в DisassRRKC 6 секция");
            }

            #endregion

            #region 13 секция
            DbFileLoader fl = new DbFileLoader(con_db);
            fl.LoadOur13Section(con_db, new FilesImported() { nzp_file = valuesFromFile.finder.nzp_file });
            fl.FillMeasure(con_db, new FilesImported() {nzp_file = valuesFromFile.finder.nzp_file});
            #endregion


            #region проверка уникальности и связности

            using (var check = new Check())
            {
                //проверка уникальности данных
                check.CheckUnique(con_db, finder, valuesFromFile.err);

                //проверка связности БД
                check.CheckRelation(con_db, finder, valuesFromFile.err);
            }

            #endregion


            return ret;
        }

        private Returns DisassGilec(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            #region проставляем nzp_kvar

            sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
                  " set nzp_kvar = (select max(nzp_kvar) from " + valuesFromFile.finder.bank + "_data" +
                  tableDelimiter + "kvar where remark = ls)" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " and nzp_kvar is null ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = " select * from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
                  " where nzp_kvar is null and nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count > 0)
            {
                valuesFromFile.err.Append("Не проставилось " + dt.Rows.Count + " кодов ЛС");
                return ret;
            }

            #endregion

            #region Ставим даты. Если нет даты рождения - ставим '01.01.1901',если нет даты убытия и прибытия - ставим дату прибытия равную дате рождения

            //ставим дату рождения там, где ее нет
            sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
                  " set dat_rogd  = '01.01.1901'" +
                  " where dat_rogd is null and  nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            //если нет даты прибытия и даты убытия одновременно, ставим дату прибытия = дате рождения
            sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
                  " set dat_pr  = dat_rogd" +
                  " where dat_pr is null and dat_ub is null and nzp_file = " +
                  valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            //sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
            //      " set nzp_gil = (select max(nzp_gil) from " + valuesFromFile.finder.bank + "_data" + tableDelimiter + "kart k" +
            //      " where k.nzp_kvar = nzp_kvar " +
            //      " and upper(trim(fio)) matches (upper(trim(k.fam))||' '||upper(trim(k.ima))||' '||upper(trim(k.otch))))" +
            //      " where nzp_file = " + valuesFromFile.finder.nzp_file + " and nzp_kart is null";
            //ret = ExecSQL(con_db, sql, true);
            //if (!ret.result)
            //{
            //    MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            //}

            #region проставляем nzp_gil

            sql = "select nzp_kvar, dat_rogd, fio" +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file +
                  " and nzp_gil is null group by 1,2,3";
            dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            foreach (DataRow rr in dt.Rows)
            {
                //получаем nzp_gil
                sql = "INSERT INTO " + valuesFromFile.finder.bank + "_data" + tableDelimiter +
                      "gilec (nzp_gil) " +
#if PG
 " VALUES (DEFAULT)";
#else
 " VALUES (0)";
#endif

                decimal nzp_gil = ClassDBUtils.ExecSQL(sql, con_db, true).GetID();

                sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
                      " set nzp_gil = " + nzp_gil +
                      " where nzp_kvar = " + rr["nzp_kvar"].ToString() +
                      " and trim(fio) matches '" + rr["fio"].ToString().Trim() +
                      "' and dat_rogd = '" + rr["dat_rogd"].ToString().Substring(0, 10) + "'";
                ret = ExecSQL(con_db, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                    valuesFromFile.err.Append("Ошибка записи в БД");
                }
            }

            #endregion

            #region создаем карточки

            sql = "select nzp_kvar, fio, dat_rogd, dat_ub, dat_pr, nzp_gil, " +
                  " substr(trim(fio), 1, pos(replace(trim(fio),' ', ';' ),';' )-1) as fam," +
                  " substr(replace((substr((trim(fio)||' '), pos(replace(trim(fio),' ', ';' ),';' )+1, " +
                  " length(fio) - pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';'),1, pos(replace((substr((trim(fio)||' '), " +
                  " pos(replace(trim(fio),' ', ';' ),';' )+1, length(fio) - pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';'), ';')-1) as ima, " +
                  sNvlWord +
                  "(substr(replace((substr((trim(fio)||' '), pos(replace(trim(fio),' ', ';' ),';' )+1, " +
                  "length(fio) - pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';'),pos(replace((substr((trim(fio)||' '), " +
                  "pos(replace(trim(fio),' ', ';' ),';' )+1, length(fio) - pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';'), ';')+1," +
                  "length(replace((substr((trim(fio)||' '), pos(replace(trim(fio),' ', ';' ),';' )+1, length(fio) - pos(replace(trim(fio),' ', ';' ),';' ))||';'), ' ', ';')) -" +
                  "pos(replace((substr((trim(fio)||' '), pos(replace(trim(fio),' ', ';' ),';' )+1, length(fio) - pos(replace(trim(fio),' ', ';' ),';' ))||';'), " +
                  "' ', ';'), ';')-1), '')  as otch" +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file;
            dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            foreach (DataRow rr in dt.Rows)
            {
                //карточка убытия
                if (rr["dat_ub"].ToString() != "")
                {
                    sql = " insert into " + valuesFromFile.finder.bank + "_data" + tableDelimiter +
                          "kart " +
                          " (nzp_gil, isactual, fam, ima, otch, dat_rog, tprp,  nzp_tkrt, nzp_kvar, dat_sost, dat_ofor, dat_izm, is_unl, cur_unl)" +
                          " values" +
                          "(" + rr["nzp_gil"].ToString() + ", 1,'" + rr["fam"].ToString() + "',' " +
                          rr["ima"].ToString() + "', '" +
                          rr["otch"].ToString() + "' , '" + rr["dat_rogd"].ToString().Substring(0, 10) +
                          "'," +
                          " 'П', 2, " + rr["nzp_kvar"] + ", '" +
                          rr["dat_ub"].ToString().Substring(0, 10) + "', '" +
                          rr["dat_ub"].ToString().Substring(0, 10) + "', " +
                          sCurDate + ",1,1 )";
                    ret = ExecSQL(con_db, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                        valuesFromFile.err.Append("Ошибка записи в БД");
                    }
                }

                //карточка 
                if (rr["dat_pr"].ToString() != "")
                {
                    sql = " insert into " + valuesFromFile.finder.bank + "_data" + tableDelimiter +
                          "kart " +
                          " (nzp_gil, isactual, fam, ima, otch, dat_rog, tprp,  nzp_tkrt, nzp_kvar, dat_sost, dat_ofor, dat_izm, is_unl, cur_unl)" +
                          " values" +
                          "(" + rr["nzp_gil"].ToString() + ", 1,'" + rr["fam"].ToString() + "',' " +
                          rr["ima"].ToString() + "', '" +
                          rr["otch"].ToString() + "' , '" + rr["dat_rogd"].ToString().Substring(0, 10) +
                          "'," +
                          " 'П', 1, " + rr["nzp_kvar"] + ", '" +
                          rr["dat_pr"].ToString().Substring(0, 10) + "', '" +
                          rr["dat_pr"].ToString().Substring(0, 10) + "', " +
                          sCurDate + ",1,1 )";
                    ret = ExecSQL(con_db, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                        valuesFromFile.err.Append("Ошибка записи в БД");
                    }
                }
            }

            #endregion

            return ret;
        }

        private Returns DisassOplata(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            #region Определяем месяц

            string date_rcl = "";

            sql =
                " select max(date_r) as date_r from " + Points.Pref + DBManager.sUploadAliasRest +
                "_onetimeoplata" +
                " where nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            sql =
                " select min(date_r)  as date_r  from " + Points.Pref + DBManager.sUploadAliasRest +
                "_onetimeoplata" +
                " where nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dt1 = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            //если год и месяц равны
            if (dt.Rows[0]["date_r"].ToString() != dt1.Rows[0]["date_r"].ToString())
            {
                valuesFromFile.err.Append("Месяц расчета должен быть одинаковым по всему файлу");
                MonitorLog.WriteLog("Месяц расчета должен быть одинаковым по всему файлу",
                    MonitorLog.typelog.Error,
                    true);
                return ret;
            }
            else
            {
                string datet = dt1.Rows[0]["date_r"].ToString().Substring(0, 10);
                DateTime dtime =
                    new DateTime(Convert.ToInt32(datet.Substring(6, 4)),
                        Convert.ToInt32(datet.Substring(3, 2)), 1); //.AddMonths(-1);
                date_rcl = dtime.ToString("dd.MM.yyyy");
            }

            #endregion

            #region Опеределяем, что все оплаты одного типа

            int type_pack;
            sql = "select distinct type_pack from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeoplata";
            dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count != 1)
            {
                MonitorLog.WriteLog("Файл должен содержать оплаты одно типа ", MonitorLog.typelog.Error,
                    true);
                valuesFromFile.err.Append("Файл должен содержать оплаты одно типа ");
                return ret;
            }
            type_pack = Convert.ToInt32(dt.Rows[0]["type_pack"]);

            #endregion

            #region восстанавливаем nzp_supp

            //sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata" +
            //      " set nzp_supp =" +
            //      " (select max(c.nzp_supp) from " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
            //      tableDelimiter +" charge_" + date_rcl.Substring(3, 2) + " c " +
            //      " where c.nzp_kvar = nzp_kvar and c.nzp_serv = nzp_serv)" +
            //      " where nzp_file = " + valuesFromFile.finder.nzp_file + 
            //      " and nzp_supp is null ";
            //ret = ExecSQL(con_db, sql, true);
            //if (!ret.result)
            //{
            //    MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            //    valuesFromFile.err.Append("Ошибка записи в БД");
            //}

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata" +
                  " set nzp_supp = " +
                  " (select max(s.nzp_supp)" +
                  " from " + valuesFromFile.finder.bank + "_kernel" + tableDelimiter + "supplier s" +
                  " where trim(upper(s.name_supp)) = trim(upper(supp_name)) )" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file +
                  " and nzp_supp is null ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata" +
                  " set nzp_supp = 1 " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file +
                  " and nzp_supp is null ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            #region Выставляем nzp_kvar

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata" +
                  " set nzp_kvar = num_ls" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file +
                  " and nzp_kvar is null ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            //sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata" +
            //      " set nzp_kvar =" +
            //      " (select k.nzp" +
            //      " from " + Points.Pref + DBManager.sUploadAliasRest + "reestr_ls k" +
            //      " where k.id_ls = num_ls)" +
            //      " where nzp_file = " + valuesFromFile.finder.nzp_file +
            //      " and nzp_kvar is null ";ret = ExecSQL(con_db, sql, true);
            //if (!ret.result)
            //{
            //    MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            //    valuesFromFile.err.Append("Ошибка записи в БД");
            //}
            //sql = "select * from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata" +
            //      " where nzp_file = " + valuesFromFile.finder.nzp_file +
            //      " and nzp_kvar is null ";
            //dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            //if (dt.Rows.Count > 0)
            //{
            //    MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            //    valuesFromFile.err.Append("Не проставлены код ЛС (биллинг) в количестве " + dt.Rows.Count);
            //    return ret;
            //}

            #endregion

            string table_name;
            int kod_sum;
            if (type_pack == 1)
            {
                table_name = "fn_supplier" + date_rcl.Substring(3, 2);
                kod_sum = 34;
            }
            else
            {
                table_name = "from_supplier";
                kod_sum = 50;//???
            }

            #region формируем пачку

            sql = "  insert into  " + Points.Pref + "_fin_" + date_rcl.Substring(8, 2) + tableDelimiter +
                  "pack" +
                  " (nzp_supp, pack_type, nzp_bank,  num_pack, dat_uchet, dat_pack, " +
                  " count_kv, sum_pack,  flag, dat_vvod, " +
                  " sum_rasp, sum_nrasp,  dat_inp,  file_name, yearr)" +
                  " select o.nzp_supp,  20, 1,  o.num_pack, f.dat_oper, o.date_pack, " +
                  " count(nzp_kvar), sum(sum_money), 21, o.date_o, " +
                  " '0', sum(sum_money), " + sCurDate + ",  'разовая загрузка " +
                  valuesFromFile.finder.nzp_file + "', " +
                  valuesFromFile.finder.nzp_file +
                  " from  " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata o, " +
                  Points.Pref + "_data" + tableDelimiter + "fn_curoperday f " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file +
                  " group by 1, 4, 5, 6, 10";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            // номер пачки 
            int pack = GetSerialValue(con_db);

            sql = " insert into " + Points.Pref + "_fin_" + date_rcl.Substring(8, 2) + tableDelimiter +
                  " pack_ls" +
                  " (nzp_pack,  num_ls, g_sum_ls,  dat_month, " +
                  " kod_sum, nzp_supp, paysource,  dat_vvod, dat_uchet,  info_num, inbasket, alg," +
                  " unl,  nzp_user, incase)" +
                  " select  " + pack + ",  o.nzp_kvar, sum(o.sum_money),  o.date_r," +
                  kod_sum + ", o.nzp_supp, 1, o.date_o , f.dat_oper,  0, 0, '1'," +
                  " 0,  1, 0" +
                  " from  " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata o, " +
                  Points.Pref + "_data" + tableDelimiter + "fn_curoperday f " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file +
                  " group by  o.nzp_supp, o.nzp_kvar, o.date_r, o.date_o, f.dat_oper " +
                  " having sum(o.sum_money)<>0";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            #region записываем в fn_supplier или from_supplier

            sql = "insert into " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter + table_name +
                  "( nzp_pack_ls, nzp_serv, nzp_supp, num_charge, " +
                  "  num_ls, sum_prih, kod_sum, dat_month," +
                  " dat_prih, dat_uchet, dat_plat)" +
                  " select -1, o.nzp_serv, o.nzp_supp, " + pack + ", " +
                  " o.nzp_kvar, sum(o.sum_money), " + kod_sum + ", o.date_r," +
                  sCurDate + ", f.dat_oper, o.date_o" +
                  " from  " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata o, " +
                  Points.Pref + "_data" + tableDelimiter + "fn_curoperday f " +
                  " where  nzp_file = " + valuesFromFile.finder.nzp_file +
                  " group by  2,3,4,5,8,10,11 having sum(o.sum_money)<>0 ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "update " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter + table_name +
                  " set nzp_pack_ls =" +
                  " (select p.nzp_pack_ls from " + Points.Pref + "_fin_" + date_rcl.Substring(8, 2) + tableDelimiter +
                  " pack_ls p " +
                  " where p.num_ls =  " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) + tableDelimiter +
                  table_name + ".num_ls" +
                  " and p.nzp_pack =  " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) + tableDelimiter +
                  table_name + ".num_charge" +
                  " and p.nzp_pack = " + pack + ")" +
                  " where dat_prih = " + sCurDate + " and nzp_pack_ls = -1";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "select * from " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter + table_name +
                  " where nzp_pack_ls = -1 and num_charge = " + pack;
            dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count > 0)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Не проставлен nzp_pack_ls в from_supplier в количестве " + dt.Rows.Count +
                                          ". Обратитесь к разработчику");
                return ret;
            }

            #endregion

            return ret;
        }

        private Returns DisassPerekidka(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            #region выявляем месяц

            string date_rcl = "";

            sql =
                " select max(date_rcl) as date_rcl from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper" +
                " where nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            sql =
                " select min(date_rcl)  as date_rcl  from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper" +
                " where nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dt1 = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows[0]["date_rcl"].ToString() != dt1.Rows[0]["date_rcl"].ToString())
            {
                valuesFromFile.err.Append("При загрузке сальдо месяц должен быть одинаковый");
                return ret;
            }
            else
            {
                string datet = dt1.Rows[0]["date_rcl"].ToString().Substring(0, 10);
                DateTime dtime =
                    new DateTime(Convert.ToInt32(datet.Substring(6, 4)),
                        Convert.ToInt32(datet.Substring(3, 2)), 1).AddMonths(-1);
                date_rcl = "'" + dtime.ToString("dd.MM.yyyy") + "'";
            }

            #endregion

            #region Проверяем все ли ЛС есть в базе

            sql = 
                " SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper" + 
                " WHERE NOT EXISTS " +
                " (SELECT k.nzp_kvar FROM " + Points.Pref + sDataAliasRest + "kvar k" +
                " WHERE k.nzp_kvar = " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper.nzp_kvar)" +
                " AND  nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dtkvar = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dtkvar.Rows.Count > 0)
            {
                valuesFromFile.err.Append("Имеются ЛС, которых нет в базе, данные не загружены");
                return new Returns(false, "Имеются ЛС, которых нет в базе", -1);
            }

            #endregion

            #region Проверяем все ли поставщики есть в базе

            sql =
                " SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper" +
                " WHERE NOT EXISTS " +
                " (SELECT k.nzp_supp FROM " + Points.Pref + sKernelAliasRest + "supplier k" +
                " WHERE k.nzp_supp = " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper.nzp_supp)" +
                " AND  nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dtsupp = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dtsupp.Rows.Count > 0)
            {
                valuesFromFile.err.Append("Имеются договоры/поставщики, которых нет в базе, данные не загружены");
                return new Returns(false, "Имеются договоры/поставщики, которых нет в базе", -1);
            }

            #endregion

            #region перезапись перкидок

            sql =
                " insert into " + finder.bank + "_charge_" + date_rcl.Substring(9, 2) + tableDelimiter + "perekidka " +
                " ( nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, date_rcl, tarif," +
                " volum, sum_rcl, month_, comment, nzp_user, nzp_reestr) " +
                " select  b.nzp_kvar, b.nzp_kvar , b.nzp_serv,  b.nzp_supp , " +
                " 1, " + date_rcl + sConvToDate  + " , '0', '0',  b.sum_insaldo," +
                date_rcl.Substring(4, 2) + ", " + sNvlWord + "(comment,'" + valuesFromFile.finder.nzp_file + "') , 1, -7" +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper  b" +
                " where b.sum_insaldo <> 0 and nzp_file = " + valuesFromFile.finder.nzp_file +
                " group by 1,2,3,4,6,9, 11 ;";

            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            }

            #endregion

            return ret;
        }

        private Returns DisassParamLSUsingOldNumLS(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            #region Проставляем nzp_kvar

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadlsold" +
                  " set nzp_kvar =" +
                  " (select max(k.nzp_kvar) from " + finder.bank + "_data" + tableDelimiter + "kvar k " +
                  " where trim(k.remark) matches trim(num_ls))" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }
            sql = "select * from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadlsold" +
                  " where nzp_kvar is null and nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dt.Rows.Count > 0)
            {
                valuesFromFile.err.Append("Не проставилось " + dt.Rows.Count + " кодов ЛС");
                return ret;
            }

            #endregion

            #region чистка перед загрузкой

            sql = "update " + finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " set (dat_when, dat_po) = " +
                  " ((select " + sCurDate + ", min(o.date_s) -1 units day from  " + Points.Pref +
                  DBManager.sUploadAliasRest + "_onetimeloadlsold o " +
                  " where o.nzp_kvar = nzp_kvar and o.nzp_prm = nzp_prm and o.nzp_file = " +
                  valuesFromFile.finder.nzp_file + " ))" +
                  " where nzp in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and nzp_prm in (select nzp_prm from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and dat_s < (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_po >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "update " + finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " set dat_when = " + sCurDate + ", is_actual = 100 " +
                  " where nzp in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and nzp_prm in (select nzp_prm from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and dat_s >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_s = (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_po >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "update " + Points.Pref + "_data" + tableDelimiter + "prm_1" +
                  " set (dat_when, dat_po) = " +
                  " ((select " + sCurDate + ", min(o.date_s) -1 units day from  " + Points.Pref +
                  DBManager.sUploadAliasRest + "_onetimeloadlsold o " +
                  " where o.nzp_kvar = nzp_kvar and o.nzp_prm = nzp_prm and o.nzp_file = " +
                  valuesFromFile.finder.nzp_file + " ))" +
                  " where nzp in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and nzp_prm in (select nzp_prm from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and dat_s < (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_po >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "update " + Points.Pref + "_data" + tableDelimiter + "prm_1" +
                  " set dat_when = " + sCurDate + ", is_actual = 100 " +
                  " where nzp in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and nzp_prm in (select nzp_prm from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and dat_s >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_s = (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_po >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadlsold " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            }

            #endregion

            #region Загрузка в таблицу prm_1

            sql = " insert into " + finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl, nzp_user," +
                  " dat_when,  user_del)" +
                  " select nzp_kvar, nzp_prm, date_s, date_po, val_prm, 1,1, " + finder.nzp_user + "," +
                  sCurDate + ", nzp_file " +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadlsold" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = " insert into " + Points.Pref + "_data" + tableDelimiter + "prm_1" +
                  " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl, nzp_user," +
                  " dat_when,  user_del)" +
                  " select nzp_kvar, nzp_prm, date_s, date_po, val_prm, 1,1, " + finder.nzp_user + "," +
                  sCurDate + ", nzp_file " +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadlsold" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns DisassParamLS(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            #region чистка перед загрузкой

            sql = "update " + finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " set dat_when = " +
                  " (select " + sCurDate + " from  " + Points.Pref +
                  DBManager.sUploadAliasRest + "_onetimeloadls o " +
                  " where o.nzp_kvar = nzp_kvar and o.nzp_prm = nzp_prm and o.nzp_file = " +
                  valuesFromFile.finder.nzp_file + " ), " +
                  " dat_po= (select  min(o.date_s) -interval '1 day' from  " + Points.Pref +
                  DBManager.sUploadAliasRest + "_onetimeloadls o " +
                  " where o.nzp_kvar = nzp_kvar and o.nzp_prm = nzp_prm and o.nzp_file = " +
                  valuesFromFile.finder.nzp_file + " )" +
                  " where nzp in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and nzp_prm in (select nzp_prm from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and dat_s < (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_po >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "update " + finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " set dat_when = " + sCurDate + ", is_actual = 100 " +
                  " where nzp in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and nzp_prm in (select nzp_prm from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and dat_s >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_s = (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_po >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "update " + Points.Pref + "_data" + tableDelimiter + "prm_1" +
                  " set dat_when = " +
                  " (select distinct " + sCurDate + " from  " + Points.Pref +
                  DBManager.sUploadAliasRest + "_onetimeloadls o " +
                  " where o.nzp_kvar = nzp_kvar and o.nzp_prm = nzp_prm and o.nzp_file = " +
                  valuesFromFile.finder.nzp_file + " ), " +
                  "dat_po= (select  min(o.date_s) -interval '1 day' from  " + Points.Pref +
                  DBManager.sUploadAliasRest + "_onetimeloadls o " +
                  " where o.nzp_kvar = nzp_kvar and o.nzp_prm = nzp_prm and o.nzp_file = " +
                  valuesFromFile.finder.nzp_file + " )" +
                  " where nzp in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and nzp_prm in (select nzp_prm from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and dat_s < (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_po >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = "update " + Points.Pref + "_data" + tableDelimiter + "prm_1" +
                  " set dat_when = " + sCurDate + ", is_actual = 100 " +
                  " where nzp in (select nzp_kvar from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and nzp_prm in (select nzp_prm from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + ")" +
                  " and dat_s >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_s = (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )" +
                  " and dat_po >= (select min(date_s) from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadls " +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file + " )";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            }

            #endregion

            #region запись в prm_1

            sql = " insert into " + finder.bank + "_data" + tableDelimiter + "prm_1" +
                  " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl, nzp_user," +
                  " dat_when,  user_del)" +
                  " select nzp_kvar, nzp_prm, date_s, date_po, val_prm, 1,1, " + finder.nzp_user + "," +
                  sCurDate + ", nzp_file " +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadls" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = " insert into " + Points.Pref + "_data" + tableDelimiter + "prm_1" +
                  " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl, nzp_user," +
                  " dat_when,  user_del)" +
                  " select nzp_kvar, nzp_prm, date_s, date_po, val_prm, 1,1, " + finder.nzp_user + "," +
                  sCurDate + ", nzp_file " +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadls" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns DisassIpuValue(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            #region ставим nzp_cnt

            sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadipu" +
                  " set nzp_cnt =" +
                  " ( select max(c.nzp_counter) from  " + finder.bank + "_data" + tableDelimiter +
                  "counters_spis c " +
                  " where c.nzp_kvar = nzp_kvar and c.kod_ipu = num_cnt and c.nzp_serv = c.nzp_serv)" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            sql = " select nzp_kvar, nzp_serv, kod_ipu from " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadipu" +
                  " where nzp_cnt is null and nzp_file = " + valuesFromFile.finder.nzp_file +
                  " group by 1,2,3";
            DataTable dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            foreach (DataRow r in dt.Rows)
            {
                #region добавляем ПУ, которого нет

                string seq = Points.Pref + "_data" + tableDelimiter + "counters_spis_nzp_counter_seq";
                string strNzp_cnt;
#if PG
                                strNzp_cnt = " nextval('" + seq + "') ";
#else
                strNzp_cnt = seq + ".nextval ";
#endif

                sql = "insert into " + finder.bank + "_data" + tableDelimiter + "counters_spis " +
                      " (nzp_counter, nzp_type, nzp, nzp_serv, nzp_cnttype, num_cnt, is_gkal, kod_pu, kod_info, dat_prov, dat_provnext, dat_oblom," +
                      " dat_poch, dat_close, comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl, cnt_ls, dat_block, user_block," +
                      " month_calc, user_del, dat_del, dat_s, dat_po)" +
                      " values" +
                      " (" + strNzp_cnt +
                      ", 3, " + r["nzp_kvar"] + ", " + r["nzp_serv"] + ", 678, " + r["kod_ipu"] +
                      ", 0,2,3, cast('' as integer),cast('' as integer), cast('' as integer), " +
                      " cast('' as integer),cast('' as integer),20000 + " + r["kod_ipu"] +
                      " ,1," +
                      " (select nzp_cnt from " + finder.bank + "_kernel" + tableDelimiter +
                      "s_counts " +
                      " where nzp_serv = " + r["nzp_serv"] + ")" +
                      ",1 ,current,0,1, cast('' as integer) ,0," +
                      " cast('' as integer)," + valuesFromFile.finder.nzp_file +
                      ",cast('' as integer),cast('' as integer),cast('' as integer))";
                ret = ExecSQL(con_db, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                    valuesFromFile.err.Append("Ошибка записи в БД");
                }

#if PG
                    sql = "SELECT currval('" + seq + "')";
#else
                sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter +
                      "dual";
#endif

                int nzp_cnt1 = Convert.ToInt32(ExecScalar(con_db, sql, out ret, true));

                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadipu" +
                      " set nzp_cnt = " + nzp_cnt1 +
                      " where nzp_kvar = " + r["nzp_kvar"] + " and nzp_serv = " + r["nzp_serv"] +
                      " and kod_ipu =" + r["kod_ipu"] +
                      " and nzp_file = " + valuesFromFile.finder.nzp_file;
                ret = ExecSQL(con_db, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                    valuesFromFile.err.Append("Ошибка записи в БД");
                }

                #endregion
            }

            #endregion

            #region запись показаний

            sql = " insert into " + finder.bank + "_data" + tableDelimiter + "counters " +
                  " (nzp_cr, nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_prov, dat_provnext, dat_uchet, val_cnt, norma, is_actual, nzp_user," +
                  " dat_when, dat_close, cur_unl, nzp_wp, ist, dat_oblom, dat_poch, dat_del, user_del, nzp_counter, month_calc," +
                  " dat_s, dat_po, dat_block, user_block) " +
                  " values " +
                  " select 0,  nzp_kvar + ,  nzp_kvar  ,  nzp_serv , 678, " +
                  " kod_ipu , '','',  date_p ,  val_p , 0,1," + valuesFromFile.finder.nzp_user + "," +
                  " current, cast('' as integer) ,1,0,1, cast('' as integer) ,cast('' as integer) ,cast('' as integer) ," +
                  valuesFromFile.finder.nzp_file + ", nzp_cnt ,cast('' as integer)," +
                  " cast('' as integer) ,cast('' as integer) ,cast('' as integer), 20000 + kod_ipu " +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadipu" +
                  " where nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns DisassSaldo(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            #region Определяем месяц

            string date_rcl = "";

            sql =
                " select max(date_rcl) as date_rcl from " + Points.Pref + DBManager.sUploadAliasRest +
                "_onetimeloadsaldo" +
                " where nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            sql =
                " select min(date_rcl)  as date_rcl  from " + Points.Pref + DBManager.sUploadAliasRest +
                "_onetimeloadsaldo" +
                " where nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dt1 = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            //если год и месяц равны
            if (dt.Rows[0]["date_rcl"].ToString().Substring(3, 7) !=
                dt1.Rows[0]["date_rcl"].ToString().Substring(3, 7))
            {
                valuesFromFile.err.Append("Дата при загрузке перекидок должна быть одинаковой");
                MonitorLog.WriteLog("Дата при загрузке перекидок должна быть одинаковой",
                    MonitorLog.typelog.Error,
                    true);
                return ret;
            }
            else
            {
                string datet = dt1.Rows[0]["date_rcl"].ToString().Substring(0, 10);
                DateTime dtime =
                    new DateTime(Convert.ToInt32(datet.Substring(6, 4)),
                        Convert.ToInt32(datet.Substring(3, 2)), 1).AddMonths(-1);
                date_rcl = dtime.ToString("dd.MM.yyyy");
            }

            #endregion

            #region кладем в Charge_XX

            sql = " update " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) +
                  " set (sum_outsaldo, real_charge ) =" +
                  " (( select b.sum_insaldo, -(" + valuesFromFile.finder.bank + "_charge_" +
                  date_rcl.Substring(8, 2) + tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) + ".sum_outsaldo - (b.sum_insaldo) )" +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadsaldo b" +
                  " where " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) + ".nzp_kvar = b.nzp_kvar" +
                  " and " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) + ".nzp_supp = b.nzp_supp " +
                  " and " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) + ".nzp_serv = b.nzp_serv))" +
                  " where nzp_kvar in " +
                  " (select b.nzp_kvar from " + valuesFromFile.finder.bank + "_charge_" +
                  date_rcl.Substring(8, 2) + tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) + " b, " + Points.Pref + "_data" +
                  tableDelimiter + "_onetimeloadsaldo a " +
                  " where a.nzp_kvar = b.nzp_kvar" +
                  " and a.nzp_supp = b.nzp_supp" +
                  " and a.nzp_serv = b.nzp_serv" +
                  " and a.nzp_file = " + valuesFromFile.finder.nzp_file + ") ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            //добавляем новые строки для тех, кого не было в charge
            sql = " insert into  " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) +
                  " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, real_charge, sum_outsaldo," +
                  " c_calc, order_print)" +
                  " select nzp_kvar , nzp_kvar , nzp_serv , nzp_supp , 1," +
                  " -(sum_insaldo ),  sum_insaldo ,  1, 187" +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadsaldo" +
                  " where nzp_kvar not in " +
                  " (select b.nzp_kvar from " + valuesFromFile.finder.bank + "_charge_" +
                  date_rcl.Substring(8, 2) + tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) + " b, " + Points.Pref + "_data" +
                  tableDelimiter + "_onetimeloadsaldo a " +
                  " where a.nzp_kvar = b.nzp_kvar" +
                  " and a.nzp_supp = b.nzp_supp" +
                  " and a.nzp_serv = b.nzp_serv" +
                  " and a.nzp_file = " + valuesFromFile.finder.nzp_file + ")";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            #region кладем в перекидку

            sql = " insert into " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter + ":perekidka " +
                  " ( nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, date_rcl, tarif," +
                  " volum, sum_rcl, month_, comment, nzp_user, nzp_reestr) " +
                  " select  a.nzp_kvar , b.num_ls ,a.nzp_serv , a.nzp_supp , " +
                  " 1, a.date_rcl , '0', '0', -(b.sum_outsaldo-( a.sum_insaldo ))," +
                  date_rcl.Substring(3, 2) + ", 'поправка из разовой загрузки " +
                  valuesFromFile.finder.nzp_file + "', 1, -3" +
                  " from " + valuesFromFile.finder.bank + "_charge_" + date_rcl.Substring(8, 2) +
                  tableDelimiter +
                  " charge_" + date_rcl.Substring(3, 2) + " b,  " +
                  Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadsaldo a" +
                  " where b.num_ls =a.nzp_kvar and b.nzp_serv = a.nzp_serv and " +
                  " a.nzp_supp = b.nzp_supp " +
                  " and (b.sum_outsaldo-(a.sum_insaldo ))<0 " +
                  " group by 1,2,3,4, 6,9 ;";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            #region формируем пачку

            sql = "  insert into  " + Points.Pref + "_fin_" + date_rcl.Substring(8, 2) + tableDelimiter +
                  "pack" +
                  "(  pack_type, nzp_bank,  num_pack, dat_uchet, dat_pack, " +
                  " count_kv, sum_pack,  flag, dat_vvod, " +
                  "sum_rasp, sum_nrasp, nzp_rs, dat_inp,  file_name)" +
                  "select 10, 1,  '987654321', " + sCurDate + ", " + sCurDate + ", " +
                  " count(nzp_kvar), sum(sum_money), 21, " + sCurDate + ", " +
                  "'0', sum(sum_money), 1, " + sCurDate + ",  'разовая загрузка'" +
                  "from  " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadsaldo o" +
                  "where nzp_file = " + valuesFromFile.finder.nzp_file;
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            // номер пачки 
            int pack = GetSerialValue(con_db);

            sql = "insert into " + Points.Pref + "_fin_" + date_rcl.Substring(8, 2) + tableDelimiter +
                  "pack_ls" +
                  " nzp_pack,  num_ls, g_sum_ls,  dat_month, " +
                  "kod_sum, nzp_supp, paysource,  dat_vvod, dat_uchet,  info_num, inbasket, alg," +
                  "unl,  nzp_user, incase, nzp_rs)" +
                  "select  " + pack + ",  o.nzp_kvar, '0',  o.date_r," +
                  "33, o.nzp_supp, 1,  current , o.date_o,  1, 1, '0'," +
                  "0,  1, 0, 1" +
                  "from  " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadsaldo o " +
                  "where nzp_file = " + valuesFromFile.finder.nzp_file +
                  "group by  o.nzp_supp, o.nzp_kvar, o.date_r, o.date_o ";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
                valuesFromFile.err.Append("Ошибка записи в БД");
            }

            #endregion

            return ret;
        }

        private Returns DisassNedop(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql;

            #region Проверяем все ли ЛС есть в базе

            sql =
                " SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimenedop" +
                " WHERE NOT EXISTS " +
                " (SELECT k.nzp_kvar FROM " + finder.bank + sDataAliasRest + "kvar k" +
                " WHERE k.nzp_kvar = " + Points.Pref + DBManager.sUploadAliasRest + "_onetimenedop.nzp_kvar)" +
                " AND  nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dtkvar = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dtkvar.Rows.Count > 0)
            {
                valuesFromFile.err.Append("Имеются ЛС, которых нет в базе, данные не загружены");
                return new Returns(false, "Имеются ЛС, которых нет в базе", -1);
            }

            #endregion

            #region Проверяем все ли поставщики есть в базе

            sql =
                " SELECT * FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimenedop" +
                " WHERE NOT EXISTS " +
                " (SELECT k.nzp_supp FROM " + finder.bank + sKernelAliasRest + "supplier k" +
                " WHERE k.nzp_supp = " + Points.Pref + DBManager.sUploadAliasRest + "_onetimenedop.nzp_supp)" +
                " AND  nzp_file = " + valuesFromFile.finder.nzp_file;
            DataTable dtsupp = ClassDBUtils.OpenSQL(sql, con_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dtsupp.Rows.Count > 0)
            {
                valuesFromFile.err.Append("Имеются договоры/поставщики, которых нет в базе, данные не загружены");
                return new Returns(false, "Имеются договоры/поставщики, которых нет в базе", -1);
            }

            #endregion

            #region перезапись недопоставок

            sql =
                " INSERT INTO " + finder.bank + sDataAliasRest + "nedop_kvar" +
                " (nzp_kvar, nzp_serv, nzp_supp, dat_s, dat_po, is_actual, nzp_user," +
                " dat_when, nzp_kind, cur_unl, month_calc, user_del, tn)" +
                " SELECT o.nzp_kvar, o.nzp_serv, o.nzp_supp, o.dat_s," +
                " o.dat_po, 1, " + finder.nzp_user + "," + sCurDate + ", min(k.nzp_kind)," +
                " 1,  " + " '01." + Points.GetCalcMonth(new CalcMonthParams { pref = finder.bank }).month_.ToString("00") + "." +
                Points.GetCalcMonth(new CalcMonthParams { pref = finder.bank }).year_ + "'" + DBManager.sConvToDate + ", o.nzp_file, o.percent" +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "_onetimenedop o, " +
                Points.Pref + DBManager.sDataAliasRest + "upg_s_kind_nedop k " +
                " WHERE  o.nzp_file =" + finder.nzp_file + 
                " AND k.kod_kind = 1 and k.nzp_kind > 2001 AND k.nzp_parent = o.nzp_serv " +
                " GROUP BY 1,2,3,4,5,12,13";
            ret = ExecSQL(con_db, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи файла ", MonitorLog.typelog.Error, true);
            }

            #endregion

            return ret;
        }

        #endregion





        /// <summary>
        /// Создает таблицы для разовой загрузки 
        /// </summary>
        /// <param name="con_db"></param>
        /// <returns></returns>
        private Returns CreateTableForOneTimeLoader(IDbConnection con_db)
        {
            var ret = new Returns();
            string sql;

#if PG
                    sql = " SET search_path TO  '" + Points.Pref + "_upload" + "'";
#else
            sql = "DATABASE " + Points.Pref + "_upload";
#endif
            ret = ExecSQL(con_db, sql, false);

            #region _onetimeloadsaldo

            sql = "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadsaldo(" +
                  " nzp_kvar INTEGER," +
                  " sum_insaldo " + sDecimalType + "(12,2)," +
                  " nzp_serv INTEGER," +
                  " nzp_supp INTEGER," +
                  " date_rcl DATE," +
                  " nzp_file INTEGER, " +
                  " type_o INTEGER)";
            ret = ExecSQL(con_db, sql, false);
            sql = "CREATE INDEX inx_onetimesaldo ON " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadsaldo(nzp_kvar, nzp_serv)";
            ret = ExecSQL(con_db, sql, false);
            sql = "CREATE INDEX inx_onetimesaldo_nzp_kvar ON " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadsaldo(nzp_kvar)";
            ret = ExecSQL(con_db, sql, false);
            sql = "CREATE INDEX inx_onetimesaldo_nzp_serv ON " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadsaldo(nzp_serv)";
            ret = ExecSQL(con_db, sql, false);
            sql = "CREATE INDEX inx_onetimesaldo_nzp_supp ON " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeloadsaldo(nzp_supp)";
            ret = ExecSQL(con_db, sql, false);

            #endregion

            #region _onetimeloadipu

            sql =
                "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadipu(" +
                " nzp_kvar INTEGER," +
                " kod_ipu CHAR(20)," +
                " date_p DATE," +
                " val_p " + sDecimalType + "(12,2)," +
                " nzp_serv INTEGER," +
                " nzp_file INTEGER," +
                " nzp_cnt INTEGER)";
            ret = ExecSQL(con_db, sql, false);

            #endregion

            #region _onetimeloadls

            sql =
                "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadls(" +
                " nzp_kvar INTEGER," +
                " val_prm CHAR(100)," +
                " date_s DATE," +
                " date_po DATE," +
                " nzp_prm INTEGER, " +
                " nzp_file INTEGER)";
            ret = ExecSQL(con_db, sql, false);

            #endregion

            #region _onetimeloadlsold

            sql =
                "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeloadlsold(" +
                " num_ls INTEGER," +
                " val_prm CHAR(100)," +
                " date_s DATE," +
                " date_po DATE," +
                " nzp_prm INTEGER, " +
                " nzp_file INTEGER," +
                " nzp_kvar INTEGER)";
            ret = ExecSQL(con_db, sql, false);

            #endregion

            #region _onetimeper

            sql =
                "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeper(" +
                " nzp_kvar INTEGER," +
                " nzp_serv INTEGER," +
                " nzp_supp INTEGER, " +
                " date_rcl DATE," +
                " sum_insaldo " + sDecimalType + "(12,2)," +
                " comment CHAR(100)," +
                " nzp_file INTEGER)";
            ret = ExecSQL(con_db, sql, false);
            sql = "CREATE INDEX inx_onetimeper ON " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeper(nzp_kvar, nzp_serv)";
            ret = ExecSQL(con_db, sql, false);

            #endregion

            #region _onetimenedop

            sql =
                "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimenedop(" +
                " nzp_kvar INTEGER," +
                " nzp_serv INTEGER," +
                " nzp_supp INTEGER, " +
                " dat_s " + DBManager.sDateTimeType + "," +
                " dat_po " + DBManager.sDateTimeType + "," +
                " percent " + sDecimalType + "(12,3)," +
                " nzp_file INTEGER)";
            ret = ExecSQL(con_db, sql, false);

            #endregion

            #region _onetimeoplata

            sql =
                "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimeoplata(" +
                " date_r DATE," +
                " date_o DATE," +
                " num_ls INTEGER," +
                " fio CHAR(100)," +
                " adres CHAR(100)," +
                " nzp_serv INTEGER," +
                " supp_name CHAR(30)," +
                " sum_money " + sDecimalType + "(12,2)," +
                " num_pack INTEGER," +
                " date_pack DATE," +
                " type_pack INTEGER," +
                " nzp_supp INTEGER," +
                " nzp_kvar INTEGER," +
                " nzp_file INTEGER)";
            ret = ExecSQL(con_db, sql, false);
            sql = "CREATE INDEX inx_onetop ON " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimeoplata(nzp_kvar, nzp_serv, nzp_supp)";
            ret = ExecSQL(con_db, sql, false);

            #endregion

            #region _onetimekart

            sql =
                "CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart(" +
                " ls INTEGER," +
                " nomer INTEGER," +
                " dat_ub DATE," +
                " fio CHAR(120)," +
                " dat_rogd DATE," +
                " dat_pr DATE," +
                " nzp_file INTEGER," +
                " nzp_kvar INTEGER," +
                " nzp_gil INTEGER)";
            ret = ExecSQL(con_db, sql, false);
            sql = "CREATE INDEX inx_onetkart_ls ON " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart(ls)";
            ret = ExecSQL(con_db, sql, false);
            sql = "CREATE INDEX inx_onetkart_nzp_kvar ON " + Points.Pref + DBManager.sUploadAliasRest +
                  "_onetimekart(nzp_kvar)";
            ret = ExecSQL(con_db, sql, false);

            #endregion

            #region _onetimerrkc

            sql =
                " CREATE TABLE " + Points.Pref + DBManager.sUploadAliasRest + "_onetimerrkc(" +
                " ls CHAR(20)," +
                " town CHAR(30)," +
                " rajon CHAR(30)," +
                " ulica CHAR(40)," +
                " dom CHAR(10)," +
                " korp CHAR(3)," +
                " nkvar CHAR(10)," +
                " nkvar_n CHAR(3)," +
                " fio CHAR(120)," +
                " kol_gil INTEGER," +
                " ob_pl " + sDecimalType + "(12,2)," +
                " otap_pl " + sDecimalType + "(12,2)," +
                " etazh INTEGER," +
                " tel CHAR(20)," +
                " ind CHAR(10)," +
                " supp_id INTEGER," +
                " serv1 INTEGER," +
                " sum_in1 " + sDecimalType + "(12,2)," +
                " serv2 INTEGER," +
                " sum_in2 " + sDecimalType + "(12,2)," +
                " serv3 INTEGER," +
                " sum_in3 " + sDecimalType + "(12,2)," +
                " dom_id INTEGER," +
                " nzp_file INTEGER)";
            ret = ExecSQL(con_db, sql, false);
            //sql = "CREATE INDEX inx_onetimerrkc_ls ON " + Points.Pref + DBManager.sUploadAliasRest + "_onetimekart(ls)";
            //ret = ExecSQL(con_db, sql, false);
            //sql = "CREATE INDEX inx_onetimerrkc_nzp_kvar ON " + Points.Pref + DBManager.sUploadAliasRest +
            //      "_onetimekart(nzp_kvar)";
            //ret = ExecSQL(con_db, sql, false);

            #endregion

            return ret;
        }

    }
}
