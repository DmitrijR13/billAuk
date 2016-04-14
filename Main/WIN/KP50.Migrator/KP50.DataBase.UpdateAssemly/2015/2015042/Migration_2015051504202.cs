using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015051504202, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015051504202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            if (Database.TableExists(res_y))
            {
                Database.Delete(res_y, "nzp_res in (3002,3006,3007,3008,3022) and nzp_y in (6,5,4)");
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3002", "6", "Формула распределения остатка ОДН на ЛС без ИПУ" });
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3006", "5", "Формула распределения остатка ОДН на ЛС без ИПУ" });
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3007", "5", "Формула распределения остатка ОДН на ЛС без ИПУ" });
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3008", "5", "Формула распределения остатка ОДН на ЛС без ИПУ" });
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3022", "4", "Формула распределения остатка ОДН на ЛС без ИПУ" });
            }
            var s_type_alg = new SchemaQualifiedObjectName() { Name = "s_type_alg", Schema = CurrentSchema };
            if (Database.TableExists(s_type_alg))
            {
                Database.Delete(s_type_alg, "nzp_type_alg in (201,202,203,204)");
                Database.Insert(s_type_alg, new string[] { "nzp_type_alg", "name_type", "name_small", "name_short" },
                    new string[] { "201", "Расчет ОДН-остаток ОДПУ распределяется на ЛС без ИПУ пропорционально количеству жильцов", "ОДН-остаток ОДПУ на ЛС без ИПУ проп. кол. жильцов", "ОДПУ-ЛСбИПУкж" });
                Database.Insert(s_type_alg, new string[] { "nzp_type_alg", "name_type", "name_small", "name_short" },
                   new string[] { "202", "Расчет ОДН-остаток ОДПУ распределяется на ЛС без ИПУ пропорционально общей площади", "ОДН-остаток ОДПУ на ЛС без ИПУ проп. общей площади", "ОДПУ-ЛСбИПУоп" });
                Database.Insert(s_type_alg, new string[] { "nzp_type_alg", "name_type", "name_small", "name_short" },
                   new string[] { "203", "Расчет ОДН-остаток ОДПУ распределяется на ЛС без ИПУ пропорционально отапливаемой площади", "ОДН-остаток ОДПУ на ЛС без ИПУ проп. отап. площади", "ОДПУ-ЛСбИПУот" });
                Database.Insert(s_type_alg, new string[] { "nzp_type_alg", "name_type", "name_small", "name_short" },
                   new string[] { "204", "Расчет ОДН-остаток ОДПУ распределяется на ЛС без ИПУ пропорционально количеству лицевых счетов", "ОДН-остаток ОДПУ на ЛС без ИПУ проп. кол. ЛС", "ОДПУ-ЛСбИПУлс" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }
}
