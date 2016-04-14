using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014102810401, Migrator.Framework.DataBase.Web)]
    public class Migration_2014102810401 : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName bill_fon = new SchemaQualifiedObjectName() { Name = "bill_fon", Schema = CurrentSchema };

            if (Database.TableExists(bill_fon))
            {
                if (!Database.ColumnExists(bill_fon, "with_uk"))
                {
                    Database.AddColumn(bill_fon, new Column("with_uk", DbType.Int16));
                }
                if (!Database.ColumnExists(bill_fon, "with_geu"))
                {
                    Database.AddColumn(bill_fon, new Column("with_geu", DbType.Int16));
                }
                if (!Database.ColumnExists(bill_fon, "with_uchastok"))
                {
                    Database.AddColumn(bill_fon, new Column("with_uchastok", DbType.Int16));
                }
            }
        }
    }
}
