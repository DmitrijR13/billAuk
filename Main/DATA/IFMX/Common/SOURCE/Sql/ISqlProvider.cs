namespace STCLINE.KP50.DataBase
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>Интерфейс провайдера для выполнения sql запросов</summary>
    public interface ISqlProvider : IDisposable
    {
        /// <summary>Создать sql команду</summary>
        /// <param name="sql">Sql запрос</param>
        /// <param name="connectionString">Строка подключения</param>
        /// <returns>Sql команда</returns>
        SqlCommand CreaSqlCommand(string sql, string connectionString);

        /// <summary>Выполнить запрос</summary>
        /// <param name="sqlCommand">Sql команда</param>
        void ExecuteNonQuery(SqlCommand sqlCommand);

        /// <summary>Получить результат выполнения запроса в виде списка</summary>
        /// <param name="sqlCommand">Sql команда</param>
        /// <returns>Список значение</returns>
        IList<object[]> List(SqlCommand sqlCommand);

        /// <summary>Получить результат выполнения запроса в виде таблицы</summary>
        /// <param name="sqlCommand">Sql команда</param>
        /// <returns>Таблица</returns>
        DataTable GetDataTable(SqlCommand sqlCommand);

        /// <summary>Выполнить единичную операцию</summary>
        /// <typeparam name="T">Тип возвращаемого значения</typeparam>
        /// <param name="sqlCommand">Sql команда</param>
        /// <returns>Результат выполнения</returns>
        T ExecuteScalar<T>(SqlCommand sqlCommand);

        /// <summary>Выполнить вставку</summary>
        /// <param name="sqlCommand">Sql команда</param>
        /// <returns>Идентификатор записи</returns>
        int Insert(SqlCommand sqlCommand);
    }
}