using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020502102, Migrator.Framework.DataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015020502102_LocalBank : Migration
    {
        public override void Apply()
        {

            var s_counters_types_alg = new SchemaQualifiedObjectName
               {
                   Name = "s_counters_types_alg",
                   Schema = CentralKernel
               };

            var s_counters_bounds_types = new SchemaQualifiedObjectName
                  {
                      Name = "s_counters_bounds_types",
                      Schema = CentralKernel
                  };
            SetSchema(Bank.Kernel);
            if (CurrentSchema == CentralKernel)
            {

                if (Database.TableExists(s_counters_types_alg))
                {
                    if (!Database.ConstraintExists(s_counters_types_alg, "s_counters_types_alg_pkey"))
                    {
                        Database.AddPrimaryKey("s_counters_types_alg_pkey", s_counters_types_alg, "id");
                    }

                    if (Database.TableExists(s_counters_bounds_types))
                    {
                        if (!Database.ConstraintExists(s_counters_bounds_types, "s_counters_bounds_types_pkey"))
                        {
                            Database.AddPrimaryKey("s_counters_bounds_types_pkey", s_counters_bounds_types, "id");
                        }

                        if (Database.ConstraintExists(s_counters_types_alg, "s_counters_types_alg_pkey"))
                        {
                            if (!Database.ConstraintExists(s_counters_bounds_types, "s_counters_bounds_types_fkey"))
                            {
                                Database.AddForeignKey("s_counters_bounds_types_fkey", s_counters_bounds_types, "alg_id", s_counters_types_alg, "id");
                            }
                        }
                    }
                }
            }

            SetSchema(Bank.Data);
            if (CurrentSchema != CentralData)
            {
                var counters_bounds = new SchemaQualifiedObjectName { Name = "counters_bounds", Schema = CurrentSchema };
                if (Database.TableExists(counters_bounds))
                {
                    if (!Database.ConstraintExists(counters_bounds, "counters_bounds_fkey"))
                    {
                        if (Database.ConstraintExists(s_counters_bounds_types, "s_counters_bounds_types_pkey"))
                        {
                            Database.Delete(counters_bounds,
                                " type_id NOT IN (SELECT id FROM " + CentralKernel + Database.TableDelimiter +
                                s_counters_bounds_types.Name + ")");
                            Database.AddForeignKey("counters_bounds_fkey", counters_bounds, "type_id", s_counters_bounds_types, "id");
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
