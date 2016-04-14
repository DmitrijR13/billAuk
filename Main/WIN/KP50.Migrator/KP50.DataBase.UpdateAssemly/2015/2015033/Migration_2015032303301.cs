using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015033
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015032303301, MigrateDataBase.CentralBank)]
    public class Migration_2015032303301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var s_group_check = new SchemaQualifiedObjectName() { Name = "s_group_check", Schema = CurrentSchema };

            if (!Database.TableExists(s_group_check))
            {
                Database.AddTable(s_group_check,
                    new Column("nzp_group", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("ngroup", DbType.StringFixedLength.WithSize(80)));
            }

            if (Database.TableExists(s_group_check))
            {
                Database.Delete(s_group_check, " nzp_group in (20)");
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие у открытых ЛС услуг без начислений" });
            }

           SetSchema(Bank.Data);
            var s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };

            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group in (20)");
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие у открытых ЛС услуг без начислений" });
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
    [Migration(2015032303301, MigrateDataBase.LocalBank)]
    public class Migration_2015032303301_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            var s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };

            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group in (20)");
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие у открытых ЛС услуг без начислений" });
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
