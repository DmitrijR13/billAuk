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
    [Migration(2014090208501, MigrateDataBase.CentralBank)]
    public class Migration_2014090208501_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_perekidki = new SchemaQualifiedObjectName() { Name = "file_perekidki", Schema = CurrentSchema };

            if (Database.TableExists(file_perekidki))
            {
                if (!Database.ColumnExists(file_perekidki, "nzp_kvar"))
                {
                    Database.AddColumn(file_perekidki, new Column("nzp_kvar", DbType.Int32));
                }
                
                if (!Database.ColumnExists(file_perekidki, "nzp_serv"))
                {
                    Database.AddColumn(file_perekidki, new Column("nzp_serv", DbType.Int32));
                }

                if (!Database.ColumnExists(file_perekidki, "nzp_supp"))
                {
                    Database.AddColumn(file_perekidki, new Column("nzp_supp", DbType.Int32));
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
