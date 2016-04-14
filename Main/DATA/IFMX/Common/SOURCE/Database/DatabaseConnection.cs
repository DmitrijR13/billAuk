using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;
using System.Reflection;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Represents an open connection to a data source, and is implemented by .NET
    /// Framework data providers that access relational databases.
    /// (!) Все методы базового класса перекрыты для последующего отказа от него.
    /// (!) При отказе заменить MyDbConnection -> MarshalByRefObject
    /// </summary>
    public partial class DatabaseConnection : MyDbConnection, IDbConnection
    {
        /// <summary>
        /// Сиквенс для выдачи идентификатора подключения
        /// </summary>
        private static long _sequence = -1;

        /// <summary>
        /// Realy connection
        /// </summary>
        private readonly IDbConnection _connection = null;

        /// <summary>
        /// Уникальный идентификатор подключения
        /// </summary>
        public long Identifier { get; private set; }

        /// <summary>
        /// Instance of real connection
        /// </summary>
        public new IDbConnection RealConnection { get { return _connection; } }

        /// <summary>
        /// Gets or sets the string used to open a database.
        /// </summary>
        public new string ConnectionString
        {
            get { return _connection.ConnectionString; }
            set { _connection.ConnectionString = value; }
        }

        /// <summary>
        /// Gets the time to wait while trying to establish a connection before terminating
        /// the attempt and generating an error.
        /// </summary>
        public new int ConnectionTimeout
        {
            get { return _connection.ConnectionTimeout; }
        }

        /// <summary>
        /// Gets the name of the current database or the database to be used after a
        /// connection is opened.
        /// </summary>
        public new string Database
        {
            get { return _connection.Database; }
        }

        /// <summary>
        /// Gets the current state of the connection.
        /// </summary>
        public new ConnectionState State
        {
            get { return _connection.State; }
        }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <returns>An object representing the new transaction.</returns>
        public new IDbTransaction BeginTransaction()
        {
            return _connection.BeginTransaction();
        }

        /// <summary>
        /// Begins a database transaction with the specified IsolationLevel value.
        /// </summary>
        /// <param name="il">One of the IsolationLevel values.</param>
        /// <returns>An object representing the new transaction.</returns>
        public new IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return _connection.BeginTransaction(il);
        }

        /// <summary>
        /// Changes the current database for an open Connection object.
        /// </summary>
        /// <param name="databaseName">The name of the database to use in place of the current database.</param>
        public new void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public new void Close()
        {
            UtcEraseTime = DateTime.UtcNow + TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Creates and returns a Command object associated with the connection.
        /// </summary>
        /// <returns>A Command object associated with the connection.</returns>
        public new IDbCommand CreateCommand()
        {
            Status.UtcActivity = DateTime.UtcNow;
            UtcEraseTime = null;
            return _connection.CreateCommand();
        }

        /// <summary>
        /// Opens a database connection with the settings specified by the ConnectionString
        /// property of the provider-specific Connection object.
        /// </summary>
        public new void Open()
        {
            _connection.Open();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public new void Dispose()
        {
            UtcEraseTime = DateTime.UtcNow + TimeSpan.FromMinutes(1);
        }


        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public new void RealClose()
        {
            if (Points.FullLogging)
                MonitorLog.WriteLog(" Закрытие БД " + _connection, MonitorLog.typelog.Info, 1, 1, true);
            _connection.Close();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public void RealDispose()
        {
            _connection.Dispose();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return string.Format("Идентификатор подключения: {0}\nState: {1}\n{2}", Identifier, State, Status);
        }

        /// <summary>
        /// Represents an open connection to a data source, and is implemented by .NET
        /// Framework data providers that access relational databases.
        /// </summary>
        /// <param name="instance">Instance of realy connection</param>
        private DatabaseConnection(IDbConnection instance) :
            base(instance)
        {
            Identifier = ++_sequence;
            _connection = instance;
        }

        /// <summary>
        /// Создает подключение к СУБД используя указанный тип
        /// </summary>
        /// <param name="ConnectionType">Тип реального подключения к СУБД</param>
        /// <returns>Подключение к СУБД</returns>
        public static IDbConnection CreateConnection(Type ConnectionType, string connectionString)
        {
            if (ConnectionType == null) throw new ArgumentNullException();
            if (!ConnectionType.IsClass) throw new TypeLoadException();


            return new DatabaseConnection(
                ConnectionType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public, null,
                    new Type[] { typeof(string) },
                    new ParameterModifier[] { }).
                Invoke(new object[] { connectionString }) as IDbConnection) as IDbConnection;
        }

        /// <summary>
        /// Создает подключение к СУБД используя указанный тип
        /// </summary>
        /// <typeparam name="TRealyConnection">Тип реального подключения к СУБД</typeparam>
        /// <returns>Подключение к СУБД</returns>
        public static IDbConnection CreateConnection<TRealyConnection>(string connectionString)
            where TRealyConnection : class, IDbConnection, new()
        {
            return new DatabaseConnection(
                typeof(TRealyConnection).GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public, null,
                    new Type[] { typeof(string) },
                    new ParameterModifier[] { }).
                Invoke(new object[] { connectionString }) as IDbConnection) as IDbConnection;
        }
    }
}
