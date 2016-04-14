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
    public class BackorderRegister : BaseUnload20
    {
        /// <summary> Наименование тега секции </summary>
        public override string Name {
            get { return "BackorderRegister"; }
        }

        /// <summary> Наименование секции </summary>
        public override string NameText {
            get { return "Недопоставки"; }
        }

        /// <summary> Номер секции </summary>
        public override int Code {
            get { return 16; }
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
            var nowDate = new DateTime(Year, Month, 1); ;
            try
            {
                CreateTempTable();

                #region заполнение временной таблицы

                string area = GetNzpArea(ListNzpArea);
                string whereArea = "";
                if (area != String.Empty)
                {
                    whereArea = " and k.nzp_area in (" + area + " )";
                }

                string prefDara = pref + DBManager.sDataAliasRest;
                string chargeYY = pref + "_charge_" + (nowDate.Year - 2000).ToString("00") + DBManager.tableDelimiter + " charge_" + nowDate.Month.ToString("00");
                string sql;
                if (TempTableInWebCashe(chargeYY))
                {
                    sql = " INSERT INTO backorder_register (account_id_sys, unit_id, temperature, re_delivery_start_date, re_delivery_end_date, re_delivery_type, re_delivery_amount) " +
                          " SELECT k.num_ls AS account_id_sys, " +
                                 " c.nzp_serv AS unit_id, " +
                                 " (n.tn " + DBManager.sConvToNum + ") AS temperature, " +
                                 " n.dat_s AS re_delivery_start_date, " +
                                 " n.dat_po AS re_delivery_end_date, " +
                                 " n.nzp_kind AS re_delivery_type, " +
                                 " SUM(c.sum_nedop) AS re_delivery_amount " +
                          " FROM " + chargeYY + " c INNER JOIN " + prefDara + "kvar k ON c.nzp_kvar = k.nzp_kvar " +
                                                  " INNER JOIN " + prefDara + "nedop_kvar n ON (k.nzp_kvar = n.nzp_kvar" +
                                                                                          " AND n.nzp_serv = c.nzp_serv) " +
                          " WHERE n.is_actual <> 100 " +
                            " AND n.dat_s <= DATE('" + nowDate.ToShortDateString() + "')" +
                            " AND n.dat_po >= DATE('" + nowDate.ToShortDateString() + "')" +
                          " AND c.nzp_serv < 1007568 " + whereArea +
                          " GROUP BY 1,2,3,4,5,6 ";
                    ExecSQL(sql);

                    sql = " UPDATE backorder_register SET percent_retention = " +
                          " (SELECT percent " +
                          " FROM " + prefDara + "upg_s_nedop_type u " +
                          " WHERE u.nzp_serv = backorder_register.unit_id " +
                            " AND u.num_nedop = backorder_register.re_delivery_type " +
                            " AND u.num_nedop BETWEEN 2001 AND 2100) ";
                    ExecSQL(sql);
                }

                #endregion

                MyDataReader reader;
                

                sql = " SELECT * FROM backorder_register";
                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    #region заполнение списка полей секции

                    var fields = new FieldsUnload
                    {
                        F =
                            new List<Field>
                              {
                                  AddField("RegAccount",          "РИК лицевого счета",                   0, 1,  "TextType",    25, reader["reg_account"]),
                                  AddField("AccountIDSys",        "№ ЛС в\nсистеме отправителя",          1, 2,  "IntType",     18, reader["account_id_sys"]),
                                  AddField("UnitID",              "Код\nуслуги",                          0, 3,  "IntType",     3,  reader["unit_id"]),
                                  AddField("ReDeliveryType",      "Тип недопоставки",                     1, 4,  "IntType",     4,  reader["re_delivery_type"]),
                                  AddField("Temperature",         "Температура (для определенных услуг)", 0, 5,  "DecimalType", 18, reader["temperature"]),
                                  AddField("ReDeliveryStartDate", "Дата начала недопоставки",             1, 6,  "DateType",    20, reader["re_delivery_start_date"]),
                                  AddField("ReDeliveryEndDate",   "Дата окончания недопоставки",          1, 7,  "DateType",    20, reader["re_delivery_end_date"]),
                                  AddField("ReDeliveryAmount",    "Сумма недопоставки",                   0, 8,  "DecimalType", 18, reader["re_delivery_amount"]),
                                  AddField("PercentRetention",    "Процент удержания",                    0, 9,  "DecimalType", 18, reader["percent_retention"])
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
                MonitorLog.WriteLog("BackorderRegister.Start(pref): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
            string sql = " CREATE TEMP TABLE backorder_register( " +
                            " id SERIAL, " +
                            " type_section INTEGER, " +
                            " name CHARACTER(50), " +
                            " reg_account CHAR(25), " +
                            " account_id_sys BIGINT, " +
                            " unit_id INTEGER, " +
                            " re_delivery_type INTEGER, " +
                            " temperature " + DBManager.sDecimalType + "(14,2), " +
                            " re_delivery_start_date DATE, " +
                            " re_delivery_end_date DATE, " +
                            " re_delivery_amount " + DBManager.sDecimalType + "(14,2), " +
                            " percent_retention " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        /// <summary> Удаление временных таблиц </summary>
        public override void DropTempTable() {
            ExecSQL(" DROP TABLE backorder_register ");
        }
    }
}
