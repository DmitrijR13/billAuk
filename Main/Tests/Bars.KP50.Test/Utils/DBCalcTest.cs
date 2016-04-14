using NUnit.Framework;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Test.Report
{
    [TestFixture]
    public class DbCalcTest
    {
     


        [Test]
        public void GetTestCalcPrefTest()
        {

            var res = DbCalc.GetTestCalcPref("[{\"testCalcPref\":\"gis\"}]");
            Assert.IsTrue(res == "gis", "Неправильно определен префикс");

            res = DbCalc.GetTestCalcPref("{\"testCalcPref\":\"gis\"}");
            Assert.IsTrue(res == "gis", "Неправильно определен префикс");

        }

      


  
    }
}
