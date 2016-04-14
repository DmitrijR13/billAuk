using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014080508202, MigrateDataBase.LocalBank)]
    public class Migration_2014080508202_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade LocalPref_Kernel
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };

            if (Database.TableExists(res_x))
            {
                Database.ChangeColumn(res_x, "name_x", DbType.String.WithSize(255), false);
            }

            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };

            if (Database.TableExists(res_y))
            {
                Database.ChangeColumn(res_y, "name_y", DbType.String.WithSize(255), false);
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };

            if (Database.TableExists(res_x))
            {
                Database.ChangeColumn(res_x, "name_x", DbType.String.WithSize(60), false);
            }

            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };

            if (Database.TableExists(res_y))
            {
                Database.ChangeColumn(res_y, "name_y", DbType.String.WithSize(60), false);
            }
        }
    }


}
