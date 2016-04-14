using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015055
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015061505501, MigrateDataBase.CentralBank)]
    public class Migration_2015061505501_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var s_group_check = new SchemaQualifiedObjectName() { Name = "s_group_check", Schema = CurrentSchema };

            if (Database.TableExists(s_group_check))
            {
                Database.Delete(s_group_check, " nzp_group in (17,20,21)");
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "17", "П-наличие показаний ИПУ без ИПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие показаний ОДПУ без ОДПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "21", "П-наличие показаний Груп.ПУ без ПУ" });
            }


            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };
            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group in (17,20,21)");
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "17", "П-наличие показаний ИПУ без ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие показаний ОДПУ без ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "21", "П-наличие показаний Груп.ПУ без ПУ" });
            }

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
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

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015061505501, MigrateDataBase.LocalBank)]
    public class Migration_2015061505501_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade LocalPref_Kernel

            SetSchema(Bank.Data);


            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };
            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group in (17,20,21)");
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "17", "П-наличие показаний ИПУ без ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие показаний ОДПУ без ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "21", "П-наличие показаний Груп.ПУ без ПУ" });
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }
    }

}
