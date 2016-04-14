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
    public class AccountInformRegister : BaseUnload20
    {

        public override string Name
        {
            get { return "AccountInformRegister"; }
        }

        public override string NameText
        {
            get { return "Реестр информации по оказанным услугам в лицевых счетах"; }
        }

        public override int Code
        {
            get { return 8; }
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

            //заглушки --------------------------------------
            DateTime curDateTime = new DateTime(Year, Month, 1);
            string prefData = pref + DBManager.sDataAliasRest,
                     prefKernel = pref + DBManager.sKernelAliasRest;
            //-------------------------------------------------
            DateTime DateTo = new DateTime(curDateTime.Year, curDateTime.Month, DateTime.DaysInMonth(curDateTime.Year, curDateTime.Month)).AddDays(1);
            string chargeYY = pref + "_charge_" + (curDateTime.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + curDateTime.Month.ToString("00");
            string calcGkuYY = pref + "_charge_" + (curDateTime.Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + curDateTime.Month.ToString("00");
            string chargeLnkYY = pref + "_charge_" + (curDateTime.Year - 2000).ToString("00") + DBManager.tableDelimiter + "lnk_charge_" + curDateTime.Month.ToString("00");

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " and k.nzp_area in (" + area + " )";
            }
            
            try
            {

                string sql = " insert into AccountInformRegister " +
                             "(PlaceIDSystem, AccountIDSys, ContractSenderID, UnitID, BalanceIn, EconomTarif, RegulableTarif, TypePaymentDA, " +
                             "ChargeSum, ReChargeSum, PaySum, InactiveUnitFlag, BalanceOut, MethodNumber, PaymentCode,PaymentDocUnitNumber,ChangeBalanceSum,ReDeliverySum,num_ls,nzp_kvar) " +
                             " SELECT " +
                             "k.num_ls AS PlaceIDSystem, " +
                             "k.num_ls AS AccountIDSys, " +
                             " c.nzp_supp AS ContractSenderID, " +
                             " c.nzp_serv AS UnitID, " +
                             " c.sum_insaldo AS BalanceIn, " +
                             " c.tarif AS EconomTarif, " +
                             " c.tarif_f AS RegulableTarif, " +
                             " c.is_device AS TypePaymentDA, " +
                             " c.rsum_tarif as ChargeSum, " +
                             " c.reval AS ReChargeSum, " +
                             " c.sum_money AS PaySum, " +
                             " c.isdel AS InactiveUnitFlag, " +
                             " c.sum_outsaldo AS BalanceOut, " +
                             " c.nzp_frm AS MethodNumber," +
                             " k.pkod AS PaymentCode, " +
                             " c.order_print AS PaymentDocUnitNumber," +
                             " c.real_charge AS ChangeBalanceSum, " +
                             " c.sum_nedop AS ReDeliverySum, " +
                             " k.num_ls, " +
                             " k.nzp_kvar " +
                             " FROM " + prefData + "kvar k INNER JOIN " + chargeYY + " c ON c.nzp_kvar = k.nzp_kvar " +
                             " WHERE c.dat_charge is null and c.nzp_serv>1 and c.nzp_serv < 1007568 " + whereArea;
                ExecSQL(sql, false);

                ExecSQL("CREATE INDEX ix_1 ON AccountInformRegister(num_ls,UnitID,ContractSenderID) ", false);
                ExecSQL(DBManager.sUpdStat + " AccountInformRegister", false);

                sql = " UPDATE AccountInformRegister SET UnitCode = " +
                         " (SELECT nzp_measure " +
                          " FROM " + prefKernel + "services s " +
                          " WHERE s.nzp_serv = AccountInformRegister.UnitID ) ";
                ExecSQL(sql);

                sql = " UPDATE AccountInformRegister SET UnitCode = " +
                      " (SELECT nzp_measure " +
                      " FROM " + prefKernel + "formuls f " +
                      " WHERE f.nzp_frm = AccountInformRegister.MethodNumber) WHERE MethodNumber is not null";
                ExecSQL(sql);

                sql = " UPDATE AccountInformRegister SET FactVolume = " +
                       " (SELECT sum(c.rashod) " +
                       " FROM " + calcGkuYY + " c WHERE AccountInformRegister.nzp_kvar = c.nzp_kvar " +
                                                " AND c.nzp_serv = AccountInformRegister.UnitID " +
                                                " AND c.nzp_supp = AccountInformRegister.ContractSenderID and c.dat_charge is null) ";
                ExecSQL(sql);

                sql = " UPDATE AccountInformRegister SET NormVolume = " +
                      " (SELECT sum(c.rashod_norm) " +
                      " FROM " + calcGkuYY + " c WHERE AccountInformRegister.nzp_kvar = c.nzp_kvar " +
                                               " AND c.nzp_serv = AccountInformRegister.UnitID " +
                                               " AND c.nzp_supp = AccountInformRegister.ContractSenderID and c.dat_charge is null) ";
                ExecSQL(sql);

                sql = " UPDATE AccountInformRegister SET PaymentDocSumType = " +
                         " (SELECT val_prm " + DBManager.sConvToInt +
                          " FROM " + prefData + "prm_10 " +
                          " WHERE nzp_prm = 1134 " +
                            " AND is_actual <> 100 " +
                            " AND dat_s <= '" + curDateTime.ToShortDateString() + "' " +
                            " AND dat_po >= '" + curDateTime.ToShortDateString() + "') ";
                ExecSQL(sql);

                string tName = "t_" + DateTime.Now.Ticks;
                string tDatS = "timestamp " + Utils.EStrNull(curDateTime.ToShortDateString());
                string tDatPo = "timestamp " + Utils.EStrNull(DateTo.ToShortDateString());

                sql = "SELECT k.num_ls, k.nzp_kvar, a.UnitID as nzp_serv, n.dat_s,n.dat_po, 0 as count_hour into temp " + tName +
                    " FROM  " + prefData + "kvar k, AccountInformRegister a, " + prefData + "nedop_kvar n" +
                      " WHERE k.num_ls=a.num_ls and k.nzp_kvar=n.nzp_kvar and a.UnitID=n.nzp_serv and n.is_actual<>100 " +
                      " and (n.dat_s, n.dat_po) OVERLAPS (" + tDatS + "," + tDatPo + ");";
                ExecSQL(sql);

               
                ExecSQL("CREATE INDEX ix_2 ON " + tName + "(num_ls,nzp_kvar) ", false);
                ExecSQL(DBManager.sUpdStat + " " + tName, false);

                sql = "DELETE FROM " + tName + " WHERE dat_s>dat_po;";
                ExecSQL(sql);
                sql = "UPDATE " + tName + " SET dat_s = " + tDatS + " WHERE dat_s<" + tDatS;
                ExecSQL(sql);
                sql = "UPDATE " + tName + " SET dat_po =  " + tDatPo + " WHERE dat_po>" + tDatPo;
                ExecSQL(sql);
                sql = "UPDATE " + tName + " SET count_hour =  (extract(epoch FROM age(dat_po, dat_s))/(3600))";
                ExecSQL(sql);

                sql = " UPDATE AccountInformRegister SET ReDeliveryHour = " +
                       " (SELECT SUM(count_hour)  FROM " + tName + " t " +
                        " WHERE t.num_ls=AccountInformRegister.num_ls and t.nzp_serv=AccountInformRegister.UnitID) ";
                ExecSQL(sql);

                ExecSQL(" DROP TABLE " + tName, false);

                ExecSQL(" ALTER TABLE AccountInformRegister DROP column num_ls", false);
                ExecSQL(" ALTER TABLE AccountInformRegister DROP column nzp_kvar", false);


                sql = " select * from AccountInformRegister  ";
         
                IDataReader reader;

                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    #region заполнение полей секции

                    var placeIdSystem = new Field
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
                    var regAccount = new Field
                    {
                        N = "RegAccount",
                        NT = "РИК лицевого счета",
                        IS = 0,
                        P = 2,
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
                        P = 3,
                        T = "TextType",
                        L = 40,
                        V =
                            (reader["AccountIDSys"] != DBNull.Value)
                                ? Convert.ToString(reader["AccountIDSys"]).Trim()
                                : ""
                    };
                    var contractId = new Field
                    {
                        N = "ContractSenderID",
                        NT = "Код договора на оказание ЖКУ",
                        IS = 1,
                        P = 4,
                        T = "IntType",
                        L = 9,
                        V =
                            (reader["ContractSenderID"] != DBNull.Value)
                                ? Convert.ToString(reader["ContractSenderID"]).Trim()
                                : ""
                    };
                    var unitId = new Field
                    {
                        N = "UnitID",
                        NT = "Код услуги",
                        IS = 1,
                        P = 5,
                        T = "IntType",
                        L = 3,
                        V =
                            (reader["UnitID"] != DBNull.Value)
                                ? Convert.ToString(reader["UnitID"]).Trim()
                                : ""
                    };
                    var balanceIn = new Field
                    {
                        N = "BalanceIn",
                        NT = "Входящее сальдо (Долг на начало месяца)",
                        IS = 1,
                        P = 6,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["BalanceIn"] != DBNull.Value)
                                ? Convert.ToString(reader["BalanceIn"]).Trim()
                                : ""
                    };
                    var economTarif = new Field
                    {
                        N = "EconomTarif",
                        NT = "Экономически обоснованный тариф",
                        IS = 1,
                        P = 7,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["EconomTarif"] != DBNull.Value)
                                ? Convert.ToString(reader["EconomTarif"]).Trim()
                                : ""
                    };
                    var rePercent = new Field
                    {
                        N = "REPercent",
                        NT = "Процент регулируемого тарифа от экономически обоснованного",
                        IS = 1,
                        P = 8,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["REPercent"] != DBNull.Value)
                                ? Convert.ToString(reader["REPercent"]).Trim()
                                : ""
                    };
                    var regulableTarif = new Field
                    {
                        N = "RegulableTarif",
                        NT = "Регулируемый тариф",
                        IS = 1,
                        P = 9,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["RegulableTarif"] != DBNull.Value)
                                ? Convert.ToString(reader["RegulableTarif"]).Trim()
                                : ""
                    };
                    var unitCode = new Field
                    {
                        N = "UnitCode",
                        NT = "Код единицы измерения расхода",
                        IS = 1,
                        P = 10,
                        T = "IntType",
                        L = 3,
                        V =
                            (reader["UnitCode"] != DBNull.Value)
                                ? Convert.ToString(ConvertCode(Convert.ToInt32(reader["UnitCode"]))).Trim()
                                : ""
                    };
                    var factVolume = new Field
                    {
                        N = "FactVolume",
                        NT = "Расход фактический",
                        IS = 1,
                        P = 11,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["FactVolume"] != DBNull.Value)
                                ? Convert.ToString(reader["FactVolume"]).Trim()
                                : ""
                    };
                    var normVolume = new Field
                    {
                        N = "NormVolume",
                        NT = "Расход по нормативу",
                        IS = 1,
                        P = 12,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["NormVolume"] != DBNull.Value)
                                ? Convert.ToString(reader["NormVolume"]).Trim()
                                : ""
                    };
                    var typePaymentDa = new Field
                    {
                        N = "TypePaymentDA",
                        NT = "Вид расчета по прибору учета",
                        IS = 1,
                        P = 13,
                        T = "IntType",
                        L = 1,
                        V =
                            (reader["TypePaymentDA"] != DBNull.Value)
                                ? Convert.ToString(reader["TypePaymentDA"]).Trim()
                                : ""
                    };
                    var chargeSum = new Field
                    {
                        N = "ChargeSum",
                        NT = "Сумма начисления",
                        IS = 1,
                        P = 14,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["ChargeSum"] != DBNull.Value)
                                ? Convert.ToString(reader["ChargeSum"]).Trim()
                                : ""
                    };
                    var reChargeSum = new Field
                    {
                        N = "ReChargeSum",
                        NT = "Сумма перерасчета начисления за предыдущий период (изменение сальдо)",
                        IS = 1,
                        P = 15,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["ReChargeSum"] != DBNull.Value)
                                ? Convert.ToString(reader["ReChargeSum"]).Trim()
                                : ""
                    };
                    var subsidySum = new Field
                    {
                        N = "SubsidySum",
                        NT = "Сумма дотации",
                        IS = 0,
                        P = 16,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["SubsidySum"] != DBNull.Value)
                                ? Convert.ToString(reader["SubsidySum"]).Trim()
                                : ""
                    };
                    var reSubsidySum = new Field
                    {
                        N = "ReSubsidySum",
                        NT = "Сумма перерасчета дотации за предыдущий период (за все месяца)",
                        IS = 0,
                        P = 17,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["ReSubsidySum"] != DBNull.Value)
                                ? Convert.ToString(reader["ReSubsidySum"]).Trim()
                                : ""
                    };
                    var benefitSum = new Field
                    {
                        N = "BenefitSum",
                        NT = "Сумма льготы",
                        IS = 0,
                        P = 18,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["BenefitSum"] != DBNull.Value)
                                ? Convert.ToString(reader["BenefitSum"]).Trim()
                                : ""
                    };
                    var reBenefitSum = new Field
                    {
                        N = "ReBenefitSum",
                        NT = "Сумма перерасчета льготы за предыдущий период",
                        IS = 0,
                        P = 19,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["ReBenefitSum"] != DBNull.Value)
                                ? Convert.ToString(reader["ReBenefitSum"]).Trim()
                                : ""
                    };
                    var poorSubsidySum = new Field
                    {
                        N = "PoorSubsidySum",
                        NT = "Сумма СМО (субсидий малоимущим)",
                        IS = 0,
                        P = 20,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["PoorSubsidySum"] != DBNull.Value)
                                ? Convert.ToString(reader["PoorSubsidySum"]).Trim()
                                : ""
                    };
                    var rePoorSubsidySum = new Field
                    {
                        N = "RePoorSubsidySum",
                        NT = "Сумма перерасчета СМО (субсидии малоимущим) за предыдущий период (за все месяца)",
                        IS = 0,
                        P = 21,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["RePoorSubsidySum"] != DBNull.Value)
                                ? Convert.ToString(reader["RePoorSubsidySum"]).Trim()
                                : ""
                    };
                    var paySum = new Field
                    {
                        N = "PaySum",
                        NT = "Сумма оплаты, поступившие за месяц начислений",
                        IS = 1,
                        P = 22,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["PaySum"] != DBNull.Value)
                                ? Convert.ToString(reader["PaySum"]).Trim()
                                : ""
                    };
                    var inactiveUnitFlag = new Field
                    {
                        N = "InactiveUnitFlag",
                        NT = "Признак недействующей услуги, по которой остались долги",
                        IS = 1,
                        P = 23,
                        T = "IntType",
                        L = 1,
                        V =
                            (reader["InactiveUnitFlag"] != DBNull.Value)
                                ? Convert.ToString(reader["InactiveUnitFlag"]).Trim()
                                : ""
                    };
                    var balanceOut = new Field
                    {
                        N = "BalanceOut",
                        NT = "Исходящее сальдо (Долг на окончание месяца)",
                        IS = 1,
                        P = 24,
                        T = "DecimalType",
                        L = 40,
                        V =
                            (reader["BalanceOut"] != DBNull.Value)
                                ? Convert.ToString(reader["BalanceOut"]).Trim()
                                : ""
                    };
                    var methodNumber = new Field
                    {
                        N = "MethodNumber",
                        NT = "Номер методики расчета",
                        IS = 0,
                        P = 25,
                        T = "IntType",
                        L = 10,
                        V =
                            (reader["MethodNumber"] != DBNull.Value)
                                ? Convert.ToString(reader["MethodNumber"]).Trim()
                                : ""
                    };
                    var paymentCode = new Field
                    {
                        N = "PaymentCode",
                        NT = "Платежный код",
                        IS = 0,
                        P = 26,
                        T = "IntType",
                        L = 10,
                        V =
                            (reader["PaymentCode"] != DBNull.Value)
                                ? Convert.ToString(reader["PaymentCode"]).Trim()
                                : ""
                    };

                    var fields = new FieldsUnload
                    {
                        F = new List<Field>
                        {
                            placeIdSystem,
                            regAccount,
                            accountIdSys,
                            contractId,
                            unitId,
                            balanceIn,
                            economTarif,
                            rePercent,
                            regulableTarif,
                            unitCode,
                            factVolume,
                            normVolume,
                            typePaymentDa,
                            chargeSum,
                            reChargeSum,
                            subsidySum,
                            reSubsidySum,
                            benefitSum,
                            reBenefitSum,
                            poorSubsidySum,
                            rePoorSubsidySum,
                            paySum,
                            inactiveUnitFlag,
                            balanceOut,
                            methodNumber,
                            paymentCode
                        }
                    };

                    #endregion

                    Data.Add(fields);
                }
            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("AccountInformRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
            string tblName = " AccountInformRegister ";
            string columnNames =
                " id SERIAL, " +
                " PlaceIDSystem	 BIGINT , " +
                " RegAccount	 CHAR ( 25), " +
                " AccountIDSys	 CHAR ( 40), " +
                " ContractSenderID	 INTEGER , " +
                " UnitID	 INTEGER , " +
                " BalanceIn	" + DBManager.sDecimalType + " (14,2) , " +
                " EconomTarif	" + DBManager.sDecimalType + " (14,2) , " +
                " REPercent	" + DBManager.sDecimalType + " (14,2) DEFAULT 100 , " +
                " RegulableTarif	" + DBManager.sDecimalType + " (14,2) , " +
                " UnitCode	 INTEGER , " +
                " FactVolume	" + DBManager.sDecimalType + " (14,2) , " +
                " NormVolume	" + DBManager.sDecimalType + " (14,2) , " +
                " TypePaymentDA	 INTEGER , " +
                " ChargeSum	" + DBManager.sDecimalType + " (14,2) , " +
                " ReChargeSum	" + DBManager.sDecimalType + " (14,2) , " +
                " SubsidySum	" + DBManager.sDecimalType + " (14,2) DEFAULT 0, " +
                " ReSubsidySum	" + DBManager.sDecimalType + " (14,2) DEFAULT 0, " +
                " BenefitSum	" + DBManager.sDecimalType + " (14,2) DEFAULT 0, " +
                " ReBenefitSum	" + DBManager.sDecimalType + " (14,2) DEFAULT 0, " +
                " PoorSubsidySum	" + DBManager.sDecimalType + " (14,2) DEFAULT 0, " +
                " RePoorSubsidySum	" + DBManager.sDecimalType + " (14,2) DEFAULT 0, " +
                " PaySum	" + DBManager.sDecimalType + " (14,2) , " +
                " InactiveUnitFlag	 INTEGER , " +
                " BalanceOut	" + DBManager.sDecimalType + " (14,2) , " +
                " MethodNumber	 INTEGER , " +
                " PaymentCode	 " + DBManager.sDecimalType + " (13,0) , " +
                " PaymentDocUnitNumber	 INTEGER , " +
                " PaymentDocSumType	 INTEGER , " +
                " ReCalcSum	" + DBManager.sDecimalType + " (14,2) DEFAULT 0, " +
                " ChangeBalanceSum	" + DBManager.sDecimalType + " (14,2) , " +
                " ReDeliverySum	" + DBManager.sDecimalType + " (14,2) , " +
                " ReDeliveryHour	" + DBManager.sDecimalType + " (14,2)  DEFAULT 0, " +
                " num_ls INTEGER, " +
                " nzp_kvar INTEGER ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnNames));
        }

        public override void DropTempTable()
        {
        }
    }
}
