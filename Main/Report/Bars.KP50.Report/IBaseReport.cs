namespace Bars.KP50.Report.Base
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;

    public interface IBaseReport
    {
        /// <summary>Уникальный идентификатор отчета</summary>
        string Code { get; }

        /// <summary>Название отчета</summary>
        string Name { get; }

        /// <summary>Описание отчета</summary>
        string Description { get; }

        /// <summary>Группы отчетов</summary>
        IList<ReportGroup> ReportGroups { get; }

        /// <summary>  Уровень выполнения отчетов </summary>
        IList<ReportKind> ReportKinds { get; }

        /// <summary>Выполняется предпросмотр</summary>
        bool IsPreview { get; }

        /// <summary>Парметры отчета</summary>
        ReportParams ReportParams { get; set; }

        /// <summary>Получить пользовательские фильтры отчета</summary>
        /// <returns>Параметры отчета</returns>
        List<UserParam> GetUserFilters();

        /// <summary>Получить пользовательские параметры отчета</summary>
        /// <returns>Параметры отчета</returns>
        List<UserParam> GetUserParams();

        /// <summary>Задать параметры для отчета</summary>
        /// <param name="reportParameters">Параметры отчета</param>
        void SetReportParameters(NameValueCollection reportParameters);

        /// <summary>Сформировать отчет</summary>
        /// <param name="reportParameters">Параметры отчета</param>
        void GenerateReport(NameValueCollection reportParameters);

        /// <summary>Получить данные отчета</summary>
        /// <returns>Данные отчета в виде DataSet</returns>
        DataSet GetData();

        /// <summary>Сформировать отчет</summary>
        /// <param name="ds">Данные отчета в виде DataSet</param>
        void Generate(DataSet ds);
    }
}