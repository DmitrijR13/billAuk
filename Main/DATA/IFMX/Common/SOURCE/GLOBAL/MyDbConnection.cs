using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    using System;
    using System.Data;

    using IBM.Data.Informix;
    using Npgsql;
    using STCLINE.KP50.Global;

    public class MyDbConnection: IDbConnection
    {
        IDbConnection _connection;

        public IDbConnection RealConnection
        {
            get { return _connection; }
        }

        public MyDbConnection(IDbConnection connection)
        {
            _connection = connection;
        }

        public void RealClose()
        {
            if (Points.FullLogging)
            {

                MonitorLog.WriteLog(" Закрытие БД " + _connection, MonitorLog.typelog.Info, 1, 1, true);
            }

            _connection.Close();
        }

        // Summary:
        //     Gets or sets the string used to open a database.
        //
        // Returns:
        //     A string containing connection settings.
        public string ConnectionString
        {
            get
            {
                return _connection.ConnectionString;
            }
            set {
                _connection.ConnectionString = value;
            }
        }

        //
        // Summary:
        //     Gets the time to wait while trying to establish a connection before terminating
        //     the attempt and generating an error.
        //
        // Returns:
        //     The time (in seconds) to wait for a connection to open. The default value
        //     is 15 seconds.
        public int ConnectionTimeout { get { return _connection.ConnectionTimeout; } }

        //
        // Summary:
        //     Gets the name of the current database or the database to be used after a
        //     connection is opened.
        //
        // Returns:
        //     The name of the current database or the name of the database to be used once
        //     a connection is open. The default value is an empty string.
        public string Database { get { return _connection.Database; } }

        //
        // Summary:
        //     Gets the current state of the connection.
        //
        // Returns:
        //     One of the System.Data.ConnectionState values.
        public ConnectionState State { get { return _connection.State; } }

        // Summary:
        //     Begins a database transaction.
        //
        // Returns:
        //     An object representing the new transaction.
        public IDbTransaction BeginTransaction()
        {
            return _connection.BeginTransaction();
        }

        //
        // Summary:
        //     Begins a database transaction with the specified System.Data.IsolationLevel
        //     value.
        //
        // Parameters:
        //   il:
        //     One of the System.Data.IsolationLevel values.
        //
        // Returns:
        //     An object representing the new transaction.
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return _connection.BeginTransaction(il);
        }

        //
        // Summary:
        //     Changes the current database for an open Connection object.
        //
        // Parameters:
        //   databaseName:
        //     The name of the database to use in place of the current database.
        public void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        //
        // Summary:
        //     Не закрывает соединение
        public void Close()
        { 

        }

        //
        // Summary:
        //     Creates and returns a Command object associated with the connection.
        //
        // Returns:
        //     A Command object associated with the connection.
        public IDbCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }

        //
        // Summary:
        //     Opens a database connection with the settings specified by the ConnectionString
        //     property of the provider-specific Connection object.
        public void Open()
        {
            _connection.Open();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
