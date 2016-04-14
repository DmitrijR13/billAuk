using System.Collections.Generic;
using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class IndicatorRegister : BaseUnload20
    {
        public override string Name
        {
            get { return "IndicatorRegister"; }
        }

        public override string NameText
        {
            get { return "Информация по приборам учета "; }
        }

        public override int Code
        {
            get { return 11; }
        }

        public override List<FieldsUnload> Data { get; set; }

        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();
            OpenConnection();
            CreateTempTable();
            FillIndicatorRegisterInfo(pref);
            try
            {

            string sql =
                " SELECT IndicatorType ,HouseIDSystem ,GroupeIDSystem ,AccountIDSys ,IndicatorIDSystem,UnitIDSys, " +
                " UnitType , ComIndicatorType, Capacity, Index ,IndicatorNum, UnitCode , CheckDate , NextCheckDate , IndicatorStartDate,IndicatorEndDate " +
                " FROM IndicatorRegister ";

            

            IDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                    #region заполнение списка полей секции
                var indicatorType = new Field
                {
                    N = "IndicatorType",
                    NT = "Номер выгрузки",
                    IS = 1,
                    P = 1,
                    T = "IntType",
                    L = 1,
                    V = (reader["IndicatorType"] != DBNull.Value) ? Convert.ToString(reader["IndicatorType"]).Trim() : ""
                };
                var houseIDSystem = new Field
                {
                    N = "HouseIDSystem",
                    NT = "Уникальный код дома в системе отправителя",
                    IS = 0,
                    P = 2,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["HouseIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["HouseIDSystem"]).Trim()
                            : ""
                };
                var groupeIDSystem = new Field
                {
                    N = "GroupeIDSystem",
                    NT = "Уникальный код Группы в системе отправителя",
                    IS = 0,
                    P = 3,
                    T = "IntType",
                    L = 9,
                    V = (reader["GroupeIDSystem"] != DBNull.Value) ? Convert.ToString(reader["GroupeIDSystem"]).Trim() : ""
                };
                var accountIDSys = new Field
                {
                    N = "AccountIDSys",
                    NT = "№ ЛС в системе отправителя",
                    IS = 0,
                    P = 4,
                    T = "IntType",
                    L = 18,
                    V = (reader["AccountIDSys"] != DBNull.Value) ? Convert.ToString(reader["AccountIDSys"]).Trim() : ""
                };
                var indicatorIDSystem = new Field
                {
                    N = "IndicatorIDSystem",
                    NT = "Уникальный код прибора учета в системе отправителя",
                    IS = 1,
                    P = 5,
                    T = "TextType",
                    L = 20,
                    V = (reader["IndicatorIDSystem"] != DBNull.Value) ? Convert.ToString(reader["IndicatorIDSystem"]).Trim() : ""
                };
                var unitIDSys = new Field
                {
                    N = "UnitIDSys",
                    NT = "Код услуги",
                    IS = 1,
                    P = 6,
                    T = "IntType",
                    L = 3,
                    V = (reader["UnitIDSys"] != DBNull.Value) ? Convert.ToString(reader["UnitIDSys"]).Trim() : ""
                };
                var unitType = new Field
                {
                    N = "UnitType",
                    NT = "Тип услуги",
                    IS = 1,
                    P = 7,
                    T = "IntType",
                    L = 1,
                    V =
                        (reader["UnitType"] != DBNull.Value)
                            ? Convert.ToString(reader["UnitType"]).Trim()
                            : ""
                };
                var indicatorType2 = new Field
                {
                    N = "ComIndicatorType",
                    NT = "Тип счетчика",
                    IS = 1,
                    P = 8,
                    T = "TextType",
                    L = 25,
                    V = (reader["ComIndicatorType"] != DBNull.Value) ? Convert.ToString(reader["ComIndicatorType"]).Trim() : ""
                };
                var capacity = new Field
                {
                    N = "Capacity",
                    NT = "Разрядность прибора",
                    IS = 1,
                    P = 9,
                    T = "IntType",
                    L = 9,
                    V = (reader["Capacity"] != DBNull.Value) ? Convert.ToString(reader["Capacity"]).Trim() : ""
                };
                var index = new Field
                {
                    N = "Index",
                    NT = "Повышающий коэффициент (коэффициент трансформации тока)",
                    IS = 1,
                    P = 10,
                    T = "IntType",
                    L = 9,
                    V =
                        (reader["Index"] != DBNull.Value)
                            ? Convert.ToString(reader["Index"]).Trim()
                            : ""
                };
                var indicatorNum = new Field
                {
                    N = "IndicatorNum",
                    NT = "Заводской номер прибора учета",
                    IS = 1,
                    P = 11,
                    T = "TextType",
                    L = 20,
                    V = (reader["IndicatorNum"] != DBNull.Value) ? Convert.ToString(reader["IndicatorNum"]).Trim() : ""
                };
                var unitCode = new Field
                {
                    N = "UnitCode",
                    NT = "Код единицы измерения расхода ",
                    IS = 1,
                    P = 12,
                    T = "IntType",
                    L = 3,
                    V = (reader["UnitCode"] != DBNull.Value) ? Convert.ToString(ConvertCode(Convert.ToInt32(reader["UnitCode"]))).Trim() : ""
                };
                var checkDate = new Field
                {
                    N = "CheckDate",
                    NT = "Дата поверки",
                    IS = 0,
                    P = 13,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["CheckDate"] != DBNull.Value)
                            ? Convert.ToDateTime(reader["CheckDate"]).ToShortDateString()
                            : ""
                };
                var nextCheckDate = new Field
                {
                    N = "NextCheckDate",
                    NT = "Дата следующей поверки",
                    IS = 0,
                    P = 14,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["NextCheckDate"] != DBNull.Value)
                            ? Convert.ToDateTime(reader["NextCheckDate"]).ToShortDateString()
                            : ""
                };
                var indicatorStartDate = new Field
                {
                    N = "IndicatorStartDate",
                    NT = "Дата установки прибора",
                    IS = 0,
                    P = 15,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["IndicatorStartDate"] != DBNull.Value)
                            ? Convert.ToDateTime(reader["IndicatorStartDate"]).ToShortDateString()
                            : ""
                };
                var indicatorEndDate = new Field
                {
                    N = "IndicatorEndDate",
                    NT = "Дата окончания действия прибора",
                    IS = 0,
                    P = 16,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["IndicatorEndDate"] != DBNull.Value)
                            ? Convert.ToDateTime(reader["IndicatorEndDate"]).ToShortDateString()
                            : ""
                };


                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {
                            indicatorType,
                            houseIDSystem,
                            groupeIDSystem ,
                            accountIDSys,
                            indicatorIDSystem,
                            unitIDSys,
                            unitType,
                            indicatorType2,
                            capacity,
                            index,
                            indicatorNum,
                            unitCode,
                            checkDate,
                            nextCheckDate,
                            indicatorStartDate,
                            indicatorEndDate
                        }
                };
                Data.Add(fields);
                #endregion
            }

            }
                catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("IndicatorRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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

        public override void CreateTempTable()
        {
            string tblName = " IndicatorRegister ";
            string columnNames =
                " id SERIAL, " +
                " IndicatorType	 INTEGER , " +
                " HouseIDSystem	 BIGINT , " +
                " GroupeIDSystem	 INTEGER , " +
                " AccountIDSys	 BIGINT , " +
                " IndicatorIDSystem	 CHAR (20), " +
                " UnitIDSys	 INTEGER , " +
                " UnitType	 INTEGER , " +
                " ComIndicatorType	 CHAR (25), " +
                " Capacity	 INTEGER , " +
                " Index	 INTEGER , " +
                " IndicatorNum	 CHAR (20), " +
                " nzp_measure	 INTEGER , " +
                " UnitCode	 INTEGER , " +
                " CheckDate	 DATE 	, " +
                " NextCheckDate	 DATE 	, " +
                " IndicatorStartDate	DATE	, " +
                " IndicatorEndDate		DATE ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));
        }

        /// <summary>
        /// Заполнение информации по всем типам приборов учета
        /// </summary>
        /// <param name="pref">Префикс банка, из которого выгружаем</param>
        private void FillIndicatorRegisterInfo(string pref)
        {
            //заполнить реестр ОДПУ
            FillIndicatorInfo(pref, 1, 1);
            //заполнить реестр ИПУ 
            FillIndicatorInfo(pref, 4, 3);

            //заполнить реестр групповых ПУ 
            //пока не заполняем, так как придется формировать секцию 28.	Реестр групп
            //FillIndicatorInfo(pref, 2, 2);
            //заполнить реестр общеквар ПУ 
            //пока не заполняем, так как придется формировать секцию 28.	Реестр групп
            //FillIndicatorInfo(pref, 3, 4);
        }

        /// <summary>
        /// Заполнение информации по приборам учета определенного типа
        /// </summary>
        /// <param name="pref">Префикс банка, из которого выгружаем</param>
        /// <param name="IndicatorType">Тип прибора учета в системе ЦХД</param>
        /// <param name="nzp_type">Тип прибора учета в Биллинге(1-ОДПУ, 2-ГрПУ, 3 - ИПУ, 4-ОбщеквартирПУ)</param>
        private void FillIndicatorInfo(string pref, int IndicatorType, int nzp_type)
        {
            string unlDate = " CAST( '" + Year + "-" + Month + "-01' as DATE) ";

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " and k.nzp_area in (" + area + " )";
            }
            string type = String.Empty;
            switch (IndicatorType)
            {
                case 4:
                    type = " 	accountidsys, ";
                    break;
                case 1:
                    type = " 	houseidsystem, ";
                    break;
            }
            string sql = " INSERT INTO indicatorregister ( " +
                         " 	indicatortype, " +
                         (type != String.Empty ? type : " 	accountidsys, ") +
                         " 	indicatoridsystem, " +
                         " 	unitidsys, " +
                         " 	unittype, " +
                         " 	comindicatortype, " +
                         " 	capacity, " +
                         " 	INDEX, " +
                         " 	IndicatorNum,	 " +
                         " 	nzp_measure, " +
                         " 	checkdate, " +
                         " 	nextcheckdate, " +
                         " 	IndicatorStartDate, " +
                         " 	IndicatorEndDate " +
                         " ) " +

                         " SELECT " +
                         IndicatorType + " AS indicatortype, " +
                         " 	cnt.nzp AS " + (type != String.Empty ? type : " 	accountidsys, ") +
                         " 	cnt.nzp_counter AS indicatoridsystem,	 " +
                         " 	cnt.nzp_serv AS unitidsys, " +
                         " 	1 AS UnitType, " +
                         " 	ct.name_type AS ComIndicatorType, " +
                         " 	ct.cnt_stage AS Capacity,  " +
                         " 	ct.mmnog AS Index, " +
                         DBManager.sNvlWord + "(cnt.num_cnt, cast(cnt.nzp_counter as char(20))) AS IndicatorNum, " +

                         " 	s.nzp_measure, " +
                         " 	cnt.dat_prov AS CheckDate, " +
                         " 	cnt.dat_provnext AS NextCheckDate, " +
                         " 	cnt.dat_s AS IndicatorStartDate, " +
                         " 	cnt.dat_po AS IndicatorEndDate	 " +
                         " FROM " +
                         " " + pref + DBManager.sDataAliasRest + "counters_spis cnt " +
                         " LEFT OUTER JOIN " + pref + DBManager.sDataAliasRest +
                         "kvar k ON cnt.nzp = k.nzp_kvar " +
                         " LEFT OUTER JOIN " + pref + DBManager.sKernelAliasRest +
                         "s_counttypes ct ON ct.nzp_cnttype = cnt.nzp_cnttype " +
                         " LEFT OUTER JOIN " + pref + DBManager.sKernelAliasRest +
                         "services s ON cnt.nzp_serv = s.nzp_serv " +
                         " WHERE cnt.is_actual <> 100 " +
                         " AND cnt.nzp_type = " + nzp_type +
                         " AND " + DBManager.sNvlWord + "(cnt.dat_s ,CAST ('01.01.1900' AS DATE)) " +
                         " <= " + unlDate + " " +
                         " AND " + DBManager.sNvlWord + "(cnt.dat_po ,CAST ('01.01.3000' AS DATE)) " +
                         " >= " + unlDate + whereArea;

            ExecSQL(sql);
        }

        public override void DropTempTable()
        {
        }
    }
}
