namespace STCLINE.KP50.DataBase
{
    using Castle.Windsor;
    
    /// <summary>Интерфейс фабрики транзакций</summary>
    public class TransactionFactory : ITransactionFactory
    {
        /// <summary>Текущая транзакция</summary>
        private IDataTransaction _currentTransaction;

        public IWindsorContainer Container { get; set; }

        /// <summary>Открыть транзакцию</summary>
        /// <returns>Транзакция</returns>
        public IDataTransaction BeginTransaction()
        {
            if (_currentTransaction != null)
            {
                if (_currentTransaction.IsActive)
                {
                    return _currentTransaction;
                }

                _currentTransaction.Dispose();
                _currentTransaction = null;
            }

            var currentConnection = Container.Resolve<IConnectionFactory>().GetConnection();
            _currentTransaction = new DataTransaction(currentConnection.BeginTransaction());

            return _currentTransaction;
        }
    }
}