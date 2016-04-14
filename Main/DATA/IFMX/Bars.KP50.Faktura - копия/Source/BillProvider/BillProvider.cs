using System.Collections.Generic;
using System.Linq;
using Bars.KP50.Report;

namespace Bars.KP50.Faktura.Source.BillProvider
{
    /// <summary>Провайдер отчетов</summary>
    public class BillProvider : IBillProvider
    {
        public BillProvider()
        {
            Bills = new List<BillInfo>();
        }

        /// <summary>Список отчетов</summary>
        protected List<BillInfo> Bills { get; set; }

        /// <summary>Зарегистрировать отчет</summary>
        /// <param name="bill">Отчет</param>
        public void RegisterBill(IBaseBill bill)
        {
            Bills.Add(new BillInfo(bill));
        }

        /// <summary>Получить список описаний отчетов</summary>
        /// <returns>Список отчетов</returns>
        public IList<BillInfo> GetBills()
        {
            return Bills;
        }

        /// <summary>Получить описание отчета</summary>
        /// <param name="code">Код счета-квитанции</param>
        /// <returns>Описание счета-квитанции</returns>
        public BillInfo GetBill(string code)
        {
            return Bills.FirstOrDefault(x => x.Code == code);
        }
    }
}