using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Globals.SOURCE.Config;
using Globals.SOURCE.Container;
using NUnit.Framework;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Bars.KP50.DB.Finans.Source;

namespace Bars.KP50.Test.Load
{
    class LastGoodValPuTestFromContersTab
    {
        IDbConnection connDb;
        private string testTableName = "public" + DBManager.tableDelimiter + "counters_test";

        //[Test]
        //public void GetLastGoodValPUByOrderingTest()
        //{
        //    init();
        //    Returns ret = createCountersTestTable();
        //    //Assert.IsTrue(ret.result, "тест не пройден из-за ошибки создания тестовой таблицы счетчиков " + ret.text + " " + ret.sql_error);
        //    //try
        //    //{
        //    //    LastGoodValPuFromCountersTab lgv = new LastGoodValPuFromCountersTab(testTableName);
        //    //    int numLS = 123;
        //    //    string dat_uchet = "2014-09-01";


        //    //    // -----Тест при puValsList=null-----
        //    //    List<PuVals> puValsList = null; // значения из файла пачки
        //    //    ret = lgv.GetLastGoodValPUByOrderingFromDB(connDb, numLS, dat_uchet, puValsList);
        //    //    Assert.IsTrue(ret.result, "Тест при puValsList = null не пройден");

        //    //    //-----Тест на переход через ноль----
        //    //    // все значение счетчика из пачки должны измениться, кроме val_cnt
        //    //    puValsList = new List<PuVals>
        //    //    {
        //    //    new PuVals {nzp_counter = 0,  ordering = 1, zero_crossing = 0, nzp_serv = 0, val_cnt  = 1},
        //    //    new PuVals {nzp_counter = 0,  ordering = 2, zero_crossing = 0, nzp_serv = 0, val_cnt  = 1},
        //    //    };
        //    //    ret = lgv.GetLastGoodValPUByOrderingFromDB(connDb, numLS, dat_uchet, puValsList);
        //    //    Assert.True(
        //    //        ret.result &&
        //    //        ret.text != "" &&
        //    //        puValsList.Count(pu => pu.nzp_counter > 0 &&
        //    //        pu.nzp_serv > 0 &&
        //    //        pu.zero_crossing == 1 &&
        //    //        pu.val_cnt == 1) == 2,
        //    //        "тест на переход через ноль не пройден");
        //    //    //----тест на отсутствие записей в  таблице counters для лицевого счета numLS---
        //    //    // при несовпадение порядкового номера ordering
        //    //    puValsList = new List<PuVals>
        //    //    {
        //    //    new PuVals {nzp_counter = -100,  ordering = 50, nzp_serv = 333, zero_crossing = 0, val_cnt = 999, num_cnt = "035A"},
        //    //    };
        //    //    ret = lgv.GetLastGoodValPUByOrderingFromDB(connDb, numLS, dat_uchet, puValsList);
        //    //    // значения полей объекта из коллекции puValsList не должны измениться
        //    //    Assert.True(
        //    //        ret.result &&
        //    //        ret.text != "" &&
        //    //        puValsList[0].nzp_counter == -100 &&
        //    //        puValsList[0].ordering == 50 &&
        //    //        puValsList[0].nzp_serv == 333 &&
        //    //        puValsList[0].zero_crossing == 0 &&
        //    //        puValsList[0].val_cnt == 999 &&
        //    //        puValsList[0].num_cnt == "035A",
        //    //        "тест на отсутствие записей в counters при несовпадении порядкового номера не пройден");
        //    //    // при отсутствии заданного numLS
        //    //    numLS = 125;
        //    //    puValsList = new List<PuVals>
        //    //    {
        //    //    new PuVals {nzp_counter = -100,  ordering = 1, nzp_serv = 333, zero_crossing = 0, val_cnt = 999, num_cnt = "035A"},
        //    //    };
        //    //    ret = lgv.GetLastGoodValPUByOrderingFromDB(connDb, numLS, dat_uchet, puValsList);
        //    //    // значения полей объекта из коллекции puValsList не должны измениться
        //    //    Assert.True(
        //    //    ret.result &&
        //    //    ret.text != "" &&
        //    //    puValsList[0].nzp_counter == -100 &&
        //    //    puValsList[0].ordering == 1 &&
        //    //    puValsList[0].nzp_serv == 333 &&
        //    //    puValsList[0].zero_crossing == 0 &&
        //    //    puValsList[0].val_cnt == 999 &&
        //    //    puValsList[0].num_cnt == "035A",
        //    //    "тест на отсутствие записей в counters при ЛС " + numLS + " не пройден");

        //    //    // при отсутствии заданной даты учета
        //    //    numLS = 124;
        //    //    ret = lgv.GetLastGoodValPUByOrderingFromDB(connDb, numLS, dat_uchet, puValsList);
        //    //    // значения полей объекта из коллекции puValsList не должны измениться
        //    //    Assert.True(
        //    //    ret.result &&
        //    //    ret.text != "" &&
        //    //    puValsList[0].nzp_counter == -100 &&
        //    //    puValsList[0].ordering == 1 &&
        //    //    puValsList[0].nzp_serv == 333 &&
        //    //    puValsList[0].zero_crossing == 0 &&
        //    //    puValsList[0].val_cnt == 999 &&
        //    //    puValsList[0].num_cnt == "035A",
        //    //    "тест на отсутствие записей в counters при дате учета " + dat_uchet + " не пройден");
        //        connDb.Close();
        //    }
        //    finally
        //    {
        //        DBManager.ExecSQL(connDb, "Drop table " + testTableName, false);
        //        connDb.Close();
        //    }
        //}

        //private void init()
        //{
        //    #region Соединение
        //    Constants.cons_Webdata = "Server=192.168.179.17;Port=5432;User Id=postgres;Password=postgres;Database=w1212;Preload Reader=true;";
        //    connDb = DBManager.GetConnection(Constants.cons_Webdata);
        //    DBManager.OpenDb(connDb, true);
        //    #endregion

        //    #region Инициализация контейнера для MonitorLog
        //    IocContainer.InitContainer();
        //    IocContainer.Current.Register(Component.For<IConfigProvider>().UsingFactoryMethod(x => FileConfigProvider.Init()).LifestyleSingleton());
        //    IocContainer.Current.Resolve<IConfigProvider>().GetConfig();
        //    IocContainer.InitLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));
        //    #endregion
        //}
        // создание тестовой таблицы
        //private Returns createCountersTestTable()
        //{
        //    string sqlCreateTable = "Create table if not exists  " + testTableName + " (" +
        //                            "num_ls integer, " +
        //                            "nzp_serv integer, " +
        //                            "nzp_counter integer, " +
        //                            "num_cnt char(50), " +
        //                            "dat_uchet date, " +
        //                            "val_cnt integer, " +
        //                            "is_actual integer); " +
        //                            " Insert into " + testTableName +
        //                            " (num_ls, nzp_serv, nzp_counter, num_cnt, dat_uchet, val_cnt, is_actual) Values " +
        //                            " (123, 6, 7565, '7fgh', '2014-09-01',56,1)," +
        //                            " (123, 9, 7566, '5A3N', '2014-09-01',23,1)," +
        //                            " (123, 7, 7567, '5678', '2014-09-01',8,0)," +
        //                            " (124, 6, 7568, '2456', '2015-01-01',8,1)," +
        //                            " (124, 7, 7569, 'KJ6578', '2015-01-01',8,1)";
        //    Returns ret = DBManager.ExecSQL(connDb, sqlCreateTable, false);
        //    if (!ret.result)
        //    {
        //        connDb.Close();
        //    }
        //    return ret;
        //}
    }
}
