using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bars.KP50.Utils;
using Castle.Components.DictionaryAdapter;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using Globals.SOURCE.Config;
using Globals.SOURCE.Container;
using Globals.SOURCE.GLOBAL;
using NUnit.Framework;
using STCLINE.KP50.Interfaces;
using Bars.KP50.DB.Exchange.Unload;
using STCLINE.KP50.Global;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Test.Utils
{
    [TestFixture]
    public class FieldUnloadTest
    {
        [Test]
        public void FieldUnload()
        {
            var field = new STCLINE.KP50.Interfaces.Field();
            field.N = "Version";
            field.NT = "Номер выгрузки";
            field.IS = 1;
            field.P = 1;
            field.T = "TextType";
            field.L = 25;
            field.V = "1";
            string result = field.ToString();
            Assert.IsTrue(result == "{\"Name\":\"Version\",\"NameText\":\"Номер выгрузки\",\"IsRequired\":1,\"Place\":1,\"Type\":\"TextType\",\"Length\":25,\"Value\":\"1\"}", "Неправильно посчитано контрольное число");
        }


    
        [Test]
        public void FieldUnload2()
        {
            var field = new STCLINE.KP50.Interfaces.Field();
            var field2 = new STCLINE.KP50.Interfaces.Field();
            field.N = "Version";
            field.NT = "Номер выгрузки";
            field.IS = 1;
            field.P = 1;
            field.T = "TextType";
            field.L = 25;
            field.V = "1";

            field2.N = "LoadDate";
            field2.NT = "Дата загрузки";
            field2.IS = 1;
            field2.P = 2;
            field2.T = "DateType";
            field2.L = 0;
            field2.V = "01.01.2014";

            var fields = new STCLINE.KP50.Interfaces.FieldsUnload();
            fields.F.Add(field);
            fields.F.Add(field2);
            string result = fields.ToString();
            Assert.IsTrue(result == "{\"Fields\":[{\"Name\":\"Version\",\"NameText\":\"Номер выгрузки\",\"IsRequired\":1,\"Place\":1,\"Type\":\"TextType\",\"Length\":25,\"Value\":\"1\"},{\"Name\":\"LoadDate\",\"NameText\":\"Дата загрузки\",\"IsRequired\":1,\"Place\":2,\"Type\":\"DateType\",\"Length\":0,\"Value\":\"01.01.2014\"}]}", "Неправильно посчитано контрольное число");
        }

        [Test]
        public void FieldUnload3()
        {
            var field = new STCLINE.KP50.Interfaces.Field();
            var field2 = new STCLINE.KP50.Interfaces.Field();

            field.N = "Version";
            field.NT = "Номер выгрузки";
            field.IS = 1;
            field.P = 1;
            field.T = "TextType";
            field.L = 25;
            field.V = "1";

            field2.N = "LoadDate";
            field2.NT = "Дата загрузки";
            field2.IS = 1;
            field2.P = 2;
            field2.T = "DateType";
            field2.L = 0;
            field2.V = "01.01.2014";

            var fields = new STCLINE.KP50.Interfaces.FieldsUnload();
            var fields2 = new STCLINE.KP50.Interfaces.FieldsUnload();

            fields.F.Add(field);
            fields.F.Add(field2);
            fields2.F.Add(field);
            fields2.F.Add(field2);

            var data = new STCLINE.KP50.Interfaces.DataFields();

            data.Data.Add(fields);
            data.Data.Add(fields2);

            string result = data.ToString();

            Assert.IsTrue(result == "{\"Data\":[{\"Fields\":[{\"Name\":\"Version\",\"NameText\":\"Номер выгрузки\",\"IsRequired\":1,\"Place\":1,\"Type\":\"TextType\",\"Length\":25,\"Value\":\"1\"},{\"Name\":\"LoadDate\",\"NameText\":\"Дата загрузки\",\"IsRequired\":1,\"Place\":2,\"Type\":\"DateType\",\"Length\":0,\"Value\":\"01.01.2014\"}]},{\"Fields\":[{\"Name\":\"Version\",\"NameText\":\"Номер выгрузки\",\"IsRequired\":1,\"Place\":1,\"Type\":\"TextType\",\"Length\":25,\"Value\":\"1\"},{\"Name\":\"LoadDate\",\"NameText\":\"Дата загрузки\",\"IsRequired\":1,\"Place\":2,\"Type\":\"DateType\",\"Length\":0,\"Value\":\"01.01.2014\"}]}]}", "Неправильно посчитано контрольное число");
        }

        [Test]
        public void FieldUnload4()
        {
            var reg = new RegisterSupplData();
            reg.Writer = new StreamWriter("C:/files/" + reg.Name + ".txt");
            reg.Start();
            reg.Writer.Flush();
            reg.Writer.Close();
        }

        [Test]
        public void FieldUnload5()
        {//инициализация логгера
            IocContainer.InitContainer(true);
            IocContainer.Current.Register(Component.For<IConfigProvider>().UsingFactoryMethod(x => FileConfigProvider.Init()).LifestyleSingleton());
            IocContainer.Current.Resolve<IConfigProvider>().GetConfig();
            IocContainer.InitLogger(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));

            //если нужно соединение с бд то определяется префикс и строка подключения
            Points.Pref = "nftul";
            Constants.cons_Webdata = "Server=LinePG;Port=5432;User Id=postgres;Password=postgres;Database=tula_dev;Preload Reader=true;";
            //using (var c = new CheckBeforeClosingExample())
            //{
            //    c.CreateCheckBeforeClosingReport(new CheckBeforeClosingParams());
            //}
        }
    }
}
