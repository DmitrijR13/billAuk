using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015092906401, MigrateDataBase.LocalBank)]
    public class Migration_2015092906401_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var services = new SchemaQualifiedObjectName() { Name = "services", Schema = CurrentSchema };
            if (Database.TableExists(services))
            {
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + services.Name + " where nzp_serv = 13")) == 0)
                {
                    Database.ExecuteNonQuery(
                        " INSERT INTO " + services.Name + 
                        " (nzp_serv, service, service_small, service_name, nzp_measure, ed_izmer, type_lgot, nzp_frm, ordering)" +
                        " SELECT nzp_serv, service, service_small, service_name, nzp_measure, ed_izmer, type_lgot, nzp_frm, ordering" +
                        " FROM " + CentralKernel + Database.TableDelimiter + "services where nzp_serv = 13 ");
                }
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }
    }

}
