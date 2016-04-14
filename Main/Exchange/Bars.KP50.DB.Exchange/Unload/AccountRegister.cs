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
    public class AccountRegister : BaseUnload20
    {
        public override string Name
        {
            get { return "AccountRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр лицевых счетов"; }
        }

        public override int Code
        {
            get { return 6; }
        }

        public override List<FieldsUnload> Data { get; set; }

        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();
            OpenConnection();
            CreateTempTable();

            try
            {
            FillAccountsRegisterInfo(pref);

            string sql =
                " SELECT PlaceIDSystem, IndividalIDSystem,  RegAccount, AccountIDSys, BeginDate, BeginReason, EndDate, EndReason " +
                " FROM " + Name;

            

            IDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                    #region заполнение списка полей секции
                var placeIDSystem = new Field
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
                var individalIDSystem = new Field
                {
                    N = "IndividalIDSystem",
                    NT = "Уникальный код физического лица в системе отправителя",
                    IS = 1,
                    P = 2,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["IndividalIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["IndividalIDSystem"])
                            : ""
                };
                var regAccount = new Field
                {
                    N = "RegAccount",
                    NT = "РИК лицевого счета",
                    IS = 1,
                    P = 3,
                    T = "TextType",
                    L = 25,
                    V = (reader["RegAccount"] != DBNull.Value) ? Convert.ToString(reader["RegAccount"]).Trim() : ""
                };
                var accountIDSys = new Field
                {
                    N = "AccountIDSys",
                    NT = "№ лицевого счета в системе отправителя",
                    IS = 1,
                    P = 4,
                    T = "IntType",
                    L = 18,
                    V =
                        (reader["AccountIDSys"] != DBNull.Value) ? Convert.ToString(reader["AccountIDSys"]).Trim() : ""
                };
                var beginDate = new Field
                {
                    N = "BeginDate",
                    NT = "Дата открытия ЛС",
                    IS = 1,
                    P = 5,
                    T = "DateType",
                    L = 0,
                    V = (reader["BeginDate"] != DBNull.Value) ? Convert.ToDateTime(reader["BeginDate"]).ToShortDateString() : ""
                };
                var beginReason = new Field
                {
                    N = "BeginReason",
                    NT = "Основание открытия",
                    IS = 0,
                    P = 6,
                    T = "TextType",
                    L = 100,
                    V =
                        (reader["BeginReason"] != DBNull.Value) ? Convert.ToString(reader["BeginReason"]).Trim() : ""
                };
                var endDate = new Field
                {
                    N = "EndDate",
                    NT = "Дата закрытия ЛС",
                    IS = 1,
                    P = 7,
                    T = "DateType",
                    L = 0,
                    V = (reader["EndDate"] != DBNull.Value) ? Convert.ToDateTime(reader["EndDate"]).ToShortDateString() : ""
                };
                var endReason = new Field
                {
                    N = "EndReasonr",
                    NT = "Основание закрытия ЛС",
                    IS = 0,
                    P = 8,
                    T = "TextType",
                    L = 100,
                    V = (reader["EndReason"] != DBNull.Value) ? Convert.ToString(reader["EndReason"]).Trim() : ""
                };

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {
                            placeIDSystem,
                            individalIDSystem,
                            regAccount,
                            accountIDSys,
                            beginDate,
                            beginReason,
                            endDate,
                            endReason
                        }
                };
                Data.Add(fields);
                #endregion
                }
                }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("AccountRegister.Start(pref): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
            string columnNames =
                " id SERIAL, " +
                " PlaceIDSystem	 BIGINT , " +
                " nzp_kvar	 INTEGER , " +
                " IndividalIDSystem	 BIGINT , " +
                " RegAccount	 CHAR ( 25), " +
                " AccountIDSys	 BIGINT , " +
                " BeginDate	 DATE 	, " +
                " BeginReason	 CHAR ( 100), " +
                " EndDate	 DATE 	, " +
                " EndReason	 CHAR ( 100) ";

            ExecSQL(" DROP TABLE " + Name, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", Name, columnNames));
        }

        public void FillAccountsRegisterInfo(string pref)
        {
            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " WHERE kv.nzp_area in (" + area + " )";
            }

            string unlDate = " CAST( '" + Year + "-" + Month + "-01' as DATE) ";
            string sql = " INSERT INTO accountregister " +
                         " ( " +
                         " PlaceIDSystem, " +
                         " AccountIDSys, " +
                         " nzp_kvar " +
                         " ) " +
                         " SELECT  " +
                         " kv.num_ls AS PlaceIDSystem, " +
                         " kv.num_ls AS AccountIDSys, " +
                         " kv.nzp_kvar " +
                         " FROM  " +
                         " " + pref + DBManager.sDataAliasRest + "kvar kv " + whereArea;
            ExecSQL(sql);

            sql = " UPDATE AccountRegister " +
                  " SET IndividalIDSystem =  " +
                  " ( " +
                  " 	SELECT " +
                  " 		nzp_gil " +
                  " 	FROM " +
                  " 		" + pref + DBManager.sDataAliasRest + "kart kart	 " +
                  " 	INNER JOIN " + pref + DBManager.sDataAliasRest + "s_rod r ON r.nzp_rod = kart.nzp_rod " +
                  " 	INNER JOIN " + pref + DBManager.sDataAliasRest + "kvar kv ON ( " +
                  " 		kv.nzp_kvar = kart.nzp_kvar " +
                  " 		AND kv.num_ls = AccountRegister.PlaceIDSystem " +
                  " 	) " +
                  " 	WHERE " +
                  " 		LOWER (rod) LIKE 'наним%' " +
                  " 	OR LOWER (rod) LIKE 'собств%' " +
                  " 	OR LOWER (rod) LIKE 'владел%' " +
                  " ); ";
            ExecSQL(sql);

            sql = " UPDATE AccountRegister " +
                  " SET BeginDate = ( " +
                  " 	SELECT " +
                  " 		dat_s " +
                  " 	FROM " +
                  " 		" + pref + DBManager.sDataAliasRest + "prm_3 P " +
                  " 	WHERE " +
                  " 		P .nzp_prm = 51 " +
                  " 	AND val_prm = '1' " +
                  " 	AND P .is_actual <> 100 " +
                  " 	AND P .nzp = AccountRegister.nzp_kvar " +
                  " 	AND dat_s < " + unlDate + " " +
                  " 	AND dat_po > " + unlDate + " " +
                  " ) " +
                  " WHERE EXISTS ( " +
                  " 	SELECT " +
                  " 		1 " +
                  " 	FROM " +
                  " 		" + pref + DBManager.sDataAliasRest + "prm_3 P " +
                  " 	WHERE " +
                  " 		P .nzp_prm = 51 " +
                  " 	AND val_prm = '1' " +
                  " 	AND P .is_actual <> 100 " +
                  " 	AND P .nzp = AccountRegister.nzp_kvar " +
                  " 	AND dat_s < " + unlDate + " " +
                  " 	AND dat_po > " + unlDate + " " +
                  " ); ";
            ExecSQL(sql);

            sql = " UPDATE AccountRegister " +
                  " SET BeginDate = ( " +
                  " 	SELECT " +
                  " 		dat_s " +
                  " 	FROM " +
                  " 		" + pref + DBManager.sDataAliasRest + "prm_3 P " +
                  " 	WHERE " +
                  " 		P .nzp_prm = 51 " +
                  " 	AND val_prm = '2' " +
                  " 	AND P .is_actual <> 100 " +
                  " 	AND P .nzp = AccountRegister.nzp_kvar " +
                  " 	AND dat_s < " + unlDate + " " +
                  " 	AND dat_po > " + unlDate + " " +
                  " ) " +
                  " WHERE EXISTS ( " +
                  " 	SELECT " +
                  " 		1 " +
                  " 	FROM " +
                  " 		" + pref + DBManager.sDataAliasRest + "prm_3 P " +
                  " 	WHERE " +
                  " 		P .nzp_prm = 51 " +
                  " 	AND val_prm = '2' " +
                  " 	AND P .is_actual <> 100 " +
                  " 	AND P .nzp = AccountRegister.nzp_kvar " +
                  " 	AND dat_s < " + unlDate + " " +
                  " 	AND dat_po > " + unlDate + " " +
                  " ); ";
            ExecSQL(sql);

        }
    }
}
