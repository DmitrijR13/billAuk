using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015050404201, MigrateDataBase.Web)]
    public class Migration_2015050404201 : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName bill_fon = new SchemaQualifiedObjectName() { Name = "bill_fon", Schema = CurrentSchema };

            if (Database.TableExists(bill_fon))
            {
                if (!Database.ColumnExists(bill_fon, "close_ls"))
                {
                    Database.AddColumn(bill_fon, new Column("close_ls", DbType.Int16,ColumnProperty.None,0));
                }
                if (!Database.ColumnExists(bill_fon, "zero_nach"))
                {
                    Database.AddColumn(bill_fon, new Column("zero_nach", DbType.Int16, ColumnProperty.None, 0));
                }
               
            }
        }
    }
}
