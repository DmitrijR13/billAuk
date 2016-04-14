using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014100110106, MigrateDataBase.CentralBank)]
    public class Migration_2014100110106 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName norm_types = new SchemaQualifiedObjectName();
            norm_types.Name = "norm_types";
            norm_types.Schema = CurrentSchema;
            if (Database.TableExists(norm_types))
            {
                if (!Database.ColumnExists(norm_types, "date_from_old"))
                {
                    Database.AddColumn(norm_types, new Column("date_from_old", DbType.Date));
                }
                if (!Database.ColumnExists(norm_types, "date_to_old"))
                {
                    Database.AddColumn(norm_types, new Column("date_to_old", DbType.Date));
                }
            }
        }
    }
}
