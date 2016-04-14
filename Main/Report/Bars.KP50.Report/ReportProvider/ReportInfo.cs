namespace Bars.KP50.Report
{
    using System.Collections.Generic;
    using System.Linq;

    using Bars.KP50.Report.Base;

    /// <summary>Описание отчета</summary>
    public class ReportInfo
    {
        public ReportInfo(IBaseReport report)
        {
            Code = report.Code;
            Name = report.Name;
            Description = report.Description;
            ReportGroups = report.ReportGroups;
            ReportKinds = report.ReportKinds;
            IsPreview = report.IsPreview;

            ListUserFilters = report.GetUserFilters();
            ListUserParams = report.GetUserParams();

            if (!IsPreview && ListUserParams.All(x => x.Code != "ExportFormat"))
            {
                ListUserParams.Add(new ExportFormatParameter());
            }
        }

        /// <summary>Уникальный идентификатор отчета</summary>
        public string Code { get; private set; }

        /// <summary>Название отчета</summary>
        public string Name { get; private set; }

        /// <summary>Описание отчета</summary>
        public string Description { get; private set; }

        /// <summary>Группы отчетов</summary>
        public IList<ReportGroup> ReportGroups { get; private set; }

        public IList<ReportKind> ReportKinds { get; private set; }

        /// <summary>Выполняется предпросмотр</summary>
        public bool IsPreview { get; private set; }

        public List<UserParam> ListUserParams { get; set; }

        public List<UserParam> ListUserFilters { get; set; }
    }
}