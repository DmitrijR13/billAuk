using System.Data;

namespace Bars.KP50.Faktura.Source
{
    public interface IBaseBill
    {
        /// <summary>Уникальный идентификатор счета-квитанции</summary>
        string Code { get; }

        /// <summary>Название счета-квитанции</summary>
        string Name { get; }

        /// <summary>Описание счета-квитанции</summary>
        string Description { get; }

        /// <summary>Имя файла счета</summary>
        string FileName { get; }

        /// <summary>
        /// Создание таблицы переменных в счете квитанции
        /// </summary>
        /// <returns>Пустая таблица с наименованиями и типами колонок</returns>
        DataTable MakeTable();

        /// <summary> Месяц начисления </summary>
        int Month { get; set; }

        /// <summary> Год начисления </summary>
        int Year { get; set; }

        /// <summary> Полное текстовое наименование месяца начисления </summary>
        string FullMonthName { get; set; }

        /// <summary> Режим работы </summary>
        STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims BillRegim { get; set; }

        /// <summary> Префикс схемы данных </summary>
        string Pref { get; set; }

        /// <summary> Код квартиры </summary>
        int NzpKvar { get; set; }

        /// <summary> Код дома </summary>
        int NzpDom { get; set; }

        /// <summary> Лицевой счет </summary>
        int NumLs { get; set; }

        /// <summary> Платежный код </summary>
        string Pkod { get; set; }

        /// <summary> Код территории </summary>
        int NzpArea { get; set; }
        
        /// <summary> Код ЖЭУ </summary>
        int NzpGeu { get; set; }

        /// <summary> Город/Район </summary>
        string Town { get; set; }

        /// <summary> Населенный пункт </summary>
        string Rajon { get; set; }

        /// <summary> Улица </summary>
        string Ulica { get; set; }

        /// <summary> Номер дома </summary>
        string NumberDom { get; set; }

        /// <summary> Номер квартиры </summary>
        string NumberFlat { get; set; }

        /// <summary> Участок </summary>
        string Ud { get; set; }


        /// <summary>
        /// Заполнение отдной строки в результирующей таблице
        /// одна строка - одна квитанция
        /// </summary>
        /// <param name="table"></param>
        bool FillRow(DataTable table);

        /// <summary>
        /// Очистка переменных перед следующей квитанцией
        /// </summary>
        void Clear();


    }
}