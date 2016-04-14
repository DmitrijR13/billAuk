using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014061806301, MigrateDataBase.CentralBank)]
    public class Migration_2014061806301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_types = new SchemaQualifiedObjectName() { Name = "prm_types", Schema = CurrentSchema };
            if (!Database.TableExists(prm_types))
            {
                Database.AddTable(prm_types,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("type_name", DbType.String.WithSize(50)));
                Database.AddIndex("ix_prm_types_1", true, prm_types, new string[] { "id" });
                Database.AddPrimaryKey("pk_prm_types", prm_types, new string[] { "id" });

                Database.Insert(prm_types, new string[] { "id", "type_name" }, new string[] { "1", "Тариф" });
                Database.Insert(prm_types, new string[] { "id", "type_name" }, new string[] { "2", "Норматив" });
                Database.Insert(prm_types, new string[] { "id", "type_name" }, new string[] { "3", "Характеристика" });
            }
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName prm_types = new SchemaQualifiedObjectName() { Name = "prm_types", Schema = CurrentSchema };
            if (Database.TableExists(prm_types))
            {
                Database.RemoveTable(prm_types);
            }
        }
    }

    [Migration(2014061806302, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014061806302_CentralAndLocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_types = new SchemaQualifiedObjectName() { Name = "prm_types", Schema = CentralKernel };
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            if (!Database.ColumnExists(prm_name, "prm_type_id"))
            {
                Database.AddColumn(prm_name, new Column("prm_type_id", DbType.Int32));
                Database.AddIndex("ix_prm_name_prm_type_id", false, prm_name, new string[] { "prm_type_id" });

                if (Database.ProviderName == "PostgreSQL")
                {
                    Database.AddForeignKey("fk_prm_name_prm_type_id", prm_name, "prm_type_id", prm_types, "id");
                }

                Database.AddColumn(prm_name, new Column("is_day_uchet", DbType.Int32, ColumnProperty.None, 0));
            }
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.ColumnExists(prm_name, "prm_type_id"))
            {
                Database.RemoveColumn(prm_name, "prm_type_id");
            }

            if (Database.ColumnExists(prm_name, "is_day_uchet"))
            {
                Database.RemoveColumn(prm_name, "is_day_uchet");
            }
        }
    }
}