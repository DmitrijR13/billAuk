using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015041
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015041104101, MigrateDataBase.CentralBank)]
    public class Migration_2015041104101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);

            SchemaQualifiedObjectName file_serv_dist = new SchemaQualifiedObjectName() { Name = "file_serv_dist", Schema = CurrentSchema };

            if (!Database.TableExists(file_serv_dist))
            {
                Database.AddTable(file_serv_dist,
                    new Column("idsrv", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("id", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_charge", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dat_dist", DbType.Date, ColumnProperty.NotNull, "current_date")
                    );
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
