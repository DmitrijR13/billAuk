namespace Bars.KP50.Report.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Bars.KP50.Report.Test.Reports;

    using Xunit;

    public class TestReports
    {
        [Fact]
        public void Run_ReportPdf_SuccessSave()
        {
            var report1 = new Report1();
            var savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tula_1.pdf");
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            report1.GenerateReport(new Dictionary<string, string>
            {
                { "userParams", "[{ \"Code\": \"Code1\", \"Value\": \"Value 1\" }, { \"Code\": \"Code2\", \"Value\": \"2\" }]" },
                { "systemParams", "{ \"PathForSave\": \"" + savePath.Replace("\\", "\\\\") + "\", \"ExportFormat\": \"Pdf\", \"Pref\": \"___pref___\" }" }
            });

            Assert.True(File.Exists(savePath));

            // File.Delete(savePath);
        }

        [Fact]
        public void Run_ReportXls_SuccessSave()
        {
            var report1 = new Report1();
            var savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tula_1.xls");
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            report1.GenerateReport(new Dictionary<string, string>
            {
                { "userParams", "[{ \"Code\": \"Code1\", \"Value\": \"Value 1\" }, { \"Code\": \"Code2\", \"Value\": \"2\" }]" },
                { "systemParams", "{ \"PathForSave\": \"" + savePath.Replace("\\", "\\\\") + "\", \"ExportFormat\": \"Excel2007\", \"Pref\": \"___pref___\" }" }
            });

            Assert.True(File.Exists(savePath));

            // File.Delete(savePath);/
        }
    }
}