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
    public class CheckAccountRegister: BaseUnload20
    {
        public override string Name
        {
            get { return "CheckAccountRegister"; }
        }

        public override string NameText
        {
            get { return "Расчётные счета"; }
        }

        public override int Code
        {
            get { return 18; }
        }

        /// <summary> Префикс локального бакнка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        public override List<FieldsUnload> Data { get; set; }

        public override void Start()
        {
            Data = new List<FieldsUnload>();
            OpenConnection();
            CreateTempTable();
            try
            {

            InsertData();

            #region Выборка данных

            string sql = " select  CurrentAccount ,IDLegalSys ,CorrespondentAccount ,BIKBank " +
                         " from  " + Name;

            

            IDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                var currentAccount = new Field
                {
                    N = "CurrentAccount",
                    NT = "РИК лицевого счета",
                    IS = 1,
                    P = 1,
                    T = "TextType",
                    L = 20,
                    V = (reader["CurrentAccount"] != DBNull.Value) ? Convert.ToString(reader["CurrentAccount"]).Trim() : ""
                };
                var iDLegalSys = new Field
                {
                    N = "IDLegalSys",
                    NT = "Уникальный код ЮЛ в системе отправителя",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 9,
                    V =
                        (reader["IDLegalSys"] != DBNull.Value)
                            ? Convert.ToString(reader["IDLegalSys"])
                            : ""
                };
                var correspondentAccount = new Field
                {
                    N = "CorrespondentAccount",
                    NT = "Корреспондентский счет",
                    IS = 1,
                    P = 3,
                    T = "TextType",
                    L = 20,
                    V = (reader["CorrespondentAccount"] != DBNull.Value) ? Convert.ToString(reader["CorrespondentAccount"]).Trim() : ""
                };
                var paymentNum = new Field
                {
                    N = "BIKBank",
                    NT = "БИК банка",
                    IS = 1,
                    P = 4,
                    T = "IntType",
                    L = 15,
                    V = (reader["BIKBank"] != DBNull.Value) ? Convert.ToString(reader["BIKBank"]).Trim() : ""
                };

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {
                            currentAccount,
                            iDLegalSys,
                            correspondentAccount,
                            paymentNum,
                        }
                };
                Data.Add(fields);

            }
            #endregion

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("ContractRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally 
            {
            DropTempTable();
            CloseConnection(); 
        }

        }

        public override void Start(string pref)
        {
            _pref = pref;
            OpenConnection();
            CreateTempTable();
            InsertData();

            #region Выборка данных

            string sql = " select  CurrentAccount ,IDLegalSys ,CorrespondentAccount ,BIKBank " +
                         " from  " + Name;

            Data = new List<FieldsUnload>();

            IDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                var currentAccount = new Field
                {
                    N = "CurrentAccount",
                    NT = "РИК лицевого счета",
                    IS = 1,
                    P = 1,
                    T = "TextType",
                    L = 20,
                    V = (reader["CurrentAccount"] != DBNull.Value) ? Convert.ToString(reader["CurrentAccount"]).Trim() : ""
                };
                var iDLegalSys = new Field
                {
                    N = "IDLegalSys",
                    NT = "Уникальный код ЮЛ в системе отправителя",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 9,
                    V =
                        (reader["IDLegalSys"] != DBNull.Value)
                            ? Convert.ToString(reader["IDLegalSys"]).Trim()
                            : ""
                };
                var correspondentAccount = new Field
                {
                    N = "CorrespondentAccount",
                    NT = "Корреспондентский счет",
                    IS = 1,
                    P = 3,
                    T = "TextType",
                    L = 20,
                    V = (reader["CorrespondentAccount"] != DBNull.Value) ? Convert.ToString(reader["CorrespondentAccount"]).Trim() : ""
                };
                var paymentNum = new Field
                {
                    N = "BIKBank",
                    NT = "БИК банка",
                    IS = 1,
                    P = 4,
                    T = "IntType",
                    L = 15,
                    V = (reader["BIKBank"] != DBNull.Value) ? Convert.ToString(reader["BIKBank"]).Trim() : ""
                };

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {
                            currentAccount,
                            iDLegalSys,
                            correspondentAccount,
                            paymentNum,
                        }
                };
                Data.Add(fields);

            }
            #endregion

            DropTempTable();
            CloseConnection(); 
        }

        public override void StartSelect()
        { 
        }

        private void InsertData()
        {
            string ChkAccountRegTblName = Name;

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " WHERE s.nzp_area in (" + area + " )";
            }

            string sql =
                " INSERT INTO " + ChkAccountRegTblName + "                  " +
                " (                                                     " +
                " 	CurrentAccount,                                         " +
                " 	IDLegalSys,                                             " +
                " 	CorrespondentAccount,                                             " +
                " 	BIKBank                                           " +
                " )                                                     " +
                " SELECT                                                " +
                " 	trim(fn.rcount),                                          " +
                " 	fn.nzp_payer,   " +
                " 	trim(fn.kcount),    " +
                " 	fn.bik" + DBManager.sConvToInt +
                " FROM " + Points.Pref+ DBManager.sDataAliasRest + "fn_bank fn " +
                " LEFT OUTER JOIN " + _pref + DBManager.sDataAliasRest + "s_area s ON fn.nzp_payer = s.nzp_payer " + whereArea;
            ExecSQL(sql);     
        }

        public override void CreateTempTable()
        {   
            string columnNames =
                " id SERIAL, " +
                " CurrentAccount	 CHAR (20), " +
                " IDLegalSys	 INTEGER , " +
                " CorrespondentAccount	 CHAR (20), " +
                " BIKBank	 BIGINT ";

            ExecSQL(" DROP TABLE " + Name, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ",Name, columnNames));
        }
    }
}
