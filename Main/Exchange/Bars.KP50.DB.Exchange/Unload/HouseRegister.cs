using System.Collections.Generic;
using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class HouseRegister : BaseUnload20
    {

        public override string Name
        {
            get { return "HouseRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр жилых домов"; }
        }

        public override int Code
        {
            get { return 3; }
        }

        string tblNamePrm = " HouseRegisterPrm ";

        /// <summary> Префикс локального банка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        public override List<FieldsUnload> Data { get; set; }

        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();
            OpenConnection();
            CreateTempTable();
            FillHouseRegisterInfo(pref);
            _pref = pref;
#warning исправить заполнение параметров
            try
            {

                #region выборка данных из временной таблицы

                string sql = " SELECT * FROM HouseRegister ";



                IDataReader reader;

                ExecRead(out reader, sql);

                while (reader.Read())
                {

                    var houseIdSystem = new Field
                    {
                        N = "HouseIDSystem",
                        NT = "Уникальный код дома в системе отправителя",
                        IS = 1,
                        P = 1,
                        T = "IntType",
                        L = 20,
                        V =
                            (reader["HouseIDSystem"] != DBNull.Value)
                                ? Convert.ToString(reader["HouseIDSystem"]).Trim()
                                : ""
                    };
                    var regHouse = new Field
                    {
                        N = "RegHouse",
                        NT = "РИК дома",
                        IS = 0,
                        P = 2,
                        T = "TextType",
                        L = 25,
                        V = (reader["RegHouse"] != DBNull.Value)
                            ? Convert.ToString(reader["RegHouse"]).Trim()
                            : ""
                    };
                    var regionCode = new Field
                    {
                        N = "RegionCode",
                        NT = "Регион",
                        IS = 1,
                        P = 3,
                        T = "TextType",
                        L = 30,
                        V = (reader["RegionCode"] != DBNull.Value)
                            ? Convert.ToString(reader["RegionCode"]).Trim()
                            : ""
                    };
                    var areaCode = new Field
                    {
                        N = "AreaCode",
                        NT = "Район",
                        IS = 0,
                        P = 4,
                        T = "TextType",
                        L = 30,
                        V = (reader["AreaCode"] != DBNull.Value)
                            ? Convert.ToString(reader["AreaCode"]).Trim()
                            : ""
                    };
                    var cityCode = new Field
                    {
                        N = "CityCode",
                        NT = "Населенный пункт(город/село/деревня)",
                        IS = 1,
                        P = 5,
                        T = "TextType",
                        L = 30,
                        V = (reader["CityCode"] != DBNull.Value)
                            ? Convert.ToString(reader["CityCode"]).Trim()
                            : ""
                    };
                    var streetCode = new Field
                    {
                        N = "StreetCode",
                        NT = "Наименование улицы",
                        IS = 1,
                        P = 6,
                        T = "TextType",
                        L = 30,
                        V = (reader["StreetCode"] != DBNull.Value)
                            ? Convert.ToString(reader["StreetCode"]).Trim()
                            : ""
                    };
                    var houseNum = new Field
                    {
                        N = "HouseNum",
                        NT = "Номер дома",
                        IS = 1,
                        P = 7,
                        T = "TextType",
                        L = 10,
                        V = (reader["HouseNum"] != DBNull.Value)
                            ? Convert.ToString(reader["HouseNum"]).Trim()
                            : ""
                    };
                    var buildNum = new Field
                    {
                        N = "BuildNum",
                        NT = "Корпус дома",
                        IS = 0,
                        P = 8,
                        T = "TextType",
                        L = 3,
                        V = (reader["BuildNum"] != DBNull.Value)
                            ? Convert.ToString(reader["BuildNum"]).Trim()
                            : ""
                    };
                    var buildDate = new Field
                    {
                        N = "BuildDate",
                        NT = "Год постройки",
                        IS = 0,
                        P = 9,
                        T = "DateType",
                        L = 0,
                        V = (reader["BuildDate"] != DBNull.Value)
                            ? Convert.ToString(reader["BuildDate"]).Trim()
                            : ""
                    };
                    var prmList = (reader["HouseIDSystem"] == DBNull.Value)
                        ? null
                        : SelectPrmFromBD(reader["HouseIDSystem"].ToString());

                    var fields = new FieldsUnload
                    {
                        F = new List<Field>
                        {
                            houseIdSystem,
                            regHouse,
                            regionCode,
                            areaCode,
                            cityCode,
                            streetCode,
                            houseNum,
                            buildNum,
                            buildDate,
                        }
                    };
                    if (prmList != null)
                        fields.P = prmList;

                    Data.Add(fields);

                }

                #endregion выборка данных из временной таблицы

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("HouseRegister.Start(pref): Ошибка добавление записи в таблицу.\n: " + ex.Message,
                    MonitorLog.typelog.Error, true);
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

        private void FillHouseRegisterInfo(string pref)
        {
            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " WHERE d.nzp_area in (" + area + " )";
            }

            string unlDate = " CAST( '" + Year + "-" + Month + "-01' as DATE) ";
            string sql =
                " INSERT INTO " + Name +
                " (                                                     " +
                " HouseIDSystem,                                        " +
                " nzp_ul,                                               " +
                " StreetCode,                                           " +
                " nzp_raj,                                              " +
                " HouseNum,                                             " +
                " BuildNum                                              " +
                " )                                                     " +
                " SELECT                                                " +
                " d.nzp_dom,                                            " +
                " d.nzp_ul,                                             " +
                " SUBSTRING(TRIM(" + DBManager.sNvlWord + "(ulica, ''))||' '||TRIM(" + DBManager.sNvlWord +
                "(ulicareg, '')) from 1 for 40), " +
                " ul.nzp_raj,                                           " +
                " d.ndom,                                               " +
                " d.nkor                                                " +
                " FROM " + pref + DBManager.sDataAliasRest + "dom d                                " +
                " LEFT OUTER JOIN " + pref + DBManager.sDataAliasRest + "s_ulica ul ON ul.nzp_ul = d.nzp_ul " + whereArea;

            ExecSQL(sql);

            //Заполнение поля 'Населенный пункт(город/село/деревня)'
            sql =
                " UPDATE " + Name +
                " SET (CityCode, nzp_town) = (                          " +
                " 	(                                                   " +
                " 		SELECT                                          " +
                " 			SUBSTRING(rajon from 1 for 30)              " +
                " 		FROM                                            " +
                " 			" + pref + DBManager.sDataAliasRest + "s_rajon r                       " +
                " 		WHERE                                           " +
                " 			r.nzp_raj = HouseRegister.nzp_raj           " +
                " 	),                                                  " +
                " 	(                                                   " +
                " 		SELECT                                          " +
                " 			nzp_town                                    " +
                " 		FROM                                            " +
                " 			" + pref + DBManager.sDataAliasRest + "s_rajon r                       " +
                " 		WHERE                                           " +
                " 			r.nzp_raj = HouseRegister.nzp_raj           " +
                " 	)                                                   " +
                " )                                                     " +
                " WHERE EXISTS                                          " +
                " (                                                     " +
                " 		SELECT                                          " +
                " 			1                                           " +
                " 		FROM                                            " +
                " 			" + pref + DBManager.sDataAliasRest + "s_rajon r                       " +
                " 		WHERE                                           " +
                " 			r.nzp_raj = HouseRegister.nzp_raj           " +
                " ); ";

            ExecSQL(sql);

            //Заполнение поля 'Район'
            sql =
                " UPDATE " + Name +
                " SET (AreaCode, nzp_stat) = (                          " +
                " 	(                                                   " +
                " 		SELECT                                          " +
                " 			SUBSTRING(town from 1 for 30)              " +
                " 		FROM                                            " +
                " 			" + pref + DBManager.sDataAliasRest + "s_town t                        " +
                " 		WHERE                                           " +
                " 			t.nzp_town = HouseRegister.nzp_town         " +
                " 	),                                                  " +
                " 	(                                                   " +
                " 		SELECT                                          " +
                " 			nzp_stat                                    " +
                " 		FROM                                            " +
                " 			" + pref + DBManager.sDataAliasRest + "s_town t                        " +
                " 		WHERE                                           " +
                " 			t.nzp_town = HouseRegister.nzp_town         " +
                " 	)                                                   " +
                " )                                                     " +
                " WHERE EXISTS                                          " +
                " (                                                     " +
                " SELECT 1                                              " +
                " 		FROM                                            " +
                " 			" + pref + DBManager.sDataAliasRest + "s_town t                        " +
                " 		WHERE                                           " +
                " 			t.nzp_town = HouseRegister.nzp_town         " +
                " ); ";

            ExecSQL(sql);

            //Заполнение поля 'Регион'
            sql =
                " UPDATE " + Name +
                " SET (RegionCode) = (                                  " +
                " 	(                                                   " +
                " 		SELECT                                          " +
                "                 replace(SUBSTRING(stat from 1 for 30),'/ЯКУТИЯ/','(ЯКУТИЯ)')" +
                " 		FROM                                            " +
                " 			" + pref + DBManager.sDataAliasRest + "s_stat s                        " +
                " 		WHERE                                           " +
                " 			s.nzp_stat = HouseRegister.nzp_stat         " +
                " 	)                                                   " +
                " )                                                     " +
                " WHERE EXISTS                                          " +
                " (                                                     " +
                " SELECT 1 FROM                                         " +
                " 			" + pref + DBManager.sDataAliasRest + "s_stat s " +
                " 		WHERE                                           " +
                " 			s.nzp_stat = HouseRegister.nzp_stat         " +
                " ); ";

            ExecSQL(sql);

            //Заполнение поля 'BuildDate'
            sql =
                " UPDATE " + Name +
                " SET BuildDate = (                                     " +
                " 	SELECT                                              " +
                " 		CAST (val_prm AS DATE)                          " +
                " 	FROM                                                " +
                " 		" + pref + DBManager.sDataAliasRest + "prm_2 P " +
                " 	WHERE                                               " +
                " 		P .nzp_prm = 150                                " +
                " 	AND P .nzp = HouseRegister.HouseIDSystem            " +
                " 	AND P .is_actual <> 100                             " +
                " 	AND dat_s <= " + unlDate +
                " 	AND dat_po >= " + unlDate +
                " )                                                     " +
                " WHERE  EXISTS (                                       " +
                " 	SELECT                                              " +
                " 	1                                                   " +
                " 	FROM                                                " +
                " 		" + pref + DBManager.sDataAliasRest + "prm_2 P " +
                " 	WHERE                                               " +
                " 		P .nzp_prm = 150                                " +
                " 	AND P .nzp = HouseRegister.HouseIDSystem            " +
                " 	AND P .is_actual <> 100                             " +
                " 	AND dat_s <= " + unlDate +
                " 	AND dat_po >= " + unlDate +
                " ) ";

            ExecSQL(sql);
        }

        private List<ParamUnload> SelectPrmFromBD(string HouseIDSystem)
        {
            List<ParamUnload> list = new List<ParamUnload>();

            string sql;
            string nzp_prm = GetAllParamString(STypeParam.Dom);

            string dat_s = " CAST( '" + Year + "-" + Month.ToString("00") + "-01' as DATE) ";
            int dayInMonth = DateTime.DaysInMonth(Year, Month);
            string dat_po = " CAST( '" + Year + "-" + Month.ToString("00") + "-" + dayInMonth +
                            "' as DATE) ";

            string SD = new DateTime(Year, Month, 1).ToShortDateString();
            ParamUnload prm;

            try
            {
                #region параметры по nzp_prm
                if (nzp_prm != "")
                {
                    sql = " DELETE FROM " + tblNamePrm;

                    ExecSQL(sql);

                    sql =
                        " INSERT INTO " + tblNamePrm +
                        " (" +
                        "   nzp_prm," +
                        "   val_prm, " +
                        "   dat_s," +
                        "   dat_po" +
                        " )" +
                        " SELECT" +
                        "    nzp_prm," +
                        "    trim(val_prm)," +
                        "    dat_s," +
                        "    dat_po" +
                        " FROM " + _pref + DBManager.sDataAliasRest + "prm_2" +
                        " WHERE is_actual <> 100 AND dat_s <= " + dat_po +
                        " AND dat_po >= " + dat_s +
                        " AND nzp = " + HouseIDSystem +
                        " AND nzp_prm IN" +
                        " (" + nzp_prm + ")";

                    ExecSQL(sql);
                    
                    sql =
                        " UPDATE " + tblNamePrm +
                        " SET val_prm = ( " +
                        " CASE WHEN val_prm = '4' THEN val_prm = '1' ELSE " +
                        " CASE WHEN val_prm = '3' or val_prm = '1' THEN val_prm = '2' ELSE " +
                        " CASE WHEN val_prm = '5' THEN val_prm = '3' END END END ) " +
                        " WHERE nzp_prm = 631";
                    ExecSQL(sql);

                    sql = " SELECT * FROM " + tblNamePrm;
                    DataTable dt = ExecSQLToTable(sql);
                    foreach (DataRow r in dt.Rows)
                    {
                        List<string> kodPrm = GetPrmNum02(Convert.ToInt32(r["nzp_prm"]));
                        foreach (var kod in kodPrm)
                        {
                            prm = new ParamUnload
                            {
                                C = kod,
                                V = r["val_prm"].ToString().Trim(),
                                SD = r["dat_s"].ToString(),
                                ED = r["dat_po"].ToString()
                            };
                            list.Add(prm);
                        }
                    }
                }

                #endregion

                #region Общая площадь квартир

                sql = " SELECT SUM(p.val_prm::numeric)" +
                      " FROM " + _pref + DBManager.sDataAliasRest + "prm_1 p" +
                      " WHERE nzp IN ( " +
                      " SELECT k.nzp_kvar FROM " + _pref + DBManager.sDataAliasRest + "kvar k" +
                      " WHERE k.nzp_dom = " + HouseIDSystem +
                      " )" +
                      " AND nzp_prm = 4 and is_actual=1 and dat_s <=" + dat_po +
                      " and dat_po>=" + dat_s;

                DataTable _dt = ExecSQLToTable(sql);
                if (_dt.Rows.Count > 0)
                {
                    DataRow r = _dt.Rows[0];
                    prm = new ParamUnload
                    {
                        //Общая площадь квартир
                        C = "008",
                        V = r["sum"].ToString(),
                        SD = SD
                    };
                    list.Add(prm);
                }

                #endregion

            }
            catch (Exception ex)
            {
                AddComment(ex.Message);
            }

            return list;
        }


        public override void CreateTempTable()
        {
            string columnNames =
                " id SERIAL, " +
                " HouseIDSystem	INTEGER,	     " +
                " RegHouse	CHAR(25),            " +
                " RegionCode	CHAR(30),        " +
                " AreaCode	CHAR(30),            " +
                " CityCode	CHAR(30),            " +
                " StreetCode	CHAR(40),        " +
                " HouseNum	CHAR(10),            " +
                " BuildNum	CHAR(3),             " +
                " BuildDate	DATE,	             " +
                " AdditionalParametrs	CHAR(255), " +
                " nzp_ul INTEGER, " +
                " nzp_raj INTEGER, " +
                " nzp_town INTEGER, " +
                " nzp_stat INTEGER ";

            ExecSQL(" DROP TABLE " + Name, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", Name, columnNames));

            

            columnNames =
                " id SERIAL, " +
                " nzp_prm 	INTEGER," +
                " val_prm CHAR ( 20), " +
                " dat_s	 DATE," +
                " dat_po DATE ";

            ExecSQL(" DROP TABLE " + tblNamePrm, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblNamePrm, columnNames));
        }
    }
}
