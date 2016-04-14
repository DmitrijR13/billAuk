using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014122312402, MigrateDataBase.Web)]
    public class Migration_2014122312402_Web : Migration
    {
        public override void Apply()
        {
            try
            {

           
            SchemaQualifiedObjectName list_houses_for_calc = new SchemaQualifiedObjectName() { Name = "list_houses_for_calc", Schema = CurrentSchema };
            if (!Database.TableExists(list_houses_for_calc))
            {
                Database.AddTable(list_houses_for_calc,
                      new Column("nzp_wp", DbType.Int32),
                      new Column("nzp_dom", DbType.Int32),
                      new Column("nzp_user", DbType.Int32),
                      new Column("nzp_key", DbType.Int32)
                      );
            }
            if (Database.TableExists(list_houses_for_calc))
            {
                if (
                      !Database.IndexExists(
                          "ix1_" + list_houses_for_calc.Name + "_index",
                          list_houses_for_calc))
                {
                    Database.AddIndex(
                        "ix1_" + list_houses_for_calc.Name + "_index",
                        false,
                        list_houses_for_calc, "nzp_wp", "nzp_user", "nzp_key");
                }
            }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }

        public override void Revert()
        {

        }
    }


}
