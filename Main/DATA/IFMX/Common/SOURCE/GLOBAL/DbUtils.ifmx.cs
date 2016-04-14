using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.DataBase
{
    public class DBUtils : DataBaseHead
    {
        public Returns SaveDataTable(IDbConnection connection, Finder finder, string tableName, DataTable dt)
        {
            if (dt == null) return new Returns(false, "Таблица не задана");
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не задан");
            if (tableName.Trim() == "") return new Returns(false, "Имя таблицы не задано");
       
            string tXX = "t" + Convert.ToString(finder.nzp_user) + "_" + tableName;

            if (TableInWebCashe(connection, tXX))
            {
                ExecSQL(connection, " Drop table " + sDefaultSchema + tXX, false);
            }

            string sql = "Create table " + sDefaultSchema + tXX + " (id serial not null";
            foreach (DataColumn column in dt.Columns)
            {
                if (column.DataType.FullName == typeof(Int32).FullName)
                {
                    sql += ", " + column.ColumnName + " integer";
                }
                else if (column.DataType.FullName == typeof(Decimal).FullName)
                {
                    sql += ", " + column.ColumnName + " decimal(" + column.ExtendedProperties["length"].ToString() + "," + column.ExtendedProperties["scale"].ToString() + ")";
                }
                else if (column.DataType.FullName == typeof(String).FullName)
                {
                    sql += ", " + column.ColumnName + " char(" + column.MaxLength + ")";
                }
                else if (column.DataType.FullName == typeof(Boolean).FullName)
                {
#if PG
                    sql += ", " + column.ColumnName + " boolean";
#else
                    sql += ", " + column.ColumnName + " byte";
#endif
                }
                else if (column.DataType.FullName == typeof(DateTime).FullName)
                {
                    sql += ", " + column.ColumnName + " date";
                }
                else if (column.DataType.FullName == typeof(Double).FullName)
                {
                    sql += ", " + column.ColumnName + " float";
                }
            }
            sql += ")";

            Returns ret = ExecSQL(connection, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            string fields = "", values = "";
            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    fields = "id";
                    values = "0";

                    foreach (DataColumn column in dt.Columns)
                    {
                        object value = row[column];
                        if (value == DBNull.Value) continue;
                        fields += ", " + column.ColumnName;
                        values += ",";
                        if (column.DataType.FullName == typeof(Int32).FullName)
                        {
                            values += (int)value;
                        }
                        else if (column.DataType.FullName == typeof(Decimal).FullName)
                        {
                            values += (decimal)value;
                        }
                        else if (column.DataType.FullName == typeof(String).FullName)
                        {
                            values += Utils.EStrNull((string)value, "");
                        }
                        else if (column.DataType.FullName == typeof(Boolean).FullName)
                        {
                            values += (bool)value ? "1" : "0";
                        }
                        else if (column.DataType.FullName == typeof(DateTime).FullName)
                        {
                            values += Utils.EStrNull(((DateTime)value).ToShortDateString(),"");
                        }
                        else if (column.DataType.FullName == typeof(Double).FullName)
                        {
                            values += (double)value;
                        }
                    }

                    sql = "insert into " + sDefaultSchema + tXX + " (" + fields + ") values (" + values + ")";
                    ret = ExecSQL(connection, sql, true);
                    if (!ret.result) break;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                /*if (ret.text.Contains("Inexact character conversion during translation"))
                {
                    ret.text = "В файле не указана или ";
                }*/

                MonitorLog.WriteLog("Ошибка в функции SaveDataTable: " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            return ret;
        }
    }
}
