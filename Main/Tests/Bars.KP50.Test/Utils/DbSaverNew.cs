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
    public class DbSaverNewTest
    {



        [Test]
        public void TestSaver()
        {
            Constants.cons_Webdata =
                "Server=192.168.222.173;Port=5432;User Id=postgres;Password=postgres;Database=webastr4;Preload Reader=true;";
            Constants.cons_Kernel = Constants.cons_Webdata;

            IDbConnection connDb = DBManager.GetConnection(Constants.cons_Webdata);
            DBManager.OpenDb(connDb, true);

            var  editData = new EditInterData();
            editData.pref = "astr01";
            editData.nzp_wp = 2;
            editData.nzp_user = 1;
            editData.primary = "nzp_tarif";
            editData.table = "tarif";

            editData.intvType = enIntvType.intv_Day;

            //указываем вставляемый период
            editData.dat_s = "01.09.2011";
            editData.dat_po = "01.01.2050";

            //перечисляем поля и значения этих полей, которые вставляются
            var vals = new Dictionary<string, string>();
            vals.Add("nzp_supp", "100288");
            vals.Add("nzp_frm", "38");
            vals.Add("tarif", "17.39");
            vals.Add("num_ls", "108396");
            vals.Add("nzp_serv", "6");

            editData.vals = vals;

            //условие выборки данных из целевой таблицы
            var result = new List<string>();
            string cond = " and nzp_serv = 6 ";
            cond += " and nzp_supp = 100288";
            cond += " and nzp_kvar = 108396";
            result.Add(cond);

            editData.dopFind = result;

            //перечисляем ключевые поля и значения (со знаком сравнения!)
            var keys = new Dictionary<string, string>();
            keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar = 108396"); //ссылка на ключевую таблицу

            editData.keys = keys;

            var dbSaverNew = new DbSaverNew(connDb, editData);

            Returns ret = dbSaverNew.Saver();
            Assert.IsTrue(ret.result, "Ошибка сохранения параметра");


            
        }


    }
}
