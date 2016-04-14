using NUnit.Framework;
using STCLINE.KP50.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bars.KP50.Test.Utils
{
    [TestFixture]
    public class DatabaseConnectionKernelTest
    {
        [Test]
        public void TestGetConnection()
        {
            var RealDbConnectionString = "Server=192.168.179.17;Port=5432;User Id=postgres;Password=postgres;Database=postgres;Preload Reader=true;";
            var WrongDbConnectionString = "Server=localhost;Port=5432;User Id=postgres;Password=postgres;Database=DropIt;Preload Reader=true;";

            var wrongInstance = DatabaseConnectionKernel.GetConnection(WrongDbConnectionString);
            Assert.IsNull(wrongInstance, "Похоже что-то создалось");

            var _tasks = new List<Task>();
            for (int j = 0; j < 1000; j++)
            {
                _tasks.Add(Task.Factory.StartNew(delegate
                {
                    var instance = DatabaseConnectionKernel.GetConnection(RealDbConnectionString);
                    Assert.IsTrue(instance != null, "Не удалось создать подключение");

                    for (var i = 0; i < 10000; i++)
                    {
                        var connection = DatabaseConnectionKernel.GetConnection(RealDbConnectionString);
                        Assert.IsTrue(instance == connection, "Вернулось новое подключение");
                    }
                }));
            }
            Task.WaitAll(_tasks.ToArray());
            DatabaseConnectionKernel.Instance.Dispose();
        }
    }
}
