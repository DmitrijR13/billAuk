using System.Collections.Generic;
using System;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{

    public class ResidentRegister : BaseUnload20
    {
        /// <summary> Наименование тега секции </summary>
        public override string Name {
            get { return "ResidentRegister"; }
        }

        /// <summary> Наименование секции </summary>
        public override string NameText {
            get { return "Реестр проживающих"; }
        }

        /// <summary> Номер секции </summary>
        public override int Code {
            get { return 13; }
        }

        /// <summary> Список полей секции </summary>
        public override List<FieldsUnload> Data { get; set; }

        /// <summary> Выборка по всем банкам данных </summary>
        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();

            OpenConnection();
            CreateTempTable();
            FillResidentRegisterInfo(pref);
            try
            {

            MyDataReader reader;
            
            string sql = " SELECT * FROM " + Name;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                #region заполнение списка полей секции

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field> 
                        {
                            AddField("PlaceIDSystem",          "Уникальный код домохозяйства в системе отправителя",
                                                                                                1, 1,   "IntType",     18,   reader["placeidsystem"]),
                            AddField("IndividualIDSystem",     "Уникальный код физического лица",                   
                                                                                                1, 2,   "IntType",     18,   reader["individualidsystem"]),
                            AddField("TenantIDSystem",         "Уникальный код жильца(гражданина) в системе отправителя",
                                                                                                1, 3,   "IntType",     18,   reader["tenantidsystem"]),
                            AddField("AddressList",            "Уникальный номер адресного листка прибытия/убытия гражданина",
                                                                                                1, 4,   "IntType",     18,   reader["addresslist"]),
                            AddField("TypeAddressList",        "Тип адресного листка",          1, 5,   "IntType",     18,   reader["typeaddresslist"]),
                            AddField("Country",                "Страна рождения",               0, 6,   "TextType",    40,   reader["country"]),
                            AddField("Region",                 "Регион рождения",               0, 7,   "TextType",    40,   reader["region"]),
                            AddField("Area",                   "Район рождения",                0, 8,   "TextType",    40,   reader["area"]),
                            AddField("City",                   "Город рождения",                0, 9,   "TextType",    40,   reader["city"]),
                            AddField("Locality",               "Нас. пункт рождения",           0, 10,  "TextType",    40,   reader["locality"]),
                            AddField("ArrivalCountry",         "Страна откуда прибыл",          0, 11,  "TextType",    40,   reader["arrivalcountry"]),
                            AddField("ArrivalRegion",          "Регион откуда прибыл",          0, 12,  "TextType",    40,   reader["arrivalregion"]),
                            AddField("ArrivalArea",            "Район откуда прибыл",           0, 13,  "TextType",    40,   reader["arrivalarea"]),
                            AddField("ArrivalCity",            "Город откуда прибыл",           0, 14,  "TextType",    40,   reader["arrivalcity"]),
                            AddField("ArrivalLocality",         "Нас. пункт откуда прибыл",      0, 15,  "TextType",    40,   reader["ArrivalLocality"]),
                            AddField("ArrivalAddress",         "Улица, дом, корпус, квартира откуда прибыл",
                                                                                                0, 16,  "TextType",    40,   reader["arrivaladdress"]),
                            AddField("DepartCountry",          "Страна куда убыл",              1, 17,  "TextType",    40,   reader["departcountry"]),
                            AddField("DepartRegion",           "Регион куда убыл",              1, 18,  "TextType",    40,   reader["departregion"]),
                            AddField("DepartArea",             "Район куда убыл",               1, 19,  "TextType",    40,   reader["departarea"]),
                            AddField("DepartCity",             "Город куда убыл",               1, 20,  "TextType",    40,   reader["departcity"]),
                            AddField("DepartLocality",         "Нас. пункт куда убыл",          1, 21,  "TextType",    40,   reader["departlocality"]),
                            AddField("DepartAddress",          "Улица, дом, корпус, квартира куда убыл",
                                                                                                1, 22,  "DateType",    40,   reader["departaddress"]),
                            AddField("CrossAddress",           "Улица ,дом, корпус, квартира для поля «переезд в том же нас. пункте»", 
                                                                                                1, 23,  "TextType",    40,   reader["crossaddress"]),
                            AddField("TypeRegistration",       "Тип регистрации",               1, 24,  "TextType",    1,    reader["typeregistration"]),
                            AddField("FirstDateRegistration",  "Дата первой регистрации по адресу",
                                                                                                0, 25,  "DateType",    20,   reader["firstdateregistration"]),
                            AddField("EndDateRegistration",    "Дата окончания регистрации по месту пребывания", 
                                                                                                1, 26,  "DateType",    20,   reader["enddateregistration"]),
                            AddField("DateMilitaryRecords",    "Дата постановки на воинский учет",
                                                                                                0, 27,  "DateType",    20,   reader["datemilitaryrecords"]),
                            AddField("StateMilitaryRecords",   "Орган регистрации воинского учета",
                                                                                                0, 28,  "TextType",    100,  reader["statemilitaryrecords"]),
                            AddField("EndDateMilitaryRecords", "Дата снятия с воинского учета", 0, 29,  "DateType",    20,   reader["enddatemilitaryrecords"]),
                            AddField("Registration",           "Орган регистрационного учета",  0, 30,  "TextType",    100,  reader["registration"]),
                            AddField("RegistrationCode",       "Код органа регистрационного учета",
                                                                                                0, 31,  "TextType",    7,    reader["registrationcode"]),
                            AddField("Filiation",              "Родственное отношение",         0, 32,  "TextType",    30,   reader["filiation"]),
                            AddField("ArrivalIntent",          "Код цели прибытия",             1, 33,  "DecimalType", 18,   reader["arrivalintent"]),
                            AddField("DepartIntent",           "Код цели убытия",               1, 34,  "DecimalType", 18,   reader["departintent"]),
                            AddField("AddListDateForm",        "Дата составления адресного листка",
                                                                                                1, 35,  "DateType",    20,   reader["addlistdateform"]),
                            AddField("RegistrationDate",       "Дата оформления регистрации",   1, 36,  "DateType",    20,   reader["registrationdate"])
                        }
                };
                #endregion
                Data.Add(fields);
            }

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("ResidentRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }finally
            {
                DropTempTable();
            CloseConnection();
            }
        }

        /// <summary> Выборка по банкам данных(территориям) </summary>
        /// <param name="pref">Префикс банка данных(территории)</param>
        public override void Start() 
        {
        }

        /// <summary> Выборка по лицевым счетам </summary>
        public override void StartSelect() {
        }

        /// <summary> Добавление поле секции </summary>
        /// <param name="name">Наименование тега поля</param>
        /// <param name="nameText">Наименование поля</param>
        /// <param name="isRequired">Обязательность заполнения</param>
        /// <param name="place">Расположение</param>
        /// <param name="type">Тип поля</param>
        /// <param name="length">Длина поля</param>
        /// <param name="value">Значение поля</param>
        private Field AddField(string name, string nameText, int isRequired, int place, string type, int length, object value) {
            return new Field
            {
                N = name,
                NT = nameText,
                IS = isRequired,
                P = place,
                T = type,
                L = length,
                V = value != DBNull.Value ? value.ToString().Trim() : string.Empty
            };
        }

        /// <summary> Создание временных таблиц </summary>
        public override void CreateTempTable()
        {
            string columnNames =
                            " id SERIAL, " +
                " PlaceIDSystem	 BIGINT , " +
                " IndividualIDSystem	 BIGINT , " +
                " TenantIDSystem	 BIGINT , " +
                " AddressList	" + DBManager.sDecimalType + " (14,0) , " +
                " TypeAddressList	" + DBManager.sDecimalType + " (14,0) , " +
                " Country	 CHAR (40), " +
                " Region	 CHAR (40), " +
                " Area	 CHAR (40), " +
                " City	 CHAR (40), " +
                " Locality	 CHAR (40), " +
                " ArrivalCountry	 CHAR (40), " +
                " ArrivalRegion	 CHAR (40), " +
                " ArrivalArea	 CHAR (40), " +
                " ArrivalCity	 CHAR (40), " +
                " ArrivalLocality	 CHAR (40), " +
                " ArrivalAddress	 CHAR (40), " +
                " DepartCountry	 CHAR (40), " +
                " DepartRegion	 CHAR (40), " +
                " DepartArea	 CHAR (40), " +
                " DepartCity	 CHAR (40), " +
                " DepartLocality	 CHAR (40), " +
                " DepartAddress	 CHAR (40), " +
                " CrossAddress	 CHAR (40), " +
                " TypeRegistration	 CHAR (1), " +
                " FirstDateRegistration	 DATE 	, " +
                " EndDateRegistration	 DATE 	, " +
                " DateMilitaryRecords	 DATE 	, " +
                " StateMilitaryRecords	 CHAR (100), " +
                " EndDateMilitaryRecords	 DATE 	, " +
                " Registration	 CHAR (100), " +
                " RegistrationCode	 CHAR (7), " +
                " Filiation	 CHAR (30), " +
                " ArrivalIntent	INTEGER , " +
                " DepartIntent	INTEGER , " +
                " AddListDateForm	 DATE 	, " +
                " RegistrationDate	 DATE 	 " +
                DBManager.sUnlogTempTable;

            ExecSQL(" DROP TABLE " + Name, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", Name, columnNames));
            
        }

        /// <summary> Удаление временных таблиц </summary>
        public override void DropTempTable() 
        {
            ExecSQL("DROP TABLE " + Name);
        }

        private void FillResidentRegisterInfo(string pref)
        {

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " WHERE nzp_area in (" + area + " )";
            }

            string sql =
                " INSERT INTO residentregister ( " +
                " 	placeidsystem, " +
                " 	tenantidsystem, " +
                " 	addresslist, " +
                " 	typeaddresslist, " +
                " 	country, " +
                " 	region, " +
                " 	area, " +
                " 	city, " +
                " 	locality, " +
                " 	arrivalcountry, " +
                " 	arrivalregion, " +
                " 	arrivalarea, " +
                " 	arrivalcity, " +
                " 	arrivallocality, " +
                " 	arrivaladdress, " +
                " 	departcountry, " +
                " 	departregion, " +
                " 	departarea, " +
                " 	departcity, " +
                " 	departlocality, " +
                " 	departaddress, " +
                " 	crossaddress, " +
                " 	typeregistration, " +
                " 	firstdateregistration, " +
                " 	enddateregistration, " +
                " 	datemilitaryrecords, " +
                " 	statemilitaryrecords, " +
                " 	enddatemilitaryrecords, " +
                " 	registration," +
                "   RegistrationCode," +
                " 	filiation, " +
                " 	arrivalintent, " +
                " 	departintent, " +
                " 	addlistdateform, " +
                " 	registrationdate " +
                " )  " +
                " SELECT " +
                " 	nzp_kvar AS PlaceIDSystem, " +
                " 	nzp_gil AS TenantIDSystem, " +
                " 	nzp_kart AS AddressList, " +
                " 	nzp_tkrt AS TypeAddressList, " +
                " 	strana_mr AS Country, " +
                " 	region_mr AS Region, " +
                " 	okrug_mr AS Area, " +
                " 	gorod_mr AS City, " +
                " 	npunkt_mr AS Locality, " +
                " 	strana_op, " +
                " 	region_op, " +
                " 	okrug_op, " +
                " 	gorod_op, " +
                " 	npunkt_op, " +
                " 	rem_op AS ArrivalAddress, " +
                " 	strana_ku, " +
                " 	region_ku, " +
                " 	okrug_ku, " +
                " 	gorod_ku, " +
                " 	npunkt_ku, " +
                " 	rem_ku AS DepartAddress, " +
                " 	rem_p AS CrossAddress, " +
                " 	tprp, " +
                " 	dat_prop AS FirstDateRegistration, " +
                " 	dat_oprp, " +
                " 	dat_pvu AS DateMilitaryRecords, " +
                " 	who_pvu, " +
                " 	dat_svu, " +
                " 	namereg AS Registration, " +
                "   kod_namereg_prn AS RegistrationCode, " +
                " 	rodstvo AS Filiation, " +
                " 	nzp_celp, " +
                " 	nzp_celu, " +
                " 	dat_sost, " +
                " 	dat_ofor " +
                " FROM " + pref + DBManager.sDataAliasRest + "kart " +
                " WHERE nzp_kvar IN " +
                "( " +
                "   SELECT  nzp_kvar FROM " + pref + DBManager.sDataAliasRest + "kvar " + whereArea +
                ") ; ";
            ExecSQL(sql);

            sql = " UPDATE residentregister " +
               " SET arrivalintent = ( " +
               " CASE WHEN arrivalintent = 0 THEN 24 END ), " +
               " departintent =  ( CASE WHEN departintent = 0 THEN 22 END )," +
               " placeidsystem = (SELECT k.num_ls FROM " + pref + DBManager.sDataAliasRest + "kvar k " +
               "  WHERE  residentregister.placeidsystem = k.nzp_kvar)";


            ExecSQL(sql);
        }

    }
}
