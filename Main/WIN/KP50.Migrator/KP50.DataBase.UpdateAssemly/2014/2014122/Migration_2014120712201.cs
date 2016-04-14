using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014122
{
    [Migration(2014120712201, MigrateDataBase.LocalBank)]
    public class Migration_2014120712201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prmName = new SchemaQualifiedObjectName
            {
                Name = "prm_name",
                Schema = CurrentSchema
            };
            if (Database.TableExists(prmName))
            {
                Database.Delete(prmName, "nzp_prm IN (1270, 1271, 1272)");
                Database.Insert(prmName,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1270", "Должность начальника абонентского отдела", "char", "7" });
                Database.Insert(prmName,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1271", "Наименование абонентского отдела", "char", "7" });
                Database.Insert(prmName,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1272", "ФИО начальника абонентского отдела", "char", "7" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            var prmName = new SchemaQualifiedObjectName
            {
                Name = "prm_name",
                Schema = CurrentSchema
            };
            if (Database.TableExists(prmName))
            {
                Database.Delete(prmName, "nzp_prm IN (1270, 1271, 1272)");
            }
        }
    }
}
