using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    [Migration(2015050804201, MigrateDataBase.CentralBank)]
    public class Migration_2015050804201_CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Upload);

            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() {Name = "file_serv", Schema = CurrentSchema};

            if (Database.TableExists(file_serv))
            {
                if (!Database.ColumnExists(file_serv, "nzp_serv_real"))
                {
                    Database.AddColumn(file_serv, new Column("nzp_serv_real", DbType.Int32));
                }
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);

            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };

            if (Database.TableExists(file_serv))
            {
                if (Database.ColumnExists(file_serv, "nzp_serv_real"))
                {
                    Database.RemoveColumn(file_serv, "nzp_serv_real");
                }
            }
        }
    }
}
