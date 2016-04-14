using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015022002101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015022002101 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            Database.Delete(prm_name, "nzp_prm in (1059,1438)");

            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1059", "Договор на управление МКД: дата заключения", "date", null, "2", null, null, null });
            Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1438", "Договор на управление МКД: номер", "char", null, "2", null, null, null });
            Database.Update(prm_name, new[] { "is_day_uchet" }, new[] { "1" }, " nzp_prm = 133");
            //
        }
    }
}
