using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015033
{
    [Migration(2015032303301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015032303301 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var fnBank = new SchemaQualifiedObjectName { Name = "fn_bank", Schema = CurrentSchema };
            if (Database.TableExists(fnBank))
            {
                if (!Database.ColumnExists(fnBank, "is_default"))
                {
                    Database.AddColumn(fnBank, new Column("is_default", DbType.Int32, ColumnProperty.NotNull, 0));
                }
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            var fnBank = new SchemaQualifiedObjectName { Name = "fn_bank", Schema = CurrentSchema };
            if (Database.TableExists(fnBank))
            {
                if (Database.ColumnExists(fnBank, "is_default"))
                {
                    Database.RemoveColumn(fnBank, "is_default");
                }
            }
        }
    }

}
