using System.Data;
using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014091
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014091009203, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014091009203 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            Database.SetSchema(CentralKernel);

            object obj =  Database.ExecuteScalar("select count(*) from s_point where flag = 1 and substring(cast(bank_number as varchar (250)), 1,2) = '71' ");

            if (Convert.ToInt32(obj) == 0)
            {
                Database.SetSchema(CurrentSchema);
                Database.ExecuteNonQuery("update prm_name set old_field = '0' where nzp_prm = 974");
            }
        }

        public override void Revert()
        {

        }
    }
}
