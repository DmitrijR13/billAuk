using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014052005201, MigrateDataBase.CentralBank)]
    public class Migration_2014052005201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };
            if (Database.ColumnExists(fn_dogovor, "naznplat"))
            {
                Database.Update(fn_dogovor, new string[] { "naznplat" }, new string[] { "" }, "naznplat is null");
                Database.ChangeColumn(fn_dogovor, "naznplat", DbType.String.WithSize(1000), true);
            }
            else Database.AddColumn(fn_dogovor, new Column("naznplat", DbType.String.WithSize(1000)));
        }

        public override void Revert()
        {
            
        }
    }
}
