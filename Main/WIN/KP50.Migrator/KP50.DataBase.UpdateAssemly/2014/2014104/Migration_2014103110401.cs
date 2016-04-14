using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014103110401, Migrator.Framework.DataBase.Web)]
    public class Migration_2014103110401 : Migration
    {
        public override void Apply()
        {
            for (int i = 0; i < 4; i++)
            {
                SchemaQualifiedObjectName calc_fon = new SchemaQualifiedObjectName() { Name = string.Format("calc_fon_{0}", i.ToString("0")), Schema = CurrentSchema };
                if (Database.TableExists(calc_fon) && !Database.ColumnExists(calc_fon, "ip_adr")) Database.AddColumn(calc_fon, new Column("ip_adr", DbType.String.WithSize(15)));
            }

            SchemaQualifiedObjectName bill_fon = new SchemaQualifiedObjectName(){ Name = "bill_fon", Schema = CurrentSchema };
            if (Database.TableExists(bill_fon))
            {
                if (!Database.ColumnExists(bill_fon, "ip_adr"))
                {
                    Database.AddColumn(bill_fon, new Column("ip_adr", DbType.String.WithSize(15)));
                }
            }
        }

        public override void Revert()
        {
            for (int i = 0; i < 4; i++)
            {
                SchemaQualifiedObjectName calc_fon = new SchemaQualifiedObjectName() { Name = string.Format("calc_fon_{0}", i.ToString("0")), Schema = CurrentSchema };
                if (Database.TableExists(calc_fon) && Database.ColumnExists(calc_fon, "ip_adr")) Database.RemoveColumn(calc_fon, "ip_adr");
            }

            SchemaQualifiedObjectName bill_fon = new SchemaQualifiedObjectName() { Name = "bill_fon", Schema = CurrentSchema };
            if (Database.TableExists(bill_fon))
            {
                if (Database.ColumnExists(bill_fon, "ip_adr"))
                {
                    Database.RemoveColumn(bill_fon, "ip_adr");
                }
            }
        }
    }
}
