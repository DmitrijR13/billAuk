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
    [Migration(2015060104201, MigrateDataBase.CentralBank)]
    public class Migration_2015060104201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var norm_prm_serv = new SchemaQualifiedObjectName() { Name = "norm_prm_serv", Schema = CurrentSchema };
            if (Database.TableExists(norm_prm_serv))
            {
                if (!Database.ColumnExists(norm_prm_serv, "nzp_serv_slave"))
                {
                    Database.AddColumn(norm_prm_serv, new Column("nzp_serv_slave", new ColumnType(DbType.Int32), ColumnProperty.NotNull, 0));
                    Database.Update(norm_prm_serv, new[] { "nzp_serv_slave" }, new[] { "7" }, " nzp_serv = 6");
                    Database.Update(norm_prm_serv, new[] { "nzp_serv_slave" }, new[] { "1007" }, " nzp_serv = 9");
                }
            }

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

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

}
