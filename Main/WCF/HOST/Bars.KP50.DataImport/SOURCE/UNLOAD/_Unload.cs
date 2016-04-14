using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.DataImport.SOURCE.EXCHANGE
{
    /// <summary>
    /// Вспомогательный класс для передачи параметров
    /// </summary>
    public class FilesExchange
    {
        /// <summary>
        /// Уникальный код файла, 
        /// уникальный код пользователя, 
        /// уникальный код версии файла
        /// </summary>
        public int nzp_file, nzp_user, nzp_version;

        /// <summary>
        /// Префикс локального банка
        /// </summary>
        public string bankPref;

        /// <summary>
        /// Расширение файла для обмена, 
        /// полный путь к файлу
        /// </summary>
        public string fileExtension, fullFileName;

        /// <summary>
        /// Загруженное название файла,
        /// уникальное название файла,
        /// название формата (N.N.N)
        /// </summary>
        public string loadedName, savedName, versionName;

        /// <summary>
        /// Резервное поле
        /// </summary>
        public string whereString;

        /// <summary>
        /// Период для загрузки/выгрузки
        /// </summary>
        public DateTime selectedDate;

        /// <summary>
        /// Разделитель полей в файле
        /// </summary>
        public char delimiter;

        /// <summary>
        /// Тип обмена: 1-'Загрузка', 2-'Выгрузка'
        /// </summary>
        public int exchangeType;

        /// <summary>
        /// Секции для обмена: (Название секции, Необходимость обработки)
        /// </summary>
        public Dictionary<string, bool> selectedSections;

        /// <summary>
        /// Конструктор
        /// </summary>
        public FilesExchange()
        {
            nzp_user = -1;
            nzp_file = -1;
            nzp_version = -1;
            exchangeType = -1;
            selectedDate = DateTime.Now;
            fileExtension = ".txt";
            fullFileName = "";
            loadedName = "";
            savedName = "";
            versionName = "";
            bankPref = "";
            whereString = "";
            delimiter = '|';
            selectedSections = new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// ВЫГРУЗКА ФАЙЛА "ХАРАКТЕРИСТИКИ ЖИЛОГО ФОНДА И НАЧИСЛЕНИЯ ЖКУ" 
    /// </summary>
    public abstract class DbDataUnload : DataBaseHeadServer
    {

        #region Поля класса
        /// <summary>
        /// Уникальный код файла, 
        /// уникальный код пользователя, 
        /// уникальный код версии файла
        /// </summary>
        protected int NzpFile, NzpUser, NzpVersion;

        /// <summary>
        /// Префикс локального банка
        /// </summary>
        public string BankPref;

        /// <summary>
        /// Расширение файла для обмена, 
        /// полный путь к файлу
        /// </summary>
        public string FileExstention, FullUnlFileName;

        /// <summary>
        /// Загруженное название файла,
        /// уникальное название файла,
        /// название формата (N.N.N)
        /// </summary>
        public string LoadedName, SavedName, VersionName;

        /// <summary>
        /// Резервное поле
        /// </summary>
        public string WhereString;

        /// <summary>
        /// Период для загрузки/выгрузки
        /// </summary>
        public DateTime SelectedDate;

        /// <summary>
        /// Разделитель полей в файле
        /// </summary>
        public char Delimiter;

        /// <summary>
        /// Секции для обмена: (Название секции, Необходимость обработки)
        /// </summary>
        public Dictionary<string, bool> SelectedSections;

        /// <summary>
        /// StringBuilder для заполнения информацией
        /// </summary>
        protected StringBuilder InfoText;

        /// <summary>
        /// StringBuilder для логирования выгрузки
        /// </summary>
        protected StringBuilder LogInfo;

        /// <summary>
        /// делегат вызова ф-ций для обработки секций
        /// </summary>
        /// <returns></returns>
        protected delegate Returns SectionsDelegates();

        /// <summary>
        /// Секции для обмена: (Название секции, Сигнатура ф-ции для обработки данной секции)
        /// </summary>
        protected Dictionary<string, SectionsDelegates> SectionMethods;

        public DbDataUnload()
        {
            NzpUser = NzpVersion = NzpFile = -1;
            FileExstention = ".txt";
            FullUnlFileName = LoadedName = SavedName = VersionName = "";
            BankPref = WhereString = "";
            Delimiter = '|';
            SelectedDate = DateTime.Now;
            SelectedSections = new Dictionary<string, bool>();
            InfoText = new StringBuilder();
            LogInfo = new StringBuilder();
            SectionMethods = new Dictionary<string, SectionsDelegates>();
        }
        #endregion Поля класса

        /// <summary>
        /// Ф-ция проверки колонки на наличие пустых значений
        /// </summary>
        /// <param name="columnName">Название колонки дял проверки</param>
        /// <param name="tblName">Таблица, в которой проверяем</param>
        /// <param name="message">Сбщ, выводимое в файл </param>
        protected void CheckColumnOnEmptiness(string columnName,  string tblName, string message)
        {
            int emptyRecordsCnt = 0;
            string sql =
                " SELECT COUNT(*) as count FROM " + Points.Pref + DBManager.sUploadAliasRest + tblName + " " +
                " WHERE nzp_file = " + NzpFile +
                " AND " + columnName + " IS NULL ";
            emptyRecordsCnt = Convert.ToInt32(OpenSQL(sql).resultData.Rows[0]["count"]);
            if (emptyRecordsCnt > 0)
            {
                LogInfo.Append("[!] Имеются "+ message + " в кол-ве: " + emptyRecordsCnt + Environment.NewLine);
            }

        }


        /// <summary>
        /// Запись StringBuilder'ов в отдельные файлы
        /// </summary>
        /// <param name="files">Текст-Полный путь для записи</param>
        /// <returns></returns>
        public static Returns Compress(Dictionary<StringBuilder, string> files)
        {
            Returns ret = new Returns();
            ret.result = true;

            foreach (var t in files)
            {
                //проверяем директорию, если не существует - создаем
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(t.Value)))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(t.Value));
                }

                //записываем в файл
                StreamWriter sw = new StreamWriter(t.Value, false, System.Text.Encoding.GetEncoding(1251));
                sw.Flush();
                sw.Write(t.Key);
                sw.Close();
                sw.Dispose();

                //архивируем файл
                string outputArchiveFullName = System.IO.Path.GetDirectoryName(t.Value) + "\\" +System.IO.Path.GetFileNameWithoutExtension(t.Value) + ".zip";
                ret.text = outputArchiveFullName;
                if (!Archive.GetInstance().Compress(outputArchiveFullName, new string[] { t.Value }, true))
                {
                    ret.text = "Ошибка при архивации файла!";
                    ret.tag = -1;
                    ret.result = false;
                    return ret;
                }
            }
            
            return ret;
        }

        /// <summary>
        /// Добавлении информации о файле 
        /// </summary>
        /// <param name="versionName">Название формата файла</param>
        /// <returns></returns>
        public Returns GetNzpFile(string versionName)
        {
            Returns ret = new Returns();

            try
            {
                #region Вытаскиваем NzpFile
                string sql =
                " SELECT nzp_version FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_versions " +
                " WHERE version_name = \'" + versionName + "\'";
                ret = ExecSQL(sql);
                if (!ret.result)
                {
                    ret.text = " Ошибка при получения версии файла. ";
                    ret.tag = -1;
                    throw new Exception(" Ошибка выполнения процедуры GetNzpFile при получении версии файла " + FullUnlFileName +
                        Environment.NewLine + " Проверьте таблицу " + Points.Pref + DBManager.sUploadAliasRest + "  file_versions ");
                }
                sql =
                   " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                   " ( nzp_version, loaded_name, saved_name, nzp_status, " +
                   "    created_by, created_on, percent, pref, file_type) " +
                   " VALUES (" + NzpVersion + ",'" + LoadedName + "',\'" + SavedName + "\',1," +
                   NzpUser + "," + sCurDateTime + ", 0, '" + BankPref + "', 1)  ";
                ExecSQL(sql);
                NzpFile = GetSerialValue(ServerConnection);

                #endregion Вытаскиваем NzpFile

            }
            catch (Exception ex)
            {
                ret.text = "Ошибка добавления информации о файле.";
                ret.result = false;
                ret.tag = -1;
                throw new Exception("Ошибка выполнения процедуры GetNzpFile: " + ex.Message);
            }
            return ret;
        }

        public abstract Returns Run(FilesExchange filesExchange);

        protected Returns FillFileHeadInfo()
        {
            Returns ret = new Returns();

            InfoText.Append(
                "1" + Delimiter +
                VersionName + Delimiter +
                "Хар-ки ЖФ и начисления ЖКУ" + Delimiter +
                "OOO 'Тест'" + Delimiter +
                "OOO 'Тест'" + Delimiter +
                "12345678910" + Delimiter +
                "123456789" + Delimiter +
                "707" + Delimiter +
                DateTime.Now.ToShortDateString() + Delimiter +
                "89172998877" + Delimiter +
                "Иванов" + Delimiter +
                SelectedDate + Delimiter +
                "1" + Delimiter +
                Environment.NewLine);
            return ret;
        }

        protected Returns FillUkInfo()
        {
            Returns ret = new Returns();
            string sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "unl_area" +
                " (nzp_file, area, jur_address, fact_address, " +
                " inn, kpp, rs, bank, bik, ks, nzp_area )" +
                " SELECT " +
                NzpFile + ", " + "a.area, 'Юр.адрес', 'Фактич.адрес', " +
                " 'ИНН', 'КПП', 'Рас.счет', 'Банк', 'БИК', 'Корр.счет',  " +
                " a.nzp_area" +
                " FROM " + BankPref + DBManager.sDataAliasRest + "s_area a";
            ExecSQL(sql);
            return ret;
        }

        protected Returns FillUrlicInfo()
        {
            Returns ret = new Returns();
            string sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "unl_urlic" +
                " ( nzp_file, urlic_id, urlic_name, urlic_name_s, " +
                "  inn, kpp, bik, ks, " +
                " is_area, is_supp, is_arendator, is_rc, " +
                " is_rso, is_agent, is_subabonent" +
                " ) " +
                " SELECT " + NzpFile + ", nzp_payer, npayer, cast(payer as char(10)), " +
                " inn, kpp, bik, ks, " +
                " 0, 0, 0, 0," +
                " 0, 0, 0 " +
                " FROM " +  Points.Pref + DBManager.sKernelAliasRest +"s_payer";
            ExecSQL(sql);

            #region Заполнение поля: '7. ИНН'

            CheckColumnOnEmptiness("inn", "unl_urlic", "ЮЛ без инн");

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_urlic SET  inn = '0' " +
              " WHERE nzp_file = " + NzpFile +
              " AND inn IS NULL ";
            ExecSQL(sql);
            #endregion Заполнение поля: '7. ИНН'

            #region Заполнение поля: '8.	КПП'

            CheckColumnOnEmptiness("kpp", "unl_urlic", "ЮЛ без кпп");

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_urlic SET  kpp = '0' " +
              " WHERE nzp_file = " + NzpFile +
              " AND kpp IS NULL ";
            ExecSQL(sql);
            #endregion Заполнение поля: '8.	КПП'

            //Заполнение поля: '18.	Признак того что предприятие является управляющей компанией (территория)'
            SetUrlicSign("is_area", 3);

            //Заполнение поля: '19.	Признак того что предприятие является поставщиком услуг'
            SetUrlicSign("is_supp", 2);

            //Заполнение поля: '20.	Признак того что предприятие является арендатором или собственником помещений'
            SetUrlicSign("is_arendator", 7);

            //Заполнение поля: '21.	Признак того что предприятие является РЦ'
            SetUrlicSign("is_rc", 5);

            //Заполнение поля: '22.	Признак того что предприятие является РСО'
            SetUrlicSign("is_rso", 6);

            //Заполнение поля: '23.	Признак того что предприятие является платежным агентом'
            SetUrlicSign("is_agent", 4);

            //Заполнение поля: '24.	Признак того что предприятие является субабонентом'
            SetUrlicSign("is_subabonent", 9);

            return ret;
        }

        private void SetUrlicSign(string columnName, int signNum)
        {
            string sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_urlic SET  " + columnName + " = 1 " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS ( SELECT 1 FROM " + Points.Pref + DBManager.sKernelAliasRest + "payer_types pt " +
              "   WHERE pt.nzp_payer =  " + Points.Pref + DBManager.sUploadAliasRest + "unl_urlic.urlic_id " +
              "   AND pt.nzp_payer_type =  " + signNum +
              " ) ";
            ExecSQL(sql);
        }

        protected Returns FillHousesInfo()
        {
            Returns ret = new Returns();
            string sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom" +
                " (nzp_file, nzp_dom, " +
                " ndom, nkor, nzp_area, nzp_ul, " +
                " odpu_row_number, kod_kladr, kod_fias " +
                " )" +
                " SELECT " + NzpFile + ", " + " a.nzp_dom, " +
                " a.ndom, a.nkor, a.nzp_area, a.nzp_ul," +
                " 0, 0, 0 " +
                " FROM " + BankPref + DBManager.sDataAliasRest + "dom a";
            ExecSQL(sql);

            #region Заполнение поля: '7. Номер дома'
            
            CheckColumnOnEmptiness("ndom", "unl_dom", "дома без номера");

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  ndom = '-' " +
              " WHERE nzp_file = " + NzpFile +
              " AND ndom IS NULL ";
            ExecSQL(sql);
            #endregion Заполнение поля: '7. Номер дома'

            #region Заполнение поля: '6. Наименование улицы
            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  (ulica, nzp_raj) = " +
              " ( " +
              "  (SELECT " + DBManager.sNvlWord + "(ulica, '')||' '||" + DBManager.sNvlWord + "(ulicareg, '') " +
              "     FROM " + BankPref + DBManager.sDataAliasRest + "s_ulica u " +
              "     WHERE u.nzp_ul = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_ul), " +
              "  (SELECT nzp_raj " +
              "     FROM " + BankPref + DBManager.sDataAliasRest + "s_ulica u " +
              "     WHERE u.nzp_ul = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_ul)" +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "s_ulica u " +
              "                         WHERE u.nzp_ul = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_ul " +
              "              )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("ulica", "unl_dom", "дома без улицы");
            #endregion Заполнение поля: '6.	Наименование улицы'

            #region Заполнение поля: '5. Село/деревня'
            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  (rajon, nzp_town) = " +
              " ( " +
              "  (SELECT rajon " +
              "     FROM " + BankPref + DBManager.sDataAliasRest + "s_rajon r " +
              "     WHERE r.nzp_raj = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_raj), " +
              "  (SELECT nzp_town " +
              "     FROM " + BankPref + DBManager.sDataAliasRest + "s_rajon r " +
              "     WHERE r.nzp_raj = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_raj) " +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "s_rajon r " +
              "                         WHERE r.nzp_raj = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_raj " +
              "              )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("rajon", "unl_dom", "дома без села/деревни");
            #endregion Заполнение поля: '5.	Село/деревня'

            #region Заполнение поля: '4. Город/район'
            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  town = " +
              " ( SELECT town " +
              "     FROM " + BankPref + DBManager.sDataAliasRest + "s_town t " +
              "     WHERE t.nzp_town = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_town " +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "s_town t " +
              "                         WHERE t.nzp_town = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_town " +
              "              )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("town", "unl_dom", "дома без города/района");
            #endregion Заполнение поля: '4.	Город/район'
            
            #region Заполнение параметра: '10. Категория благоустройства'
            sql =
               " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  cat_blago = " +
               " ( " +
               " SELECT val_prm FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               " WHERE p.nzp_prm = 2001 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "       AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               " )" +
               " WHERE nzp_file = " + NzpFile +
               " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               "               WHERE p.nzp_prm = 2001 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "               AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               "               )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("cat_blago", "unl_dom", "дома без категории благоустройства");
            #endregion Заполнение параметра: '10. Категория благоустройства'

            #region Заполнение параметра: '11. Этажность'
            sql =
               " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  etazh = " +
               " ( " +
               " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               " WHERE p.nzp_prm = 37 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "       AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               " )" +
               " WHERE nzp_file = " + NzpFile +
               " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               "               WHERE p.nzp_prm = 37 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "               AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               "               )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("etazh","unl_dom", "дома без информации об этажности");

            sql =
            " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  etazh = 1 " +
            " WHERE nzp_file = " + NzpFile +
            " AND etazh IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '11. Этажность'

            #region Заполнение параметра: '12. Год постройки'
            sql =
               " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  build_year = " +
               " ( " +
               " SELECT CAST(val_prm AS DATE) FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               " WHERE p.nzp_prm = 150 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "       AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               " ) " +
               " WHERE nzp_file = " + NzpFile +
               " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               "               WHERE p.nzp_prm = 150 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "               AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               "               )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("build_year", "unl_dom", "дома без кода постройки");
            #endregion Заполнение параметра: '12. Год постройки'

            #region Заполнение параметра: '13. Общая площадь'
            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  total_square = " +
                " ( " +
                " SELECT CAST(val_prm AS "+DBManager.sDecimalType+"(14,2)) FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
                " WHERE p.nzp_prm = 40 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
                "       AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
                " )" +
                " WHERE nzp_file = " + NzpFile +
                " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
                "               WHERE p.nzp_prm = 40 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
                "               AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
                "               )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("total_square", "unl_dom", "дома без общей площади");

            sql =
             " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  total_square = 0 " +
             " WHERE nzp_file = " + NzpFile +
             " AND total_square IS NULL ";
            ExecSQL(sql);
            #endregion Заполнение параметра: '13. Общая площадь'

            #region Заполнение параметра: '14.	Площадь мест общего пользования'
            sql =
               " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  mop_square = " +
               " ( " +
               " SELECT CAST(val_prm AS " + DBManager.sDecimalType + "(14,2)) FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               " WHERE p.nzp_prm = 2049 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "       AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               " )" +
               " WHERE nzp_file = " + NzpFile +
               " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               "               WHERE p.nzp_prm = 2049 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "               AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               "               )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("mop_square", "unl_dom", "дома без информации о площади мест общего пользования");

            sql =
             " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  mop_square = 0 " +
             " WHERE nzp_file = " + NzpFile +
             " AND mop_square IS NULL ";
            ExecSQL(sql);
            #endregion Заполнение параметра: '14.	Площадь мест общего пользования'

            #region Заполнение параметра: '15.	Полезная (отапливаемая площадь)'
            sql =
               " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  useful_square = " +
               " ( " +
               " SELECT CAST(val_prm AS " + DBManager.sDecimalType + "(14,2)) FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               " WHERE p.nzp_prm = 36 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "       AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               " )" +
               " WHERE nzp_file = " + NzpFile +
               " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_2 p " +
               "               WHERE p.nzp_prm = 36 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom " +
               "               AND p.is_actual <> 100 AND dat_s < CAST( '" + SelectedDate + "' as DATE) AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               "               )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("useful_square", "unl_dom", "дома без информации о полезной (отапливаемой площади)");

            sql =
             " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  useful_square = 0 " +
             " WHERE nzp_file = " + NzpFile +
             " AND useful_square IS NULL ";
            ExecSQL(sql);
            #endregion Заполнение параметра: '15.	Полезная (отапливаемая площадь)'

            #region Заполнение параметра: '16.	Код Муниципального образования '
            //TODO: не разбираем, а надо!!
            #endregion Заполнение параметра: '16.	Код Муниципального образования '

            #region Заполнение параметра: '17.	Дополнительные характеристики дома '
            //не разбираем это поле, поэтому не выгружаем
            #endregion Заполнение параметра: '17.	Дополнительные характеристики дома '

            #region Заполнение параметра: '18.	Количество строк - лицевой счет'

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  ls_row_number = " +
                " ( " +
                " SELECT COUNT(*) FROM " + BankPref + DBManager.sDataAliasRest + "kvar k" +
                " WHERE nzp_dom in " +
                "               ( " +
                "                   SELECT nzp_dom FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom d" +
                "                   WHERE nzp_file =  " + NzpFile +
                "                   AND d.nzp_dom = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom" +
                "                )" +
                " ) " +
                " WHERE nzp_file = " + NzpFile +
                " AND  EXISTS (SELECT 1 FROM " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom d" +
                "                 WHERE nzp_file =  " + NzpFile +
                "                 AND d.nzp_dom = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_dom" +
                "               )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("ls_row_number", "unl_dom", "дома без информации о кол-ве строк - ЛС");

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  ls_row_number = 0 " +
              " WHERE nzp_file = " + NzpFile +
              " AND ls_row_number IS NULL ";
            ExecSQL(sql);
            #endregion Заполнение параметра: '18.	Количество строк - лицевой счет'

            #region Заполнение параметра: '19.	Количество строк - общедомовой прибор учета'
            //TODO: НАДО ДОПИСАТЬ! odpu_row_number
            #endregion Заполнение параметра: '19.	Количество строк - общедомовой прибор учета'

            #region Заполнение параметра: '20. Код Улицы (КЛАДР) '

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  kod_kladr = " +
              " ( " +
              "  (SELECT soato " +
              "     FROM " + BankPref + DBManager.sDataAliasRest + "s_ulica u " +
              "     WHERE u.nzp_ul = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_ul) " +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "s_ulica u " +
              "                         WHERE u.nzp_ul = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom.nzp_ul " +
              "              )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("kod_kladr", "unl_dom", "дома бз кода улицы (КЛАДР)");

            sql =
             " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dom SET  kod_kladr = '0' " +
             " WHERE nzp_file = " + NzpFile +
             " AND kod_kladr IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '20. Код Улицы (КЛАДР) '
            
            return ret;
        }

        protected Returns FillLsInfo()
        {
            Returns ret = new Returns();
            string sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar " +
                " ( nzp_file, nzp_kvar, num_ls, nzp_dom, " +
                " typek, fio, nkvar, nkvar_n, nzp_area, nzp_geu," +
                " kol_gil, kol_vrem_prib, kol_vrem_ub, room_number, " +
                " total_square, living_square, otapl_square, naim_square, " +
                " is_communal, is_el_plita, is_gas_plita, is_gas_colonka, is_fire_plita, " +
                " is_open_otopl, service_row_number, reval_params_row_number, ipu_row_number " +
                " ) " +
                " SELECT " + NzpFile + ", k.nzp_kvar, k.num_ls, k.nzp_dom, " +
                " k.typek, k.fio, k.nkvar, k.nkvar_n, k.nzp_area, k.nzp_geu, " +
                " 0, 0, 0, 1, " +
                " 0, 0, 0, 0," +
                " 0, 0, 0, 0, 0," +
                "0, 0, 0, 0 " +
                " FROM " + BankPref + DBManager.sDataAliasRest + "kvar k";
            ExecSQL(sql);


            #region Заполнение параметра: '9.	Дата рождения квартиросъемщика'
            //не разбираем это поле, поэтому не выгружаем
            #endregion Заполнение параметра: '9.	Дата рождения квартиросъемщика'

            #region Заполнение поля: '10.	Квартира'

            CheckColumnOnEmptiness("nkvar", "unl_kvar", "ЛС без номера квартиры");

            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  nkvar = '-' " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND nkvar IS NULL ";
            ExecSQL(sql);
            #endregion Заполнение поля: '10.	Квартира'

            #region Заполнение параметра: '12.	Дата открытия ЛС'

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  open_date = " +
                " ( " +
                " SELECT dat_s FROM " + BankPref + DBManager.sDataAliasRest + "prm_3 p " +
                " WHERE p.nzp_prm = 51 " +
                "   AND val_prm = '1' " +
                "   AND p.is_actual <> 100 " +
                "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
                "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
                "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
                " )" +
                " WHERE nzp_file = " + NzpFile +
                " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_3 p " +
                "              WHERE p.nzp_prm = 51 AND val_prm = '1' " +
                "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
                "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
                "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
                "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("open_date", "unl_kvar", "ЛС без информации о дате открытия ЛС");

            #endregion Заполнение параметра: '12.	Дата открытия ЛС'

            #region Заполнение параметра: '13.	Основание открытия ЛС'
            //не разбираем это поле, поэтому не выгружаем
            #endregion Заполнение параметра: '13.	Основание открытия ЛС'

            #region Заполнение параметра: '14.	Дата закрытия ЛС'

            sql =
               " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  close_date = " +
               " ( " +
               " SELECT dat_s FROM " + BankPref + DBManager.sDataAliasRest + "prm_3 p " +
               " WHERE p.nzp_prm = 51 " +
               "   AND val_prm = '2' " +
               "   AND p.is_actual <> 100 " +
               "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
               "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
               "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               " )" +
               " WHERE nzp_file = " + NzpFile +
               " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_3 p " +
               "              WHERE p.nzp_prm = 51 AND val_prm = '2' " +
               "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
               "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
               "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
               "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("close_date", "unl_kvar", "ЛС без информации о дате закрытия ЛС");
            #endregion Заполнение параметра: '14.	Дата закрытия ЛС'

            #region Заполнение параметра: '15.	Основание закрытия ЛС'
            //не разбираем это поле, поэтому не выгружаем
            #endregion Заполнение параметра: '15.	Основание закрытия ЛС'

            #region Заполнение параметра: '16.	Количество проживающих'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  kol_gil = " +
              " ( " +
              " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 5 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " )" +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 5 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("kol_gil", "unl_kvar", "ЛС без информации о количесиве проживающих");
            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  kol_gil = 0 " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND kol_gil IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '16.	Количество проживающих'

            #region Заполнение параметра: '17.	Количество врем. прибывших жильцов'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  kol_vrem_prib = " +
              " ( " +
              " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 131 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " )" +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 131 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("kol_vrem_prib", "unl_kvar", "ЛС без информации о количестве врем.прибывших жильцов");
            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  kol_vrem_prib = 0 " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND kol_vrem_prib IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '17.	Количество врем. прибывших жильцов'

            #region Заполнение параметра: '18.	Количество  врем. убывших жильцов'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  kol_vrem_ub = " +
              " ( " +
              " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 10 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " )" +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 10 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("kol_vrem_prib", "unl_kvar", "ЛС без информации о количестве врем.убывших жильцов");
            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  kol_vrem_ub = 0 " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND kol_vrem_ub IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '18.	Количество  врем. убывших жильцов'

            #region Заполнение параметра: '19.	Количество комнат'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  room_number = " +
              " ( " +
              " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 19 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " )" +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 19 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("room_number", "unl_kvar", "ЛС без информации о количестве комнат");
            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  room_number = 1 " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND room_number IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '19.	Количество комнат'

            #region Заполнение параметра: '20.	Общая площадь'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  total_square = " +
              " ( " +
              " SELECT CAST(val_prm AS " + DBManager.sDecimalType + ") FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 4 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " )" +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 4 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("total_square", "unl_kvar", "ЛС без информации об общей площади");
            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  total_square = 0 " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND total_square IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '20.	Общая площадь'

            #region Заполнение параметра: '21.	Жилая площадь '

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  living_square = " +
              " ( " +
              " SELECT CAST(val_prm AS " + DBManager.sDecimalType + ") FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 6 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " )" +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 6 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("living_square","unl_kvar", "ЛС без информации о жилой площади");

            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  living_square = 0 " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND living_square IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '21.	Жилая площадь '

            #region Заполнение параметра: '22.	Отапливаемая площадь'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  otapl_square = " +
              " ( " +
              " SELECT CAST(val_prm AS " + DBManager.sDecimalType + ") FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 6 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " )" +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 6 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("otapl_square","unl_kvar", "ЛС без информации об отапливаемой площади");

            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  otapl_square = 0 " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND otapl_square IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '22.	Отапливаемая площадь'

            #region Заполнение параметра: '23.	Площадь для найма'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  naim_square = " +
              " ( " +
              " SELECT CAST(val_prm AS " + DBManager.sDecimalType + ") FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 314 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " )" +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 314 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";

            ExecSQL(sql);

            CheckColumnOnEmptiness("naim_square", "unl_kvar", "ЛС без информации о площаде для найма");
            sql =
                 " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  naim_square = 0 " +
                 " WHERE nzp_file = " + NzpFile +
                 " AND naim_square IS NULL ";
            ExecSQL(sql);

            #endregion Заполнение параметра: '23.	Площадь для найма'

            #region Заполнение параметра: '24.	Признак коммунальной квартиры(1-да, 0 –нет)'
            //TODO: не разбираем, а надо!!
            #endregion Заполнение параметра: '24.	Признак коммунальной квартиры(1-да, 0 –нет)'

            #region Заполнение параметра: '25.	Наличие эл. плиты (1-да, 0 –нет)'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  is_el_plita = " +
              " ( " +
              " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 19 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 19 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("is_el_plita", "unl_kvar", "ЛС без информации о наличии эл.плиты");

            #endregion Заполнение параметра: '25.	Наличие эл. плиты (1-да, 0 –нет)'

            #region Заполнение параметра: '26.	Наличие газовой плиты (1-да, 0 –нет)'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  is_gas_plita = " +
              " ( " +
              " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 551 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 551 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("is_gas_plita", "unl_kvar", "ЛС без информации о наличии газовой плиты");

            #endregion Заполнение параметра: '26.	Наличие газовой плиты (1-да, 0 –нет)'

            #region Заполнение параметра: '27.	Наличие газовой колонки (1-да, 0 –нет)'

            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  is_gas_colonka = " +
              " ( " +
              " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 1 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 1 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("is_gas_colonka", "unl_kvar", "ЛС без информации о наличии газовой колонки");

            #endregion Заполнение параметра: '27.	Наличие газовой колонки (1-да, 0 –нет)'

            #region Заполнение параметра: '28.	Наличие огневой плиты (1-да, 0 –нет)'
            //отсутствует данный параметр
            //TODO: не разбираем, а надо?
            #endregion Заполнение параметра: '28.	Наличие огневой плиты (1-да, 0 –нет)'

            #region Заполнение параметра: '29.	Код типа жилья по газоснабжению (из справочника)'
            //TODO: не разбираем, а надо?
            #endregion Заполнение параметра: '29.	Код типа жилья по газоснабжению (из справочника)'

            #region Заполнение параметра: '30.	Код типа жилья по водоснабжению (из справочника)'
            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  water_type = " +
              " ( " +
              " SELECT CAST(val_prm AS INTEGER) FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              " WHERE p.nzp_prm = 7 " +
              "   AND p.is_actual <> 100 " +
              "   AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS (SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "prm_1 p " +
              "              WHERE p.nzp_prm = 7 " +
              "                   AND p.is_actual <> 100 AND p.nzp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_kvar " +
              "                   AND dat_s < CAST( '" + SelectedDate + "' as DATE) " +
              "                   AND dat_po > CAST('" + SelectedDate + "' as DATE) " +
              "             )";
            ExecSQL(sql);

            CheckColumnOnEmptiness("water_type", "unl_kvar", "ЛС без информации коде типа жидья по водоснабжению");
            #endregion Заполнение параметра: '30.	Код типа жилья по водоснабжению (из справочника)'

            #region Заполнение параметра: '31.	Код типа жилья по горячей воде (из справочника)'
            //TODO: не разбираем, а надо?
            #endregion Заполнение параметра: '31.	Код типа жилья по горячей воде (из справочника)'

            #region Заполнение параметра: '32.	Код типа жилья по канализации (из справочника)'
            //TODO: не разбираем, а надо?
            #endregion Заполнение параметра: '32.	Код типа жилья по канализации (из справочника)'

            #region Заполнение параметра: '33.	Наличие забора из открытой системы отопления (1-да, 0 –нет)'
            //TODO: не разбираем, а надо?
            #endregion Заполнение параметра: '33.	Наличие забора из открытой системы отопления (1-да, 0 –нет)'

            #region Заполнение параметра: '34.	Дополнительные характеристики ЛС '
            //не разбираем это поле, поэтому не выгружаем
            #endregion Заполнение параметра: '34.	Дополнительные характеристики ЛС '

            #region Заполнение параметра: '35.	Количество строк - услуга'
            //TODO: надо дописать
            #endregion Заполнение параметра: '35.	Количество строк - услуга'

            #region Заполнение параметра: '36.	Количество строк  – параметры в месяце перерасчета лицевого счета'
            //TODO: надо дописать
            #endregion Заполнение параметра: '36.	Количество строк  – параметры в месяце перерасчета лицевого счета'

            #region Заполнение параметра: '37.	Количество строк – индивидуальный прибор учета'
            //TODO: надо дописать
            #endregion Заполнение параметра: '37.	Количество строк – индивидуальный прибор учета'

            #region Заполнение параметра: '38.	Уникальный код ЮЛ для арендатора'
            //не разбираем это поле, поэтому не выгружаем
            #endregion Заполнение параметра: '38.	Уникальный код ЮЛ для арендатора'
            
            #region Заполнение параметра: '39.	Тип владения '
            //TODO: не разбираем это поле!
            #endregion Заполнение параметра: '39.	Тип владения '

            #region Заполнение параметра: '40.	Уникальный код жильца квартиросъемщика'
            //выгружать это поле не нужно
            #endregion Заполнение параметра: '40.	Уникальный код жильца квартиросъемщика'
            
            #region Заполнение параметра: '41.	Участок (может быть ЖЭУ)'
            //uch
            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar SET  uch = " +
              " ( " +
              " SELECT geu FROM " + BankPref + DBManager.sDataAliasRest + "s_geu s " +
              " WHERE s.nzp_geu = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_geu " +
              " ) " +
              " WHERE nzp_file = " + NzpFile +
              " AND  EXISTS ( SELECT 1 FROM " + BankPref + DBManager.sDataAliasRest + "s_geu s " +
              " WHERE s.nzp_geu = " + Points.Pref + DBManager.sUploadAliasRest + "unl_kvar.nzp_geu " +
              " ) " ;
            ExecSQL(sql);

            CheckColumnOnEmptiness("uch","unl_kvar", "ЛС без информации об участке");
            #endregion Заполнение параметра: '41.	Участок (может быть ЖЭУ)'
            
            return ret;
        }

        protected Returns FillContractInfo()
        {
        
            Returns ret = new Returns();
            string sql =
               " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "unl_dog " +
               " (nzp_file, nzp_supp,  nzp_payer_agent, " +
               " nzp_payer_princip, nzp_payer_supp, name_supp) " +
               " SELECT " + NzpFile + ", nzp_supp, nzp_payer_agent, " +
               " nzp_payer_princip, nzp_payer_supp, name_supp" +
               " FROM " + BankPref + DBManager.sKernelAliasRest + "supplier";
            ExecSQL(sql);

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dog SET num_dog = " +
                " ( " +
                "   SELECT num_dog FROM " + Points.Pref + DBManager.sDataAliasRest + "fn_dogovor " +
                "   WHERE nzp_supp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dog.nzp_supp " +
                " ) " +
                " WHERE nzp_file = " + NzpFile +
                " AND EXISTS ( " +
                "               SELECT 1 FROM " + Points.Pref + DBManager.sDataAliasRest + "fn_dogovor " +
                "               WHERE nzp_supp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dog.nzp_supp " +
                " ) " +
                "";
            ExecSQL(sql);

            sql =
               " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_dog SET dat_dog = " +
               " ( " +
               "   SELECT dat_dog FROM " + Points.Pref + DBManager.sDataAliasRest + "fn_dogovor " +
               "   WHERE nzp_supp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dog.nzp_supp " +
               " ) " +
               " WHERE nzp_file = " + NzpFile +
               " AND EXISTS ( " +
               "               SELECT 1 FROM " + Points.Pref + DBManager.sDataAliasRest + "fn_dogovor " +
               "               WHERE nzp_supp = " + Points.Pref + DBManager.sUploadAliasRest + "unl_dog.nzp_supp " +
               " )";
            ExecSQL(sql);
            
            return ret;
        }

        protected Returns FillServicesInfo()
        {
            Returns ret = new Returns();

            //todo: дописать проверку SelectedDate на адекватность
            string chargeTbl = BankPref + "_charge_" + (SelectedDate.Year - 2000) +
                               tableDelimiter + "charge_" + SelectedDate.Month.ToString("00");

            string sql = 
                  " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv " +
                  " (" +
                  " nzp_file,           nzp_kvar,       nzp_supp,       nzp_serv,          sum_insaldo,    eot, " +
                  " reg_tarif_percent,  reg_tarif,      fact_rashod,    norm_rashod,       is_pu_calc, " +
                  " sum_nach,           sum_reval,      sum_subsidy,    sum_subsidyp,      sum_lgota, " +
                  " sum_smo,            sum_money,      is_del,         sum_outsaldo,      met_calc " +
                  " ) " +
                  
                  "SELECT " + 
                  NzpFile + ",          nzp_kvar,       nzp_supp,       nzp_serv,          sum_insaldo,    tarif, " +
                  " c_nedop,            tarif_f,        c_calc,         c_sn,              is_device," +
                  " sum_real,           real_charge,    sum_subsidy,    sum_subsidy_reval, sum_lgota, " +
                  " sum_smo,            sum_money,      isdel,          sum_outsaldo,      nzp_frm " + 
                  
                  " FROM " + chargeTbl + 
                  " WHERE nzp_serv > 1 ";
            /*sum_smop,*/
            /*nzp_measure,*/
            /*sum_lgotap,*/
            ExecSQL(sql);
            #region Заполнение поля: '9.	Код единицы измерения расхода (из справочника)'
            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv SET nzp_measure =  " +
              " ( SELECT f.nzp_measure FROM " + Points.Pref + DBManager.sKernelAliasRest + "formuls f " +
              "    WHERE f.nzp_frm = " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv.met_calc " +
              " ) " + 
              " WHERE nzp_file = " + NzpFile +
              " AND EXISTS ( SELECT 1 FROM " + Points.Pref + DBManager.sKernelAliasRest + "formuls f " +
              "              WHERE f.nzp_frm = " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv.met_calc " +
              "             ) " ;
            ExecSQL(sql);

            CheckColumnOnEmptiness("nzp_measure", "unl_serv", "услуга без кода единицы измерения расхода (из справочника)");

            sql =
             " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv SET nzp_measure = 100 " +
             " WHERE nzp_file = " + NzpFile +
             " AND  nzp_measure is null ";
            ExecSQL(sql);
            //TODO: вытащить из таблицы services
            #endregion Заполнение поля: '9.	Код единицы измерения расхода (из справочника)'

            #region Заполнение поля: '12.	Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)'
            CheckColumnOnEmptiness("is_pu_calc", "unl_serv", "услуга без вида расчета по прибору учета");
            sql =
              " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv SET is_pu_calc = 1 " +
              " WHERE nzp_file = " + NzpFile +
              " AND  is_pu_calc = 9 ";
            ExecSQL(sql);
            #endregion Заполнение поля: '12.	Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)'

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv SET is_del = 0 " +
                " WHERE nzp_file = " + NzpFile +
                " AND  is_del IS NULL";
            ExecSQL(sql);

            sql =
                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "unl_serv SET servp_row_number = 0 " +
                " WHERE nzp_file = " + NzpFile +
                " AND  servp_row_number IS NULL";
            ExecSQL(sql);
            
            return ret;
        }

        protected Returns FillOdpuInfo()
        {
              Returns ret = new Returns();

            string dat_s = "  cast('02." + SelectedDate.Month.ToString("00") + "." + SelectedDate.Year +"' as DATE) ";
            string dat_po = " cast('" + SelectedDate.ToShortDateString() + "' as DATE) ";

            string sql =
                " INSERT INTO " + Points.Pref+ DBManager.sUploadAliasRest + "unl_odpu " +
                " (             nzp_file, nzp_counter, nzp_dom, nzp_serv, nzp_cnttype, " +
                " num_cnt, nzp_measure, dat_prov, dat_provnext, dat_uchet, val_cnt)" +
                " SELECT " + NzpFile + ", nzp_counter, nzp_dom, nzp_serv, nzp_cnttype, " +
                " num_cnt, nzp_measure, dat_prov, dat_provnext, dat_uchet, val_cnt " +
                " FROM " + BankPref + DBManager.sDataAliasRest + "counters_dom " +
                " WHERE is_actual =1 " +
                " AND dat_uchet BETWEEN " + dat_s + " AND " + dat_po;
            ExecSQL(sql);
 
            //TODO: написать update для
            //serv_type
            //counter_type
            //cnt_stage
            //mmnog
            //rashod_type

            sql =
                " update " + Points.Pref + DBManager.sUploadAliasRest + "unl_odpu " +
                " set (counter_type, serv_type, rashod_type, mmnog ) = (1,1,1,1);";
            ExecSQL(sql);

            sql =
                " update " + Points.Pref + DBManager.sUploadAliasRest + "unl_odpu " +
                " set nzp_measure = 100 " +
                " WHERE nzp_file = " + NzpFile +
                " AND  nzp_measure is null";
            ExecSQL(sql);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "unl_odpu  set (cnt_stage) = " +
                  " ((select length(trim(cast(val_cnt as varchar(10)))) + 1 " +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "unl_odpu o" +
                  " where o.nzp_file  = " + NzpFile +
                  " and o.nzp_counter= " + Points.Pref + DBManager.sUploadAliasRest + "unl_odpu.nzp_counter" +
                  " and o.dat_uchet= " + Points.Pref + DBManager.sUploadAliasRest + "unl_odpu.dat_uchet" +
                  "))";
            ExecSQL(sql);
            
            return ret;
        }

        protected Returns FillIpuInfo()
        {
            Returns ret = new Returns();

            string dat_s = "  cast('02." + SelectedDate.Month.ToString("00") + "." + SelectedDate.Year + "' as DATE) ";
            string dat_po = " cast('" + SelectedDate.ToShortDateString() + "' as DATE) ";

            string sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu  " +
                " (             nzp_file, nzp_counter, nzp_kvar, nzp_serv, nzp_cnttype, " +
                " num_cnt,  dat_prov, dat_provnext, dat_uchet, val_cnt) " +
                " SELECT " + NzpFile + ", nzp_counter, nzp_kvar, nzp_serv, nzp_cnttype, " +
                " num_cnt,  dat_prov, dat_provnext, dat_uchet, val_cnt " +
                " FROM " + BankPref + DBManager.sDataAliasRest + "counters " +
                " WHERE is_actual =1 " +
                " AND dat_uchet BETWEEN " + dat_s + " AND " + dat_po;
            ExecSQL(sql);


            sql =
                " update " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu " +
                " set (counter_type, serv_type, rashod_type, mmnog) = (1,1,1,1) ";
            ExecSQL(sql);

            sql =
                " update " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu " +
                " set nzp_measure = 100 " +
                " WHERE nzp_file = " + NzpFile +
                " AND  nzp_measure is null";
            ExecSQL(sql);

            sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu  set (cnt_stage) = " +
                  " ((select length(trim(cast(val_cnt as varchar(10)))) + 1 " +
                  " from " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu o " +
                  " where o.nzp_file  = " + NzpFile +
                  " and o.nzp_counter= " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu.nzp_counter " +
                  " and o.dat_uchet= " + Points.Pref + DBManager.sUploadAliasRest + "unl_ipu.dat_uchet " +
                  "))";
            ExecSQL(sql);

            return ret;
        }

        protected Returns FillMoInfo()
        {
            Returns ret = new Returns();

            string sql =
                " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + " unl_mo " +
                " (                                   nzp_file, nzp_vill, vill_name) " +
                " SELECT " + NzpFile + ", nzp_vill, cast(vill as char(60)) " +
                " FROM " + BankPref + DBManager.sKernelAliasRest + "s_vill ";
            ExecSQL(sql);

            sql =
                " update " + Points.Pref + DBManager.sUploadAliasRest + " unl_mo  set rajon = '-' " +
                " where nzp_file  = " + NzpFile +
                " and rajon is null ";
            ExecSQL(sql);

            sql =
                " update " + Points.Pref + DBManager.sUploadAliasRest + " unl_mo set kod_kladr = 0 " +
                " where nzp_file  = " + NzpFile +
                " and kod_kladr is null";
            ExecSQL(sql);

            return ret;
        }

        protected Returns FillGilecInfo()
        {
            Returns ret = new Returns();

            string sql =
            " INSERT INTO " + Points.Pref + DBManager.sUploadAliasRest + "unl_gilec" +
            " ( nzp_file, nzp_kvar, nzp_gil, nzp_kart, nzp_tkrt, fam, ima, " +
            " otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, gender, nzp_dok, " +
            " serij, nomer, vid_dat, vid_mes, kod_podrazd, strana_mr, region_mr, " +
            " okrug_mr, gorod_mr, npunkt_mr, rem_mr, strana_op, region_op, " +
            " okrug_op, gorod_op, npunkt_op, rem_op, strana_ku, region_ku, " +
            " okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp, dat_prop, " +
            " dat_oprp, dat_pvu, who_pvu, dat_svu, namereg,  rod, " +
            " nzp_celp, nzp_celu, dat_sost, dat_ofor" +
            " )" +
            " SELECT " + NzpFile + ", nzp_kvar, nzp_gil, nzp_kart, nzp_tkrt, fam, ima, " +
            " otch, dat_rog, fam_c, ima_c, otch_c, dat_rog_c, gender, nzp_dok, " +
            " serij, nomer, vid_dat, vid_mes, kod_podrazd, strana_mr, region_mr, " +
            " okrug_mr, gorod_mr, npunkt_mr, rem_mr, strana_op, region_op, " +
            " okrug_op, gorod_op, npunkt_op, rem_op, strana_ku, region_ku, " +
            " okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp, dat_prop, " +
            " dat_oprp, dat_pvu, who_pvu, dat_svu, namereg, rodstvo, " +
            " nzp_celp, nzp_celu, dat_sost, dat_ofor " +
            " FROM " + BankPref + DBManager.sDataAliasRest + "kart ";
            ExecSQL(sql);

            return ret;
        }
    }
}
