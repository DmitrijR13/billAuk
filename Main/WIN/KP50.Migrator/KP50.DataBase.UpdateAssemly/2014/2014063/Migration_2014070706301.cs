using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014063
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014070706301, MigrateDataBase.CentralBank)]
    public class Migration_2014070706301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions)) Database.Delete(file_versions, "nzp_version = 6");
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "6", "6", "1.3.4" });

            SchemaQualifiedObjectName file_perekidki = new SchemaQualifiedObjectName() { Name = "file_perekidki", Schema = CurrentSchema };
            if (!Database.TableExists(file_perekidki))
            {
                Database.AddTable(file_perekidki,
                    new Column("id_ls", DbType.String.WithSize(20), ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_serv", DbType.Int32),
                    new Column("dog_id", DbType.Int32),
                    new Column("id_type", DbType.Int32),
                    new Column("sum_perekidki", DbType.Decimal.WithSize(14, 2)),
                    new Column("tarif", DbType.Decimal.WithSize(14, 3)),
                    new Column("volum", DbType.Decimal.WithSize(14, 6)),
                    new Column("comment", DbType.String.WithSize(100)),
                    new Column("nzp_file", DbType.Int32));                                   
               
            }

            SchemaQualifiedObjectName file_gilec = new SchemaQualifiedObjectName() { Name = "file_gilec", Schema = CurrentSchema };
            if (!Database.TableExists(file_gilec))
            {
                Database.ChangeColumn(file_gilec, "fam", DbType.String.WithSize(40), true);
                Database.ChangeColumn(file_gilec, "ima", DbType.String.WithSize(40), true);
                Database.ChangeColumn(file_gilec, "otch", DbType.String.WithSize(40), true);
            }

            SchemaQualifiedObjectName file_pack = new SchemaQualifiedObjectName() { Name = "file_pack", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_pack, "kod_type_opl"))
            {
                Database.AddColumn(file_pack, new Column("kod_type_opl", DbType.Int32));
            }
            
            if (!Database.ColumnExists(file_pack, "is_raspr"))
            {
                Database.AddColumn(file_pack, new Column("is_raspr", DbType.Int32));
            }

            SchemaQualifiedObjectName file_raspr = new SchemaQualifiedObjectName() { Name = "file_raspr", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_raspr, "nzp_file"))
            {
                Database.AddColumn(file_raspr, new Column("nzp_file", DbType.Int32));
            }         
        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions)) Database.Delete(file_versions, "nzp_version = 6");

            SchemaQualifiedObjectName file_perekidki = new SchemaQualifiedObjectName() { Name = "file_perekidki", Schema = CurrentSchema };
            if (Database.TableExists(file_perekidki)) Database.RemoveTable(file_perekidki);

            SchemaQualifiedObjectName file_gelic = new SchemaQualifiedObjectName() { Name = "file_gelic", Schema = CurrentSchema };
            if (!Database.TableExists(file_gelic))
            {
                Database.ChangeColumn(file_gelic, "fam", DbType.String.WithSize(30), true);
                Database.ChangeColumn(file_gelic, "ima", DbType.String.WithSize(30), true);
                Database.ChangeColumn(file_gelic, "otch", DbType.String.WithSize(30), true);
            }

            SchemaQualifiedObjectName file_pack = new SchemaQualifiedObjectName() { Name = "file_pack", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_pack, "kod_type_opl"))
            {
                Database.RemoveColumn(file_pack, "kod_type_opl");
            }

            if (!Database.ColumnExists(file_pack, "is_raspr"))
            {
                Database.RemoveColumn(file_pack, "is_raspr");
            }

            SchemaQualifiedObjectName file_raspr = new SchemaQualifiedObjectName() { Name = "file_raspr", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_raspr, "nzp_file"))
            {
                Database.RemoveColumn(file_raspr, "nzp_file");
            }    
        }
    }
}
