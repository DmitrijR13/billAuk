using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014094
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014092309401, MigrateDataBase.CentralBank)]
    public class Migration_2014092309401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };

            if (Database.TableExists(file_serv))
            {
                if (Database.ColumnExists(file_serv, "reg_tarif_percent"))
                {
                    Database.ChangeColumn(file_serv, "reg_tarif_percent", DbType.Decimal.WithSize(14,7), true);
                }

                if (Database.ColumnExists(file_serv, "reg_tarif"))
                {
                    Database.ChangeColumn(file_serv, "reg_tarif", DbType.Decimal.WithSize(14, 7), true);
                }
            }
        }

        public override void Revert()
        {

            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };

            if (Database.TableExists(file_serv))
            {
                if (Database.ColumnExists(file_serv, "reg_tarif_percent"))
                {
                    Database.ChangeColumn(file_serv, "reg_tarif_percent", DbType.Decimal.WithSize(14, 3), true);
                }

                if (Database.ColumnExists(file_serv, "reg_tarif"))
                {
                    Database.ChangeColumn(file_serv, "reg_tarif", DbType.Decimal.WithSize(14, 3), true);
                }
            }
        }
    }
}
