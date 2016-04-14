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
    [Migration(2015071106401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015071106401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = CurrentSchema };
            if (Database.TableExists(supplier))
            {
                object obj =
                    Database.ExecuteScalar("SELECT count(*) FROM " + supplier.Schema + Database.TableDelimiter +
                                           supplier.Name +
                                           " WHERE nzp_supp = -999987654;");
                var count = Convert.ToInt32(obj);

                if (count == 0)
                {
                    Database.Insert(supplier, new string[] {"nzp_supp", "name_supp", "dpd", "changed_on"}, new string[]
                    {
                        "-999987654", "s0", "0", "now()"
                    });
                }
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
