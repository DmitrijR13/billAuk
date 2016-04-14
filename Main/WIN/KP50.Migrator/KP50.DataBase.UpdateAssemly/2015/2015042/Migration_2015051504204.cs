using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    [Migration(2015051504204, MigrateDataBase.CentralBank)]
    public class Migration_2015051504204_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);

            SchemaQualifiedObjectName file_typeparams = new SchemaQualifiedObjectName() { Name = "file_typeparams", Schema = CurrentSchema };

            if (Database.TableExists(file_typeparams))
            {
                if (!Database.ColumnExists(file_typeparams, "prm_num"))
                {
                    Database.AddColumn(file_typeparams, new Column("prm_num", DbType.Int32));
                }
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);

            SchemaQualifiedObjectName file_typeparams = new SchemaQualifiedObjectName() { Name = "file_typeparams", Schema = CurrentSchema };

            if (Database.TableExists(file_typeparams))
            {
                if (Database.ColumnExists(file_typeparams, "prm_num"))
                {
                    Database.RemoveColumn(file_typeparams, "prm_num");
                }
            }
        }
    }
}
