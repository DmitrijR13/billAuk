using System.Data;
using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014091209201, MigrateDataBase.CentralBank)]
    public class Migration_2014091209201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_error_types = new SchemaQualifiedObjectName()
            {
                Name = "s_error_types",
                Schema = CurrentSchema
            };

            if (Database.TableExists(s_error_types))
            {
                var reader = Database.ExecuteReader(" SELECT nzp_err " +
                                                    " FROM " + CurrentSchema + ".s_error_types" +
                                                    " WHERE nzp_err = 404 ");
                if (reader.Read())
                {
                    Database.Update(s_error_types, new string[] {"nzp_err", "name"},
                        new string[] {"404", "Ошибка определения услуги для уточнения оплаты в ЕПД"}, "nzp_err = 404");
                }
                else
                {
                    Database.Insert(s_error_types, new string[] {"nzp_err", "name"},
                        new string[] {"404", "Ошибка определения услуги для уточнения оплаты в ЕПД"});
                }
                reader.Close();
            }
        }
    }
}
