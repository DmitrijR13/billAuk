// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListExtension.cs" company="">
//   
// </copyright>
// <summary>
//   Extensions for List
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Bars.KP50.Utils
{
    using System.Collections;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// Extensions for List
    /// </summary>
    public static class ListExtension
    {
        /// <summary>
        /// The to data table.
        /// </summary>
        /// <param name="list">
        /// The list. 
        /// </param>
        /// <param name="colStrings">
        /// The col strings. 
        /// </param>
        /// <returns>
        /// </returns>
        public static DataTable ToDataTable(this IList list, string[] colStrings)
        {
            DataTable table = new DataTable();
            foreach (string col in colStrings)
            {
                table.Columns.Add(col, typeof(string));
            }

            foreach (object[] item in list)
            {
                DataRow row = table.NewRow();

                for (int i = 0; i < colStrings.Count(); i++)
                {
                    row[colStrings[i]] = item[i] ?? string.Empty;
                }

                table.Rows.Add(row);
            }

            return table;
        }
    }
}