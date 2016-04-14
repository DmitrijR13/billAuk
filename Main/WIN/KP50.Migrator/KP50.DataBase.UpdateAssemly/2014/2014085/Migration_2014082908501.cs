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
    [Migration(2014082908501, MigrateDataBase.CentralBank)]
    public class Migration_2014082908501_CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_gilec = new SchemaQualifiedObjectName() { Name = "file_gilec", Schema = CurrentSchema };

            if (Database.TableExists(file_gilec))
            {
                if (Database.ColumnExists(file_gilec, "num_ls"))
                {
                    Database.RemoveColumn(file_gilec, "num_ls");
                }
            }

            if (Database.TableExists(file_gilec))
            {
                if (!Database.ColumnExists(file_gilec, "num_ls"))
                {
                    Database.AddColumn(file_gilec, new Column("num_ls", DbType.Int32));
                }
            }

            SchemaQualifiedObjectName file_paramsdom = new SchemaQualifiedObjectName() { Name = "file_paramsdom", Schema = CurrentSchema };

            if (Database.TableExists(file_paramsdom))
            {
                if (Database.ColumnExists(file_paramsdom, "id_dom"))
                {
                    Database.RemoveColumn(file_paramsdom, "id_dom");
                }
            }

            if (Database.TableExists(file_paramsdom))
            {
                if (!Database.ColumnExists(file_paramsdom, "id_dom"))
                {
                    Database.AddColumn(file_paramsdom, new Column("id_dom", DbType.Decimal.WithSize(18,0)));
                }
            }

            SchemaQualifiedObjectName file_info_pu = new SchemaQualifiedObjectName() { Name = "file_info_pu", Schema = CurrentSchema };

            if (Database.TableExists(file_info_pu))
            {
                if (!Database.ColumnExists(file_info_pu, "nzp_kvar"))
                {
                    Database.AddColumn(file_info_pu, new Column("nzp_kvar", DbType.Int32));
                }
                if (!Database.ColumnExists(file_info_pu, "nzp_counter"))
                {
                    Database.AddColumn(file_info_pu, new Column("nzp_counter", DbType.Int32));
                }
            }

            SchemaQualifiedObjectName file_perekidki = new SchemaQualifiedObjectName() { Name = "file_perekidki", Schema = CurrentSchema };
            if (Database.TableExists(file_perekidki))
            {
                if (!Database.ColumnExists(file_perekidki, "id"))
                {
                    Database.AddColumn(file_perekidki, new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull));
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
