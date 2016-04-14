namespace Bars.KP50.Report.Test.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;

    using Bars.KP50.Report.Base;
    using Bars.KP50.Report.Test.Properties;

    using FastReport;

    public class Report1 : BaseFastReport
    {
        public override string Name
        {
            get { return "Тестовый отчет"; }
        }

        public override string Description
        {
            get { return "Описание тестового отчета"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup>() {ReportGroup.Reports}; }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get
            {
                return Resources.tula_1;
            }
        }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>();
        }

        public override DataSet GetData()
        {
            var valueString = UserParams["Code1"].GetValue<string>();
            var valueInt = UserParams["Code2"].GetValue<int>();

            var dt = new DataTable();
            dt.TableName = "Q_master";

            dt.Columns.Add("service", typeof(string));
            dt.Columns.Add("name_supp", typeof(string));
            dt.Columns.Add("sum_charge", typeof(decimal));

            dt.Rows.Add("Строка 1", valueString, valueInt);
            dt.Rows.Add("Строка 2", "Бла Бла Бла", 222.22M);
            dt.Rows.Add("Строка 3", "Бла Бла Бла", 333.33M);
            dt.Rows.Add("Строка 4", "Бла Бла Бла", 444.44M);
            dt.Rows.Add("Строка 5", "Бла Бла Бла", 555.55M);
            dt.Rows.Add("Строка 6", "Бла Бла Бла", 666M);

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            report.GetDataSource("Q_master").Enabled = true;
            report.SetParameterValue("month", "11");
            report.SetParameterValue("year", 2013);
            report.SetParameterValue("reportHeader", Name);
            report.SetParameterValue("sumHeader", "Вид начислено");
            report.SetParameterValue("ercName", "ЕРЦ");
            report.SetParameterValue("principal", "principal");
        }
    }
}