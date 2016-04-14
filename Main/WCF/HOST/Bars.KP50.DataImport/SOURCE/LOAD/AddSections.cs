using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bars.KP50.Utils;
using Newtonsoft.Json.Serialization;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class AddSections : DbAdminClient
    {
        public AddSections(DbValuesFromFile values)
        {
            this.valuesFromFile = values;
        }

        private delegate void sectionDelegate(DbValuesFromFile valuesFromFile);

        private DbValuesFromFile valuesFromFile;

        /// <summary>
        /// Загрузка секций 
        ///  (формирует sql-запросы для вставки и записывает их в таблицу file_sql)
        /// </summary>SectionsToDB
        /// <param name="con_db"></param>
        /// <param name="valuesFromFile"></param>
        /// <returns></returns>`
        public Returns Run(IDbConnection con_db)
        {
            var ret = new Returns();

            #region Запросы
            try
            {

                //создаем таблички, если их нет
                using (var createTableForLoader = new CreateTablesForLoader())
                {
                    createTableForLoader.Run(con_db);
                }

                //очищаем табличку запросов file_sql
#if PG

                string sql = " SET search_path TO '" + Points.Pref + "_upload'";
#else                    
                string sql = "database " + Points.Pref + "_upload";
#endif

                ret = ExecSQL(con_db, sql, true);
                sql =
                    " truncate " + Points.Pref + DBManager.sUploadAliasRest + "file_sql ";
                ret = ExecSQL(con_db, sql, true);

                sql =
                    sUpdStat + " " + Points.Pref + DBManager.sUploadAliasRest + "file_sql";
                ret = ExecSQL(con_db, sql, true);

                //заполняем file_section - те секции, которые мы разрешаем грузить
                SectionsToDB(con_db, valuesFromFile.finder);

                Dictionary<int, sectionDelegate> sectionMethods = FillSectionsMethods(valuesFromFile.finder.format_name);

                valuesFromFile.Pvers = "";
                int currSection = -1;
               
                #region Собрать запросы

                int i = 0;
                int jj = 0;
                string sql_str = "";
                List<string> listDbValuesFromFile = new List<string>();
                int count = 0;
                //цикл по строкам
                foreach (string str in valuesFromFile.fileStrings)
                {
                    i++;
                    jj++;
                    //защита от пустых строк(пустые строки для сохранения нумерации)
                    if (str.Trim() == "")
                    {
                        var addSections1 = new AddSections(valuesFromFile);
                        addSections1.InsertIntoListFileSql(con_db, sql_str);
                        sql_str = "";
                        count = 0;
                        continue;
                    }

                    ret = Utils.InitReturns();
                    valuesFromFile.sql = "";


                    //массив значений строки
                    valuesFromFile.vals = str.Split(new char[] { '|' }, StringSplitOptions.None);
                    Array.ForEach(valuesFromFile.vals, x => x = x.Trim());

          
                    //пропуск пустой строчки
                    if (valuesFromFile.vals.Length == 0)
                    {
                        continue;
                    }

                    currSection = valuesFromFile.vals[0].ToInt();                   

                    if (currSection == 0)
                    {
                        throw new Exception("Ошибка загрузки файла: Не заполнено поле 'Тип строки'");
                    }
                    //номер строки и секции в файле
                   
                    //valuesFromFile.rowNumber = Environment.NewLine + " (строка " +
                    //                           (Array.IndexOf(valuesFromFile.fileStrings, str) + 1).ToString() +
                    //                           ", секция " + currSection + ") ";
                    valuesFromFile.rowNumber = Environment.NewLine + " (строка " +
                                               jj.ToString()  +", секция " + currSection + ") ";
                    ret.text += valuesFromFile.rowNumber;
                  
                    valuesFromFile.rowNumber1 = jj;

                    if (valuesFromFile.fileStrings.Length / 25 != 0 && valuesFromFile.rowNumber1 % (valuesFromFile.fileStrings.Length / 25) == 0)
                    {
                        string sqlPercent =
                            "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set percent = " +
                            ((decimal)valuesFromFile.rowNumber1 / (decimal)valuesFromFile.fileStrings.Length) / 3 +
                            " where nzp_file = " + valuesFromFile.finder.nzp_file;
                        ret = ExecSQL(con_db, sqlPercent, true);
                    }

                    var addSections = new AddSections(valuesFromFile);

                 
                    if (valuesFromFile.finder.selectedSections[currSection])
                    {
                        sectionMethods[currSection](valuesFromFile);
                    }

                    sql_str  += GetInsertIntoFileSql(valuesFromFile);
                    count++;
                    //Вставляем в file_sql по 1000 строк
                    if (count == 1000 || i == valuesFromFile.fileStrings.Count() - 1)
                    {
                        addSections.InsertIntoListFileSql(con_db, sql_str);
                        sql_str = "";
                        count = 0;
                    }
             
                

                    //добавление sql-запросов в таблицу file_sql
                    //addSections.InsertIntoFileSql(con_db, valuesFromFile);
                   
                }
                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddSections : " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                valuesFromFile.err.Append("Ошибка выполнения процедуры AddSections : " + ex.Message + "\n" + ex.StackTrace);
                //return ret;
            }
            #endregion

            return ret;
        }

        /// <summary>
        /// Ф-ция заполнения таблицы file_section - те секции, которые мы разрешаем грузить
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        private void SectionsToDB(IDbConnection conn_db, FilesImported finder)
        {
            try
            {
                foreach (var item in finder.selectedSections)
                {
                    string sql =
                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "file_section " +
                        " ( num_sec, sec_name, nzp_file, is_need_load)" +
                        " values( " + item.Key + ", null, " + finder.nzp_file + ", " + Convert.ToInt32(item.Value) + " )";
                    ExecSQL(conn_db, sql, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функции SectionsToDB" + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
        }


        /// <summary>
        /// Загрузка 1 секции "Заголовок файла"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection01_FileHead(DbValuesFromFile valuesFromFile)
        {

            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 1;

            if (valuesFromFile.vals.Length < 13)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + " Неправильный формат файла загрузки: Заголовок файла, количество полей =" + valuesFromFile.vals.Length + " вместо 13 ");
                return;
            }

            valuesFromFile.sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_head" +
                " (nzp_file, org_name, branch_name, inn, kpp, file_no, file_date, sender_phone, sender_fio, calc_date, row_number) " +
                " VALUES (" + valuesFromFile.finder.nzp_file + ", ";
            int i = 1;

            //2. Версия формата
            string vers = CheckType.CheckText2(valuesFromFile, i, true, 10, ref ret, "Версия формата");
            valuesFromFile.Pvers = vers;
            valuesFromFile.finder.format_name = vers;
            valuesFromFile.Pvers = "1.2";
            i++;

            //3. Тип файла
            CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, "Тип файла");
            i++;

            //4. Наименование организации-отправителя 
            string org_name = CheckType.CheckText2(valuesFromFile, i, true, 40, ref ret, "Наименование организации-отправителя");
            valuesFromFile.sql += org_name + ", ";
            i++;

            //5. Подразделение организации-отправителя
            string branch_name = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Подразделение организации-отправителя");
            if (branch_name == "null")
            {
                branch_name = " '-' ";
            }

            valuesFromFile.sql += branch_name + ", ";
            i++;

            //6. ИНН
            string inn = CheckType.CheckText2(valuesFromFile, i, false, 12, ref ret, "ИНН" );
            if (inn == "null")
            {
                inn = " '0' ";

            }
            valuesFromFile.sql += inn + ", ";
            i++;

            //7. КПП
            string kpp = CheckType.CheckText2(valuesFromFile, i, false, 9, ref ret, "КПП");
            if (kpp == "null")
            {
                kpp = " '0' ";
            }
            valuesFromFile.sql += kpp + ", ";
            i++;

            //8. № файла
            string num = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "№ файла");
            valuesFromFile.sql += num + ", ";
            i++;

            //9. Дата файла
            string datf = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата файла");
            valuesFromFile.sql += datf + ", ";
            i++;

            //10. Телефон отправителя
            string tel = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Телефон отправителя");
            valuesFromFile.sql += tel + ", ";
            i++;

            //11. ФИО отправителя
            string fio = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "ФИО отправителя");
            valuesFromFile.sql += fio + ", ";
            i++;

            //12. Месяц и год начислений
            string month = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Месяц и год начислений");
            valuesFromFile.sql += month + ", ";
            i++;

            //13. Количество записей в файле
            string rows = CheckType.CheckInt2(valuesFromFile, i, true, 0, null, ref ret, "Количество записей в файле");
            valuesFromFile.sql += rows + "); ";
            i++;

        }


        /// <summary>
        ///  Загрузка 2 секции "Управляющие компании"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection02_FileArea(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 2;

            if (valuesFromFile.vals.Length < 11)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + " Неправильный формат файла загрузки: УК, количество полей = " + valuesFromFile.vals.Length + " вместо 11 ");
                return;
            }

            #region Загрузка 2 секции
            valuesFromFile.sql =
                        " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_area " +
                        "( id, nzp_file, area, jur_address, fact_address, inn, kpp, rs, bank, bik, ks, nzp_area) " +
                        " VALUES (";
            int i = 1;

            //2. Уникальный код УК
            string id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Уникальный код УК");
            valuesFromFile.sql += id + ", ";
            i++;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            //3. Наименование УК
            string area = CheckType.CheckText2(valuesFromFile, i, true, 60, ref ret, "Наименование УК");
            valuesFromFile.sql += area + ", ";
            i++;

            //4. Юридический адрес
            string jur_address = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Юридический адрес");
            valuesFromFile.sql += jur_address + ", ";
            i++;

            //5. Фактический адрес
            string fact_address = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Фактический адрес");
            valuesFromFile.sql += fact_address + ", ";
            i++;

            //6. ИНН
            string inn = CheckType.CheckText2(valuesFromFile, i, true, 12, ref ret, "ИНН");
            valuesFromFile.sql += inn + ", ";
            i++;

            //7. КПП
            string kpp = CheckType.CheckText2(valuesFromFile, i, true, 9, ref ret, "КПП");
            valuesFromFile.sql += kpp + ", ";
            i++;

            //8. Расчетный счет
            string rch = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Расчетный счет");
            valuesFromFile.sql += rch + ", ";
            i++;

            //9. Банк
            string bank = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Банк");
            valuesFromFile.sql += bank + ", ";
            i++;

            //10. БИК банка
            string bik = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " БИК банка");
            valuesFromFile.sql += bik + ", ";
            i++;

            //11. Корреспондентский счет
            string kch = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Корреспондентский счет");
            valuesFromFile.sql += kch + ", ";
            i++;

            //добавляем nzp_area
            valuesFromFile.sql += "null);";

            #endregion Загрузка 2 секции
        }

        public void AddSection02_29_FileUrlic_v_132(DbValuesFromFile valuesFromFile)
        {
            int numSection = valuesFromFile.vals[0].ToInt();
            AddSection02_29_FileUrlic_v_132(valuesFromFile, numSection);
        }

        /// <summary>
        ///  Формат 1.3.2 Загрузка 2 секции "Юридические лица"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        private void AddSection02_29_FileUrlic_v_132(DbValuesFromFile valuesFromFile, int num_section)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 2;

            if (valuesFromFile.vals.Length < (num_section == 2 ? 24 : 17))
            {
                valuesFromFile.err.Append(string.Format("{0}Неправильный формат файла загрузки: {1}, количество полей = {2} вместо 24/17",
                    valuesFromFile.rowNumber, (num_section == 2 ? "Юридические лица" : "Банки"), valuesFromFile.vals.Length));
                return;
            }


            #region Заготовка инсерта

            valuesFromFile.sql =
                        " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                        " file_urlic (nzp_file, urlic_id, urlic_name, urlic_name_s, " +
                        " jur_address, fact_address, inn, kpp, tel_chief, tel_b, chief_name,chief_post, " +
                        " b_name, okonh1, okonh2, okpo, post_and_name," +
                        " is_area, is_supp, is_arendator, is_rc, is_rso, is_agent, is_subabonent, is_bank, nzp_payer) " +
                        " VALUES(";


            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем, nzp_file

            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код ЮЛ
            string supp_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный код ЮЛ ");
            if (supp_id == "0")
            {
                supp_id = "1000000";
            }

            valuesFromFile.sql += supp_id + ", ";
            i++;

            #endregion 2. Уникальный код ЮЛ

            #region 3. Наименование ЮЛ

            string supp_name = CheckType.CheckText2(valuesFromFile, i, true, 100, ref ret, " Наименование ЮЛ");
            valuesFromFile.sql += supp_name + ", ";
            i++;

            #endregion 3. Наименование ЮЛ

            #region 4. Сокращенное наименование ЮЛ

            string urlic_name_s = CheckType.CheckText2(valuesFromFile, i, true, 25, ref ret, " Сокращенное наименование  ЮЛ");
            valuesFromFile.sql += urlic_name_s + ", ";
            i++;

            #endregion 4. Сокращенное наименование  ЮЛ

            #region 5. Юридический адрес

            string jur_address = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Юридический адрес");
            valuesFromFile.sql += jur_address + ", ";
            i++;

            #endregion 5. Юридический адрес

            #region 6. Фактический адрес

            string fact_address = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Фактический адрес");
            valuesFromFile.sql += fact_address + ", ";
            i++;

            #endregion 6. Фактический адрес

            #region 7. ИНН

            string inn = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, " ИНН");
            valuesFromFile.sql += inn + ", ";
            i++;

            #endregion 7. ИНН

            #region 8. КПП

            string kpp = CheckType.CheckText2(valuesFromFile, i, true, 9, ref ret, " КПП");
            valuesFromFile.sql += kpp + ", ";
            i++;

            #endregion 8. КПП

            #region 9. Телефон руководителя

            string tel_chief = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Телефон руководителя");
            valuesFromFile.sql += tel_chief + ", ";
            i++;

            #endregion

            #region 10. Телефон бухгалтерии

            string tel_b = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Телефон бухгалтерии");
            valuesFromFile.sql += tel_b + ", ";
            i++;

            #endregion

            #region 11. ФИО руководителя

            string chief_name = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " ФИО руководителя");
            valuesFromFile.sql += chief_name + ", ";
            i++;

            #endregion

            #region 12. Должность руководителя

            string chief_post = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Должность руководителя");
            valuesFromFile.sql += chief_post + ", ";
            i++;

            #endregion

            #region 13. ФИО бухгалтера

            string b_name = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " ФИО бухгалтера");
            valuesFromFile.sql += b_name + ", ";
            i++;

            #endregion

            #region 14. ОКОНХ1

            string okonh1 = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКОНХ1");
            valuesFromFile.sql += okonh1 + ", ";
            i++;

            #endregion

            #region 15. ОКОНХ2

            string okonh2 = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКОНХ2");
            valuesFromFile.sql += okonh2 + ", ";
            i++;

            #endregion

            #region 16. ОКПО

            string okpo = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКПО");
            valuesFromFile.sql += okpo + ", ";
            i++;

            #endregion

            #region 17. Должность + ФИО в Р.п.

            string post_and_name = CheckType.CheckText2(valuesFromFile, i, false, 200, ref ret, " Должность + ФИО в Р.п.");
            valuesFromFile.sql += post_and_name + ", ";
            i++;

            #endregion

            #region 18. Признак того, что предприятие является УК

            string is_area;
            if (num_section == 2)
            {
                is_area = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является УК ");

            }
            else is_area = "0";
            valuesFromFile.sql += is_area + ", ";
            i++;

            #endregion

            #region 19. Признак того, что предприятие является поставщиком

            string is_supp;
            if (num_section == 2)
            {
                is_supp = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является поставщиком ");
            }
            else is_supp = "0";
            valuesFromFile.sql += is_supp + ", ";
            i++;

            #endregion

            #region 20. Признак является того, что предприятие арендатором/собственником помещений

            string is_arendator;
            if (num_section == 2)
            {
                is_arendator = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является арендатором/собственником помещений ");

            }
            else is_arendator = "0";
            valuesFromFile.sql += is_arendator + ", ";
            i++;

            #endregion

            #region 21. Признак является того, что предприятие РЦ

            string is_rc;
            if (num_section == 2)
            {
                is_rc = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является РЦ ");
            }
            else is_rc = "0";
            valuesFromFile.sql += is_rc + ", ";
            i++;

            #endregion

            #region 22. Признак является того, что предприятие РСО

            string is_rso;
            if (num_section == 2)
            {
                is_rso = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является РСО ");
            }
            else is_rso = "0";
            valuesFromFile.sql += is_rso + ", ";
            i++;

            #endregion

            #region 23. Признак является того, что предприятие агентом

            string is_agent;
            if (num_section == 2)
            {
                is_agent = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является платежным агентом ");
            }
            else is_agent = "0";
            valuesFromFile.sql += is_agent + ", ";
            i++;

            #endregion

            #region 24. Признак является того, что предприятие является субабонентом

            string is_subabonent;
            if (num_section == 2)
            {
                is_subabonent = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является субабонентом ");
            }
            else is_subabonent = "0";
            valuesFromFile.sql += is_subabonent + ", ";
            i++;

            #endregion

            #region 25. Признак является того, что предприятие является банком

            string is_bank;
            is_bank = (num_section == 2) ? "0" : "1";
            valuesFromFile.sql += is_bank + ", ";
            i++;

            #endregion

            #region Конец запроса

            valuesFromFile.sql += " null);";
            i++;

            #endregion
        }

        public void AddSection02_29_FileUrlic_v_138(DbValuesFromFile valuesFromFile)
        {
            int numSection = valuesFromFile.vals[0].ToInt();
            AddSection02_29_FileUrlic_v_138(valuesFromFile, numSection);
        }

        /// <summary>
        ///  Формат 1.3.2 Загрузка 2 секции "Юридические лица"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        private void AddSection02_29_FileUrlic_v_138(DbValuesFromFile valuesFromFile, int num_section)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 2;

            if (valuesFromFile.vals.Length < (num_section == 2 ? 24 : 17))
            {
                valuesFromFile.err.Append(string.Format("{0}Неправильный формат файла загрузки: {1}, количество полей = {2} вместо 24/17",
                    valuesFromFile.rowNumber, (num_section == 2 ? "Юридические лица" : "Банки"), valuesFromFile.vals.Length));
                return;
            }


            #region Заготовка инсерта

            valuesFromFile.sql =
                        " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                        " file_urlic (nzp_file, urlic_id, urlic_name, urlic_name_s, " +
                        " jur_address, fact_address, inn, kpp, tel_chief, tel_b, chief_name,chief_post, " +
                        " b_name, okonh1, okonh2, okpo, post_and_name," +
                        " is_area, is_supp, is_arendator, is_rc, is_rso, is_agent, is_subabonent, is_bank, nzp_payer) " +
                        " VALUES(";


            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем, nzp_file

            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код ЮЛ
            string supp_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный код ЮЛ ");
            valuesFromFile.sql += supp_id + ", ";
            i++;

            #endregion 2. Уникальный код ЮЛ

            #region 3. Наименование ЮЛ

            string supp_name = CheckType.CheckText2(valuesFromFile, i, true, 100, ref ret, " Наименование ЮЛ");
            valuesFromFile.sql += supp_name + ", ";
            i++;

            #endregion 3. Наименование ЮЛ

            #region 4. Сокращенное наименование ЮЛ

            string urlic_name_s = CheckType.CheckText2(valuesFromFile, i, true, 25, ref ret, " Сокращенное наименование  ЮЛ");
            valuesFromFile.sql += urlic_name_s + ", ";
            i++;

            #endregion 4. Сокращенное наименование  ЮЛ

            #region 5. Юридический адрес

            string jur_address = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Юридический адрес");
            valuesFromFile.sql += jur_address + ", ";
            i++;

            #endregion 5. Юридический адрес

            #region 6. Фактический адрес

            string fact_address = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Фактический адрес");
            valuesFromFile.sql += fact_address + ", ";
            i++;

            #endregion 6. Фактический адрес

            #region 7. ИНН

            string inn = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, " ИНН");
            valuesFromFile.sql += inn + ", ";
            i++;

            #endregion 7. ИНН

            #region 8. КПП

            string kpp = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, " КПП");
            valuesFromFile.sql += kpp + ", ";
            i++;

            #endregion 8. КПП

            #region 9. Телефон руководителя

            string tel_chief = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Телефон руководителя");
            valuesFromFile.sql += tel_chief + ", ";
            i++;

            #endregion

            #region 10. Телефон бухгалтерии

            string tel_b = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Телефон бухгалтерии");
            valuesFromFile.sql += tel_b + ", ";
            i++;

            #endregion

            #region 11. ФИО руководителя

            string chief_name = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " ФИО руководителя");
            valuesFromFile.sql += chief_name + ", ";
            i++;

            #endregion

            #region 12. Должность руководителя

            string chief_post = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Должность руководителя");
            valuesFromFile.sql += chief_post + ", ";
            i++;

            #endregion

            #region 13. ФИО бухгалтера

            string b_name = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " ФИО бухгалтера");
            valuesFromFile.sql += b_name + ", ";
            i++;

            #endregion

            #region 14. ОКОНХ1

            string okonh1 = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКОНХ1");
            valuesFromFile.sql += okonh1 + ", ";
            i++;

            #endregion

            #region 15. ОКОНХ2

            string okonh2 = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКОНХ2");
            valuesFromFile.sql += okonh2 + ", ";
            i++;

            #endregion

            #region 16. ОКПО

            string okpo = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКПО");
            valuesFromFile.sql += okpo + ", ";
            i++;

            #endregion

            #region 17. Должность + ФИО в Р.п.

            string post_and_name = CheckType.CheckText2(valuesFromFile, i, false, 200, ref ret, " Должность + ФИО в Р.п.");
            valuesFromFile.sql += post_and_name + ", ";
            i++;

            #endregion

            #region 18. Признак того, что предприятие является УК

            string is_area;
            if (num_section == 2)
            {
                is_area = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является УК ");

            }
            else is_area = "0";
            valuesFromFile.sql += is_area + ", ";
            i++;

            #endregion

            #region 19. Признак того, что предприятие является поставщиком

            string is_supp;
            if (num_section == 2)
            {
                is_supp = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является поставщиком ");
            }
            else is_supp = "0";
            valuesFromFile.sql += is_supp + ", ";
            i++;

            #endregion

            #region 20. Признак является того, что предприятие арендатором/собственником помещений

            string is_arendator;
            if (num_section == 2)
            {
                is_arendator = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является арендатором/собственником помещений ");

            }
            else is_arendator = "0";
            valuesFromFile.sql += is_arendator + ", ";
            i++;

            #endregion

            #region 21. Признак является того, что предприятие РЦ

            string is_rc;
            if (num_section == 2)
            {
                is_rc = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является РЦ ");
            }
            else is_rc = "0";
            valuesFromFile.sql += is_rc + ", ";
            i++;

            #endregion

            #region 22. Признак является того, что предприятие РСО

            string is_rso;
            if (num_section == 2)
            {
                is_rso = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является РСО ");
            }
            else is_rso = "0";
            valuesFromFile.sql += is_rso + ", ";
            i++;

            #endregion

            #region 23. Признак является того, что предприятие агентом

            string is_agent;
            if (num_section == 2)
            {
                is_agent = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является платежным агентом ");
            }
            else is_agent = "0";
            valuesFromFile.sql += is_agent + ", ";
            i++;

            #endregion

            #region 24. Признак является того, что предприятие является субабонентом

            string is_subabonent;
            if (num_section == 2)
            {
                is_subabonent = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, " Признак того, что предприятие является субабонентом ");
            }
            else is_subabonent = "0";
            valuesFromFile.sql += is_subabonent + ", ";
            i++;

            #endregion

            #region 25. Признак является того, что предприятие является банком

            string is_bank;
            is_bank = (num_section == 2) ? "0" : "1";
            valuesFromFile.sql += is_bank + ", ";
            i++;

            #endregion

            #region Конец запроса

            valuesFromFile.sql += " null);";
            i++;

            #endregion
        }

        /// <summary>
        ///  Загрузка 3 секции "Информация о домах"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection03_FileDom(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 3;

            if (valuesFromFile.vals.Length < 19)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Дома, количество полей = " + valuesFromFile.vals.Length + " вместо 19");
                return;
            }

            #region Загрузка 3 секции
            valuesFromFile.sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                "( id, local_id, nzp_file, ukds, town, rajon, ulica, ndom, nkor, area_id, cat_blago, etazh, build_year, total_square, mop_square, useful_square,  " +
                " mo_id, params, ls_row_number, odpu_row_number, nzp_ul, nzp_dom, comment ";
            if (valuesFromFile.finder.format_name.Trim() == "'1.2.2'")
                valuesFromFile.sql += ", kod_kladr) ";
            else valuesFromFile.sql += ")";
            valuesFromFile.sql += " VALUES ( ";
            int i = 1;

            //2. УКДС
            string ukds = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "УКДС");
            i++;

            //3. Уникальный код дома в системе отправителя

            string local_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код дома в системе отправителя");

            string id = "-1";

            i++;

            valuesFromFile.sql += id + ", " + local_id + ", " + valuesFromFile.finder.nzp_file + ", " + ukds + ", ";


            //4. Город/район
            string town = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Город/район");

            valuesFromFile.sql += town + ", ";
            i++;


            //5. Село/деревня
            string rajon = CheckType.CheckText2(valuesFromFile, i, false, 50, ref ret, "Село/деревня");
            valuesFromFile.sql += rajon + ", ";
            i++;


            //6. Наименование улицы
            string ul = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Наименование улицы");
            valuesFromFile.sql += ul + ", ";
            i++;


            //7. Дом
            string dom = CheckType.CheckText2(valuesFromFile, i, false, 10, ref ret, "Дом");
            valuesFromFile.sql += dom + ", ";
            i++;


            //8. Корпус
            string kor = CheckType.CheckText2(valuesFromFile, i, false, 10, ref ret, "Корпус");
            valuesFromFile.sql += kor + ", ";
            i++;


            //9. Код УК         
            string uk = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " Код УК");
            valuesFromFile.sql += uk + ", ";
            i++;


            //10. Категория благоустроенности (значение из справочника)
            string cat = CheckType.CheckText2(valuesFromFile, i, false, 30, ref ret, "Категория благоустроенности (значение из справочника)");
            valuesFromFile.sql += cat + ", ";
            i++;


            //11. Этажность
            string etag = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " Этажность");
            if (!ret.result)
            {
                etag = "-1";
            }
            valuesFromFile.sql += etag + ", ";
            i++;


            //12. Год постройки
            //string y = CheckType.CheckDateTime(vals[i], true, ref ret);
            string pval;
            if (valuesFromFile.vals[i].Length == 4) { pval = "01.01." + valuesFromFile.vals[i]; } else { pval = valuesFromFile.vals[i]; }
            string y = CheckType.CheckDateTime(pval, false, ref ret);
            if (!ret.result)
            {
                y = "'01.01.1900'";
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Год постройки");
            }
            valuesFromFile.sql += y + ", ";
            i++;


            //13. Общая площадь - необязат
            string tot_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (ret.result)
            {
                if (tot_sq == "null") { tot_sq = "0"; }  // почему то не у всех есть общая площадь
                valuesFromFile.sql += tot_sq + ", ";
            }
            else
            {

                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Общая площадь");
            }
            i++;

            //14. Площадь мест общего пользования
            string mop_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Площадь мест общего пользования");
            }
            valuesFromFile.sql += mop_sq + ", ";
            i++;


            //15. Полезная (отапливаемая площадь)
            string use_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (ret.result)
            {
                valuesFromFile.sql += use_sq + ", ";
            }
            else
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Полезная (отапливаемая площадь)");
            }
            i++;

            //16. Код Муниципального образования (значение из справочника)
            string mo_id = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Код Муниципального образования (значение из справочника)");
            valuesFromFile.sql += mo_id + ", ";
            /*if (ret.result)
            {
                valuesFromFile.sql += mo_id + ", ";
            }
            else
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код Муниципального образования (значение из справочника)");
            }*/
            i++;

            //17. Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)
            string p = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, "Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)");
            valuesFromFile.sql += p + ", ";
            /*if (ret.result)
            {
                valuesFromFile.sql += p + ", ";
            }
            else
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)");
            }*/
            i++;


            //18. Количество строк - лицевой счет
            valuesFromFile.sql += "0, ";
            i++;

            //19. Количество строк - общедомовой прибор учета

            valuesFromFile.sql += "0, ";
            i++;

            // nzp_ul, nzp_dom, comment
            valuesFromFile.sql += "null,null,null";

            if (valuesFromFile.finder.format_name.Trim() == "'1.2.2'")
            {
                #region 20. Код улицы КЛАДР
                string kod_kladr = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, " Код улицы КЛАДР");
                valuesFromFile.sql += ", substr (" + kod_kladr + ", 1, 15) ";
                i++;
                #endregion 11. Код улицы КЛАДР
            }
            valuesFromFile.sql += ");";

            #endregion Загрузка 3 секции
        }

        /// <summary>
        /// Формат 1.3.2  Загрузка 3 секции "Информация о домах"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection03_FileDom_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 3;

            if (valuesFromFile.vals.Length < 21)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: Дома, количество полей = " + valuesFromFile.vals.Length + " вместо 21 ");
                return;
            }


            #region Загрузка 3 секции

            valuesFromFile.sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                "( id, local_id, nzp_file, ukds, town, rajon, ulica, ndom, nkor, area_id, cat_blago, etazh, build_year, total_square, mop_square, useful_square,  " +
                " mo_id, params, ls_row_number, odpu_row_number, nzp_ul, nzp_dom, comment , kod_kladr, kod_fias) ";
            valuesFromFile.sql += " VALUES ( ";
            int i = 1;

            //2. УКДС
            string ukds = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "УКДС");
            i++;

            //3. Уникальный код дома в системе отправителя

            string local_id = CheckType.CheckText(valuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          "Уникальный код дома в системе отправителя");
            }


            string id = "-1 ";

            i++;


            valuesFromFile.sql += id + ", " + local_id + ", " + valuesFromFile.finder.nzp_file + ", " + ukds + ", ";


            //4. Город/район
            string town = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, "Город/район");

            valuesFromFile.sql += town + ", ";
            i++;


            //5. Село/деревня
            string rajon = CheckType.CheckText2(valuesFromFile, i, false, 30, ref ret, "Село/деревня");
            valuesFromFile.sql += rajon + ", ";
            i++;


            //6. Наименование улицы
            string ul = CheckType.CheckText2(valuesFromFile, i, true, 40, ref ret, "Наименование улицы");
            valuesFromFile.sql += ul + ", ";
            i++;


            //7. Дом
            string dom = CheckType.CheckText2(valuesFromFile, i, true, 10, ref ret, "Дом");
            if (dom == "null") dom = "-";
            valuesFromFile.sql += dom + ", ";
            i++;


            //8. Корпус
            string kor = CheckType.CheckText2(valuesFromFile, i, false, 3, ref ret, "Корпус");
            valuesFromFile.sql += kor + ", ";
            i++;


            //9. Код ЮЛ, где лежит паспорт дома (УК)      
            string area_id = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Код ЮЛ, где лежит паспорт дома (УК)    ");
            valuesFromFile.sql += area_id + ", ";
            i++;


            //10. Категория благоустроенности (значение из справочника)
            string cat = CheckType.CheckText2(valuesFromFile, i, false, 30, ref ret, "Категория благоустроенности (значение из справочника)");
            valuesFromFile.sql += cat + ", ";
            i++;


            //11. Этажность
            string etag = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, " Этажность");
            if (!ret.result)
            {
                etag = "-1";
                //valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Этажность");
            }
            valuesFromFile.sql += etag + ", ";
            i++;


            //12. Год постройки
            //string y = CheckType.CheckDateTime(vals[i], true, ref ret);
