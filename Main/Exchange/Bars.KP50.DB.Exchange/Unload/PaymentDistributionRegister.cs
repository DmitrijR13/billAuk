using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.SqlServer.Server;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class PaymentDistributionRegister : BaseUnload20
    {
        /// <summary> Префикс локального бакнка (из которого осуществляется выгрузка)</summary>
        private string _pref = String.Empty;

        /// <summary>Номер месяца (за который осуществляется выгрузка)</summary>
        public int _unloadMonthNumber = DateTime.Now.Month;

        /// <summary>Год (за который осуществляется выгрузка)</summary>
        public int _unloadYear = DateTime.Now.Year;

        string tblName = " PaymentDistributionRegister ";

        public override string Name
        {
            get { return "PaymentDistributionRegister"; }
        }

        public override string NameText
        {
            get { return "Распределение оплат"; }
        }

        public override int Code
        {
            get { return 15; }
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

            string sql = " SELECT * FROM "+ tblName;

            

            IDataReader reader;

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                #region заполнение полей секции
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
                var accountIdSys = new Field
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
                var paymentIdSystem = new Field
                {
                    N = "PaymentIDSystem",
                    NT = "Уникальный код оплаты в системе отправителя",
                    IS = 1,
                    P = 3,
                    T = "TextType",
                    L = 30,
                    V =
                        (reader["PaymentIDSystem"] != DBNull.Value)
                            ? Convert.ToString(reader["PaymentIDSystem"]).Trim()
                            : ""
                };
                var unitId = new Field
                {
                    N = "UnitID",
                    NT = "Код услуги",
                    IS = 1,
                    P = 4,
                    T = "IntType",
                    L = 3,
                    V =
                        (reader["UnitID"] != DBNull.Value)
                            ? Convert.ToString(reader["UnitID"]).Trim()
                            : ""
                };
                var contractId = new Field
                {
                    N = "ContractID",
                    NT = "Код договора в системе отправителя",
                    IS = 1,
                    P = 5,
                    T = "IntType",
                    L = 9,
                    V =
                        (reader["ContractID"] != DBNull.Value)
                            ? Convert.ToString(reader["ContractID"]).Trim()
                            : ""
                };
                var distributionAmount = new Field
                {
                    N = "DistributionAmount",
                    NT = "Сумма распределения",
                    IS = 1,
                    P = 6,
                    T = "DecimalType",
                    L = 9,
                    V =
                        (reader["DistributionAmount"] != DBNull.Value)
                            ? Convert.ToString(reader["DistributionAmount"]).Trim()
                            : ""
                };
                var fields = new FieldsUnload
                {
                    F = new List<Field>
                    {
                      regAccount,
                      accountIdSys,
                      paymentIdSystem,
                      unitId,
                      contractId,
                      distributionAmount
                    }
                };


                #endregion

                Data.Add(fields);
            }

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("PaymentDistributionRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally 
            {
                DropTempTable();
            CloseConnection();
        }
        }


        private void SelectFromDB()
        {
            if (Points.Pref == null) Points.Pref = "nftul";
            try
            {
                string pack = Points.Pref + "_fin_" + _unloadYear.ToString().Substring(2, 2) + DBManager.tableDelimiter +
                              "pack ";
                string pack_ls = Points.Pref + "_fin_" + _unloadYear.ToString().Substring(2, 2) +
                                 DBManager.tableDelimiter +
                                 "pack_ls ";
                string from_supplier = _pref + "_charge_" + _unloadYear.ToString().Substring(2, 2) +
                                       DBManager.tableDelimiter +
                                       "from_supplier ";
                string to_supplier = _pref + "_charge_" + _unloadYear.ToString().Substring(2, 2) +
                                     DBManager.tableDelimiter +
                                     "to_supplier" + _unloadMonthNumber.ToString("00");


                string dat_s = " CAST( '" + _unloadYear + "-" + _unloadMonthNumber.ToString("00") + "-01' as DATE) ";
                int dayInMonth = DateTime.DaysInMonth(_unloadYear, _unloadMonthNumber);
                string dat_po = " CAST( '" + _unloadYear + "-" + _unloadMonthNumber.ToString("00") + "-" + dayInMonth +
                                "' as DATE) ";

                string area = GetNzpArea(ListNzpArea);
                string whereArea = "";
                if (area != String.Empty)
                {
                    whereArea = " and k.nzp_area in (" + area + " )";
                }

                string sql =
                    " INSERT INTO " + tblName +
                    "(" +
                    "   AccountIDSys, " +
                    "   PaymentIDSystem, " +
                    "   UnitID, " +
                    "   ContractID, " +
                    "   DistributionAmount" +
                    ")" +
                    " SELECT" +
                    " pl.num_ls, " +
                    " pl.nzp_pack_ls," +
                    " f.nzp_serv," +
                    " p.nzp_supp," +
                    " f.sum_prih " +
                    " FROM " + pack_ls + "pl left outer join " + _pref + DBManager.sDataAliasRest + "kvar k on  pl.num_ls = k.num_ls ," +
                    pack + "p, " +
                    from_supplier + " f" +
                    " WHERE pl.nzp_pack = p.nzp_pack " +
                    " AND pl.kod_sum in (40, 50,49)" +//???
                    " AND f.nzp_pack_ls = pl.nzp_pack_ls" +
                    " AND pl.dat_uchet >= " + dat_s +
                    " AND pl.dat_uchet <= " + dat_po +
                    " AND f.nzp_serv < 1007568 " + whereArea;
                //ограничить дату пачки
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tblName +
                    "(" +
                    "   AccountIDSys, " +
                    "   PaymentIDSystem, " +
                    "   UnitID, " +
                    "   ContractID, " +
                    "   DistributionAmount" +
                    ")" +
                    " SELECT" +
                    " pl.num_ls, " +
                    " pl.nzp_pack_ls," +
                    " f.nzp_serv," +
                    " p.nzp_supp," +
                    " f.sum_prih " +
                    " FROM " + pack_ls + "pl left outer join " + _pref + DBManager.sDataAliasRest + "kvar k on  pl.num_ls = k.num_ls ," +
                    pack + "p, " +
                    to_supplier + " f" +
                    " WHERE pl.nzp_pack = p.nzp_pack " +
                    " AND pl.kod_sum in (33)" +
                    " AND f.nzp_pack_ls = pl.nzp_pack_ls" +
                    " AND pl.dat_uchet >= " + dat_s +
                    " AND pl.dat_uchet <= " + dat_po + whereArea;
                //ограничить дату пачки
                ExecSQL(sql);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("PaymentDistributionRegister.Start(pref): Ошибка выборки данных из БД.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

        public override void StartSelect()
        {
        }

        public override void CreateTempTable()
        {
            string columnNames =
                " id SERIAL, " +
                " RegAccount	 CHAR (25), " +
                " AccountIDSys	 BIGINT , " +
                " PaymentIDSystem	 CHAR (30), " +
                " UnitID	 INTEGER , " +
                " ContractID	 INTEGER , " +
                " DistributionAmount	" + DBManager.sDecimalType + " (14,2) ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));
        }

        public override void DropTempTable()
        {
        }
    }
}
