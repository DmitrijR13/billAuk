using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014062006302, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014062006302 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CurrentSchema };

            Database.Delete(s_typercl, "type_rcl = 20");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename" }, new string[] { "20", "0", "Корректировка входящего сальдо" });
        }
    }
}