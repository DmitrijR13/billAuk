using System.Data;
using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015030202304, MigrateDataBase.CentralBank)]
    public class Migration_2015030202304CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var simple_pay_file = new SchemaQualifiedObjectName
            {
                Name = "simple_pay_file",
                Schema = CurrentSchema
            };
            if (!Database.TableExists(simple_pay_file))
            {
                Database.AddTable(simple_pay_file,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_load", DbType.Int32),
                    new Column("data_type", DbType.DateTime));
                Database.AddPrimaryKey("simple_pay_file_pkey", simple_pay_file, "id");
            }


        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            var simple_pay_file = new SchemaQualifiedObjectName
            {
                Name = "simple_pay_file",
                Schema = CurrentSchema
            };
            if (Database.TableExists(simple_pay_file)) Database.RemoveTable(simple_pay_file);
        }
    }
}


