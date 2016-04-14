using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Пул соединений с СУБД
    /// </summary>
    internal class DatabaseConnectionPool : IDisposable
    {
        /// <summary>
        /// Идентификатор отслеживающего потока
        /// </summary>
        internal int ManagmentThreadId { get; private set; }

        /// <summary>
        /// Время жизни неактивного подключения
        /// </summary>
        internal TimeSpan ConnectionLifeTimeout { get; private set; }

        /// <summary>
        /// Время блокировки соединения
        /// </summary>
        public TimeSpan LockTimeout { get; protected internal set; }

        /// <summary>
        /// Тип подключения к СУБД
        /// </summary>
        internal readonly Type _connectionType = null;

        /// <summary>
        /// Список созданных подключений
        /// </summary>
        internal readonly IList<DatabaseConnection> _connections = new List<DatabaseConnection>();

        /// <summary>
        /// Мьютекс
        /// </summary>
        private readonly Mutex _mutex = new Mutex();

        /// <summary>
        /// Запрошена ли сборка объекта
        /// </summary>
        internal bool DisposeRequired { get; private set; }

        /// <summary>
        /// Строка подключения
        /// </summary>
        internal string ConnectionString { get; private set; }

        /// <summary>
        /// Получить подключение к СУБД
        /// </summary>
        /// <returns>Подключение к СУБД</returns>
        internal IDbConnection GetConnection()
        {
            if (DisposeRequired) throw new ObjectDisposedException("Database connection pool is disposed.");
            _mutex.WaitOne();
            var connection = _connections.FirstOrDefault(
                x => x.Status.ConnectionStatus == ConnectionStatus.InUse &&
                     x.Status.ThreadId == Thread.CurrentThread.ManagedThreadId) ??
                _connections.Where(x => x.Status.ConnectionStatus == ConnectionStatus.Free).
                    OrderBy(x => x.Status.UtcActivity).FirstOrDefault();
            if (connection == null)
            {
                connection = DatabaseConnection.CreateConnection(_connectionType, ConnectionString) as DatabaseConnection;
                _connections.Add(connection);
            }

            try
            {
                ValidateConnection(connection);
                connection.Status = new DatabaseConnectionStatus
                {
                    ConnectionStatus = ConnectionStatus.InUse,
                    ThreadId = Thread.CurrentThread.ManagedThreadId,
                    UtcActivity = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                _connections.Remove(connection);
                connection.RealClose();
                connection.RealDispose();
                connection = null;
                if (!_connections.Any())
                    Dispose();
            }
            _mutex.ReleaseMutex();
            return connection;
        }

        /// <summary>
        /// Валидирует подключение
        /// </summary>
        /// <param name="connection">Подключение</param>
        private void ValidateConnection(IDbConnection connection)
        {
            switch (connection.State)
            {
                case ConnectionState.Closed:
                    connection.Open();
                    break;
                case ConnectionState.Broken:
                    connection.Close();
                    connection.Open();
                    break;
                case ConnectionState.Executing:
                case ConnectionState.Fetching:
                    throw new InvalidOperationException("Подключение уже используется");
            }
        }

        /// <summary>
        /// Валидирует и убивает неиспользуемые подключения
        /// </summary>
        private void ValidatePool()
        {
            ThreadPool.UnsafeQueueUserWorkItem(delegate
            {
                ManagmentThreadId = Thread.CurrentThread.ManagedThreadId;
                while (!DisposeRequired)
                {
                    _mutex.WaitOne();

                    foreach (var connection in _connections.
                        Where(x => x.Status.ConnectionStatus == ConnectionStatus.InUse &&
                            (x.Status.UtcActivity + LockTimeout < DateTime.UtcNow ||
                            x.UtcEraseTime < DateTime.UtcNow)))
                    {
                        if (Points.FullLogging && connection.Status.UtcActivity + LockTimeout < DateTime.UtcNow)
                        {
                            var message = "Поток " + connection.Status.ThreadId +
                                " не освободил соединение. Время последнего обращения: " +
                                connection.Status.UtcActivity.ToLocalTime().ToString("HH:mm:ss") + ".";
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Warn, false);
                        }
                        connection.RealClose();
                        connection.UtcEraseTime = null;
                        connection.Status.ConnectionStatus = ConnectionStatus.Free;
                    }

                    var _disposed = _connections.
                        Where(x => x.Status.ConnectionStatus == ConnectionStatus.Free &&
                            x.Status.UtcActivity + ConnectionLifeTimeout < DateTime.UtcNow).
                        OrderByDescending(x => x.Status.UtcActivity).ToList();
                    foreach (var connection in _disposed)
                    {
                        _connections.Remove(connection);
                        connection.RealClose();
                        connection.RealDispose();
                    }

                    foreach (var connection in _disposed)
                        _connections.Remove(connection);
                    _disposed.Clear();

                    if (!_connections.Any())
                        Dispose();
                    _mutex.ReleaseMutex();
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            }, null);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DatabaseConnectionKernel.Instance.PoolDisposed(this);
            DisposeRequired = true;
            _mutex.WaitOne();
            foreach (var connection in _connections)
            {
                connection.RealClose();
                connection.RealDispose();
            }
            _connections.Clear();
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Пул соединений с СУБД
        /// </summary>
        /// <param name="connectionType">Базовый класс подключения к СУБД</param>
        /// <param name="connectionString">Строка подключения</param>
        internal DatabaseConnectionPool(Type connectionType, string connectionString)
        {
            _connectionType = connectionType;
            ConnectionString = connectionString;
            DisposeRequired = false;
            LockTimeout = TimeSpan.FromMinutes(15);
            ConnectionLifeTimeout = TimeSpan.FromMinutes(30);
            ValidatePool();
        }
    }
}
