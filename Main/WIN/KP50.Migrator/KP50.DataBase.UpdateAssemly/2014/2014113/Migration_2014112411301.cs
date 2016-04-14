using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014112411301, MigrateDataBase.LocalBank)]
    public class Migration_2014112411301_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName counters_spis = new SchemaQualifiedObjectName()
            {
                Name = "counters_spis",
                Schema = CurrentSchema
            };

            if (Database.TableExists(counters_spis))
            {
                Database.ExecuteNonQuery(
                    Database.FormatSql(" UPDATE {0:NAME} SET nzp_cnt = 5 WHERE nzp_serv = 8 and nzp_cnt = 16 ", counters_spis)
                    );
                Database.ExecuteNonQuery(
                    Database.FormatSql(" UPDATE {0:NAME} SET nzp_cnt = 3 WHERE nzp_serv = 9 and nzp_cnt = 15 ", counters_spis)
                    );
            }
        }

        public override void Revert() {
            // toDoList
        }
    }
}
