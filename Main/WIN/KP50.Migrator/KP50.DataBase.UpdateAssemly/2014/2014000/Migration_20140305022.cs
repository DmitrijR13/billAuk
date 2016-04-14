using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305022, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_20140305022_CentralOrLocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kart = new SchemaQualifiedObjectName() { Name = "kart", Schema = CurrentSchema };
            if (Database.TableExists(kart) && !Database.ColumnExists(kart, "dat_smert"))
                Database.AddColumn(kart, new Column("dat_smert", DbType.Date));
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kart = new SchemaQualifiedObjectName() { Name = "kart", Schema = CurrentSchema };
            if (Database.ColumnExists(kart, "dat_smert")) Database.RemoveColumn(kart, "dat_smert");
        }
    }
}
