using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305013, MigrateDataBase.CentralBank)]
    public class Migration_20140305013_CentralBank : Migration
    {
        public override void Apply()
        
        {

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName() { Name = "prm_10", Schema = CurrentSchema };
            Database.Delete(prm_10, "nzp_prm IN (1277, 1278)");


            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            Database.Delete(prm_name, "nzp_prm IN (1277, 1278)");
            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1277", "Режим смены операционного дня", "bool", null, "10", null, null, null });
            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1278", "Время автоматической смены операционного дня", "date", null, "10", null, null, null });

            SetSchema(Bank.Data);
            if (Database.ProviderName == "PostgreSQL")
            {
                Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, val_prm, dat_s, dat_po, is_actual, nzp_user, dat_when) VALUES (0, 1277, '0', '01.01.1900', '01.01.3000', 1, null, current_date)");
                Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, val_prm, dat_s, dat_po, is_actual, nzp_user, dat_when) VALUES (0, 1278, '00:00', '01.01.1900', '01.01.3000', 1, null, current_date)");
            }
            if (Database.ProviderName == "Informix")
            {
                Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, val_prm, dat_s, dat_po, is_actual, nzp_user, dat_when) VALUES (0, 1277, '0', '01.01.1900', '01.01.3000', 1, null, today)");
                Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, val_prm, dat_s, dat_po, is_actual, nzp_user, dat_when) VALUES (0, 1278, '00:00', '01.01.1900', '01.01.3000', 1, null, today)");
            }

            SchemaQualifiedObjectName object_types = new SchemaQualifiedObjectName() { Name = "object_types", Schema = CurrentSchema };
            if (!Database.TableExists(object_types))
                Database.AddTable(object_types,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("type_name", DbType.String.WithSize(25)));
            Database.Delete(object_types);
            Database.Insert(object_types, new string[] { "id", "type_name" }, new string[] { "1", "ЛС" });
            Database.Insert(object_types, new string[] { "id", "type_name" }, new string[] { "2", "ПУ" });

            SchemaQualifiedObjectName sys_dictionary_values = new SchemaQualifiedObjectName(){Name="sys_dictionary_values", Schema=CurrentSchema};
            if (Database.TableExists(sys_dictionary_values))
            {
                Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "1" }, "nzp_dict IN (6481, 6482, 6483, 8214, 6495, 6605, 6637)");
                Database.Update(sys_dictionary_values, new string[] { "code" }, new string[] { "2" }, "nzp_dict IN (6489, 6490, 6491)");
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName object_types = new SchemaQualifiedObjectName() { Name = "object_types", Schema = CurrentSchema };
            if (Database.TableExists(object_types)) Database.RemoveTable(object_types);
        }
    }

    [Migration(20140305013, MigrateDataBase.LocalBank)]
    public class Migration_20140305013_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName sys_events = new SchemaQualifiedObjectName() { Name = "sys_events", Schema = CurrentSchema };
            if (!Database.TableExists(sys_events))
            {
                if(Database.ProviderName == "Informix")
                    Database.ExecuteNonQuery(
                        "CREATE TABLE \"are\".sys_events(" +
                        "nzp_event SERIAL NOT NULL, " +
                        "date_ DATETIME YEAR to FRACTION(3) default Current YEAR to FRACTION(3), " +
                        "nzp_user INTEGER, " +
                        "nzp_dict_event INTEGER, " +
                        "nzp INTEGER, " +
                        "note CHAR(200)) " +
                        "EXTENT SIZE 3348 NEXT SIZE 332 LOCK MODE PAGE"
                        );

                if (Database.ProviderName == "PostgreSQL")
                    Database.ExecuteNonQuery(
                        "CREATE TABLE sys_events( " +
                        "nzp_event SERIAL NOT NULL, " +
                        "date_ TIMESTAMP default NOW(), " +
                        "nzp_user INTEGER, " +
                        "nzp_dict_event INTEGER, " +
                        "nzp INTEGER, " +
                        "note CHAR(200))");
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName sys_events = new SchemaQualifiedObjectName() { Name = "sys_events", Schema = CurrentSchema };
            if (Database.TableExists(sys_events)) Database.RemoveTable(sys_events);
        }
    }

    [Migration(20140305013, MigrateDataBase.Web)]
    public class Migration_20140305013_Web : Migration
    {
        public override void Apply()
        {
            for (int i = 0; i < 4; i++)
            {
                SchemaQualifiedObjectName calc_fon = new SchemaQualifiedObjectName() { Name = string.Format("calc_fon_{0}", i.ToString("0")), Schema = CurrentSchema };
                if (Database.TableExists(calc_fon) && !Database.ColumnExists(calc_fon, "dat_when")) Database.AddColumn(calc_fon, new Column("dat_when", DbType.DateTime));
            }
        }

        public override void Revert()
        {
            for (int i = 0; i < 4; i++)
            {
                SchemaQualifiedObjectName calc_fon = new SchemaQualifiedObjectName() { Name = string.Format("calc_fon_{0}", i.ToString("0")), Schema = CurrentSchema };
                if (Database.TableExists(calc_fon) && !Database.ColumnExists(calc_fon, "dat_when")) Database.RemoveColumn(calc_fon, "dat_when");
            }
        }
    }
}
