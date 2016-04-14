using System;
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
    [Migration(2015051204201, MigrateDataBase.CentralBank)]
    public class Migration_2015051204201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SetSchema(Bank.Upload);
            
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions))
            {
                object obj = Database.ExecuteScalar("SELECT count(*) FROM " + file_versions.Schema + Database.TableDelimiter + file_versions.Name +
                " WHERE nzp_version = 12;");
                var count = Convert.ToInt32(obj);
                if(count == 0)
                Database.Insert(file_versions, new string[] { "nzp_version", "nzp_ff", "version_name" }, new string[] { "12", "12", "1.3.8.1" });
            }

            SchemaQualifiedObjectName file_odpu = new SchemaQualifiedObjectName() { Name = "file_odpu", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_odpu, "dat_close"))
            {
                Database.AddColumn(file_odpu, new Column("dat_close", DbType.Date));
            }

            SchemaQualifiedObjectName file_ipu = new SchemaQualifiedObjectName() { Name = "file_ipu", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_ipu, "dat_close"))
            {
                Database.AddColumn(file_ipu, new Column("dat_close", DbType.Date));
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
