using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014100110103, MigrateDataBase.LocalBank)]
    public class Migration_2014100110103 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName counters = new SchemaQualifiedObjectName() { Name = "counters", Schema = CurrentSchema };
            if (!Database.ColumnExists(counters, "ist"))
            {
                Database.AddColumn(counters, new Column("ist", DbType.Int32, 0));
            }

            SchemaQualifiedObjectName counters_dom = new SchemaQualifiedObjectName() { Name = "counters_dom", Schema = CurrentSchema };
            if (!Database.ColumnExists(counters_dom, "ist"))
            {
                Database.AddColumn(counters_dom, new Column("ist", DbType.Int32, 0));
            }

            SchemaQualifiedObjectName counters_group = new SchemaQualifiedObjectName() { Name = "counters_group", Schema = CurrentSchema };
            if (!Database.ColumnExists(counters_group, "ist"))
            {
                Database.AddColumn(counters_group, new Column("ist", DbType.Int32, 0));
            }
        }
    }
}
