using System;
using System.Data;
using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015030602304, MigrateDataBase.CentralBank)]
    public class Migration_2015030602304CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName peni_actions_type = new SchemaQualifiedObjectName();
            peni_actions_type.Schema = CurrentSchema;
            peni_actions_type.Name = "peni_actions_type";

            if (Database.TableExists(peni_actions_type))
            {
                var count =
               Convert.ToInt32(
                   Database.ExecuteScalar("SELECT count(*) FROM " + peni_actions_type.Schema + Database.TableDelimiter +
                                          peni_actions_type.Name + " WHERE id=6"));

                if (count == 0)
                {
                    Database.Insert(peni_actions_type, new string[] { "id", "type" },
                        new string[] { "6", "Переформирование проводок для списка ЛС" });
                }
            }

        }

        public override void Revert()
        {
        }
    }
}


