using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    using System;
    using System.Collections.Generic;
    using STCLINE.KP50.Interfaces;

    public class RegisterLegalFace : BaseUnload20
    {
        public override string Name
        {
            get { return "RegisterLegalFace"; }
        }

        public override string NameText
        {
            get { return "Реестр юридических лиц"; }
        }

        public override int Code
        {
            get { return 2; }
        }

        /// <summary> Префикс локального бакнка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        public override List<FieldsUnload> Data { get; set; }

        public override void Start(string pref)
        {

            Data = new List<FieldsUnload>();
            OpenConnection();
            CreateTempTable();
            FillRegisterLegalFaceInfo(pref);

            _pref = pref;

            try
            {
            #region выборка данных из временной таблицы
            string sql = " SELECT * FROM  RegisterLegalFace ";
            
            
            
            IDataReader reader;
            ExecRead(out reader, sql);
           
            while (reader.Read())
            {
                var idLegalSys = new Field
                {
                    N = "IDLegalSys",
                    NT = "Уникальный код ЮЛ в системе поставщика",
                    IS = 1,
                    P = 1,
                    T = "IntType",
                    L = 30,
                    V = (reader["IDLegalSys"]!=DBNull.Value)?Convert.ToString(reader["IDLegalSys"]).Trim() : ""
                };
                var nameLF = new Field
                {
                    N = "NameLF",
                    NT = "Наименование ЮЛ",
                    IS = 1,
                    P = 2,
                    T = "TextType",
                    L = 150,
                    V = (reader["NameLF"] != DBNull.Value) ? Convert.ToString(reader["NameLF"]).Trim() : ""
                };
                var abbrLF = new Field
                {
                    N = "AbbrLF",
                    NT = "Сокращенное наименование ЮЛ",
                    IS = 1,
                    P = 3,
                    T = "TextType",
                    L = 50,
                    V = (reader["AbbrLF"] != DBNull.Value) ? Convert.ToString(reader["AbbrLF"]).Trim() : ""
                };
                var addressLF = new Field
                {
                    N = "AddressLF",
                    NT = "Юридический адрес",
                    IS = 1,
                    P = 4,
                    T = "TextType",
                    L = 50,
                    V = (reader["AddressLF"] != DBNull.Value) ? Convert.ToString(reader["AddressLF"]).Trim(): ""
                };
                var addressFact = new Field
                {
                    N = "AddressFact",
                    NT = "КПП организации-отправителя",
                    IS = 1,
                    P = 5,
                    T = "TextType",
                    L = 15,
                    V =(reader["AddressFact"] != DBNull.Value) ? Convert.ToString(reader["AddressFact"]).Trim(): ""
                };
                var inn = new Field
                {
                    N = "INNLF",
                    NT = "ИНН",
                    IS = 1,
                    P = 6,
                    T = "IntType",
                    L = 12,
                    V = (reader["INNLF"] != DBNull.Value) ? Convert.ToString(reader["INNLF"]).Trim(): ""
                };
                var kpp = new Field
                {
                    N = "KPPLF",
                    NT = "КПП",
                    IS = 1,
                    P = 7,
                    T = "IntType",
                    L = 10,
                    V = (reader["KPPLF"] != DBNull.Value) ? Convert.ToString(reader["KPPLF"]).Trim(): ""
                };
                var phoneLeader = new Field
                {
                    N = "PhoneLeader",
                    NT = "Телефон руководителя",
                    IS = 0,
                    P = 8,
                    T = "TextType",
                    L = 20,
                    V = (reader["PhoneLeader"] != DBNull.Value) ? Convert.ToString(reader["PhoneLeader"]).Trim() : ""
                };
                var nameLeader = new Field
                {
                    N = "NameLeader",
                    NT = "ФИО руководителя",
                    IS = 0,
                    P = 9,
                    T = "TexteType",
                    L = 50,
                    V = (reader["NameLeader"]!= DBNull.Value) ?Convert.ToString(reader["NameLeader"]).Trim(): ""
                };
                var postLeader = new Field
                {
                    N = "PostLeader",
                    NT = "Должность руководителя",
                    IS = 0,
                    P = 10,
                    T = "TextType",
                    L = 40,
                    V = (reader["PostLeader"] != DBNull.Value) ?Convert.ToString(reader["PostLeader"]).Trim(): ""
                };
                var phoneAccountant = new Field
                {
                    N = "PhoneAccountant",
                    NT = "Телефон бухгалтера",
                    IS = 0,
                    P = 11,
                    T = "TextType",
                    L = 20,
                    V = (reader["PhoneAccountant"]!= DBNull.Value) ?Convert.ToString(reader["PhoneAccountant"]).Trim(): ""
                };
                var nameAccountant = new Field
                {
                    N = "NameAccountant",
                    NT = "ФИО бухгалтера",
                    IS = 0,
                    P = 12,
                    T = "TexType",
                    L = 50,
                    V = (reader["NameAccountant"] != DBNull.Value) ?Convert.ToString(reader["NameAccountant"]).Trim(): ""
                };
                var okonh1 = new Field
                {
                    N = "OKONH1",
                    NT = "ОКОНХ1",
                    IS = 0,
                    P = 13,
                    T = "TexType",
                    L = 20,
                    V = (reader["OKONH1"]!= DBNull.Value) ?Convert.ToString(reader["OKONH1"]).Trim(): ""
                };
                var okonh2 = new Field
                {
                    N = "OKONH2",
                    NT = "ОКОНХ2",
                    IS = 0,
                    P = 14,
                    T = "TexType",
                    L = 20,
                    V = (reader["OKONH2"]!= DBNull.Value) ?Convert.ToString(reader["OKONH2"]).Trim() : ""
                };
                var okpo = new Field
                {
                    N = "OKPO",
                    NT = "ОКОНХ2",
                    IS = 0,
                    P = 15,
                    T = "TexType",
                    L = 20,
                    V = (reader["OKPO"]!= DBNull.Value) ?Convert.ToString(reader["OKPO"]).Trim(): ""
                };
                var postName = new Field
                {
                    N = "PostName",
                    NT = "Должность+ФИО в родит.падеже",
                    IS = 0,
                    P = 16,
                    T = "TexType",
                    L = 200,
                    V = (reader["PostName"]!= DBNull.Value) ?Convert.ToString(reader["PostName"]).Trim(): ""
                };
                var signMC = new Field
                {
                    N = "SignMC",
                    NT = "Признак роли УК",
                    IS = 1,
                    P = 17,
                    T = "IntType",
                    L = 3,
                    V = (reader["SignMC"]!= DBNull.Value) ?Convert.ToString(reader["SignMC"]).Trim() : ""
                };
                var signSP = new Field
                {
                    N = "SignSP",
                    NT = "Признак роли Пост.услуг",
                    IS = 1,
                    P = 18,
                    T = "IntType",
                    L = 3,
                    V = (reader["SignSP"] != DBNull.Value) ? Convert.ToString(reader["SignSP"]).Trim() : ""
                };
                var signTenant = new Field
                {
                    N = "SignTenant",
                    NT = "Признак роли Арендатор",
                    IS = 1,
                    P = 19,
                    T = "IntType",
                    L = 3,
                    V = (reader["SignTenant"] != DBNull.Value) ? Convert.ToString(reader["SignTenant"]).Trim() : ""
                };
                var signSC = new Field
                {
                    N = "SignSC",
                    NT = "Признак роли РЦ",
                    IS = 1,
                    P = 20,
                    T = "IntType",
                    L = 3,
                    V = (reader["SignSC"] != DBNull.Value) ? Convert.ToString(reader["SignSC"]).Trim() : ""
                };
                var signRSO = new Field
                {
                    N = "SignRSO",
                    NT = "Признак роли РСО",
                    IS = 1,
                    P = 21,
                    T = "IntType",
                    L = 3,
                    V = (reader["SignRSO"] != DBNull.Value) ? Convert.ToString(reader["SignRSO"]).Trim() : ""
                };
                var signPA = new Field
                {
                    N = "SignPA",
                    NT = "Признак роли ПА",
                    IS = 1,
                    P = 22,
                    T = "IntType",
                    L = 3,
                    V = (reader["SignPA"] != DBNull.Value) ? Convert.ToString(reader["SignPA"]).Trim() : ""
                };  
                var signSubTenant = new Field
                {
                    N = "SignSubTenant",
                    NT = "Признак роли Субабонент",
                    IS = 1,
                    P = 23,
                    T = "IntType",
                    L = 3,
                    V = (reader["SignSubTenant"] != DBNull.Value) ? Convert.ToString(reader["SignSubTenant"]).Trim() : ""
                };
                var signBank = new Field
                {
                    N = "SignBank",
                    NT = "Признак роли Банк",
                    IS = 1,
                    P = 24,
                    T = "IntType",
                    L = 3,
                    V = (reader["SignBank"] != DBNull.Value) ? Convert.ToString(reader["SignBank"]).Trim() : ""
                };
                var signSubs = new Field
                {
                    N = "SignSubs",
                    NT = "Признак отделения Соц.защиты",
                    IS = 1,
                    P = 25,
                    T = "IntType",
                    L = 1,
                    V = (reader["SignSubs"] != DBNull.Value) ? Convert.ToString(reader["SignSubs"]).Trim() : ""
                };
                var subsidyDepartmentID = new Field
                {
                    N = "SubsidyDepartmentID",
                    NT = "Признак отделения Соц.защиты",
                    IS = 0,
                    P = 26,
                    T = "IntType",
                    L = 9,
                    V = (reader["SubsidyDepartmentID"] != DBNull.Value) ? Convert.ToString(reader["SubsidyDepartmentID"]).Trim() : ""
                };                              
                var startDate = new Field
                {
                    N = "StartDate",
                    NT = "Дата действия с",
                    IS = 1,
                    P = 27,
                    T = "DateType",
                    L = 0,
                    V = (reader["StartDate"] != DBNull.Value) ? Convert.ToDateTime(reader["StartDate"]).ToShortDateString() : ""
                };
                var endDate = new Field
                {
                    N = "EndDate",
                    NT = "Дата действия по",
                    IS = 0,
                    P = 28,
                    T = "DateType",
                    L = 0,
                    V = (reader["EndDate"] != DBNull.Value) ? Convert.ToDateTime(reader["EndDate"]).ToShortDateString() : ""
                };

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {
                            idLegalSys,
                            nameLF,
                            abbrLF,
                            addressLF,
                            addressFact,
                            inn,
                            kpp,                            
                            phoneLeader,
                            nameLeader,
                            postLeader,
                            phoneAccountant,
                            nameAccountant, 
                            okonh1,
                            okonh2,
                            okpo,
                            postName,
                            signMC,
                            signSP,
                            signTenant,                                                  
                            signSC,
                            signRSO,
                            signPA,
                            signSubTenant,
                            signBank,
                            signSubs,
                            subsidyDepartmentID,
                            startDate,
                            endDate
                        }
                };
                Data.Add(fields);
            }
            #endregion выборка данных из временной таблицы
            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("RegisterLegalFace.Start(pref): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally 
            {
            DropTempTable();
            CloseConnection();
        }
        }

        public override void Start()
        {
        }
        
        public override void StartSelect()
        {
        }

        private void FillRegisterLegalFaceInfo(string _pref)
        {
            string legalFaceTblName = Name;

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " WHERE s.nzp_area in (" + area + " )";
            }

            string sql =
                " INSERT INTO " + legalFaceTblName + "                  " +
                " (                                                     " +
                " 	idlegalsys,                                         " +
                " 	namelf,                                             " +
                " 	abbrlf                                             " +
                " )                                                     " +
                " SELECT                                                " +
                " 	p.nzp_payer,                                          " +
                " 	TRIM(SUBSTRING(p.npayer from 1 for 150)) AS namelf,   " +
                " 	TRIM(SUBSTRING(p.payer from 1 for 50)) AS abbrlf    " +
                " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_payer p " +
                " left outer join " + _pref + DBManager.sDataAliasRest + "s_area s on p.nzp_payer = s.nzp_payer "  + whereArea;
            ExecSQL(sql);

            UpdatePrms uprms = new UpdatePrms();
            uprms.PrmTblFullName = _pref + DBManager.sDataAliasRest + "prm_9";
            uprms.TablePrimaryColumnName = "idlegalsys";
            uprms.RemoveChars = false;

            uprms.ColumnName = "namelf";
            uprms.ColumnNameText = " юр.лица без информации о НАИМЕНОВАНИИ ЮР.ЛИЦА ";
            CheckColumnOnEmptiness(uprms.ColumnName, uprms.ColumnNameText);

            uprms.ColumnName = "abbrlf";
            uprms.ColumnNameText = " юр.лица без информации о СОКРАЩЕННОМ НАИМЕНОВАНИИ ";
            CheckColumnOnEmptiness(uprms.ColumnName, uprms.ColumnNameText);

            uprms.ColumnName = "addresslf";
            uprms.ColumnNameText = " юр.лица без информации о ЮРИДИЧЕСКОМ АДРЕСЕ ";
            uprms.ColumnLength = 250;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 505;
            Update(uprms);
            CheckColumnOnEmptiness(uprms.ColumnName, uprms.ColumnNameText);


            uprms.ColumnName = "addressfact";
            uprms.ColumnNameText = " юр.лица без информации о ФАКТИЧЕСКОМ АДРЕСЕ ";
            uprms.ColumnLength = 250;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 1269;
            Update(uprms);
            CheckColumnOnEmptiness(uprms.ColumnName, uprms.ColumnNameText);

            uprms.ColumnName = "INNLF";
            uprms.ColumnNameText = " юр.лица без информации о ИНН ";
            uprms.ColumnLength = 12;
            uprms.ColumnDataType = DBManager.sDecimalType + " (" + uprms.ColumnLength + ",0)";
            uprms.NzpPrm = 502;
            uprms.RemoveChars = true;
            Update(uprms);
            CheckColumnOnEmptiness(uprms.ColumnName, uprms.ColumnNameText);

            uprms.ColumnName = "KPPLF";
            uprms.ColumnNameText = " юр.лица без информации о КПП ";
            uprms.ColumnLength = 10;
            uprms.ColumnDataType = DBManager.sDecimalType + " (" + uprms.ColumnLength + ",0)";
            uprms.NzpPrm = 503;
            Update(uprms);
            CheckColumnOnEmptiness(uprms.ColumnName, uprms.ColumnNameText);

            uprms.ColumnName = "PhoneLeader";
            uprms.ColumnLength = 20;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 1306;
            uprms.RemoveChars = false;
            Update(uprms);

            uprms.ColumnName = "NameLeader";
            uprms.ColumnLength = 50;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 1308;
            Update(uprms);

            uprms.ColumnName = "PostLeader";
            uprms.ColumnLength = 40;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 1306;
            Update(uprms);

            uprms.ColumnName = "PhoneAccountant";
            uprms.ColumnLength = 20;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 1307;
            Update(uprms);

            uprms.ColumnName = "NameAccountant";
            uprms.ColumnLength = 50;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 1310;
            Update(uprms);

            uprms.ColumnName = "OKONH1";
            uprms.ColumnLength = 20;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 506;
            Update(uprms);

            uprms.ColumnName = "OKONH2";
            uprms.ColumnLength = 20;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 1311;
            Update(uprms);

            uprms.ColumnName = "OKPO";
            uprms.ColumnLength = 20;
            uprms.ColumnDataType = " CHAR(" + uprms.ColumnLength + ")";
            uprms.NzpPrm = 507;
            Update(uprms);

            //Заполнение поля: 'Признак того что предприятие является управляющей компанией (территория)'
            uprms.ColumnName = "SignMC";
            SetUrlicSign(uprms, 3);

            //Заполнение поля: 'Признак того что предприятие является поставщиком услуг'
            uprms.ColumnName = "SignSP";
            SetUrlicSign(uprms, 2);

            //Заполнение поля: 'Признак того что предприятие является арендатором или собственником помещений'
            uprms.ColumnName = "SignTenant";
            SetUrlicSign(uprms, 7);

            //Заполнение поля: 'Признак того что предприятие является РЦ'
            uprms.ColumnName = "SignSC";
            SetUrlicSign(uprms, 5);

            //Заполнение поля: 'Признак того что предприятие является РСО'
            uprms.ColumnName = "SignRSO";
            SetUrlicSign(uprms, 6);

            //Заполнение поля: 'Признак того что предприятие является платежным агентом'
            uprms.ColumnName = "SignPA";
            SetUrlicSign(uprms, 4);

            //Заполнение поля: 'Признак того что предприятие является субабонентом'
            uprms.ColumnName = "SignSubTenant";
            SetUrlicSign(uprms, 9);

            //Заполнение поля: 'Признак того что предприятие является Банком'
            uprms.ColumnName = "SignBank";
            SetUrlicSign(uprms, 8);

        }

        #region вспомогательные методы для заполнения данными

        /// <summary>
        /// Вспомогательная структура для заполнения полей таблицы 
        /// </summary>
        private struct UpdatePrms
        {
            public string TablePrimaryColumnName;
            public string ColumnName;
            public string ColumnNameText;
            public string PrmTblFullName;
            public string ColumnDataType;
            public int NzpPrm;
            public int ColumnLength;
            public bool RemoveChars;

        }

        /// <summary>
        /// Заполнение признаков юр.лиц
        /// </summary>
        /// <param name="uprms">Вспомогательная структура с параметрами таблицы</param>
        /// <param name="signNum">Номер признака</param>
        private void SetUrlicSign(UpdatePrms uprms, int signNum)
        {
            string sql =
                " UPDATE " + Name + " SET  " + uprms.ColumnName + " = 1 " +
                " WHERE EXISTS ( SELECT 1 FROM " + Points.Pref + DBManager.sKernelAliasRest + "payer_types pt " +
                "   WHERE pt.nzp_payer =  " + Name + "." + uprms.TablePrimaryColumnName + 
                "   AND pt.nzp_payer_type =  " + signNum +
                " ) ";
            ExecSQL(sql);
        }
        /// <summary>
        /// Заполнение полей таблицы (параметров юр.лиц)
        /// </summary>
        /// <param name="uPrms"></param>
        private void Update(UpdatePrms uPrms)
        {
            string val_prm = " val_prm ";
            if (uPrms.RemoveChars)
            {
                //если включен параметр удаления символов, то вызываем хранимую процедуру
                 val_prm = " sortnum(val_prm) ";
            }
            string unlDate = " CAST( '" + Year + "-" + Month + "-01' as DATE) ";
            string sql =
                " UPDATE   " + Name +
                " SET " + uPrms.ColumnName + " = " +
                " ( " +
                " SELECT " +
                " CAST(" + val_prm + " AS " + uPrms.ColumnDataType + " ) " +
                " FROM   " + uPrms.PrmTblFullName + " p " +
                " WHERE p.nzp_prm =   " + uPrms.NzpPrm +
                " AND p.nzp = CAST(" + Name + "." + uPrms.TablePrimaryColumnName + " AS INTEGER)" +
                " AND p.is_actual <> 100 " +
                " AND dat_s  < " + unlDate +
                " AND dat_po > " + unlDate +
                " AND LENGTH(TRIM(val_prm)) <= " + uPrms.ColumnLength +
                " ) " +
                " WHERE  EXISTS                                            " +
                " (                                                        " +
                " SELECT 1                                                 " +
                " FROM   " + uPrms.PrmTblFullName + " p " +
                " WHERE p.nzp_prm =   " + uPrms.NzpPrm +
                " AND p.nzp = CAST(" + Name + "." + uPrms.TablePrimaryColumnName + " AS INTEGER)" +
                " AND p.is_actual <> 100 " +
                " AND dat_s  < " + unlDate +
                " AND dat_po > " + unlDate +
                " AND LENGTH(TRIM(val_prm)) <= " + uPrms.ColumnLength +
                " )";
            ExecSQL(sql);

            sql =
               " SELECT " + DBManager.sNvlWord + " (nzp," + Constants._ZERO_ + ") AS key_value" +
               " FROM   " + uPrms.PrmTblFullName + " p, " + Name + " t " +
               " WHERE p.nzp_prm =   " + uPrms.NzpPrm +
               " AND p.nzp = CAST(t." + uPrms.TablePrimaryColumnName + " AS INTEGER)" +
               " AND p.is_actual <> 100 " +
               " AND dat_s  < " + unlDate +
               " AND dat_po > " + unlDate +
               " AND LENGTH(TRIM(val_prm)) > " + uPrms.ColumnLength;

            WriteAboutIncorrectNotes(sql, uPrms.ColumnName, uPrms.ColumnLength);
        }
        
        #endregion вспомогательные методы для заполнения данными

        public override void CreateTempTable()
        {
            string columnNames =
                " id SERIAL, " +
                " IDLegalSys INTEGER,         " +
                " NameLF CHAR(150),           " +
                " AbbrLF CHAR(50),            " +
                " AddressLF CHAR(250),        " +
                " AddressFact CHAR(250),      " +
                " INNLF " + DBManager.sDecimalType + "(12,0), " +
                " KPPLF " + DBManager.sDecimalType + "(10,0), " +
                " PhoneLeader CHAR(20),       " +
                " NameLeader CHAR(50),        " +
                " PostLeader CHAR(40),        " +
                " PhoneAccountant CHAR(20),   " +
                " NameAccountant CHAR(50),    " +
                " OKONH1 CHAR(20),            " +
                " OKONH2 CHAR(20),            " +
                " OKPO CHAR(20),              " +
                " PostName CHAR(200),         " +
                " SignMC INTEGER,             " +
                " SignSP INTEGER,             " +
                " SignTenant INTEGER,         " +
                " SignSC INTEGER,             " +
                " SignRSO INTEGER,            " +
                " SignPA INTEGER,             " +
                " SignSubTenant INTEGER,      " +
                " SignSubs INTEGER,           " +
                " SubsidyDepartmentID INTEGER," +
                " SignBank INTEGER,           " +
                " StartDate DATE,             " +
                " EndDate DATE               ";

            ExecSQL(" DROP TABLE " + Name, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", Name, columnNames));
        }

    }
}
