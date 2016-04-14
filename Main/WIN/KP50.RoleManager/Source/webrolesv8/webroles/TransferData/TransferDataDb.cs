using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace webroles.TransferData
{
    class TransferDataDb:IDisposable
    {   
        public static NpgsqlDataReader ExecuteReader(string sqlRequest, out Returns ret, IDbConnection connection)
        {
            NpgsqlDataReader reader = null;
            NpgsqlCommand command = new NpgsqlCommand(sqlRequest,(NpgsqlConnection)connection);
            ret = new Returns(true, "");
            try
            {
               // connection.Open();
                reader = command.ExecuteReader();
                return reader;
            }
            catch (NpgsqlException except)
            {
                ret.Result = false;
                ret.SqlError = "Ошибка " + except.ErrorCode.ToString() + ". " + except.Message;
                MessageBox.Show(ret.SqlError, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                connection.Close();
                if (reader != null && !reader.IsClosed) reader.Close();
                return null;
            }
        }

        public static void ExecuteScalar(string sqlRequest, out Returns ret)
        {
            ret.Result = true;
            ret.SqlError = "";
            NpgsqlConnection connection = ConnectionToPostgreSqlDb.GetConnection();
            NpgsqlCommand command = new NpgsqlCommand(sqlRequest, connection);
            try
            {
                connection.Open();
                command.ExecuteScalar();
                // Проверка логина на уникальность
            }
            catch (NpgsqlException except)
            {
                ret.Result = false;
                ret.SqlError = "Ошибка " + except.ErrorCode.ToString() + ". " + except.Message;
                MessageBox.Show(ret.SqlError, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            finally
            {
                connection.Close();
            }
        }

        public static Returns ExecSQL(string sqlRequest)
        {
            Returns ret = new Returns();
            ret.Result = true;
            ret.SqlError = "";
            NpgsqlConnection connection = ConnectionToPostgreSqlDb.GetConnection();
            NpgsqlCommand command = new NpgsqlCommand(sqlRequest, connection);
            try
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (NpgsqlException except)
            {
                ret.Result = false;
                ret.SqlError = "Ошибка " + except.ErrorCode + ". " + except.Message;
                MessageBox.Show(ret.SqlError, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            finally
            {
                connection.Close();
            }
            return ret;
        }

        public static Returns ExecSQL(string sqlRequest, IDbConnection connection)
        {
            Returns ret = new Returns();
            ret.Result = true;
            ret.SqlError = "";
            NpgsqlCommand command = new NpgsqlCommand(sqlRequest,(NpgsqlConnection) connection);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (NpgsqlException except)
            {
                ret.Result = false;
                ret.SqlError = "Ошибка " + except.ErrorCode + ". " + except.Message;
                MessageBox.Show(ret.SqlError, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return ret;
        }

        public void Dispose()
        {
           
        }

        public static bool UpdateDbTable(NpgsqlDataAdapter adapter, DataTable dataTable)
        {
            try
            {
                if (adapter.DeleteCommand != null) adapter.Update(dataTable.Select(null, null, DataViewRowState.Deleted));
                if (adapter.UpdateCommand != null) adapter.Update(dataTable.Select(null, null, DataViewRowState.ModifiedCurrent));
                var n = dataTable.Select(null, null, DataViewRowState.Added);
                if (adapter.InsertCommand != null) adapter.Update(dataTable.Select(null, null, DataViewRowState.Added));
                //adapter.Dispose();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show("Сообщение PostgreSql: " + ex.Message);
                return  false;
            }
            return true;
        }

        public static bool Fill(NpgsqlDataAdapter adapter, DataSet dataSet)
        {
            try
            {
                adapter.Fill(dataSet);
            }

            catch (NpgsqlException exc)
            {
                MessageBox.Show(exc.Message);
                return false;
            }
            return true;
        }

        public static void Fill(NpgsqlDataAdapter adapter, DataTable dataTable)
        {
            try
            {
                adapter.Fill(dataTable);
            }

            catch (NpgsqlException exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
    }
}
