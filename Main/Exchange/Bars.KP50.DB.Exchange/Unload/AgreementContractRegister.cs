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
    public class AgreementContractRegister : BaseUnload20
    {
        /// <summary> Префикс локального бакнка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        string tblName = " AgreementContractRegister ";

        public override string Name
        {
            get { return "AgreementContractRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр соглашений по перечислению в разрезе договоров"; }
        }

        public override int Code
        {
            get { return 20; }
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

            try
            {

            SelectFromDB();
             

            string sql = " SELECT * FROM " + tblName;

            

            IDataReader reader;

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                #region заполнение полей секции
                var contracSenderId = new Field
                {
                    N = "ContracSenderID",
                    NT = "Уникальный код договора в системе отправителя",
                    IS = 1,
                    P = 1,
                    T = "IntType",
                    L = 9,
                    V =
                        (reader["ContracSenderID"] != DBNull.Value)
                            ? Convert.ToString(reader["ContracSenderID"]).Trim()
                            : ""
                };
                var houseIdSystem = new Field
                {
                    N = "HouseIDSystem",
                    NT = "Уникальный код дома в системе отправителя",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["HouseIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["HouseIDSystem"]).Trim()
                            : ""
                };
                var transUnitId = new Field
                {
                    N = "TransUnitID",
                    NT = "Код услуги",
                    IS = 1,
                    P = 3,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["TransUnitID"] != DBNull.Value)
                            ? Convert.ToString(reader["TransUnitID"]).Trim()
                            : ""
                };
                var idLegal = new Field
                {
                    N = "IDLegal",
                    NT = "Код агента получателя комиссии за обслуживание",
                    IS = 1,
                    P = 4,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["IDLegal"] != DBNull.Value)
                            ? Convert.ToString(reader["IDLegal"]).Trim()
                            : ""
                };
                var reUnitId = new Field
                {
                    N = "ReUnitID",
                    NT = "Код услуги",
                    IS = 1,
                    P = 5,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["ReUnitID"] != DBNull.Value)
                            ? Convert.ToString(reader["ReUnitID"]).Trim()
                            : ""
                };
                var holdPercent = new Field
                {
                    N = "HoldPercent",
                    NT = "Процент удержания",
                    IS = 1,
                    P = 6,
                    T = "DecimalType",
                    L = 10,
                    V =
                        (reader["HoldPercent"] != DBNull.Value)
                            ? Convert.ToString(reader["HoldPercent"]).Trim()
                            : ""
                };
                var agrStartDate = new Field
                {
                    N = "AgrStartDate",
                    NT = "Дата начала действия соглашения",
                    IS = 1,
                    P = 7,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["AgrStartDate"] != DBNull.Value)
                            ? Convert.ToString(reader["AgrStartDate"]).Trim()
                            : ""
                };
                var agrEndDate = new Field
                {
                    N = "AgrEndDate",
                    NT = "Дата окончания действия соглашения",
                    IS = 1,
                    P = 8,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["AgrEndDate"] != DBNull.Value)
                            ? Convert.ToString(reader["AgrEndDate"]).Trim()
                            : ""
                };

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {   
                            contracSenderId,
                            houseIdSystem,
                            transUnitId,
                            idLegal,
                            reUnitId,
                            holdPercent,
                            agrStartDate,
                            agrEndDate
                        }
                };
                #endregion

                Data.Add(fields);
            }

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("AgreementContractRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
                whereArea = " and d.nzp_area in (" + area + " )";
            }

            string sql =
                 " INSERT INTO " + tblName +
                 " ( " +
                 "   ContracSenderID, " +
                 "   HouseIDSystem, " +
                 "   TransUnitID, " +
                 "   IDLegal," +
                 "   ReUnitID," +
                 "   HoldPercent," +
                 "   AgrStartDate," +
                 "   AgrEndDate" +
                 "  ) " +
                 " SELECT " +
                 "   fn.nzp_supp, " +
                 "   fn.nzp_dom," +
                 "   fn.nzp_serv_from," +
                 "   fn.nzp_payer," +
                 "   fn.nzp_serv," +
                 "   fn.perc_ud," +
                 "   fn.dat_s," +
                 "   fn.dat_po " +
                 " FROM  " + Points.Pref + DBManager.sDataAliasRest + "fn_percent_dom fn " +
                 " left outer join " + _pref + DBManager.sDataAliasRest + "dom d on fn.nzp_dom = d.nzp_dom " +
                 " WHERE fn.nzp_dom > 0 AND fn.nzp_supp IN " +
                 "      (SELECT s1.nzp_supp FROM " + _pref + DBManager.sKernelAliasRest + "supplier s1," +
                 "      " + Points.Pref + DBManager.sKernelAliasRest + "supplier s2" +
                 "      WHERE s1.nzp_supp = s2.nzp_supp) " +
                 " AND fn.nzp_serv_from < 1007568 " +
                 " AND fn.nzp_serv < 1007568 " + whereArea;
            ExecSQL(sql);
        }

        public override void StartSelect()
        {
        }

        public override void CreateTempTable()
        {
            string columnNames =
                " id SERIAL, " +
                " ContracSenderID	 INTEGER , " +
                " HouseIDSystem	 BIGINT , " +
                " TransUnitID	INTEGER	, " +
                " IDLegal	INTEGER	, " +
                " ReUnitID	INTEGER	, " +
                " HoldPercent	" + DBManager.sDecimalType + " (14,2) , " +
                " AgrStartDate	 DATE 	, " +
                " AgrEndDate	 DATE 	";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));
        }

        public override void DropTempTable()
        {
        }
    }
}
