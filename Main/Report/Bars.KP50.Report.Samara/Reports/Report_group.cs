using System;
using System.Collections.Generic;
using System.Linq;
using Bars.KP50.Report.Base;
using System.Data;
using Globals.SOURCE.Container;
using Bars.KP50.Report.Samara.Properties;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report_group : BaseSqlReport
    {
        /// <summary>
        /// список выбранных отчетов
        /// </summary>
        protected List<string> Reports { set; get; }

        /// <summary>
        /// список выбранных районов
        /// </summary>
        protected List<int> Rajons { set; get; }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> suppliers { get; set; }

        public override string Name
        {
            get { return "Пакетное выполнение отчетов"; }
        }

        public override string Description
        {
            get { return "Пакетное выполнение отчетов"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get 
            {
                var resut = new List<ReportGroup> { ReportGroup.Reports };

                return resut;
            }
        }
        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get
            {
                return null; //Resources.Report_group; }
            }
        }

        public override List<UserParam> GetUserParams()
        {
            
            return new List<UserParam>
            {
                new MonthParameter {Value =  DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new ComboBoxParameter(true)
                {
                    TypeValue = typeof(List<int>),
                    Code = "ReportNames",
                    Require = false,
                    Name = "Отчеты",
                    StoreData = IocContainer.Current.Resolve<IReportProvider>().GetReports()
                            .Select(x => new { Id = x.Code, Name = x.Name }).ToList().OrderBy(p=>p.Name)
                },
                new RaionsParameter(),
            };
        }

        public override DataSet GetData()
        {
            switch (Reports[0])
            {
                case "Bars.KP50.Report.Main.Saldo_nach_supp":
                    {
                        
                        break;
                    }
            }
            return new DataSet();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            do
            {
                //make all reports and pack into zip file
            }
            while (true);
        }

        protected override void PrepareParams()
        {
            Reports = UserParamValues["ReportNames"].GetValue<List<string>>();
            Rajons = UserParamValues["Raions"].GetValue<List<int>>();
            Month = UserParamValues["Month"].GetValue<int>();
        }

        protected override void CreateTempTable()
        {
        }

        protected override void DropTempTable()
        {
        }
    }
}