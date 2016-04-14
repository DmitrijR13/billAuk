using System;
using System.Data;
using System.IO;
using Npgsql;

namespace SqlExecutor
{
    /// <summary>
    /// Класс для выполнения sql-запросов
    /// </summary>
    public  class SqlExecutor
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="connectionString">Строка подключения к БД</param>
        public SqlExecutor(string connectionString)
        {
            Connection = new NpgsqlConnection(connectionString);
        }

        /// <summary>
        /// Соединение с БД
        /// </summary>
        public IDbConnection Connection { get; private set; }

        /// <summary>
        /// Создание нового запроса 
        /// </summary>
        /// <param name="commandText">Текст команды</param>
        /// <param name="connection">Соединение</param>
        /// <param name="transaction">Транзакция</param>
        /// <param name="timeout">Таймаут времени выполнения</param>
        private IDbCommand NewDbCommand(string commandText, IDbConnection connection, IDbTransaction transaction,
            int timeout)
        {
            var dbCommand = connection.CreateCommand();
            dbCommand.CommandText = commandText;
            dbCommand.Connection = connection;
            dbCommand.Transaction = transaction;
            dbCommand.CommandTimeout = timeout;
            return dbCommand;
        }

        /// <summary>
        /// Открытие соединения с БД
        /// </summary>
        /// <returns></returns>
        private void OpenConnection()
        {
            try
            {
                switch (this.Connection.State)
                {
                    case ConnectionState.Closed:
                        Connection.Open();
                        break;
                    case ConnectionState.Broken:
                        Connection.Close();
                        Connection.Open();
                        break;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Ошибка при открытии соединения с БД! ", exception);
            }
        }

        /// <summary>
        /// Закрытие соединения с БД
        /// </summary>
        /// <returns></returns>
        private void CloseConnection()
        {
            try
            {
                if (this.Connection != null && this.Connection.State == ConnectionState.Open)
                {
                    Connection.Close();
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Ошибка при закрытии соединения с БД!", exception);
            }
        }

        /// <summary>
        /// Выполнение sql-запроса без транзакции
        /// </summary>
        /// <param name="sqlQuery">Текст sql-запроса</param>
        /// <returns></returns>
        public void ExecuteSql(string sqlQuery)
        {
            ExecuteSql(sqlQuery, this.Connection, null, 3600);
        }

        /// <summary>
        /// Выполнение sql-запроса
        /// </summary>
        /// <param name="sqlQuery">Текст sql-запроса</param>
        /// <param name="connection">Соединение</param>
        /// <param name="transaction">Транзакция</param>
        /// <param name="dbCommandTimeout">Таймаут выполнения</param>
        /// <returns></returns>
        private void ExecuteSql(string sqlQuery, IDbConnection connection, IDbTransaction transaction, int dbCommandTimeout)
        {
            try
            {
                OpenConnection();

                using (IDbCommand myCommand = NewDbCommand(sqlQuery, connection, transaction, dbCommandTimeout))
                {
                    myCommand.ExecuteNonQuery();
                }

            }
            catch (Exception exception)
            {
                throw new Exception(
                    String.Format(" Ошибка при выполнении sql-запроса! \n Текст sql-запроса: {0}", sqlQuery), exception);
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Передача потока напрямую в БД
        /// </summary>
        /// <param name="sqlQuery">Sql-запрос (copy ... from stdin )</param>
        /// <param name="stream">Поток</param>
        public void CopyIn(string sqlQuery, Stream stream)
        {
            OpenConnection();
            
            var copyIn = new NpgsqlCopyIn(new NpgsqlCommand(sqlQuery, (NpgsqlConnection)Connection), (NpgsqlConnection)Connection, stream);
            copyIn.Start();
            copyIn.End();
            CloseConnection();
        }

    }
}
