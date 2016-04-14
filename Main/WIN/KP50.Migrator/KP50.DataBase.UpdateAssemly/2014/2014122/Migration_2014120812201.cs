using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014122
{
    [Migration(2014120812201, MigrateDataBase.LocalBank | MigrateDataBase.CentralBank)]
    public class Migration_2014120812201_CentralOrLocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_rod = new SchemaQualifiedObjectName() { Name = "s_rod", Schema = CurrentSchema };
            if (Database.TableExists(s_rod))
                Database.Update(s_rod, new string[] { "rod" }, new string[] { "племянница мужа" }, " nzp_rod = 50");
        }

        public override void Revert()
        {
        }
    }
}
