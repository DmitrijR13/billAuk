using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015092806401, MigrateDataBase.CentralBank)]
    public class Migration_2015092806401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Supg);
            var s_answer = new SchemaQualifiedObjectName() { Name = "s_answer", Schema = CurrentSchema };

            if (Database.TableExists(s_answer))
            {
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_answer.Name + " where nzp_answer = 1")) == 0)
                {
                    Database.Delete(s_answer, " nzp_answer = 1");
                    Database.Insert(s_answer, new string[] { "nzp_answer", "name_answer" }, new string[] { "1", "Электронная почта" });
                }
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_answer.Name + " where nzp_answer = 2")) == 0)
                {
                    Database.Delete(s_answer, " nzp_answer = 2");
                    Database.Insert(s_answer, new string[] { "nzp_answer", "name_answer" }, new string[] { "2", "Телефон" });
                }
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_answer.Name + " where nzp_answer = 3")) == 0)
                {
                    Database.Delete(s_answer, " nzp_answer = 3");
                    Database.Insert(s_answer, new string[] { "nzp_answer", "name_answer" }, new string[] { "3", "Наряд-заказ" });
                }
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_answer.Name + " where nzp_answer = 4")) == 0)
                {
                    Database.Delete(s_answer, " nzp_answer = 4");
                    Database.Insert(s_answer, new string[] { "nzp_answer", "name_answer" }, new string[] { "4", "Другое" });
                }
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_answer.Name + " where nzp_answer = 5")) == 0)
                {
                    Database.Delete(s_answer, " nzp_answer = 5");
                    Database.Insert(s_answer, new string[] { "nzp_answer", "name_answer" }, new string[] { "5", "Составлен акт" });
                }
            }
        }

        public override void Revert()
        {

        }
    }

}
