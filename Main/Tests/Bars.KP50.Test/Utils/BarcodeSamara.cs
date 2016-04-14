using NUnit.Framework;

namespace Bars.KP50.Test.Report
{
    [TestFixture]
    public class BarcodeSamaraTest
    {
     


        [Test]
        public void BarcodeSamara()
        {
            
            var samaraCrc = STCLINE.KP50.Global.Utils.BarcodeCrcSamara("0045866002620891213000127109");
            Assert.IsTrue(samaraCrc == "22", "Неправильно посчитано контрольное число");
        }

          [Test]
        public void BarcodeSamara2()
        {
            
            var samaraCrc = STCLINE.KP50.Global.Utils.BarcodeCrcSamara("0046801174980770114000117993");
            Assert.IsTrue(samaraCrc == "64", "Неправильно посчитано контрольное число");
        }


  
    }
}
