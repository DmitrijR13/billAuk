using Bars.KP50.DB.Faktura;
using Bars.KP50.Faktura.Source.Base;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bars.KP50.Test.Faktura
{
    [TestFixture]
    public class SumServ2Test
    {
        
        public SumServ2 SetServGvs(bool addTarif, bool addVolume, 
            int nzp_measure, decimal tarif, decimal volume)
        {
            SumServ2 old_ss = new SumServ2();
            old_ss.NameServ = "Подогрев воды";
            old_ss.Measure = "Гкал";
            old_ss.NzpServ = 9;
            old_ss.NzpOdnMaster = 0;
            old_ss.NzpFrm = 40;
            old_ss.IsDevice = 1;
            old_ss.NzpMeasure = nzp_measure;
            old_ss.OldMeasure = 3;
            old_ss.Ordering = 3;
            old_ss.SumInsaldo = 10;
            old_ss.SumOutsaldo = 10;
            old_ss.Tarif = tarif;
            old_ss.TarifF = 0;
            old_ss.RsumTarif = 100;
            old_ss.SumTarif = 90;
            old_ss.SumLgota = 0;
            old_ss.SumNedop = 10;
            old_ss.SumReal = 90;
            old_ss.SumSn = 100;
            old_ss.Reval = 0;
            old_ss.RevalGil = 0;
            old_ss.Norma = 10;
            old_ss.SumPere = 0;
            old_ss.RealCharge = 0;
            old_ss.SumCharge = 10;
            old_ss.SumMoney = 90;
            old_ss.CCalc = volume;
            old_ss.CReval = 0;
            old_ss.UnionServ = true;
            old_ss.CanAddTarif = addTarif;
            old_ss.CanAddVolume = addVolume;
            old_ss.IsOdn = false;
            old_ss.COkaz = 30;
            return old_ss;
        }

        public SumServ2 SetServHvsGvs(bool addTarif, bool addVolume,
            int nzp_measure, decimal tarif, decimal volume)
        {
            SumServ2 old_ss = new SumServ2();
            old_ss.NameServ = "ХВС для ГВС";
            old_ss.Measure = "куб.м.";
            old_ss.NzpServ = 14;
            old_ss.NzpOdnMaster = 0;
            old_ss.NzpFrm = 41;
            old_ss.IsDevice = 1;
            old_ss.NzpMeasure = nzp_measure;
            old_ss.OldMeasure = 3;
            old_ss.Ordering = 3;
            old_ss.SumInsaldo = 5;
            old_ss.SumOutsaldo = 5;
            old_ss.Tarif = tarif;
            old_ss.TarifF = 0;
            old_ss.RsumTarif = 50;
            old_ss.SumTarif = 45;
            old_ss.SumLgota = 0;
            old_ss.SumNedop = 5;
            old_ss.SumReal = 45;
            old_ss.SumSn = 50;
            old_ss.Reval = 0;
            old_ss.RevalGil = 0;
            old_ss.Norma = 11;
            old_ss.SumPere = 0;
            old_ss.RealCharge = 0;
            old_ss.SumCharge = 5;
            old_ss.SumMoney = 45;
            old_ss.CCalc = volume;
            old_ss.CReval = 0;
            old_ss.UnionServ = true;
            old_ss.CanAddTarif = addTarif;
            old_ss.CanAddVolume = addVolume;
            old_ss.IsOdn = false;
            old_ss.COkaz = 30;
            return old_ss;
        }

        public SumServ2 SetServGvsOdn(bool addTarif, bool addVolume,
            int nzp_measure, decimal tarif, decimal volume)
        {
            SumServ2 old_ss = new SumServ2();
            old_ss.NameServ = "ОДН Горячая вода";
            old_ss.Measure = "Гкал";
            old_ss.NzpServ = 9;
            old_ss.NzpOdnMaster = 0;
            old_ss.NzpFrm = 40;
            old_ss.IsDevice = 1;
            old_ss.NzpMeasure = nzp_measure;
            old_ss.OldMeasure = 3;
            old_ss.Ordering = 3;
            old_ss.SumInsaldo = 10;
            old_ss.SumOutsaldo = 10;
            old_ss.Tarif = tarif;
            old_ss.TarifF = 0;
            old_ss.RsumTarif = 100;
            old_ss.SumTarif = 90;
            old_ss.SumLgota = 0;
            old_ss.SumNedop = 10;
            old_ss.SumReal = 90;
            old_ss.SumSn = 100;
            old_ss.Reval = 0;
            old_ss.RevalGil = 0;
            old_ss.Norma = 10;
            old_ss.SumPere = 0;
            old_ss.RealCharge = 0;
            old_ss.SumCharge = 10;
            old_ss.SumMoney = 90;
            old_ss.CCalc = volume;
            old_ss.CReval = 0;
            old_ss.UnionServ = true;
            old_ss.CanAddTarif = addTarif;
            old_ss.CanAddVolume = addVolume;
            old_ss.IsOdn = true;
            old_ss.COkaz = 30;
            return old_ss;
        }


        public SumServ2 SetServHvsGvsOdn(bool addTarif, bool addVolume,
            int nzp_measure, decimal tarif, decimal volume)
        {
            SumServ2 old_ss = new SumServ2();
            old_ss.NameServ = "ОДН Хвс для ГВС";
            old_ss.Measure = "куб.м.";
            old_ss.NzpServ = 9;
            old_ss.NzpOdnMaster = 0;
            old_ss.NzpFrm = 40;
            old_ss.IsDevice = 1;
            old_ss.NzpMeasure = nzp_measure;
            old_ss.OldMeasure = 3;
            old_ss.Ordering = 3;
            old_ss.SumInsaldo = 10;
            old_ss.SumOutsaldo = 10;
            old_ss.Tarif = tarif;
            old_ss.TarifF = 0;
            old_ss.RsumTarif = 100;
            old_ss.SumTarif = 90;
            old_ss.SumLgota = 0;
            old_ss.SumNedop = 10;
            old_ss.SumReal = 90;
            old_ss.SumSn = 100;
            old_ss.Reval = 0;
            old_ss.RevalGil = 0;
            old_ss.Norma = 10;
            old_ss.SumPere = 0;
            old_ss.RealCharge = 0;
            old_ss.SumCharge = 10;
            old_ss.SumMoney = 90;
            old_ss.CCalc = volume;
            old_ss.CReval = 0;
            old_ss.UnionServ = true;
            old_ss.CanAddTarif = addTarif;
            old_ss.CanAddVolume = addVolume;
            old_ss.IsOdn = true;
            old_ss.COkaz = 30;
            return old_ss;
        }


        public SumServ2 SetServ(int nzpServ, bool addTarif, bool addVolume,
                  int nzp_measure, decimal tarif, decimal volume)
        {
            SumServ2 old_ss = new SumServ2();
            old_ss.NameServ = "Простая услуга";
            old_ss.Measure = "куб.м.";
            old_ss.NzpServ = nzpServ;
            old_ss.NzpFrm = 40;
            old_ss.IsDevice = 1;
            old_ss.NzpMeasure = nzp_measure;
            old_ss.Ordering = 3;
            old_ss.SumInsaldo = 10;
            old_ss.SumOutsaldo = 10;
            old_ss.Tarif = tarif;
            old_ss.RsumTarif = 100;
            old_ss.SumTarif = 90;
            old_ss.SumNedop = 10;
            old_ss.SumReal = 90;
            old_ss.SumSn = 100;
            old_ss.Norma = 10;
            old_ss.SumCharge = 10;
            old_ss.SumMoney = 90;
            old_ss.CCalc = volume;
            old_ss.CReval = 0;
            old_ss.UnionServ = false;
            old_ss.CanAddTarif = addTarif;
            old_ss.CanAddVolume = addVolume;
            old_ss.IsOdn = true;
            old_ss.COkaz = 30;
            return old_ss;
        }

        [Test]
        public void SumServ2AddUnionServSingleMeasure()
        {

            SumServ2 new_ss = SetServGvs(true, true, 3, 100, 1);
            new_ss.AddSum(SetServHvsGvs(true, false, 3, 50, 1));
            new_ss.AddSum(SetServGvsOdn(false, true, 3, 100, 1));
            new_ss.AddSum(SetServHvsGvsOdn(false, false, 3, 50, 1));
            Assert.IsTrue(new_ss.Tarif == 150, "Ошибка в суммировании тарифа ожидалось 150 пришло " + new_ss.Tarif.ToString("0.00"));
            Assert.IsTrue(new_ss.CCalc == 2, "Ошибка в суммировании объема ожидалось 2 пришло " + new_ss.CCalc.ToString("0.00"));
        }


        [Test]
        public void SumServ2AddUnionMultipleMeasure()
        {

            SumServ2 new_ss = SetServGvs(true, true, 4, 100, 1);
            new_ss.AddSum(SetServHvsGvs(true, false, 3, 50, 10));
            new_ss.AddSum(SetServGvsOdn(false, true, 4, 100, 1));
            new_ss.AddSum(SetServHvsGvsOdn(false, false, 3, 50, 10));
            Assert.IsTrue(new_ss.Tarif == 100, "Ошибка в суммировании тарифа (разные единицы измерения) "+
                " ожидалось 100 пришло " +
                new_ss.Tarif.ToString("0.00"));
            Assert.IsTrue(new_ss.CCalc == 2, "Ошибка в суммировании объема (разные единицы измерения) "+
                " ожидалось 2 пришло " + 
                new_ss.CCalc.ToString("0.00"));
        }



        /// <summary>
        /// Случайное добавление услуг с одной и той же единицей измерения
        /// </summary>
        [Test]
        public void SumServ2AddUnionSingleMeasureRandomAdd()
        {

            SumServ2 new_ss = SetServGvs(true, true, 3, 0, 0);
            new_ss.AddSum(SetServHvsGvs(true, true, 3, 100, 1));
            new_ss.AddSum(SetServGvs(true, false, 3, 50, 1));
            new_ss.AddSum(SetServHvsGvsOdn(false, false, 3, 50, 1));
            new_ss.AddSum(SetServGvsOdn(false, true, 3, 100, 1));
            Assert.IsTrue(new_ss.Tarif == 150, "Ошибка в суммировании тарифа ожидалось 150 пришло " + new_ss.Tarif.ToString("0.00"));
            Assert.IsTrue(new_ss.CCalc == 2, "Ошибка в суммировании объема ожидалось 2 пришло " + new_ss.CCalc.ToString("0.00"));
        }


        /// <summary>
        /// Случайное добавление услуг с разными единицами измерения
        /// </summary>
        [Test]
        public void SumServ2AddUnionMultipleMeasureRandomAdd()
        {

            SumServ2 new_ss = SetServGvs(true, true, 4, 0, 0);
            new_ss.AddSum(SetServHvsGvs(true, true, 4, 100, 1));
            new_ss.AddSum(SetServGvs(true, false, 3, 50, 10));
            new_ss.AddSum(SetServHvsGvsOdn(false, false, 3, 50, 10));
            new_ss.AddSum(SetServGvsOdn(false, true, 4, 100, 1));
            Assert.IsTrue(new_ss.Tarif == 100, "Ошибка в суммировании тарифа (разные единицы измерения) " +
                " ожидалось 100 пришло " +
                new_ss.Tarif.ToString("0.00"));
            Assert.IsTrue(new_ss.CCalc == 2, "Ошибка в суммировании объема (разные единицы измерения) " +
                " ожидалось 2 пришло " +
                new_ss.CCalc.ToString("0.00"));
        }


        [Test]
        public void SumServ2AddCommonServVolume()
        {

            SumServ2 new_ss = SetServ(17, true, true, 1, 12, 50);
            new_ss.AddSum(SetServ(17, false, true, 1, 12, 50));
            Assert.IsTrue(new_ss.Tarif == 12, "Ошибка в суммировании тарифа (разные единицы измерения) " +
                " ожидалось 12 пришло " +
                new_ss.Tarif.ToString("0.00"));
            Assert.IsTrue(new_ss.CCalc == 100, "Ошибка в суммировании объема (разные единицы измерения) " +
                " ожидалось 100 пришло " +
                new_ss.CCalc.ToString("0.00"));
        }

        [Test]
        public void SumServ2AddCommonServTarif()
        {

            SumServ2 new_ss = SetServ(17, true, true, 1, 12, 50);
            new_ss.AddSum(SetServ(17, true, false, 1, 12, 50));
            Assert.IsTrue(new_ss.Tarif == 24, "Ошибка в суммировании тарифа (разные единицы измерения) " +
                " ожидалось 24 пришло " +
                new_ss.Tarif.ToString("0.00"));
            Assert.IsTrue(new_ss.CCalc == 50, "Ошибка в суммировании объема (разные единицы измерения) " +
                " ожидалось 50 пришло " +
                new_ss.CCalc.ToString("0.00"));
        }
    }
}
