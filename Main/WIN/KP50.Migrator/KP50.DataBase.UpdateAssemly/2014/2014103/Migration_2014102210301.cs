using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014103
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014102210301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014102210301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var kodsum = new SchemaQualifiedObjectName { Name = "kodsum", Schema = CurrentSchema };
            if (Database.TableExists(kodsum))
            {
                if (!Database.ColumnExists(kodsum, "is_erc"))
                {
                    Database.AddColumn(kodsum,
                        new Column("is_erc", DbType.Int32, defaultValue: 0));
                }
                Database.Update(kodsum, new string[] { "is_erc" }, new string[] { "1" }, "kod in (33, 57)");
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            var kodsum = new SchemaQualifiedObjectName { Name = "kodsum", Schema = CurrentSchema };
            if (Database.TableExists(kodsum))
            {
                if (!Database.ColumnExists(kodsum, "is_erc"))
                {
                    Database.RemoveColumn(kodsum,"is_erc");
                }
            }

        }
    }

}
