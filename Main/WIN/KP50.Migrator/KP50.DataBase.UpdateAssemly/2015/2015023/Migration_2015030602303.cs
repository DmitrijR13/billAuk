using System;
using System.Data;
using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015030602303, MigrateDataBase.CentralBank)]
    public class Migration_2015030602303CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName == "PostgreSQL")
            {
                SetSchema(Bank.Data);
                var peni_debt = new SchemaQualifiedObjectName();
                peni_debt.Name = "peni_debt";
                peni_debt.Schema = CurrentSchema;
                if (Database.TableExists(peni_debt))
                {
                    if (Database.ColumnExists(peni_debt, "cnt_days_with_prm"))
                    {
                        Database.ExecuteNonQuery("ALTER TABLE " + CurrentSchema + Database.TableDelimiter +
                                                       peni_debt.Name +
                                                       " SET (FILLFACTOR = 70)");
                        Database.ExecuteNonQuery("REINDEX TABLE " + CurrentSchema + Database.TableDelimiter +
                                                 peni_debt.Name);
                        if (!Database.IndexExists("ix1", peni_debt))
                        {
                            Database.AddIndex("ix1", true, peni_debt, new[] { "id" });
                        }
                        Database.ExecuteNonQuery("CLUSTER " + CurrentSchema + Database.TableDelimiter +
                                                peni_debt.Name + " USING ix1");
                        var min = Convert.ToInt32(Database.ExecuteScalar("select coalesce(min(id),0) FROM " +
                            peni_debt.Schema + Database.TableDelimiter + peni_debt.Name));
                        var max = Convert.ToInt32(Database.ExecuteScalar("select coalesce(max(id),0) FROM " +
                            peni_debt.Schema + Database.TableDelimiter + peni_debt.Name));
                        
                        var cnt = 0;
                        while (min <= max)
                        {
                            Database.ExecuteNonQuery(" UPDATE " + CurrentSchema + Database.TableDelimiter + peni_debt.Name +
                                                     " SET cnt_days_with_prm=cnt_days WHERE cnt_days is not null AND id>=" + min + " AND id<" + (min + 10000));
                            min += 10000;
                            cnt += 10000;
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


