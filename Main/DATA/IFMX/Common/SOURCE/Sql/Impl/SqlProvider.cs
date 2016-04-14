namespace STCLINE.KP50.DataBase.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using global::Bars.KP50.Utils;

    /// <summary>
    /// Реализация провайдера для выполнения sql запросов
    /// Особеность: 
    /// 1. После создания подключения использует его для всех запросов;
    /// 2. Соединения с БД закрываются при уничтожении экземпляра SqlProvider;
    /// </summary>
    public class SqlProvider : ISqlProvider
    {
        protected Dictionary<string, IDbConnection> Connections { get; set; }

        /// <summary>Создать sql команду</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="connectionString">Строка подключения</param>
        /// <returns>Sql команда</returns>
        public SqlCommand CreaSqlCommand(string sql, string connectionString)
        {
            return SqlCommand.CreateCommand(sql, connectionString);
        }

        /// <summary>Выполнить запрос</summary>
        /// <param name="sqlCommand">Sql команда</param>
        public void ExecuteNonQuery(SqlCommand sqlCommand)
        {
            OpenConnection(sqlCommand, connection => ExecuteCommand(sqlCommand, connection, command => command.ExecuteNonQuery()));
        }

        /// <summary>Получить результат выполнения запроса в виде списка</summary>
        /// <param name="sqlCommand">Sql команда</param>
        /// <returns>Список значение</returns>
        public IList<object[]> List(SqlCommand sqlCommand)
        {
            var list = new List<object[]>();
            OpenConnection(
                sqlCommand,
                connection => ExecuteCommand(
                    sqlCommand,
                    connection,
                    command => OpenReader(
                        command,
                        reader =>
                        {
                            while (reader.Read())
                            {
                                var array = new object[reader.FieldCount];
                                reader.GetValues(array);
                                list.Add(array);
                            }
                        })));

            return list;
        }

        /// <summary>Получить результат выполнения запроса в виде таблицы</summary>
        /// <param name="sqlCommand">Sql команда</param>
        /// <returns>Таблица</returns>
        public DataTable GetDataTable(SqlCommand sqlCommand)
        {
            var dataTable = new DataTable();
            OpenConnection(
                sqlCommand,
                connection => ExecuteCommand(
                    sqlCommand,
                    connection,
                    command => OpenReader(command, dataTable.Load)));

            return dataTable;
        }

        /// <summary>Выполнить единичную операцию</summary>
        /// <typeparam name="T">Тип возвращаемого значения</typeparam>
        /// <param name="sqlCommand">Sql команда</param>
        /// <returns>Результат выполнения</returns>
        public T ExecuteScalar<T>(SqlCommand sqlCommand)
        {
            object result = null;
            OpenConnection(
                sqlCommand,
                connection => ExecuteCommand(
                    sqlCommand,
                    connection,
                    command =>
                    {
                        result = command.ExecuteScalar();
                    }));

            return result.To<T>();
        }

        /// <summary>Выполнить вставку</summary>
        /// <param name="sqlCommand">Sql команда</param>
        /// <returns>Идентификатор записи</returns>
        public int Insert(SqlCommand sqlCommand)
        {
            var id = 0;
            OpenConnection(
                sqlCommand,
                connection => ExecuteCommand(
                    sqlCommand,
                    connection,
                    command =>
                    {
                        command.ExecuteNonQuery();
                        id = DBManager.GetSerialValue(connection);
                    }));

            return id;
        }

        public void Dispose()
        {
            if (Connections != null)
            {
                foreach (var connection in Connections.Values)
                {
                    connection.Close();
                }
            }
        }

        protected virtual IDbConnection GetConnection(string connectionString)
        {
            var connection = DBManager.GetConnection(connectionString);
            var result = DBManager.OpenDb(connection, true);
            if (!result.result)
            {
                throw new SqlException(string.Format("Не удалось открыть соединение с БД: \"{0}\"", result.text));
            }

            return connection;
        }

        protected IDbCommand CreateCommand(SqlCommand sqlCommand, IDbConnection connection)
        {
            var command = DBManager.newDbCommand(sqlCommand.Sql, connection);
            command.CommandTimeout = 300;

            foreach (var parameter in sqlCommand.Parameters)
            {
                var param = command.CreateParameter();
                param.ParameterName = parameter.Key;
                param.Value = parameter.Value;
                command.Parameters.Add(param);
            }

            return command;
        }

        protected virtual void OpenReader(IDbCommand command, Action<IDataReader> action)
        {
            IDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                
                action(reader);
            }
            catch (SqlException)
            {
                throw;
            }
            catch (Exception exc)
            {
                throw new SqlException("Не удалось выполнить запрос", exc);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        protected virtual void ExecuteCommand(SqlCommand sqlCommand, IDbConnection connection, Action<IDbCommand> action)
        {
            IDbCommand command = null;
            try
            {
                command = CreateCommand(sqlCommand, connection);

                action(command);
            }
            catch (SqlException)
            {
                throw;
            }
            catch (Exception exc)
            {
                throw new SqlException(string.Format("Не удалось выполнить запрос: \"{0}\"", sqlCommand.Sql), exc);
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }
            }
        }

        protected virtual void OpenConnection(SqlCommand sqlCommand, Action<IDbConnection> action)
        {
            try
            {
                if (Connections == null)
                {
                    Connections = new Dictionary<string, IDbConnection>();
                }

                IDbConnection connection;
                if (Connections.ContainsKey(sqlCommand.ConnectionString))
                {
                    connection = Connections[sqlCommand.ConnectionString];
                }
                else
                {
                    connection = GetConnection(sqlCommand.ConnectionString);
                    Connections.Add(sqlCommand.ConnectionString, connection);
                }
                
                action(connection);
            }
            catch (SqlException)
            {
                throw;
            }
            catch (Exception exc)
            {
                throw new SqlException("Произошла не известная ошибка при выполнении", exc);
            }
        }
    }
}