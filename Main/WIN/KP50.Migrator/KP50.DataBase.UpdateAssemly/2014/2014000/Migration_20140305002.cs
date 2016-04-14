using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssemly
{
    [Migration(20140305002, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_20140305002_CentralOrLocalBank : Migration // One migration for central and local banks
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName moving_types = new SchemaQualifiedObjectName() { Name = "moving_types", Schema = CurrentSchema };
            if (!Database.TableExists(moving_types))
            {
                Database.AddTable(moving_types,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("type_name", DbType.StringFixedLength.WithSize(25)));
            }
            if (!Database.IndexExists("ix_moving_types_1", moving_types))
            {
                Database.AddIndex("ix_moving_types_1", true, moving_types, "id");
            }

            SchemaQualifiedObjectName object_types = new SchemaQualifiedObjectName() { Name = "object_types", Schema = CurrentSchema };
            if (!Database.TableExists(object_types)) Database.AddTable(object_types,
                new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                new Column("type_name", DbType.StringFixedLength.WithSize(25)));
            if (!Database.IndexExists("ix_object_types_1", object_types)) Database.AddIndex("ix_object_types_1", true, object_types, "id");

            SchemaQualifiedObjectName keys = new SchemaQualifiedObjectName() { Name = "keys", Schema = CurrentSchema };
            if (!Database.TableExists(keys)) Database.AddTable(keys,
                new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                new Column("nzp_payer", DbType.Int32, ColumnProperty.NotNull),
                new Column("key_value", DbType.StringFixedLength.WithSize(500), ColumnProperty.NotNull),
                new Column("license_number", DbType.Int32, ColumnProperty.NotNull),
                new Column("sing", DbType.StringFixedLength.WithSize(32), ColumnProperty.NotNull),
                new Column("changed_by", DbType.Int32, ColumnProperty.NotNull),
                new Column("changed_on", DbType.DateTime, ColumnProperty.NotNull));

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName moving_operations = new SchemaQualifiedObjectName() { Name = "moving_operations", Schema = CurrentSchema };
            if (!Database.TableExists(moving_operations)) Database.AddTable(moving_operations,
                new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                new Column("created_by", DbType.Int32),
                new Column("created_on", DbType.DateTime),
                new Column("operation_type_id", DbType.Int32));
            if (!Database.IndexExists("ix_moving_operations_1", moving_operations)) Database.AddIndex("ix_moving_operations_1", true, moving_operations, "id");

            SchemaQualifiedObjectName moving_objects = new SchemaQualifiedObjectName() { Name = "moving_objects", Schema = CurrentSchema };
            if (!Database.TableExists(moving_objects)) Database.AddTable(moving_objects,
                new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                new Column("operation_id", DbType.Int32),
                new Column("old_id", DbType.Int32),
                new Column("new_id", DbType.Int32),
                new Column("object_type_id", DbType.Int32));
            if (!Database.IndexExists("ix_moving_objects_1", moving_objects)) Database.AddIndex("ix_moving_objects_1", true, moving_objects, "id");
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName moving_types = new SchemaQualifiedObjectName() { Name = "moving_types", Schema = CurrentSchema };
            if (Database.IndexExists("ix_moving_types_1", moving_types)) Database.RemoveIndex("ix_moving_types_1", moving_types);
            if (Database.TableExists(moving_types)) Database.RemoveTable(moving_types);

            SchemaQualifiedObjectName object_types = new SchemaQualifiedObjectName() { Name = "object_types", Schema = CurrentSchema };
            if (Database.IndexExists("ix_object_types_1", object_types)) Database.RemoveIndex("ix_object_types_1", object_types);
            if (Database.TableExists(object_types)) Database.RemoveTable(object_types);

            SchemaQualifiedObjectName keys = new SchemaQualifiedObjectName() { Name = "keys", Schema = CurrentSchema };
            if (Database.TableExists(keys)) Database.RemoveTable(keys);

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName moving_operations = new SchemaQualifiedObjectName() { Name = "moving_operations", Schema = CurrentSchema };
            if (Database.IndexExists("ix_moving_operations_1", moving_operations)) Database.RemoveIndex("ix_moving_operations_1", moving_operations);
            if (Database.TableExists(moving_operations)) Database.RemoveTable(moving_operations);

            SchemaQualifiedObjectName moving_objects = new SchemaQualifiedObjectName() { Name = "moving_objects", Schema = CurrentSchema };
            if (Database.IndexExists("ix_moving_objects_1", moving_objects)) Database.RemoveIndex("ix_moving_objects_1", moving_objects);
            if (Database.TableExists(moving_objects)) Database.RemoveTable(moving_objects);
        }
    }

    [Migration(20140305002, MigrateDataBase.Fin)]
    public class Migration_20140305002_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName pack = new SchemaQualifiedObjectName() { Name = "pack", Schema = CurrentSchema };
            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };
            //SchemaQualifiedObjectName gil_sums = new SchemaQualifiedObjectName() { Name = "gil_sums", Schema = CurrentSchema };
            if (Database.IndexExists("ix193_1", pack))
            {
                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery(string.Format("RENAME INDEX \"{0}\"{1}ix193_1 TO ix_pack_1", CurrentSchema, Database.TableDelimiter));
                if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery(string.Format("ALTER INDEX \"{0}\"{1}ix193_1 RENAME TO ix_pack_1", CurrentSchema, Database.TableDelimiter));
            }
            if (Database.IndexExists("ix194_1", pack_ls))
            {
                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery(string.Format("RENAME INDEX \"{0}\"{1}ix194_1 TO ix_pack_ls_1", CurrentSchema, Database.TableDelimiter));
                if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery(string.Format("ALTER INDEX \"{0}\"{1}ix194_1 RENAME TO ix_pack_ls_1", CurrentSchema, Database.TableDelimiter));
            }

            if (!Database.ColumnExists(pack, "changed_by")) Database.AddColumn(pack, new Column("changed_by", DbType.Int32));
            if (!Database.ColumnExists(pack, "changed_on")) Database.AddColumn(pack, new Column("changed_on", DbType.DateTime));
            if (!Database.IndexExists("ix_pack_1", pack)) Database.AddIndex("ix_pack_1", true, pack, "nzp_pack");
            if (!Database.ConstraintExists(pack, "pk_pack")) Database.AddPrimaryKey("pk_pack", pack, "nzp_pack");
            //if (!Database.ConstraintExists(pack_ls, "fk_pack_ls_1")) Database.AddForeignKey("fk_pack_ls_1", pack_ls, "nzp_pack", pack, "nzp_pack");
            if (!Database.IndexExists("ix_pack_ls_1", pack_ls)) Database.AddIndex("ix_pack_ls_1", true, pack_ls, "nzp_pack_ls");
            if (!Database.ConstraintExists(pack_ls, "pk_pack_ls")) Database.AddPrimaryKey("pk_pack_ls", pack_ls, "nzp_pack_ls");
            //if (!Database.ConstraintExists(gil_sums, "fk_gil_sums_1")) Database.AddForeignKey("fk_gil_sums_1", gil_sums, "nzp_pack_ls", pack_ls, "nzp_pack_ls");
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName pack = new SchemaQualifiedObjectName() { Name = "pack", Schema = CurrentSchema };
            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };
            SchemaQualifiedObjectName gil_sums = new SchemaQualifiedObjectName() { Name = "gil_sums", Schema = CurrentSchema };
            if (Database.ColumnExists(pack, "changed_by")) Database.RemoveColumn(pack, "changed_by");
            if (Database.ColumnExists(pack, "changed_on")) Database.RemoveColumn(pack, "changed_on");
            if (Database.ConstraintExists(pack, "pk_pack")) Database.RemoveConstraint(pack, "pk_pack");
            if (Database.ConstraintExists(pack, "fk_pack_ls_1")) Database.RemoveConstraint(pack_ls, "fk_pack_ls_1");
            if (Database.ConstraintExists(pack_ls, "pk_pack_ls")) Database.RemoveConstraint(pack_ls, "pk_pack_ls");
            if (Database.ConstraintExists(gil_sums, "fk_gil_sums_1")) Database.RemoveConstraint(gil_sums, "fk_gil_sums_1");
        }
    }
}
