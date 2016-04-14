using System;
using KP50.DataBase.Migrator.Framework;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014093010101, MigrateDataBase.Web)]
    public class Migration_2014093010101_Web : Migration
    {
        public override void Apply()
        {

            var pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };

            if (Database.TableExists(pack_ls))
            {
                if (!Database.ColumnExists(pack_ls, "month_from"))
                {
                    Column month_from = new Column("month_from",DbType.Date);
                    Database.AddColumn(pack_ls,month_from);
                }
                if (!Database.ColumnExists(pack_ls, "month_to"))
                {
                    Column month_to = new Column("month_to", DbType.Date);
                    Database.AddColumn(pack_ls, month_to);
                }
                if (!Database.ColumnExists(pack_ls, "type_pay"))
                {
                    Column type_pay = new Column("type_pay", DbType.Int32, ColumnProperty.NotNull, 1);
                    Database.AddColumn(pack_ls, type_pay);
                }
            }
            
        }
    }
}
