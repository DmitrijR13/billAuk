using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014084
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014082008403, MigrateDataBase.CentralBank)]
    public class Migration_2014082008403_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_kvar = new SchemaQualifiedObjectName() { Name = "file_kvar", Schema = CurrentSchema };

            if (!Database.ColumnExists(file_kvar, "dom_id_char"))
            {
                Database.AddColumn(file_kvar, new Column("dom_id_char", DbType.String.WithSize(20)));
            }

            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_odpu = new SchemaQualifiedObjectName() { Name = "file_odpu", Schema = CurrentSchema };

            if (!Database.ColumnExists(file_odpu, "dom_id_char"))
            {
                Database.AddColumn(file_odpu, new Column("dom_id_char", DbType.String.WithSize(20)));
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }

}
