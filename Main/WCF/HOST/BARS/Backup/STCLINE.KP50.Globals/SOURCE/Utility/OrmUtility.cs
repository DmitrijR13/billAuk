using System;
using System.Collections.Generic;
using System.IO;
using System.Data;

namespace STCLINE.KP50.Utility
{

    public static class OrmConvert
    {
        #region Utils

        public static List<TOutput> ConvertDataRows<TOutput>(DataRowCollection rows, Converter<DataRow, TOutput> converter)
        {
            List<TOutput> list = new List<TOutput>(0);
            foreach (DataRow dr in rows)
            {
                list.Add(converter(dr));
            }
            return list;
        }
        public static List<TOutput> ConvertDataRows<TOutput>(DataRow[] rows, Converter<DataRow, TOutput> converter)
        {
            List<TOutput> list = new List<TOutput>(0);
            foreach (DataRow dr in rows)
            {
                list.Add(converter(dr));
            }
            return list;
        }

        public static List<TOutput> ConvertDataReader<TOutput>(IDataReader reader, Converter<IDataRecord, TOutput> converter)
        {
            List<TOutput> list = new List<TOutput>(0);

            while (reader.Read())
            {
                list.Add(converter(reader));
            }
            return list;
        }

        #endregion
    }


        public static class DataConvert
        {
            #region Utils
            internal const string c_DateStringFormat = "dd.MM.yyyy";

            public static T FieldValue<T>(DataRow dr, string colName, T defaultValue)
            {
                return (dr[colName] != DBNull.Value ? dr.Field<T>(colName) : defaultValue);
            }
            public static Nullable<decimal> FieldDecimalValue(DataRow dr, string colName)
            {
                return (dr[colName] != DBNull.Value ? (decimal?)(dr.Field<decimal>(colName)) : null);
            }
            public static Nullable<DateTime> FieldDateTimeValue(DataRow dr, string colName)
            {
                return (dr[colName] != DBNull.Value ? (DateTime?)(dr.Field<DateTime>(colName)) : null);
            }
            public static string FieldDateString(DataRow dr, string dateTimeColName, string defaultValue)
            {
                return (dr[dateTimeColName] != DBNull.Value ?
                    dr.Field<DateTime>(dateTimeColName).ToString(c_DateStringFormat) :
                    defaultValue);
            }


            public static T FieldValue<T>(IDataRecord dr, string colName, T defaultValue)
            {
                return (dr[colName] != DBNull.Value ? (T)(dr[colName]) : defaultValue);
            }

            #endregion
        }
}
