using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bars.KP50.Utils.Extension
{
    public static class DataReaderExtension
    {
        public static IEnumerable<T> Select<T>(this IDataReader reader,
                                     Func<IDataReader, T> projection)
        {
            while (reader.Read())
            {
                yield return projection(reader);
            }
        }

        public static T ConvertedEntity<T>(this IDataReader dr) where T : new()
        {
            var t = typeof(T);
            var returnObject = new T();
            for (var i = 0; i < dr.FieldCount; i++)
            {
                var colName = string.Empty;
                colName = dr.GetName(i);
                var pInfo = t.GetProperty(colName.ToLower(), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (pInfo == null) continue;
                var val = dr[colName];
                var IsNullable = (Nullable.GetUnderlyingType(pInfo.PropertyType) != null);
                if (IsNullable)
                {
                    if (val is System.DBNull)
                    {
                        val = null;
                    }
                    else
                    {
                        val = Convert.ChangeType(val, Nullable.GetUnderlyingType(pInfo.PropertyType));
                    }
                }
                else
                {
                    val = Convert.ChangeType(val, pInfo.PropertyType);
                }
                pInfo.SetValue(returnObject, val, null);
            }
            return returnObject;
        }
    }
}
