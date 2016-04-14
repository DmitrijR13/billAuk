using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014102110401, MigrateDataBase.CentralBank, Ignore = true)]
    public class Migration_2014102110401_CentralBank : Migration
    {
        public override void Apply() 
        {
            SetSchema(Bank.Data);
            var tula_file_reestr = new SchemaQualifiedObjectName { Name = "tula_file_reestr", Schema = CurrentSchema };
            if (Database.ColumnExists(tula_file_reestr, "transaction_id"))
            {
                Database.ChangeColumn(tula_file_reestr, "transaction_id", DbType.String.WithSize(30), false);
            }

            if (!Database.ColumnExists(tula_file_reestr, "service_field"))
            {
                Database.AddColumn(tula_file_reestr, new Column("service_field", DbType.String.WithSize(30)));
            }

            if (!Database.ColumnExists(tula_file_reestr, "payment_datetime"))
            {
                Database.AddColumn(tula_file_reestr, new Column("payment_datetime", DbType.DateTime));
            }

            if (!Database.ColumnExists(tula_file_reestr, "address"))
            {
                Database.AddColumn(tula_file_reestr, new Column("address", DbType.String.WithSize(100)));
            }
        }

        public override void Revert() 
        {
            
        }
    }
}
