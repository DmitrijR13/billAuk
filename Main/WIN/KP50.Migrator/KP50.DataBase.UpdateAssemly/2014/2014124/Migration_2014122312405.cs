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
    [Migration(2014122312405, MigrateDataBase.CentralBank)]
    public class Migration_2014122312405_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions))
            {
                Database.Delete(file_versions, "nzp_version = 9");
                Database.Insert(file_versions, new string[] { "nzp_version", "nzp_ff", "version_name" }, new string[] { "9", "9", "1.3.8" });
            }

            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_serv, "sum_to_payment"))
            {
                Database.AddColumn(file_serv, new Column("sum_to_payment", DbType.Decimal.WithSize(14, 2)));
            }

            SchemaQualifiedObjectName file_paramsdom = new SchemaQualifiedObjectName() { Name = "file_paramsdom", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_paramsdom, "dats_val"))
            {
                Database.AddColumn(file_paramsdom, new Column("dats_val", DbType.Date));
            }
            if (!Database.ColumnExists(file_paramsdom, "datpo_val"))
            {
                Database.AddColumn(file_paramsdom, new Column("datpo_val", DbType.Date));
            }

            SchemaQualifiedObjectName file_paramsls = new SchemaQualifiedObjectName() { Name = "file_paramsls", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_paramsls, "dats_val"))
            {
                Database.AddColumn(file_paramsls, new Column("dats_val", DbType.Date));
            }
            if (!Database.ColumnExists(file_paramsls, "datpo_val"))
            {
                Database.AddColumn(file_paramsls, new Column("datpo_val", DbType.Date));
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions))
            {
                Database.Delete(file_versions, "nzp_version = 9");
            }

            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };
            if (Database.ColumnExists(file_serv, "sum_to_payment"))
            {
                Database.RemoveColumn(file_serv, "sum_to_payment");
            }

            SchemaQualifiedObjectName file_paramsdom = new SchemaQualifiedObjectName() { Name = "file_paramsdom", Schema = CurrentSchema };
            if (Database.ColumnExists(file_paramsdom, "dats_val"))
            {
                Database.RemoveColumn(file_paramsdom, "dats_val");
            }
            if (Database.ColumnExists(file_paramsdom, "datpo_val"))
            {
                Database.RemoveColumn(file_paramsdom, "datpo_val");
            }

            SchemaQualifiedObjectName file_paramsls = new SchemaQualifiedObjectName() { Name = "file_paramsls", Schema = CurrentSchema };
            if (Database.ColumnExists(file_paramsls, "dats_val"))
            {
                Database.RemoveColumn(file_paramsls, "dats_val");
            }
            if (Database.ColumnExists(file_paramsls, "datpo_val"))
            {
                Database.RemoveColumn(file_paramsls, "datpo_val");
            }
        }
    }

}
