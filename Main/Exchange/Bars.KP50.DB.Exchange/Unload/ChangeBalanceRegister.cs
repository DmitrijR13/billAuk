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
    public class ChangeBalanceRegister : BaseUnload20
    {
        /// <summary> Наименование тега секции </summary>
        public override string Name {
            get { return "ChangeBalanceRegister"; }
        }

        /// <summary> Наименование секции </summary>
        public override string NameText {
            get { return "Реестр перекидок(изменения сальдо)"; }
        }

        /// <summary> Номер секции </summary>
        public override int Code {
            get { return 23; }
        }

        /// <summary> Список полей секции </summary>
        public override List<FieldsUnload> Data { get; set; }

        /// <summary> Выборка по всем банкам данных </summary>
        public override void Start()
        {
           
        }

        /// <summary> Выборка по банкам данных(территориям) </summary>
        /// <param name="pref">Префикс банка данных(территории)</param>
        public override void Start(string pref) 
        {
            Data = new List<FieldsUnload>();
            OpenConnection();
            CreateTempTable();
            var tables = new DbTables(Connection);
            var sql = new StringBuilder();
            //Month = 6;
            //Year = 2014;
            try
            {
            string perekidka = pref + "_charge_" + (Year%100).ToString("00") + DBManager.tableDelimiter + "perekidka";

            string area = GetNzpArea(ListNzpArea);
            string whereArea = "";
            if (area != String.Empty)
            {
                whereArea = " and k.nzp_area in (" + area + " )";
            }

            if (TempTableInWebCashe(perekidka))
            {
                sql.Remove(0, sql.Length);
                sql.AppendFormat(" INSERT INTO {0} (type_section, name, account_id_sys, unit_id, ", Name);
                sql.Append(" contrac_sender_id, balance_id, balance_sum, tarif, volume, ");
                sql.Append(" comment, doc_name, doc_number, doc_date) ");
                sql.AppendFormat(" SELECT {0}, '{1}', p.num_ls, nzp_serv,", Code, Name);
                sql.Append(" nzp_supp, type_rcl, sum_rcl, tarif, volum, ");
                sql.Append(" d.comment, td.doc_name, d.num_doc, d.dat_doc");
                sql.AppendFormat(" from {0} p left outer join {2} d on p.nzp_doc_base = d.nzp_doc_base , {1} k, {3} td ", perekidka, pref + DBManager.sDataAliasRest + "kvar", tables.document_base, tables.s_type_doc);
                sql.AppendFormat(" where  d.nzp_type_doc = td.nzp_type_doc and p.month_ = {0} and nzp_serv < 1007568 and p.nzp_kvar = k.nzp_kvar {1}", Month, whereArea);
                ExecSQL(sql.ToString());
            }
            else AddComment("Нет таблицы перекидка " + perekidka);
          
            MyDataReader reader;
            
            sql.Remove(0, sql.Length);
            sql.AppendFormat(" SELECT * FROM {0}", Name);
            ExecRead(out reader, sql.ToString());

            while (reader.Read())
            {
                #region заполнение списка полей секции

                var fields = new FieldsUnload
                {
                    F =
                        new List<Field>
                        {
                            AddField("RegAccount",      "РИК лицевого счета",                            0, 1,  "TextType",    25,  reader["reg_account"]),
                            AddField("AccountIDSys",    "№ ЛС в\nсистеме отправителя",                   1, 2,  "IntType",     25,  reader["account_id_sys"]),
                            AddField("UnitID",          "Код\nуслуги",                                   1, 3,  "IntType",     3,   reader["unit_id"]),
                            AddField("ContracSenderID", "Уникальный код договора в\nсистеме поставщика", 1, 4,  "IntType",     3,   reader["contrac_sender_id"]),
                            AddField("BalanceID",       "Код типа\nперекидки",                           1, 5,  "IntType",     3,   reader["balance_id"]),
                            AddField("BalanceSum",      "Сумма\nперекидки",                              1, 6,  "DecimalType", 3,   reader["balance_sum"]),
                            AddField("Tarif",           "Тариф",                                         1, 7,  "DecimalType", 3,   reader["tarif"]),
                            AddField("Volume",          "Расход",                                        1, 8,  "DecimalType", 3,   reader["volume"]),
                            AddField("Comment",         "Комментарий",                                   1, 9,  "TextType",    100, reader["comment"]),
                            AddField("DocName",         "Наименование\nдокумента",                       1, 10, "TextType",    100, reader["doc_name"]),
                            AddField("DocNumber",       "Номер\nдокумента",                              1, 11, "TextType",    20,  reader["doc_number"]),
                            AddField("DocDate",         "Дата\nдокумента",                               1, 12, "DateType",    20,  reader["doc_date"])
                        }
                };
                #endregion
                Data.Add(fields);
            }

            }
            catch (Exception ex)
            {
                AddComment("Ошибка выгрузки секции " + NameText + "." + ((Data == null || Data.Count == 0) ? " Данные не выгружены." : ""));
                MonitorLog.WriteLog("ChangeBalanceRegister.Start(): Ошибка добавление записи в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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
            string sql = " CREATE TEMP TABLE " + Name + "( " +
                               " id SERIAL NOT NULL, " +
                               " type_section INTEGER , " +
                               " name CHARACTER(50), " +
                               " reg_account CHARACTER(25), " +
                               " account_id_sys INTEGER, " +
                               " unit_id INTEGER, " +
                               " contrac_sender_id INTEGER, " +
                               " balance_id INTEGER, " +
                               " balance_sum " + DBManager.sDecimalType + "(14,2), " +
                               " tarif " + DBManager.sDecimalType + "(14,4), " +
                               " volume " + DBManager.sDecimalType + "(14,4), " +
                               " comment CHARACTER(100), " +
                               " doc_name CHARACTER(100), " +
                               " doc_number CHARACTER(20), " +
                               " doc_date DATE, " +
                               " format_version CHARACTER(25)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        /// <summary> Удаление временных таблиц </summary>
        public override void DropTempTable() {
            ExecSQL("DROP TABLE " + Name);
        }

    }
}
