using System;
using NUnit.Framework;
using STCLINE.KP50.IFMX.Report.SOURCE.Samara;

namespace Bars.KP50.Test.Report
{
    [TestFixture]
    public class SamaraCalcLocalTest
    {
     


        [Test]
        public void SamaraCalcLocal()
        {

            var samaraCalcLocal = new SamaraCalcLocal(null, "smr36", 2013, 10);
            samaraCalcLocal.SetSumNach("smr36_data:kvar");

            Assert.IsTrue(samaraCalcLocal.TestSql != String.Empty, "Ошибка формирования запросов");
            samaraCalcLocal.Close();
        }

        [Test]
        public void SamaraGroupCalcReport()
        {

            var samaraGroupCalcReport = new SamaraGroupCalcReport(null, 2013, 10, 1);
            samaraGroupCalcReport.PrepareTempTable();

            Assert.IsTrue(samaraGroupCalcReport.TestSql != String.Empty,
                "Ошибка формирования запросов");
        }


        [Test]
        public void SamaraReportNedop()
        {

            var samaraReportNedop = new SamaraReportNedop(null,"smr36_data:kvar");
            samaraReportNedop.GetNedopByKvar("smr36", 2013, 10);

            Assert.IsTrue(samaraReportNedop.TestSql != String.Empty, "Ошибка формирования запросов");
            samaraReportNedop.Close();
        }


  
    }
}
