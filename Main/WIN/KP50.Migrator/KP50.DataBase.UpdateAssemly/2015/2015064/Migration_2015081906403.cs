using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015081906403, MigrateDataBase.CentralBank)]
    public class Migration_2015081906403_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            var tula_ex_sz_file = new SchemaQualifiedObjectName { Name = "tula_ex_sz_file", Schema = CurrentSchema };

            if (Database.TableExists(tula_ex_sz_file))
            {
                if(!Database.ColumnExists(tula_ex_sz_file, "nzp_wp"))
                    Database.AddColumn(tula_ex_sz_file, new Column("nzp_wp", DbType.Int32));
            }

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
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
