using System.Data;
using Bars.QueueCore;
using Castle.Windsor;

namespace Bars.KP50.Faktura.Source.Base
{
    /// <summary>
    /// Базовый класс счета квитанции, от которого наследуются все квитанции
    /// </summary>
    public abstract class BaseBill : IBaseBill
    {
        public IWindsorContainer Container { get; set; }

        /// <summary>Название счета-квитанции</summary>
        public abstract string Name { get; }

        /// <summary>Название счета-квитанции</summary>
        public virtual string Code
        {
            get { return GetType().FullName; }
        }

        /// <summary>Описание счета-квитанции</summary>
        public abstract string Description { get; }

        /// <summary> Имя файла счета </summary>
        public abstract string FileName { get; }

        public decimal SumTicket; // К оплате в штрих-коде, для самары могут отличаться
        public int Month { get; set; } //Месяц за который выдана счет-квитанция
        public int Year { get; set; } //Год за который выдана счет-квитанция
        public string MonthPredlog;//Наименование месяца в предложном падеже
        public string FullMonthName { get; set; }//Полное наименование месяца в счете
        public string Ud { get; set; } //УД в самаре
        public string Rajon { get; set; } //Район
        public string Town { get; set; } //Населенный пункт
        public virtual string Ulica { get; set; } //Наименование улицы
        public virtual string NumberDom { get; set; }//Наименование дома и корпуса
        public virtual string NumberFlat { get; set; }//Наименование квартиры и комнаты
        public virtual string NumberRoom { get; set; }//Наименование комнаты
        public string Pref { get; set; } //Префикс БД лицеовго счета
        public int NzpArea { get; set; } //Код территории, к которой принадлежит ЛС
        public int NzpGeu { get; set; }//Код участка, к которому принадлежит ЛС
        public int NzpDom { get; set; }//Код дома, к которому принадлежит ЛС
        public int NzpKvar { get; set; }//Код квартиры
        public int NumLs { get; set; }//Код ЛС
        public string Pkod { get; set; } //Код плательщика
        public string Indecs;//Индекс дома
        public string Shtrih;//Штрихкод для счета

        public STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims BillRegim { get; set; } //Режим формирования лицевого счета

        /// <summary>
        /// Создание перечня всех полей счета
        /// </summary>
        /// <returns></returns>
        public abstract DataTable MakeTable();
        protected virtual IDbConnection Connection { get; set; }

        /// <summary>
        /// Заполение 1 строки резульирующей таблицы данными ЛС
        /// </summary>
        /// <param name="dt">результирующая таблица</param>
        /// <returns></returns>
        public abstract bool FillRow(DataTable dt);

        public abstract void Clear();

        

    }

}


