using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305018, MigrateDataBase.CentralBank)]
    public class Migration_20140305018_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName sys_dictionary_values = new SchemaQualifiedObjectName() { Name = "sys_dictionary_values", Schema = CurrentSchema };
            SchemaQualifiedObjectName object_types = new SchemaQualifiedObjectName() { Name = "object_types", Schema = CurrentSchema };
            Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "1" }, "nzp_dict IN (8216, 8216, 6481, 6602, 6603, 6604, 6600, 6601)");
            Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "2" }, "nzp_dict IN (6489, 6490, 6491)");
            Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "3" }, "nzp_dict IN (8215, 6484, 6606, 6485, 6608, 6609)");
            Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "4" }, "nzp_dict IN (6492, 6493, 6494, 6607)");
            Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "5" }, "nzp_dict IN (6486, 6487, 6488)");
            Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "6" }, "nzp_dict IN (6611, 6612, 6498, 6499, 6500)");
            Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "7" }, "nzp_dict IN (6597, 6598, 6599)");
            Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "8" }, "nzp_dict IN (7429)");
            Database.Update(sys_dictionary_values, new string[] { "name", "code" }, new string[] { "Расчёт начислений ЛС", "1" }, "nzp_dict in (6595)");

            Database.Delete(sys_dictionary_values, "nzp_dict IN (6496, 6497, 6498, 6499, 6500, 7431, 6594)");
            Database.Insert(sys_dictionary_values, new string[] { "nzp_dict", "name", "nzp_tdict", "code" }, new string[] { "6496", "Изменение ПУ", "101", "2" });
            Database.Insert(sys_dictionary_values, new string[] { "nzp_dict", "name", "nzp_tdict", "code" }, new string[] { "6497", "Добавление ПУ", "101", "2" });
            Database.Insert(sys_dictionary_values, new string[] { "nzp_dict", "name", "nzp_tdict", "code" }, new string[] { "6498", "Изменение пачки оплат", "101", "2" });
            Database.Insert(sys_dictionary_values, new string[] { "nzp_dict", "name", "nzp_tdict", "code" }, new string[] { "6499", "Добавление пачки оплат", "101", "2" });
            Database.Insert(sys_dictionary_values, new string[] { "nzp_dict", "name", "nzp_tdict", "code" }, new string[] { "6500", "Удаление пачки оплат", "101", "2" });
            Database.Insert(sys_dictionary_values, new string[] { "nzp_dict", "name", "nzp_tdict", "code" }, new string[] { "7431", "Удаление договора с квартиросъёмщиком", "101", "8" });
            Database.Insert(sys_dictionary_values, new string[] { "nzp_dict", "name", "nzp_tdict", "code" }, new string[] { "6594", "Расчёт начислений по дому", "101", "3" });

            Database.Delete(object_types, "id BETWEEN 3 AND 8");
            Database.Insert(object_types, new string[] { "id", "type_name" }, new string[] { "3", "Дом" });
            Database.Insert(object_types, new string[] { "id", "type_name" }, new string[] { "4", "Жилец" });
            Database.Insert(object_types, new string[] { "id", "type_name" }, new string[] { "5", "Оплата" });
            Database.Insert(object_types, new string[] { "id", "type_name" }, new string[] { "6", "Пачка оплат" });
            Database.Insert(object_types, new string[] { "id", "type_name" }, new string[] { "7", "Перекидка" });
            Database.Insert(object_types, new string[] { "id", "type_name" }, new string[] { "8", "Договор" });
        }
    }

    [Migration(20140305018, MigrateDataBase.Fin)]
    public class Migration_20140305018_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName sys_events = new SchemaQualifiedObjectName() { Name = "sys_events", Schema = CurrentSchema };
            if (!Database.TableExists(sys_events))
            {
                if (Database.ProviderName == "PostgreSQL")
                    Database.ExecuteNonQuery(
                        "CREATE TABLE sys_events (" +
                        "nzp_event serial NOT NULL, " +
                        "date_ timestamp without time zone DEFAULT now(), " +
                        "nzp_user integer, " +
                        "nzp_dict_event integer, " +
                        "nzp integer, " +
                        "note character(200))");
                if (Database.ProviderName == "Informix")
                    Database.ExecuteNonQuery(
                        "CREATE TABLE sys_events (" +
                        "nzp_event serial NOT NULL, " +
                        "date_ DATETIME YEAR to FRACTION(3) default Current YEAR to FRACTION(3), " +
                        "nzp_user integer, " +
                        "nzp_dict_event integer, " +
                        "nzp integer, " +
                        "note character(200))");
            }
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName sys_events = new SchemaQualifiedObjectName() { Name = "sys_events", Schema = CurrentSchema };
            if (Database.TableExists(sys_events)) Database.RemoveTable(sys_events);
        }
    }
}
