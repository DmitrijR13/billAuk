using System.Text;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System.Data;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014120512101, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2014120512101_CentralBank : Migration
    {
         public override void Apply()
         {
             SetSchema(Bank.Data);
             var s_place_requirement = new SchemaQualifiedObjectName() { Name = "s_place_requirement", Schema = CurrentSchema };

             if (!Database.TableExists(s_place_requirement))
             {
                 Database.AddTable(s_place_requirement,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("place", DbType.StringFixedLength.WithSize(250)));
             }

         }
    }
}
