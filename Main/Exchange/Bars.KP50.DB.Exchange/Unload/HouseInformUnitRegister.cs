using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Bars.KP50.DataImport;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace Bars.KP50.DB.Exchange.Unload
{
    public class HouseInformUnitRegister : BaseUnload20
    {
        /// <summary> Наименование тега секции </summary>
        public override string Name {
            get { return "HouseInformUnitRegister"; }
        }

        /// <summary> Наименование секции </summary>
        public override string NameText {
            get { return "Реестр информации об оказываемых услугах в доме"; }
        }

        /// <summary> Номер секции </summary>
        public override int Code {
            get { return 22; }
        }

        /// <summary> Список полей секции </summary>
        public override List<FieldsUnload> Data { get; set; }

        /// <summary> Выборка по всем банкам данных </summary>
        public override void Start() {

        }

        /// <summary> Выборка по банкам данных(территориям) </summary>
        /// <param name="pref">Префикс банка данных(территории)</param>
        public override void Start(string pref) 
        {
            Data = new List<FieldsUnload>();
            OpenConnection();
            var nowDate = new DateTime(Year, Month, 1);
            try
            {
                CreateTempTable();
                #region заполнение временной таблицы

                string prefDara = pref + DBManager.sDataAliasRest,
                        prefKernel = pref + DBManager.sKernelAliasRest;
                string chargeYY = pref + "_charge_" + (nowDate.Year - 2000).ToString("00") + DBManager.tableDelimiter + " charge_" + nowDate.Month.ToString("00");
                string counterYY = pref + "_charge_" + (nowDate.Year - 2000).ToString("00") + DBManager.tableDelimiter + " counters_" + nowDate.Month.ToString("00");
                string sql;

                string area = GetNzpArea(ListNzpArea);
                string whereArea = "";
                if (area != String.Empty)
                {
                    whereArea = " AND k.nzp_area in (" + area + " )";
                }

                if (TempTableInWebCashe(chargeYY))
                {
                    sql = " INSERT INTO house_inform_unit_register (house_id_system, unit_id, unit_code, econom_tarif, nation_tarif, nation_start_sum, nation_over_start_period, " +
                                                                    " unit_charge_sum, charge_sum_last_period, total_re_delivery, payment_sum, nation_fact_payment, nation_end_sum) " +
                          " SELECT k.nzp_dom AS house_id_system, " +
                                 " c.nzp_serv AS unit_id, " +
                                 " f.nzp_measure AS unit_code, " +
                                 " MAX(c.tarif) AS econom_tarif, " +
                                 " MAX(c.tarif_f) AS nation_tarif, " +
                                 " SUM(c.sum_insaldo) AS nation_start_sum, " +
                                 " (SUM(" + DBManager.sNvlWord + "(c.sum_insaldo,0)) - SUM(" + DBManager.sNvlWord + "(c.sum_money,0))) AS nation_over_start_period, " +
                                 " SUM(c.sum_tarif) AS unit_charge_sum, " +
                                 " SUM(c.reval) AS charge_sum_last_period, " +
                                 " SUM(c.sum_nedop) AS total_re_delivery, " +
                                 " SUM(c.sum_charge) AS payment_sum, " +
                                 " SUM(c.sum_money) AS nation_fact_payment, " +
                                 " SUM(c.sum_outsaldo) AS nation_end_sum " +
                          " FROM " + chargeYY + " c INNER JOIN " + prefDara + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
                                                  " LEFT OUTER JOIN " + prefKernel + "formuls f ON f.nzp_frm = c.nzp_frm " +
                          " WHERE c.nzp_serv < 1007568 " + whereArea +
                          " GROUP BY 1,2,3";
                    ExecSQL(sql);

                    sql = " UPDATE house_inform_unit_register SET unit_code = " +
                          " (SELECT nzp_measure " +
                           " FROM " + prefKernel + "services s " +
                           " WHERE s.nzp_serv = house_inform_unit_register.unit_id) " +
                        " WHERE unit_code IS NULL  ";
                    ExecSQL(sql);

                    sql = " UPDATE house_inform_unit_register SET sum_unit_bulk = " +
                          " (SELECT SUM(rashod) " +
                           " FROM " + counterYY + " c " +
                           " WHERE stek = 3 " +
                             " AND c.nzp_dom = house_inform_unit_register.house_id_system " +
                             " AND c.nzp_serv = house_inform_unit_register.unit_id)";
                    ExecSQL(sql);

                    sql = " UPDATE house_inform_unit_register SET reg_unit_bulk = " +
                          " (SELECT SUM(rashod) " +
                           " FROM " + counterYY + " c " +
                           " WHERE stek = 3 " +
                             " AND c.nzp_dom = house_inform_unit_register.house_id_system " +
                             " AND c.nzp_serv = house_inform_unit_register.unit_id)";
                    ExecSQL(sql);

                    sql = " UPDATE house_inform_unit_register SET ind_unit_bulk = " +
                          " (SELECT SUM(val2) " +
                           " FROM " + counterYY + " c " +
                           " WHERE stek = 3 " +
                             " AND nzp_type = 1 " +
                             " AND c.nzp_dom = house_inform_unit_register.house_id_system " +
                             " AND c.nzp_serv = house_inform_unit_register.unit_id)";
                    ExecSQL(sql);

                    sql = " UPDATE house_inform_unit_register SET com_unit_bulk = " +
                          " (SELECT SUM(val2) " +
                           " FROM " + counterYY + " c " +
                           " WHERE stek = 3 " +
                             " AND nzp_type = 3 " +
                             " AND c.nzp_dom = house_inform_unit_register.house_id_system " +
                             " AND c.nzp_serv = house_inform_unit_register.unit_id)";
                    ExecSQL(sql);
                }

                #endregion

                MyDataReader reader;
                

                sql = " SELECT * FROM house_inform_unit_register ";
                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    #region заполнение списка полей секции

                    var fields = new FieldsUnload
                    {
                        F =
                            new List<Field>
                               {
                                   AddField("RegHouseID",                  "РИК дома",                                 0, 1,  "TextType",    25, reader["reg_house_id"]),
                                   AddField("HouseIDSystem",               "Уникальный код дома в системе отправителя",1, 2,  "IntType",     18, reader["house_id_system"]),
                                   //AddField("InformDate",                  "Месяц и Год показаний",                    1, 3,  "DateType",    18, reader["inform_date"]),
                                   AddField("InformDate",                  "Месяц и Год показаний",                    1, 3,  "DateType",    18, nowDate),
                                   AddField("UnitID",                      "Код услуги",                               1, 4,  "IntType",     18, reader["unit_id"]),
                                   AddField("UnitCode",                    "Единица измерения",                        1, 5,  "IntType",     18, ConvertCode(Convert.ToInt32(reader["unit_code"]))),
                                   AddField("SumUnitBulk",                 "Общий объем оказанной услуги",             1, 6,  "DecimalType", 18, reader["sum_unit_bulk"]),
                                   AddField("IndUnitBulk",                 "Объем оказанной услуги по ИПУ",            1, 7,  "DecimalType", 18, reader["ind_unit_bulk"]),
                                   AddField("RegUnitBulk",                 "Объем оказанной услуги по ЕСТКУ",          1, 8,  "DecimalType", 18, reader["reg_unit_bulk"]),
                                   AddField("ComUnitBulk",                 "Объем оказанной услуги по ОДПУ",           1, 9,  "DecimalType", 18, reader["com_unit_bulk"]),
                                   AddField("EconomTarif",                 "Экономически обоснованный тариф",          1, 10, "DecimalType", 18, reader["econom_tarif"]),
                                   AddField("NationTarif",                 "Тариф для населения",                      1, 11, "DecimalType", 20, reader["nation_tarif"]),
                                   AddField("NationStartSum",              "Общая задолженность населения на начало отчетного месяца",             
                                                                                                                       1, 12, "DecimalType", 18, reader["nation_start_sum"]),
                                   AddField("NationOverStartPeriod",       "Просроченная задолженность  населения на начало отчетного месяца",              
                                                                                                                       1, 13, "DecimalType", 18, reader["nation_over_start_period"]),
                                   AddField("CreditClaimStartPeriod",      "Просроченная задолженность населения на  начало отчетного месяца, по которой предъявлены иски", 
                                                                                                                       1, 14, "DecimalType", 18, reader["credit_claim_start_period"]),
                                   AddField("CreditRestrucStartPeriod",    "Просроченная задолженность населения на начало отчетного месяца, по которой подписана реструктуризация", 
                                                                                                                       1, 15, "DecimalType", 18, reader["credit_restruc_start_period"]),
                                   AddField("CreditHopeStartPeriod",       "Безнадежная к взысканию просроченная задолженность населения на начало отчетного месяца",
                                                                                                                       1, 16, "DecimalType", 18, reader["credit_hope_start_period"]),
                                   AddField("CreditMCStartPeriod",         "Общая задолженность УК (ТСЖ) на начало отчетного месяца",
                                                                                                                       1, 17, "DecimalType", 18, reader["credit_mc_start_period"]),
                                   AddField("CreditMCOverStartPeriod",     "Просроченная задолженность УК (ТСЖ) на начало отчетного месяца",
                                                                                                                       1, 18, "DecimalType", 18, reader["credit_mc_over_start_period"]),
                                   AddField("CreditMCClaimStartPeriod",    "Просроченная задолженность УК (ТСЖ) на начало отчетного месяца", 
                                                                                                                       1, 19, "DecimalType", 18, reader["credit_mc_claim_start_period"]),
                                   AddField("CreditMCRestrucStartPeriod",  "Просроченная задолженность УК (ТСЖ) на начало отчетного месяца, по которой подписана реструктуризация",
                                                                                                                       1, 20, "DecimalType", 18, reader["credit_mc_restruc_start_period"]), 
                                   AddField("CreditMCHopeStartPeriod",     "Безнадежная к взысканию просроченная задолженность УК (ТСЖ) на начало отчетного месяца",
                                                                                                                       1, 21, "DecimalType", 18, reader["credit_mc_hope_start_period"]),
                                   AddField("UnitChargeSum",               "Начислено по услуге",                      1, 22, "DecimalType", 18, reader["unit_charge_sum"]),
                                   AddField("TotalReDelivery",             "Общая недопоставка",                       1, 23, "DecimalType", 18, reader["total_re_delivery"]),
                                   AddField("ChargeSumLastPeriod",         "Перерасчет за предыдущие месяцы",          1, 24, "DecimalType", 18, reader["charge_sum_last_period"]),
                                   AddField("TimeReDelivery",              "Недопоставка: временное отсутствие",       1, 25, "DecimalType", 18, reader["time_re_delivery"]),
                                   AddField("UnqualityReDelivery",         "Недопоставка: некачественная поставка",    1, 26, "DecimalType", 18, reader["unquality_re_delivery"]),
                                   AddField("IndicatorReDelivery",         "Недопоставка: показания приборов учета",   1, 27, "DecimalType", 18, reader["indicator_re_delivery"]),
                                   AddField("DisconnectReDelivery",        "Недопоставка: добровольное отключение жилого фонда",                               
                                                                                                                       1, 28, "DecimalType", 18, reader["disconnect_re_delivery"]),
                                   AddField("OptimizationReDelivery",      "Недопоставка: оптимизация жилого фонда",   1, 29, "DecimalType", 18, reader["optimization_re_delivery"]),
                                   AddField("TransferMCReDelivery",        "Недопоставка: передача жилого фонда другим УК, ТСЖ",            
                                                                                                                       1, 30, "DecimalType", 18, reader["transfer_mc_re_delivery"]),
                                   AddField("IncorrectSeasonReDelivery",   "Недопоставка: позднее начало и раннее окончание сезона",                              
                                                                                                                       1, 31, "DecimalType", 18, reader["incorrect_season_re_delivery"]),
                                   AddField("CrashReDelivery",             "Недопоставка: наличие аварийных ситуаций", 1, 32, "DecimalType", 18, reader["crash_re_delivery"]),
                                   AddField("TotalOverDelivery",           "Общая перепоставка",                       1, 33, "DecimalType", 18, reader["total_over_delivery"]),
                                   AddField("IndicatorOverDelivery",       "Перепоставка: показания приборов учета",   1, 34, "DecimalType", 18, reader["indicator_over_delivery"]),
                                   AddField("TransferMCOverDelivery",      "Перепоставка: Принятие жилого фонда от других УК, ТСЖ",         
                                                                                                                       1, 35, "DecimalType", 18, reader["transfer_mc_over_delivery"]),
                                   AddField("IncorrectSeasonOverDelivery", "Перепоставка: Раннее начало и продление сезона",                               
                                                                                                                       1, 36, "DecimalType", 18, reader["incorrect_season_over_delivery"]),
                                   AddField("DiscountMC",                  "Скидка, предоставленная УК, ТСЖ",          1, 37, "DecimalType", 18, reader["discount_mc"]),
                                   AddField("PaymentSum",                  "Сумма к оплате",                           1, 38, "DecimalType", 18, reader["payment_sum"]),
                                   AddField("NationFactPayment",           "Всего фактически оплачено населением",     1, 39, "DecimalType", 18, reader["nation_fact_payment"]),
                                   AddField("MCPayment",                   "Оплачено через УК (ТСЖ)",                  1, 40, "DecimalType", 18, reader["mc_payment"]),
                                   AddField("AgentPayment",                "Оплачено через агентов",                   1, 41, "DecimalType", 18, reader["agent_payment"]),
                                   AddField("TerminalPayment",             "Оплачено через терминал, почту, банк",     1, 42, "DecimalType", 18, reader["terminal_payment"]),
                                   AddField("PenaltiesPayment",            "Оплачено пени",                            1, 43, "DecimalType", 18, reader["penalties_payment"]),
                                   AddField("TransferMCFact",              "Фактически перечислено УК (ТСЖ) поставщику",                    
                                                                                                                       1, 44, "DecimalType", 18, reader["transfer_mc_fact"]),
                                   AddField("NationEndSum",                "Общая задолженность населения на конец отчетного месяца",                              
                                                                                                                       1, 45, "DecimalType", 18, reader["nation_end_sum"]),
                                   AddField("NationOverEndPeriod",         "Просроченная задолженность населения на конец отчетного месяца",                               
                                                                                                                       1, 46, "DecimalType", 18, reader["nation_over_end_period"]),
                                   AddField("CreditClaimEndPeriod",        "Просроченная задолженность населения на конец отчетного месяца, по которой предъявлены иски",                               
                                                                                                                       1, 47, "DecimalType", 18, reader["credit_claim_end_period"]),
                                   AddField("CreditRestrucEndPeriod",      "Просроченная задолженность населения на конец отчетного месяца, по которой подписана реструктуризация",                               
                                                                                                                       1, 48, "DecimalType", 18, reader["credit_restruc_end_period"]),
                                   AddField("CreditHopeEndPeriod",         "Безнадежная к взысканию просроченная задолженность населения на конец отчетного месяца",                               
                                                                                                                       1, 49, "DecimalType", 18, reader["credit_hope_end_period"]),
                                   AddField("CreditMCEndPeriod",           "Общая задолженность УК (ТСЖ) на конец отчетного месяца",                               
                                                                                                                       1, 50, "DecimalType", 18, reader["credit_mc_end_period"]),
                                   AddField("CreditMCOverEndPeriod",       "Просроченная задолженность УК (ТСЖ) на конец отчетного месяца",                               
                                                                                                                       1, 51, "DecimalType", 18, reader["credit_mc_over_end_period"]),
                                   AddField("CreditMCClaimEndPeriod",      "Просроченная задолженность УК (ТСЖ) на конец отчетного месяца, по которой предъявлены иски",                               
                                                                                                                       1, 52, "DecimalType", 18, reader["credit_mc_claim_end_period"]),
                                   AddField("CreditMCRestrucEndPeriod",    "Просроченная задолженность УК (ТСЖ) на конец отчетного месяца, по которой подписана реструктуризация",                               
                                                                                                                       1, 53, "DecimalType", 18, reader["credit_mc_restruc_end_period"]),
                                   AddField("CreditMCHopeEndPeriod",       "Безнадежная к взысканию просроченная задолженность УК (ТСЖ) на конец отчетного месяца",                               
                                                                                                                       1, 54, "DecimalType", 18, reader["credit_mc_hope_end_period"])
                               }
                    };
                    #endregion
                    Data.Add(fields);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("AccountPaymentReCalcRegister.Start(pref): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }
        }

        /// <summary> Выборка по лицевым счетам </summary>
        public override void StartSelect() {
        }

        /// <summary> Добавление поле секции </summary>
        /// <param name="name">Наименование тега поля</param>
        /// <param name="nameText">Наименование поля</param>
        /// <param name="isRequired">Обязательность заполнения</param>
        /// <param name="place">Расположение</param>
        /// <param name="type">Тип поля</param>
        /// <param name="length">Длина поля</param>
        /// <param name="value">Значение поля</param>
        private Field AddField(string name, string nameText, int isRequired, int place, string type, int length, object value) {
            return new Field
            {
                N = name,
                NT = nameText,
                IS = isRequired,
                P = place,
                T = type,
                L = length,
                V = value != DBNull.Value ? value.ToString().Trim() : string.Empty
            };
        }

        /// <summary> Создание временных таблиц </summary>
        public override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE house_inform_unit_register(" +
                               " id SERIAL NOT NULL, " +
                               " type_section INTEGER , " +
                               " name CHARACTER(50), " +
                               " reg_house_id CHARACTER(25), " +
                               " house_id_system INTEGER, " +
                               " inform_date DATE, " +
                               " unit_id INTEGER, " +
                               " unit_code INTEGER, " +
                               " sum_unit_bulk " + DBManager.sDecimalType + "(14,4), " +
                               " ind_unit_bulk " + DBManager.sDecimalType + "(14,4), " +
                               " reg_unit_bulk " + DBManager.sDecimalType + "(14,4), " +
                               " com_unit_bulk " + DBManager.sDecimalType + "(14,4), " +
                               " econom_tarif " + DBManager.sDecimalType + "(14,4), " +
                               " nation_tarif " + DBManager.sDecimalType + "(14,4), " +
                               " nation_start_sum " + DBManager.sDecimalType + "(14,4), " +
                               " nation_over_start_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_claim_start_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_restruc_start_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_hope_start_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_start_period " + DBManager.sDecimalType + "(14,4)," +
                               " credit_mc_over_start_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_claim_start_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_restruc_start_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_hope_start_period " + DBManager.sDecimalType + "(14,4), " +
                               " unit_charge_sum " + DBManager.sDecimalType + "(14,4), " +
                               " total_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " charge_sum_last_period " + DBManager.sDecimalType + "(14,4), " +
                               " time_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " unquality_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " indicator_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " disconnect_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " optimization_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " transfer_mc_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " incorrect_season_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " crash_re_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " total_over_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " indicator_over_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " transfer_mc_over_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " incorrect_season_over_delivery " + DBManager.sDecimalType + "(14,4), " +
                               " discount_mc " + DBManager.sDecimalType + "(14,4), " +
                               " payment_sum " + DBManager.sDecimalType + "(14,4), " +
                               " nation_fact_payment " + DBManager.sDecimalType + "(14,4), " +
                               " mc_payment " + DBManager.sDecimalType + "(14,4), " +
                               " agent_payment " + DBManager.sDecimalType + "(14,4), " +
                               " terminal_payment " + DBManager.sDecimalType + "(14,4), " +
                               " penalties_payment " + DBManager.sDecimalType + "(14,4), " +
                               " transfer_mc_fact " + DBManager.sDecimalType + "(14,4), " +
                               " nation_end_sum " + DBManager.sDecimalType + "(14,4), " +
                               " nation_over_end_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_claim_end_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_restruc_end_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_hope_end_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_end_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_over_end_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_claim_end_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_restruc_end_period " + DBManager.sDecimalType + "(14,4), " +
                               " credit_mc_hope_end_period " + DBManager.sDecimalType + "(14,4)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        /// <summary> Удаление временных таблиц </summary>
        public override void DropTempTable() {
            ExecSQL("DROP TABLE house_inform_unit_register");
        }
    }
}
