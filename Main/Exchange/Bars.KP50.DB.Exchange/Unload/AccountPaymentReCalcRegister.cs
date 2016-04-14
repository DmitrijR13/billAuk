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
    public class AccountPaymentReCalcRegister : BaseUnload20
    {
        /// <summary> Наименование тега секции </summary>
        public override string Name {
            get { return "AccountPaymentReCalcRegister"; }
        }

        /// <summary> Наименование секции </summary>
        public override string NameText {
            get { return "Реестр информации о перерасчете начислений ЛС по услугам"; }
        }

        /// <summary> Номер секции </summary>
        public override int Code {
            get { return 10; }
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

                string area = GetNzpArea(ListNzpArea);
                string whereArea = "";
                if (area != String.Empty)
                {
                    whereArea = " and k.nzp_area in (" + area + " )";
                }

                #region заполнение временной таблицы
                string prefDara = pref + DBManager.sDataAliasRest,
                        prefKernel = pref + DBManager.sKernelAliasRest;
                string chargeYY = pref + "_charge_" + (nowDate.Year - 2000).ToString("00") + DBManager.tableDelimiter + " charge_" + nowDate.Month.ToString("00");
                string calcGkuYY = pref + "_charge_" + (nowDate.Year - 2000).ToString("00") + DBManager.tableDelimiter + " calc_gku_" + nowDate.Month.ToString("00");
                string chargeLnkYY = pref + "_charge_" + (nowDate.Year - 2000).ToString("00") + DBManager.tableDelimiter + " lnk_charge_" + nowDate.Month.ToString("00");
                string sql;

                if (TempTableInWebCashe(chargeYY))
                {
                    sql = " INSERT INTO account_payment_re_calc_register(account_id_sys, contract_code, unit_id, econom_tarif, method_number, " +
                                                                              " type_payment_da, re_charge_sum, payment_code, change_balance_sum_re_calc, re_delivery_sum_re_calc, payment_doc_unit_number) " +
                                 " SELECT k.num_ls AS account_id_sys, " +
                                        " c.nzp_supp AS contract_code, " +
                                        " c.nzp_serv AS unit_id, " +
                                        " c.tarif AS econom_tarif, " +
                                        " c.nzp_frm AS method_number, " +
                                        " c.is_device AS type_payment_da, " +
                                        " c.reval AS re_charge_sum, " +
                                        " k.pkod AS payment_code, " +
                                        " c.real_charge AS change_balance_sum_re_calc, " +
                                        " c.real_charge AS re_delivery_sum_re_calc, " +
                                        " c.order_print AS payment_doc_unit_number " +
                                 " FROM " + prefDara + "kvar k INNER JOIN " + chargeYY + " c ON c.nzp_kvar = k.nzp_kvar " + 
                                 " WHERE c.nzp_serv < 1007568 " + whereArea;
                    ExecSQL(sql);

                    sql = " UPDATE account_payment_re_calc_register SET individual_id_system = " +
                          " (SELECT nzp_gil " +
                           " FROM " + prefDara + "kart k INNER JOIN " + prefDara + "s_rod r ON r.nzp_rod = k.nzp_rod " +
                                                       " INNER JOIN " + prefDara + "kvar kv ON (kv.nzp_kvar = k.nzp_kvar " +
                                                                                          " AND kv.num_ls = account_payment_re_calc_register.account_id_sys) " +
                          " WHERE rod LIKE 'наним%' OR rod LIKE 'собств%' OR rod LIKE 'владел%');";
                    ExecSQL(sql);

                    sql = " UPDATE account_payment_re_calc_register SET unit_code = " +
                           " (SELECT nzp_measure " +
                            " FROM " + prefKernel + "services s " +
                            " WHERE s.nzp_serv = account_payment_re_calc_register.unit_id ) ";
                    ExecSQL(sql);

                    sql = " UPDATE account_payment_re_calc_register SET unit_code = " +
                          " (SELECT nzp_measure " +
                          " FROM " + prefKernel + "formuls f " +
                          " WHERE f.nzp_frm = account_payment_re_calc_register.method_number)  where method_number is not null ";
                    ExecSQL(sql);

                    sql = " UPDATE account_payment_re_calc_register SET fact_volume = " +
                          " (SELECT sum(c.rashod) " +
                          " FROM " + calcGkuYY + " c INNER JOIN " +
                                 prefDara + "kvar kv ON (kv.nzp_kvar = c.nzp_kvar " +
                                                   " AND kv.num_ls = account_payment_re_calc_register.account_id_sys " +
                                                   " AND c.nzp_serv = account_payment_re_calc_register.unit_id " +
                                                   " AND c.nzp_supp = account_payment_re_calc_register.contract_code)) ";
                    ExecSQL(sql);

                    sql = " UPDATE account_payment_re_calc_register SET norm_volume = " +
                          " (SELECT sum(c.rashod_norm) " +
                          " FROM " + calcGkuYY + " c INNER JOIN " +
                                 prefDara + "kvar kv ON (kv.nzp_kvar = c.nzp_kvar " +
                                                   " AND kv.num_ls = account_payment_re_calc_register.account_id_sys " +
                                                   " AND c.nzp_serv = account_payment_re_calc_register.unit_id " +
                                                   " AND c.nzp_supp = account_payment_re_calc_register.contract_code))";
                    ExecSQL(sql);

                    sql = " UPDATE account_payment_re_calc_register SET payment_doc_sum_type = " +
                          " (SELECT val_prm " + DBManager.sConvToInt +
                           " FROM " + prefDara + "prm_10 " +
                           " WHERE nzp_prm = 1134 " +
                             " AND is_actual <> 100 " +
                             " AND dat_s <= '" + nowDate.ToShortDateString() + "' " +
                             " AND dat_po >= '" + nowDate.ToShortDateString() + "') ";
                    ExecSQL(sql);

#if PG
                    sql = " UPDATE account_payment_re_calc_register SET recalculation_date = " +
                          " (SELECT DATE('01.' || month_ || '.' || year_) " +
                          " FROM " + chargeLnkYY + " l INNER JOIN " + prefDara + "kvar kv ON (kv.nzp_kvar = l.nzp_kvar " +
                                                                                        " AND kv.num_ls = account_payment_re_calc_register.account_id_sys) " +
                          " ORDER BY year_ DESC, month_ DESC limit 1)";
                    ExecSQL(sql);
#endif
                }
                #endregion

                MyDataReader reader;
                

                sql = " SELECT * FROM account_payment_re_calc_register";
                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    #region заполнение списка полей секции
                    var UnitCode = new Field
                    {
                        N = "UnitCode",
                        NT = "Код единицы измерения расхода (из справочника)",
                        IS = 1,
                        P = 11,
                        T = "IntType",
                        L = 18,
                        V = (reader["unit_code"] != DBNull.Value)
                            ? Convert.ToString(ConvertCode(Convert.ToInt32(reader["unit_code"]))).Trim()
                            : ""
                    };
                    var fields = new FieldsUnload
                    {
                        F =
                            new List<Field>
                            {
                                 AddField("PlaceIDSystem",          "Уникальный код домохозяйства в системе отправителя",    1, 1,  "IntType",     18, reader["place_id_system"]),
                                 AddField("IndividualIDSystem",     "Уникальный код физического лица в системе отправителя", 1, 2,  "IntType",     18, reader["individual_id_system"]),
                                 AddField("RegAccount",             "РИК лицевого счета",                                    1, 3,  "TextType",    25, reader["reg_account"]),
                                 AddField("RecalculationDate",      "Месяц и год перерасчета",                               1, 4,  "DateType",    20, reader["recalculation_date"]),
                                 AddField("AccountIDSys",           "№ ЛС в системе отправителя",                            1, 5,  "IntType",     18, reader["account_id_sys"]),
                                 AddField("ContractCode",           "Код договора на оказание ЖКУ",                          1, 6,  "IntType",     25, reader["contract_code"]),
                                 AddField("UnitID",                 "Код услуги",                                            1, 7,  "IntType",     18, reader["unit_id"]),
                                 AddField("EconomTarif",            "Экономически обоснованный тариф",                       1, 8,  "DecimalType", 18, reader["econom_tarif"]),
                                 AddField("REPercent",              "Процент регулируемого тарифа от экономически обоснованного",
                                                                                                                             1, 9,  "DecimalType", 18, reader["re_percent"]),
                                 AddField("RegulableTarif",         "Регулируемый тариф",                                    1, 10, "DecimalType", 18, reader["regulable_tarif"]),
                                 UnitCode,
                                 //AddField("UnitCode",               "Код единицы измерения расхода (из справочника)",        1, 11, "IntType",     18, ConvertCode(Convert.ToInt32(reader["unit_code"]))),
                                 AddField("FactVolume",             "Расход фактический",                                    1, 12, "DecimalType", 18, reader["fact_volume"]),
                                 AddField("NormVolume",             "Расход по нормативу",                                   1, 13, "DecimalType", 18, reader["norm_volume"]),
                                 AddField("TypePaymentDA",          "TypePaymentDA",                                         1, 14, "IntType",     18, reader["type_payment_da"]),
                                 AddField("ReChargeSum",            "ReChargeSum",                                           1, 15, "DecimalType", 18, reader["re_charge_sum"]),
                                 AddField("ReSubsidySum",           "Сумма перерасчета дотации за месяц перерасчета",        1, 16, "DecimalType", 18, reader["re_subsidy_sum"]),
                                 AddField("ReBenefitSum",           "Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)",
                                                                                                                             1, 17, "DecimalType", 18, reader["re_benefit_sum"]),
                                 AddField("RePoorSubsidySum",       "Сумма перерасчета СМО (субсидии малоимущим) за месяц перерасчета", 
                                                                                                                             1, 18, "DecimalType", 18, reader["re_poor_subsidy_sum"]),
                                 AddField("MethodNumber",           "Номер методики расчета",                                0, 19, "IntType",     5, reader["method_number"]),
                                 AddField("PaymentCode",            "Платежный код",                                         0, 20, "IntType",     18, reader["payment_code"]),
                                 AddField("PaymentDocUnitNumber",   "Порядковый номер услуги в ЕПД",                         0, 21, "IntType",     5, reader["payment_doc_unit_number"]),
                                 AddField("PaymentDocSumType",      "Тип суммы к оплате для ЕПД",                            0, 1,  "IntType",     22, reader["payment_doc_sum_type"]),
                                 AddField("ChangeBalanceSumReCalc", "Сумма перекидки в месяц перерасчета",                   0, 23, "DecimalType", 18, reader["change_balance_sum_re_calc"]),
                                 AddField("ReDeliverySumReCalc",    "Сумма учтенной перекидки в месяц перерасчета",          0, 24, "DecimalType", 18, reader["re_delivery_sum_re_calc"]),
                                 AddField("ReDeliveryHourReCalc",   "Количество часов в месяц перерасчета",                  0, 25, "DecimalType", 18, reader["re_delivery_hour_re_calc"])
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
        public override void CreateTempTable() {
            string sql = " CREATE TEMP TABLE account_payment_re_calc_register( " +
                            " id SERIAL NOT NULL, " +
                            " place_id_system BIGINT , " +
                            " individual_id_system BIGINT , " +
                            " reg_account CHAR(25), " +
                            " recalculation_date DATE , " +
                            " account_id_sys BIGINT , " +
                            " contract_code INTEGER , " +
                            " unit_id INTEGER, " +
                            " econom_tarif " + DBManager.sDecimalType + "(14,2), " +
                            " re_percent " + DBManager.sDecimalType + "(14,2), " +
                            " regulable_tarif " + DBManager.sDecimalType + "(14,2), " +
                            " unit_code INTEGER, " +
                            " fact_volume " + DBManager.sDecimalType + "(14,2), " +
                            " norm_volume " + DBManager.sDecimalType + "(14,2), " +
                            " type_payment_da INTEGER, " +
                            " re_charge_sum " + DBManager.sDecimalType + "(14,2), " +
                            " re_subsidy_sum " + DBManager.sDecimalType + "(14,2), " +
                            " re_benefit_sum " + DBManager.sDecimalType + "(14,2), " +
                            " re_poor_subsidy_sum " + DBManager.sDecimalType + " (14,2), " +
                            " method_number INTEGER, " +
                            " payment_code BIGINT, " +
                            " payment_doc_unit_number INTEGER, " +
                            " payment_doc_sum_type INTEGER, " +
                            " change_balance_sum_re_calc " + DBManager.sDecimalType + "(14,2), " +
                            " re_delivery_sum_re_calc " + DBManager.sDecimalType + " (14,2), " +
                            " re_delivery_hour_re_calc " + DBManager.sDecimalType + " (14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        /// <summary> Удаление временных таблиц </summary>
        public override void DropTempTable() {
            ExecSQL("DROP TABLE account_payment_re_calc_register ");
        }
    }
}