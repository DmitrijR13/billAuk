using System.Collections.Generic;
using Bars.KP50.Report;

namespace Bars.KP50.Faktura.Source.BillProvider
{
    /// <summary>Интерфейс провайдера отчетов</summary>
    public interface IBillProvider
    {
        /// <summary>Зарегистрировать отчет</summary>
        /// <param name="bill">Отчет</param>
        void RegisterBill(IBaseBill bill);

        /// <summary>Получить список описаний счетов-квитанций</summary>
        /// <returns>Список отчетов</returns>
        IList<BillInfo> GetBills();

        /// <summary>Получить описание счетов-квитанций</summary>
        /// <param name="code">Код счета-квитанции</param>
        /// <returns>Описание отчета</returns>
        BillInfo GetBill(string code);
    }
}