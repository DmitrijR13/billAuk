using Castle.MicroKernel.Registration;
using Globals.SOURCE.Config;
using Globals.SOURCE.Container;
using NUnit.Framework;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.IO;


namespace Bars.KP50.Test.Report
{
    [TestFixture]
    public class DbRevalReasonTest
    {



        [Test]
        public void GenerateLs()
        {
            Constants.cons_Webdata =
                "Server=192.168.224.23;Port=5432;User Id=postgres;Password=postgres;Database=w0203;Preload Reader=true;";
           var connWeb = DBManager.GetConnection(Constants.cons_Webdata);
            Points.Pref = "nftul";
            //инициализация логгера
            IocContainer.InitContainer(true);
            IocContainer.Current.Register(Component.For<IConfigProvider>().UsingFactoryMethod(x => FileConfigProvider.Init()).LifestyleSingleton());
            IocContainer.Current.Resolve<IConfigProvider>().GetConfig();
            IocContainer.InitLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));

           if (!DBManager.OpenDb(connWeb, true).result) return;

            using (var dbReason = new DbRevalReason(connWeb))
            {
                //dbReason.GenerateLs(39912, 2015, 1);
                dbReason.GenerateLs(360282, 2015, 1);
            }

        }

        [Test]
        public void Generate()
        {
            Constants.cons_Webdata =
                     "Server=192.168.224.23;Port=5432;User Id=postgres;Password=postgres;Database=w0203;Preload Reader=true;";
            var connWeb = DBManager.GetConnection(Constants.cons_Webdata);
            Points.Pref = "nftul";
            //инициализация логгера
            IocContainer.InitContainer(true);
            IocContainer.Current.Register(Component.For<IConfigProvider>().UsingFactoryMethod(x => FileConfigProvider.Init()).LifestyleSingleton());
            IocContainer.Current.Resolve<IConfigProvider>().GetConfig();
            IocContainer.InitLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));

            if (!DBManager.OpenDb(connWeb, true).result) return;

            using (var dbReason = new DbRevalReason(connWeb))
            {
                //dbReason.GenerateLs(39912, 2015, 1);
                dbReason.Generate("ntul07_data.kvar", 2015, 1);
            }

        }


    }
}
