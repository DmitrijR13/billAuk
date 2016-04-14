using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305017, MigrateDataBase.CentralBank)]
    public class Migration_20140305017_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName() { Name = "prm_10", Schema = CurrentSchema };
            Database.Delete(prm_10, "nzp_prm = 1281");

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };

            Database.Delete(prm_name, "nzp_prm = 1281");
            Database.Delete(resolution, "nzp_res = 3021");
            Database.Delete(res_y, "nzp_res = 3021");
            Database.Delete(res_x, "nzp_res = 3021");
            Database.Delete(res_values, "nzp_res = 3021");

            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1281", "Вид платежного кода", "sprav", "3021", "10", null, null, null });
            Database.Insert(resolution,
                new string[] { "nzp_res","name_short","name_res" },
                new string[] { "3021","ТВидПКода","Вид платежного кода" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3021", "1", "Стандарт" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3021", "2", "Самарская область" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3021", "3", "Татарстан" });
            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3021", "1", "-" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3021", "1", "1", "" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3021", "2", "1", "" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3021", "3", "1", "" });

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName files_selected = new SchemaQualifiedObjectName() { Name = "files_selected", Schema = CurrentSchema };
            if (!Database.TableExists(files_selected))
            {
                Database.AddTable(files_selected,
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_user", DbType.Int32),
                    new Column("pref", DbType.String.WithSize(20)),
                    new Column("num", DbType.Int32),
                    new Column("comment", DbType.String.WithSize(100)));
                Database.AddIndex("fi_sel_1", false, files_selected, "nzp_file", "nzp_user", "pref");
            }
            
            if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) VALUES (0, 1281, '01.01.1900', '01.01.3000', 1, 1, 1, now())");
            if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) VALUES (0, 1281, '01.01.1900', '01.01.3000', 1, 1, 1, current)");
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName files_selected = new SchemaQualifiedObjectName() { Name = "files_selected", Schema = CurrentSchema };
            if (Database.TableExists(files_selected)) Database.RemoveTable(files_selected);
        }
    }

    [Migration(20140305017, MigrateDataBase.LocalBank)]
    public class Migration_20140305017_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName() { Name = "prm_10", Schema = CurrentSchema };
            Database.Delete(prm_10, "nzp_prm = 1281");


            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };

            Database.Delete(prm_name, "nzp_prm = 1281");
            Database.Delete(resolution, "nzp_res = 3021");
            Database.Delete(res_y, "nzp_res = 3021");
            Database.Delete(res_x, "nzp_res = 3021");
            Database.Delete(res_values, "nzp_res = 3021");

            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1281", "Вид платежного кода", "sprav", "3021", "10", null, null, null });
            Database.Insert(resolution,
                new string[] { "nzp_res", "name_short", "name_res" },
                new string[] { "3021", "ТВидПКода", "Вид платежного кода" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3021", "1", "Стандарт" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3021", "2", "Самарская область" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3021", "3", "Татарстан" });
            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3021", "1", "-" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3021", "1", "1", "" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3021", "2", "1", "" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3021", "3", "1", "" });

            SetSchema(Bank.Data);
            if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) VALUES (0, 1281, '01.01.1900', '01.01.3000', 1, 1, 1, now())");
            if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) VALUES (0, 1281, '01.01.1900', '01.01.3000', 1, 1, 1, current)");
        }
    }

    [Migration(20140305017, MigrateDataBase.Web)]
    public class Migration_20140305017_Web : Migration
    {
        public override void Apply()
        {
            #warning Индусский код detected.
            for (int i = 1; i < 12; i++)
            {
                SchemaQualifiedObjectName calc_fon = new SchemaQualifiedObjectName() { Name = string.Format("calc_fon_{0}", i.ToString("00")), Schema = CurrentSchema };
                if (Database.TableExists(calc_fon) && !Database.ColumnExists(calc_fon, "parameters"))
                    Database.AddColumn(calc_fon, new Column("parameters", DbType.String.WithSize(2000)));
            }
        }

        public override void Revert()
        {
            for (int i = 1; i < 12; i++)
            {
                SchemaQualifiedObjectName calc_fon = new SchemaQualifiedObjectName() { Name = string.Format("calc_fon_{0}", i.ToString("00")), Schema = CurrentSchema };
                if (Database.TableExists(calc_fon) && Database.ColumnExists(calc_fon, "parameters"))
                    Database.RemoveColumn(calc_fon, "parameters");
            }
        }
    }
}
