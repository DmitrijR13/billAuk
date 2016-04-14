using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015062906402, MigrateDataBase.LocalBank)]
    public class Migration_2015062906402_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName counters_spis = new SchemaQualifiedObjectName() { Name = "counters_spis", Schema = CurrentSchema };

            if (Database.ProviderName == "PostgreSQL")
            {
                if (Database.SequenceExists(CentralData, "counters_spis_nzp_counter_seq"))
                {
                    int lastVal = 0;
                    object obj = Database.ExecuteScalar(" SELECT last_value FROM " + CentralData + ".counters_spis_nzp_counter_seq ");
                    Int32.TryParse(obj.ToString(), out lastVal);

                    if (Database.TableExists(counters_spis))
                    {
                        int lastValLocal = 0;

                        if (Database.TableExists(counters_spis))
                        {
                            obj =
                                Database.ExecuteScalar(" SELECT max(nzp_counter) FROM " + counters_spis.Schema + "." +
                                                       counters_spis.Name);
                            if (Int32.TryParse(obj.ToString(), out lastValLocal))
                            {
                                if (lastValLocal > lastVal)
                                {
                                    Database.ExecuteNonQuery(" ALTER SEQUENCE " + CentralData +
                                                             ".counters_spis_nzp_counter_seq RESTART " + lastValLocal);
                                }
                            }
                            Database.ExecuteNonQuery(" ALTER TABLE " + counters_spis.Schema + "." + counters_spis.Name +
                                                     " ALTER COLUMN nzp_counter DROP DEFAULT;");
                            Database.ExecuteNonQuery(" ALTER TABLE " + counters_spis.Schema + "." + counters_spis.Name +
                                                     " ALTER COLUMN nzp_counter SET DEFAULT NEXTVAL('" + CentralData +
                                                     ".counters_spis_nzp_counter_seq')");
                        }
                    }
                }
            }
        }

        public override void Revert()
        {
        }
    }
}
