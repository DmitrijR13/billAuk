using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015055
{
      [Migration(2015061705501, Migrator.Framework.DataBase.CentralBank | Migrator.Framework.DataBase.LocalBank)]
    public class Migration_2015061705501: Migration
    {
          public override void Apply()
          {
              SetSchema(Bank.Kernel);
              var supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = CurrentSchema };
              if (Database.TableExists(supplier))
                  if (!Database.ColumnExists(supplier, "allow_overpayments")) 
                      Database.AddColumn(supplier, new Column("allow_overpayments", DbType.Int16, ColumnProperty.None, 0));
          }
    }
}
