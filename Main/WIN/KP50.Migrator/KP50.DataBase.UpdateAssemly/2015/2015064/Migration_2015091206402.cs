using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015091206402, MigrateDataBase.CentralBank)]
    public class Migration_2015091206402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var tulaKvitReestr = new SchemaQualifiedObjectName() { Name = "tula_kvit_reestr", Schema = CurrentSchema };
            var tulaFileReestr = new SchemaQualifiedObjectName()
            {
                Name = "tula_file_reestr",
                Schema = CurrentSchema
            };

            var tulaReestrDownloads = new SchemaQualifiedObjectName()
            {
                Name = "tula_reestr_downloads",
                Schema = CurrentSchema
            };

            if (Database.TableExists(tulaKvitReestr))
            {
                if (Database.ColumnExists(tulaKvitReestr, "file_name"))
                {
                    Database.ChangeColumn(tulaKvitReestr, "file_name", DbType.String.WithSize(200), false);
                }
                if (Database.ColumnExists(tulaFileReestr, "transaction_id"))
                {
                    Database.ChangeColumn(tulaFileReestr, "transaction_id", DbType.String.WithSize(255), false);
                }
                if (Database.ColumnExists(tulaReestrDownloads, "file_name"))
                {
                    Database.ChangeColumn(tulaReestrDownloads, "file_name", DbType.String.WithSize(200), false);
                }
            }
         
        }

    }

  
}
