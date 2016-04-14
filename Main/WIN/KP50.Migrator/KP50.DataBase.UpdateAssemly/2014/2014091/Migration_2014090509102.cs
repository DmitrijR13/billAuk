using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014091
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014090509102, MigrateDataBase.CentralBank)]
    public class Migration_2014090509102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            
            if (Database.TableExists(file_versions))
            {
                Database.Insert(file_versions, new string[] {"nzp_version", "nzp_ff", "version_name"},new string[] {"8", "8", "1.3.6"});             
            }

            
            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };

            if (Database.TableExists(file_serv))
            {
                if (Database.ColumnExists(file_serv, "sum_hour_nedop"))
                {
                    if (!Database.ColumnExists(file_serv, "num_hour_nedop"))
                    {
                        Database.RenameColumn(file_serv, "sum_hour_nedop", "num_hour_nedop");
                    }
                }
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
