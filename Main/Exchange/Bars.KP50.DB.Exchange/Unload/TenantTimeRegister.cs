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
    public class TenantTimeRegister : BaseUnload20
    {
        /// <summary> Префикс локального бакнка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        public override string Name
        {
            get { return "TenantTimeRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр временно убывших"; }
        }

        public override int Code
        {
            get { return 19; }
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

            Select();

           string sql = " select * from TenantTimeRegister ";

            

            IDataReader reader;

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                #region заполнение полей секции
                var regPlace = new Field
                {
                    N = "RegPlace",
                    NT = "РИК домохозяйства",
                    IS = 0,
                    P = 1,
                    T = "TextType",
                    L = 25,
                    V =
                        (reader["RegPlace"] != DBNull.Value)
                            ? Convert.ToString(reader["RegPlace"]).Trim()
                            : ""
                };
                var placeIdSystem = new Field
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
                var tenantId = new Field
                {
                    N = "TenantID",
                    NT = "Уникальный номер жильца (гражданина)",
                    IS = 1,
                    P = 3,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["TenantID"] != DBNull.Value)
                            ? Convert.ToString(reader["TenantID"]).Trim()
                            : ""
                };
                var startDate = new Field
                {
                    N = "StartDate",
                    NT = "Дата начала",
                    IS = 1,
                    P = 4,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["StartDate"] != DBNull.Value)
                            ? Convert.ToString(reader["StartDate"]).Trim()
                            : ""
                };
                var endDate = new Field
                {
                    N = "EndDate",
                    NT = "Дата окончания",
                    IS = 1,
                    P = 5,
                    T = "DateType",
                    L = 0,
                    V =
                        (reader["EndDate"] != DBNull.Value)
                            ? Convert.ToString(reader["EndDate"]).Trim()
                            : ""
                };
                var fields = new FieldsUnload
                {
                    F = new List<Field>
                    {
                      regPlace,
                      placeIdSystem,
                      tenantId,
                      startDate,
                      endDate
                    }
                };

                #endregion

                Data.Add(fields);
            }

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("TenantTimeRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
            string tblName = " TenantTimeRegister ";
            string columnNames =
                " id SERIAL, " +
                " RegPlace	 CHAR (25), " +
                " PlaceIDSystem	 BIGINT , " +
                " TenantID	 BIGINT , " +
                " StartDate	 DATE 	, " +
                " EndDate	 DATE 	 ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));
        }

        private void Select()
        {

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " WHERE k.nzp_area in ( " + area + " )";
            }

            string sql = "INSERT INTO " + Name +
                         " ( " +
                         " PlaceIDSystem ," +
                         " TenantID, " +
                         " StartDate, " +
                         " EndDate " +
                         " ) " +
                         " SELECT " +
                         DBManager.sNvlWord + "( k.num_ls, g.nzp_kvar) , " +
                         " g.nzp_gilec, " +
                         " g.dat_s, " +
                         " g.dat_po " +
                         " FROM " + _pref + DBManager.sDataAliasRest + "gil_periods g left outer join " + _pref + DBManager.sDataAliasRest + "kvar k on g.nzp_kvar = k.nzp_kvar" + whereArea;
            ExecSQL(sql);
        }


    }
}
