using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using webroles.TransferData;

namespace webroles
{
    public static class DBManager
    {
        public const string sKernelAliasRest = "_kernel.";
        public const string sDataAliasRest = "_data.";
        public const string sUploadAliasRest = "_upload.";
        public const string sSupgAliasRest = "_supg.";
        public const string sDebtAliasRest = "_debt.";
        public const string tbluser = "";
        public const string sDecimalType = "numeric";
        public const string sCharType = "character";
        public const string sUniqueWord = "distinct";
        public const string sNvlWord = "coalesce";
        public const string sConvToNum = "::numeric";
        public const string sConvToInt = "::int";
        public const string sConvToChar = "::character";
        public const string sConvToChar10 = "::character(10)";
        public const string sConvToVarChar = "::varchar";
        public const string sConvToDate = "::date";
        public const string sDefaultSchema = "public.";
        public const string s0hour = "interval '0 hour'";
        public const string sUpdStat = "analyze";
        public const string sCrtTempTable = "temp";
        public const string sUnlogTempTable = "";
        public const string sCurDate = "current_date";
        public const string sCurDateTime = "now()";
        public const string DateNullString = "Null::date";
        public const string sFirstWord = "limit";
        public const string sSerialDefault = "default";
        public const string sYearFromDate = "Extract(year from ";
        public const string sMonthFromDate = "Extract(month from ";
        public const string sDateTimeType = "timestamp";
        public const string sLockMode = "";
        public const string sMatchesWord = "similar to";
        public const string sRegularExpressionAnySymbol = "%";
        public const string Limit1 = " limit 1 ";

        public static bool CheckExistsRefernces(string sql, string[] tablesArr, int posit)
        {
            IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
            IDataReader reader = null;
            Returns ret = new Returns(true, "");
            try
            {
                connection.Open();
                reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
                if (!ret.Result)
                {
                    return false;
                }
                int i = 0;
                do
                {
                    while (reader.Read())
                    {
                        if ((bool) reader["exists"])
                        {
                            MessageBox.Show(
                                "В таблице " + tablesArr[i] + " еще имеются ссылки на группу №" + posit,
                                "Удаление невозможно", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        i++;
                    }
                } while (reader.NextResult());
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            finally
            {
                if (reader!=null) reader.Close();
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }

        }
    }
}
