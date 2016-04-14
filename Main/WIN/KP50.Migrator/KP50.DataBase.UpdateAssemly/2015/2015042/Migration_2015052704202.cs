using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015052704202, MigrateDataBase.CentralBank)]
    public class Migration_2015052704202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };

            object obj = Database.ExecuteScalar("SELECT count(*) FROM " + s_payer.Schema + Database.TableDelimiter + s_payer.Name +
                " WHERE nzp_payer = 0;");
            var count = Convert.ToInt32(obj);

            if (count == 0)
            {
                Database.Insert(s_payer, new string[]
                {
                    "nzp_payer", "payer", "npayer"
                }, new string[]
                {
                    "0", "Неопределенный поставщик", "Неопределенный поставщик"
                });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }

}
