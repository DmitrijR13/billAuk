using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using System.Reflection;
using System.IO;

namespace KP50.DataBase.Update
{
    using Migrator = KP50.DataBase.Migrator.Migrator;
    using KP50.DataBase.Migrator.Exceptions;
    class Program
    {
        static int Main(string[] args)
        {
            bool boolNoHelp = false;
            try
            {
                int intMigratorOut;
                UpdateInfo inf = new UpdateInfo(args);
                boolNoHelp = inf.NoHelp;
                Migrator migrator = new Migrator(inf.Provider, inf.ConnectionString, Assembly.LoadFile(Path.GetFullPath(inf.AssemblyPath)), inf.CentralPrefix);
                migrator.onMigrate += new Migrator.MigratorEventHandler(WriteConsole);
                if (!inf.CheckUpdate) Console.WriteLine("Старт процесса обновления.");
                intMigratorOut = migrator.Migrate(inf.CentralPrefix, inf.Version, inf.CheckUpdate, inf.IgnoreLocalBank);
                if (!string.IsNullOrWhiteSpace(inf.WebConnection))
                {
                    migrator = new Migrator(inf.Provider, inf.WebConnection, Assembly.LoadFile(Path.GetFullPath(inf.AssemblyPath)), null);
                    migrator.onMigrate += new Migrator.MigratorEventHandler(WriteConsole);
                    intMigratorOut += migrator.Migrate(null, inf.Version, inf.CheckUpdate, inf.IgnoreLocalBank);
                }
                if (inf.CheckUpdate)
                {
                    Console.WriteLine(string.Format("Доступно новых обновлений банков данных: {0}.", intMigratorOut));
                    return intMigratorOut;
                }
            }
            catch (VersionException ex)
            {
                Console.WriteLine(
                    string.Format(ex.Message + "\nВерсии: {0}", 
                    string.Join(", ", ex.Versions.ToArray())));
                if (!boolNoHelp)
                {
                    ShowHelp();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (!boolNoHelp)
                {
                    ShowHelp();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
                return -1;
            }
            return 0;
        }

        public static void ShowHelp()
        {
            Console.WriteLine("USING:");
            Console.WriteLine("KP50.DataBase.Update.exe -i | --informix         INFORMIX PROVIDER");
            Console.WriteLine("                         -p | --postgresql       POSTGRESQL PROVIDER");
            Console.WriteLine("                         -a | --assembly         PATH TO UPDATE LIBRARY");
            Console.WriteLine("                         -f | --prefix           PREFIX OF CENTRAL BANK");
            Console.WriteLine("                        [-u | --checkupdates     CHECK DATABASE FOR UPDATES   ]");
//          Console.WriteLine("                        [-c | --config           PATCH TO HOST.CONFIG FILE    ]");
            Console.WriteLine("                        [-s | --connectionstring CONNECTION STRING TO DATABASE]");
            Console.WriteLine("                        [-w | --webconnection    CONNECTION STRING TO WEB     ]");
            Console.WriteLine("                        [-v | --version          UPDATE TO VERSION            ]");
            Console.WriteLine("                        [-h | --help             SHOW THIS HELP               ]");
            Console.WriteLine("                        [-n | --no-help          DON'T PRINT HELP ON EXCEPTION]");
            Console.WriteLine();
            Console.WriteLine("EXAMPLE:");
            Console.WriteLine("KP50.DataBase.Update.exe -i");
            Console.WriteLine("                         -a KP50.DataBase.UpdateAssemly.dll");
            Console.WriteLine("                         -c \"C:\\HOST\\HOST.CONFIG\"");
            Console.WriteLine("                         -f fbank");
        }

        private static void WriteConsole(string msg) { Console.Write(msg); }
    }
}
