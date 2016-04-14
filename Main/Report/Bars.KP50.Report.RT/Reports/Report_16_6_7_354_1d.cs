using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RT.Properties;
using Castle.Core.Internal;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.RT.Reports
{
    class Report_16_6_7_354_1d : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.6.7.354.1d Состояние жилого фонда по приборам учета"; }
        }

        public override string Description
        {
            get { return "Состояние жилого фонда по приборам учета"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup>();
                result.Add(ReportGroup.Reports);
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        /// <summary>Управляющие компании</summary>
        private int Areas { get; set; }

        /// <summary>Улица</summary>

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
                {
                    new SupplierAndBankParameter(),
                    new ServiceParameter(),
                };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dat_s", DateTime.Today.AddDays(-DateTime.Today.Day + 1));
            report.SetParameterValue("dat_po", DateTime.Today.AddMonths(1).AddDays(-DateTime.Today.Day));
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_6_7_354_1d; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        protected override void PrepareParams()
        {
  
        }

        public override DataSet GetData()
        {
            var dt = ExecSQLToTable("");
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereAdr(string tablePrefix)
        {
            return Areas != 0 ? " and " + tablePrefix + "nzp_area in (" + Areas + ") " : String.Empty;
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {

        }

        protected override void DropTempTable()
        {
        }

    }
}

