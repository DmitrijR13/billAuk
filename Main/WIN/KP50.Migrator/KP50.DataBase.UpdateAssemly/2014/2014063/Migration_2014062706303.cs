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
    [Migration(2014062706303, MigrateDataBase.CentralBank)]
    public class Migration_2014062706303_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel
            SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = CurrentSchema };
            if (!Database.ColumnExists(supplier, "changed_by")) Database.AddColumn(supplier, new Column("changed_by", DbType.Int32));

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
            SchemaQualifiedObjectName default_values = new SchemaQualifiedObjectName() { Name = "default_values", Schema = CurrentSchema };
            if (!Database.TableExists(default_values))
            {
                Database.AddTable(default_values,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("field_name", DbType.String.WithSize(40)),
                    new Column("field_value", DbType.Int32));
            }


            SchemaQualifiedObjectName file_nedopost = new SchemaQualifiedObjectName() { Name = "file_nedopost", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_dog = new SchemaQualifiedObjectName() { Name = "file_dog", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_typenedopost = new SchemaQualifiedObjectName() { Name = "file_typenedopost", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_oplats = new SchemaQualifiedObjectName() { Name = "file_oplats", Schema = CurrentSchema };

            if (!Database.ColumnExists(file_nedopost, "percent")) Database.AddColumn(file_nedopost, new Column("percent", DbType.Decimal.WithSize(14, 3)));
            if (!Database.ColumnExists(file_dog, "rs")) Database.AddColumn(file_dog, new Column("rs", DbType.String.WithSize(20)));
            if (!Database.ColumnExists(file_typenedopost, "nzp_kind")) Database.AddColumn(file_typenedopost, new Column("nzp_kind", DbType.Int32));
            if (!Database.ColumnExists(file_oplats, "type_oplat")) Database.AddColumn(file_oplats, new Column("type_oplat", DbType.Int32));
            if (!Database.ColumnExists(file_oplats, "kod_oplat")) Database.AddColumn(file_oplats, new Column("kod_oplat", DbType.Int32));



            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions)) Database.Delete(file_versions, "nzp_version = 5");
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "5", "5", "1.3.3" });


            SchemaQualifiedObjectName file_raspr = new SchemaQualifiedObjectName() { Name = "file_raspr", Schema = CurrentSchema };
            if (!Database.TableExists(file_raspr))
            {
                Database.AddTable(file_raspr,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("kod_oplat", DbType.Int32),
                    new Column("id_serv", DbType.Int32),
                    new Column("id_dog", DbType.Int32),
                    new Column("sum_money", DbType.Decimal.WithSize(14, 2)));
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
            SchemaQualifiedObjectName default_values = new SchemaQualifiedObjectName() { Name = "default_values", Schema = CurrentSchema };
            if (Database.TableExists(default_values)) Database.RemoveTable(default_values);

            SchemaQualifiedObjectName file_raspr = new SchemaQualifiedObjectName() { Name = "file_raspr", Schema = CurrentSchema };
            if (Database.TableExists(file_raspr)) Database.RemoveTable(file_raspr);


            SchemaQualifiedObjectName file_nedopost = new SchemaQualifiedObjectName() { Name = "file_nedopost", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_dog = new SchemaQualifiedObjectName() { Name = "file_dog", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_typenedopost = new SchemaQualifiedObjectName() { Name = "file_typenedopost", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_oplats = new SchemaQualifiedObjectName() { Name = "file_oplats", Schema = CurrentSchema };

            if (Database.ColumnExists(file_nedopost, "percent")) Database.RemoveColumn(file_nedopost, "percent");
            if (Database.ColumnExists(file_dog, "rs")) Database.RemoveColumn(file_dog, "rs");
            if (Database.ColumnExists(file_typenedopost, "nzp_kind")) Database.RemoveColumn(file_typenedopost, "nzp_kind");
            if (Database.ColumnExists(file_oplats, "type_oplat")) Database.RemoveColumn(file_oplats, "type_oplat");
            if (Database.ColumnExists(file_oplats, "kod_oplat")) Database.RemoveColumn(file_oplats, "kod_oplat");
        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014062706303, MigrateDataBase.LocalBank)]
    public class Migration_2014062706303_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade LocalPref_Kernel
            SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = CurrentSchema };
            if (!Database.ColumnExists(supplier, "changed_by")) Database.AddColumn(supplier, new Column("changed_by", DbType.Int32));

            SetSchema(Bank.Data);
            // TODO: Upgrade LocalPref_Data

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }
    }
}
