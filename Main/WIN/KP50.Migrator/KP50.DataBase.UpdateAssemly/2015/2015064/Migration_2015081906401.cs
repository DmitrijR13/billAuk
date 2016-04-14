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
    [Migration(2015081906401, MigrateDataBase.CentralBank)]
    public class Migration_2015081906401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName { Name = "prm_name", Schema = CurrentSchema };

            if (Database.TableExists(prm_name))
            {
                Database.ExecuteNonQuery(" INSERT INTO " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                                             " (nzp_prm, name_prm, type_prm, prm_num) " +
                                             " SELECT 1169, 'Квартира(электроснабжение) с электроводонагревателем', 'bool', 1 " +
                                             " WHERE NOT EXISTS (SELECT 1 FROM " + prm_name.Schema +
                                             Database.TableDelimiter + prm_name.Name + " WHERE nzp_prm = 1169);");
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
