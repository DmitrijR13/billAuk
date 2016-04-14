using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Test.Utils
{
    [TestFixture]
    public class DbMustCalcNewTest
    {



        [Test]
        public void TestReason()
        {
            Constants.cons_Webdata =
                "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=webtul2;Preload Reader=true;";
            IDbConnection connDb = DBManager.GetConnection(Constants.cons_Webdata);
            DBManager.OpenDb(connDb, true);
            var dbMustCalcNew = new DbMustCalcNew(connDb);

            var must = new MustCalcTable
            {
                NzpKvar = -1,
                NzpServ = 6,
                NzpSupp = 0,
                NzpUser = -1,
                Reason = MustCalcReasons.Service,
                Kod2 = 1,
                Year = 2014,
                Month = 4,
                Comment = "Тест",
                DatPo = new DateTime(2014, 03, 31),
                DatS = new DateTime(2014, 03, 01),
                DatWhen = DateTime.Now
            };
            Returns ret = dbMustCalcNew.InsertReason("tul01_data",must);
            Assert.IsTrue(ret.result, "Ошибка добаления перерасчета");

            List<MustCalcTable> must1 = dbMustCalcNew.GetReason("tul01_data", -1, out ret);
            Assert.IsTrue(must1[0].NzpServ == 6, "Ошибка считывания перерасчета");

            ret =  dbMustCalcNew.DeleteReason("tul01_data",2014,4, -1);
            Assert.IsTrue(ret.result, "Ошибка удаления группового перерасчета");


            
        }

        [Test]
        public void TestReasons()
        {
            Constants.cons_Webdata =
                "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=webtul2;Preload Reader=true;";
            IDbConnection connDb = DBManager.GetConnection(Constants.cons_Webdata);
            DBManager.OpenDb(connDb, true);
            var dbMustCalcNew = new DbMustCalcNew(connDb);

          

            List<MustCalcTable> listMast = new List<MustCalcTable>();
            for (int i = 0; i<40; i++)
            {
                var must = new MustCalcTable
                {
                    NzpKvar = -1,
                    NzpServ = 6,
                    NzpSupp = 0,
                    NzpUser = -1,
                    Reason = MustCalcReasons.Service,
                    Kod2 = i,
                    Year = 2014,
                    Month = 4,
                    Comment = "Тест",
                    DatPo = new DateTime(2014, 03, 31),
                    DatS = new DateTime(2014, 03, 01),
                    DatWhen = DateTime.Now
                };
                listMast.Add(must);
            }
            Returns ret = dbMustCalcNew.InsertReasons("tul01_data",listMast);
            Assert.IsTrue(ret.result, "Ошибка добаления группового перерасчета");

            
            List<MustCalcTable> must1 = dbMustCalcNew.GetReason("tul01_data", -1, out ret);
            Assert.IsTrue(must1.Count == 40, "Ошибка считывания группового перерасчета");

            ret = dbMustCalcNew.DeleteReason("tul01_data",2014,4, -1);
            Assert.IsTrue(ret.result, "Ошибка удаления группового перерасчета");


            

        }


    }
}
