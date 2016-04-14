using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class IndividualRegister : BaseUnload20
    {
        /// <summary> Префикс локального бакнка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        public override string Name
        {
            get { return "IndividualRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр физических лиц"; }
        }

        public override int Code
        {
            get { return 5; }
        }

        public override List<FieldsUnload> Data { get; set; }

        public override void Start()
        {
            OpenConnection();
            CreateTempTable();

            try
            {
            string sql = " insert into IndividualRegister " +
                         "(IndividualIDSystem, TenantIDSystem, Surname, Name, Patronymic, DOBTenant, Sex, SNILS, ReSurname, ReName, RePatronymic, ReBirthDay, DocType, DocSeries, " +
                         "DocNum, DocDate, DocPlace, AgencyCode, AdditionalParametrs) " +
                         " values " +
                         " (1, 1, 'Иванов', 'Иван', 'Иванович', '01.01.2014', '1', '454444', 'Иванов', 'Петр', 'Петрович', '01.01.2014', 1, '23242', '1343255', '01.01.2014', 'Якутия', '777', '???')";

            ExecSQL(sql, false);

            sql = " select * from IndividualRegister ";

            Data = new List<FieldsUnload>();

            IDataReader reader;

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                #region заполнение полей секции

                var individualIdSystem = new Field
                {
                    N = "IndividualIDSystem",
                    NT = "Уникальный код физического лица в системе отправителя",
                    IS = 1,
                    P = 1,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["IndividualIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["IndividualIDSystem"]).Trim()
                            : ""
                };
                var tenantIdSystem = new Field
                {
                    N = "TenantIDSystem",
                    NT = "Уникальный код жильца в системе отправителя",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["TenantIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["TenantIDSystem"]).Trim()
                            : ""
                };
                var surname = new Field
                {
                    N = "Surname",
                    NT = "Фамилия",
                    IS = 1,
                    P = 3,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["Surname"] != DBNull.Value)
                            ? Convert.ToString(reader["Surname"]).Trim()
                            : ""
                };
                var name = new Field
                {
                    N = "Name",
                    NT = "Имя",
                    IS = 1,
                    P = 4,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["Name"] != DBNull.Value)
                            ? Convert.ToString(reader["Name"]).Trim()
                            : ""
                };
                var patronymic = new Field
                {
                    N = "Patronymic",
                    NT = "Отчество",
                    IS = 0,
                    P = 5,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["Patronymic"] != DBNull.Value)
                            ? Convert.ToString(reader["Patronymic"]).Trim()
                            : ""
                };
                var dobTenant = new Field
                {
                    N = "DOBTenant",
                    NT = "Дата рождения",
                    IS = 0,
                    P = 6,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["DOBTenant"] != DBNull.Value)
                            ? Convert.ToString(reader["DOBTenant"]).Trim()
                            : ""
                };
                var sex = new Field
                {
                    N = "Sex",
                    NT = "Пол",
                    IS = 1,
                    P = 7,
                    T = "TextType",
                    L = 1,
                    V =
                        (reader["Sex"] != DBNull.Value)
                            ? Convert.ToString(reader["Sex"]).Trim()
                            : ""
                };
                var snils = new Field
                {
                    N = "SNILS",
                    NT = "СНИЛС",
                    IS = 0,
                    P = 8,
                    T = "TextType",
                    L = 25,
                    V =
                        (reader["SNILS"] != DBNull.Value)
                            ? Convert.ToString(reader["SNILS"]).Trim()
                            : ""
                };
                var reSurname = new Field
                {
                    N = "ReSurname",
                    NT = "Измененная фамилия",
                    IS = 1,
                    P = 9,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["ReSurname"] != DBNull.Value)
                            ? Convert.ToString(reader["ReSurname"]).Trim()
                            : ""
                };
                var reName = new Field
                {
                    N = "ReName",
                    NT = "Измененное имя",
                    IS = 1,
                    P = 10,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["ReName"] != DBNull.Value)
                            ? Convert.ToString(reader["ReName"]).Trim()
                            : ""
                };
                var rePatronymic = new Field
                {
                    N = "RePatronymic",
                    NT = "Измененное отчество",
                    IS = 0,
                    P = 11,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["RePatronymic"] != DBNull.Value)
                            ? Convert.ToString(reader["RePatronymic"]).Trim()
                            : ""
                };
                var reBirthDay = new Field
                {
                    N = "ReBirthDay",
                    NT = "Измененная дата рождения",
                    IS = 1,
                    P = 12,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["ReBirthDay"] != DBNull.Value)
                            ? Convert.ToString(reader["ReBirthDay"]).Trim()
                            : ""
                };
                var docType = new Field
                {
                    N = "DocType",
                    NT = "Тип удостоверения личности",
                    IS = 1,
                    P = 13,
                    T = "DecimalType",
                    L = 3,
                    V =
                        (reader["DocType"] != DBNull.Value)
                            ? Convert.ToString(reader["DocType"]).Trim()
                            : ""
                };
                var docSeries = new Field
                {
                    N = "DocSeries",
                    NT = "Серия удостоверения личности",
                    IS = 1,
                    P = 14,
                    T = "TextType",
                    L = 10,
                    V =
                        (reader["DocSeries"] != DBNull.Value)
                            ? Convert.ToString(reader["DocSeries"]).Trim()
                            : ""
                };
                var docNum = new Field
                {
                    N = "DocNum",
                    NT = "Номер удостоверения личности",
                    IS = 1,
                    P = 15,
                    T = "TextType",
                    L = 7,
                    V =
                        (reader["DocNum"] != DBNull.Value)
                            ? Convert.ToString(reader["DocNum"]).Trim()
                            : ""
                };
                var docDate = new Field
                {
                    N = "DocDate",
                    NT = "Дата выдачи удостоверения личности",
                    IS = 1,
                    P = 16,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["DocDate"] != DBNull.Value)
                            ? Convert.ToString(reader["DocDate"]).Trim()
                            : ""
                };
                var docPlace = new Field
                {
                    N = "DocPlace",
                    NT = "Место выдачи удостоверения личности",
                    IS = 1,
                    P = 17,
                    T = "TextType",
                    L = 70,
                    V =
                        (reader["DocPlace"] != DBNull.Value)
                            ? Convert.ToString(reader["DocPlace"]).Trim()
                            : ""
                };
                var agencyCode = new Field
                {
                    N = "AgencyCode",
                    NT = "Код органа выдачи удостоверения личности",
                    IS = 0,
                    P = 18,
                    T = "TextType",
                    L = 7,
                    V =
                        (reader["AgencyCode"] != DBNull.Value)
                            ? Convert.ToString(reader["AgencyCode"]).Trim()
                            : ""
                };

                //var prm = new ParamUnload { C = "001", V = "10", SD = "01.10.2014 12:00:00", ED = "" };

                var fields = new FieldsUnload
                {
                    F = new List<Field>
                    {
                       individualIdSystem,
                       tenantIdSystem,
                       surname,
                       name,
                       patronymic,
                       dobTenant,
                       sex,
                       snils,
                       reSurname,
                       reName,
                       rePatronymic,
                       reBirthDay,
                       docType,
                       docSeries,
                       docNum,
                       docDate,
                       docPlace,
                       agencyCode
                    }
                };

                #endregion

                Data.Add(fields);
            }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("IndividualRegister.Start(pref): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
                }
            finally 
            {
            DropTempTable();
            CloseConnection();
        }
        }

        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();
            _pref = pref;

            OpenConnection();
            CreateTempTable();

            try
            {
            Select();

            string sql = " select * from IndividualRegister ";

            

            IDataReader reader;

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                #region заполнение полей секции

                var individualIdSystem = new Field
                {
                    N = "IndividualIDSystem",
                    NT = "Уникальный код физического лица в системе отправителя",
                    IS = 1,
                    P = 1,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["IndividualIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["IndividualIDSystem"]).Trim()
                            : ""
                };
                var tenantIdSystem = new Field
                {
                    N = "TenantIDSystem",
                    NT = "Уникальный код жильца в системе отправителя",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["TenantIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["TenantIDSystem"]).Trim()
                            : ""
                };
                var surname = new Field
                {
                    N = "Surname",
                    NT = "Фамилия",
                    IS = 1,
                    P = 3,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["Surname"] != DBNull.Value)
                            ? Convert.ToString(reader["Surname"]).Trim()
                            : ""
                };
                var name = new Field
                {
                    N = "Name",
                    NT = "Имя",
                    IS = 1,
                    P = 4,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["Name"] != DBNull.Value)
                            ? Convert.ToString(reader["Name"]).Trim()
                            : ""
                };
                var patronymic = new Field
                {
                    N = "Patronymic",
                    NT = "Отчество",
                    IS = 0,
                    P = 5,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["Patronymic"] != DBNull.Value)
                            ? Convert.ToString(reader["Patronymic"]).Trim()
                            : ""
                };
                var dobTenant = new Field
                {
                    N = "DOBTenant",
                    NT = "Дата рождения",
                    IS = 0,
                    P = 6,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["DOBTenant"] != DBNull.Value)
                            ? Convert.ToString(reader["DOBTenant"]).Trim()
                            : ""
                };
                var sex = new Field
                {
                    N = "Sex",
                    NT = "Пол",
                    IS = 1,
                    P = 7,
                    T = "TextType",
                    L = 1,
                    V =
                        (reader["Sex"] != DBNull.Value)
                            ? Convert.ToString(reader["Sex"]).Trim()
                            : ""
                };
                var snils = new Field
                {
                    N = "SNILS",
                    NT = "СНИЛС",
                    IS = 0,
                    P = 8,
                    T = "TextType",
                    L = 25,
                    V =
                        (reader["SNILS"] != DBNull.Value)
                            ? Convert.ToString(reader["SNILS"]).Trim()
                            : ""
                };
                var reSurname = new Field
                {
                    N = "ReSurname",
                    NT = "Измененная фамилия",
                    IS = 1,
                    P = 9,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["ReSurname"] != DBNull.Value)
                            ? Convert.ToString(reader["ReSurname"]).Trim()
                            : ""
                };
                var reName = new Field
                {
                    N = "ReName",
                    NT = "Измененное имя",
                    IS = 1,
                    P = 10,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["ReName"] != DBNull.Value)
                            ? Convert.ToString(reader["ReName"]).Trim()
                            : ""
                };
                var rePatronymic = new Field
                {
                    N = "RePatronymic",
                    NT = "Измененное отчество",
                    IS = 0,
                    P = 11,
                    T = "TextType",
                    L = 40,
                    V =
                        (reader["RePatronymic"] != DBNull.Value)
                            ? Convert.ToString(reader["RePatronymic"]).Trim()
                            : ""
                };
                var reBirthDay = new Field
                {
                    N = "ReBirthDay",
                    NT = "Измененная дата рождения",
                    IS = 1,
                    P = 12,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["ReBirthDay"] != DBNull.Value)
                            ? Convert.ToString(reader["ReBirthDay"]).Trim()
                            : ""
                };
                var docType = new Field
                {
                    N = "DocType",
                    NT = "Тип удостоверения личности",
                    IS = 1,
                    P = 13,
                    T = "IntType",
                    L = 20,
                    V =
                        (reader["DocType"] != DBNull.Value)
                            ? Convert.ToString(reader["DocType"]).Trim()
                            : ""
                };
                var docSeries = new Field
                {
                    N = "DocSeries",
                    NT = "Серия удостоверения личности",
                    IS = 1,
                    P = 14,
                    T = "TextType",
                    L = 10,
                    V =
                        (reader["DocSeries"] != DBNull.Value)
                            ? Convert.ToString(reader["DocSeries"]).Trim()
                            : ""
                };
                var docNum = new Field
                {
                    N = "DocNum",
                    NT = "Номер удостоверения личности",
                    IS = 1,
                    P = 15,
                    T = "TextType",
                    L = 7,
                    V =
                        (reader["DocNum"] != DBNull.Value)
                            ? Convert.ToString(reader["DocNum"]).Trim()
                            : ""
                };
                var docDate = new Field
                {
                    N = "DocDate",
                    NT = "Дата выдачи удостоверения личности",
                    IS = 1,
                    P = 16,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["DocDate"] != DBNull.Value)
                            ? Convert.ToString(reader["DocDate"]).Trim()
                            : ""
                };
                var docPlace = new Field
                {
                    N = "DocPlace",
                    NT = "Место выдачи удостоверения личности",
                    IS = 1,
                    P = 17,
                    T = "TextType",
                    L = 70,
                    V =
                        (reader["DocPlace"] != DBNull.Value)
                            ? Convert.ToString(reader["DocPlace"]).Trim()
                            : ""
                };
                var agencyCode = new Field
                {
                    N = "AgencyCode",
                    NT = "Код органа выдачи удостоверения личности",
                    IS = 0,
                    P = 18,
                    T = "TextType",
                    L = 7,
                    V =
                        (reader["AgencyCode"] != DBNull.Value)
                            ? Convert.ToString(reader["AgencyCode"]).Trim()
                            : ""
                };

                var fields = new FieldsUnload
                {
                    F = new List<Field>
                    {
                       individualIdSystem,
                       tenantIdSystem,
                       surname,
                       name,
                       patronymic,
                       dobTenant,
                       sex,
                       snils,
                       reSurname,
                       reName,
                       rePatronymic,
                       reBirthDay,
                       docType,
                       docSeries,
                       docNum,
                       docDate,
                       docPlace,
                       agencyCode
                    }
                };

                #endregion

                Data.Add(fields);
            }

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("IndividualRegister.Start(pref): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally 
            {
            DropTempTable();
                CloseConnection();
            }

        }

        public override void StartSelect()
        {
        }

        private void Select()
        {
            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " WHERE kv.nzp_area in (" + area + " )";
            }

            string sql = "INSERT INTO " + Name +
                         " ( " +
                         " IndividualIDSystem, " +
                         " TenantIDSystem, " +
                         " Surname, " +
                         " Name, " +
                         " Patronymic, " +
                         " DOBTenant, " +
                         " Sex, " +
                         " ReSurname, " +
                         " ReName, " +
                         " RePatronymic, " +
                         " ReBirthDay, " +
                         " DocType, " +
                         " DocSeries, " +
                         " DocNum, " +
                         " DocDate, " +
                         " DocPlace, " +
                         " AgencyCode " +
                         " ) " +
                         " SELECT " +
                         " k.nzp_kart, " +
                         " k.nzp_gil, " +
                         " k.fam, " +
                         " k.ima, " +
                         " k.otch, " +
                         " k.dat_rog, " +
                         " k.gender, " +
                         " k.fam_c, " +
                         " k.ima_c, " +
                         " k.otch, " +
                         " k.dat_rog_c, " +
                         " k.nzp_dok, " +
                         " k.serij, " +
                         " k.nomer, " +
                         " k.vid_dat, " +
                         " k.vid_mes, " +
                         " k.kod_podrazd " +
                         " FROM " + _pref + DBManager.sDataAliasRest + "kart k left outer join " + _pref + DBManager.sDataAliasRest + "kvar kv on k.nzp_kvar = kv.nzp_kvar " + whereArea;

            ExecSQL(sql);


            sql = " UPDATE " + Name +
                  " SET Sex = ( " +
                  " CASE WHEN Sex = 'М' THEN '1' ELSE '2' END )";

            ExecSQL(sql);
        }

        public override void CreateTempTable()
        {
            string tblName = " IndividualRegister ";
            string columnNames =
                 " id SERIAL, " +
                " IndividualIDSystem	 BIGINT , " +
                " TenantIDSystem	 BIGINT , " +
                " Surname	 CHAR ( 40), " +
                " Name	 CHAR ( 40), " +
                " Patronymic	 CHAR ( 40), " +
                " DOBTenant	 DATE , " +
                " Sex	 CHAR ( 1), " +
                " SNILS	 CHAR ( 25), " +
                " ReSurname	 CHAR ( 40), " +
                " ReName	 CHAR ( 40), " +
                " RePatronymic CHAR ( 40), " +
                " ReBirthDay	 DATE 	, " +
                " DocType	BIGINT, " +
                " DocSeries	 CHAR ( 10), " +
                " DocNum	 CHAR ( 7), " +
                " DocDate	 DATE 	, " +
                " DocPlace	 CHAR ( 70), " +
                " AgencyCode	 CHAR ( 7), " +
                " AdditionalParametrs	CHAR (255) 	 ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));
        }

        public override void DropTempTable()
        {
        }
    }
}
