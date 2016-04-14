namespace STCLINE.KP50.DataBase
{
    using System;
    using System.Data;
    using System.Data.Common;

    using Castle.Windsor;

    using Globals.SOURCE.Config;
    using Globals.SOURCE.Container;

    using IBM.Data.Informix;

    using Npgsql;
    using STCLINE.KP50.Global;

    /// <summary>Фабрика соединений</summary>
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly DbProviderFactory _providerFactory;
        private readonly DbParams _dbParams;
        private readonly IWindsorContainer _container;

        private IDbConnection _currentConnection;

        public ConnectionFactory(IWindsorContainer container, DbProviderFactory providerFactory)
        {
            _container = container;
            _providerFactory = providerFactory;
            _dbParams = container.Resolve<IConfigProvider>().GetConfig().MainDbParams;
        }

        public static IConnectionFactory Init()
        {
#if PG
            return new ConnectionFactory(IocContainer.Current, NpgsqlFactory.Instance);
#else
            return new ConnectionFactory(IocContainer.Current, IfxFactory.Instance);
#endif
        }

        /// <summary>Открыть соединение</summary>
        /// <returns></returns>
        public IDbConnection GetConnection(bool opened = true)
        {
            if (_currentConnection != null)
            {
                if (_currentConnection.State == ConnectionState.Open)
                {
                    return _currentConnection;
                }

                _currentConnection.Close();
                _currentConnection = null;
            }

            _currentConnection = _providerFactory.CreateConnection();
            _currentConnection.ConnectionString = _dbParams.ConnectionString;

            if (opened)
            {
                try
                {
                    _currentConnection.Open();
                }
                catch (Exception exc)
                {
                    MonitorLog.WriteException("Ошибка функции GetConnection", exc);
                    throw;
                }
            }

            return _currentConnection;
        }
    }
}