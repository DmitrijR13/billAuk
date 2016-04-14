using KP50.DataBase.Migrator.Framework;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014091009202, MigrateDataBase.CentralBank)]
    public class Migration_2014091009202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName simple_load = new SchemaQualifiedObjectName() { Name = "simple_load", Schema = CurrentSchema };
            if (!Database.ColumnExists(simple_load, "nzp_supp")) Database.AddColumn(simple_load, new Column("nzp_supp", DbType.Int32));
            if (!Database.ColumnExists(simple_load, "nzp_wp")) Database.AddColumn(simple_load, new Column("nzp_wp", DbType.Int32));
            if (!Database.ColumnExists(simple_load, "month_")) Database.AddColumn(simple_load, new Column("month_", DbType.Int32));
            if (!Database.ColumnExists(simple_load, "year_")) Database.AddColumn(simple_load, new Column("year_", DbType.Int32));
            if (!Database.ColumnExists(simple_load, "parsing_status")) Database.AddColumn(simple_load, new Column("parsing_status", DbType.Int32));            
            if (!Database.ColumnExists(simple_load, "download_status")) Database.AddColumn(simple_load, new Column("download_status", DbType.Int32));
        }
    }
}
