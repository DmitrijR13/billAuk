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
    public class AccountUnitRegister : BaseUnload20
    {
       
        public override string Name
        {
            get { return "AccountUnitRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр услуг лицевого счета"; }
        }

        public override int Code
        {
            get { return 21; }
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
            string sql = " select  * " +
                         " from AccountUnitRegister ";

            

            IDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                var regIndividalID = new Field
                {
                    N = "RegIndividalID ",
                    NT = "РИК лицевого счета",
                    IS = 1,
                    P = 1,
                    T = "TextType",
                    L = 25,
                    V = (reader["RegIndividalID"] != DBNull.Value) ? Convert.ToString(reader["RegIndividalID"]).Trim() : ""
                };
                var accountIDSys = new Field
                {
                    N = "AccountIDSys",
                    NT = "№ ЛС в системе отправителя",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["AccountIDSys"] != DBNull.Value)
                            ? Convert.ToString(reader["AccountIDSys"]).Trim()
                            : ""
                };
                var unitID = new Field
                {
                    N = "UnitID",
                    NT = "Код услуги",
                    IS = 1,
                    P = 3,
                    T = "IntType",
                    L = 3,
                    V = (reader["UnitID"] != DBNull.Value) ? Convert.ToString(reader["UnitID"]).Trim() : ""
                };
                var starteUnitDate = new Field
                {
                    N = "StarteUnitDate",
                    NT = "Дата начала действия услуги",
                    IS = 1,
                    P = 4,
                    T = "IntType",
                    L = 18,
                    V = (reader["StarteUnitDate"] != DBNull.Value) ? Convert.ToDateTime(reader["StarteUnitDate"]).ToString("dd.MM.yyyy hh:mm:ss") : ""
                };
                var endUnitDate = new Field
                {
                    N = "EndUnitDate",
                    NT = "Дата окончания действия услуги",
                    IS = 1,
                    P = 5,
                    T = "IntType",
                    L = 18,
                    V = (reader["EndUnitDate"] != DBNull.Value) ? Convert.ToDateTime(reader["EndUnitDate"]).ToString("dd.MM.yyyy hh:mm:ss") : ""
                };
                var contracSenderID = new Field
                {
                    N = "ContracSenderID",
                    NT = "Уникальный код договора в системе отправителя",
                    IS = 1,
                    P = 6,
                    T = "IntType",
                    L = 9,
                    V = (reader["ContracSenderID"] != DBNull.Value) ? Convert.ToString(reader["ContracSenderID"]).Trim() : ""
                };
                var methodNumber = new Field
                {
                    N = "MethodNumber",
                    NT = "Номер методики расчета",
                    IS = 0,
                    P = 7,
                    T = "IntType",
                    L = 5,
                    V = (reader["MethodNumber"] != DBNull.Value) ? Convert.ToString(reader["MethodNumber"]).Trim() : ""
                }; 
                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {   
                            regIndividalID,
                            accountIDSys,
                            unitID,
                            starteUnitDate,
                            endUnitDate,
                            contracSenderID,
                            methodNumber
                        }
                };
                Data.Add(fields);
            }
            #endregion

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("AccountUnitRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
            string tblName = " AccountUnitRegister ";
            string columnNames =
                " id SERIAL, " +
                " RegIndividalID	 CHAR(25) , " +   //рик
                " AccountIDSys	  BIGINT	, " +
                " UnitID	INTEGER, " +
                " StarteUnitDate	DATE	, " +
                " EndUnitDate	DATE	, " +
                " ContracSenderID	INTEGER,	"+
                " MethodNumber 	INTEGER	";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));
        }

        private void InsertData(string pref)
        {
            int _Year = Year == 0 ? DateTime.Now.Year : Year;
            int _Month = Month == 0 ? DateTime.Now.Month : Month;
            string dat_s = " CAST( '" + _Year + "-" + _Month + "-01' as DATE) ";
            string dat_po = " CAST ( '" + _Year + "-" + _Month + "-" + DateTime.DaysInMonth(_Year, _Month) + "' as DATE ) ";

            string AccUnitTblName = Name;

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " and k.nzp_area in (" + area + " )";
            }

            string sql =
                " INSERT INTO " + AccUnitTblName + "   " +
                " (                                    " +
              //  " 	RegIndividalID,                    " +
                " 	AccountIDSys,                      " +
                " 	UnitID,                            " +
                " 	StarteUnitDate,                    " +
                " 	EndUnitDate,                       " +
                " 	ContracSenderID,                   " +
                " 	MethodNumber                       " +
                " )                                    " +
                " SELECT                               " +
             //   " 	,         " +
                " 	t.num_ls,   " +
                " 	t.nzp_serv, " +
                " 	t.dat_s, " +
                " 	t.dat_po, " +
                " 	t.nzp_supp, " +
                " 	t.nzp_frm " +
                " FROM " + pref + DBManager.sDataAliasRest + "tarif t left outer join " + pref + DBManager.sDataAliasRest + "kvar k on t.nzp_kvar = k.nzp_kvar " +
                " WHERE t.is_actual = 1 and t.dat_s <= " + dat_po +
                " AND t.dat_po >= " + dat_s +
                " AND t.nzp_serv < 1007568 " + whereArea;

                ExecSQL(sql);
        }

    }
}
