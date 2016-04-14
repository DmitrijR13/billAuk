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
    public class PlaceRegister : BaseUnload20
    {
        /// <summary> Префикс локального бакнка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        /// <summary>Номер месяца (за который осуществляется выгрузка)</summary>
        public int _unloadMonthNumber;

        /// <summary>Год (за который осуществляется выгрузка)</summary>
        public int _unloadYear;

        string tblName = " PlaceRegister ";
        string tblNamePrm = " PlaceRegisterPrm ";

        public override string Name
        {
            get { return "PlaceRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр домохозяйств(помещений)"; }
        }

        public override int Code
        {
            get { return 4; }
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

            _pref = pref;
            _unloadMonthNumber = Month;
            _unloadYear = Year;

            try
            {
            SelectFromDB();

            #region заполнение полей
            string sql = " SELECT * FROM " + tblName;

            

            IDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                var houseIDSystem = new Field
                {
                    N = "HouseIDSystem",
                    NT = "Уникальный код дома в системе отправителя",
                    IS = 1,
                    P = 1,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["HouseIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["HouseIDSystem"])
                            : ""
                };
                var placeIDSystem = new Field
                {
                    N = "PlaceIDSystem",
                    NT = "Уникальный код домохозяйства в системе отправителя",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["PlaceIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["PlaceIDSystem"]).Trim()
                            : ""
                };

                var regPlaceID = new Field
                {
                    N = "RegPlaceID",
                    NT = "РИК домохозяйства",
                    IS = 0,
                    P = 3,
                    T = "TextType",
                    L = 25,
                    V = (reader["RegPlaceID"] != DBNull.Value) ? Convert.ToString(reader["RegPlaceID"]).Trim() : ""
                };
                var flatNum = new Field
                {
                    N = "FlatNum",
                    NT = "Номер квартиры",
                    IS = 1,
                    P = 4,
                    T = "TextType",
                    L = 5,
                    V =
                        (reader["FlatNum"] != DBNull.Value) ? Convert.ToString(reader["FlatNum"]).Trim() : ""
                };
                var flatNumber = new Field
                {
                    N = "FlatNumber",
                    NT = "Комната ЛС (Номер)",
                    IS = 0,
                    P = 5,
                    T = "TextType",
                    L = 5,
                    V = (reader["FlatNumber"] != DBNull.Value) ? Convert.ToString(reader["FlatNumber"]).Trim() : ""
                };

                var prmList = (reader["nzp_kvar"] == DBNull.Value)
                            ? null
                            : SelectPrmFromBD(reader["nzp_kvar"].ToString());

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {
                            houseIDSystem,
                            placeIDSystem,
                            regPlaceID,
                            flatNum,
                            flatNumber
                        }
                };
                if (prmList != null)
                    fields.P = prmList;
                Data.Add(fields);
            }
            #endregion

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("PlaceRegister.Start(pref): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally 
            {
                DropTempTable();
            CloseConnection();
        }
        }

        private void SelectFromDB()
        {

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " WHERE k.nzp_area in (" + area + " )";
            }

            string sql =
                " INSERT INTO " + tblName +
                " ( " +
                "   HouseIDSystem, " +
                "   PlaceIDSystem, " +
                "   nzp_kvar, " +
                "   FlatNum, " +
                "   FlatNumber" +
                "  ) " +
                " SELECT " +
                "   k.nzp_dom, " +
                "   k.num_ls, " +
                "   k.nzp_kvar, " +
                "   k.nkvar, " +
                "   k.nkvar_n " +
                " FROM  " + _pref + DBManager.sDataAliasRest + "kvar k" + whereArea;
            ExecSQL(sql);
        }

        private List<ParamUnload> SelectPrmFromBD(string PlaceIDSystem)
        {
            List<ParamUnload> list = new List<ParamUnload>();

            string sql;
            string nzp_prm = GetAllParamString(STypeParam.Kvar);

            string dat_s = " CAST( '" + _unloadYear + "-" + _unloadMonthNumber.ToString("00") + "-01' as DATE) ";
            int dayInMonth = DateTime.DaysInMonth(_unloadYear, _unloadMonthNumber);
            string dat_po = " CAST( '" + _unloadYear + "-" + _unloadMonthNumber.ToString("00") + "-" + dayInMonth +
                            "' as DATE) ";

            string SD = new DateTime(_unloadYear, _unloadMonthNumber, 1).ToShortDateString();
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
                        " FROM " + _pref + DBManager.sDataAliasRest + "prm_1" +
                        " WHERE is_actual <> 100 AND dat_s <= " + dat_po +
                        " AND dat_po >= " + dat_s +
                        " AND nzp = " + PlaceIDSystem +
                        " AND nzp_prm IN" +
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
                    if (Convert.ToInt32(r["val1"]) >= 0)
                    {
                        prm = new ParamUnload
                        {
                            //количество зарегистрированных
                            C = "029",
                            V = r["val1"].ToString(),
                            SD = SD
                        };
                        list.Add(prm);
                    }
                    if (Convert.ToInt32(r["val3"]) >= 0)
                    {
                        prm = new ParamUnload
                        {
                            //количество временно прибывших
                            C = "019",
                            V = r["val3"].ToString(),
                            SD = SD
                        };
                        list.Add(prm);
                    }
                    if (Convert.ToInt32(r["val5"]) >= 0)
                    {
                        prm = new ParamUnload
                        {
                            //количество временно убывших
                            C = "020",
                            V = r["val5"].ToString(),
                            SD = SD
                        };
                        list.Add(prm);
                    }
                    if (Convert.ToInt32(r["kol_g"]) >= 0)
                    {
                        prm = new ParamUnload
                        {
                            //количество проживающих
                            C = "018",
                            V = (r["kol_g"]).ToString(),
                            SD = SD
                        };
                        list.Add(prm);
                    }
                }

                #endregion

                #region Наличие ПУ

                sql = " SELECT *" +
                      " FROM " + _pref + DBManager.sDataAliasRest + "counters_spis" +
                      " WHERE is_actual <> 100 AND nzp_type = 3" +
                      " AND nzp = " + PlaceIDSystem;
                DataTable dtPU = ExecSQLToTable(sql);

                prm = new ParamUnload
                {
                    C = "027",
                    V = dtPU.Rows.Count > 0 ? "1" : "2",
                    SD = SD
                };
                list.Add(prm);


                #endregion
            }
            catch (Exception ex)
            {
                AddComment(ex.Message);
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
                " HouseIDSystem	 BIGINT , " +
                " PlaceIDSystem	 BIGINT , " +
                " nzp_kvar BIGINT, " +
                " RegPlaceID	 CHAR ( 25), " +
                " FlatNum	 CHAR ( 10), " +
                " FlatNumber	 CHAR ( 10) ";

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
