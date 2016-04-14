﻿using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015080506402, MigrateDataBase.CentralBank)]
    public class Migration_2015080506402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_norm_table = new SchemaQualifiedObjectName() { Name = "file_norm_table", Schema = CurrentSchema };
            if(Database.TableExists(file_norm_table))
            if (Database.ColumnExists(file_norm_table, "str_name"))
                Database.ChangeColumn(file_norm_table, "str_name", DbType.String.WithSize(200), true);
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
