using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020902101, Migrator.Framework.DataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015020902101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName
                 {
                     Name = "prm_name"
                 };
            Database.Delete(prm_name, "nzp_prm = 1427");
            Database.Insert(prm_name, new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new[] { "1427", "Запрет учета временно-выбывших для ЛС с ИПУ", "bool", "10" });
        }

        public override void Revert()
        {
        }
    }
}
