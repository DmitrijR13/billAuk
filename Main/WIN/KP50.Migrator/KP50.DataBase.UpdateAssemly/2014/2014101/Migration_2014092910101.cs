using KP50.DataBase.Migrator.Framework;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014092910101, MigrateDataBase.LocalBank)]
    public class Migration_2014092910101_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var gil_periods = new SchemaQualifiedObjectName { Name = "gil_periods", Schema = CurrentSchema };
            if (!Database.ColumnExists(gil_periods, "no_podtv_docs"))
            {
                Database.AddColumn(gil_periods, new Column("no_podtv_docs", DbType.Int32));
            }
        }
    }
}


