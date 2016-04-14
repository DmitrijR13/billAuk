namespace Bars.KP50.Report.Base
{
    /// <summary>
    /// Виды отчетов
    /// </summary>
    public enum ReportKind : byte
    {
        /// <summary>
        /// Отчеты по одной карточке жильца
        /// </summary>
        Person = 10,

        /// <summary>
        /// Отчеты по одному лицевому счету
        /// </summary>
        LC = 20,

        /// <summary>
        /// Отчеты по одному дому
        /// </summary>
        House = 30,

        /// <summary>
        /// Отчеты по списку карточек жильцов
        /// </summary>
        ListPerson = 40,

        /// <summary>
        /// Отчеты по списку лицевых счетов
        /// </summary>
        ListLC = 50,

        /// <summary>
        /// Отчеты по списку домов
        /// </summary>
        ListHouse = 60,

        /// <summary>
        /// Отчеты по всей базе данных
        /// </summary>
        Base = 70
    }
}