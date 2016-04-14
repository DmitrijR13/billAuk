using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class AccountPaymentRegister : BaseUnload20
    {
        public override string Name
        {
            get { return "AccountPaymentRegister"; }
        }

        public override string NameText
        {
            get { return "Оплаты проведенные по ЛС"; }
        }

        public override int Code
        {
            get { return 14; }
        }

        public override List<FieldsUnload> Data { get; set; }

        public override void Start()
        {

        }

        public override void Start(string pref)
        {
            Data = new List<FieldsUnload>();
            int _Year = Year == 0 ? DateTime.Now.Year : Year;
            OpenConnection();
            CreateTempTable();
            try
            {

            InsertData(pref);  
            #region заполнение списка полей секции
            string sql = " select  RegAccount, AccountIDSys, PaymentType,  PaymentDate	, RegistrDate,  AdjustmentDate	, Amount,  SourcePayment, AmountDate,  BatchNum, PaymentIDSystem,  kod_sum, pkod, dat_month, id_bill, sum_ls " +
                         " from AccountPaymentRegister ";

            

            IDataReader reader;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["pkod"] != DBNull.Value)
                {
                    #region заполнение полей

                    var regAccount = new Field
                    {
                        N = "RegAccount",
                        NT = "РИК лицевого счета",
                        IS = 0,
                        P = 1,
                        T = "TextType",
                        L = 25,
                        V =
                            (reader["RegAccount"] != DBNull.Value) ? Convert.ToString(reader["RegAccount"]).Trim() : ""
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
                    var paymentType = new Field
                    {
                        N = "PaymentType",
                        NT = "Тип операции",
                        IS = 1,
                        P = 3,
                        T = "IntType",
                        L = 1,
                        V =
                            (reader["PaymentType"] != DBNull.Value)
                                ? Convert.ToString(reader["PaymentType"]).Trim()
                                : ""
                    };
                    string str = (reader["kod_sum"] != DBNull.Value ? Convert.ToString(reader["kod_sum"]).Trim() : "") +
                                 (reader["pkod"] != DBNull.Value ? Convert.ToString(reader["pkod"]).Trim() : "") +
                                 (reader["dat_month"] != DBNull.Value
                                     ? Convert.ToDateTime(reader["dat_month"]).ToString("MM")
                                     : "")
                                 + _Year +
                                 (reader["id_bill"] != DBNull.Value ? Convert.ToString(reader["id_bill"]).Trim() : "").PadLeft(
                                     4, '0');

                    string s = (reader["sum_ls"] != DBNull.Value ? Convert.ToString(reader["sum_ls"]).Trim() : "");
                    if (s != "")
                    {
                        s = s.Remove(s.Length - 3, 1);
                        s = s.Trim('-');
                    }
                    str += s;
                    str += BarcodeCrc(str);

                    var paymentNum = new Field
                    {
                        N = "PaymentNum",
                        NT = "Номер платежного документа",
                        IS = 1,
                        P = 4,
                        T = "TextType",
                        L = 80,
                        V = str
                    };
                    var paymentDate = new Field
                    {
                        N = "PaymentDate",
                        NT = "Дата оплаты",
                        IS = 1,
                        P = 5,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["PaymentDate"] != DBNull.Value)
                                ? Convert.ToDateTime(reader["PaymentDate"]).ToString("dd.MM.yyyy hh:mm:ss")
                                : ""
                    };
                    var registrDate = new Field
                    {
                        N = "RegistrDate",
                        NT = "Дата учета",
                        IS = 0,
                        P = 6,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["RegistrDate"] != DBNull.Value)
                                ? Convert.ToDateTime(reader["RegistrDate"]).ToString("dd.MM.yyyy hh:mm:ss")
                                : ""
                    };
                    var adjustmentDate = new Field
                    {
                        N = "AdjustmentDate",
                        NT = "Дата корректировки",
                        IS = 0,
                        P = 7,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["AdjustmentDate"] != DBNull.Value)
                                ? Convert.ToDateTime(reader["AdjustmentDate"]).ToString("dd.MM.yyyy hh:mm:ss")
                                : ""
                    };
                    var amount = new Field
                    {
                        N = "Amount",
                        NT = "Сумма оплаты",
                        IS = 1,
                        P = 8,
                        T = "DecimalType",
                        L = 0,
                        V = (reader["Amount"] != DBNull.Value) ? Convert.ToString(reader["Amount"]).Trim() : ""
                    };
                    var sourcePayment = new Field
                    {
                        N = "SourcePayment",
                        NT = "Источник оплаты",
                        IS = 0,
                        P = 9,
                        T = "TextType",
                        L = 60,
                        V =
                            (reader["SourcePayment"] != DBNull.Value)
                                ? Convert.ToString(reader["SourcePayment"]).Trim()
                                : ""
                    };
                    var amountDate = new Field
                    {
                        N = "AmountDate",
                        NT = "Месяц и год, за который произведена оплата",
                        IS = 0,
                        P = 10,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["AmountDate"] != DBNull.Value)
                                ? Convert.ToDateTime(reader["AmountDate"]).ToString()
                                : ""
                    };
                    var batchNum = new Field
                    {
                        N = "BatchNum",
                        NT = "Уникальный номер пачки",
                        IS = 1,
                        P = 11,
                        T = "TextType",
                        L = 30,
                        V = (reader["BatchNum"] != DBNull.Value) ? Convert.ToString(reader["BatchNum"]).Trim() : ""
                    };
                    var paymentIDSystem = new Field
                    {
                        N = "PaymentIDSystem",
                        NT = "Уникальный код оплаты в системе отправителя",
                        IS = 1,
                        P = 12,
                        T = "TextType",
                        L = 30,
                        V = _Year + batchNum.V
                    };

                    #endregion

                    var fields = new FieldsUnload
                    {
                        F =
                            new List<Field>
                            {
                                regAccount,
                                accountIDSys,
                                paymentType,
                                paymentNum,
                                paymentDate,
                                registrDate,
                                adjustmentDate,
                                amount,
                                sourcePayment,
                                amountDate,
                                batchNum,
                                paymentIDSystem
                            }
                    };
                    Data.Add(fields);
                }
                else
                {
                    AddComment("У ЛС " + Convert.ToString(reader["AccountIDSys"]) + " отсутствует платежный код.");
                }
            }
            #endregion

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("AccountPaymentRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
                " RegAccount	 CHAR (25), " +
                " AccountIDSys	 BIGINT , " +
                " PaymentType	 INTEGER , " +
                //" PaymentNum	 CHAR (80), " +
                " PaymentDate	 DATE 	, " +
                " RegistrDate	 DATE 	, " +
                " AdjustmentDate	 DATE 	, " +
                " Amount	" + DBManager.sDecimalType + " (14,2) , " +
                " SourcePayment	 CHAR (60), " +
                " AmountDate	 DATE 	, " +
                " BatchNum	 CHAR (30), " +
                " PaymentIDSystem	 CHAR (30), "+
                " kod_sum INTEGER, pkod BIGINT, dat_month DATE, id_bill INTEGER, sum_ls " + DBManager.sDecimalType + "(14,2) ";

            ExecSQL(" DROP TABLE " + Name, false);
            string s = String.Format(" CREATE TEMP TABLE {0} ({1}) ", Name, columnNames);
            ExecSQL(s);
        }

        private void InsertData(string pref)
        {
            int _Year = Year == 0 ? DateTime.Now.Year : Year;
            int _Month = Month == 0 ? DateTime.Now.Month : Month;
            string unlDate = " CAST( '" + _Year + "-" + _Month+ "-01' as DATE) ";
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
                // " RegAccount,	  " +
                " AccountIDSys	 , " +
                " PaymentType	 , " +
                //" PaymentNum	, " +
                " PaymentDate	 ," +
                " RegistrDate	, " +
                // " AdjustmentDate , " +
                " Amount , " +
                " SourcePayment	, " +
                " AmountDate	, " +
                " BatchNum , " +
               // " PaymentIDSystem	 " +
                " kod_sum, pkod, dat_month, id_bill, sum_ls  )    " +
                " SELECT                                                " +
                //" 	,                                          " +
                " 	pl.num_ls,                                          " +
                " 	(case when kod_sum= 40 or kod_sum=50 or kod_sum=49 then 2 else 1 end),   " +//???
                //" 	kod_sum" + DBManager.sConvToVarChar + " || pl.pkod" + DBManager.sConvToVarChar + " || " + DBManager.sMonthFromDate + "pl.dat_month) ||" + _Year + "|| LPAD(" + DBManager.sNvlWord + "(id_bill,0)" + DBManager.sConvToVarChar + ",4,'0') || g_sum_ls*100 ," +
                " 	pl.dat_vvod,    " +
                " 	pl.dat_uchet,    " +
                // " 	,    " +
                " 	g_sum_ls,    " +
                " 	payer,    " +
                unlDate + "," +
                " 	nzp_pack_ls," +
               // _Year + " || nzp_pack_ls" + DBManager.sConvToVarChar +
                 " kod_sum, pl.pkod, pl.dat_month, id_bill,sum_ls   " +
                " FROM "
                 + Points.Pref + "_fin_" + _Year % 2000 + DBManager.tableDelimiter + "pack p,"
                 + Points.Pref + "_fin_" + _Year % 2000 + DBManager.tableDelimiter + "pack_ls pl,"
                 + Points.Pref + DBManager.sKernelAliasRest + "s_bank sb,"
                 + Points.Pref + DBManager.sKernelAliasRest + "s_payer sp,"
                 + pref + DBManager.sDataAliasRest + "kvar k"
                 + " WHERE pl.nzp_pack=p.nzp_pack AND p.nzp_bank=sb.nzp_bank AND sp.nzp_payer=sb.nzp_payer and k.num_ls=pl.num_ls" + whereArea;
            ExecSQL(sql);
        }
        
  
        /// <summary>
        /// Подсчет контрольной суммы в штрих-коде
        /// </summary>
        /// <param name="acode">Штрих-код</param>
        /// <returns>Контрольная цифра</returns>
        public string BarcodeCrc(string acode)
        {
            int sum = 0;


            for (int i = 0; i < acode.Length; i++)
            {
                if (i != 30)
                {
                    if ((i % 2) == 1)
                    {
                        sum = sum + Convert.ToInt16(acode.Substring(i, 1));
                    }
                    else sum = sum + 3 * Convert.ToInt16(acode.Substring(i, 1));
                }
            }

            String s = ((10 - sum % 10) % 10).ToString(CultureInfo.InvariantCulture);

            return s.Substring(0, 1);
        }
    }
}
