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
    public class GroupRegister : BaseUnload20
    {
        public override string Name
        {
            get { return "GroupRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр групп"; }
        }

        public override int Code
        {
            get { return 28; }
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

            InsertData(pref); 
            #region заполнение списка полей секции
            string sql = " select  GroupIDSystem , GroupType ,HouseIDSystem ,AccountIDSys,  PlaceIDSystem " +
                         " from GroupRegister";

            

            IDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                var groupIDSystem = new Field
                {
                    N = "GroupIDSystem",
                    NT = "Уникальный код группы в системе отправителя",
                    IS = 1,
                    P = 1,
                    T = "IntType",
                    L = 9,
                    V = (reader["GroupIDSystem"] != DBNull.Value) ? Convert.ToString(reader["GroupIDSystem"]).Trim() : ""
                };
                var groupType = new Field
                {
                    N = "GroupType",
                    NT = "Тип группы",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 1,
                    V =
                        (reader["GroupType"] != DBNull.Value)
                            ? Convert.ToString(reader["GroupType"]).Trim()
                            : ""
                };
                var houseIDSystem = new Field
                {
                    N = "HouseIDSystem",
                    NT = "Уникальный код дома в системе отправителя ",
                    IS = 0,
                    P = 3,
                    T = "IntType",
                    L = 18,
                    V = (reader["HouseIDSystem"] != DBNull.Value) ? Convert.ToString(reader["HouseIDSystem"]).Trim() : ""
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
                var placeIDSystem = new Field
                {
                    N = " PlaceIDSystem ",
                    NT = "Уникальный код домохозяйства в системе отправителя",
                    IS = 0,
                    P = 5,
                    T = "IntType",
                    L = 18,
                    V = (reader["PlaceIDSystem"] != DBNull.Value) ? Convert.ToString(reader["PlaceIDSystem"]).Trim() : ""
                };

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {   
                            groupIDSystem, 
                            groupType,
                            houseIDSystem,
                            accountIDSys,
                            placeIDSystem,
                        }
                };
                Data.Add(fields);
            }
            #endregion

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("GroupRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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

        public override void CreateTempTable()
        {
            string columnNames =
                " id SERIAL, " +
                " GroupIDSystem	 INTEGER , " +
                " GroupType	 INTEGER , " +
                " HouseIDSystem	BIGINT	, " +
                " AccountIDSys	BIGINT	, " +
                " PlaceIDSystem	BIGINT	" ;

            ExecSQL(" DROP TABLE " + Name, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", Name, columnNames));
        }

        private void InsertData(string pref)
        {
            int _Year = Year == 0 ? DateTime.Now.Year : Year;
            int _Month = Month == 0 ? DateTime.Now.Month : Month;
            string unlDate = " CAST( '" + _Year + "-" + _Month.ToString("00") + "-01' as DATE) ";
            string RegSuppTblName = Name;

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " and k.nzp_area in (" + area + " )";
            }

            string sql =
                " INSERT INTO " + RegSuppTblName + "                  " +
                " (                                                     " +
                " 	GroupIDSystem,                                         " +
                " 	GroupType,                                             " +
                " 	AccountIDSys                                             " +
                " )                                                     " +
                " SELECT                                                " +
                " 	s.nzp_counter,                                          " +
                "   2,  " +  // потомучто все по ЛС
                "   k.num_ls  " +
                " FROM " + pref + DBManager.sDataAliasRest + "counters_link l, "
                + pref + DBManager.sDataAliasRest + "kvar k , "
                + pref + DBManager.sDataAliasRest + "counters_spis s "
                + " WHERE s.nzp_counter=l.nzp_counter and l.nzp_kvar=k.nzp_kvar "
                +" AND s.is_actual = 1 AND (s.dat_close is null or s.dat_close >= " + unlDate+")" + whereArea;
            ExecSQL(sql);
        }
    }
}
