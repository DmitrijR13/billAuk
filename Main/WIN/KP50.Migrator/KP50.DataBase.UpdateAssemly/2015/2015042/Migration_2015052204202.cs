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
    [Migration(2015052204202, MigrateDataBase.CentralBank)]
    public class Migration_2015052204202_CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Kernel);

            var s_group_check = new SchemaQualifiedObjectName() { Name = "s_group_check", Schema = CurrentSchema };
            if (Database.TableExists(s_group_check))
            {
                Database.Delete(s_group_check, "nzp_group = 20");
            }

            SetSchema(Bank.Data);

            var checkchmon = new SchemaQualifiedObjectName() { Name = "checkchmon", Schema = CurrentSchema };
            if (Database.TableExists(checkchmon))
            {
                Database.Delete(checkchmon, "nzp_grp  = 20");
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


    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015052204202, MigrateDataBase.LocalBank)]
    public class Migration_2015052204202_LocalBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);

            var s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };
            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, "nzp_group = 20");
            }

            var link_group = new SchemaQualifiedObjectName() { Name = "link_group", Schema = CurrentSchema };
            if (Database.TableExists(link_group))
            {
                Database.Delete(link_group, "nzp_group = 20");
            }
        }

        public override void Revert()
        {

        }
    }
}
