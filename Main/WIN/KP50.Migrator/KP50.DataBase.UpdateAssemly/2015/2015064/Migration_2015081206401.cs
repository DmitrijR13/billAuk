using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015081206401, MigrateDataBase.CentralBank)]
    public class Migration_2015081206401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName serv_norm_koef = new SchemaQualifiedObjectName() { Name = "serv_norm_koef", Schema = CurrentSchema };
            if (!Database.TableExists(serv_norm_koef))
            {
                Database.AddTable(serv_norm_koef,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_serv_link", DbType.Int32),
                    new Column("nzp_frm", DbType.Int32),
                    new Column("nzp_prm", DbType.Int32)
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