#warning ?? небезопасно
            //TODO: обсудить
            string pval;
            if (valuesFromFile.vals[i].Length == 4)
            {
                pval = "01.01." + valuesFromFile.vals[i];
            }
            else
            {
                pval = valuesFromFile.vals[i];
            }
            string y = CheckType.CheckDateTime(pval, false, ref ret);
            if (!ret.result)
            {
                y = "'01.01.1900'";
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Год постройки");
            }
            valuesFromFile.sql += y + ", ";
            i++;


            //13. Общая площадь - необязат
            string tot_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (ret.result)
            {
                if (tot_sq == "null")
                {
                    tot_sq = "0";
                } // почему-то не у всех есть общая площадь
                valuesFromFile.sql += tot_sq + ", ";
            }
            else
            {

                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Общая площадь");
            }
            i++;

            //14. Площадь мест общего пользования
            string mop_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (ret.result)
            {
                valuesFromFile.sql += mop_sq + ", ";
            }
            else
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Площадь мест общего пользования");
            }
            i++;


            //15. Полезная (отапливаемая площадь)
            string use_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (ret.result)
            {
                valuesFromFile.sql += use_sq + ", ";
            }
            else
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Полезная (отапливаемая площадь)");
            }
            i++;

            //16. Код Муниципального образования (значение из справочника)
            string mo_id = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Код Муниципального образования (значение из справочника)");
            valuesFromFile.sql += mo_id + ", ";
            /*if (ret.result)
            {
                valuesFromFile.sql += mo_id + ", ";
            }
            else
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          "Код Муниципального образования (значение из справочника)");
            }*/
            i++;

            //17. Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)
            string p = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, "Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)");
            valuesFromFile.sql += p + ", ";
            /*if (ret.result)
            {
                valuesFromFile.sql += p + ", ";
            }
            else
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          "Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)");
            }*/
            i++;


            //18. Количество строк - лицевой счет
            /*string ls_row_number = CheckType.CheckInt(valuesFromFile.vals[i], false, 0, null, ref ret);
            if (!ret.result)
            {
                ls_row_number = "0";
            }
            valuesFromFile.sql += ls_row_number + ", ";
            i++;*/

            string ls_row_number = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Количество строк - лицевой счет");
            if (!ret.result || ls_row_number == "null")
            {
                ls_row_number = "0";
            }
            valuesFromFile.sql += ls_row_number + ", ";
            i++;



            //19. Количество строк - общедомовой прибор учета
            string odpu_row_number = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Количество строк - общедомовой прибор учета");
            if (!ret.result || odpu_row_number == "null")
            {
                odpu_row_number = "0";
                // valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Количество строк - общедомовой прибор учета");
            }
            valuesFromFile.sql += odpu_row_number + ", ";
            i++;

            // nzp_ul, nzp_dom, comment
            valuesFromFile.sql += "null,null,null";


            #region 20. Код улицы КЛАДР

            string kod_kladr = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, " Код улицы КЛАДР");
            if (kod_kladr == "null") kod_kladr = "0"; 
            valuesFromFile.sql += ", substr (" + kod_kladr + ", 1, 15) ";
            i++;

            #endregion 11. Код улицы КЛАДР

            #region 21. Код улицы ФИАС

            string kod_fias = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, " Код улицы ФИАС");
            if (kod_fias == "null") kod_fias = "0";
            valuesFromFile.sql += ", substr (" + kod_fias + ", 1, 15) ";
            i++;

            #endregion 21. Код улицы ФИАС

            valuesFromFile.sql += ");";

            #endregion Загрузка 3 секции
        }

        /// <summary>
        ///  Загрузка 4 секции "Информация о лицевых счетах"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection04_FileKvar(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 4;


            if (valuesFromFile.vals.Length < 35)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация о лицевых счетах, количество полей = " + valuesFromFile.vals.Length + " вместо 35 ");
                return;
            }

            #region Загрузка 4секции

            valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                    "( id, nzp_file, ukas, dom_id,  dom_id_char, ls_type, fam, ima, otch, birth_date, nkvar, nkvar_n,open_date, opening_osnov, close_date, closing_osnov, " +
                    " kol_gil, kol_vrem_prib, kol_vrem_ub, room_number, total_square, living_square, otapl_square, naim_square, is_communal, is_el_plita, " +
                    " is_gas_plita, is_gas_colonka, is_fire_plita, gas_type, water_type, hotwater_type,canalization_type, is_open_otopl, params, " +
                    " service_row_number, reval_params_row_number, ipu_row_number, id_urlic, nzp_dom, nzp_kvar) " +
                    " VALUES(";
            int i = 1;

            #region 2. УКАС
            string ukas = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "УКАС");
            i++;
            #endregion

            #region 3. Уникальный код дома для строки типа 2 – реквизит 3.
            string nzp_dom = CheckType.CheckText(valuesFromFile.vals[i], true, 20, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          "Уникальный код дома в системе отправителя");
            }

            i++;
            #endregion

            #region 3. Уникальный код дома для строки типа 2 – реквизит 3.

            nzp_dom = "-1";
            string dom_id_char = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код дома(char)");

            i++;

            #endregion


            #region 4. № ЛС в системе поставщика
            string id = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                id = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, "№ ЛС в системе поставщика");
            }
            i++;

            #endregion

            valuesFromFile.sql += id + ", " + valuesFromFile.finder.nzp_file + ", " + ukas + ", " + nzp_dom + ", ";


            #region 5. Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)
            string tp = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)");
            valuesFromFile.sql += tp + ", ";
            i++;
            #endregion

            #region 6. Фамилия квартиросъемщика
            string fam = CheckType.CheckText2(valuesFromFile, i, false, 200, ref ret, "Фамилия квартиросъемщика");
            valuesFromFile.sql += fam + ", ";
            i++;
            #endregion

            #region 7. Имя квартиросъемщика
            string name = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Имя квартиросъемщика");
            valuesFromFile.sql += name + ", ";
            i++;
            #endregion

            #region 8. Отчество квартиросъемщика
            string sur = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Отчество квартиросъемщика");
            valuesFromFile.sql += sur + ", ";
            i++;
            #endregion

            #region 9. Дата рождения квартиросъемщика
            string burthDate = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата рождения квартиросъемщика");
            valuesFromFile.sql += burthDate + ", ";
            i++;
            #endregion

            #region 10. Квартира
            string kvar = CheckType.CheckText2(valuesFromFile, i, false, 10, ref ret, "Квартира");
            if (kvar == "null") kvar = "'-'";
            valuesFromFile.sql += kvar + ", ";
            i++;
            #endregion

            #region 11. Комната лицевого счета
            string nkvar_n = CheckType.CheckText2(valuesFromFile, i, false, 3, ref ret, " Комната лицевого счета");
            if (nkvar_n == "null") nkvar_n = "'-'";
            valuesFromFile.sql += nkvar_n + ", ";
            i++;
            #endregion

            #region 12. Дата открытия ЛС
            string open_date = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата открытия ЛС");
            valuesFromFile.sql += open_date + ", ";
            i++;
            #endregion

            #region 13. Основание открытия ЛС
            string osnov = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Основание открытия ЛС");
            valuesFromFile.sql += osnov + ", ";
            i++;
            #endregion

            #region 14. Дата закрытия ЛС
            string dat_close = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата закрытия ЛС");
            valuesFromFile.sql += dat_close + ", ";
            i++;
            #endregion

            #region 15. Основание закрытия ЛС
            string close_osnov = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Основание закрытия ЛС");
            valuesFromFile.sql += close_osnov + ", ";
            i++;
            #endregion

            #region 16. Количество проживающих
            string col_gil = "";
            if (valuesFromFile.Pvers == "1.0" || valuesFromFile.Pvers == "1.1")
            {
                col_gil = CheckType.CheckInt(valuesFromFile.vals[i], false, 0, null, ref ret);
            }
            else
            {
                col_gil = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            }
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Количество проживающих");

            }
            if (col_gil == "null") { col_gil = "0"; };
            valuesFromFile.sql += col_gil + ", ";
            i++;
            #endregion

            #region 17. Количество врем. прибывших жильцов
            if (valuesFromFile.vals[i].Length == 0) { valuesFromFile.vals[i] = "0"; };
            string col_prib = "";
            if (valuesFromFile.Pvers == "1.0" || valuesFromFile.Pvers == "1.1")
            {
                col_prib = CheckType.CheckInt(valuesFromFile.vals[i], false, 0, null, ref ret);
            }
            else
            {
                col_prib = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            }
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Количество проживающих");
            }
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Количество врем. прибывших жильцов");

            }
            if (col_prib == "null") { col_prib = "0"; };
            valuesFromFile.sql += col_prib + ", ";
            i++;
            #endregion

            #region 18. Количество  врем. убывших жильцов
            if (valuesFromFile.vals[i].Length == 0) { valuesFromFile.vals[i] = "0"; };
            string col_ub = "";
            if (valuesFromFile.Pvers == "1.0" || valuesFromFile.Pvers == "1.1")
            {
                col_ub = CheckType.CheckInt(valuesFromFile.vals[i], false, 0, null, ref ret);
            }
            else
            {
                col_ub = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            }
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Количество проживающих");

            }
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Количество  врем. убывших жильцов");
            }
            if (col_ub == "null") { col_ub = "0"; };
            valuesFromFile.sql += col_ub + ", ";
            i++;
            #endregion

            #region 19. Количество комнат
            string room = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Количество комнат");
            if (room == "null") { room = "0"; };
            valuesFromFile.sql += room + ", ";
            i++;
            #endregion

            #region 20. Общая площадь
            string tot_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Общая площадь");
            }
            if (tot_sq == "null") { tot_sq = "0"; };
            valuesFromFile.sql += tot_sq + ", ";
            i++;
            #endregion

            #region 21. Жилая площадь
            string gil_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Жилая площадь");
            }
            valuesFromFile.sql += gil_sq + ", ";
            i++;
            #endregion

            #region 22. Отапливаемая площадь
            string otap_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Отапливаемая площадь");

            }
            valuesFromFile.sql += otap_sq + ", ";
            i++;
            #endregion

            #region 23. Площадь для найма
            string naim_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Площадь для найма");
            }
            valuesFromFile.sql += naim_sq + ", ";
            i++;
            #endregion

            #region 24. Признак коммунальной квартиры(1-да, 0 –нет)
            string is_komm = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, "Признак коммунальной квартиры(1-да, 0 –нет)");
            valuesFromFile.sql += is_komm + ", ";
            i++;
            #endregion

            #region 25. Наличие эл. плиты (1-да, 0 –нет)
            string el_pl = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие эл. плиты (1-да, 0 –нет)");
            valuesFromFile.sql += el_pl + ", ";
            i++;
            #endregion

            #region 26. Наличие газовой плиты (1-да, 0 –нет)
            string gas_pl = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие газовой плиты (1-да, 0 –нет)");
            valuesFromFile.sql += gas_pl + ", ";
            i++;
            #endregion

            #region 27. Наличие газовой колонки (1-да, 0 –нет)
            string gas_col = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие газовой колонки (1-да, 0 –нет)");
            valuesFromFile.sql += gas_col + ", ";
            i++;
            #endregion

            #region 28. Наличие огневой плиты (1-да, 0 –нет)
            string ogn_pl = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие огневой плиты (1-да, 0 –нет)");
            valuesFromFile.sql += ogn_pl + ", ";
            i++;
            #endregion

            #region 29. Код типа жилья по газоснабжению (из справочника)
            string ktg_gas = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Код типа жилья по газоснабжению (из справочника)");
            valuesFromFile.sql += ktg_gas + ", ";
            i++;
            #endregion

            #region 30. Код типа жилья по водоснабжению (из справочника)
            string ktg_water = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, " Код типа жилья по водоснабжению (из справочника)");
            valuesFromFile.sql += ktg_water + ", ";
            i++;
            #endregion

            #region 31. Код типа жилья по горячей воде (из справочника)
            string ktg_water_hot = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, " Код типа жилья по горячей воде (из справочника)");
            valuesFromFile.sql += ktg_water_hot + ", ";
            i++;
            #endregion

            #region 32. Код типа жилья по канализации (из справочника)
            string ktg_canal = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, " Код типа жилья по канализации (из справочника)");
            valuesFromFile.sql += ktg_canal + ", ";
            i++;
            #endregion

            #region 33. Наличие забора из открытой системы отопления (1-да, 0 –нет)
            string z_otop = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие забора из открытой системы отопления (1-да, 0 –нет)");
            valuesFromFile.sql += z_otop + ", ";
            i++;
            #endregion

            #region 34. Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)
            string dop_har = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, " Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)");
            valuesFromFile.sql += dop_har + ", ";
            i++;
            #endregion

            #region 35. Количество строк - услуга
            string rows = CheckType.CheckInt(valuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                rows = "0";
            }
            valuesFromFile.sql += rows + ", ";
            i++;
            #endregion

            #region 36. Количество строк  – параметры в месяце перерасчета лицевого счета
            try
            {
                string rows_params = CheckType.CheckInt(valuesFromFile.vals[i], true, 0, null, ref ret);
                if (!ret.result)
                {
                    rows_params = "0";
                    //err.Append(rowNumber + ret.text + " Количество строк  – параметры в месяце перерасчета лицевого счета");
                }
                valuesFromFile.sql += rows_params + ", ";
                i++;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка загрузки файла Характеристики жилого фонда и начисления ЖКУ", ex);
                valuesFromFile.err.Append("Отсутствует | Количество строк  – параметры в месяце перерасчета лицевого счета");
            }
            #endregion

            //   try
            //   {
            #region 37. Количество строк – индивидуальный прибор учета
            string rows_ipu = CheckType.CheckInt(valuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                //if (true)
                { rows_ipu = "0"; }   // стал необязательным 
                //else
                //{
                //    err.Append(rowNumber + ret.text + " Количество строк – индивидуальный прибор учета");
                //}
            }
            valuesFromFile.sql += rows_ipu + ", ";
            i++;
            #endregion
            //  }
            //  catch (Exception ex) { err.Append(rowNumber + ret.text + "Отсутствует | Количество строк – индивидуальный прибор учета"); }

            #region 38. Уникальный код ЮЛ
            string id_urlic;
            if (valuesFromFile.Pvers != "1.0" && valuesFromFile.vals.Length > 37)
            {
                id_urlic = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Уникальный код ЮЛ)");
            }
            else id_urlic = "null";
            valuesFromFile.sql += id_urlic + ", ";
            i++;
            #endregion

            valuesFromFile.sql += "null,null);";

            #endregion Загрузка 4 секции
        }

        /// <summary>
        /// Формат 1.3.2  Загрузка 4 секции "Информация о лицевых счетах"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection04_FileKvar_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 4;


            if (valuesFromFile.vals.Length < 42)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: Информация о лицевых счетах, количество полей = " + valuesFromFile.vals.Length + " вместо 42 ");
                return;
            }

            #region Загрузка 4секции

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                "( id, nzp_file, ukas, dom_id, dom_id_char, ls_type, fam, ima, otch, birth_date, nkvar, nkvar_n,open_date, opening_osnov, close_date, closing_osnov, " +
                " kol_gil, kol_vrem_prib, kol_vrem_ub, room_number, total_square, living_square, otapl_square, naim_square, is_communal, is_el_plita, " +
                " is_gas_plita, is_gas_colonka, is_fire_plita, gas_type, water_type, hotwater_type,canalization_type, is_open_otopl, params, " +
                " service_row_number, reval_params_row_number, ipu_row_number, id_urlic, type_owner, id_gil, uch, id_urlic_pass_dom," +
                " nzp_dom, nzp_kvar) " +
                " VALUES(";
            int i = 1;

            #region 2. УКАС

            string ukas = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "УКАС");
            i++;

            #endregion

            #region 3. Уникальный код дома для строки типа 2 – реквизит 3.

            string nzp_dom = "-1";
            string dom_id_char = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, "Уникальный код дома(char)");

            i++;

            #endregion


            #region 4. № ЛС в системе поставщика

            string id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");
            i++;
            #endregion

            valuesFromFile.sql += id + ", " + valuesFromFile.finder.nzp_file + ", " + ukas + ", " + nzp_dom + ", " + dom_id_char + ", ";


            #region 5. Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)

            string tp = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)");
            valuesFromFile.sql += tp + ", ";
            i++;

            #endregion

            #region 6. Фамилия квартиросъемщика

            string fam = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Фамилия квартиросъемщика");
            valuesFromFile.sql += fam + ", ";
            i++;

            #endregion

            #region 7. Имя квартиросъемщика

            string name = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Имя квартиросъемщика");
            valuesFromFile.sql += name + ", ";
            i++;

            #endregion

            #region 8. Отчество квартиросъемщика

            string sur = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Отчество квартиросъемщика");
            valuesFromFile.sql += sur + ", ";
            i++;

            #endregion

            #region 9. Дата рождения квартиросъемщика

            string burthDate = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата рождения квартиросъемщика");
            valuesFromFile.sql += burthDate + ", ";
            i++;

            #endregion

            #region 10. Квартира

            string kvar = CheckType.CheckText2(valuesFromFile, i, true, 10, ref ret, "Квартира");
            if (kvar == "null") kvar = "'-'";
            valuesFromFile.sql += kvar + ", ";
            i++;

            #endregion

            #region 11. Комната лицевого счета

            string nkvar_n = CheckType.CheckText2(valuesFromFile, i, false, 3, ref ret, " Комната лицевого счета");
            if (nkvar_n == "null") nkvar_n = "'-'";
            valuesFromFile.sql += nkvar_n + ", ";
            i++;

            #endregion

            #region 12. Дата открытия ЛС

            string open_date = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата открытия ЛС");
            valuesFromFile.sql += open_date + ", ";
            i++;

            #endregion

            #region 13. Основание открытия ЛС

            string osnov = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Основание открытия ЛС");
            valuesFromFile.sql += osnov + ", ";
            i++;

            #endregion

            #region 14. Дата закрытия ЛС

            string dat_close = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата закрытия ЛС");
            valuesFromFile.sql += dat_close + ", ";
            i++;

            #endregion

            #region 15. Основание закрытия ЛС

            string close_osnov = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Основание закрытия ЛС");
            valuesFromFile.sql += close_osnov + ", ";
            i++;

            #endregion

            #region 16. Количество проживающих

            string col_gil = "";

            col_gil = CheckType.CheckInt2(valuesFromFile, i, true, 0, null, ref ret, "Количество проживающих");
            if (col_gil == "null")
            {
                col_gil = "0";
            };
            valuesFromFile.sql += col_gil + ", ";
            i++;

            #endregion

            #region 17. Количество врем. прибывших жильцов

            string col_prib = CheckType.CheckInt2(valuesFromFile, i, true, 0, null, ref ret, "Количество врем. прибывших жильцов");
            if (col_prib == "null")
            {
                col_prib = "0";
            }
            ;
            valuesFromFile.sql += col_prib + ", ";
            i++;

            #endregion

            #region 18. Количество  врем. убывших жильцов
            string col_ub = CheckType.CheckInt2(valuesFromFile, i, true, 0, null, ref ret, "Количество  врем. убывших жильцов");
            if (col_ub == "null")
            {
                col_ub = "0";
            }
            ;
            valuesFromFile.sql += col_ub + ", ";
            i++;

            #endregion

            #region 19. Количество комнат

            string room = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Количество комнат");
            if (room == "null")
            {
                room = "1";
            }
            ;
            valuesFromFile.sql += room + ", ";
            i++;

            #endregion

            #region 20. Общая площадь

            string tot_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Общая площадь");
            }
            if (tot_sq == "null")
            {
                tot_sq = "0";
            }
            ;
            valuesFromFile.sql += tot_sq + ", ";
            i++;

            #endregion

            #region 21. Жилая площадь

            string gil_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Жилая площадь");
            }
            valuesFromFile.sql += gil_sq + ", ";
            i++;

            #endregion

            #region 22. Отапливаемая площадь

            string otap_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Отапливаемая площадь");

            }
            valuesFromFile.sql += otap_sq + ", ";
            i++;

            #endregion

            #region 23. Площадь для найма

            string naim_sq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Площадь для найма");
            }
            valuesFromFile.sql += naim_sq + ", ";
            i++;

            #endregion

            #region 24. Признак коммунальной квартиры(1-да, 0 –нет)

            string is_komm = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, "Признак коммунальной квартиры(1-да, 0 –нет)");
            valuesFromFile.sql += is_komm + ", ";
            i++;

            #endregion

            #region 25. Наличие эл. плиты (1-да, 0 –нет)

            string el_pl = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие эл. плиты (1-да, 0 –нет)");
            valuesFromFile.sql += el_pl + ", ";
            i++;

            #endregion

            #region 26. Наличие газовой плиты (1-да, 0 –нет)

            string gas_pl = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие газовой плиты (1-да, 0 –нет)");
            valuesFromFile.sql += gas_pl + ", ";
            i++;

            #endregion

            #region 27. Наличие газовой колонки (1-да, 0 –нет)

            string gas_col = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие газовой колонки (1-да, 0 –нет)");
            valuesFromFile.sql += gas_col + ", ";
            i++;

            #endregion

            #region 28. Наличие огневой плиты (1-да, 0 –нет)

            string ogn_pl = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие огневой плиты (1-да, 0 –нет)");
            valuesFromFile.sql += ogn_pl + ", ";
            i++;

            #endregion

            #region 29. Код типа жилья по газоснабжению (из справочника)

            string ktg_gas = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, "Код типа жилья по газоснабжению (из справочника)");
            valuesFromFile.sql += ktg_gas + ", ";
            i++;

            #endregion

            #region 30. Код типа жилья по водоснабжению (из справочника)

            string ktg_water = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, " Код типа жилья по водоснабжению (из справочника)");
            valuesFromFile.sql += ktg_water + ", ";
            i++;

            #endregion

            #region 31. Код типа жилья по горячей воде (из справочника)

            string ktg_water_hot = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, " Код типа жилья по горячей воде (из справочника)");
            valuesFromFile.sql += ktg_water_hot + ", ";
            i++;

            #endregion

            #region 32. Код типа жилья по канализации (из справочника)

            string ktg_canal = CheckType.CheckInt2(valuesFromFile, i, false, 1, null, ref ret, " Код типа жилья по канализации (из справочника)");
            valuesFromFile.sql += ktg_canal + ", ";
            i++;

            #endregion

            #region 33. Наличие забора из открытой системы отопления (1-да, 0 –нет)

            string z_otop = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие забора из открытой системы отопления (1-да, 0 –нет)");
            valuesFromFile.sql += z_otop + ", ";
            i++;

            #endregion

            #region 34. Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)

            string dop_har = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, " Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)");
            valuesFromFile.sql += dop_har + ", ";
            i++;

            #endregion

            #region 35. Количество строк - услуга

            string rows = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Количество строк - услуга");
            if (!ret.result || rows == "null")
            {
                rows = "0";

            }
            valuesFromFile.sql += rows + ", ";
            i++;

            #endregion

            #region 36. Количество строк  – параметры в месяце перерасчета лицевого счета

            string rows_params = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Отсутствует | Количество строк  – параметры в месяце перерасчета лицевого счета");
            if (!ret.result || rows_params == "null")
            {
                rows_params = "0";
                // valuesFromFile.err.Append(
                //"Отсутствует | Количество строк  – параметры в месяце перерасчета лицевого счета");
            }
            valuesFromFile.sql += rows_params + ", ";
            i++;



            #endregion

            #region 37. Количество строк – индивидуальный прибор учета

            string rows_ipu = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Количество строк – индивидуальный прибор учета");
            if (!ret.result || rows_ipu == "null")
            {
                rows_ipu = "0";
            }
            valuesFromFile.sql += rows_ipu + ", ";
            i++;

            #endregion

            #region 38. Уникальный код ЮЛ

            string id_urlic = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Уникальный код ЮЛ)");
            if (id_urlic == "0")
            {
                id_urlic = "1000000";
            }

            valuesFromFile.sql += id_urlic + ", ";
            i++;

            #endregion

            #region 39. Тип владения

            string type_owner = CheckType.CheckText2(valuesFromFile, i, false, 30, ref ret, "Тип владения");
            valuesFromFile.sql += type_owner + ", ";
            i++;

            #endregion

            #region 40. Уникальный код жильца квартиросъемщика

            string id_gil = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Уникальный код жильца квартиросъемщика)");
            valuesFromFile.sql += id_gil + ", ";
            i++;

            #endregion

            #region 41. Участок

            string uch = CheckType.CheckText2(valuesFromFile, i, false, 60, ref ret, "Участок");
            valuesFromFile.sql += uch + ", ";
            i++;

            #endregion

            #region 40. Код ЮЛ, где лежит паспорт дома

            //string id_urlic_pass_dom = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код ЮЛ, где лежит паспорт дома)");
            string id_urlic_pass_dom = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код ЮЛ, где лежит паспорт дома)"); //теперь может быть пустым
            if (id_urlic_pass_dom == "null")
            {
                id_urlic_pass_dom = "0"; // Спасибо Павел Павленко и Рустем Шакиров подписались 
            }
            if (id_urlic_pass_dom == "0")
            {
                id_urlic_pass_dom = "1000000";
            }
            valuesFromFile.sql += id_urlic_pass_dom + ", ";
            i++;

            #endregion

            valuesFromFile.sql += "null,null);";

            #endregion Загрузка 4 секции
        }

        /// <summary>
        ///  Загрузка 5 секции "Поставщики услуг"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection05_FileSupp(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 5;


            if (valuesFromFile.vals.Length < 11)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Поставщики услуг, количество полей = " + valuesFromFile.vals.Length + " вместо 11 ");
                return;
            }

            #region Загрузка 5 секции
            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + " file_supp " +
                "( id, nzp_file, supp_id, supp_name, jur_address, fact_address, inn, kpp, rs, bank, bik, ks, nzp_supp) " +
                " VALUES( ";
            int i = 1;

            #region 2. Уникальный код поставщика
            string id = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "УКАС");

            }
            i++;
            #endregion

            //id serial
            valuesFromFile.sql += "0, ";

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", " + id + ", "; //продублировал

            #region 3. Наименование поставщика
            //string name_supp = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
            string name_supp = CheckType.CheckText(valuesFromFile.vals[i], true, 180, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Наименование поставщика");

            }
            valuesFromFile.sql += name_supp + ", ";
            i++;
            #endregion

            #region 4. Юридический адрес
            string jur_adr = CheckType.CheckText(valuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Юридический адрес");

            }
            valuesFromFile.sql += jur_adr + ", ";
            i++;
            #endregion

            #region 5. Фактический адрес
            string fact_adr = CheckType.CheckText(valuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Фактический адрес");

            }
            valuesFromFile.sql += fact_adr + ", ";
            i++;
            #endregion

            #region 6. ИНН
            string inn = CheckType.CheckText(valuesFromFile.vals[i], true, 12, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "ИНН");

            }
            valuesFromFile.sql += inn + ", ";
            i++;
            #endregion

            #region 7. КПП
            string kpp = CheckType.CheckText(valuesFromFile.vals[i], true, 9, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "КПП");

            }
            valuesFromFile.sql += kpp + ", ";
            i++;
            #endregion

            #region 8. Расчетный счет
            string rchet = CheckType.CheckText(valuesFromFile.vals[i].Trim(), false, 20, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расчетный счет");

            }
            valuesFromFile.sql += rchet + ", ";
            i++;
            #endregion

            #region 9. Банк
            string bank = CheckType.CheckText(valuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Банк");

            }
            valuesFromFile.sql += bank + ", ";
            i++;
            #endregion

            #region 10. БИК банка
            string bik = CheckType.CheckText(valuesFromFile.vals[i], false, 20, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " БИК банка");

            }
            valuesFromFile.sql += bik + ", ";
            i++;
            #endregion

            #region 11. Корреспондентский счет
            string kschet = CheckType.CheckText(valuesFromFile.vals[i], false, 20, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Корреспондентский счет");

            }
            valuesFromFile.sql += kschet + ", ";
            i++;
            #endregion

            //nzp_supp
            valuesFromFile.sql += "null);";

            #endregion Загрузка 5 секции
        }


        /// <summary>
        ///  Формат 1.3.2 Загрузка 5 секции "Договор на оказание ЖКУ"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection05_FileSupp_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 5;

            if (valuesFromFile.vals.Length < 9)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Договор на оказание ЖКУ, количество полей = " + valuesFromFile.vals.Length + " вместо 9 ");
                return;
            }

            #region Загрузка 5 секции

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + " file_dog " +
                "( nzp_file, dog_id, id_agent, id_urlic_p, id_supp, dog_name, dog_num, dog_date, comment, nzp_supp) " +
                " VALUES( ";

            #region 1. Тип строки пропускаем, nzp_file

            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код договора
            string dog_id = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Уникальный код договора");
            valuesFromFile.sql += dog_id + ", ";
            i++;
            #endregion

            #region 3. Код агента получателя платежей
            string id_agent = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код агента получателя платеже");
            valuesFromFile.sql += id_agent + ", ";
            i++;
            #endregion

            #region 4. Код ЮЛ принципала
            string id_urlic_p = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код ЮЛ принципала");
            if (id_urlic_p == "0")
            {
                id_urlic_p = "1000000";
            }

            valuesFromFile.sql += id_urlic_p + ", ";
            i++;
            #endregion

            #region 5. Код поставщика
            string id_supp = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код поставщика");
            valuesFromFile.sql += id_supp + ", ";
            i++;
            #endregion

            #region 6. Наименование договра
            string dog_name = CheckType.CheckText2(valuesFromFile, i, false, 60, ref ret, "Наименование договра");
            valuesFromFile.sql += dog_name + ", ";
            i++;
            #endregion

            #region 7. Номер договора
            string dog_num = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Номер договора");
            valuesFromFile.sql += dog_num + ", ";
            i++;
            #endregion

            #region 8. Дата договора
            string dog_date = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата договора");
            valuesFromFile.sql += dog_date + ", ";
            i++;
            #endregion

            #region 9. Комментарий
            string comment = CheckType.CheckText2(valuesFromFile, i, false, 200, ref ret, "Фактический адрес");
            valuesFromFile.sql += comment + ", ";
            i++;
            #endregion

            //nzp_supp
            valuesFromFile.sql += "null);";

            #endregion Загрузка 5 секции
        }


        /// <summary>
        ///  Формат 1.3.3 Загрузка 5 секции "Договор на оказание ЖКУ"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection05_FileSupp_v_133(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 5;

            if (valuesFromFile.vals.Length < 10)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Договор на оказание ЖКУ");
                return;
            }

            #region Загрузка 5 секции

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + " file_dog " +
                "( nzp_file, dog_id, id_agent, id_urlic_p, id_supp, dog_name, dog_num, dog_date, comment, rs, nzp_supp) " +
                " VALUES( ";

            #region 1. Тип строки пропускаем, nzp_file

            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код договора
            string dog_id = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Уникальный код договора");
            valuesFromFile.sql += dog_id + ", ";
            i++;
            #endregion

            #region 3. Код агента получателя платежей
            string id_agent = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код агента получателя платеже");
            valuesFromFile.sql += id_agent + ", ";
            i++;
            #endregion


            #region 4. Код ЮЛ принципала
            string id_urlic_p = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код ЮЛ принципала");
            valuesFromFile.sql += id_urlic_p + ", ";
            i++;
            #endregion

            #region 5. Код поставщика
            string id_supp = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код поставщика");
            valuesFromFile.sql += id_supp + ", ";
            i++;
            #endregion

            #region 6. Наименование договра
            string dog_name = CheckType.CheckText2(valuesFromFile, i, false, 60, ref ret, "Наименование договора");
            valuesFromFile.sql += dog_name + ", ";
            i++;
            #endregion

            #region 7. Номер договора
            string dog_num = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Номер договора");
            valuesFromFile.sql += dog_num + ", ";
            i++;
            #endregion

            #region 8. Дата договора
            string dog_date = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата договора");
            valuesFromFile.sql += dog_date + ", ";
            i++;
            #endregion

            #region 9. Комментарий
            string comment = CheckType.CheckText2(valuesFromFile, i, false, 200, ref ret, "Фактический адрес");
            valuesFromFile.sql += comment + ", ";
            i++;
            #endregion

            #region 10. Расчетный счет
            string rs = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Расчетный счет");
            valuesFromFile.sql += rs + ", ";
            i++;
            #endregion

            //nzp_supp
            valuesFromFile.sql += "null);";

            #endregion Загрузка 5 секции
        }

        /// <summary>
        ///  Загрузка 6 секции "Информация об оказываемых услугах"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection06_FileServ(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 6;

            if (valuesFromFile.vals.Length < 24)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация об оказываемых услугах, количество полей = " + valuesFromFile.vals.Length + " вместо 24 ");
                return;
            }

            #region Загрузка 6 секции
            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                "(nzp_file, ls_id, supp_id, nzp_serv, sum_insaldo, eot, reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_nach, sum_reval, " +
                "sum_subsidy, sum_subsidyp, sum_lgota, sum_lgotap,sum_smo, sum_smop, sum_money, is_del, sum_outsaldo, servp_row_number, nzp_kvar, nzp_supp) " +
                " VALUES (";
            int i = 1;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";


            #region 2. № ЛС в системе поставщика
            string ls_id = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret).ToString();
            if (!ret.result)
            {
                ls_id = CheckType.CheckText(valuesFromFile.vals[i], true, 20, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "№ ЛС в системе поставщика");

                }
            }
            valuesFromFile.sql += ls_id + ", ";
            i++;
            #endregion

            #region 3. Код поставщика услуг
            string supp_id = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код поставщика услуг");

            }
            valuesFromFile.sql += supp_id + ", ";
            i++;
            #endregion

            #region 4. Код услуги (из справочника)
            string nzp_serv = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код услуги (из справочника)");

            }
            valuesFromFile.sql += nzp_serv + ", ";
            i++;
            #endregion

            #region 5. Входящее сальдо (Долг на начало месяца)
            //string saldo_in = CheckType.CheckDecimal(vals[i], true, true, null, null, ref ret);
            string saldo_in = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret); // Ослабил для Губкина 
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Входящее сальдо (Долг на начало месяца)");

            }
            if (saldo_in == "null") { saldo_in = "0"; };
            valuesFromFile.sql += saldo_in + ", ";
            i++;
            #endregion

            #region 6. Экономически обоснованный тариф
            string eot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");

            }
            valuesFromFile.sql += eot + ", ";
            i++;
            #endregion

            #region 7. Процент регулируемого тарифа от экономически обоснованного
            string reg_tarif_percent = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Процент регулируемого тарифа от экономически обоснованного");

            }
            #endregion

            #region  8. Регулируемый тариф
            decimal? check = null;
            try
            {
                decimal checkt = Convert.ToDecimal(eot) * Convert.ToDecimal(reg_tarif_percent) / 100;
                checkt = Decimal.Round(checkt, 2);
                check = checkt;
                //проверка параметра 6
                //eot = CheckType.CheckDecimal(vals[i], true, false, Convert.ToDecimal(check), null, ref ret);

                eot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, Convert.ToDecimal(check), null, ref ret);
                if (!ret.result)
                {
                    // нужно вычислить правильный процент
                    // но мы вычислать не будем пока
                    // err.Append(rowNumber + ret.text + "Экономически обоснованный тариф ");
                    eot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
                    if (!ret.result)
                    {
                        valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");
                    }
                }
            }
            catch (Exception) { }

            valuesFromFile.sql += reg_tarif_percent + ", ";
            i++;

            //проверка параметра 8
            string reg_tarif = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Регулируемый тариф ");

            }
            valuesFromFile.sql += reg_tarif + ", ";
            i++;
            #endregion

            #region 9. Код единицы измерения расхода (из справочника)
            string nzp_measure = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код единицы измерения расхода (из справочника) ");

            }
            valuesFromFile.sql += nzp_measure + ", ";
            i++;
            #endregion

            #region 10. Расход фактический
            string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            //string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход фактический");

            }
            valuesFromFile.sql += ras_fact + ", ";
            i++;
            #endregion

            #region 11. Расход по нормативу
            string ras_norm = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход по нормативу");

            }
            valuesFromFile.sql += ras_norm + ", ";
            i++;
            #endregion

            #region 12. Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)
            string is_pu_calc = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 1, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)");

            }
            valuesFromFile.sql += is_pu_calc + ", ";
            i++;
            #endregion

            #region 13. Сумма начисления
            //check = Convert.ToDecimal(eot) * Convert.ToDecimal(ras_fact);
            //check = Decimal.Round(check, 2,MidpointRounding.ToEven);
            //string sum_nach = CheckType.CheckDecimal(vals[i], true, false, null, null, ref ret); 
            string sum_nach = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret); // ослабил для Губкина
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма начисления");

            }
            if (sum_nach == "null") { sum_nach = "0"; };

            valuesFromFile.sql += sum_nach + ", ";
            i++;
            #endregion

            #region 14. Сумма перерасчета начисления за предыдущий период (изменение сальдо)

            string sum_per;
            if (valuesFromFile.vals[i] != "")
                sum_per = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            else
                sum_per = CheckType.CheckDecimal("0.00", true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета начисления за предыдущий период (изменение сальдо)");

            }
            valuesFromFile.sql += sum_per + ", ";
            i++;
            #endregion

            #region 15. Сумма дотации
            string sum_dot = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма дотации");
            }

            if (sum_dot == "null") { sum_dot = "0"; }
            valuesFromFile.sql += sum_dot + ", ";
            i++;
            #endregion

            #region 16. Сумма перерасчета дотации за предыдущий период (за все месяца)
            string sum_dot_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);

            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета дотации за предыдущий период (за все месяца)");

            }
            if (sum_dot_per == "null") { sum_dot_per = "0"; }
            valuesFromFile.sql += sum_dot_per + ", ";
            i++;
            #endregion

            #region 17. Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)
            string sum_lgota = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)");
            }
            if (sum_lgota == "null") { sum_lgota = "0"; }
            valuesFromFile.sql += sum_lgota + ", ";
            i++;
            #endregion

            #region 18. Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)
            string sum_lgota_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)");

            }

            if (sum_lgota_per == "null") { sum_lgota_per = "0"; }
            valuesFromFile.sql += sum_lgota_per + ", ";
            i++;
            #endregion

            #region 19. Сумма СМО
            string smo = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма СМО");
            }

            if (smo == "null") { smo = "0"; }
            valuesFromFile.sql += smo + ", ";
            i++;
            #endregion

            #region 20. Сумма перерасчета  СМО за предыдущий период (за все месяца)
            string smo_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета  СМО за предыдущий период (за все месяца)");

            }
            if (smo_per == "null") { smo_per = "0"; }
            valuesFromFile.sql += smo_per + ", ";
            i++;
            #endregion

            #region 21. Сумма оплаты, поступившие за месяц начислений
            string sum_opl = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма оплаты, поступившие за месяц начислений");

            }
            valuesFromFile.sql += sum_opl + ", ";
            i++;
            #endregion

            #region 22. Признак удаленности услуги
            string is_del = CheckType.CheckInt(valuesFromFile.vals[i], true, 0, 1, ref ret);
            if (!ret.result)
            {
                if (valuesFromFile.vals[i] == "0.00") { ret.result = true; }
                else
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Признак удаленности услуги");

            }
            valuesFromFile.sql += is_del + ", ";
            i++;
            #endregion

            #region 23. Исходящее сальдо (Долг на окончание месяца)
            //string outsaldo = CheckType.CheckDecimal(vals[i], true, true, null, null, ref ret);
            string outsaldo = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret, true); // Убрал условие из_моней для Губкина, добавил условие из_губкин
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Исходящее сальдо (Долг на окончание месяца)");

            }
            if (outsaldo == "null") { outsaldo = "0"; };
            // округление до 2 знаков                            
            valuesFromFile.sql += outsaldo + ", ";
            i++;
            #endregion

            #region 24. Количество строк – перерасчетов начисления по услуге

            string rows = CheckType.CheckInt(valuesFromFile.vals[i], true, 0, null, ref ret);
            if (!ret.result)
            {
                rows = "0";
                //  err.Append(rowNumber + ret.text + "Количество строк – перерасчетов начисления по услуге");
            }
            valuesFromFile.sql += rows + ", ";
            i++;
            #endregion

            //nzp_kvar, nzp_supp
            valuesFromFile.sql += "null,null);";
            #endregion
        }

        /// <summary>
        /// Формат 1.3.2  Загрузка 6 секции "Информация об оказываемых услугах"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection06_FileServ_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 6;

            if (valuesFromFile.vals.Length < 26)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация об оказываемых услугах, количество полей = " + valuesFromFile.vals.Length + " вместо 26 ");
                return;
            }

            #region Загрузка 6 секции
            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                "(nzp_file, ls_id, dog_id, nzp_serv, sum_insaldo, eot, reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_nach, sum_reval, " +
                " sum_subsidy, sum_subsidyp, sum_lgota, sum_lgotap,sum_smo, sum_smop, sum_money, is_del, sum_outsaldo, servp_row_number, met_calc, pkod," +
                " nzp_kvar, nzp_supp) " +
                " VALUES (";
            int i = 1;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";


            #region 2. № ЛС в системе поставщика
            string id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");
            i++;
            valuesFromFile.sql += id + ", ";
            #endregion

            #region 3. Код договора
            string dog_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код договора");
            valuesFromFile.sql += dog_id + ", ";
            i++;
            #endregion

            #region 4. Код услуги (из справочника)
            string nzp_serv = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код услуги (из справочника)");
            valuesFromFile.sql += nzp_serv + ", ";
            i++;
            #endregion

            #region 5. Входящее сальдо (Долг на начало месяца)
            string saldo_in = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Входящее сальдо (Долг на начало месяца)");

            }
            valuesFromFile.sql += saldo_in + ", ";
            i++;
            #endregion

            #region 6. Экономически обоснованный тариф
            string eot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");

            }
            valuesFromFile.sql += eot + ", ";
            i++;
            #endregion

            #region 7. Процент регулируемого тарифа от экономически обоснованного
            string reg_tarif_percent = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Процент регулируемого тарифа от экономически обоснованного");

            }

            valuesFromFile.sql += reg_tarif_percent + ", ";
            i++;
            #endregion

            #region  8. Регулируемый тариф
            string reg_tarif = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Регулируемый тариф ");

            }
            valuesFromFile.sql += reg_tarif + ", ";
            i++;

            //проверка параметра 8
            decimal checkt = Convert.ToDecimal(eot) * Convert.ToDecimal(reg_tarif_percent) / 100;
            int kolZn = reg_tarif.Substring(reg_tarif.IndexOf(".") + 1).Length;
            if (Math.Abs(Math.Round(checkt, kolZn) - Convert.ToDecimal(reg_tarif))>Math.Abs(checkt/100))
            {
                MonitorLog.WriteLog(valuesFromFile.rowNumber + "Секция 6: Регулируемый тариф не равен (ЭОТ * процент)/100 " +
                    Convert.ToDecimal(reg_tarif) + " не равно " + Math.Round(checkt, kolZn), MonitorLog.typelog.Info, true);
               // valuesFromFile.err.Append(valuesFromFile.rowNumber + "Секция 6: Регулируемый тариф не равен (ЭОТ * процент)/100 " +
               //     Convert.ToDecimal(reg_tarif) + " не равно " + Math.Round(checkt, kolZn));
            }
            #endregion

            #region 9. Код единицы измерения расхода (из справочника)
            string nzp_measure = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
            valuesFromFile.sql += nzp_measure + ", ";
            i++;
            #endregion

            #region 10. Расход фактический
            string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход фактический");

            }
            valuesFromFile.sql += ras_fact + ", ";
            i++;
            #endregion

            #region 11. Расход по нормативу
            string ras_norm = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход по нормативу");

            }
            valuesFromFile.sql += ras_norm + ", ";
            i++;
            #endregion

            #region 12. Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)
            string is_pu_calc = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 1, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)");

            }
            valuesFromFile.sql += is_pu_calc + ", ";
            i++;
            #endregion

            #region 13. Сумма начисления
            //check = Convert.ToDecimal(eot) * Convert.ToDecimal(ras_fact);
            //check = Decimal.Round(check, 2,MidpointRounding.ToEven);
            //string sum_nach = CheckType.CheckDecimal(vals[i], true, false, null, null, ref ret); 
            string sum_nach = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret); // ослабил для Губкина
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма начисления");

            }
            if (sum_nach == "null") { sum_nach = "0"; };

            valuesFromFile.sql += sum_nach + ", ";
            i++;
            #endregion

            #region 14. Сумма перерасчета начисления за предыдущий период (изменение сальдо)

            string sum_per;
            if (valuesFromFile.vals[i] != "")
                sum_per = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            else
                sum_per = CheckType.CheckDecimal("0.00", true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета начисления за предыдущий период (изменение сальдо)");

            }
            valuesFromFile.sql += sum_per + ", ";
            i++;
            #endregion

            #region 15. Сумма дотации
            string sum_dot = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма дотации");
            }

            if (sum_dot == "null") { sum_dot = "0"; }
            valuesFromFile.sql += sum_dot + ", ";
            i++;
            #endregion

            #region 16. Сумма перерасчета дотации за предыдущий период (за все месяца)
            string sum_dot_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);

            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета дотации за предыдущий период (за все месяца)");

            }
            if (sum_dot_per == "null") { sum_dot_per = "0"; }
            valuesFromFile.sql += sum_dot_per + ", ";
            i++;
            #endregion

            //Разрешаем грузить отрицательные льготы с предупреждением в InfoLog
            #region 17. Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)
            string sum_lgota = CheckType.CheckDecimal2(valuesFromFile, i, false, true, 0, null, ref ret, "Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)", 1);

            if (sum_lgota == "null") { sum_lgota = "0"; }
            valuesFromFile.sql += sum_lgota + ", ";
            i++;
            #endregion

            #region 18. Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)
            string sum_lgota_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)");

            }

            if (sum_lgota_per == "null") { sum_lgota_per = "0"; }
            valuesFromFile.sql += sum_lgota_per + ", ";
            i++;
            #endregion

            #region 19. Сумма СМО
            string smo = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма СМО");
            }

            if (smo == "null") { smo = "0"; }
            valuesFromFile.sql += smo + ", ";
            i++;
            #endregion

            #region 20. Сумма перерасчета  СМО за предыдущий период (за все месяца)
            string smo_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета  СМО за предыдущий период (за все месяца)");

            }
            if (smo_per == "null") { smo_per = "0"; }
            valuesFromFile.sql += smo_per + ", ";
            i++;
            #endregion

            #region 21. Сумма оплаты, поступившие за месяц начислений
            string sum_opl = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма оплаты, поступившие за месяц начислений");

            }
            valuesFromFile.sql += sum_opl + ", ";
            i++;
            #endregion

            #region 22. Признак недействующей услуги, по которой остались долги
            string is_del = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, "Признак недействующей услуги, по которой остались долги ");
            if (!ret.result)
            {
                if (valuesFromFile.vals[i] == "0.00") { ret.result = true; }
                /*else
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Признак удаленности услуги");*/

            }
            valuesFromFile.sql += is_del + ", ";
            i++;
            #endregion

            #region 23. Исходящее сальдо (Долг на окончание месяца)
            //string outsaldo = CheckType.CheckDecimal(vals[i], true, true, null, null, ref ret);
            string outsaldo = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret, true); // Убрал условие из_моней для Губкина, добавил условие из_губкин
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Исходящее сальдо (Долг на окончание месяца)");

            }
            if (outsaldo == "null") { outsaldo = "0"; };
            // округление до 2 знаков                            
            valuesFromFile.sql += outsaldo + ", ";
            i++;
            #endregion

            #region 24. Количество строк – перерасчетов начисления по услуге

            string rows = CheckType.CheckInt2(valuesFromFile, i, true, 0, null, ref ret, "Количество строк – перерасчетов начисления по услуге");
            if (!ret.result)
            {
                rows = "0";
            }
            valuesFromFile.sql += rows + ", ";
            i++;
            #endregion

            #region 25. Номер методики расчета

            string met_calc = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Номер методики расчета");
            valuesFromFile.sql += met_calc + ", ";
            i++;
            #endregion

            #region 26. Платежный код
            string pkod = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Платежный код");
            }
            valuesFromFile.sql += pkod + ", ";
            i++;
            #endregion

            //nzp_kvar, nzp_supp
            valuesFromFile.sql += "null,null);";
            #endregion
        }

        /// <summary>
        /// Формат 1.3.6  Загрузка 6 секции "Информация об оказываемых услугах"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection06_FileServ_v_136(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 6;

            if (valuesFromFile.vals.Length < 32)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация об оказываемых услугах, количество полей = " + valuesFromFile.vals.Length + " вместо 26 ");
                return;
            }

            #region Загрузка 6 секции
            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                "(nzp_file, ls_id, dog_id, nzp_serv, sum_insaldo, eot, reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_nach, sum_reval, " +
                " sum_subsidy, sum_subsidyp, sum_lgota, sum_lgotap,sum_smo, sum_smop, sum_money, is_del, sum_outsaldo, servp_row_number, met_calc, pkod, " +
                " id_serv_epd, sum_type_epd, sum_recalc, sum_perekidka, sum_uch_nedop, num_hour_nedop, " +
                " nzp_kvar, nzp_supp) " +
                " VALUES (";
            int i = 1;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";


            #region 2. № ЛС в системе поставщика
            string id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");
            i++;
            valuesFromFile.sql += id + ", ";
            #endregion

            #region 3. Код договора
            string dog_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код договора");
            valuesFromFile.sql += dog_id + ", ";
            i++;
            #endregion

            #region 4. Код услуги (из справочника)
            string nzp_serv = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код услуги (из справочника)");
            valuesFromFile.sql += nzp_serv + ", ";
            i++;
            #endregion

            #region 5. Входящее сальдо (Долг на начало месяца)
            string saldo_in = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Входящее сальдо (Долг на начало месяца)");

            }
            valuesFromFile.sql += saldo_in + ", ";
            i++;
            #endregion

            #region 6. Экономически обоснованный тариф
            string eot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");

            }
            valuesFromFile.sql += eot + ", ";
            i++;
            #endregion

            #region 7. Процент регулируемого тарифа от экономически обоснованного
            string reg_tarif_percent = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Процент регулируемого тарифа от экономически обоснованного");

            }

            valuesFromFile.sql += reg_tarif_percent + ", ";
            i++;
            #endregion

            #region  8. Регулируемый тариф
            string reg_tarif = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Регулируемый тариф ");

            }
            valuesFromFile.sql += reg_tarif + ", ";
            i++;

            //проверка параметра 8
            decimal checkt = Convert.ToDecimal(eot) * Convert.ToDecimal(reg_tarif_percent) / 100;
            int kolZn = reg_tarif.Substring(reg_tarif.IndexOf(".") + 1).Length;
            if (Math.Abs( (Math.Round(checkt, kolZn) - Convert.ToDecimal(reg_tarif)))>Math.Abs(checkt/100))
            {
                MonitorLog.WriteLog(valuesFromFile.rowNumber + "Секция 6: Регулируемый тариф не равен (ЭОТ * процент)/100 " +
                    Convert.ToDecimal(reg_tarif) + " не равно " + Math.Round(checkt, kolZn), MonitorLog.typelog.Info, true);
            //    valuesFromFile.err.Append(valuesFromFile.rowNumber + "Секция 6: Регулируемый тариф не равен (ЭОТ * процент)/100 " +
            //        Convert.ToDecimal(reg_tarif) + " не равно " + Math.Round(checkt, kolZn));
            }
            #endregion

            #region 9. Код единицы измерения расхода (из справочника)
            string nzp_measure = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
            valuesFromFile.sql += nzp_measure + ", ";
            i++;
            #endregion

            #region 10. Расход фактический
            string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход фактический");

            }
            valuesFromFile.sql += ras_fact + ", ";
            i++;
            #endregion

            #region 11. Расход по нормативу
            string ras_norm = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход по нормативу");

            }
            valuesFromFile.sql += ras_norm + ", ";
            i++;
            #endregion

            #region 12. Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)
            string is_pu_calc = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 1, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)");

            }
            valuesFromFile.sql += is_pu_calc + ", ";
            i++;
            #endregion

            #region 13. Сумма начисления
            //check = Convert.ToDecimal(eot) * Convert.ToDecimal(ras_fact);
            //check = Decimal.Round(check, 2,MidpointRounding.ToEven);
            //string sum_nach = CheckType.CheckDecimal(vals[i], true, false, null, null, ref ret); 
            string sum_nach = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret); // ослабил для Губкина
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма начисления");

            }
            if (sum_nach == "null") { sum_nach = "0"; };

            valuesFromFile.sql += sum_nach + ", ";
            i++;
            #endregion

            #region 14. Сумма перерасчета начисления за предыдущий период (изменение сальдо)

            string sum_per;
            if (valuesFromFile.vals[i] != "")
                sum_per = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            else
                sum_per = CheckType.CheckDecimal("0.00", true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета начисления за предыдущий период (изменение сальдо)");

            }
            valuesFromFile.sql += sum_per + ", ";
            i++;
            #endregion

            #region 15. Сумма дотации
            string sum_dot = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма дотации");
            }

            if (sum_dot == "null") { sum_dot = "0"; }
            valuesFromFile.sql += sum_dot + ", ";
            i++;
            #endregion

            #region 16. Сумма перерасчета дотации за предыдущий период (за все месяца)
            string sum_dot_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);

            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета дотации за предыдущий период (за все месяца)");

            }
            if (sum_dot_per == "null") { sum_dot_per = "0"; }
            valuesFromFile.sql += sum_dot_per + ", ";
            i++;
            #endregion

            //Разрешаем грузить отрицательные льготы с предупреждением в InfoLog
            #region 17. Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)
            string sum_lgota = CheckType.CheckDecimal2(valuesFromFile, i,  false, true, 0, null, ref ret, "Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)", 1);
            
            if (sum_lgota == "null") { sum_lgota = "0"; }
            valuesFromFile.sql += sum_lgota + ", ";
            i++;
            #endregion

            #region 18. Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)
            string sum_lgota_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)");

            }

            if (sum_lgota_per == "null") { sum_lgota_per = "0"; }
            valuesFromFile.sql += sum_lgota_per + ", ";
            i++;
            #endregion

            #region 19. Сумма СМО
            string smo = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма СМО");
            }

            if (smo == "null") { smo = "0"; }
            valuesFromFile.sql += smo + ", ";
            i++;
            #endregion

            #region 20. Сумма перерасчета  СМО за предыдущий период (за все месяца)
            string smo_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета  СМО за предыдущий период (за все месяца)");

            }
            if (smo_per == "null") { smo_per = "0"; }
            valuesFromFile.sql += smo_per + ", ";
            i++;
            #endregion

            #region 21. Сумма оплаты, поступившие за месяц начислений
            string sum_opl = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма оплаты, поступившие за месяц начислений");

            }
            valuesFromFile.sql += sum_opl + ", ";
            i++;
            #endregion

            #region 22. Признак недействующей услуги, по которой остались долги
            string is_del = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, "Признак недействующей услуги, по которой остались долги ");
            if (!ret.result)
            {
                if (valuesFromFile.vals[i] == "0.00") { ret.result = true; }
                /*else
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Признак удаленности услуги");*/

            }
            valuesFromFile.sql += is_del + ", ";
            i++;
            #endregion

            #region 23. Исходящее сальдо (Долг на окончание месяца)
            //string outsaldo = CheckType.CheckDecimal(vals[i], true, true, null, null, ref ret);
            string outsaldo = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret, true); // Убрал условие из_моней для Губкина, добавил условие из_губкин
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Исходящее сальдо (Долг на окончание месяца)");

            }
            if (outsaldo == "null") { outsaldo = "0"; };
            // округление до 2 знаков                            
            valuesFromFile.sql += outsaldo + ", ";
            i++;
            #endregion

            #region 24. Количество строк – перерасчетов начисления по услуге

            string rows = CheckType.CheckInt2(valuesFromFile, i, true, 0, null, ref ret, "Количество строк – перерасчетов начисления по услуге");
            if (!ret.result)
            {
                rows = "0";
            }
            valuesFromFile.sql += rows + ", ";
            i++;
            #endregion

            #region 25. Номер методики расчета

            string met_calc = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Номер методики расчета");
            valuesFromFile.sql += met_calc + ", ";
            i++;
            #endregion

            #region 26. Платежный код
            string pkod = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Платежный код");
            }
            valuesFromFile.sql += pkod + ", ";
            i++;
            #endregion

            #region 27. Порядковый номер услуги в ЕПД

            string id_serv_epd = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Порядковый номер услуги в ЕПД");
            valuesFromFile.sql += id_serv_epd + ", ";
            i++;
            #endregion Порядковый номер услуги в ЕПД

            #region 28. Тип суммы к оплате для ЕПД

            string sum_type_epd = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Тип суммы к оплате для ЕПД");
            valuesFromFile.sql += sum_type_epd + ", ";
            i++;
            #endregion Тип суммы к оплате для ЕПД

            #region 29. Сумма перерасчета
            string sum_recalc = CheckType.CheckDecimal2(valuesFromFile, i, false, false, null, null, ref ret, "Сумма перерасчета");
            valuesFromFile.sql += sum_recalc + ", ";
            i++;
            #endregion Сумма перерасчета

            #region 30. Сумма перекидки
            string sum_perekidka = CheckType.CheckDecimal2(valuesFromFile, i, false, false, null, null, ref ret, "Сумма перекидки");
            valuesFromFile.sql += sum_perekidka + ", ";
            i++;
            #endregion Сумма перекидки

            #region 31. Сумма учтенной недопоставки
            string sum_uch_nedop = CheckType.CheckDecimal2(valuesFromFile, i, false, false, null, null, ref ret, "Сумма учтенной недопоставки");
            valuesFromFile.sql += sum_uch_nedop + ", ";
            i++;
            #endregion Сумма учтенной недопоставки

            #region 32. Количество часов недопоставки
            string num_hour_nedop = CheckType.CheckDecimal2(valuesFromFile, i, false, false, null, null, ref ret, "Количество часов недопоставки");
            valuesFromFile.sql += num_hour_nedop + ", ";
            i++;
            #endregion Количество часов недопоставки

            //nzp_kvar, nzp_supp
            valuesFromFile.sql += "null, null);";
            #endregion
        }

        /// <summary>
        /// Формат 1.3.8  Загрузка 6 секции "Информация об оказываемых услугах"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection06_FileServ_v_138(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 6;

            if (valuesFromFile.vals.Length < 33)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация об оказываемых услугах, количество полей = " + valuesFromFile.vals.Length + " вместо 26 ");
                return;
            }

            //valuesFromFile.sql =
            //    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
            //    "(nzp_file, ls_id, dog_id, nzp_serv, sum_insaldo, eot, reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_nach, sum_reval, " +
            //    " sum_subsidy, sum_subsidyp, sum_lgota, sum_lgotap,sum_smo, sum_smop, sum_money, is_del, sum_outsaldo, servp_row_number, met_calc, pkod, " +
            //    " id_serv_epd, sum_type_epd, sum_recalc, sum_perekidka, sum_uch_nedop, num_hour_nedop, " +
            //    " sum_to_payment,nzp_kvar, nzp_supp) " +
            //    " VALUES (";

            #region Загрузка 6 секции
            
            int i = 1;

            //nzp_file
            //valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";


            #region 2. № ЛС в системе поставщика
            string id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");
            i++;
            //valuesFromFile.sql += id + ", ";
            #endregion

            #region 3. Код договора
            string dog_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код договора");
            //valuesFromFile.sql += dog_id + ", ";
            i++;
            #endregion

            #region 4. Код услуги (из справочника)
            string nzp_serv = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код услуги (из справочника)");
            //valuesFromFile.sql += nzp_serv + ", ";
            i++;
            #endregion

            #region 5. Входящее сальдо (Долг на начало месяца)
            string saldo_in = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Входящее сальдо (Долг на начало месяца)");

            }
            //valuesFromFile.sql += saldo_in + ", ";
            i++;
            #endregion

            #region 6. Экономически обоснованный тариф
            string eot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");

            }
            //valuesFromFile.sql += eot + ", ";
            i++;
            #endregion

            #region 7. Процент регулируемого тарифа от экономически обоснованного
            string reg_tarif_percent = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Процент регулируемого тарифа от экономически обоснованного");

            }

            //valuesFromFile.sql += reg_tarif_percent + ", ";
            i++;
            #endregion

            #region  8. Регулируемый тариф
            string reg_tarif = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Регулируемый тариф ");

            }
            //valuesFromFile.sql += reg_tarif + ", ";
            i++;

            //проверка параметра 8
            decimal checkt = Convert.ToDecimal(eot) * Convert.ToDecimal(reg_tarif_percent) / 100;
            int kol_zn = reg_tarif.Substring(reg_tarif.IndexOf(".") + 1).Length;
            if (Math.Round(checkt, kol_zn) != Convert.ToDecimal(reg_tarif))
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + "Секция 6: Регулируемый тариф не равен (ЭОТ * процент)/100 " +
                    Convert.ToDecimal(reg_tarif) + " не равно " + Math.Round(checkt, kol_zn));
            }
            #endregion

            #region 9. Код единицы измерения расхода (из справочника)
            string nzp_measure = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
            //valuesFromFile.sql += nzp_measure + ", ";
            i++;
            #endregion

            #region 10. Расход фактический
            //string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret); // теперь можно отрицательный расход
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход фактический");

            }
            //valuesFromFile.sql += ras_fact + ", ";
            i++;
            #endregion

            #region 11. Расход по нормативу
            string ras_norm = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход по нормативу");

            }
            //valuesFromFile.sql += ras_norm + ", ";
            i++;
            #endregion

            #region 12. Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)
            string is_pu_calc = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 1, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)");

            }
            //valuesFromFile.sql += is_pu_calc + ", ";
            i++;
            #endregion

            #region 13. Сумма начисления
            //check = Convert.ToDecimal(eot) * Convert.ToDecimal(ras_fact);
            //check = Decimal.Round(check, 2,MidpointRounding.ToEven);
            //string sum_nach = CheckType.CheckDecimal(vals[i], true, false, null, null, ref ret); 
            string sum_nach = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret); // ослабил для Губкина
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма начисления");

            }
            if (sum_nach == "null") { sum_nach = "0"; };

            //valuesFromFile.sql += sum_nach + ", ";
            i++;
            #endregion

            #region 14. Сумма перерасчета начисления за предыдущий период (изменение сальдо)

            string sum_per;
            if (valuesFromFile.vals[i] != "")
                sum_per = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            else
                sum_per = CheckType.CheckDecimal("0.00", true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета начисления за предыдущий период (изменение сальдо)");

            }
            //valuesFromFile.sql += sum_per + ", ";
            i++;
            #endregion

            #region 15. Сумма дотации
            string sum_dot = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма дотации");
            }

            if (sum_dot == "null") { sum_dot = "0"; }
            //valuesFromFile.sql += sum_dot + ", ";
            i++;
            #endregion

            #region 16. Сумма перерасчета дотации за предыдущий период (за все месяца)
            string sum_dot_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);

            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета дотации за предыдущий период (за все месяца)");

            }
            if (sum_dot_per == "null") { sum_dot_per = "0"; }
            //valuesFromFile.sql += sum_dot_per + ", ";
            i++;
            #endregion

            //Разрешаем грузить отрицательные льготы с предупреждением в InfoLog
            #region 17. Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)
            string sum_lgota = CheckType.CheckDecimal2(valuesFromFile, i, false, true, 0, null, ref ret, "Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)", 1);

            if (sum_lgota == "null") { sum_lgota = "0"; }
            //valuesFromFile.sql += sum_lgota + ", ";
            i++;
            #endregion

            #region 18. Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)
            string sum_lgota_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)");

            }

            if (sum_lgota_per == "null") { sum_lgota_per = "0"; }
            //valuesFromFile.sql += sum_lgota_per + ", ";
            i++;
            #endregion

            #region 19. Сумма СМО
            string smo = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма СМО");
            }

            if (smo == "null") { smo = "0"; }
            //valuesFromFile.sql += smo + ", ";
            i++;
            #endregion

            #region 20. Сумма перерасчета  СМО за предыдущий период (за все месяца)
            string smo_per = CheckType.CheckDecimal(valuesFromFile.vals[i], false, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета  СМО за предыдущий период (за все месяца)");

            }
            if (smo_per == "null") { smo_per = "0"; }
            //valuesFromFile.sql += smo_per + ", ";
            i++;
            #endregion

            #region 21. Сумма оплаты, поступившие за месяц начислений
            string sum_opl = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма оплаты, поступившие за месяц начислений");

            }
            //valuesFromFile.sql += sum_opl + ", ";
            i++;
            #endregion

            #region 22. Признак недействующей услуги, по которой остались долги
            string is_del = CheckType.CheckInt2(valuesFromFile, i, true, 0, 1, ref ret, "Признак недействующей услуги, по которой остались долги ");
            if (!ret.result)
            {
                if (valuesFromFile.vals[i] == "0.00") { ret.result = true; }
                /*else
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Признак удаленности услуги");*/

            }
            //valuesFromFile.sql += is_del + ", ";
            i++;
            #endregion

            #region 23. Исходящее сальдо (Долг на окончание месяца)
            //string outsaldo = CheckType.CheckDecimal(vals[i], true, true, null, null, ref ret);
            string outsaldo = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret, true); // Убрал условие из_моней для Губкина, добавил условие из_губкин
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Исходящее сальдо (Долг на окончание месяца)");

            }
            if (outsaldo == "null") { outsaldo = "0"; };
            // округление до 2 знаков                            
            //valuesFromFile.sql += outsaldo + ", ";
            i++;
            #endregion

            #region 24. Количество строк – перерасчетов начисления по услуге

            string rows = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Количество строк – перерасчетов начисления по услуге");
            if (!ret.result)
            {
                rows = "0";
            }
            //valuesFromFile.sql += rows + ", ";
            i++;
            #endregion

            #region 25. Номер методики расчета

            string met_calc = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Номер методики расчета");
            //valuesFromFile.sql += met_calc + ", ";
            i++;
            #endregion

            #region 26. Платежный код
            string pkod = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Платежный код");
            }
            //valuesFromFile.sql += pkod + ", ";
            i++;
            #endregion

            #region 27. Порядковый номер услуги в ЕПД

            string id_serv_epd = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Порядковый номер услуги в ЕПД");
            //valuesFromFile.sql += id_serv_epd + ", ";
            i++;
            #endregion Порядковый номер услуги в ЕПД

            #region 28. Тип суммы к оплате для ЕПД

            string sum_type_epd = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Тип суммы к оплате для ЕПД");
            //valuesFromFile.sql += sum_type_epd + ", ";
            i++;
            #endregion Тип суммы к оплате для ЕПД

            #region 29. Сумма перерасчета
            string sum_recalc = CheckType.CheckDecimal2(valuesFromFile, i, false, false, null, null, ref ret, "Сумма перерасчета");
            //valuesFromFile.sql += sum_recalc + ", ";
            i++;
            #endregion Сумма перерасчета

            #region 30. Сумма перекидки
            string sum_perekidka = CheckType.CheckDecimal2(valuesFromFile, i, false, false, null, null, ref ret, "Сумма перекидки");
            //valuesFromFile.sql += sum_perekidka + ", ";
            i++;
            #endregion Сумма перекидки

            #region 31. Сумма учтенной недопоставки
            string sum_uch_nedop = CheckType.CheckDecimal2(valuesFromFile, i, false, false, null, null, ref ret, "Сумма учтенной недопоставки");
            //valuesFromFile.sql += sum_uch_nedop + ", ";
            i++;
            #endregion Сумма учтенной недопоставки

            #region 32. Количество часов недопоставки
            string num_hour_nedop = CheckType.CheckDecimal2(valuesFromFile, i, false, false, null, null, ref ret, "Количество часов недопоставки");
            //valuesFromFile.sql += num_hour_nedop + ", ";
            i++;
            #endregion Количество часов недопоставки

            #region 33. Начислено к оплате
            string sum_to_payment = CheckType.CheckDecimal2(valuesFromFile, i, true, false, null, null, ref ret, "Начислено к оплате");
            //valuesFromFile.sql += sum_to_payment + ", ";
            i++;
            #endregion Начислено к оплате

            //nzp_kvar, nzp_supp
            //valuesFromFile.sql += "null, null);";

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
                "(nzp_file, ls_id, dog_id, nzp_serv, sum_insaldo, eot, reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_nach, sum_reval, " +
                " sum_subsidy, sum_subsidyp, sum_lgota, sum_lgotap,sum_smo, sum_smop, sum_money, is_del, sum_outsaldo, servp_row_number, met_calc, pkod, " +
                " id_serv_epd, sum_type_epd, sum_recalc, sum_perekidka, sum_uch_nedop, num_hour_nedop, " +
                " sum_to_payment,nzp_kvar, nzp_supp) " +
                " VALUES (" + valuesFromFile.finder.nzp_file + ", " + id + ", " + dog_id + ", " + nzp_serv + ", " + saldo_in + ", " + eot + ", " + reg_tarif_percent + ", " +
                reg_tarif + ", " + nzp_measure + ", " + ras_fact + ", " + ras_norm + ", " + is_pu_calc + ", " + sum_nach + ", " + sum_per + ", " +
                sum_dot + ", " + sum_dot_per + ", " + sum_lgota + ", " + sum_lgota_per + ", " + smo + ", " + smo_per + ", " + sum_opl + ", " + is_del + ", " +
                outsaldo + ", " + rows + ", " + met_calc + ", " + pkod + ", " + id_serv_epd + ", " + sum_type_epd + ", " + sum_recalc + ", " + sum_perekidka + ", " +
                sum_uch_nedop + ", " + num_hour_nedop + ", " + sum_to_payment + ", null, null);";
            #endregion
        }

        /// <summary>
        ///  Загрузка 7 секции "Информация о параметрах лицевых счетов в месяце перерасчета"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection07_FileKvarp(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 7;

            if (valuesFromFile.vals.Length < 32)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация о параметрах лицевых счетов в месяце перерасчета, количество полей = " + valuesFromFile.vals.Length + " вместо 32 ");
                return;
            }

            #region загрузка 7 секции
            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_kvarp( id, reval_month, nzp_file, fam, ima, otch, birth_date, nkvar, nkvar_n, open_date, opening_osnov, close_date,  " +
                " closing_osnov, kol_gil, kol_vrem_prib, kol_vrem_ub, room_number, total_square, living_square, otapl_square, naim_square, is_communal, " +
                " is_el_plita, is_gas_plita, is_gas_colonka, is_fire_plita, gas_type, water_type, hotwater_type, canalization_type, is_open_otopl, params, " +
                " nzp_dom, nzp_kvar, comment, nzp_status)   " +
                " VALUES( ";
            int i = 1;


            //2. 2. Месяц и год перерасчета
            string my_per = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Месяц и год перерасчета");
            //sql += my_per + ", ";
            i++;


            //nzp_file
            //sql += finder.nzp_file + ", ";


            //3. № ЛС в системе поставщика
            string ls = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");

            i++;

            valuesFromFile.sql += ls + ", " + my_per + ", " + valuesFromFile.finder.nzp_file + ", ";

            //4. Фамилия квартиросъемщика
            string fam = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Фамилия квартиросъемщика");
            valuesFromFile.sql += fam + ", ";
            i++;


            //5. Имя квартиросъемщика
            string name = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Имя квартиросъемщика");
            valuesFromFile.sql += name + ", ";
            i++;

            //6. Отчество квартиросъемщика
            string patronymic = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "Отчество квартиросъемщика");
            valuesFromFile.sql += patronymic + ", ";
            i++;


            //7. Дата рождения квартиросъемщика
            string patronym = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата рождения квартиросъемщика");
            valuesFromFile.sql += patronym + ", ";
            i++;


            //8. Квартира
            string kvar = CheckType.CheckText2(valuesFromFile, i, false, 10, ref ret, "Квартира");
            valuesFromFile.sql += kvar + ", ";
            i++;

            //9. Комната
            string kom = CheckType.CheckText2(valuesFromFile, i, false, 3, ref ret, "Комната");
            valuesFromFile.sql += kom + ", ";
            i++;

            //10. Дата открытия ЛС
            string dat_openLs = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата открытия ЛС");
            valuesFromFile.sql += dat_openLs + ", ";
            i++;

            //11. Основание открытия ЛС
            string osnov_ls = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Основание открытия ЛС");
            valuesFromFile.sql += osnov_ls + ", ";
            i++;

            //12. Дата закрытия ЛС
            string datCloseLs = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата закрытия ЛС");
            valuesFromFile.sql += datCloseLs + ", ";
            i++;

            //13. Основание закрытия ЛС
            string osnovClose = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Основание закрытия ЛС");
            valuesFromFile.sql += osnovClose + ", ";
            i++;

            //14. Количество проживающих
            string countLiv = CheckType.CheckInt2(valuesFromFile, i, true, 0, null, ref ret, "Количество проживающих");
            if (countLiv == "null") { countLiv = "0"; };
            valuesFromFile.sql += countLiv + ", ";
            i++;

            //15. Количество врем. Прибывших жильцов
            string countTempLiv = CheckType.CheckInt2(valuesFromFile, i, true, 0, null, ref ret, "Количество врем. Прибывших жильцов");
            if (countTempLiv == "null") { countTempLiv = "0"; };

            valuesFromFile.sql += countTempLiv + ", ";
            i++;

            //16. Количество  врем. Убывших жильцов
            string countTempOut = CheckType.CheckInt2(valuesFromFile, i, false, 0, null, ref ret, "Количество  врем. Убывших жильцов");
            if (countTempOut == "null") { countTempOut = "0"; };
            valuesFromFile.sql += countTempOut + ", ";
            i++;

            //17. Количество комнат
            string countRoom = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Количество комнат");
            if (countRoom == "null") { countRoom = "0"; };
            valuesFromFile.sql += countRoom + ", ";
            i++;


            //18. Общая площадь
            string totSq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Общая площадь");

            }
            valuesFromFile.sql += totSq + ", ";
            i++;

            //19. Жилая площадь
            string livSq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Жилая площадь");

            }
            valuesFromFile.sql += livSq + ", ";
            i++;


            //20. Отапливаемая площадь
            string warmSq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Отапливаемая площадь");

            }
            valuesFromFile.sql += warmSq + ", ";
            i++;

            //21. Площадь для найма
            string rentSq = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Площадь для найма");

            }
            valuesFromFile.sql += rentSq + ", ";
            i++;

            //22. Признак коммунальной квартиры(1-да, 0 –нет)
            string comKv = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Признак коммунальной квартиры(1-да, 0 –нет)");
            valuesFromFile.sql += comKv + ", ";
            i++;


            //23. Наличие эл. Плиты (1-да, 0 –нет)
            string is_plate = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие эл. Плиты (1-да, 0 –нет)");
            valuesFromFile.sql += is_plate + ", ";
            i++;

            //24. Наличие газовой плиты (1-да, 0 –нет)
            string is_plateGas = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие газовой плиты (1-да, 0 –нет)");
            valuesFromFile.sql += is_plateGas + ", ";
            i++;


            //25. Наличие газовой колонки (1-да, 0 –нет
            string is_geyser = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие газовой колонки (1-да, 0 –нет");
            valuesFromFile.sql += is_geyser + ", ";
            i++;

            //26. Наличие огневой плиты (1-да, 0 –нет)
            string is_plateFire = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие огневой плиты (1-да, 0 –нет)");
            valuesFromFile.sql += is_plateFire + ", ";
            i++;

            //27. Код типа жилья по газоснабжению (из справочника)
            string codeGas = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Код типа жилья по газоснабжению (из справочника)");
            if (codeGas == "null") { codeGas = "0"; };
            valuesFromFile.sql += codeGas + ", ";
            i++;


            //28. Код типа жилья по водоснабжению (из справочника)
            string codWater = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Код типа жилья по водоснабжению (из справочника)");
            if (codWater == "null") { codWater = "0"; };
            valuesFromFile.sql += codWater + ", ";
            i++;

            //29. Код типа жилья по горячей воде (из справочника)
            string codWaterHot = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Код типа жилья по горячей воде (из справочника)");
            if (codWaterHot == "null") { codWaterHot = "0"; };
            valuesFromFile.sql += codWaterHot + ", ";
            i++;

            //30. Код типа жилья по канализации (из справочника)
            string codCanal = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Код типа жилья по канализации (из справочника)");
            if (codCanal == "null") { codCanal = "0"; };
            valuesFromFile.sql += codCanal + ", ";
            i++;

            //31. Наличие забора из открытой системы отопления (1-да, 0 –нет)
            string codOpen = CheckType.CheckInt2(valuesFromFile, i, false, 0, 1, ref ret, "Наличие забора из открытой системы отопления (1-да, 0 –нет)");
            valuesFromFile.sql += codOpen + ", ";
            i++;

            //32. Дополнительные характеристики ЛС (заполняется в соответствии с форматом задания значений параметров)
            string additionalHar = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, "Дополнительные характеристики ЛС");
            valuesFromFile.sql += additionalHar + ", ";
            i++;


            //nzp_dom, nzp_kvar, comment, nzp_status
            valuesFromFile.sql += " null,null,null,null); ";

            #endregion
        }


        /// <summary>
        ///  Загрузка 8 секции "Информация о перерасчетах начислений по услугам"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection08_FileServp(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 8;

            if (valuesFromFile.vals.Length < 16)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация о перерасчетах начислений по услугам, количество полей = " + valuesFromFile.vals.Length + " вместо 16 ");
                return;
            }

            #region Загрузка 8 секции
            valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_servp" +
                                 " ( nzp_file, reval_month, ls_id, supp_id, nzp_serv, eot, reg_tarif_percent, reg_tarif, nzp_measure,  " +
                                 " fact_rashod, norm_rashod, is_pu_calc, sum_reval, sum_subsidyp, sum_lgotap, sum_smop, nzp_kvar, nzp_supp) " +
                                 "VALUES (";
            int i = 1;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";


            //2. 2. Месяц и год перерасчета
            string my_per = CheckType.CheckDateTime(valuesFromFile.vals[i], false, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Месяц и год перерасчета");
            }
            valuesFromFile.sql += my_per + ", ";
            i++;

            //3. № ЛС в системе поставщика
            string ls = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
            if (!ret.result)
            {
                ls = CheckType.CheckText(valuesFromFile.vals[i], false, 20, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "№ ЛС в системе поставщика");
                }
            }
            valuesFromFile.sql += ls + ", ";
            i++;

            //4. Код поставщика.
            string supp_id = CheckType.CheckInt(valuesFromFile.vals[i], false, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код поставщика");
            }
            valuesFromFile.sql += supp_id + ", ";
            i++;

            //5. Код услуги (из справочника)
            string nzp_serv = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код услуги (из справочника)");
            }
            valuesFromFile.sql += nzp_serv + ", ";
            i++;

            //6. Экономически обоснованный тариф 
            string eot = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");
            }
            valuesFromFile.sql += eot + ", ";
            i++;


            //7. Процент регулируемого тарифа от экономически обоснованного
            string reg_tarif_percent = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Процент регулируемого тарифа от экономически обоснованного");
            }
            valuesFromFile.sql += reg_tarif_percent + ", ";
            i++;

            //8. Регулируемый тариф                        
            decimal? check = null;
            try
            {
                decimal checkt = Convert.ToDecimal(eot) * Convert.ToDecimal(reg_tarif_percent) / 100;
                checkt = Decimal.Round(checkt, 2);
                check = checkt;
                //проверка параметра 6
                eot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, Convert.ToDecimal(check), null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");
                }
            }
            catch (Exception) { }

            //проверка параметра 8
            string reg_tarif = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, check, check, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Регулируемый тариф ");
            }
            valuesFromFile.sql += reg_tarif + ", ";
            i++;

            //9. Код единицы измерения расхода (из справочника)                            
            string nzp_measure = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код единицы измерения расхода (из справочника) ");
            }
            valuesFromFile.sql += nzp_measure + ", ";
            i++;

            //10. Расход фактический
            string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход фактический");
            }
            valuesFromFile.sql += ras_fact + ", ";
            i++;

            //11. Расход по нормативу
            string ras_norm = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход по нормативу");
            }
            valuesFromFile.sql += ras_norm + ", ";
            i++;

            //12. Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)
            string is_pu_calc = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 1, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Вид расчета по прибору учета");
            }
            valuesFromFile.sql += is_pu_calc + ", ";
            i++;

            //13. Сумма перерасчета начисления за месяц перерасчета
            string sumPerMonthPer = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета начисления за месяц перерасчета");
            }
            valuesFromFile.sql += sumPerMonthPer + ", ";
            i++;

            //14. Сумма перерасчета дотации за месяц перерасчета
            string sumPerSubsMothPer = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета дотации за месяц перерасчета");
            }
            valuesFromFile.sql += sumPerSubsMothPer + ", ";
            i++;

            //15. Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)
            string sumPerLgot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)");
            }
            valuesFromFile.sql += sumPerLgot + ", ";
            i++;

            //16. Сумма перерасчета СМО за месяц перерасчета
            string sumPerSmo = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета СМО за месяц перерасчета");
            }
            valuesFromFile.sql += sumPerSmo + ", ";
            i++;

            //nzp_kvar, nzp_supp
            valuesFromFile.sql += "null,null);";
            #endregion
        }


        /// <summary>
        ///  Формат 1.3.2  Загрузка 8 секции "Информация о перерасчетах начислений по услугам"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection08_FileServp_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 8;

            if (valuesFromFile.vals.Length < 16)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Информация о перерасчетах начислений по услугам, количество полей = " + valuesFromFile.vals.Length + " вместо 16 ");
                return;
            }

            #region Загрузка 8 секции
            valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_servp" +
                                 " ( nzp_file, reval_month, ls_id, dog_id, nzp_serv, eot, reg_tarif_percent, reg_tarif, nzp_measure,  " +
                                 " fact_rashod, norm_rashod, is_pu_calc, sum_reval, sum_subsidyp, sum_lgotap, sum_smop, nzp_kvar, nzp_supp) " +
                                 "VALUES (";
            int i = 1;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";


            //2. 2. Месяц и год перерасчета
            string my_per = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Месяц и год перерасчета");
            valuesFromFile.sql += my_per + ", ";
            i++;

            //3. № ЛС в системе поставщика
            string ls = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");

            valuesFromFile.sql += ls + ", ";
            i++;

            //4. Код договора
            string dog_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код договора");
            valuesFromFile.sql += dog_id + ", ";
            i++;

            //5. Код услуги (из справочника)
            string nzp_serv = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код услуги (из справочника)");
            valuesFromFile.sql += nzp_serv + ", ";
            i++;

            //6. Экономически обоснованный тариф 
            string eot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Экономически обоснованный тариф ");
            }
            valuesFromFile.sql += eot + ", ";
            i++;


            //7. Процент регулируемого тарифа от экономически обоснованного
            string reg_tarif_percent = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Процент регулируемого тарифа от экономически обоснованного");
            }
            valuesFromFile.sql += reg_tarif_percent + ", ";
            i++;

            //8. Регулируемый тариф  
            string reg_tarif = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Регулируемый тариф ");
            }
            valuesFromFile.sql += reg_tarif + ", ";
            i++;

           // проверка параметра 8
            decimal checkt = Convert.ToDecimal(eot) * Convert.ToDecimal(reg_tarif_percent) / 100;
            int kolZn = reg_tarif.Substring(reg_tarif.IndexOf(".") + 1).Length;

            if (Math.Abs(Math.Round(checkt, kolZn) - Convert.ToDecimal(reg_tarif)) > Math.Abs(checkt/100) )
            {
                MonitorLog.WriteLog(valuesFromFile.rowNumber + "Секция 8: Регулируемый тариф не равен (ЭОТ * процент)/100 " +
                    Convert.ToDecimal(reg_tarif) + " не равно " + Math.Round(checkt, kolZn), MonitorLog.typelog.Info, true);
                //valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Секция 8: Регулируемый тариф не равен (ЭОТ * процент)/100  " +
                //    Convert.ToDecimal(reg_tarif) + " не равно " + Math.Round(checkt, kolZn));
            }

            //9. Код единицы измерения расхода (из справочника)                            
            string nzp_measure = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
            valuesFromFile.sql += nzp_measure + ", ";
            i++;

            //10. Расход фактический
            //string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            string ras_fact = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null , null, ref ret); // теперь можно отрицательный расход Павленко и Шакиров
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход фактический");
            }
            valuesFromFile.sql += ras_fact + ", ";
            i++;

            //11. Расход по нормативу
            string ras_norm = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Расход по нормативу");
            }
            valuesFromFile.sql += ras_norm + ", ";
            i++;

            //12. Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)
            string is_pu_calc = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, 1, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Вид расчета по прибору учета");
            }
            valuesFromFile.sql += is_pu_calc + ", ";
            i++;

            //13. Сумма перерасчета начисления за месяц перерасчета
            string sumPerMonthPer = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета начисления за месяц перерасчета");
            }
            valuesFromFile.sql += sumPerMonthPer + ", ";
            i++;

            //14. Сумма перерасчета дотации за месяц перерасчета
            string sumPerSubsMothPer = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета дотации за месяц перерасчета");
            }
            valuesFromFile.sql += sumPerSubsMothPer + ", ";
            i++;

            //15. Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)
            string sumPerLgot = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)");
            }
            valuesFromFile.sql += sumPerLgot + ", ";
            i++;

            //16. Сумма перерасчета СМО за месяц перерасчета
            string sumPerSmo = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Сумма перерасчета СМО за месяц перерасчета");
            }
            valuesFromFile.sql += sumPerSmo + ", ";
            i++;

            //nzp_kvar, nzp_supp
            valuesFromFile.sql += "null,null);";
            #endregion
        }


        /// <summary>
        ///  Загрузка 9 секции "Информация об общедомовых приборах учета"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection09_FileOdpu(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 9;

            #region Версия 1.2 (Здесь только счетчики ОДПУ без показаний )

            if (valuesFromFile.vals.Length < 13)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: Информация об общедомовых приборах учета, количество полей = " + valuesFromFile.vals.Length + " вместо 13 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_odpu( nzp_file, dom_id, dom_id_char, local_id, nzp_serv, serv_type,  counter_type, cnt_stage,  " +
                "mmnog, num_cnt, nzp_measure, dat_prov, dat_provnext, doppar) " +
                " VALUES(";

            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем

            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region  2. Уникальный код дома для строки типа 2 – реквизит 3.

            //2. Уникальный код дома для строки типа 2 – реквизит 3.
            //string codeDom = CheckType.CheckInt(vals[i], true, 1, null, ref ret);

            string codeDom = "100";
            string dom_id_char = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, "Уникальный код дома(char)");

            valuesFromFile.sql += codeDom + ", " + dom_id_char + ", ";
            i++;

            #endregion  Уникальный код дома для строки типа 2 – реквизит 3.

            #region 3. Уникальный код прибора в системе поставщика ОДПУ.

            //3. Уникальный код прибора ОДПУ.
            string codeOdpu = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, " Уникальный код прибора учета в системе поставщика ");
            valuesFromFile.sql += codeOdpu + ", ";
            i++;

            #endregion 3. Уникальный код прибора ОДПУ.

            #region 4. Код услуги (из справочника)

            //4. Код услуги (из справочника)
            string codeServ = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "  Код услуги (из справочника)");
            valuesFromFile.sql += codeServ + ", ";
            i++;

            #endregion 4. Код услуги (из справочника)

            #region 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

            //5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
            string typeService = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");
            valuesFromFile.sql += typeService + ", ";
            i++;

            #endregion 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

            #region 6. Тип счетчика

            //6. Тип счетчика 
            string typeCounter = CheckType.CheckText2(valuesFromFile, i, true, 25, ref ret, "Тип счетчика ");
            valuesFromFile.sql += typeCounter + ", ";
            i++;

            #endregion 6. Тип счетчика

            #region 7. Разрядность прибора

            //7. Разрядность прибора 
            string capCounter = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Разрядность прибора ");
            valuesFromFile.sql += capCounter + ", ";
            i++;

            #endregion 7. Разрядность прибора

            #region 8. Повышающий коэффициент (коэффициент трансформации тока)

            //8. Повышающий коэффициент (коэффициент трансформации тока)
            string upIndex = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Повышающий коэффициент (коэффициент трансформации тока) ");
            valuesFromFile.sql += upIndex + ", ";
            i++;

            #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

            #region 9. Заводской номер прибора учета

            //9. Заводской номер прибора учета
            string numCounter = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, "Заводской номер прибора учета");
            valuesFromFile.sql += numCounter + ", ";
            i++;

            #endregion 9. Заводской номер прибора учета

            #region 10. Код единицы измерения расхода (из справочника)

            //12. Код единицы измерения расхода (из справочника)
            string upIndexExp = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
            valuesFromFile.sql += upIndexExp + ", ";
            i++;

            #endregion 10. Код единицы измерения расхода (из справочника)

            #region 11. Дата поверки

            //13. Дата поверки
            string dateCheck = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата поверки");
            valuesFromFile.sql += dateCheck + ", ";
            i++;

            #endregion 11. Дата поверки

            #region 12. Дата следующей поверки

            //14. Дата следующей поверки
            string dateCheckNext = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата следующей поверки");
            valuesFromFile.sql += dateCheckNext + ", ";
            i++;

            #endregion 12. Дата следующей поверки

            #region 13. Дополнительные характеристики (временно в загрузке не участвуют , только в разборе строки )

            //13. Дополнительные характеристики
            string DopCharact = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, "Дополнительные характеристики ОДПУ");

            #region временно не загружаем

            valuesFromFile.sql += DopCharact + " ";
            i++;

            #endregion временно не загружаем



            #endregion 13. Дополнительные характеристики

            #region Завершающие действия для инсерта в спец таблицу

            //nzp_dom, nzp_counter
            valuesFromFile.sql += ");";

            #endregion Завершающие действия для инсерта в спец таблицу

            #endregion Версия 1.2

        }

        public void AddSection09_FileOdpu_v_135(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 9;

            #region Версия 1.2 (Здесь только счетчики ОДПУ без показаний )

            if (valuesFromFile.vals.Length < 14)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: Информация об общедомовых приборах учета, количество полей = " + valuesFromFile.vals.Length + " вместо 14 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_odpu( nzp_file, dom_id, dom_id_char, local_id, nzp_serv, serv_type,  counter_type, cnt_stage,  " +
                "mmnog, num_cnt, nzp_measure, dat_prov, dat_provnext, doppar, type_pu) " +
                " VALUES(";

            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем

            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region  2. Уникальный код дома для строки типа 2 – реквизит 3.

            //2. Уникальный код дома для строки типа 2 – реквизит 3.
            //string codeDom = CheckType.CheckInt(vals[i], true, 1, null, ref ret);

            string codeDom = "-1";
            string dom_id_char = CheckType.CheckText3(valuesFromFile, i, false, 20, ref ret, "Уникальный код дома(char)");

            valuesFromFile.sql += codeDom + ", " + dom_id_char + ", ";
            i++;

            #endregion  Уникальный код дома для строки типа 2 – реквизит 3.

            #region 3. Уникальный код прибора в системе поставщика ОДПУ.

            //3. Уникальный код прибора ОДПУ.
            string codeOdpu = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, " Уникальный код прибора учета в системе поставщика ");
            valuesFromFile.sql += codeOdpu + ", ";
            i++;

            #endregion 3. Уникальный код прибора ОДПУ.

            #region 4. Код услуги (из справочника)

            //4. Код услуги (из справочника)
            string codeServ = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "  Код услуги (из справочника)");
            valuesFromFile.sql += codeServ + ", ";
            i++;

            #endregion 4. Код услуги (из справочника)

            #region 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

            //5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
            string typeService = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");
            valuesFromFile.sql += typeService + ", ";
            i++;

            #endregion 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

            #region 6. Тип счетчика

            //6. Тип счетчика 
            string typeCounter = CheckType.CheckText2(valuesFromFile, i, true, 25, ref ret, "Тип счетчика ");
            if (typeCounter == "null") typeCounter = "-";
            valuesFromFile.sql += typeCounter + ", ";
            i++;

            #endregion 6. Тип счетчика

            #region 7. Разрядность прибора

            //7. Разрядность прибора 
            string capCounter = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Разрядность прибора ");
            valuesFromFile.sql += capCounter + ", ";
            i++;

            #endregion 7. Разрядность прибора

            #region 8. Повышающий коэффициент (коэффициент трансформации тока)

            //8. Повышающий коэффициент (коэффициент трансформации тока)
            string upIndex = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Повышающий коэффициент (коэффициент трансформации тока) ");
            if (upIndex == "null") upIndex = "1";
            valuesFromFile.sql += upIndex + ", ";
            i++;

            #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

            #region 9. Заводской номер прибора учета

            //9. Заводской номер прибора учета
            string numCounter = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, "Заводской номер прибора учета");
            valuesFromFile.sql += numCounter + ", ";
            i++;

            #endregion 9. Заводской номер прибора учета

            #region 10. Код единицы измерения расхода (из справочника)

            //12. Код единицы измерения расхода (из справочника)
            string upIndexExp = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
            valuesFromFile.sql += upIndexExp + ", ";
            i++;

            #endregion 10. Код единицы измерения расхода (из справочника)

            #region 11. Дата поверки

            //13. Дата поверки
            string dateCheck = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата поверки");
            valuesFromFile.sql += dateCheck + ", ";
            i++;

            #endregion 11. Дата поверки

            #region 12. Дата следующей поверки

            //14. Дата следующей поверки
            string dateCheckNext = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата следующей поверки");
            valuesFromFile.sql += dateCheckNext + ", ";
            i++;

            #endregion 12. Дата следующей поверки

            #region 13. Дополнительные характеристики (временно в загрузке не участвуют , только в разборе строки )

            //13. Дополнительные характеристики
            string DopCharact = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, "Дополнительные характеристики ОДПУ");

            #region временно не загружаем

            valuesFromFile.sql += DopCharact + ", ";
            i++;

            #endregion временно не загружаем

            #endregion 13. Дополнительные характеристики

            #region 14.	Тип прибора учета

            string type_pu = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Тип прибора учета ");
            valuesFromFile.sql += type_pu + " ";
            i++;

            #endregion Тип прибора учета

            #region Завершающие действия для инсерта в спец таблицу

            //nzp_dom, nzp_counter
            valuesFromFile.sql += ");";

            #endregion Завершающие действия для инсерта в спец таблицу

            #endregion Версия 1.2

        }

        /// <summary>
        /// Формат 1.3.8.1. Загрузка 9 секции "Информация об общедомовых приборах учета" 
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection09_FileOdpu_v_1381(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 9;

            
            if (valuesFromFile.vals.Length < 15)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: Информация об общедомовых приборах учета, количество полей = " + valuesFromFile.vals.Length + " вместо 14 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_odpu( nzp_file, dom_id, dom_id_char, local_id, nzp_serv, serv_type,  counter_type, cnt_stage,  " +
                "mmnog, num_cnt, nzp_measure, dat_prov, dat_provnext, doppar, type_pu, dat_close) " +
                " VALUES(";

            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем

            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region  2. Уникальный код дома для строки типа 2 – реквизит 3.

            //2. Уникальный код дома для строки типа 2 – реквизит 3.
            //string codeDom = CheckType.CheckInt(vals[i], true, 1, null, ref ret);

            string codeDom = "-1";
            string dom_id_char = CheckType.CheckText3(valuesFromFile, i, false, 20, ref ret, "Уникальный код дома(char)");

            valuesFromFile.sql += codeDom + ", " + dom_id_char + ", ";
            i++;

            #endregion  Уникальный код дома для строки типа 2 – реквизит 3.

            #region 3. Уникальный код прибора в системе поставщика ОДПУ.

            //3. Уникальный код прибора ОДПУ.
            string codeOdpu = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, " Уникальный код прибора учета в системе поставщика ");
            valuesFromFile.sql += codeOdpu + ", ";
            i++;

            #endregion 3. Уникальный код прибора ОДПУ.

            #region 4. Код услуги (из справочника)

            //4. Код услуги (из справочника)
            string codeServ = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "  Код услуги (из справочника)");
            valuesFromFile.sql += codeServ + ", ";
            i++;

            #endregion 4. Код услуги (из справочника)

            #region 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

            //5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
            string typeService = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");
            valuesFromFile.sql += typeService + ", ";
            i++;

            #endregion 5. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

            #region 6. Тип счетчика

            //6. Тип счетчика 
            string typeCounter = CheckType.CheckText2(valuesFromFile, i, true, 25, ref ret, "Тип счетчика ");
            if (typeCounter == "null") typeCounter = "-";
            valuesFromFile.sql += typeCounter + ", ";
            i++;

            #endregion 6. Тип счетчика

            #region 7. Разрядность прибора

            //7. Разрядность прибора 
            string capCounter = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Разрядность прибора ");
            valuesFromFile.sql += capCounter + ", ";
            i++;

            #endregion 7. Разрядность прибора

            #region 8. Повышающий коэффициент (коэффициент трансформации тока)

            //8. Повышающий коэффициент (коэффициент трансформации тока)
            string upIndex = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Повышающий коэффициент (коэффициент трансформации тока) ");
            if (upIndex == "null") upIndex = "1";
            valuesFromFile.sql += upIndex + ", ";
            i++;

            #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

            #region 9. Заводской номер прибора учета

            //9. Заводской номер прибора учета
            string numCounter = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, "Заводской номер прибора учета");
            valuesFromFile.sql += numCounter + ", ";
            i++;

            #endregion 9. Заводской номер прибора учета

            #region 10. Код единицы измерения расхода (из справочника)

            //12. Код единицы измерения расхода (из справочника)
            string upIndexExp = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
            valuesFromFile.sql += upIndexExp + ", ";
            i++;

            #endregion 10. Код единицы измерения расхода (из справочника)

            #region 11. Дата поверки

            //13. Дата поверки
            string dateCheck = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата поверки");
            valuesFromFile.sql += dateCheck + ", ";
            i++;

            #endregion 11. Дата поверки

            #region 12. Дата следующей поверки

            //14. Дата следующей поверки
            string dateCheckNext = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата следующей поверки");
            valuesFromFile.sql += dateCheckNext + ", ";
            i++;

            #endregion 12. Дата следующей поверки

            #region 13. Дополнительные характеристики (временно в загрузке не участвуют , только в разборе строки )

            //13. Дополнительные характеристики
            string DopCharact = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, "Дополнительные характеристики ОДПУ");

            #region временно не загружаем

            valuesFromFile.sql += DopCharact + ", ";
            i++;

            #endregion временно не загружаем

            #endregion 13. Дополнительные характеристики

            #region 14.	Тип прибора учета

            string type_pu = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Тип прибора учета ");
            valuesFromFile.sql += type_pu + ", ";
            i++;

            #endregion Тип прибора учета

            #region 15. Дата закрытия счетчика

            string dat_close = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата закрытия счетчика");
            valuesFromFile.sql += dat_close + " ";
            i++;

            #endregion 15. Дата закрытия счетчика

            #region Завершающие действия для инсерта в спец таблицу

            //nzp_dom, nzp_counter
            valuesFromFile.sql += ");";

            #endregion Завершающие действия для инсерта в спец таблицу

        }

        /// <summary>
        ///  Загрузка 10 секции "Показания общедомовых приборов учета"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection10_FileOdpuP(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 10;

            #region Версия 1.2 (здесь только  показания ОДПУ)

            if (valuesFromFile.vals.Length < 5)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          "Неправильный формат файла загрузки: Информация об индивидуальных приборах учета, количество полей = " + valuesFromFile.vals.Length + " вместо 5 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_odpu_p(nzp_file , id_odpu  , rashod_type ,  dat_uchet ,   val_cnt  ) " +
                " VALUES(";

            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем

            int i = 1;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код счетчика одпу

            string local_id = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, "Уникальный код счетчика");

            valuesFromFile.sql += local_id + ", ";
            i++;

            #endregion 2. Уникальный код счетчика одпу

            #region 3. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)

            string typeExpenditure = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)");
            valuesFromFile.sql += typeExpenditure + ", ";
            i++;

            #endregion 3. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)

            #region 4. Дата показания прибора учета / Месяц показания

            string dateCounter = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, ". Дата показания прибора учета / Месяц показания");
            valuesFromFile.sql += dateCounter + ", ";
            i++;

            #endregion 4. Дата показания прибора учета / Месяц показания

            #region 5. Показание прибора учета / Месячный расход

            string dateCounerMonthExp = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          "Показание прибора учета / Месячный расход");

            }
            valuesFromFile.sql += dateCounerMonthExp + " ";
            i++;

            #endregion 5. Показание прибора учета / Месячный расход

            //nzp_dom, nzp_counter
            valuesFromFile.sql += ");";

            #endregion Версия 1.2 (здесь показания ОДПУ)

        }

        /// <summary>
        ///  Загрузка 11 секции "Информация об индивидуальных приборах учета"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection11_FileIpu(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 11;

            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Здесь счетчики ИПУ без показаний , показания в 12 секции)

                if (valuesFromFile.vals.Length < 13)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: показания ИПУ, количество полей = " + valuesFromFile.vals.Length + " вместо 13 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu(nzp_file, ls_id, local_id,kod_serv, serv_type, counter_type, cnt_stage, mmnog, num_cnt,   " +
                    " nzp_measure, dat_prov, dat_provnext,doppar) " +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. № ЛС в системе поставщика
                string ls = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");

                valuesFromFile.sql += ls + ", ";
                i++;
                #endregion 2. № ЛС в системе поставщика

                #region 3.Код прибора учета в системе поставщика
                string local_id = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);

                if (!ret.result)
                {
                    local_id = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, " № Индивидуального прибора учета в системе поставщика");
                }
                valuesFromFile.sql += local_id + ", ";
                i++;

                #endregion 3.Код прибора учета в системе поставщика

                #region 4. Код услуги (из справочника)
                string codeServ = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "  Код услуги (из справочника)");
                valuesFromFile.sql += codeServ + ", ";
                i++;
                #endregion 3. Код услуги (из справочника)

                #region 4. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
                //string typeService = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                string typeService = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");
                valuesFromFile.sql += typeService + ", ";
                i++;
                #endregion 4. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

                #region 6. Тип счетчика
                string typeCounter = CheckType.CheckText2(valuesFromFile, i, true, 25, ref ret, "Тип счетчика ");
                if (typeCounter == "null") typeCounter = "-";
                valuesFromFile.sql += typeCounter + ", ";
                i++;
                #endregion 6. Тип счетчика

                #region 7. Разрядность прибора
                string capCounter = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Разрядность прибора ");
                valuesFromFile.sql += capCounter + ", ";
                i++;
                #endregion 7. Разрядность прибора

                #region 8. Повышающий коэффициент (коэффициент трансформации тока)
                string upIndex = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Повышающий коэффициент (коэффициент трансформации тока) ");
                if (upIndex == "null") upIndex = "1";
                valuesFromFile.sql += upIndex + ", ";
                i++;
                #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

                #region 9. Заводской номер прибора учета
                string numCounter = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, "Заводской номер прибора учета");
                valuesFromFile.sql += numCounter + ", ";
                i++;
                #endregion 9. Заводской номер прибора учета

                #region 10. Код единицы измерения расхода (из справочника)
                string upIndexExp = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
                valuesFromFile.sql += upIndexExp + ", ";
                i++;
                #endregion 10. Код единицы измерения расхода (из справочника)

                #region 11. Дата поверки
                string dateCheck = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата поверки");
                valuesFromFile.sql += dateCheck + ", ";
                i++;
                #endregion 11. Дата поверки

                #region 12. Дата следующей поверки
                string dateCheckNext = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата следующей поверки");
                valuesFromFile.sql += dateCheckNext + ", ";
                i++;
                #endregion 12. Дата следующей поверки

                #region 13. Доп параметры ИПУ
                string DopParIpu = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, "13. Доп параметры ИПУ");
                #region Пока не используем но проверяем наличие
                if (DopParIpu.Length == 0) { DopParIpu = "null"; };
                valuesFromFile.sql += DopParIpu + " ";
                i++;
                #endregion Пока не используем но проверяем наличие
                #endregion 13. Доп параметры ИПУ

                #region завершение инсерта
                //nzp_kvar, nzp_counter
                valuesFromFile.sql += ");";
                #endregion завершение инсерта


                #endregion Версия 1.2
            }
        }

        /// <summary>
        /// Формат 1.3.8.1. Загрузка 11 секции "Информация об индивидуальных приборах учета"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection11_FileIpu_v_1381(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 11;

            if (valuesFromFile.Pvers != "1.0")
            {

                if (valuesFromFile.vals.Length < 14)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: показания ИПУ, количество полей = " + valuesFromFile.vals.Length + " вместо 13 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu(nzp_file, ls_id, local_id,kod_serv, serv_type, counter_type, cnt_stage, mmnog, num_cnt,   " +
                    " nzp_measure, dat_prov, dat_provnext,doppar, dat_close) " +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. № ЛС в системе поставщика
                string ls = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");

                valuesFromFile.sql += ls + ", ";
                i++;
                #endregion 2. № ЛС в системе поставщика

                #region 3.Код прибора учета в системе поставщика
                string local_id = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);

                if (!ret.result)
                {
                    local_id = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, " № Индивидуального прибора учета в системе поставщика");
                }
                valuesFromFile.sql += local_id + ", ";
                i++;

                #endregion 3.Код прибора учета в системе поставщика

                #region 4. Код услуги (из справочника)
                string codeServ = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "  Код услуги (из справочника)");
                valuesFromFile.sql += codeServ + ", ";
                i++;
                #endregion 3. Код услуги (из справочника)

                #region 4. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)
                //string typeService = CheckType.CheckInt(vals[i], true, null, null, ref ret);
                string typeService = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)");
                valuesFromFile.sql += typeService + ", ";
                i++;
                #endregion 4. Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)

                #region 6. Тип счетчика
                string typeCounter = CheckType.CheckText2(valuesFromFile, i, true, 25, ref ret, "Тип счетчика ");
                if (typeCounter == "null") typeCounter = "-";
                valuesFromFile.sql += typeCounter + ", ";
                i++;
                #endregion 6. Тип счетчика

                #region 7. Разрядность прибора
                string capCounter = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Разрядность прибора ");
                valuesFromFile.sql += capCounter + ", ";
                i++;
                #endregion 7. Разрядность прибора

                #region 8. Повышающий коэффициент (коэффициент трансформации тока)
                string upIndex = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Повышающий коэффициент (коэффициент трансформации тока) ");
                if (upIndex == "null") upIndex = "1";
                valuesFromFile.sql += upIndex + ", ";
                i++;
                #endregion 8. Повышающий коэффициент (коэффициент трансформации тока)

                #region 9. Заводской номер прибора учета
                string numCounter = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, "Заводской номер прибора учета");
                valuesFromFile.sql += numCounter + ", ";
                i++;
                #endregion 9. Заводской номер прибора учета

                #region 10. Код единицы измерения расхода (из справочника)
                string upIndexExp = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код единицы измерения расхода (из справочника) ");
                valuesFromFile.sql += upIndexExp + ", ";
                i++;
                #endregion 10. Код единицы измерения расхода (из справочника)

                #region 11. Дата поверки
                string dateCheck = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата поверки");
                valuesFromFile.sql += dateCheck + ", ";
                i++;
                #endregion 11. Дата поверки

                #region 12. Дата следующей поверки
                string dateCheckNext = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата следующей поверки");
                valuesFromFile.sql += dateCheckNext + ", ";
                i++;
                #endregion 12. Дата следующей поверки

                #region 13. Доп параметры ИПУ
                string DopParIpu = CheckType.CheckText2(valuesFromFile, i, false, 250, ref ret, "13. Доп параметры ИПУ");
                #region Пока не используем но проверяем наличие
                if (DopParIpu.Length == 0) { DopParIpu = "null"; };
                valuesFromFile.sql += DopParIpu + ", ";
                i++;
                #endregion Пока не используем но проверяем наличие
                #endregion 13. Доп параметры ИПУ

                #region 14. Дата закрытия счетчика

                string dat_close = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата закрытия счетчика");
                valuesFromFile.sql += dat_close + " ";
                i++;

                #endregion 14. Дата закрытия счетчика

                #region завершение инсерта
                //nzp_kvar, nzp_counter
                valuesFromFile.sql += ");";
                #endregion завершение инсерта

            }
        }


        /// <summary>
        ///  Загрузка 12 секции "Показания индивидуальных приборов учета"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection12_FileIpu_p(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 12;


            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Здесь Показания ИПУ )

                if (valuesFromFile.vals.Length < 6)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: показания ИПУ, количество полей = " + valuesFromFile.vals.Length + " вместо 6 ");
                    return;
                }
                
                //valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu_p( " +
                //                     "nzp_file , id_ipu,  rashod_type ,  dat_uchet , val_cnt, kod_serv) " +
                //                     " VALUES(";

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                //valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код счетчика ипу
                string local_id = CheckType.CheckInt(valuesFromFile.vals[i], true, 1, null, ref ret);

                if (!ret.result)
                {
                    local_id = CheckType.CheckText3(valuesFromFile, i, true, 20, ref ret, " № Индивидуального прибора учета в системе поставщика");
                }
                //valuesFromFile.sql += local_id + ", ";
                i++;
                #endregion 2. Уникальный код счетчика одпу

                #region 3. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)
                string typeExpenditure = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)");
                //valuesFromFile.sql += typeExpenditure + ", ";
                i++;
                #endregion 3. Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)

                #region 4. Дата показания прибора учета / Месяц показания
                string dateCounter = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, ". Дата показания прибора учета / Месяц показания");
                //valuesFromFile.sql += dateCounter + ", ";
                i++;
                #endregion 4. Дата показания прибора учета / Месяц показания

                #region 5. Показание прибора учета / Месячный расход
                string dateCounerMonthExp = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Показание прибора учета / Месячный расход");
                }
                //valuesFromFile.sql += dateCounerMonthExp + ", ";
                i++;
                #endregion 5. Показание прибора учета / Месячный расход

                #region 6. Код услуги
                string kod_serv = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код услуги");

                }
                //valuesFromFile.sql += kod_serv + " ";
                i++;
                #endregion 6. Код услуги

                #region завершение инсерта
                //valuesFromFile.sql += ");";
                #endregion завершение инсерта

                #region Заготовка инсерта

                valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu_p( " +
                                     "nzp_file , id_ipu,  rashod_type ,  dat_uchet , val_cnt, kod_serv) " +
                                     " VALUES(" + valuesFromFile.finder.nzp_file + ", " + local_id + ", " +
                                     typeExpenditure + ", " + dateCounter + ", " + dateCounerMonthExp + ", " + kod_serv + ");";

                #endregion Заготовка инсерта

                #endregion Версия 1.2
            }
        }


        /// <summary>
        ///  Загрузка 13 секции "Перечень выгруженных услуг"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection13_FileServices(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 13;

            //загружаем ли 13 секцию? если нет, то берем из собственных таблиц
            valuesFromFile.loaded13section = true;

            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень выгруженных услуг )
                int fieldsCount = 7;

                if (valuesFromFile.finder.format_name == "'1.2.1'")
                {
                    fieldsCount = 5;
                }

                if (valuesFromFile.vals.Length < fieldsCount)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: Перечень выгруженных услуг, количество полей = " + valuesFromFile.vals.Length + " вместо 7 ");
                    return;
                }

                #region Заготовка инсерта
                valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_services( nzp_file, id_serv, service, service2, type_serv)" +
                    " VALUES(";

                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код услуги в системе поставщика информации
                string local_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Уникальный код услуги в системе поставщика информации");
                valuesFromFile.sql += local_id + ", ";
                i++;
                #endregion 2. Уникальный код услуги в системе поставщика информации

                #region 3. Наименование услуги
                string serv_name = CheckType.CheckText2(valuesFromFile, i, true, 60, ref ret, " Наименование услуги ");
                valuesFromFile.sql += serv_name + ", ";
                i++;
                #endregion 3.  Наименование услуги

                #region 4. Краткое наименование услуги
                string serv_name_short = CheckType.CheckText2(valuesFromFile, i, false, 60, ref ret, " Краткое наименование услуги ");
                valuesFromFile.sql += serv_name_short + ", ";
                i++;
                #endregion 4.  Краткое наименование услуги

                #region 5. Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)
                string type_serv = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, "Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)");
                valuesFromFile.sql += type_serv + ");";
                #endregion 5. Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)

                #endregion Версия 1.2
            }
        }


        /// <summary>
        ///  Загрузка 14 секции "Перечень выгруженных муниципальных образований "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection14_FileMo(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 14;


            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень выгруженных муниципальных образований )

                if (valuesFromFile.vals.Length < 5)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных муниципальных образований, количество полей = " + valuesFromFile.vals.Length + " вместо 5 ");
                    return;
                }

                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_mo(nzp_file, id_mo, mo_name, raj, nzp_raj)" +
                    "VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код муниципального образования (МО) в системе поставщика информации
                string id_mo = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Уникальный код муниципального образования (МО) в системе поставщика информации");
                }
                valuesFromFile.sql += id_mo + ", ";
                i++;
                #endregion 2. Уникальный код муниципального образования (МО) в системе поставщика информации

                #region 3. Наименование МО
                string mo_name = CheckType.CheckText(valuesFromFile.vals[i], true, 60, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Наименование МО ");

                }
                valuesFromFile.sql += mo_name + ", ";
                i++;
                #endregion 3.  Наименование МО

                #region 4. Район
                string raj = CheckType.CheckText(valuesFromFile.vals[i], true, 60, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Район ");

                }
                valuesFromFile.sql += raj + ", ";
                i++;
                #endregion 4.  Район

                #region 5. Уникальный код района
                string nzp_raj = CheckType.CheckInt(valuesFromFile.vals[i], false, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Уникальный код района");
                }
                valuesFromFile.sql += nzp_raj + "); ";
                #endregion 5. Уникальный код района

                //доисать код униципального образованная (КЛАДР)


                #endregion Версия 1.2
            }
        }


        /// <summary>
        ///  Загрузка 15 секции "Информация по проживающим "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection15_FileGilec(DbValuesFromFile valuesFromFile)
        {
            
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 15;


            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Информация о проживающем)

                if (valuesFromFile.vals.Length < 51)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: информация о проживающем, количество полей = " + valuesFromFile.vals.Length + " вместо 51 ");
                  //  return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec (nzp_file, num_ls, nzp_gil, nzp_kart, nzp_tkrt, fam," +
                    " ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, gender, nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd," +
                    " strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, strana_op, region_op, okrug_op, gorod_op, npunkt_op," +
                    " rem_op, strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp, dat_prop, dat_oprp, dat_pvu, " +
                    "who_pvu, dat_svu, namereg, kod_namereg, rod, nzp_celp, nzp_celu, dat_sost, dat_ofor)" +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный номер лицевого счета
                string num_ls = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "  Уникальный номер лицевого счета ");
                //(valuesFromFile, i, true, null, null, ref ret, " Уникальный номер лицевого счета ");
                valuesFromFile.sql += num_ls + ", ";
                i++;
                #endregion 2.  Уникальный номер лицевого счета

                #region 3. Уникальный номер гражданина
                string nzp_gil = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Уникальный номер гражданина");
                valuesFromFile.sql += nzp_gil + ", ";
                i++;
                #endregion 3. Уникальный номер гражданина

                #region 4. Уникальный номер адресного листка прибытия/убытия гражданина
                string nzp_kart = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Уникальный номер адресного листка прибытия/убытия гражданина");
                valuesFromFile.sql += nzp_kart + ", ";
                i++;
                #endregion 4. Уникальный номер адресного листка прибытия/убытия гражданина

                #region 5. Тип адресного листка (1 - прибытие, 2 - убытие)
                string nzp_tkrt = CheckType.CheckInt2(valuesFromFile, i, true, 1, 2, ref ret, "Тип адресного листка");
                valuesFromFile.sql += nzp_tkrt + ", ";
                i++;
                #endregion 5. Тип адресного листка

                #region 6. Фамилия
                string fam = CheckType.CheckText2(valuesFromFile, i, true, 40, ref ret, " Фамилия  ");
                valuesFromFile.sql += fam + ", ";
                i++;
                #endregion 6. Фамилия

                #region 7. Имя
                string ima = CheckType.CheckText2(valuesFromFile, i, true, 40, ref ret, " Имя ");
                valuesFromFile.sql += ima + ", ";
                i++;
                #endregion 7. Имя

                #region 8. Отчество
                string otch = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Отчество ");
                valuesFromFile.sql += otch + ", ";
                i++;
                #endregion 8. Отчество

                #region 9. Дата рождения
                string dat_rog = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата рождения");
                valuesFromFile.sql += dat_rog + ", ";
                i++;
                #endregion 9. Дата рождения

                #region 10. Измененная фамилия
                string fam_c = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Измененная фамилия ");
                valuesFromFile.sql += fam_c + ", ";
                i++;
                #endregion 10. Измененная фамилия

                #region 11. Измененное имя
                string ima_c = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Измененное имя ");
                valuesFromFile.sql += ima_c + ", ";
                i++;
                #endregion 11. Измененное имя

                #region 12. Измененное отчество
                string otch_c = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Измененное отчество ");
                valuesFromFile.sql += otch_c + ", ";
                i++;
                #endregion 12. Измененное отчество

                #region 13. Измененная дата рождения
                string dat_rog_c = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Измененная дата рождения");
                valuesFromFile.sql += dat_rog_c + ", ";
                i++;
                #endregion 13. Измененная дата рождения

                #region 14. Пол (М - мужской, Ж - женский)
                string gender = CheckType.CheckText2(valuesFromFile, i, true, 1, ref ret, " Пол (М - мужской, Ж - женский) ");
                valuesFromFile.sql += gender + ", ";
                i++;
                #endregion 14. Пол

                #region 15. Тип удостоверения личности (1-паспорт, 2-св-во, 3-справка, 4-воен.билет, 5-удостоверение)
                string nzp_dok = CheckType.CheckInt2(valuesFromFile, i, true, 1, 5, ref ret, "Тип удостоверения личности");
                valuesFromFile.sql += nzp_dok + ", ";
                i++;
                #endregion 15. Тип удостоверения личности

                #region 16. Серия удостоверения личности
                string serij = CheckType.CheckText2(valuesFromFile, i, true, 10, ref ret, " Серия удостоверения личности ");
                valuesFromFile.sql += serij + ", ";
                i++;
                #endregion 16. Серия удостоверения личности

                #region 17. Номер удостоверения личности
                string nomer = CheckType.CheckText2(valuesFromFile, i, true, 7, ref ret, " Номер удостоверения личности ");
                valuesFromFile.sql += nomer + ", ";
                i++;
                #endregion 17. Номер удостоверения личности

                #region 18. Дата выдачи удостоверения личности
                string vid_dat = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата выдачи удостоверения личности");
                valuesFromFile.sql += vid_dat + ", ";
                i++;
                #endregion 18. Дата выдачи удостоверения личности

                #region 19. Место выдачи удостоверения личности
                string vid_mes = CheckType.CheckText2(valuesFromFile, i, true, 70, ref ret, " Место выдачи удостоверения личности ");
                valuesFromFile.sql += vid_mes + ", ";
                i++;
                #endregion 19. Место выдачи удостоверения личности

                #region 20. Код органа выдачи удостоверения личности
                string kod_podrazd = CheckType.CheckText2(valuesFromFile, i, false, 7, ref ret, " Код органа выдачи удостоверения личности ");
                valuesFromFile.sql += kod_podrazd + ", ";
                i++;
                #endregion 20. Код органа выдачи удостоверения личности

                #region 21. Страна рождения
                string strana_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Страна рождения ");
                valuesFromFile.sql += strana_mr + ", ";
                i++;
                #endregion 21. Страна рождения

                #region 22. Регион рождения
                string region_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Регион рождения ");
                valuesFromFile.sql += region_mr + ", ";
                i++;
                #endregion 22. Регион рождения

                #region 23. Район рождения
                string okrug_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Район рождения ");
                valuesFromFile.sql += okrug_mr + ", ";
                i++;
                #endregion 23. Район рождения

                #region 24. Город рождения
                string gorod_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Город рождения ");
                valuesFromFile.sql += gorod_mr + ", ";
                i++;
                #endregion 24. Город рождения

                #region 25. Нас. пункт рождения
                string npunkt_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Нас. пункт рождения ");
                valuesFromFile.sql += npunkt_mr + ", ";
                i++;
                #endregion 25. Нас. пункт рождения

                #region 26. Страна откуда прибыл
                string strana_op;
                //if (nzp_tkrt == "1") strana_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                strana_op = CheckType.CheckText(valuesFromFile.vals[i], false, 40, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "  Страна откуда прибыл ");
                }
                valuesFromFile.sql += strana_op + ", ";
                i++;
                #endregion 26.  Страна откуда прибыл

                #region 27. Регион откуда прибыл
                string region_op;
                //if (nzp_tkrt == "1") region_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                region_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "  Регион откуда прибыл ");
                valuesFromFile.sql += region_op + ", ";
                i++;
                #endregion 27.  Регион откуда прибыл

                #region 28. Район откуда прибыл
                string okrug_op;
                //if (nzp_tkrt == "1") okrug_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                okrug_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "  Район откуда прибыл ");
                valuesFromFile.sql += okrug_op + ", ";
                i++;
                #endregion 28.  Район откуда прибыл

                #region 29. Город откуда прибыл
                string gorod_op;
                //if (nzp_tkrt == "1") gorod_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                gorod_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Город откуда прибыл ");
                valuesFromFile.sql += gorod_op + ", ";
                i++;
                #endregion 29.  Город откуда прибыл

                #region 30. Нас. пункт откуда прибыл
                string npunkt_op;
                //if (nzp_tkrt == "1") npunkt_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                npunkt_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Нас. пункт откуда прибыл ");
                valuesFromFile.sql += npunkt_op + ", ";
                i++;
                #endregion 30.  Нас. пункт откуда прибыл

                #region 31. Улица, дом, корпус, квартира откуда прибыл
                string rem_op;
                //if (nzp_tkrt == "1") rem_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                rem_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Улица, дом, корпус, квартира откуда прибыл ");
                valuesFromFile.sql += rem_op + ", ";
                i++;
                #endregion 31.  Улица, дом, корпус, квартира откуда прибыл

                #region 32. Страна куда убыл
                string strana_ku;
                if (nzp_tkrt == "2") strana_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    strana_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Страна куда убыл ");
                valuesFromFile.sql += strana_ku + ", ";
                i++;
                #endregion 32.  Страна куда убыл

                #region 33. Регион куда убыл
                string region_ku;
                if (nzp_tkrt == "2") region_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    region_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Регион куда убыл ");
                valuesFromFile.sql += region_ku + ", ";
                i++;
                #endregion 33. Регион куда убыл

                #region 34. Район куда убыл
                string okrug_ku;
                if (nzp_tkrt == "2") okrug_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    okrug_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Район куда убыл ");
                valuesFromFile.sql += okrug_ku + ", ";
                i++;
                #endregion 34. Район куда убыл

                #region 35. Город куда убыл
                string gorod_ku;
                if (nzp_tkrt == "2") gorod_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    gorod_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Город куда убыл ");
                valuesFromFile.sql += gorod_ku + ", ";
                i++;
                #endregion 35. Город куда убыл

                #region 36. Нас.пункт куда убыл
                string npunkt_ku;
                if (nzp_tkrt == "2") npunkt_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    npunkt_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Нас.пункт куда убыл ");
                valuesFromFile.sql += npunkt_ku + ", ";
                i++;
                #endregion 36. Нас.пункт куда убыл

                #region 37. Улица, дом, корпус, квартира куда убыл
                string rem_ku;
                if (nzp_tkrt == "2") rem_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    rem_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Улица, дом, корпус, квартира куда убыл ");
                valuesFromFile.sql += rem_ku + ", ";
                i++;
                #endregion 37. Улица, дом, корпус, квартира куда убыл

                #region 38. Улица, дом, корпус, квартира для поля "переезд в том же нас. пункте"
                string rem_p;
                if (nzp_tkrt == "2") rem_p = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    rem_p = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Улица, дом, корпус, квартира для поля \"переезд в том же нас. пункте\" ");
                valuesFromFile.sql += rem_p + ", ";
                i++;
                #endregion 38. Улица, дом, корпус, квартира для поля "переезд в том же нас. пункте"

                #region 39. Тип регистрации (П - по месту жмительства, В - по месту пребывания)
                string tprp = CheckType.CheckText2(valuesFromFile, i, true, 1, ref ret, " Тип регистрации ");
                valuesFromFile.sql += tprp + ", ";
                i++;
                #endregion 39. Тип регистрации

                #region 40. Дата первой регистрации по адресу
                string dat_prop = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, " Дата первой регистрации по адресу ");
                valuesFromFile.sql += dat_prop + ", ";
                i++;
                #endregion 40. Дата первой регистрации по адресу

                #region 41. Дата окончания регистрации по месту пребывания
                string dat_oprp;
                if (nzp_tkrt == "1") dat_oprp = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
                else if (tprp == "В") dat_oprp = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
                else dat_oprp = CheckType.CheckDateTime(valuesFromFile.vals[i], false, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Дата окончания регистрации по месту пребывания ");
                }
                valuesFromFile.sql += dat_oprp + ", ";
                i++;
                #endregion 41. Дата окончания регистрации по месту пребывания

                #region 42. Дата постановки на воинский учет
                string dat_pvu = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, " Дата постановки на воинский учет ");
                valuesFromFile.sql += dat_pvu + ", ";
                i++;
                #endregion 42. Дата постановки на воинский учет

                #region 43. Орган регистрации воинского учета
                string who_pvu = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Орган регистрации воинского учета ");
                valuesFromFile.sql += who_pvu + ", ";
                i++;
                #endregion 43. Орган регистрации воинского учета

                #region 44. Дата снятия с воинского учета
                string dat_svu = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, " Дата снятия с воинского учета ");
                valuesFromFile.sql += dat_svu + ", ";
                i++;
                #endregion 44. Дата снятия с воинского учета

                #region 45. Орган регистрационного учета
                string namereg = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Орган регистрационного учета ");
                valuesFromFile.sql += namereg + ", ";
                i++;
                #endregion 45. Орган регистрационного учета

                #region 46. Код органа регистрации учета
                string kod_namereg = CheckType.CheckText2(valuesFromFile, i, false, 7, ref ret, " Код органа регистрации учета ");
                valuesFromFile.sql += kod_namereg + ", ";
                i++;
                #endregion 46. Код органа регистрации учета

                #region 47. Родственные отношения
                string rod = CheckType.CheckText2(valuesFromFile, i, false, 30, ref ret, " Родственные отношения ");
                valuesFromFile.sql += rod + ", ";
                i++;
                #endregion 47. Родстенные отношения

                #region 48. Код цели прибытия
                string nzp_celp;
                if (nzp_tkrt == "1") nzp_celp = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
                else
                    nzp_celp = CheckType.CheckInt(valuesFromFile.vals[i], false, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код цели прибытия");
                }
                valuesFromFile.sql += nzp_celp + ", ";
                i++;
                #endregion 48. Код цели прибытия

                #region 49. Код цели убытия
                string nzp_celu;
                if (nzp_tkrt == "2") nzp_celu = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
                else
                    nzp_celu = CheckType.CheckInt(valuesFromFile.vals[i], false, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код цели убытия");
                }
                valuesFromFile.sql += nzp_celu + ", ";
                i++;
                #endregion 49. Код цели убытия

                #region 50. Дата составления адресного листка
                string dat_sost = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " Дата составления адресного листка");
                valuesFromFile.sql += dat_sost + ", ";
                i++;
                #endregion 50. Дата составления адресного листка

                #region 51. Дата оформления регистрации
                string dat_ofor = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " Дата оформления регистрации");
                valuesFromFile.sql += dat_ofor + "); ";
                // MonitorLog.WriteLog(valuesFromFile.sql, MonitorLog.typelog.Info, true);
                #endregion 51. Дата оформления регистрации

                #endregion Версия 1.2
            }
        }

        /// <summary>
        ///  Загрузка 15 секции "Информация по проживающим "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection15_FileGilec_v_1382(DbValuesFromFile valuesFromFile)
        {

            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 15;


            if (valuesFromFile.Pvers != "1.0")
            {
               
                if (valuesFromFile.vals.Length < 52)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: информация о проживающем, количество полей = " + valuesFromFile.vals.Length + " вместо 52 ");
                    //  return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec (nzp_file, num_ls, nzp_gil, nzp_kart, nzp_tkrt, fam," +
                    " ima, otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, gender, nzp_dok, serij, nomer, vid_dat, vid_mes, kod_podrazd," +
                    " strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, strana_op, region_op, okrug_op, gorod_op, npunkt_op," +
                    " rem_op, strana_ku, region_ku, okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp, dat_prop, dat_oprp, dat_pvu, " +
                    "who_pvu, dat_svu, namereg, kod_namereg, rod, nzp_celp, nzp_celu, dat_sost, dat_ofor, doc_name)" +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный номер лицевого счета
                string num_ls = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "  Уникальный номер лицевого счета ");
                //(valuesFromFile, i, true, null, null, ref ret, " Уникальный номер лицевого счета ");
                valuesFromFile.sql += num_ls + ", ";
                i++;
                #endregion 2.  Уникальный номер лицевого счета

                #region 3. Уникальный номер гражданина
                string nzp_gil = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Уникальный номер гражданина");
                valuesFromFile.sql += nzp_gil + ", ";
                i++;
                #endregion 3. Уникальный номер гражданина

                #region 4. Уникальный номер адресного листка прибытия/убытия гражданина
                string nzp_kart = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Уникальный номер адресного листка прибытия/убытия гражданина");
                valuesFromFile.sql += nzp_kart + ", ";
                i++;
                #endregion 4. Уникальный номер адресного листка прибытия/убытия гражданина

                #region 5. Тип адресного листка (1 - прибытие, 2 - убытие)
                string nzp_tkrt = CheckType.CheckInt2(valuesFromFile, i, true, 1, 2, ref ret, "Тип адресного листка");
                valuesFromFile.sql += nzp_tkrt + ", ";
                i++;
                #endregion 5. Тип адресного листка

                #region 6. Фамилия
                string fam = CheckType.CheckText2(valuesFromFile, i, true, 40, ref ret, " Фамилия  ");
                valuesFromFile.sql += fam + ", ";
                i++;
                #endregion 6. Фамилия

                #region 7. Имя
                string ima = CheckType.CheckText2(valuesFromFile, i, true, 40, ref ret, " Имя ");
                valuesFromFile.sql += ima + ", ";
                i++;
                #endregion 7. Имя

                #region 8. Отчество
                string otch = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Отчество ");
                valuesFromFile.sql += otch + ", ";
                i++;
                #endregion 8. Отчество

                #region 9. Дата рождения
                string dat_rog = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата рождения");
                valuesFromFile.sql += dat_rog + ", ";
                i++;
                #endregion 9. Дата рождения

                #region 10. Измененная фамилия
                string fam_c = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Измененная фамилия ");
                valuesFromFile.sql += fam_c + ", ";
                i++;
                #endregion 10. Измененная фамилия

                #region 11. Измененное имя
                string ima_c = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Измененное имя ");
                valuesFromFile.sql += ima_c + ", ";
                i++;
                #endregion 11. Измененное имя

                #region 12. Измененное отчество
                string otch_c = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Измененное отчество ");
                valuesFromFile.sql += otch_c + ", ";
                i++;
                #endregion 12. Измененное отчество

                #region 13. Измененная дата рождения
                string dat_rog_c = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Измененная дата рождения");
                valuesFromFile.sql += dat_rog_c + ", ";
                i++;
                #endregion 13. Измененная дата рождения

                #region 14. Пол (М - мужской, Ж - женский)
                string gender = CheckType.CheckText2(valuesFromFile, i, true, 1, ref ret, " Пол (М - мужской, Ж - женский) ");
                valuesFromFile.sql += gender + ", ";
                i++;
                #endregion 14. Пол

                #region 15. Тип удостоверения личности (1-паспорт, 2-св-во, 3-справка, 4-воен.билет, 5-удостоверение)
                string nzp_dok = CheckType.CheckInt2(valuesFromFile, i, true, 1, 5, ref ret, "Тип удостоверения личности");
                valuesFromFile.sql += nzp_dok + ", ";
                i++;
                #endregion 15. Тип удостоверения личности

                #region 16. Серия удостоверения личности
                string serij = CheckType.CheckText2(valuesFromFile, i, true, 10, ref ret, " Серия удостоверения личности ");
                valuesFromFile.sql += serij + ", ";
                i++;
                #endregion 16. Серия удостоверения личности

                #region 17. Номер удостоверения личности
                string nomer = CheckType.CheckText2(valuesFromFile, i, true, 7, ref ret, " Номер удостоверения личности ");
                valuesFromFile.sql += nomer + ", ";
                i++;
                #endregion 17. Номер удостоверения личности

                #region 18. Дата выдачи удостоверения личности
                string vid_dat = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата выдачи удостоверения личности");
                valuesFromFile.sql += vid_dat + ", ";
                i++;
                #endregion 18. Дата выдачи удостоверения личности

                #region 19. Место выдачи удостоверения личности
                string vid_mes = CheckType.CheckText2(valuesFromFile, i, true, 70, ref ret, " Место выдачи удостоверения личности ");
                valuesFromFile.sql += vid_mes + ", ";
                i++;
                #endregion 19. Место выдачи удостоверения личности

                #region 20. Код органа выдачи удостоверения личности
                string kod_podrazd = CheckType.CheckText2(valuesFromFile, i, false, 7, ref ret, " Код органа выдачи удостоверения личности ");
                valuesFromFile.sql += kod_podrazd + ", ";
                i++;
                #endregion 20. Код органа выдачи удостоверения личности

                #region 21. Страна рождения
                string strana_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Страна рождения ");
                valuesFromFile.sql += strana_mr + ", ";
                i++;
                #endregion 21. Страна рождения

                #region 22. Регион рождения
                string region_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Регион рождения ");
                valuesFromFile.sql += region_mr + ", ";
                i++;
                #endregion 22. Регион рождения

                #region 23. Район рождения
                string okrug_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Район рождения ");
                valuesFromFile.sql += okrug_mr + ", ";
                i++;
                #endregion 23. Район рождения

                #region 24. Город рождения
                string gorod_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Город рождения ");
                valuesFromFile.sql += gorod_mr + ", ";
                i++;
                #endregion 24. Город рождения

                #region 25. Нас. пункт рождения
                string npunkt_mr = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Нас. пункт рождения ");
                valuesFromFile.sql += npunkt_mr + ", ";
                i++;
                #endregion 25. Нас. пункт рождения

                #region 26. Страна откуда прибыл
                string strana_op;
                //if (nzp_tkrt == "1") strana_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                strana_op = CheckType.CheckText(valuesFromFile.vals[i], false, 40, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "  Страна откуда прибыл ");
                }
                valuesFromFile.sql += strana_op + ", ";
                i++;
                #endregion 26.  Страна откуда прибыл

                #region 27. Регион откуда прибыл
                string region_op;
                //if (nzp_tkrt == "1") region_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                region_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "  Регион откуда прибыл ");
                valuesFromFile.sql += region_op + ", ";
                i++;
                #endregion 27.  Регион откуда прибыл

                #region 28. Район откуда прибыл
                string okrug_op;
                //if (nzp_tkrt == "1") okrug_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                okrug_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, "  Район откуда прибыл ");
                valuesFromFile.sql += okrug_op + ", ";
                i++;
                #endregion 28.  Район откуда прибыл

                #region 29. Город откуда прибыл
                string gorod_op;
                //if (nzp_tkrt == "1") gorod_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                gorod_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Город откуда прибыл ");
                valuesFromFile.sql += gorod_op + ", ";
                i++;
                #endregion 29.  Город откуда прибыл

                #region 30. Нас. пункт откуда прибыл
                string npunkt_op;
                //if (nzp_tkrt == "1") npunkt_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                npunkt_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Нас. пункт откуда прибыл ");
                valuesFromFile.sql += npunkt_op + ", ";
                i++;
                #endregion 30.  Нас. пункт откуда прибыл

                #region 31. Улица, дом, корпус, квартира откуда прибыл
                string rem_op;
                //if (nzp_tkrt == "1") rem_op = CheckType.CheckText(vals[i], true, 40, ref ret);
                //else 
                rem_op = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Улица, дом, корпус, квартира откуда прибыл ");
                valuesFromFile.sql += rem_op + ", ";
                i++;
                #endregion 31.  Улица, дом, корпус, квартира откуда прибыл

                #region 32. Страна куда убыл
                string strana_ku;
                if (nzp_tkrt == "2") strana_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    strana_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Страна куда убыл ");
                valuesFromFile.sql += strana_ku + ", ";
                i++;
                #endregion 32.  Страна куда убыл

                #region 33. Регион куда убыл
                string region_ku;
                if (nzp_tkrt == "2") region_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    region_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Регион куда убыл ");
                valuesFromFile.sql += region_ku + ", ";
                i++;
                #endregion 33. Регион куда убыл

                #region 34. Район куда убыл
                string okrug_ku;
                if (nzp_tkrt == "2") okrug_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    okrug_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Район куда убыл ");
                valuesFromFile.sql += okrug_ku + ", ";
                i++;
                #endregion 34. Район куда убыл

                #region 35. Город куда убыл
                string gorod_ku;
                if (nzp_tkrt == "2") gorod_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    gorod_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Город куда убыл ");
                valuesFromFile.sql += gorod_ku + ", ";
                i++;
                #endregion 35. Город куда убыл

                #region 36. Нас.пункт куда убыл
                string npunkt_ku;
                if (nzp_tkrt == "2") npunkt_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    npunkt_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Нас.пункт куда убыл ");
                valuesFromFile.sql += npunkt_ku + ", ";
                i++;
                #endregion 36. Нас.пункт куда убыл

                #region 37. Улица, дом, корпус, квартира куда убыл
                string rem_ku;
                if (nzp_tkrt == "2") rem_ku = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    rem_ku = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Улица, дом, корпус, квартира куда убыл ");
                valuesFromFile.sql += rem_ku + ", ";
                i++;
                #endregion 37. Улица, дом, корпус, квартира куда убыл

                #region 38. Улица, дом, корпус, квартира для поля "переезд в том же нас. пункте"
                string rem_p;
                if (nzp_tkrt == "2") rem_p = CheckType.CheckText(valuesFromFile.vals[i], true, 40, ref ret);
                else
                    rem_p = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Улица, дом, корпус, квартира для поля \"переезд в том же нас. пункте\" ");
                valuesFromFile.sql += rem_p + ", ";
                i++;
                #endregion 38. Улица, дом, корпус, квартира для поля "переезд в том же нас. пункте"

                #region 39. Тип регистрации (П - по месту жмительства, В - по месту пребывания)
                string tprp = CheckType.CheckText2(valuesFromFile, i, true, 1, ref ret, " Тип регистрации ");
                valuesFromFile.sql += tprp + ", ";
                i++;
                #endregion 39. Тип регистрации

                #region 40. Дата первой регистрации по адресу
                string dat_prop = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, " Дата первой регистрации по адресу ");
                valuesFromFile.sql += dat_prop + ", ";
                i++;
                #endregion 40. Дата первой регистрации по адресу

                #region 41. Дата окончания регистрации по месту пребывания
                string dat_oprp;
                if (nzp_tkrt == "1") dat_oprp = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
                else if (tprp == "В") dat_oprp = CheckType.CheckDateTime(valuesFromFile.vals[i], true, ref ret);
                else dat_oprp = CheckType.CheckDateTime(valuesFromFile.vals[i], false, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Дата окончания регистрации по месту пребывания ");
                }
                valuesFromFile.sql += dat_oprp + ", ";
                i++;
                #endregion 41. Дата окончания регистрации по месту пребывания

                #region 42. Дата постановки на воинский учет
                string dat_pvu = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, " Дата постановки на воинский учет ");
                valuesFromFile.sql += dat_pvu + ", ";
                i++;
                #endregion 42. Дата постановки на воинский учет

                #region 43. Орган регистрации воинского учета
                string who_pvu = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Орган регистрации воинского учета ");
                valuesFromFile.sql += who_pvu + ", ";
                i++;
                #endregion 43. Орган регистрации воинского учета

                #region 44. Дата снятия с воинского учета
                string dat_svu = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, " Дата снятия с воинского учета ");
                valuesFromFile.sql += dat_svu + ", ";
                i++;
                #endregion 44. Дата снятия с воинского учета

                #region 45. Орган регистрационного учета
                string namereg = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Орган регистрационного учета ");
                valuesFromFile.sql += namereg + ", ";
                i++;
                #endregion 45. Орган регистрационного учета

                #region 46. Код органа регистрации учета
                string kod_namereg = CheckType.CheckText2(valuesFromFile, i, false, 7, ref ret, " Код органа регистрации учета ");
                valuesFromFile.sql += kod_namereg + ", ";
                i++;
                #endregion 46. Код органа регистрации учета

                #region 47. Родственные отношения
                string rod = CheckType.CheckText2(valuesFromFile, i, false, 30, ref ret, " Родственные отношения ");
                valuesFromFile.sql += rod + ", ";
                i++;
                #endregion 47. Родстенные отношения

                #region 48. Код цели прибытия
                string nzp_celp;
                if (nzp_tkrt == "1") nzp_celp = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
                else
                    nzp_celp = CheckType.CheckInt(valuesFromFile.vals[i], false, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код цели прибытия");
                }
                valuesFromFile.sql += nzp_celp + ", ";
                i++;
                #endregion 48. Код цели прибытия

                #region 49. Код цели убытия
                string nzp_celu;
                if (nzp_tkrt == "2") nzp_celu = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
                else
                    nzp_celu = CheckType.CheckInt(valuesFromFile.vals[i], false, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Код цели убытия");
                }
                valuesFromFile.sql += nzp_celu + ", ";
                i++;
                #endregion 49. Код цели убытия

                #region 50. Дата составления адресного листка
                string dat_sost = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " Дата составления адресного листка");
                valuesFromFile.sql += dat_sost + ", ";
                i++;
                #endregion 50. Дата составления адресного листка

                #region 51. Дата оформления регистрации
                string dat_ofor = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " Дата оформления регистрации");
                valuesFromFile.sql += dat_ofor + ", ";
                i++;
                // MonitorLog.WriteLog(valuesFromFile.sql, MonitorLog.typelog.Info, true);
                #endregion 51. Дата оформления регистрации

                #region 52. Наименование документа удостоверения личности
                string doc_name = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, " Наименование документа удостоверения личности");
                valuesFromFile.sql += doc_name + "); ";
                i++;
                #endregion 52. Наименование документа удостоверения личности

            }
        }

        /// <summary>
        ///  Загрузка 16 секции "Перечень выгруженных параметров "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection16_FileTypeparams(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 16;

            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень выгруженных параметров )

                if (valuesFromFile.vals.Length < 5)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных параметров, количество полей = " + valuesFromFile.vals.Length + " вместо 5 ");
                    return;
                }

                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams(nzp_file , id_prm, prm_name, level_, type_prm ) " +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код параметра в системе поставщика информации
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный код параметра в системе поставщика информации ");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 2. Уникальный код параметра в системе поставщика информации

                #region 3. Наименование параметра
                string prm_name = CheckType.CheckText2(valuesFromFile, i, true, 60, ref ret, "Наименование параметра");
                valuesFromFile.sql += prm_name + ", ";
                i++;

                #endregion 3. Наименование параметра

                #region 4. Принадлежность к уровню (1-база, 2-дом, 3-лицевой счет, 4-домовой ПУ)
                string level = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Принадлежность к уровню ");
                valuesFromFile.sql += level + ", ";
                i++;
                #endregion 4. Принадлежность к уровню (1-база, 2-дом, 3-лицевой счет, 4-домовой ПУ)

                #region 5. Тип параметра (1-текст, 2-число, 3-дата)
                string type_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Тип параметра ");
                valuesFromFile.sql += type_prm + "); ";
                #endregion 5. Тип параметра
                #endregion Версия 1.2
            }
        }


        /// <summary>
        ///  Загрузка 17 секции "Перечень выгруженных типов жилья по газоснабжению "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection17_FileGaz(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 17;


            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень выгруженных типов жилья по газоснабжению)

                if (valuesFromFile.vals.Length < 3)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных типов жилья по газоснабжению, количество полей = " + valuesFromFile.vals.Length + " вместо 3 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_gaz( nzp_file , id_prm, name) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код типа жилья по газоснабжению в системе поставщика информации
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный код типа жилья по газоснабжению в системе поставщика информации");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 2. Уникальный код типа жилья по газоснабжению в системе поставщика информации

                #region 3. Наименование типа
                string name = CheckType.CheckText2(valuesFromFile, i, true, 60, ref ret, "Наименование типа");
                valuesFromFile.sql += name + "); ";
                #endregion 3. Наименование типа

                #endregion Версия 1.2
            }
        }


        /// <summary>
        ///  Загрузка 18 секции "Перечень выгруженных типов жилья по водоснабжению"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection18_FileVoda(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 18;

            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень выгруженных типов жилья по водоснабжению)
                if (valuesFromFile.vals.Length < 3)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных типов жилья по водоснабжению, количество полей = " + valuesFromFile.vals.Length + " вместо 3 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_voda( nzp_file , id_prm, name) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код типа жилья по водоснабжению в системе поставщика информации
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный код типа жилья по водоснабжению в системе поставщика информации");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 2. Уникальный код типа жилья по водоснабжению в системе поставщика информации

                #region 3. Наименование типа
                string name = CheckType.CheckText2(valuesFromFile, i, true, 60, ref ret, "Наименование типа");
                valuesFromFile.sql += name + "); ";
                #endregion 3. Наименование типа

                #endregion Версия 1.2
            }
        }


        /// <summary>
        ///  Загрузка 19 секции "Перечень выгруженных категорий благоустройства дома "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection19_FileBlag(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 19;

            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень выгруженных категорий благоустройства дома)

                if (valuesFromFile.vals.Length < 3)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень выгруженных категорий благоустройства дома, количество полей = " + valuesFromFile.vals.Length + " вместо 3 ");
                    return;
                }

                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_blag(nzp_file , id_prm, name) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код категории в системе поставщика информации
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный код категории в системе поставщика информации");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 2. Уникальный код категории в системе поставщика информации

                #region 3. Наименование категории благоустройства
                string name = CheckType.CheckText2(valuesFromFile, i, true, 60, ref ret, "Наименование категории благоустройства");
                valuesFromFile.sql += name + "); ";
                #endregion 3. Наименование категории благоустройства
                #endregion Версия 1.2
            }
        }


        /// <summary>
        ///  Загрузка 20 секции "Перечень дополнительных характеристик дома "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection20_FileParamsdom(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 20;

            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень дополнительных характеристик дома)
                if (valuesFromFile.vals.Length < 4)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень дополнительных характеристик дома, количество полей = " + valuesFromFile.vals.Length + " вместо 4 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom(nzp_file , id_dom, id_prm, val_prm) " +
                    " VALUES(";
                #endregion Заготовка инсерта



                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код дома в системе поставщика информации
                string id_dom = CheckType.CheckDecimal2(valuesFromFile, i, true, false, null, null, ref ret, "Уникальный код дома в системе поставщика информации");
                valuesFromFile.sql += id_dom + ", ";
                i++;
                #endregion 2. Уникальный код дома в системе поставщика информации

                #region 3. Код параметра дома
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код параметра дома");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 3. Код параметра дома

                #region 4. Значение параметра дома
                string val_prm = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Значение параметра дома");
                valuesFromFile.sql += val_prm + "); ";
                #endregion 4. Значение параметра дома

                #endregion Версия 1.2
            }
        }

        /// <summary>
        ///  Формат 1.3.7 Загрузка 20 секции "Перечень дополнительных характеристик дома "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection20_FileParamsdom_v_137(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 20;

            if (valuesFromFile.Pvers != "1.0")
            {
                if (valuesFromFile.vals.Length < 4)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень дополнительных характеристик дома, количество полей = " + valuesFromFile.vals.Length + " вместо 4 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom(nzp_file , id_dom, id_prm, val_prm) " +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код дома в системе поставщика информации
                string id_dom = CheckType.CheckDecimal2(valuesFromFile, i, true, false, null, null, ref ret, "Уникальный код дома в системе поставщика информации");
                valuesFromFile.sql += id_dom + ", ";
                i++;
                #endregion 2. Уникальный код дома в системе поставщика информации

                #region 3. Код параметра дома
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код параметра дома");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 3. Код параметра дома

                #region 4. Значение параметра дома
                string val_prm = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Значение параметра дома");
                valuesFromFile.sql += val_prm + "); ";
                #endregion 4. Значение параметра дома

            }
        }

        /// <summary>
        ///  Формат 1.3.8 Загрузка 20 секции "Перечень дополнительных характеристик дома "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection20_FileParamsdom_v_138(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 20;

            if (valuesFromFile.Pvers != "1.0")
            {
                if (valuesFromFile.vals.Length < 6)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень дополнительных характеристик дома, количество полей = " + valuesFromFile.vals.Length + " вместо 4 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql = " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom(nzp_file , id_dom, id_prm, val_prm, dats_val, datpo_val) " +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код дома в системе поставщика информации
                string id_dom = CheckType.CheckDecimal2(valuesFromFile, i, true, false, null, null, ref ret, "Уникальный код дома в системе поставщика информации");
                valuesFromFile.sql += id_dom + ", ";
                i++;
                #endregion 2. Уникальный код дома в системе поставщика информации

                #region 3. Код параметра дома
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код параметра дома");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 3. Код параметра дома

                #region 4. Значение параметра дома
                string val_prm = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Значение параметра дома");
                valuesFromFile.sql += val_prm + ", ";
                i++;
                #endregion 4. Значение параметра дома

                #region 5. Дата начала действия значения
                string dats_val = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата начала действия значения");
                valuesFromFile.sql += dats_val + ", ";
                i++;
                #endregion 5. Дата начала действия значения

                #region 6. Дата окончания действия значения
                string datpo_val = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата окончания действия значения");
                valuesFromFile.sql += datpo_val + "); ";
                #endregion 6. Дата окончания действия значения
            }
        }

        /// <summary>
        ///  Загрузка 21 секции "Перечень дополнительных характеристик лицевого счета "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection21_FileParamsls(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 21;


            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень дополнительных характеристик лицевого счета)
                if (valuesFromFile.vals.Length < 4)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень дополнительных характеристик лицевого счета, количество полей = " + valuesFromFile.vals.Length + " вместо 4 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls(nzp_file , ls_id, id_prm, val_prm) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код лицевого счета в системе поставщика информации
                string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код лицевого счета в системе поставщика информации");
                valuesFromFile.sql += ls_id + ", ";
                i++;
                #endregion 2. Уникальный код лицевого счета в системе поставщика информации

                #region 3. Код параметра лицевого счета
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код параметра лицевого счета");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 3. Код параметра лицевого счета

                #region 4. Значение параметра лицевого счета
                string val_prm = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Значение параметра лицевого счета");
                valuesFromFile.sql += val_prm + "); ";
                #endregion 4. Значение параметра лицевого счета

                #endregion Версия 1.2
            }
        }

        /// <summary>
        ///  Формат 1.3.7 Загрузка 21 секции "Перечень дополнительных характеристик лицевого счета "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection21_FileParamsls_v_137(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 21;


            if (valuesFromFile.Pvers != "1.0")
            {

                if (valuesFromFile.vals.Length < 4)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень дополнительных характеристик лицевого счета, количество полей = " + valuesFromFile.vals.Length + " вместо 4 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls(nzp_file , ls_id, id_prm, val_prm) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код лицевого счета в системе поставщика информации
                string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код лицевого счета в системе поставщика информации");
                valuesFromFile.sql += ls_id + ", ";
                i++;
                #endregion 2. Уникальный код лицевого счета в системе поставщика информации

                #region 3. Код параметра лицевого счета
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код параметра лицевого счета");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 3. Код параметра лицевого счета

                #region 4. Значение параметра лицевого счета
                string val_prm = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Значение параметра лицевого счета");
                valuesFromFile.sql += val_prm + "); ";
                #endregion 4. Значение параметра лицевого счета

            }
        }

        /// <summary>
        ///  Формат 1.3.8 Загрузка 21 секции "Перечень дополнительных характеристик лицевого счета "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection21_FileParamsls_v_138(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 21;


            if (valuesFromFile.Pvers != "1.0")
            {

                if (valuesFromFile.vals.Length < 6)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень дополнительных характеристик лицевого счета, количество полей = " + valuesFromFile.vals.Length + " вместо 4 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls(nzp_file , ls_id, id_prm, val_prm, dats_val, datpo_val) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код лицевого счета в системе поставщика информации
                string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код лицевого счета в системе поставщика информации");
                valuesFromFile.sql += ls_id + ", ";
                i++;
                #endregion 2. Уникальный код лицевого счета в системе поставщика информации

                #region 3. Код параметра лицевого счета
                string id_prm = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код параметра лицевого счета");
                valuesFromFile.sql += id_prm + ", ";
                i++;
                #endregion 3. Код параметра лицевого счета

                #region 4. Значение параметра лицевого счета
                string val_prm = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Значение параметра лицевого счета");
                valuesFromFile.sql += val_prm + ", ";
                i++;
                #endregion 4. Значение параметра лицевого счета

                #region 5. Дата начала действия значения
                string dats_val = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата начала действия значения");
                valuesFromFile.sql += dats_val + ", ";
                i++;
                #endregion 5. Дата начала действия значения

                #region 6. Дата окончания действия значения
                string datpo_val = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата окончания действия значения");
                valuesFromFile.sql += datpo_val + "); ";
                #endregion 6. Дата окончания действия значения
            }
        }

        /// <summary>
        ///  Загрузка 22 секции "Перечень оплат проведенных по лицевому счету "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection22_FileOplats(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 22;


            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень оплат проведенных по лицевому счету)
                if (valuesFromFile.vals.Length < 10)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень оплат проведенных по лицевому счету, количество полей = " + valuesFromFile.vals.Length + " вместо 10 ");
                    return;
                }

                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats(nzp_file , ls_id, type_oper, numplat, dat_opl, " +
                    "dat_uchet, dat_izm, sum_oplat, ist_opl, mes_oplat ";
                if (valuesFromFile.finder.format_name.Trim() == "'1.2.2'") { valuesFromFile.sql += ", nzp_pack, id_serv) "; }
                else { valuesFromFile.sql += ")"; }

                valuesFromFile.sql += " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;

                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код лицевого счета в системе поставщика информации
                string ls_id = CheckType.CheckInt(valuesFromFile.vals[i], true, null, null, ref ret);
                if (!ret.result)
                {
                    ls_id = CheckType.CheckText(valuesFromFile.vals[i], true, 20, ref ret);
                    if (!ret.result)
                    {
                        valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + "Уникальный код лицевого счета в системе поставщика информации");
                    }
                }
                valuesFromFile.sql += ls_id + ", ";
                i++;
                #endregion 2. Уникальный код лицевого счета в системе поставщика информации

                #region 3. Тип операции(1-оплата, 2-сторнирование оплаты)
                string type_oper = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Тип операции(1-оплата, 2-сторнирование оплаты) ");
                valuesFromFile.sql += type_oper + ", ";
                i++;
                #endregion 3. Тип операции(1-оплата, 2-сторнирование оплаты)

                #region 4. Номер платежного документа
                string numplat = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Номер платежного документа");
                valuesFromFile.sql += numplat + ", ";
                i++;
                #endregion 4. Номер платежного документа

                #region 5. Дата оплаты
                string dat_opl = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата оплаты");
                valuesFromFile.sql += dat_opl + ", ";
                i++;
                #endregion 5. Дата оплаты

                #region 6. Дата учета
                string dat_uchet = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата учета");
                valuesFromFile.sql += dat_uchet + ", ";
                i++;
                #endregion 6. Дата учета

                #region 7. Дата корректировки
                string dat_izm = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата корректировки");
                valuesFromFile.sql += dat_izm + ", ";
                i++;
                #endregion 7. Дата корректировки

                #region 8. Сумма оплаты
                string sum_oplat = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Сумма оплаты ");

                }
                valuesFromFile.sql += sum_oplat + ", ";
                i++;
                #endregion 8. Сумма оплаты

                #region 9. Источник оплаты
                string ist_opl = CheckType.CheckText2(valuesFromFile, i, false, 60, ref ret, "Источник оплаты");
                valuesFromFile.sql += ist_opl + ", ";
                i++;
                #endregion 9. Источник оплаты

                #region 10. Месяц, за который произведена оплата
                string mes_oplat = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Месяц, за который произведена оплата ");
                valuesFromFile.sql += mes_oplat;
                i++;
                #endregion 10. Месяц, за который произведена оплата

                if (valuesFromFile.finder.format_name.Trim() == "'1.2.2'")
                {
                    #region 11. Уникальный номер пачки
                    string nzp_pack = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Уникальный номер пачки ");
                    valuesFromFile.sql += ", " + nzp_pack + ", ";
                    i++;
                    #endregion 11. Уникальный номер пачки

                    #region 12. Код услуги
                    string id_serv = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Код услуги ");
                    valuesFromFile.sql += id_serv;
                    i++;
                    #endregion 12. Код услуги
                }
                valuesFromFile.sql += ");";

                #endregion Версия 1.2
            }
        }

        /// <summary>
        /// Формат 1.3.2  Загрузка 22 секции "Перечень оплат проведенных по лицевому счету "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection22_FileOplats_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 22;


            if (valuesFromFile.vals.Length < 13)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: перечень оплат проведенных по лицевому счету, количество полей = " + valuesFromFile.vals.Length + " вместо 13 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_oplats(nzp_file , ls_id, type_oper, numplat, dat_opl, " +
                "dat_uchet, dat_izm, sum_oplat, ist_opl, mes_oplat, nzp_pack, kod_type_opl, nzp_opl) ";

            valuesFromFile.sql += " VALUES(";

            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем

            int i = 1;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код лицевого счета в системе поставщика информации

            string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код лицевого счета в системе поставщика информации");
            valuesFromFile.sql += ls_id + ", ";
            i++;

            #endregion 2. Уникальный код лицевого счета в системе поставщика информации

            #region 3. Тип операции(1-оплата, 2-сторнирование оплаты)

            string type_oper = CheckType.CheckInt2(valuesFromFile, i, true, 1, 2, ref ret, " Тип операции(1-оплата, 2-сторнирование оплаты) ");
            valuesFromFile.sql += type_oper + ", ";
            i++;

            #endregion 3. Тип операции(1-оплата, 2-сторнирование оплаты)

            #region 4. Номер платежного документа

            string numplat = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Номер платежного документа");
            valuesFromFile.sql += numplat + ", ";
            i++;

            #endregion 4. Номер платежного документа

            #region 5. Дата оплаты

            string dat_opl = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата оплаты");
            valuesFromFile.sql += dat_opl + ", ";
            i++;

            #endregion 5. Дата оплаты

            #region 6. Дата учета

            string dat_uchet = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата учета");
            valuesFromFile.sql += dat_uchet + ", ";
            i++;

            #endregion 6. Дата учета

            #region 7. Дата корректировки

            string dat_izm = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата корректировки");
            valuesFromFile.sql += dat_izm + ", ";
            i++;

            #endregion 7. Дата корректировки

            #region 8. Сумма оплаты

            string sum_oplat = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Сумма оплаты ");

            }
            valuesFromFile.sql += sum_oplat + ", ";
            i++;

            #endregion 8. Сумма оплаты

            #region 9. Источник оплаты

            string ist_opl = CheckType.CheckText2(valuesFromFile, i, false, 60, ref ret, "Источник оплаты");
            valuesFromFile.sql += ist_opl + ", ";
            i++;

            #endregion 9. Источник оплаты

            #region 10. Месяц, за который произведена оплата

            string mes_oplat = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Месяц, за который произведена оплата ");
            valuesFromFile.sql += mes_oplat;
            i++;

            #endregion 10. Месяц, за который произведена оплата

            #region 11. Уникальный номер пачки

            string nzp_pack = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Уникальный номер пачки ");
            valuesFromFile.sql += ", " + nzp_pack;
            i++;

            #endregion 11. Уникальный номер пачки

            #region 12. Код типа оплаты
            string kod_type_opl = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Код типа оплаты ");
            valuesFromFile.sql += ", " + kod_type_opl;
            i++;
            #endregion 12. Код типа оплаты

            #region 13. Уникальный код оплатыis_

            string nzp_opl = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Уникальный код оплаты ");
            valuesFromFile.sql += ", " + nzp_opl;
            i++;

            #endregion 13. Уникальный код оплаты

            valuesFromFile.sql += ");";

        }

        /// <summary>
        /// Загрузка 22 секции оплат по формату 1.3.4
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection22_FileOplats_v_134(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 22;


            if (valuesFromFile.vals.Length < 12)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: перечень оплат проведенных по лицевому счету, количество полей = " + valuesFromFile.vals.Length + " вместо 12 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_oplats(nzp_file , ls_id, type_oper, numplat, dat_opl, " +
                "dat_uchet, dat_izm, sum_oplat, ist_opl, mes_oplat, nzp_pack, kod_oplat) ";

            valuesFromFile.sql += " VALUES(";

            #endregion Заготовка инсерта

            #region 1. Тип строки пропускаем

            int i = 1;

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код лицевого счета в системе поставщика информации

            string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код лицевого счета в системе поставщика информации");
            valuesFromFile.sql += ls_id + ", ";
            i++;

            #endregion 2. Уникальный код лицевого счета в системе поставщика информации

            #region 3. Тип операции(1-оплата, 2-сторнирование оплаты)

            string type_oper = CheckType.CheckInt2(valuesFromFile, i, true, 1, 2, ref ret, " Тип операции(1-оплата, 2-сторнирование оплаты) ");
            valuesFromFile.sql += type_oper + ", ";
            i++;

            #endregion 3. Тип операции(1-оплата, 2-сторнирование оплаты)

            #region 4. Номер платежного документа

            string numplat = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Номер платежного документа");
            valuesFromFile.sql += numplat + ", ";
            i++;

            #endregion 4. Номер платежного документа

            #region 5. Дата оплаты

            string dat_opl = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата оплаты");
            valuesFromFile.sql += dat_opl + ", ";
            i++;

            #endregion 5. Дата оплаты

            #region 6. Дата учета

            string dat_uchet = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата учета");
            valuesFromFile.sql += dat_uchet + ", ";
            i++;

            #endregion 6. Дата учета

            #region 7. Дата корректировки

            string dat_izm = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата корректировки");
            valuesFromFile.sql += dat_izm + ", ";
            i++;

            #endregion 7. Дата корректировки

            #region 8. Сумма оплаты

            string sum_oplat = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Сумма оплаты ");

            }
            valuesFromFile.sql += sum_oplat + ", ";
            i++;

            #endregion 8. Сумма оплаты

            #region 9. Источник оплаты

            string ist_opl = CheckType.CheckText2(valuesFromFile, i, false, 60, ref ret, "Источник оплаты");
            valuesFromFile.sql += ist_opl + ", ";
            i++;

            #endregion 9. Источник оплаты

            #region 10. Месяц, за который произведена оплата

            string mes_oplat = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Месяц, за который произведена оплата ");
            valuesFromFile.sql += mes_oplat;
            i++;

            #endregion 10. Месяц, за который произведена оплата

            #region 11. Уникальный номер пачки

            string nzp_pack = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный номер пачки ");
            valuesFromFile.sql += ", " + nzp_pack;
            i++;

            #endregion 11. Уникальный номер пачки


            #region 12. Уникальный код оплаты

            string kod_oplat = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Уникальный код оплаты ");
            valuesFromFile.sql += ", " + kod_oplat;
            i++;

            #endregion 12. Уникальный код оплаты

            valuesFromFile.sql += ");";

        }

        /// <summary>
        ///  Загрузка 23 секции "Перечень недопоставок "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection23_FileNedopost(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 23;


            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень недоспоставок)
                if (valuesFromFile.vals.Length < 8)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень недоспоставок, количество полей = " + valuesFromFile.vals.Length + " вместо 8 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost (nzp_file, ls_id, id_serv, type_ned, temper, dat_nedstart, dat_nedstop, sum_ned) " +
                    " VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;
                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";
                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код лицевого счета в системе поставщика информации
                string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код лицевого счета в системе поставщика информации");
                valuesFromFile.sql += ls_id + ", ";
                i++;
                #endregion 2. Уникальный код лицевого счета в системе поставщика информации

                #region 3. Код услуги в системе постащика
                string id_serv = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Код услуги в системе поставщика");
                valuesFromFile.sql += id_serv + ", ";
                i++;
                #endregion 3. Код услуги в системе поставщика

                #region 4. Тип недопоставки
                string type_ned = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Тип недопоставки ");
                valuesFromFile.sql += type_ned + ", ";
                i++;
                #endregion 4. Тип недопоставки

                #region 5. Температура
                string temper = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Температура ");
                valuesFromFile.sql += temper + ", ";
                i++;
                #endregion 5. Температура

                #region 6. Дата начала недопоставки
                string dat_nedstart = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата начала недопоставки");
                valuesFromFile.sql += dat_nedstart + ", ";
                i++;
                #endregion 6. Дата начала недопоставки

                #region 7. Дата окончания недопоставки
                string dat_nedstop = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата окончания недопоставки");
                valuesFromFile.sql += dat_nedstop + ", ";
                i++;
                #endregion 7. Дата окончания недопоставки

                #region 8. Сумма недопоставки
                string sum_ned = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);

                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Сумма недопоставки ");
                }
                if (sum_ned == null)
                { sum_ned = "0"; }
                valuesFromFile.sql += sum_ned + "); ";
                i++;
                #endregion 8. Сумма недопоставки
                #endregion Версия 1.2
            }
        }

        /// <summary>
        ///  Загрузка 23 секции "Перечень недопоставок "
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection23_FileNedopost_v_133(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 23;

            #region Перечень недоспоставок

            if (valuesFromFile.vals.Length < 9)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень недоспоставок");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost" +
                " (nzp_file, ls_id, id_serv, type_ned, temper, dat_nedstart, dat_nedstop, sum_ned, percent) " +
                " VALUES(";

            #endregion Заготовка инсерта

            //1. Тип строки пропускаем
            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #region 2. Уникальный код лицевого счета в системе поставщика информации

            string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код лицевого счета в системе поставщика информации");
            valuesFromFile.sql += ls_id + ", ";
            i++;

            #endregion 2. Уникальный код лицевого счета в системе поставщика информации

            //3. Код услуги в системе постащика
            string id_serv = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Код услуги в системе поставщика");
            valuesFromFile.sql += id_serv + ", ";
            i++;

            //4. Тип недопоставки
            string type_ned = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Тип недопоставки ");
            valuesFromFile.sql += type_ned + ", ";
            i++;

            //5. Температура
            string temper = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Температура ");
            valuesFromFile.sql += temper + ", ";
            i++;

            //6. Дата начала недопоставки
            string dat_nedstart = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата начала недопоставки");
            valuesFromFile.sql += dat_nedstart + ", ";
            i++;

            //7. Дата окончания недопоставки
            string dat_nedstop = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата окончания недопоставки");
            valuesFromFile.sql += dat_nedstop + ", ";
            i++;

            //8. Сумма недопоставки
            string sum_ned = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Сумма недопоставки ");
            }
            if (sum_ned == null) sum_ned = "0";
            valuesFromFile.sql += sum_ned + ", ";
            i++;


            //9. Процент удержания
            string percent = CheckType.CheckDecimal(valuesFromFile.vals[i], false, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Процент удержания ");
            }
            valuesFromFile.sql += percent + "); ";
            i++;

            #endregion

        }

        /// <summary>
        /// Загрузка 24 секции "Перечень типов недопоставки" 
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection24_FileTypenedopost(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 24;


            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень типов недопоставок)

                if (valuesFromFile.vals.Length < 3)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень типов недопоставок, количество полей = " + valuesFromFile.vals.Length + " вместо 3 ");
                    return;
                }
                #region Заготовка инсерта
                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_typenedopost (nzp_file, type_ned, ned_name) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;
                //nzp_file
                valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";
                #endregion 1. Тип строки пропускаем

                #region 2. Тип недопоставки
                string type_ned = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Тип недопоставки ");
                valuesFromFile.sql += type_ned + ", ";
                i++;
                #endregion 2. Тип недопоставки

                #region 3. Наименование недопоставки
                string ned_name = CheckType.CheckText2(valuesFromFile, i, true, 100, ref ret, "Наименование недопоставки");
                valuesFromFile.sql += ned_name + "); ";
                i++;
                #endregion 3. Наименование недопоставки

                #endregion Версия 1.2
            }
        }

        /// <summary>
        ///  Загрузка 25 секции "Перечень услуг лицевого счета"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection25_FileServls(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 25;

            if (valuesFromFile.Pvers != "1.0")
            {
                #region Версия 1.2 (Перечень услуг лицевого счета)

                if (valuesFromFile.vals.Length < 5)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Неправильный формат файла загрузки: перечень услуг лицевого счета, количество полей = " + valuesFromFile.vals.Length + " вместо 5 ");
                    return;
                }
                #region Заготовка инсерта
                //valuesFromFile.sql =
                //    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_servls (nzp_file, ls_id, id_serv, dat_start, dat_stop, supp_id) VALUES(";
                #endregion Заготовка инсерта

                #region 1. Тип строки пропускаем
                int i = 1;
                //nzp_file
                //valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";
                #endregion 1. Тип строки пропускаем

                #region 2. Уникальный код лицевого в системе поставщика услуг

                string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код лицевого счета в системе поставщика информации");
                //valuesFromFile.sql += ls_id + ", ";
                i++;
                #endregion 2. Уникальный код лицевого в системе поставщика услуг

                #region 3. Код услуги в системе поставщика информации
                string id_serv = CheckType.CheckText2(valuesFromFile, i, true, 100, ref ret, "Код услуги в системе поставщика информации");
                //valuesFromFile.sql += id_serv + ", ";
                i++;
                #endregion 3. Код услуги в системе поставщика информации

                #region 4. Дата начала действия услуг
                string dat_start = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата начала действия услуг");
                //valuesFromFile.sql += dat_start + ", ";
                i++;
                #endregion 4. Дата начала действия услуг

                #region 5. Дата окончиная действия услуг
                string dat_stop = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата окончания действия услуг");
                //valuesFromFile.sql += dat_stop + ", ";
                i++;
                #endregion 5. Дата окончиная действия услуг

                //if (versionNameFull.Trim() == "'1.2.2'")
                //{
                #region 6. Уникальный код поставщика
                string supp_id = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, 0, null, ref ret);
                if (!ret.result)
                {
                    valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Уникальный код поставщика");
                    // в формате в шестом поле уникальный код договора вместо уникального кода поставщика 
                }
                //valuesFromFile.sql += supp_id;
                i++;
                #endregion 6. Уникальный код поставщика

                //}
                //else
                //    sql += "null";

                //valuesFromFile.sql += "); ";

                valuesFromFile.sql =
                    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_servls (" +
                    " nzp_file, ls_id, id_serv, dat_start, dat_stop, supp_id) " +
                    " VALUES(" + valuesFromFile.finder.nzp_file + ", " + ls_id + ", " + id_serv + ", " + dat_start + ", " +
                    dat_stop + ", " + supp_id + ");";

                #endregion Версия 1.2
            }
        }

        /// <summary>
        ///  Загрузка 26 секции "Пачки реестров"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection26_FilePack(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 26;


            #region Версия 1.2.2

            if (valuesFromFile.vals.Length < 6)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: пачки реестров, количество полей = " + valuesFromFile.vals.Length + " вместо 6 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_pack (id, nzp_file, dat_plat, num_plat, sum_plat, kol_plat) VALUES( ";

            #endregion Заготовка инсерта

            #region 1. Уникальный номер пачки (тип сроки пропускаем)

            int i = 1;
            string id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный номер пачки ");
            valuesFromFile.sql += id + ", ";
            i++;

            #endregion 1.

            #region 2. nzp_file

            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion

            #region 3. Дата платежного поручения

            string dat_plat = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата платежного поручения");
            valuesFromFile.sql += dat_plat + ", ";
            i++;

            #endregion

            #region 4. Номер платежного поручения

            string num_plat = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Номер платежного поручения ");
            valuesFromFile.sql += num_plat + ", ";
            i++;

            #endregion

            #region 5. Сумма платежа

            string sum_plat = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Сумма платежа ");

            }
            valuesFromFile.sql += sum_plat + ", ";
            i++;

            #endregion

            #region 6. Количество платежей, вошедших в платежное поручение

            string kol_plat = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Количество платежей, вошедших в платежное поручение ");
            valuesFromFile.sql += kol_plat + "); ";
            i++;

            #endregion

            #endregion

        }
        /// <summary>
        /// Загрузка 26 секции "Пачки реестров" по формату 1.3.4
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection26_FilePack_v134(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 26;


            if (valuesFromFile.vals.Length < 8)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: пачки реестров, количество полей = " + valuesFromFile.vals.Length + " вместо 8 ");
                return;
            }

            #region Заготовка инсерта

            //valuesFromFile.sql =
            //    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
            //    "file_pack (id, nzp_file, dat_plat, num_plat, sum_plat, kol_plat, kod_type_opl, is_raspr) VALUES( ";

            #endregion Заготовка инсерта

            #region 1. Уникальный номер пачки (тип сроки пропускаем)

            int i = 1;
            string id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный номер пачки ");
            //valuesFromFile.sql += id + ", ";
            i++;

            #endregion 1.

            #region 2. nzp_file

            //nzp_file
            //valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            #endregion

            #region 3. Дата платежного поручения

            string dat_plat = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата платежного поручения");
            //valuesFromFile.sql += dat_plat + ", ";
            i++;

            #endregion

            #region 4. Номер платежного поручения

            string num_plat = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Номер платежного поручения ");
            //valuesFromFile.sql += num_plat + ", ";
            i++;

            #endregion

            #region 5. Сумма платежа

            string sum_plat = CheckType.CheckDecimal(valuesFromFile.vals[i], true, true, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Сумма платежа ");

            }
            //valuesFromFile.sql += sum_plat + ", ";
            i++;

            #endregion

            #region 6. Количество платежей, вошедших в платежное поручение

            string kol_plat = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Количество платежей, вошедших в платежное поручение ");
            //valuesFromFile.sql += kol_plat + ", ";
            i++;

            #endregion

            #region 7.Код типа оплаты

            string kod_type_opl = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код типа оплаты ");
            //valuesFromFile.sql += kod_type_opl + ", ";
            i++;

            #endregion

            #region 8.Признак распределения пачки

            string is_raspr = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Код типа оплаты ");
            //valuesFromFile.sql += is_raspr + "); ";
            i++;

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                " file_pack (id, nzp_file, dat_plat, num_plat, sum_plat, kol_plat, kod_type_opl, is_raspr) " +
                " VALUES( " + id + ", " + valuesFromFile.finder.nzp_file + ", " + dat_plat + ", " + num_plat + ", " +
                sum_plat + ", " + kol_plat + ", " + kod_type_opl + ", " + is_raspr + ");";

            #endregion

        }

        /// <summary>
        ///  Загрузка 27 секции "Юридические лица (арендаторы и собственники)"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection27_FileUrlic(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 27;

            #region Версия 1.2.2

            if (valuesFromFile.vals.Length < 25)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: юридические лица (арендаторы и собственники), количество полей = " + valuesFromFile.vals.Length + " вместо 25 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_urlic (nzp_file, urlic_id, urlic_name, " +
                " jur_address, fact_address, inn, kpp, rs, bank, bik_bank, ks, tel_chief, tel_b, chief_name,chief_post, " +
                " b_name, okonh1, okonh2, okpo, bank_pr, bank_adr, bik, rs_pr, ks_pr, post_and_name, nzp_payer) " +
                " VALUES(";

            #endregion Заготовка инсерта

            //1. Тип строки пропускаем, nzp_file
            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            //2. Уникальный код ЮЛ
            string supp_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный код ЮЛ ");
            valuesFromFile.sql += supp_id + ", ";
            i++;

            //3. Наименование ЮЛ
            string supp_name = CheckType.CheckText2(valuesFromFile, i, true, 100, ref ret, " Наименование ЮЛ");
            valuesFromFile.sql += supp_name + ", ";
            i++;

            //4. Юридический адрес
            string jur_address = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Юридический адрес");
            valuesFromFile.sql += jur_address + ", ";
            i++;

            //5. Фактический адрес
            string fact_address = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Фактический адрес");
            valuesFromFile.sql += fact_address + ", ";
            i++;

            //6. ИНН
            string inn = CheckType.CheckText2(valuesFromFile, i, true, 12, ref ret, " ИНН");
            valuesFromFile.sql += inn + ", ";
            i++;

            //7. КПП
            string kpp = CheckType.CheckText2(valuesFromFile, i, true, 9, ref ret, " КПП");
            valuesFromFile.sql += kpp + ", ";
            i++;

            //8. Расчетный счет
            string rs = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Расчетный счет");
            valuesFromFile.sql += rs + ", ";
            i++;

            //9. Банк
            string bank = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Банк");
            valuesFromFile.sql += bank + ", ";
            i++;

            //10. БИК банка
            string bik_bank = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " БИК банка");
            valuesFromFile.sql += bik_bank + ", ";
            i++;

            //11. Корреспондентский счет
            string ks = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Корреспондентский счет");
            valuesFromFile.sql += ks + ", ";
            i++;

            //12. Телефон руководителя
            string tel_chief = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Телефон руководителя");
            valuesFromFile.sql += tel_chief + ", ";
            i++;

            //13. Телефон бухгалтерии
            string tel_b = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Телефон бухгалтерии");
            valuesFromFile.sql += tel_b + ", ";
            i++;

            //14. ФИО руководителя
            string chief_name = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " ФИО руководителя");
            valuesFromFile.sql += chief_name + ", ";
            i++;

            //15. Должность руководителя
            string chief_post = CheckType.CheckText2(valuesFromFile, i, false, 40, ref ret, " Должность руководителя");
            valuesFromFile.sql += chief_post + ", ";
            i++;

            //16. ФИО бухгалтера
            string b_name = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " ФИО бухгалтера");
            valuesFromFile.sql += b_name + ", ";
            i++;

            //17. ОКОНХ1
            string okonh1 = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКОНХ1");
            valuesFromFile.sql += okonh1 + ", ";
            i++;

            //18. ОКОНХ2
            string okonh2 = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКОНХ2");
            valuesFromFile.sql += okonh2 + ", ";
            i++;

            //19. ОКПО
            string okpo = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " ОКПО");
            valuesFromFile.sql += okpo + ", ";
            i++;

            //20. Банк предприятия
            string bank_pr = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, " Банк предприятия");
            valuesFromFile.sql += bank_pr + ", ";
            i++;

            //21. Адрес банка

            string bank_adr = CheckType.CheckText(valuesFromFile.vals[i], false, 100, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Адрес банка");
            }
            valuesFromFile.sql += bank_adr + ", ";
            i++;

            //22. БИК
            string bik = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " БИК");
            valuesFromFile.sql += bik + ", ";
            i++;

            //23. Р/счет предприятия
            string rs_pr = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Р/счет предприятия");
            valuesFromFile.sql += rs_pr + ", ";
            i++;

            //24. К/счет предприятия
            string ks_pr = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " К/счет предприятия");
            valuesFromFile.sql += ks_pr + ", ";
            i++;

            //25. Должность + ФИО в Р.п.
            string post_and_name = CheckType.CheckText2(valuesFromFile, i, false, 200, ref ret, " Должность + ФИО в Р.п.");
            valuesFromFile.sql += post_and_name + ", ";
            i++;

            //Конец запроса
            valuesFromFile.sql += " null);";
            i++;

            #endregion

        }

        /// <summary>
        /// Формат 1.3.2  Загрузка 27 секции "Реестр ЛС"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection27_Reestr_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 27;

            if (valuesFromFile.vals.Length < 9)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: Реестр ЛС, количество полей = " + valuesFromFile.vals.Length + " вместо 9 ");
                return;
            }

            #region Версия 1.3.2

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                " file_reestr_ls (nzp_file, ls_id_supp, ls_id_ns, ls_pkod, dat_open, open_osnov, dat_close, close_osnov, ls_id_sz) " +
                " VALUES(";

            #endregion Заготовка инсерта

            //1. Тип строки пропускаем, nzp_file
            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            //2. Лс в системе поставщика
            string ls_id_supp = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, " Лс в системе поставщика ");
            valuesFromFile.sql += ls_id_supp + ", ";
            i++;

            //3. Лс в наследуемой системе
            string ls_id_ns = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, " Лс в наследуемой системе ");
            valuesFromFile.sql += ls_id_ns + ", ";
            i++;

            // 4. Лс (пкод) поставщика
            string ls_pkod = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, " Лс поставщика");
            valuesFromFile.sql += ls_pkod + ", ";
            i++;

            //5. Дата открытия ЛС
            string open_date = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата открытия ЛС");
            valuesFromFile.sql += open_date + ", ";
            i++;

            //6. Основание открытия ЛС
            string osnov = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Основание открытия ЛС");
            valuesFromFile.sql += osnov + ", ";
            i++;

            // 7. Дата закрытия ЛС
            string dat_close = CheckType.CheckDateTime2(valuesFromFile, i, false, ref ret, "Дата закрытия ЛС");
            valuesFromFile.sql += dat_close + ", ";
            i++;

            // 8. Основание закрытия ЛС
            string close_osnov = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Основание закрытия ЛС");
            valuesFromFile.sql += close_osnov + ", ";
            i++;

            //9. Лс в соц защите
            string ls_id_sz = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " Лс в соц защите");
            valuesFromFile.sql += ls_id_sz;
            i++;

            //Конец запроса
            valuesFromFile.sql += " );";
            i++;

            #endregion
        }

        /// <summary>
        ///  Загрузка 28 секции "Реестр временно-убывших"
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection28_FileVrub(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 28;

            #region Версия 1.2.2

            if (valuesFromFile.vals.Length < 5)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: реестр временно убывших, количество полей = " + valuesFromFile.vals.Length + " вместо 5 ");
                return;
            }

            #region Заготовка инсерта

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_vrub (nzp_file, ls_id, gil_id, dat_vrvib, dat_end)  VALUES(";

            #endregion Заготовка инсерта

            #region Insert

            //1. Тип строки пропускаем, nzp_file
            int i = 1;
            //nzp_file
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            //2. Уникальный номер лицевого счета
            string ls_id = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, " Уникальный номер лицевого счета");
            valuesFromFile.sql += ls_id + ", ";
            i++;

            //3. Уникальный номер гражданина
            string gil_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, " Уникальный номер гражданина ");
            valuesFromFile.sql += gil_id + ", ";
            i++;

            //4. Дата начала
            string dat_vrvib = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " Дата начала");
            valuesFromFile.sql += dat_vrvib + ", ";
            i++;

            //5. Дата окончания
            string dat_end = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " Дата окончания");
            valuesFromFile.sql += dat_end + "); ";
            i++;

            #endregion

            #endregion
        }

        /// <summary>
        ///  Загрузка 30 секции расчетный счет
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection30_RS_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 30;

            #region Версия 1.3.2

            if (valuesFromFile.vals.Length < 6)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: расчетный счет, количество полей = " + valuesFromFile.vals.Length + " вместо 6 ");
                return;
            }

            #region Insert

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest +
                "file_rs (nzp_file, rs, id_bank, id_urlic, ks, bik)  VALUES(";


            //1. Тип строки пропускаем, nzp_file
            int i = 1;
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            //2. Расчетный счет
            string rs = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Расчетный счет");
            valuesFromFile.sql += rs + ", ";
            i++;

            //3. Код банка 
            string id_bank = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Уникальный код банка");
            valuesFromFile.sql += id_bank + ", ";
            i++;

            //4. Код ЮЛ
            string id_ul = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Уникальный код ЮЛ");
            valuesFromFile.sql += id_ul + ", ";
            i++;

            //5. Кор счет
            string ks = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, "Кор. счет");
            valuesFromFile.sql += ks + ", ";
            i++;

            //6. БИК банка
            string bik = CheckType.CheckText2(valuesFromFile, i, false, 20, ref ret, " БИК банка");
            valuesFromFile.sql += bik + "); ";
            i++;

            #endregion

            #endregion
        }

        /// <summary>
        ///  Загрузка 31 секции соглашения по перечислениям
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection31_SoglPoPerech_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 31;

            #region Версия 1.3.2

            if (valuesFromFile.vals.Length < 9)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: соглашения по перечислениям, количество полей = " + valuesFromFile.vals.Length + " вместо 9 ");
                return;
            }

            #region Insert

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_agreement" +
                " (nzp_file,  id_dog, id_dom, id_serv_from,  id_urlic_agent, id_serv_to, percent, dat_s, dat_po) " +
                " VALUES(";


            //1. Тип строки пропускаем, nzp_file
            int i = 1;
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", ";

            //2. Код договора
            string id_dog = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код договора");
            valuesFromFile.sql += id_dog + ", ";
            i++;

            //3. Уникальный код дома в системе отправителя
            string id_dom = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код дома в системе отправителя");
            valuesFromFile.sql += id_dom + ", ";
            i++;

            //4. Код услуги, с которой рассчитывается комиссия
            string id_serv_from = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код услуги, с которой рассчитывается комиссия");
            valuesFromFile.sql += id_serv_from + ", ";
            i++;

            //5. Код агента-получателя комиссии
            string id_ul = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код агента-получателя комиссии");
            if (id_ul == "0")
            {
                id_ul = "1000000";
            }

            valuesFromFile.sql += id_ul + ", ";
            i++;

            //6. Код услуги, на какую начисляется комиссия
            string id_serv_to = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код услуги, на какую начисляется комиссия");
            valuesFromFile.sql += id_serv_to + ", ";
            i++;

            //7. Процент удержания
            string percent = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Процент удержания ");

            }
            valuesFromFile.sql += percent + ", ";
            i++;

            //8. Дата начала действия соглашения
            string dat_s = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " Дата начала действия соглашения");
            valuesFromFile.sql += dat_s + ", ";
            i++;

            //9. Дата окончания действия соглашения
            string dat_po = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, " Дата окончания действия соглашения");
            valuesFromFile.sql += dat_po + "); ";
            i++;

            #endregion

            #endregion
        }

        /// <summary>
        /// Загрузка 32 секции распределения оплат
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection32_RaspredOplat_v_132(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 32;

            #region Версия 1.3.2

            if (valuesFromFile.vals.Length < 5)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: соглашения по перечислениям, количество полей = " + valuesFromFile.vals.Length + " вместо 5 ");
                return;
            }

            #region Insert

            //valuesFromFile.sql =
            //    " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_raspr" +
            //    " (nzp_file,  kod_oplat, id_serv, id_dog, sum_money) " +
            //    " VALUES(";

            #region 1. Тип строки пропускаем
            int i = 1;
            //valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", "; //nzp_file

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код оплаты

            string kod_oplat = CheckType.CheckInt2(valuesFromFile, i, false, null, null, ref ret, " Уникальный код оплаты ");
            //valuesFromFile.sql += kod_oplat + ", ";
            i++;

            #endregion 2. Уникальный код оплаты

            #region 3. Уникальный код услуги
            string id_serv = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Уникальный код услуги");
            //valuesFromFile.sql += id_serv + ", ";
            i++;
            #endregion 3. Уникальный код услуги

            #region 4. Код договора
            string id_dog = CheckType.CheckInt2(valuesFromFile, i, true, 1, null, ref ret, "Код договора");
            //valuesFromFile.sql += id_dog + ", ";
            i++;
            #endregion 4. Код договора

            #region 5. Сумма
            string sum_money = CheckType.CheckDecimal(valuesFromFile.vals[i], true, false, null, null, ref ret);
            if (!ret.result)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text + " Сумма ");
            }
            //valuesFromFile.sql += sum_money + ") ";
            i++;
            #endregion 5. Сумма

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_raspr" +
                " (nzp_file,  kod_oplat, id_serv, id_dog, sum_money) " +
                " VALUES(" + valuesFromFile.finder.nzp_file + ", " + kod_oplat + ", " + id_serv + ", " + id_dog + ", " + sum_money + ");";

            #endregion

            #endregion
        }

        /// <summary>
        /// Загрузка 33 секции перекидок
        /// </summary>
        /// <param name="valuesFromFile"></param>
        public void AddSection33_Perekidki_v_134(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 33;

            if (valuesFromFile.vals.Length < 9)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: соглашения по перечислениям, количество полей = " +
                                          valuesFromFile.vals.Length + " вместо 9 ");
                return;
            }

            #region Insert

            valuesFromFile.sql =
                " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_perekidki" +
                " (nzp_file,  id_ls, id_serv, dog_id, id_type, sum_perekidki, tarif, volum, comment, nzp_kvar, nzp_serv, nzp_supp) " +
                " VALUES(";

            #region 1. Тип строки пропускаем

            int i = 1;
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", "; //nzp_file

            #endregion 1. Тип строки пропускаем

            #region 2. № ЛС в системе поставщика
            string id_ls = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "№ ЛС в системе поставщика");
            valuesFromFile.sql += id_ls + ", ";
            i++;

            #endregion  № ЛС в системе поставщика

            #region 3.Код услуги
            string id_serv = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код услуги ");
            valuesFromFile.sql += id_serv + ", ";
            i++;

            #endregion  3.Код услуги

            #region 4.Код договора
            string dog_id = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код договора ");
            valuesFromFile.sql += dog_id + ", ";
            i++;

            #endregion  4.Код договора

            #region 5.Код типа перекидки
            bool check = false;
            string id_type = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код типа перекидки ");
            valuesFromFile.sql += id_type + ", ";
            i++;
            if (Convert.ToInt32(id_type) > 100)
                check = true;
            #endregion  5.Код типа перекидки

            #region 6.Сумма перекидки
            string sum_perekidki = CheckType.CheckDecimal2(valuesFromFile, i, true, false, null, null, ref ret, "Сумма перекидки");
            valuesFromFile.sql += sum_perekidki + ", ";
            i++;

            #endregion  6.Сумма перекидки

            #region 7.Тариф
            string tarif = CheckType.CheckDecimal2(valuesFromFile, i, check, false, null, null, ref ret, "Тариф");
            valuesFromFile.sql += tarif + ", ";
            i++;

            #endregion 7.Тариф

            #region 8.Расход
            string volum = CheckType.CheckDecimal2(valuesFromFile, i, check, false, null, null, ref ret, "Расход");
            valuesFromFile.sql += volum + ", ";
            i++;

            #endregion 8.Расход

            #region 9.Комментарий
            string comment = CheckType.CheckText2(valuesFromFile, i, false, 100, ref ret, "Комментарий ");
            valuesFromFile.sql += comment + ", ";
            i++;

            #endregion 9.Комментарий

            valuesFromFile.sql += "null, null, null);";
            #endregion
        }

        public void AddSection34_InfLsByPu_v_135(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 34;

            if (valuesFromFile.vals.Length < 3)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: соглашения по перечислениям, количество полей = " +
                                          valuesFromFile.vals.Length + " вместо 3 ");
                return;
            }

            #region Insert
            valuesFromFile.sql =
                            " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_info_pu" +
                            " (nzp_file, id_pu, num_ls_pu) " +
                            " VALUES(";

            #region 1. Тип строки пропускаем

            int i = 1;
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", "; //nzp_file

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код прибора учета в системе поставщика
            string id_pu = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код прибора учета в системе поставщика");
            valuesFromFile.sql += id_pu + ", ";
            i++;

            #endregion Уникальный код прибора учета в системе поставщика

            #region 3. Номер лицевого счета, относящегося к прибору учета
            string num_ls_pu = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Номер лицевого счета, относящегося к прибору учета ");
            valuesFromFile.sql += num_ls_pu + ");";
            i++;

            #endregion  Номер лицевого счета, относящегося к прибору учета

            #endregion
        }

        public void AddSection35_FileDoc_v_1382(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 35;

            if (valuesFromFile.vals.Length < 10)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: соглашения по перечислениям, количество полей = " +
                                          valuesFromFile.vals.Length + " вместо 10 ");
                return;
            }

            #region Insert
            valuesFromFile.sql =
                            " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_doc" +
                            " ( nzp_file, uniq_doc_code, num_ls, " +
                            "   nzp_gil, urlic_id, fam, " +
                            "   ima, otch, birth_date, doc_sobstv_code) " +
                            " VALUES(";

            #region 1. Тип строки пропускаем

            int i = 1;
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", "; //nzp_file

            #endregion 1. Тип строки пропускаем

            #region 2. Уникальный код документа собственности
            string uniq_doc_code = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Уникальный код документа собственности");
            valuesFromFile.sql += uniq_doc_code + ", ";
            i++;

            #endregion Уникальный код документа собственности

            #region 3. Номер лицевого счета
            string num_ls = CheckType.CheckText2(valuesFromFile, i, true, 20, ref ret, "Номер лицевого счета");
            valuesFromFile.sql += num_ls + ", ";
            i++;

            #endregion  Номер лицевого счета

            #region 4. Уникальный номер гражданина собственника
            string nzp_gil = CheckType.CheckInt2(valuesFromFile, i, true, 0, 0, ref ret, "Уникальный номер гражданина собственника");
            valuesFromFile.sql += nzp_gil + ", ";
            i++;

            #endregion  Уникальный номер гражданина собственника

            #region 5. Код юридического лица
            string urlic_id = CheckType.CheckInt2(valuesFromFile, i, true, 0, 0, ref ret, "Код юридического лица");
            valuesFromFile.sql += urlic_id + ", ";
            i++;

            #endregion Код юридического лица

            #region 6. Фамилия собственника
            string fam = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, "Фамилия собственника");
            valuesFromFile.sql += fam + ", ";
            i++;

            #endregion Фамилия собственника

            #region 7. Имя собственника
            string ima = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, "Имя собственника");
            valuesFromFile.sql += ima + ", ";
            i++;

            #endregion Имя собственника

            #region 8. Отчество собственника
            string otch = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, "Отчество собственника");
            valuesFromFile.sql += otch + ", ";
            i++;

            #endregion Отчество собственника

            #region 9. Дата рождения собственника
            string birth_date = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата рождения собственника");
            valuesFromFile.sql += birth_date + ", ";
            i++;

            #endregion Дата рождения собственника

            #region 10. Код документа собственника
            string doc_sobstv_code = CheckType.CheckInt2(valuesFromFile, i, true, 0, 0, ref ret, "Код документа собственника");
            valuesFromFile.sql += doc_sobstv_code + ", ";
            i++;

            #endregion Код документа собственника

            #endregion
        }

        public void AddSection40_FileGroup_v_1382(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 40;

            if (valuesFromFile.vals.Length < 6)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: соглашения по перечислениям, количество полей = " +
                                          valuesFromFile.vals.Length + " вместо 6 ");
                return;
            }

            #region Insert
            valuesFromFile.sql =
                            " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_group" +
                            " (nzp_file, group_code, group_name, " +
                            "  dat_s, dat_po, table_code) " +
                            " VALUES(";

            #region 1. Тип строки пропускаем

            int i = 1;
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", "; //nzp_file

            #endregion 1. Тип строки пропускаем

            #region 2. Код группы норматива
            string group_code = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код группы норматива");
            valuesFromFile.sql += group_code + ", ";
            i++;

            #endregion Код группы норматива

            #region 3. Наименование группы норматива
            string group_name = CheckType.CheckText2(valuesFromFile, i, true, 90, ref ret, "Наименование группы норматива");
            valuesFromFile.sql += group_name + ", ";
            i++;

            #endregion  Наименование группы норматива

            #region 4. Дата начала действия норматива
            string dat_s = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата начала действия норматива");
            valuesFromFile.sql += dat_s + ", ";
            i++;

            #endregion Дата начала действия норматива

            #region 5. Дата окончания действия норматива
            string dat_po = CheckType.CheckDateTime2(valuesFromFile, i, true, ref ret, "Дата окончания действия норматива");
            valuesFromFile.sql += dat_po + ", ";
            i++;

            #endregion Дата окончания действия норматива

            #region 6. Код таблицы норматива
            string table_code = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код таблицы норматива");
            valuesFromFile.sql += table_code + "); ";
            i++;

            #endregion Код таблицы норматива

            #endregion
        }

        public void AddSection41_FileNormTable_v_1382(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            valuesFromFile.sectionNumber = 41;

            if (valuesFromFile.vals.Length < 7)
            {
                valuesFromFile.err.Append(valuesFromFile.rowNumber + ret.text +
                                          " Неправильный формат файла загрузки: соглашения по перечислениям, количество полей = " +
                                          valuesFromFile.vals.Length + " вместо 7 ");
                return;
            }

            #region Insert
            valuesFromFile.sql =
                            " INSERT INTO  " + Points.Pref + DBManager.sUploadAliasRest + "file_norm_table" +
                            " (nzp_file, table_code, str_name, " +
                            "  str_code, col_name, col_code, znach) " +
                            " VALUES(";

            #region 1. Тип строки пропускаем

            int i = 1;
            valuesFromFile.sql += valuesFromFile.finder.nzp_file + ", "; //nzp_file

            #endregion 1. Тип строки пропускаем

            #region 2. Код таблицы норматива
            string table_code = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код таблицы норматива");
            valuesFromFile.sql += table_code + ", ";
            i++;

            #endregion Код таблицы норматива

            #region 3. Наименование строки норматива
            string str_name = CheckType.CheckText2(valuesFromFile, i, true, 200, ref ret, "Наименование строки норматива");
            valuesFromFile.sql += str_name + ", ";
            i++;

            #endregion  Наименование строки норматива

            #region 4. Код строки норматива
            string str_code = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код строки норматива");
            valuesFromFile.sql += str_code + ", ";
            i++;

            #endregion Код строки норматива

            #region 5. Наименование столбца норматива
            string col_name = CheckType.CheckText2(valuesFromFile, i, true, 80, ref ret, "Наименование столбца норматива");
            valuesFromFile.sql += col_name + ", ";
            i++;

            #endregion  Наименование столбца норматива

            #region 6. Код столбца норматива
            string col_code = CheckType.CheckInt2(valuesFromFile, i, true, null, null, ref ret, "Код столбца норматива");
            valuesFromFile.sql += col_code + ", ";
            i++;

            #endregion Код столбца норматива

            #region 7. Значение норматива
            string znach = CheckType.CheckText2(valuesFromFile, i, true, 30, ref ret, "Значение норматива");
            valuesFromFile.sql += znach + "); ";
            i++;

            #endregion Значение норматива

            #endregion
        }

        public void InsertIntoFileSql(IDbConnection con_db, DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            if (valuesFromFile.sql.Trim() != "")
            {
                valuesFromFile.sql = valuesFromFile.sql.Replace("\"", " ").Replace("\'", "$");

                string sql_in_table =
                    " insert into " + Points.Pref + DBManager.sUploadAliasRest + "file_sql " +
                    " values(" + valuesFromFile.rowNumber1 + "," + valuesFromFile.finder.nzp_file.ToString() + ", '" + valuesFromFile.sql + "') ";
                ret = ExecSQL(con_db, sql_in_table);

                if (!ret.result)
                {
                    valuesFromFile.err.Append(" Ошибка загрузки файла в базу данных.  Ошибка в секции " + valuesFromFile.sectionNumber + ". Номер строки из файла: " + valuesFromFile.rowNumber1 + Environment.NewLine);
                    MonitorLog.WriteLog(" Ошибка записи в таблицу file_sql. Секция: " + valuesFromFile.sectionNumber + ".\n Имя файла:" + valuesFromFile.fileName + ". Номер строки из файла: " + valuesFromFile.rowNumber1, MonitorLog.typelog.Error, true);
                }
            }
        }

        public string GetInsertIntoFileSql(DbValuesFromFile valuesFromFile)
        {
            Returns ret = new Returns();
            string sql_in_table = String.Empty;
            if (valuesFromFile.sql.Trim() != "")
            {
                valuesFromFile.sql = valuesFromFile.sql.Replace("\"", " ").Replace("\'", "$");

                sql_in_table =
                    " insert into " + Points.Pref + DBManager.sUploadAliasRest + "file_sql (id, nzp_file, sql_zapr) " +
                    " values(" + valuesFromFile.rowNumber1 + "," + valuesFromFile.finder.nzp_file.ToString() + ", '" + valuesFromFile.sql + "'); ";
            }
            return sql_in_table;
        }

        public void InsertIntoListFileSql(IDbConnection con_db, string sql)
        {
            Returns ret = new Returns();
            string sql_in_table = String.Empty;
            if (sql.Trim() != "")
                {
                    sql_in_table += sql;
                }
            if (sql_in_table != String.Empty)
            {
                ret = ExecSQL(con_db, sql_in_table);

                if (!ret.result)
                {
                    valuesFromFile.err.Append(" Ошибка загрузки файла в базу данных.  Ошибка в секции " + valuesFromFile.sectionNumber + ". Номер строки из файла: " + valuesFromFile.rowNumber1 + Environment.NewLine);
                    MonitorLog.WriteLog(" Ошибка записи в таблицу file_sql. Секция: " + valuesFromFile.sectionNumber + ".\n Имя файла:" + valuesFromFile.fileName + ". Номер строки из файла: " + valuesFromFile.rowNumber1, MonitorLog.typelog.Error, true);
                }
            }

        }

        private Dictionary<int, sectionDelegate> FillSectionsMethods(string versionNameFull)
        {
            Dictionary<int, sectionDelegate> dict = new Dictionary<int, sectionDelegate>();
            switch (versionNameFull)
            {
                case "1.2.1":
                case "1.2.2":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_FileArea);
                    dict.Add(3, AddSection03_FileDom);
                    dict.Add(4, AddSection04_FileKvar);
                    dict.Add(5, AddSection05_FileSupp);
                    dict.Add(6, AddSection06_FileServ);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp);
                    dict.Add(9, AddSection09_FileOdpu);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom);
                    dict.Add(21, AddSection21_FileParamsls);
                    dict.Add(22, AddSection22_FileOplats);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection25_FileServls);
                    dict.Add(27, AddSection26_FilePack);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    break;
                case "1.3.2":
                case "1.3.3":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_132);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_132);
                    dict.Add(6, AddSection06_FileServ_v_132);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom);
                    dict.Add(21, AddSection21_FileParamsls);
                    dict.Add(22, AddSection22_FileOplats_v_132);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    break;
                case "1.3.4":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_132);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_133);
                    dict.Add(6, AddSection06_FileServ_v_132);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom);
                    dict.Add(21, AddSection21_FileParamsls);
                    dict.Add(22, AddSection22_FileOplats_v_134);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack_v134);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    dict.Add(33, AddSection33_Perekidki_v_134);
                    break;
                case "1.3.5":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_132);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_133);
                    dict.Add(6, AddSection06_FileServ_v_132);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu_v_135);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom);
                    dict.Add(21, AddSection21_FileParamsls);
                    dict.Add(22, AddSection22_FileOplats_v_134);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack_v134);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    dict.Add(33, AddSection33_Perekidki_v_134);
                    dict.Add(34, AddSection34_InfLsByPu_v_135);
                    break;
                case "1.3.6":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_132);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_133);
                    dict.Add(6, AddSection06_FileServ_v_136);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu_v_135);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom);
                    dict.Add(21, AddSection21_FileParamsls);
                    dict.Add(22, AddSection22_FileOplats_v_134);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack_v134);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    dict.Add(33, AddSection33_Perekidki_v_134);
                    dict.Add(34, AddSection34_InfLsByPu_v_135);
                    break;
                case "1.3.7":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_138);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_133);
                    dict.Add(6, AddSection06_FileServ_v_138);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu_v_135);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom_v_137);
                    dict.Add(21, AddSection21_FileParamsls_v_137);
                    dict.Add(22, AddSection22_FileOplats_v_134);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack_v134);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    dict.Add(33, AddSection33_Perekidki_v_134);
                    dict.Add(34, AddSection34_InfLsByPu_v_135);
                    break;
                case "1.3.8":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_138);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_133);
                    dict.Add(6, AddSection06_FileServ_v_138);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu_v_135);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom_v_138);
                    dict.Add(21, AddSection21_FileParamsls_v_138);
                    dict.Add(22, AddSection22_FileOplats_v_134);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack_v134);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    dict.Add(33, AddSection33_Perekidki_v_134);
                    dict.Add(34, AddSection34_InfLsByPu_v_135);
                    break;
                case "1.3.8.1":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_138);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_133);
                    dict.Add(6, AddSection06_FileServ_v_138);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu_v_1381);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu_v_1381);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom_v_138);
                    dict.Add(21, AddSection21_FileParamsls_v_138);
                    dict.Add(22, AddSection22_FileOplats_v_134);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack_v134);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    dict.Add(33, AddSection33_Perekidki_v_134);
                    dict.Add(34, AddSection34_InfLsByPu_v_135);
                    break;
                case "1.3.8.2":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_138);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_133);
                    dict.Add(6, AddSection06_FileServ_v_138);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu_v_1381);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu_v_1381);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec_v_1382);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom_v_138);
                    dict.Add(21, AddSection21_FileParamsls_v_138);
                    dict.Add(22, AddSection22_FileOplats_v_134);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack_v134);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    dict.Add(33, AddSection33_Perekidki_v_134);
                    dict.Add(34, AddSection34_InfLsByPu_v_135);
                    dict.Add(35, AddSection35_FileDoc_v_1382);
                    dict.Add(40, AddSection40_FileGroup_v_1382);
                    dict.Add(41, AddSection41_FileNormTable_v_1382);
                    break;
                case "1.3.8.3":
                    dict.Add(1, AddSection01_FileHead);
                    dict.Add(2, AddSection02_29_FileUrlic_v_138);
                    dict.Add(3, AddSection03_FileDom_v_132);
                    dict.Add(4, AddSection04_FileKvar_v_132);
                    dict.Add(5, AddSection05_FileSupp_v_133);
                    dict.Add(6, AddSection06_FileServ_v_138);
                    dict.Add(7, AddSection07_FileKvarp);
                    dict.Add(8, AddSection08_FileServp_v_132);
                    dict.Add(9, AddSection09_FileOdpu_v_1381);
                    dict.Add(10, AddSection10_FileOdpuP);
                    dict.Add(11, AddSection11_FileIpu_v_1381);
                    dict.Add(12, AddSection12_FileIpu_p);
                    dict.Add(13, AddSection13_FileServices);
                    dict.Add(14, AddSection14_FileMo);
                    dict.Add(15, AddSection15_FileGilec_v_1382);
                    dict.Add(16, AddSection16_FileTypeparams);
                    dict.Add(17, AddSection17_FileGaz);
                    dict.Add(18, AddSection18_FileVoda);
                    dict.Add(19, AddSection19_FileBlag);
                    dict.Add(20, AddSection20_FileParamsdom_v_138);
                    dict.Add(21, AddSection21_FileParamsls_v_138);
                    dict.Add(22, AddSection22_FileOplats_v_134);
                    dict.Add(23, AddSection23_FileNedopost);
                    dict.Add(24, AddSection24_FileTypenedopost);
                    dict.Add(25, AddSection25_FileServls);
                    dict.Add(26, AddSection26_FilePack_v134);
                    dict.Add(27, AddSection27_Reestr_v_132);
                    dict.Add(28, AddSection28_FileVrub);
                    dict.Add(29, AddSection02_29_FileUrlic_v_132);
                    dict.Add(30, AddSection30_RS_v_132);
                    dict.Add(31, AddSection31_SoglPoPerech_v_132);
                    dict.Add(32, AddSection32_RaspredOplat_v_132);
                    dict.Add(33, AddSection33_Perekidki_v_134);
                    dict.Add(34, AddSection34_InfLsByPu_v_135);
                    dict.Add(35, AddSection35_FileDoc_v_1382);
                    dict.Add(40, AddSection40_FileGroup_v_1382);
                    dict.Add(41, AddSection41_FileNormTable_v_1382);
                    break;
            }
            return dict;
        }

    }
}
