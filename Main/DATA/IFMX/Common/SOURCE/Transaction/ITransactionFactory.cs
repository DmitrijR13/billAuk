namespace STCLINE.KP50.DataBase
{
    /// <summary>Интерфейс фабрики транзакций</summary>
    public interface ITransactionFactory
    {
        /// <summary>Открыть транзакцию</summary>
        /// <returns>Транзакция</returns>
        IDataTransaction BeginTransaction(); 
    }
}