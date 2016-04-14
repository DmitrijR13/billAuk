namespace Bars.KP50.Report
{
    using Faktura.Source;
    

    /// <summary>Описание отчета</summary>
    public class BillInfo
    {
        public BillInfo(IBaseBill bill)
        {
            Code = bill.Code;
            Name = bill.Name;
            Description = bill.Description;
            FileName = bill.FileName;
        }

        /// <summary>Уникальный идентификатор отчета</summary>
        public string Code { get; private set; }

        /// <summary>Название отчета</summary>
        public string Name { get; private set; }

        /// <summary>Описание отчета</summary>
        public string Description { get; private set; }

        /// <summary>Описание отчета</summary>
        public string FileName { get; private set; }
     
        /// <summary>Выполняется предпросмотр</summary>
        public bool IsPreview { get; private set; }

    }
}