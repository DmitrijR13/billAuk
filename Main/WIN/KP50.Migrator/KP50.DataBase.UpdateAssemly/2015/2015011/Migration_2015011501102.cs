using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015011501102, MigrateDataBase.CentralBank)]
    public class Migration_2015011501102_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Kernel);
            var prmName = new SchemaQualifiedObjectName { Schema = CurrentSchema, Name = "prm_name" };

            //изменение в списке параметров
            if (Database.TableExists(prmName))
            {
                Database.Update(prmName, new[] { "type_prm" }, new[] { "char" }, "type_prm = '''char'''");
                Database.Update(prmName, new[] { "type_prm" }, new[] { "float" }, "type_prm = '''float'''");
            }
        }
    }

    [Migration(2015011501102, MigrateDataBase.LocalBank)]
    public class Migration_2015011501102_LocalBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Kernel);
            var prmName = new SchemaQualifiedObjectName { Schema = CurrentSchema, Name = "prm_name" };

            //изменение в списке параметров
            if (Database.TableExists(prmName))
            {
                Database.Update(prmName, new[] { "type_prm" }, new[] { "char" }, "type_prm = '''char'''");
                Database.Update(prmName, new[] { "type_prm" }, new[] { "float" }, "type_prm = '''float'''");
            }
        }
    }
}
