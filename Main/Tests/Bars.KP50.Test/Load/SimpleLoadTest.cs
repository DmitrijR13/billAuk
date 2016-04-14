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


namespace Bars.KP50.Test.Faktura
{
    [TestFixture]
    public class SimpleLoadTest
    {

        protected string GetSystemParams(string fileName)
        {
            return JsonConvert.SerializeObject(new
            {
                NzpUser = 1,
                NzpExcelUtility = 2,
                UserLogin = "websystem",
                PathForSave = @"D:\temp\import\"+fileName+".dbf"
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
            Constants.cons_Webdata = "Server=192.168.170.173;Port=5432;User Id=postgres;Password=postgres;Database=tula_dev;Preload Reader=true;";
            Constants.cons_Kernel = "Server=192.168.170.173;Port=5432;User Id=postgres;Password=postgres;Database=tula_dev;Preload Reader=true;";
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
            var loadDomCounters = new LoadObnCounters();

            loadDomCounters.GenerateLoad(new NameValueCollection
                                {
                                    { "UserParamValues", GetUserParams() },
                                    { "SystemParams",GetSystemParams("2")}
                                });

            Assert.IsTrue(!String.IsNullOrEmpty(loadDomCounters.ProtocolFileName), 
                "Не сформирован файл протокола загрузки ");
            
        }

        [Test]
        public void SimpleLoadTest2()
        {
            Init();
            var loadDomCounters = new LoadObnCounters();

            loadDomCounters.GenerateLoad(new NameValueCollection
                                {
                                    { "UserParamValues", GetUserParams() },
                                    { "SystemParams",GetSystemParams("1")}
                                });

            Assert.IsTrue(!String.IsNullOrEmpty(loadDomCounters.ProtocolFileName),
                "Не сформирован файл протокола загрузки ");

        }

    }
}
