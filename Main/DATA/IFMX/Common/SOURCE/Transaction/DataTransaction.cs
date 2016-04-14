namespace STCLINE.KP50.DataBase
{
    using System.Data;

    /// <summary>Реализация транзакции</summary>
    public class DataTransaction : IDataTransaction
    {
        private bool _isActive;
        private bool _wasRolledBack;
        private bool _wasCommitted;

        private IDbTransaction _innerTransaction;

        public DataTransaction(IDbTransaction transaction)
        {
            _innerTransaction = transaction;
            _isActive = true;
        }

        /// <summary>Является активной (не вызывали Commit или Rollback)</summary>
        public bool IsActive
        {
            get { return _isActive; }
        }

        /// <summary>Была отменена</summary>
        public bool WasRolledBack
        {
            get { return _wasRolledBack; }
        }

        /// <summary>Была успешно сохранена</summary>
        public bool WasCommitted
        {
            get { return _wasCommitted; }
        }

        /// <summary>Сохранить</summary>
        public void Commit()
        {
            _innerTransaction.Commit();
            _wasCommitted = true;
            _isActive = false;
        }

        /// <summary>Откатить</summary>
        public void Rollback()
        {
            _innerTransaction.Rollback();
            _wasRolledBack = true;
            _isActive = false;
        }

        public void Dispose()
        {
            _innerTransaction.Dispose();
        }
    }
}