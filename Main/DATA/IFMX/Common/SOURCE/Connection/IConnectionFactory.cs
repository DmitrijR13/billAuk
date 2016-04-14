namespace STCLINE.KP50.DataBase
{
    using System.Data;

    /// <summary>Интерфейс фабрики соединений</summary>
    public interface IConnectionFactory
    {
        /// <summary>Открыть соединение</summary>
        /// <returns></returns>
        IDbConnection GetConnection(bool opened = true);
    }
}