using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014072107301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014072107301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (!Database.TableExists(prm_name))
            {
                Database.Delete(prm_name, "nzp_prm in (1152,1157, 2471,2472,2473, 2091,2092,2093,2094, 2484,2485)");

                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1152", "Общая площадь для группового ПУ", "float", "17", "0", "1000000", "7" });
                Database.Insert(
                    prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1157", "Количество этажей для группового ПУ", "int", "17", "0", "1000000", "7" });

                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "2471", "Площадь МОП Груп.ПУ", "float"     , "17", "0", "1000000", "7" });
                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "2472", "Площадь МОП Груп.ПУ на ОДН по ХВС", "float", "17", "0", "1000000", "7" });
                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "2473", "Площадь МОП Груп.ПУ на ОДН по ГВС", "float", "17", "0", "1000000", "7" });

                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new string[] { "2091", "ОДН КАН-Тип распределения"                , "sprav", "2" });
                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new string[] { "2092", "ОДН КАН-НЕ начислять по Пост.307 если К<1", "bool" , "2" });
                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new string[] { "2093", "ОДН КАН-Вид распределения"                , "sprav", "2" });
                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new string[] { "2094", "ОДН КАН-Показатель распределения", "sprav", "2" });

                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "2484", "Тариф (куб.м)-Вывоз ЖБО"    , "float",  "1", "0", "1000000", "7" });
                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "2485", "Тариф Пост(куб.м)-Вывоз ЖБО", "float", "11", "0", "1000000", "7" });
            }
            SchemaQualifiedObjectName s_reg_prm = new SchemaQualifiedObjectName() { Name = "s_reg_prm", Schema = CurrentSchema };
            if (!Database.TableExists(s_reg_prm))
            {
                Database.Delete(s_reg_prm, "nzp_prm in (1152,1157, 2471,2472,2473, 2091,2092,2093,2094)");

                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "4", "1152",  "8", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "4", "1157",  "9", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "4", "2471", "10", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "4", "2472", "11", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "4", "2473", "12", "1" });

                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "3", "2091", "7", "30", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "3", "2092", "7", "31", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "3", "2093", "7", "32", "1" });
                Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "3", "2094", "7", "33", "1" });
            }
            SchemaQualifiedObjectName formuls = new SchemaQualifiedObjectName() { Name = "formuls", Schema = CurrentSchema };
            if (!Database.TableExists(formuls))
            {
                Database.Delete(formuls, "nzp_frm in (1071)");
                Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, 
                    new string[] { "1071", "Вывоз ЖБО-ОДПУ для ЖБО (куб.м)", "3", "0" });
            }
            SchemaQualifiedObjectName formuls_opis = new SchemaQualifiedObjectName() { Name = "formuls_opis", Schema = CurrentSchema };
            if (!Database.TableExists(formuls_opis))
            {
                Database.Delete(formuls_opis, "nzp_frm in (1071)");
                Database.Insert(formuls_opis, 
                    new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", 
                        "nzp_prm_tarif_su", "nzp_prm_tarif_bd", "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1", "nzp_prm_rash2" },
                    new string[] { "1071"," 1071"," 1"," 2484"," 0"," 0","2485"," 0","5","0","0","0" });
            }
            SchemaQualifiedObjectName prm_tarifs = new SchemaQualifiedObjectName() { Name = "prm_tarifs", Schema = CurrentSchema };
            if (!Database.TableExists(prm_tarifs))
            {
                Database.Delete(prm_tarifs, "nzp_frm in (1071)");

                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "324", "1071", "2484", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "324", "1071", "2485", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "324", "1071",    "7", "1", "-1000" });
            }
            SchemaQualifiedObjectName prm_frm = new SchemaQualifiedObjectName() { Name = "prm_frm", Schema = CurrentSchema };
            if (!Database.TableExists(prm_frm))
            {
                Database.Delete(prm_frm, "nzp_frm in (1071)");

                Database.Insert(prm_frm, new string[] { "nzp_frm", "frm_calc", "is_prm", "operation", "nzp_prm", "frm_p1", "frm_p2", "frm_p3", "result" },
                    new string[] { "1071", "999", "1", "  FLD", "         7", "", "", "", "" });
                Database.Insert(prm_frm, new string[] { "nzp_frm","frm_calc","is_prm","operation","nzp_prm","frm_p1","frm_p2","frm_p3","result" },
                    new string[] { "1071", "999", "1", "  FLD", "      2484", "", "", "", "" });
                Database.Insert(prm_frm, new string[] { "nzp_frm", "frm_calc", "is_prm", "operation", "nzp_prm", "frm_p1", "frm_p2", "frm_p3", "result" },
                    new string[] { "1071", "999", "1", "  FLD", "      2485", "", "", "", "" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (!Database.TableExists(prm_name))
            {
                Database.Delete(prm_name, "nzp_prm in (1152,1157, 2471,2472,2473, 2091,2092,2093,2094, 2484,2485)");
            }
            SchemaQualifiedObjectName s_reg_prm = new SchemaQualifiedObjectName() { Name = "s_reg_prm", Schema = CurrentSchema };
            if (!Database.TableExists(s_reg_prm))
            {
                Database.Delete(s_reg_prm, "nzp_prm in (1152,1157, 2471,2472,2473, 2091,2092,2093,2094)");
            }
            SchemaQualifiedObjectName formuls = new SchemaQualifiedObjectName() { Name = "formuls", Schema = CurrentSchema };
            if (!Database.TableExists(formuls))
            {
                Database.Delete(formuls, "nzp_frm in (1071)");
            }
            SchemaQualifiedObjectName formuls_opis = new SchemaQualifiedObjectName() { Name = "formuls_opis", Schema = CurrentSchema };
            if (!Database.TableExists(formuls_opis))
            {
                Database.Delete(formuls_opis, "nzp_frm in (1071)");
            }
            SchemaQualifiedObjectName prm_tarifs = new SchemaQualifiedObjectName() { Name = "prm_tarifs", Schema = CurrentSchema };
            if (!Database.TableExists(prm_tarifs))
            {
                Database.Delete(prm_tarifs, "nzp_frm in (1071)");
            }
            SchemaQualifiedObjectName prm_frm = new SchemaQualifiedObjectName() { Name = "prm_frm", Schema = CurrentSchema };
            if (!Database.TableExists(prm_frm))
            {
                Database.Delete(prm_frm, "nzp_frm in (1071)");
            }
        }
    }

}
