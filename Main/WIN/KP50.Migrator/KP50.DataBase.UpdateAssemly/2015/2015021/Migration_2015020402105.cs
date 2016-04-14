using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020402105, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015020402105_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var s_bill_archive = new SchemaQualifiedObjectName { Name = "s_bill_archive", Schema = CurrentSchema };
            if (Database.TableExists("s_bill_archive"))
            {
                Database.RemoveTable(s_bill_archive);
            }
        }
    }
}
