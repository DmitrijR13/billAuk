using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014062006302, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014062006303 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            Database.Update(prm_name, new string[] {"type_prm"}, new string[] {"char"},
                " nzp_prm = 2004 ");
        }
    }
}