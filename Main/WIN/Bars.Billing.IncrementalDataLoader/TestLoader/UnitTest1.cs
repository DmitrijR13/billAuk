using System;
using System.Collections.Generic;
using System.IO;
using Bars.Billing.IncrementalDataLoader;
using Bars.Billing.IncrementalDataLoader.Utils;
using Bars.Billing.IncrementalDataLoader.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestLoader
{
    /// <summary>
    /// Класс для тестирования загрузки в ПГУ
    /// </summary>
    [TestClass]
    public class TestLoadPgu
    {
        /// <summary>
        /// Тест загрузки в ПГУ
        /// </summary>
        [TestMethod]
        public void TestLoadManyFilesIntoPgu()
        {
            var connect = new ConfigurationParams()
            {
                //строка соединения к пром серверу ПГУ
                connectionString =
                    "Server=192.168.229.21;Port=5432;User Id=postgres;Password=postgres;Database=portal_test;Preload Reader=true;",
                database = "portal_test",
                password = "postgres",
                port = "5432",
                psqlPath = @"D:\Program Files\PostgreSQL\9.3\bin\psql",
                server = "192.168.229.21",
                user = "postgres"
                
                //строка соединения к тестовому серверу ПГУ
                //connectionString =
                //    "Server=linepg;Port=5432;User Id=postgres;Password=postgres;Database=portal_test_settings;Preload Reader=true;",
                //database = "portal_test_settings",
                //password = "postgres",
                //port = "5432",
                //psqlPath = @"D:\Program Files\PostgreSQL\9.3\bin\psql",
                //server = "linepg",
                //user = "postgres"
            };

            LogUtils.EnableLogger(true);
            LogUtils.WriteLog(String.Format("Test '{0}' started.\n ", System.Reflection.MethodBase.GetCurrentMethod()),
                ELogType.Info);

            foreach (var oneFile in new DirectoryInfo(@"D:\public\_tmp\mgf\pgu\for load\").GetFiles())
            {
                var newFileName =
                    Path.GetFileNameWithoutExtension(oneFile.Name)
                        .Replace(')', '_')
                        .Replace('(', '_')
                        .Replace(',', '_')
                        .Replace('.', '_')
                        .Replace(' ', '_') +
                    DateTime.Now.Ticks +
                    oneFile.Extension;
                File.Copy(oneFile.FullName, Path.Combine(oneFile.DirectoryName, newFileName));
                Instance.LoaderInstance.StartOperationPGU(connect, new List<OtherParams>()
                {
                    new OtherParams()
                    {
                        FileName = newFileName,
                        GisFileId = Convert.ToInt64(DateTime.Now.ToString("yyMMddhhmmss")),
                        nzp_user = 301,
                        OriginalName = oneFile.Name,
                        Path = oneFile.DirectoryName,
                        RegistrationName = "PGU",
                        EnableLogger = false
                    }
                });

                File.Delete(Path.Combine(oneFile.DirectoryName, newFileName));
            }

            LogUtils.EnableLogger(true);

            LogUtils.WriteLog(String.Format("Test '{0}' finished.\n ", System.Reflection.MethodBase.GetCurrentMethod()),
                ELogType.Info);
        }
    }
}
