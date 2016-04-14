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
    [Migration(2015071006402, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015071006402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName formuls = new SchemaQualifiedObjectName() { Name = "formuls", Schema = CurrentSchema };
            if (Database.TableExists(formuls))
            {
                object obj =
                    Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                                           formuls.Name +
                                           " WHERE nzp_frm = -999987654;");
                var count = Convert.ToInt32(obj);

                if (count == 0)
                {
                    Database.Insert(formuls, new string[] {"nzp_frm", "name_frm", "nzp_measure", "is_device"},
                        new string[]
                        {
                            "-999987654", "f0", "7", "1"
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
