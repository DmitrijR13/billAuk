using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class ContractRegister : BaseUnload20
    {
        public override string Name
        {
            get { return "ContractRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр договоров"; }
        }

        public override int Code
        {
            get { return 7; }
        }

        public override List<FieldsUnload> Data { get; set; }

        public override void Start()
        {
            Data = new List<FieldsUnload>();
            OpenConnection();

            CreateTempTable();

            
            DateTime curDate = new DateTime(Year, Month, 1);
            string prefData = Points.Pref + DBManager.sDataAliasRest,
                      prefKernel = Points.Pref + DBManager.sKernelAliasRest;
            

            try
            {
                string sql = " insert into ContractRegister " +
                             "(ContractSenderID, PaymentAgentID, PrincipalID, ProviderID, ContractName, ContractNumber, ContractDate, Comments, CurrentAccount, ContractStartDate,ContractEndDate) " +
                             " SELECT s.nzp_supp as ContractSenderID, s.nzp_payer_agent as PaymentAgentID,s.nzp_payer_princip as  PrincipalID, " +
                             " s.nzp_payer_supp as ProviderID, TRIM(" + DBManager.sNvlWord +
                             "(s.name_supp,'')) as ContractName, " +
                             " d.num_dog as ContractNumber,d.dat_dog as ContractDate, " +
                             "  TRIM(" + DBManager.sNvlWord + "(d.target,'')) as Comments, " +
                             " b.rcount as CurrentAccount, " +
                             " d.dat_s as ContractStartDate, d.dat_po as ContractEndDate " +
                             " FROM " + prefKernel + "supplier s " +
                             " LEFT OUTER JOIN " + prefData + "fn_dogovor d ON s.nzp_supp=d.nzp_supp" +
                             " AND  " + Utils.EStrNull(curDate.ToShortDateString()) + " BETWEEN d.dat_s and d.dat_po" +
                             " LEFT OUTER JOIN " + prefData + "fn_bank b ON d.nzp_fb=b.nzp_fb " +
                             " ORDER BY s.nzp_supp ";

                ExecSQL(sql, false);

                sql = " select * from ContractRegister ";

                

                IDataReader reader;

                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    #region заполнение полей секции

                    var contractSenderId = new Field
                    {
                        N = "ContractSenderID",
                        NT = "Уникальный код договора в системе отправителя",
                        IS = 1,
                        P = 1,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["ContractSenderID"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractSenderID"]).Trim()
                                : ""
                    };
                    var paymentAgentId = new Field
                    {
                        N = "PaymentAgentIDSys",
                        NT = "Код агента получателя платежей",
                        IS = 1,
                        P = 2,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["PaymentAgentID"] != DBNull.Value)
                                ? Convert.ToString(reader["PaymentAgentID"]).Trim()
                                : ""
                    };
                    var principalId = new Field
                    {
                        N = "PrincipalIDSys",
                        NT = "Код принципала",
                        IS = 1,
                        P = 3,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["PrincipalID"] != DBNull.Value)
                                ? Convert.ToString(reader["PrincipalID"]).Trim()
                                : ""
                    };
                    var providerId = new Field
                    {
                        N = "ProviderIDSys",
                        NT = "Код поставщика ЖКУ",
                        IS = 1,
                        P = 4,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["ProviderID"] != DBNull.Value)
                                ? Convert.ToString(reader["ProviderID"]).Trim()
                                : ""
                    };
                    var contractName = new Field
                    {
                        N = "ContractName",
                        NT = "Наименование договора по ЖКУ",
                        IS = 0,
                        P = 5,
                        T = "TextType",
                        L = 60,
                        V =
                            (reader["ContractName"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractName"]).Trim()
                                : ""
                    };
                    var contractNumber = new Field
                    {
                        N = "ContractNumber",
                        NT = "Номер договора",
                        IS = 1,
                        P = 6,
                        T = "TextType",
                        L = 20,
                        V =
                            (reader["ContractNumber"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractNumber"]).Trim()
                                : ""
                    };
                    var contractDate = new Field
                    {
                        N = "ContractDate",
                        NT = "Дата договора",
                        IS = 1,
                        P = 7,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["ContractDate"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractDate"]).Trim()
                                : ""
                    };
                    var comments = new Field
                    {
                        N = "Comments",
                        NT = "Комментарий",
                        IS = 0,
                        P = 8,
                        T = "TextType",
                        L = 200,
                        V =
                            (reader["Comments"] != DBNull.Value)
                                ? Convert.ToString(reader["Comments"]).Trim()
                                : ""
                    };
                    var currentAccount = new Field
                    {
                        N = "CurrentAccount",
                        NT = "Расчетный счет",
                        IS = 1,
                        P = 9,
                        T = "TextType",
                        L = 20,
                        V =
                            (reader["CurrentAccount"] != DBNull.Value)
                                ? Convert.ToString(reader["CurrentAccount"]).Trim()
                                : ""
                    };
                    var contractStartDate = new Field
                    {
                        N = "ContractStartDate",
                        NT = "Дата действия с",
                        IS = 1,
                        P = 10,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["ContractStartDate"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractStartDate"]).Trim()
                                : ""
                    };
                    var ContractEndDate = new Field
                    {
                        N = "ContractEndDate",
                        NT = "Дата действия по",
                        IS = 0,
                        P = 11,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["ContractEndDate"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractEndDate"]).Trim()
                                : ""
                    };

                    var fields = new FieldsUnload
                    {
                        F = new List<Field>
                        {
                            contractSenderId,
                            paymentAgentId,
                            principalId,
                            providerId,
                            contractName,
                            contractNumber,
                            contractDate,
                            comments,
                            currentAccount,
                            contractStartDate,
                            ContractEndDate
                        }
                    };

                    #endregion

                    Data.Add(fields);
                }
            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции - " + NameText);
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
            Data = new List<FieldsUnload>();
            OpenConnection();

            CreateTempTable();

            //убрать заглушки-------------------------------------------
            DateTime curDate = new DateTime(Year, Month, 1); 
            string prefData = Points.Pref + DBManager.sDataAliasRest, //"nftul" + DBManager.sDataAliasRest,
                      prefKernel = Points.Pref + DBManager.sKernelAliasRest;// "nftul" + DBManager.sKernelAliasRest;
            //----------------------------------------------------------

            try
            {
                
                string area = GetNzpArea(ListNzpArea);
                string whereArea = "";
                if (area != String.Empty)
                {
                    whereArea = " and sa.nzp_area in (" + area + " )";
                }

                string sql = " insert into ContractRegister " +
                             "(ContractSenderID, PaymentAgentID, PrincipalID, ProviderID, ContractName, ContractNumber, ContractDate, Comments, CurrentAccount, ContractStartDate,ContractEndDate) " +
                             " SELECT s.nzp_supp as ContractSenderID, s.nzp_payer_agent as PaymentAgentID,s.nzp_payer_princip as  PrincipalID, " +
                             " s.nzp_payer_supp as ProviderID, TRIM(" + DBManager.sNvlWord +
                             "(s.name_supp,'')) as ContractName, " +
                             " d.num_dog as ContractNumber,d.dat_dog as ContractDate, " +
                             "  TRIM(" + DBManager.sNvlWord + "(d.target,'')) as Comments, " +
                             " b.rcount as CurrentAccount, " +
                             " d.dat_s as ContractStartDate, d.dat_po as ContractEndDate " +
                             " FROM " + prefKernel + "supplier s " +
                             " LEFT OUTER JOIN " + pref + DBManager.sDataAliasRest + "s_area sa ON s.nzp_supp = sa.nzp_supp " +
                             " LEFT OUTER JOIN " + prefData + "fn_dogovor d ON s.nzp_supp=d.nzp_supp " +
                             " LEFT OUTER JOIN " + prefData + "fn_bank b ON d.nzp_fb=b.nzp_fb " +
                             " WHERE " + Utils.EStrNull(curDate.ToShortDateString()) + " BETWEEN d.dat_s and d.dat_po " + whereArea +
                             " ORDER BY s.nzp_supp ";

                ExecSQL(sql, false);

                sql = " select * from ContractRegister ";

                

                IDataReader reader;

                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    #region заполнение полей секции

                    var contractSenderId = new Field
                    {
                        N = "ContractSenderID",
                        NT = "Уникальный код договора в системе отправителя",
                        IS = 1,
                        P = 1,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["ContractSenderID"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractSenderID"]).Trim()
                                : ""
                    };
                    var paymentAgentId = new Field
                    {
                        N = "PaymentAgentIDSys",
                        NT = "Код агента получателя платежей",
                        IS = 1,
                        P = 2,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["PaymentAgentID"] != DBNull.Value)
                                ? Convert.ToString(reader["PaymentAgentID"]).Trim()
                                : ""
                    };
                    var principalId = new Field
                    {
                        N = "PrincipalIDSys",
                        NT = "Код принципала",
                        IS = 1,
                        P = 3,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["PrincipalID"] != DBNull.Value)
                                ? Convert.ToString(reader["PrincipalID"]).Trim()
                                : ""
                    };
                    var providerId = new Field
                    {
                        N = "ProviderIDSys",
                        NT = "Код поставщика ЖКУ",
                        IS = 1,
                        P = 4,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["ProviderID"] != DBNull.Value)
                                ? Convert.ToString(reader["ProviderID"]).Trim()
                                : ""
                    };
                    var contractName = new Field
                    {
                        N = "ContractName",
                        NT = "Наименование договора по ЖКУ",
                        IS = 0,
                        P = 5,
                        T = "TextType",
                        L = 100,
                        V =
                            (reader["ContractName"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractName"]).Trim()
                                : ""
                    };
                    var contractNumber = new Field
                    {
                        N = "ContractNumber",
                        NT = "Номер договора",
                        IS = 1,
                        P = 6,
                        T = "TextType",
                        L = 20,
                        V =
                            (reader["ContractNumber"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractNumber"]).Trim()
                                : ""
                    };
                    var contractDate = new Field
                    {
                        N = "ContractDate",
                        NT = "Дата договора",
                        IS = 1,
                        P = 7,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["ContractDate"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractDate"]).Trim()
                                : ""
                    };
                    var comments = new Field
                    {
                        N = "Comments",
                        NT = "Комментарий",
                        IS = 0,
                        P = 8,
                        T = "TextType",
                        L = 200,
                        V =
                            (reader["Comments"] != DBNull.Value)
                                ? Convert.ToString(reader["Comments"]).Trim()
                                : ""
                    };
                    var currentAccount = new Field
                    {
                        N = "CurrentAccount",
                        NT = "Расчетный счет",
                        IS = 1,
                        P = 9,
                        T = "TextType",
                        L = 20,
                        V =
                            (reader["CurrentAccount"] != DBNull.Value)
                                ? Convert.ToString(reader["CurrentAccount"]).Trim()
                                : ""
                    };
                    var contractStartDate = new Field
                    {
                        N = "ContractStartDate",
                        NT = "Дата действия с",
                        IS = 1,
                        P = 10,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["ContractStartDate"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractStartDate"]).Trim()
                                : ""
                    };
                    var ContractEndDate = new Field
                    {
                        N = "ContractEndDate",
                        NT = "Дата действия по",
                        IS = 0,
                        P = 11,
                        T = "DateType",
                        L = 0,
                        V =
                            (reader["ContractEndDate"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractEndDate"]).Trim()
                                : ""
                    };

                    var fields = new FieldsUnload
                    {
                        F = new List<Field>
                        {
                            contractSenderId,
                            paymentAgentId,
                            principalId,
                            providerId,
                            contractName,
                            contractNumber,
                            contractDate,
                            comments,
                            currentAccount,
                            contractStartDate,
                            ContractEndDate
                        }
                    };

                    #endregion

                    Data.Add(fields);
                }
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

        public override void StartSelect()
        {
        }

        public override void CreateTempTable()
        {
            string tblName = " ContractRegister ";
            string columnNames =
                " id SERIAL, " +
                " ContractSenderID	 INTEGER , " +
                " PaymentAgentID	 INTEGER , " +
                " PrincipalID	 INTEGER , " +
                " ProviderID	 INTEGER , " +
                " ContractName	 CHAR (100), " +
                " ContractNumber	 CHAR ( 20), " +
                " ContractDate	 DATE 	, " +
                " Comments	 CHAR ( 200), " +
                " CurrentAccount	 CHAR ( 20), " +
                " ContractStartDate	 DATE 	, " +
                " ContractEndDate	 DATE 	 ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));
        }

        public override void DropTempTable()
        {
        }
    }
}
