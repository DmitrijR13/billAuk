using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015011
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015011201102, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015011201102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                //Database.Delete(prm_name, "nzp_prm=46"); 

                var reader = Database.ExecuteReader(" SELECT nzp_prm " +
                                                                  " from " + CurrentSchema + ".prm_name" +
                                                                  " where nzp_prm=46");
                if (reader.Read())
                {

                }
                else
                {
                    Database.Insert(
                        prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                        new[] { "46", "ФИО квартиросъемщика", "char", "3", null, null, null });
                }
                reader.Close();
            }


            SchemaQualifiedObjectName s_reg_prm = new SchemaQualifiedObjectName() { Name = "s_reg_prm", Schema = CurrentSchema };
            if (Database.TableExists(s_reg_prm))
            {
                Database.Delete(s_reg_prm, "nzp_reg=1 and nzp_prm=46"); 
                Database.Insert(
                     s_reg_prm,
                     new[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" },
                     new[] { "1", "46", "0", "21", "1" });
            }


            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
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
