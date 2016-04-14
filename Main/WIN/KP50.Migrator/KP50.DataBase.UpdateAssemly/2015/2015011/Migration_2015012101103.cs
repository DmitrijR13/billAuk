using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015012101103, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015012101103_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            Database.Delete(prm_name, "nzp_prm = 1979");
          
           // 1979|Запускать расчет средних расходов ИПУ всегда при расчете|||bool||5||||

            Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                new[] { "1979", "Запускать расчет средних расходов ИПУ всегда при расчете", "bool", "5", null, null, null });

        }

        public override void Revert()
        {
        }
    }


}
