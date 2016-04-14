using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{


    //миграция, которая возможно не выполнилась ранее    
    

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(20140519051, MigrateDataBase.CentralBank)]
    public class Migration_20140519051_CentralBank : Migration
    {
        public override void Apply()
        {
            #region Kernel
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel            
            #endregion

            #region Data
            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };
            if (Database.ColumnExists(kvar,"area_code"))
            {
                Database.ChangeColumn(kvar, "area_code", DbType.Int32, false);
            }
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema};
            if (Database.ColumnExists(users, "name"))
            {
                Database.ChangeColumn(users, "name", DbType.String.WithSize(50), false);
            }
            if (Database.ColumnExists(kvar, "fio")) Database.ChangeColumn(kvar, "fio", DbType.String.WithSize(50), false);
            #endregion

            #region Upload
            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload

            SchemaQualifiedObjectName file_sql = new SchemaQualifiedObjectName() { Name = "file_sql", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_servls = new SchemaQualifiedObjectName() { Name = "file_servls", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_ipu = new SchemaQualifiedObjectName() { Name = "file_ipu", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_kvar = new SchemaQualifiedObjectName() { Name = "file_kvar", Schema = CurrentSchema };
            if (!Database.IndexExists("ifilesql", file_sql)) Database.AddIndex("ifilesql", false, file_sql, "nzp_file", "id");
            if (!Database.IndexExists("ifileservls", file_servls)) Database.AddIndex("ifileservls", false, file_servls, "nzp_file", "ls_id", "id_serv");
            if (!Database.IndexExists("ifileipu", file_ipu)) Database.AddIndex("ifileipu", false, file_ipu, "nzp_file", "kod_serv", "id", "local_id");
            if (!Database.IndexExists("fk1", file_kvar)) Database.AddIndex("fk1", false, file_kvar, "nzp_file", "nzp_kvar", "id");
            if (!Database.IndexExists("fk2", file_kvar)) Database.AddIndex("fk2", false, file_kvar, "nzp_file", "id", "nzp_kvar");
            if (!Database.IndexExists("if_kvardom1", file_kvar)) Database.AddIndex("if_kvardom1", false, file_kvar, "nzp_file", "nzp_dom", "nzp_kvar");
            if (!Database.IndexExists("i_fk_nzp", file_kvar)) Database.AddIndex("i_fk_nzp", false, file_kvar, "nzp_kvar", "nzp_file", "id");
            if (!Database.IndexExists("i_fk_ukas", file_kvar)) Database.AddIndex("i_fk_ukas", false, file_kvar, "ukas", "nzp_file", "nzp_kvar");
            #endregion
        }

        public override void Revert()
        {
            #region Kernel
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel
            #endregion

            #region Data
            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };
            if (Database.ColumnExists(kvar, "area_code"))
            {
                Database.ChangeColumn(kvar, "area_code", DbType.Int16, false);
            }
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema };
            if (Database.ColumnExists(users, "name"))
            {
                Database.ChangeColumn(users, "name", DbType.String.WithSize(10), false);
            }
            if (Database.ColumnExists(kvar, "fio")) Database.ChangeColumn(kvar, "fio", DbType.String.WithSize(20), false);
            #endregion

            #region Upload
            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload

            SchemaQualifiedObjectName file_sql = new SchemaQualifiedObjectName() { Name = "file_sql", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_servls = new SchemaQualifiedObjectName() { Name = "file_servls", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_ipu = new SchemaQualifiedObjectName() { Name = "file_ipu", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_kvar = new SchemaQualifiedObjectName() { Name = "file_kvar", Schema = CurrentSchema };
            if (Database.IndexExists("ifilesql", file_sql)) Database.RemoveIndex("ifilesql", file_sql);
            if (Database.IndexExists("ifileservls", file_servls)) Database.RemoveIndex("ifileservls", file_servls);
            if (Database.IndexExists("ifileipu", file_ipu)) Database.RemoveIndex("ifileipu", file_ipu);
            if (Database.IndexExists("fk1", file_kvar)) Database.RemoveIndex("fk1", file_kvar);
            if (Database.IndexExists("fk2", file_kvar)) Database.RemoveIndex("fk2", file_kvar);
            if (Database.IndexExists("if_kvardom1", file_kvar)) Database.RemoveIndex("if_kvardom1", file_kvar);
            if (Database.IndexExists("i_fk_nzp", file_kvar)) Database.RemoveIndex("i_fk_nzp", file_kvar);
            if (Database.IndexExists("i_fk_ukas", file_kvar)) Database.RemoveIndex("i_fk_ukas", file_kvar);
            #endregion
        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(20140519051, MigrateDataBase.LocalBank)]
    public class Migration_20140519051_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Upgrade LocalPref_Data
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema };
            if (Database.ColumnExists(users, "name"))
            {
                Database.ChangeColumn(users, "name", DbType.String.WithSize(50), false);
            }
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };
            if (Database.ColumnExists(kvar, "fio")) Database.ChangeColumn(kvar, "fio", DbType.String.WithSize(50), false);
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };
            if (Database.ColumnExists(kvar, "fio")) Database.ChangeColumn(kvar, "fio", DbType.String.WithSize(20), false);
        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(20140519051, MigrateDataBase.Charge)]
    public class Migration_20140519051_Charge : Migration
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
    [Migration(20140519051, MigrateDataBase.Fin)]
    public class Migration_20140519051_Fin : Migration
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
    [Migration(20140519051, MigrateDataBase.Web)]
    public class Migration_20140519051_Web : Migration
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
