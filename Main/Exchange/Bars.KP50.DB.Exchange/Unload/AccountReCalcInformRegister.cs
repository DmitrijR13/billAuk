using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DB.Exchange.Unload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using STCLINE.KP50.Interfaces;
    using STCLINE.KP50.Global;

    public class AccountReCalcInformRegister : BaseUnload20
    {
        /// <summary> Префикс локального бакнка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        /// <summary>Номер месяца (за который осуществляется выгрузка)</summary>
        public int _unloadMonthNumber;

        /// <summary>Год (за который осуществляется выгрузка)</summary>
        public int _unloadYear;

        string tblName = " AccountReCalcInformRegister ";
        string tblNamePrm = " AccountReCalcInformRegisterPrm ";

        public override string Name
        {
            get { return "AccountReCalcInformRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр информации параметров ЛС, в месяце перерасчета"; }
        }

        public override int Code
        {
            get { return 9; }
        }

        public override List<FieldsUnload> Data { get; set; }

        public override void Start()
        {
           
        }

        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();
            OpenConnection();
            CreateTempTable();

            try
            {

            _pref = pref;
            _unloadMonthNumber = Month;
            _unloadYear = Year;

            SelectFromDB();

            string sql = " select * from AccountReCalcInformRegister ";

            

            IDataReader reader;

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                #region заполнение полей секции
                var placeIdSystem = new Field
                {
                    N = "PlaceIDSystem",
                    NT = "Уникальный код домохозяйства в системе отправителя",
                    IS = 1,
                    P = 1,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["PlaceIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["PlaceIDSystem"]).Trim()
                            : ""
                };
                var regAccount = new Field
                {
                    N = "RegAccount",
                    NT = "РИК лицевого счета",
                    IS = 0,
                    P = 1,
                    T = "TextType",
                    L = 25,
                    V =
                        (reader["RegAccount"] != DBNull.Value)
                            ? Convert.ToString(reader["RegAccount"]).Trim()
                            : ""
                };
                var recalculationDate = new Field
                {
                    N = "RecalculationDate",
                    NT = "Месяц и год перерасчета",
                    IS = 1,
                    P = 3,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["RecalculationDate"] != DBNull.Value)
                            ? Convert.ToString(reader["RecalculationDate"]).Trim()
                            : ""
                };
                var accountIdSys = new Field
                {
                    N = "AccountIDSys",
                    NT = "№ ЛС в системе отправителя",
                    IS = 1,
                    P = 4,
                    T = "TextType",
                    L = 25,
                    V =
                        (reader["AccountIDSys"] != DBNull.Value)
                            ? Convert.ToString(reader["AccountIDSys"]).Trim()
                            : ""
                };
                var individualIdSystem = new Field
                {
                    N = "IndividualIDSystem",
                    NT = "Уникальный код физического лица в системе отправителя",
                    IS = 1,
                    P = 5,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["IndividualIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["IndividualIDSystem"]).Trim()
                            : ""
                };
                var birthDate = new Field
                {
                    N = "BirthDate",
                    NT = "Дата рождения кв.съемщика",
                    IS = 0,
                    P = 6,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["BirthDate"] != DBNull.Value)
                            ? Convert.ToString(reader["BirthDate"]).Trim()
                            : ""
                };
                var flatNumber = new Field
                {
                    N = "FlatNumber",
                    NT = "Квартира",
                    IS = 0,
                    P = 7,
                    T = "TextType",
                    L = 10,
                    V =
                        (reader["FlatNumber"] != DBNull.Value)
                            ? Convert.ToString(reader["FlatNumber"]).Trim()
                            : ""
                };
                var roomNumber = new Field
                {
                    N = "RoomNumber",
                    NT = "Комната",
                    IS = 0,
                    P = 8,
                    T = "TextType",
                    L = 3,
                    V =
                        (reader["RoomNumber"] != DBNull.Value)
                            ? Convert.ToString(reader["RoomNumber"]).Trim()
                            : ""
                };
                var beginDate = new Field
                {
                    N = "BeginDate",
                    NT = "Дата открытия ЛС",
                    IS = 0,
                    P = 9,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["BeginDate"] != DBNull.Value)
                            ? Convert.ToString(reader["BeginDate"]).Trim()
                            : ""
                };
                var beginReason = new Field
                {
                    N = "BeginReason",
                    NT = "Основание открытия ЛС",
                    IS = 0,
                    P = 10,
                    T = "TextType",
                    L = 100,
                    V =
                        (reader["BeginReason"] != DBNull.Value)
                            ? Convert.ToString(reader["BeginReason"]).Trim()
                            : ""
                };
                var endDate = new Field
                {
                    N = "EndDate",
                    NT = "Дата закрытия ЛС",
                    IS = 0,
                    P = 11,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["EndDate"] != DBNull.Value)
                            ? Convert.ToString(reader["EndDate"]).Trim()
                            : ""
                };
                var endReason = new Field
                {
                    N = "EndReason",
                    NT = "Основание закрытия ЛС",
                    IS = 0,
                    P = 12,
                    T = "TextType",
                    L = 100,
                    V =
                        (reader["EndReason"] != DBNull.Value)
                            ? Convert.ToString(reader["EndReason"]).Trim()
                            : ""
                };


                var prmList = (reader["nzp_kvar"] == DBNull.Value && reader["RecalculationDate"] != DBNull.Value)
                            ? null
                            : SelectPrmFromBD(reader["nzp_kvar"].ToString(), reader["RecalculationDate"].ToString().Trim());

                var fields = new FieldsUnload
                {
                    F = new List<Field>
                    {
                        placeIdSystem,
                        //regAccount,
                        recalculationDate,
                        accountIdSys,
                        individualIdSystem,
                        birthDate,
                        flatNumber,
                        roomNumber,
                        beginDate,
                        beginReason,
                        endDate,
                        endReason
                    }
                };
                if (prmList != null)
                    fields.P = prmList;
                #endregion

                Data.Add(fields);
            }

               }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("AccountReCalcInformRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally 
            {
                DropTempTable();
                CloseConnection();
            }
        }

        private void SelectFromDB()
        {
            string chargeLnkYY = _pref + "_charge_" + _unloadYear.ToString().Substring(2,2) + 
                DBManager.tableDelimiter + "lnk_charge_" + _unloadMonthNumber.ToString("00");
            try
            {
                string area = GetNzpArea(ListNzpArea);
                string whereArea = "";
                if (area != String.Empty)
                {
                    whereArea = " and kv.nzp_area in (" + area + " )";
                }

                string sql =
                    " INSERT INTO " + tblName +
                    " (" +
                    "   PlaceIDSystem, " +
                    "   nzp_kvar, " +
                    "   RecalculationDate, " +
                    "   IndividualIDSystem, " +
                    "   BirthDate, " +
                    "   FlatNumber, " +
                    "   RoomNumber " +
                    " )" +
                    " SELECT  kv.num_ls, kv.nzp_kvar, DATE('01.' || lnk.month_ || '.' || lnk.year_) as recalculation_date," +
                    " kart.nzp_gil, kart.dat_rog, kv.nkvar, kv.nkvar_n" +
                    " FROM " + _pref + DBManager.sDataAliasRest + "kvar kv" +
                    " LEFT OUTER JOIN " + _pref + DBManager.sDataAliasRest + "kart kart on kv.nzp_kvar = kart.nzp_kvar " +
                    " LEFT OUTER JOIN " + _pref + DBManager.sDataAliasRest + "s_rod rod on kart.nzp_rod = rod.nzp_rod " +
                    " AND (rod LIKE 'наним%' OR rod LIKE 'собств%' OR rod LIKE 'владел%') ," +
                    chargeLnkYY + " lnk" +
                    " WHERE lnk.nzp_kvar = kv.nzp_kvar " + whereArea;
                ExecSQL(sql);
                
                string dat_s = " CAST( '" + _unloadYear + "-" + _unloadMonthNumber.ToString("00") + "-01' as DATE) ";
                int dayInMonth = DateTime.DaysInMonth(_unloadYear, _unloadMonthNumber);
                string dat_po = " CAST( '" + _unloadYear + "-" + _unloadMonthNumber.ToString("00") + "-" + dayInMonth +
                                "' as DATE) ";

                sql =
                    " UPDATE " + tblName +
                    " SET BeginDate =" +
                    " (SELECT p.dat_s FROM " + _pref + DBManager.sDataAliasRest + "prm_3 p" +
                    " WHERE p.nzp_prm = 51 AND trim(p.val_prm) = '1' AND is_actual <> 100" +
                    " AND dat_s <= " + dat_s + " AND dat_po >= " + dat_po +
                    " AND p.nzp = " + tblName + ".PlaceIDSystem )";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tblName +
                    " SET EndDate =" +
                    " (SELECT p.dat_s FROM " + _pref + DBManager.sDataAliasRest + "prm_3 p" +
                    " WHERE p.nzp_prm = 51 AND trim(p.val_prm) = '2' AND is_actual <> 100" +
                    " AND dat_s <= " + dat_s + " AND dat_po >= " + dat_po +
                    " AND p.nzp = " + tblName + ".PlaceIDSystem )";
                ExecSQL(sql);
            }
            catch (Exception ex)
            {
                AddComment("AccountReCalcInformRegister : SelectFromBD : " + ex.Message);
            }
        }


        private List<ParamUnload> SelectPrmFromBD(string PlaceIDSystem, string Date)
        {
            List<ParamUnload> list = new List<ParamUnload>();
            
            string sql;
            string nzp_prm = "107";

            string dat_s = " CAST( '" + Date + "' as DATE) ";
            int dayInMonth = DateTime.DaysInMonth(Convert.ToInt32(Date.Substring(6, 4)), Convert.ToInt32(Date.Substring(3, 2)));
            string dat_po = " CAST( '" + Date.Substring(6, 4) + "-" + Date.Substring(3, 2) + "-" + dayInMonth +
                            "' as DATE) ";

            string SD = new DateTime(Convert.ToInt32(Date.Substring(6, 4)), Convert.ToInt32(Date.Substring(3, 2)), 1).ToShortDateString();
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
                        "    val_prm," +
                        "    dat_s," +
                        "    dat_po" +
                        " FROM " + _pref + DBManager.sDataAliasRest + "prm_1" +
                        " WHERE is_actual <> 100 AND dat_s <= " + dat_s +
                        " AND dat_po >= " + dat_po +
                        " AND nzp = " + PlaceIDSystem +
                        " AND nzp_prm in" +
                        " (" + nzp_prm + ")";

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

                #region количество проживающих, временно прибывшие, временно убывшие, количество зарегистрированных

                string gil = _pref + "_charge_" + _unloadYear.ToString().Substring(2, 2) + DBManager.tableDelimiter +
                             "gil_" + _unloadMonthNumber.ToString("00");
                sql = " SELECT val1, val3, val5, (val1 + val3 - val5) as kol_g" +
                      " FROM " + gil +
                      " WHERE nzp_kvar = " + PlaceIDSystem;
                DataTable dtGil = ExecSQLToTable(sql);
                if (dtGil.Rows.Count > 0)
                {
                    DataRow r = dtGil.Rows[0];

                    prm = new ParamUnload
                    {
                        //количество временно прибывших
                        C = "019",
                        V = r["val3"].ToString(),
                        SD = SD
                    };
                    list.Add(prm);

                    prm = new ParamUnload
                    {
                        //количество временно убывших
                        C = "020",
                        V = r["val5"].ToString(),
                        SD = SD
                    };
                    list.Add(prm);

                    prm = new ParamUnload
                    {
                        //количество проживающих
                        C = "018",
                        V = (r["kol_g"]).ToString(),
                        SD = SD
                    };
                    list.Add(prm);

                }

                #endregion
            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                //AddComment("AccountReCalcInformRegister : SelectPrmFromBD : " + ex.Message);
            }

            return list;
        }

        public override void StartSelect()
        {
        }

        public override void CreateTempTable()
        {
            string columnNames =
                " id SERIAL, " +
                " PlaceIDSystem	 BIGINT , " +
                " nzp_kvar BIGINT, " +
                " RegAccount	 CHAR (25), " +
                " RecalculationDate	 DATE 	, " +
                " AccountIDSys	 CHAR (25), " +
                " IndividualIDSystem	 BIGINT , " +
                " BirthDate	 DATE 	, " +
                " FlatNumber	 CHAR (10), " +
                " RoomNumber	 CHAR (3), " +
                " BeginDate	 DATE 	, " +
                " BeginReason	 CHAR (100), " +
                " EndDate	 DATE 	, " +
                " EndReason	 CHAR (100) "
                //+ " ,AdditionalParametrs	CHAR (255) 	 "
                ;

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));

            columnNames =
                " id SERIAL, " +
                " nzp_prm 	INTEGER," +
                " val_prm CHAR ( 20), " +
                " dat_s	 DATE," +
                " dat_po DATE ";

            ExecSQL(" DROP TABLE " + tblNamePrm, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblNamePrm, columnNames));
        }

        public override void DropTempTable()
        {
        }
    }
}
