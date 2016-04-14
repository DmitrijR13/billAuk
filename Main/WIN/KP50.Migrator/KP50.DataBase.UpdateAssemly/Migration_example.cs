using KP50.DataBase.Migrator.Framework;
using System;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(31122999999, MigrateDataBase.CentralBank, Ignore = true)]
    public class Migration_example_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            // Tables:
            SchemaQualifiedObjectName Table = new SchemaQualifiedObjectName();
            Table.Name = string.Format("{0}_test_table", CurrentPrefix);
            Table.Schema = CurrentSchema;

            if (!Database.TableExists(Table))
            {
                Database.AddTable(Table,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("date", DbType.Date, ColumnProperty.NotNull, DateTime.Now),
                    new Column("char(20)", DbType.String.WithSize(20), ColumnProperty.None, "DefaultValue"),
                    new Column("decimal", DbType.Decimal.WithSize(14, 2))
                    );

                // Columns:
                if (!Database.ColumnExists(Table, "ColumnName"))
                {
                    Database.AddColumn(Table, new Column("ColumnName", DbType.Decimal.WithSize(14, 2)));
                    Database.ChangeColumn(Table, "ColumnName", DbType.Int32, true);
                    Database.ChangeDefaultValue(Table, "ColumnName", 0);
                    Database.RenameColumn(Table, "ColumnName", "NewColumnName");
                    Database.RemoveColumn(Table, "NewColumnName");
                }

                // Index:
                if (!Database.IndexExists("IndexName", Table))
                {
                    Database.AddIndex("IndexName", true, Table, "Column1", "Column2");
                    
                    // Execute SQL command:
                    if (Database.ProviderName == "Informix")
                    {
                        Database.ExecuteNonQuery("RENAME INDEX IndexName TO NewIndexName");
                    }

                    if (Database.ProviderName == "PostgreSQL")
                    {
                        Database.ExecuteNonQuery("ALTER INDEX IndexName RENAME TO NewIndexName");
                    }
                    
                    Database.RemoveIndex("NewIndexName", Table);
                }

                // Keys:
                if (Database.ConstraintExists(Table, "PK_Key"))
                {
                    Database.AddPrimaryKey("PK_Key", Table, "Column");
                    Database.RemoveConstraint(Table, "PK_Key");
                }

                if (Database.ConstraintExists(Table, "FK_Key"))
                {
                    SchemaQualifiedObjectName SourceTable = new SchemaQualifiedObjectName() {Name="SourceTable", Schema=CurrentSchema};
                    SchemaQualifiedObjectName DestTable = new SchemaQualifiedObjectName() {Name="DestTable", Schema=CurrentSchema};
                    Database.AddForeignKey("FK_Key", SourceTable, "SourceColumn", DestTable, "DestColumn");
                    Database.RemoveConstraint(Table, "FK_Key");
                }

                // Procedures:
                if (!Database.ProcedureExists(CurrentSchema, "ProcedureName"))
                {
                    // Execute querys
                }

                // Sequences:
                if (!Database.SequenceExists(CurrentSchema, "SequenceName"))
                {
                    Database.AddSequence(CurrentSchema, "SequenceName");
                    Database.RemoveSequence(CurrentSchema, "SequenceName");
                }
            }

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
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

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(31122999999, MigrateDataBase.LocalBank, Ignore = true)]
    public class Migration_example_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade LocalPref_Kernel

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

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(31122999999, MigrateDataBase.Charge, Ignore = true)]
    public class Migration_example_Charge : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Charges

        }

        public override void Revert()
        {
            // TODO: Downgrade Charges

        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(31122999999, MigrateDataBase.Fin, Ignore = true)]
    public class Migration_example_Fin : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Fins

        }

        public override void Revert()
        {
            // TODO: Downgrade Fins

        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(31122999999, MigrateDataBase.Web, Ignore = true)]
    public class Migration_example_Web : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Web

        }

        public override void Revert()
        {
            // TODO: Downgrade Web

        }
    }
}
