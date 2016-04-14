using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Globals.SOURCE;
using Globals.SOURCE.Config;
using Globals.SOURCE.Container;
using NUnit.Framework;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace Bars.KP50.Test.Utils
{
    [TestFixture]
    public class Transfer
    {
        [Test]
        public void TestTransfer()
        {
            Constants.cons_Webdata =
                "Server=LinePG;Port=5432;User Id=postgres;Password=postgres;Database=webtul;Preload Reader=true;";
            Constants.cons_Kernel = Constants.cons_Webdata;
            Points.Pref = "nftul";
            var cont = new TransferParams
            {
                nzp_user = 1,
                user = new User() { nzp_user = 1 },
                houses = new List<Dom>()
                {
                    new Dom {nzp_dom = 321},
                    new Dom {nzp_dom = 8},
                    new Dom {nzp_dom = 2}
                },
                fPoint = new _Point { nzp_wp = 25, pref = "ntul01" },
                tPoint = new _Point { nzp_wp = 27, pref = "ntul03" }
            };
            var db = new DbExchange();
            IocContainer.InitContainer(true);
            IocContainer.Current.Register(Component.For<IConfigProvider>().UsingFactoryMethod(x => FileConfigProvider.Init()).LifestyleSingleton());
            IocContainer.Current.Resolve<IConfigProvider>().GetConfig();
            IocContainer.InitLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));
            db.StartTransfer(cont);
        }
    }
}
