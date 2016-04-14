namespace Bars.KP50.Report.Main
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using System.Text;
    using Bars.KP50.Report;
    using Bars.KP50.Report.Base;
    using Bars.KP50.Report.Main.Properties;
    using Bars.KP50.Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    class Test : BaseSqlReport
    {
        /// <summary>
        /// Объект адрес
        /// </summary>
        protected AddressParameterValue adr { get; set; }

        public override string Name
        {
            get { return "Тестовый отчет"; }
        }

        public override string Description
        {
            get { return "Тестовый отчет"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Test; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new AddressParameter(),
                new SupplierAndBankParameter()
            };
        }

        public override DataSet GetData()
        {
            return new DataSet();
        }

        protected override void PrepareReport(Report report)
        {
            report.SetParameterValue("raions", String.Join(",", adr.Raions.Select(x=>x.ToString()).ToArray()));
            report.SetParameterValue("streets", String.Join(",", adr.Streets.Select(x=>x.ToString()).ToArray()));
            report.SetParameterValue("houses", String.Join(",", adr.Houses.Select(x=>x.ToString()).ToArray()));
        }

        protected override void PrepareParams()
        {
            adr = JsonConvert.DeserializeObject<AddressParameterValue>(UserParamValues["Address"].Value.ToString());
            var supp = JsonConvert.DeserializeObject<SupplierAndBankParameter>(UserParamValues["SupplierAndBank"].Value.ToString());
        }

        protected override void CreateTempTable()
        {
        }

        protected override void DropTempTable()
        {
        }
    }
}
