using System;
using System.Data;

namespace Bars.KP50.Report.Base
{
    /// <summary>
    /// Гибкий отчет.
    /// Подключается динамически. Данные выбираются непосредственно PgSql функциями.
    /// </summary>
    public interface ISoftReport
    {
        /// <summary>
        /// Инициализировать свойства
        /// </summary>
        /// <param name="id">Идентификатор отчета. Из таблицы report.list</param>
        /// <param name="connection">Подключение к базе данных</param>
        void InitSoftProperties(int reportID, int userID, IDbConnection connection);
    }
}
