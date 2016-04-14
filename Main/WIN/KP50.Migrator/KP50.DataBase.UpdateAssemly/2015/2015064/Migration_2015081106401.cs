using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015081106401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015081106401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                object obj = Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                " WHERE nzp_prm = 2110;");
                var count = Convert.ToInt32(obj);
                if (count == 0)
                    Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "2110", "В ЕПД - ГВС", "char", "2" });

                Database.ExecuteNonQuery("update  " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                                         " set name_prm ='п'||(name_prm) WHERE nzp_prm = 3000014");


                object obj1 = Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                " WHERE nzp_prm = 2111;");
                var count1 = Convert.ToInt32(obj1);
                if (count1 == 0)
                    Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "2111", "В ЕПД - ХВС для ГВС", "char", "2" });

                object obj2 = Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                " WHERE nzp_prm = 2112;");
                var count2 = Convert.ToInt32(obj2);
                if (count2 == 0)
                    Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "2112", "В ЕПД - ОДН - ГВС", "char", "2" });

                object obj3 = Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                " WHERE nzp_prm = 2113;");
                var count3 = Convert.ToInt32(obj3);
                if (count3 == 0)
                    Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "2113", "В ЕПД - ОДН ХВС для ГВС", "char", "2" });
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }
}
