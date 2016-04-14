using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014085
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014082708503, MigrateDataBase.CentralBank)]
    public class Migration_2014082708503_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
            SchemaQualifiedObjectName file_gilec = new SchemaQualifiedObjectName() { Name = "file_gilec", Schema = CurrentSchema };

            if (Database.TableExists(file_gilec))
            {
                Database.ChangeColumn(file_gilec, "num_ls", DbType.String.WithSize(20), true);
            }

            SchemaQualifiedObjectName file_servls = new SchemaQualifiedObjectName() { Name = "file_servls", Schema = CurrentSchema };

            if (Database.TableExists(file_servls))
            {
                Database.ChangeColumn(file_servls, "ls_id", DbType.String.WithSize(20), true);
            }

            SchemaQualifiedObjectName file_perekidki = new SchemaQualifiedObjectName() { Name = "file_perekidki", Schema = CurrentSchema };

            if (Database.TableExists(file_perekidki))
            {
                Database.ChangeColumn(file_perekidki, "id_ls", DbType.String.WithSize(20), true);
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload

            SchemaQualifiedObjectName file_gilec = new SchemaQualifiedObjectName() { Name = "file_gilec", Schema = CurrentSchema };

            if (Database.TableExists(file_gilec))
            {
                Database.ChangeColumn(file_gilec, "num_ls", DbType.Int32, true);
            }

            SchemaQualifiedObjectName file_servls = new SchemaQualifiedObjectName() { Name = "file_servls", Schema = CurrentSchema };

            if (Database.TableExists(file_servls))
            {
                Database.ChangeColumn(file_servls, "ls_id", DbType.Int32, true);
            }

            SchemaQualifiedObjectName file_perekidki = new SchemaQualifiedObjectName() { Name = "file_perekidki", Schema = CurrentSchema };

            if (Database.TableExists(file_perekidki))
            {
                Database.ChangeColumn(file_perekidki, "id_ls", DbType.Int32, true);
            }
        }
    }

    
}
