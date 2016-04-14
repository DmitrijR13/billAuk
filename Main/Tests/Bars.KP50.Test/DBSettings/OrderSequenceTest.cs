using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.DB.Admin.Source.OrderSequence;
using NUnit.Framework;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Test.DBSettings
{

    internal class OrderSequenceTest
    {
        [Test]
        public void StartOrderTest()
        {
            Constants.cons_Webdata =
                "Server=192.168.222.135;Port=5432;User Id=postgres;Password=123;Database=tula_test;Preload Reader=true;";
            Constants.cons_Kernel = Constants.cons_Webdata;
            using (OrderingSequence orderSeq = new OrderingSequence())
            {
                Returns ret = orderSeq.DoOrderSequences();
                Assert.AreEqual(true, ret.result);
            }
        }
    }
}

