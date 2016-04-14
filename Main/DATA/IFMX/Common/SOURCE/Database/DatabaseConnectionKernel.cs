using IBM.Data.Informix;
using Npgsql;
using STCLINE.KP50.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Фабрика подключений к СУБД
    /// </summary>
    public class DatabaseConnectionKernel : Singleton<DatabaseConnectionKernel>, IDisposable
    {
        /// <summary>
        /// Получает соединение из пула
        /// </summary>
        /// <param name="connectionString">Строка подключения к СУБД</param>
        /// <returns></returns>
        public static IDbConnection GetConnection(string connectionString)
        {
            return Instance.GetConnectionFromPool(connectionString);
        }

        /// <summary>
        /// Запрошена ли сборка объекта
        /// </summary>
        protected internal bool DisposeRequired { get; private set; }

        /// <summary>
        /// Пулы подключений к СУБД
        /// </summary>
        private readonly List<DatabaseConnectionPool> _pools = new List<DatabaseConnectionPool>();

        /// <summary>
        /// Мьютекс
        /// </summary>
        private readonly Mutex _mutex = new Mutex();

        /// <summary>
        /// Тип реального подключения к СУБД
        /// </summary>
        public Type RealConnectionType { get; protected internal set; }

        /// <summary>
        /// Получает соединение из пула
        /// </summary>
        /// <param name="connectionString">Строка подключения к СУБД</param>
        /// <returns></returns>
        protected internal IDbConnection GetConnectionFromPool(string connectionString)
        {
            _mutex.WaitOne();
            var pool = _pools.FirstOrDefault(x => x.ConnectionString == connectionString);
            if (pool == null)
                _pools.Add(pool = new DatabaseConnectionPool(RealConnectionType, connectionString));
            var connection = pool.GetConnection();
            _mutex.ReleaseMutex();
            return connection;
        }

        /// <summary>
        /// Пулл был уничтожен
        /// </summary>
        /// <param name="pool">Уничтоженный пул</param>
        internal void PoolDisposed(DatabaseConnectionPool pool)
        {
            if (!DisposeRequired)
            {
                _mutex.WaitOne();
                _pools.Remove(pool);
                _mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Фабрика подключений к СУБД
        /// </summary>
        private DatabaseConnectionKernel()
        {
            DisposeRequired = false;
#if PG
            RealConnectionType = typeof(NpgsqlConnection);
#else
            RealConnectionType = typeof(IfxConnection);
#endif
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DisposeRequired = true;
            _mutex.WaitOne();
            foreach (var pool in _pools)
                pool.Dispose();
            _pools.Clear();
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}
