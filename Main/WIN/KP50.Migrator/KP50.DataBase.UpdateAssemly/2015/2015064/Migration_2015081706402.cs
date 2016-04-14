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
    [Migration(2015081706402, MigrateDataBase.CentralBank)]
    public class Migration_2015081706402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var s_serv_for_norm = new SchemaQualifiedObjectName { Name = "s_serv_for_norm", Schema = CurrentSchema };
            var services = new SchemaQualifiedObjectName { Name = "services", Schema = CurrentSchema };
            var serv_odn = new SchemaQualifiedObjectName { Name = "serv_odn", Schema = CurrentSchema };



            if (Database.TableExists(s_serv_for_norm))
            {
                Database.ExecuteNonQuery(" DELETE FROM " + s_serv_for_norm.Schema + Database.TableDelimiter + s_serv_for_norm.Name +
                                         " WHERE nzp_serv > 500 " +
                                         " AND nzp_serv < 550;");

                Database.ExecuteNonQuery(" INSERT INTO " + s_serv_for_norm.Schema + Database.TableDelimiter + s_serv_for_norm.Name +
                                         " (nzp_serv, service, ordering) " +
                                         " SELECT nzp_serv, service, ordering " +
                                         " FROM " + services.Schema + Database.TableDelimiter + services.Name + " " +
                                         " WHERE nzp_serv in (" +
                                         "  SELECT nzp_serv " +
                                         "  FROM " + serv_odn.Schema + Database.TableDelimiter + serv_odn.Name + " " +
                                         "  WHERE nzp_serv_link not in (8, 14, 210)) order by 1;");
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
