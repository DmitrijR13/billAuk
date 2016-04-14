using System.IO;
using Bars.KP50.Load.Obninsk;
using Bars.QueueCore;
using Newtonsoft.Json;
using NUnit.Framework;
using STCLINE.KP50.Global;
using System.Collections.Specialized;
using Globals.SOURCE.Container;
using Castle.MicroKernel.Registration;
using Globals.SOURCE.Config;
using System;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.Collections.Generic;


namespace Bars.KP50.Test.Faktura
{
    [TestFixture]
    public class LoadSuppChargeTest
    {

        protected string GetSystemParams(string fileName)
        {
            return JsonConvert.SerializeObject(new
            {
                NzpUser = 1,
                NzpExcelUtility = 2,
                UserLogin = "websystem",
                DateLoad = DateTime.Now.ToShortDateString(),
                nzpSupp =1,
                pref = "ntul01",
                PathForSave = @"D:\temp\import\"+fileName+".csv"
            });
        }

        protected string GetUserParams()
        {
            return JsonConvert.SerializeObject(new
            {
                Test ="test"
            });
        }


        protected void Init()
        {
            Constants.cons_Webdata = "Server=192.168.170.173;Port=5432;User Id=postgres;Password=postgres;Database=tula_test_20140818;Preload Reader=true;";
            Constants.cons_Kernel = "Server=192.168.170.173;Port=5432;User Id=postgres;Password=postgres;Database=tula_test_20140818;Preload Reader=true;";
            IocContainer.InitContainer(true);
            IocContainer.Current.Register(Component.For<IConfigProvider>().UsingFactoryMethod(x => FileConfigProvider.Init()).LifestyleSingleton());
            IocContainer.Current.Resolve<IConfigProvider>().GetConfig();
            IocContainer.InitLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));
            Points.Pref = "nftul";
            Constants.Directories.FilesDir = @"d:\temp\";
        }

        [Test]
        public void SimpleLoadTest1()
        {
            Init();
            var loadSuppCharge = new LoadSuppCharge();

            loadSuppCharge.GenerateLoad(new NameValueCollection
                                {
                                    { "UserParamValues", GetUserParams() },
                                    { "SystemParams",GetSystemParams("1")}
                                });

            Assert.IsTrue(!String.IsNullOrEmpty(loadSuppCharge.ProtocolFileName), 
                "Не сформирован файл протокола загрузки ");
            
        }

        [Test]
        public void SimpleLoadTest2()
        {
            Init();
            var loadSuppCharge = new LoadSuppCharge();

            loadSuppCharge.GenerateLoad(new NameValueCollection
                                {
                                    { "UserParamValues", GetUserParams() },
                                    { "SystemParams",GetSystemParams("2")}
                                });

            Assert.IsTrue(!String.IsNullOrEmpty(loadSuppCharge.ProtocolFileName),
                "Не сформирован файл протокола загрузки ");

        }

        [Test]
        public void SimpleGetServTest()
        {
            Init();
            using (var suppChargeService = new SuppChargeService())
            {

                var services = suppChargeService.GetServiceNameLoadSuppCharge(7);

                Assert.IsTrue(services.Count == 1, "Список услуг пустой! ");
            }
        }


        [Test]
        public void DisassembleTest()
        {
            Init();
            using (var suppChargeService = new SuppChargeService())
            {
                var servGood = new Dictionary<string, int> {{"Стоки", 7}};

                var ret = suppChargeService.DisassemleLoadSuppCharge(7, servGood);

                Assert.IsTrue(ret.result, "Ошибка при разборе начислений от сторонних поставщиков");
            }
        }

        [Test]
        public void DeleteLoadTest()
        {
            Init();
            using (var suppChargeService = new SuppChargeService())
            {
                var ret = suppChargeService.DeleteSuppCharge(5);

                Assert.IsTrue(ret.result, "Ошибка при разборе начислений от сторонних поставщиков");
            }
        }
    }
}
