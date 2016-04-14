using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Admin.Source.OrderSequence
{
    /// <summary>
    /// Класс функционала упорядочивания последовательностей таблиц БД
    /// </summary>
    public class OrderingSequence:DataBaseHead
    {
        /// <summary>
        /// Смещает последовательность в случае, если max id колонки, 
        /// на которой установлена последовательность, больше текущего значения последовательности
        /// </summary>
        /// <returns></returns>
        public Returns DoOrderSequences()
        {
            IDbConnection connection = GetConnection();
            Returns ret = Utils.InitReturns();
            ret = OpenDb(connection, true);
            if (!ret.result) return ret;

            #region подключение к базе

            IDataReader readerSchema = null;
            IDataReader readerRelated = null;
            #endregion

            string table = "";
            string column = "";
            string sequence = "";
            string schema = "";
            try
            {
                // извлекаем все схемы
                var sql =
                    "select schema_name from information_schema.schemata where schema_name not like 'pg%' order by schema_name";
                ret = ExecRead(connection, out readerSchema, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
                DataTable schemasList = new DataTable();
                schemasList.Load(readerSchema);
                foreach (DataRow dr in schemasList.Rows)
                {
                    if (dr["schema_name"] == DBNull.Value) continue;
                    schema = dr["schema_name"].ToString();
                    if (schema == "information_schema") continue;
                    // Для каждой схемы
                    // извлекаем связку : таблица, колонка, на которой уствновлена последовательность, наименование последовательности 
                    sql = "SELECT t.relname as related_table, " +
                          "a.attname as related_column, " +
                          "s.relname as sequence_name " +
                          "FROM pg_class s  " +
                          "JOIN pg_depend d ON d.objid = s.oid  " +
                          "JOIN pg_class t ON d.objid = s.oid AND d.refobjid = t.oid " +
                          "JOIN pg_attribute a ON (d.refobjid, d.refobjsubid) = (a.attrelid, a.attnum) " +
                          "JOIN pg_namespace n ON n.oid = s.relnamespace  " +
                          "WHERE s.relkind = 'S' " +
                          "AND n.nspname = '" + schema + "'";
                    ret = ExecRead(connection, out readerRelated, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    DataTable relatedTable = new DataTable();
                    relatedTable.Load(readerRelated);
                    foreach (DataRow drlat in relatedTable.Rows)
                    {
                        if (drlat["related_table"] == DBNull.Value) continue;
                        if (drlat["related_column"] == DBNull.Value) continue;
                        if (drlat["sequence_name"] == DBNull.Value) continue;
                        table = drlat["related_table"].ToString();
                        column = drlat["related_column"].ToString();
                        sequence = drlat["sequence_name"].ToString();
                        // эту таблицу нужно пропустить, т.к. у нее первичный ключ типа varchar
                        if (table == "file_perekidki" && column == "id_ls") continue;
                        //сместить последовательность
                        sql = "SELECT setval ('" + schema + DBManager.tableDelimiter + sequence + "',  (select GREATEST ((SELECT last_value FROM " + schema + DBManager.tableDelimiter + sequence + ") ," +
                              "(select " + DBManager.sNvlWord + "(max(" + column + "), 0) from " + schema + DBManager.tableDelimiter + table + "))))";
                        ret = ExecRead(connection, out readerSchema, sql, true);
                        if (!ret.result)
                        {
                            return ret;
                        }
                        //------Не удалять. Этот код для тестирования
                        //sql = "select  (SELECT last_value FROM " + schema + DBManager.tableDelimiter + sequence + ") >= " +
                        //      "(select " + DBManager.sNvlWord + "(max(" + column + "),'0')::bigint from " + schema + DBManager.tableDelimiter + table + ")";
                        //bool res = (bool)ExecScalar(connection, sql, out ret, true);
                        //if (!ret.result)
                        //{
                        //    return ret;
                        //}
                        //if (!res)
                        //{
                        //    ret.result = false;
                        //    ret.text = " Последовательность не сдвинулась. Схема " + schema + ", таблица " + table + ", колонка " + column + ", последовательность " + sequence;
                        //    return ret;
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка упорядочивания последовательностей: \n" + ex.Message + "Схема " + schema + ", таблица " + table + ", колонка " + column + ", последовательность " + sequence;
                MonitorLog.WriteLog("Ошибка упорядочивания последовательностей: \n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                return ret;
            }
            finally
            {
                if (readerSchema != null)
                {
                    readerSchema.Close();
                }
                if (readerRelated != null)
                {
                    readerRelated.Close();
                }
                if (connection != null)
                {
                    connection.Close();
                }
            }
            return ret;
        }
    }
}
