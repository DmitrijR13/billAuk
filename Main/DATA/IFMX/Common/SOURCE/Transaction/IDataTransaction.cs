namespace STCLINE.KP50.DataBase
{
    using System;

    /// <summary>Интерфейс транзакция</summary>
    public interface IDataTransaction : IDisposable
    {
        /// <summary>Является активной (не вызывали Commit или Rollback)</summary>
        bool IsActive { get; }

        /// <summary>Была отменена</summary>
        bool WasRolledBack { get; }

        /// <summary>Была успешно сохранена</summary>
        bool WasCommitted { get; }

        /// <summary>Сохранить</summary>
        void Commit();

        /// <summary>Откатить</summary>
        void Rollback();
    }
}