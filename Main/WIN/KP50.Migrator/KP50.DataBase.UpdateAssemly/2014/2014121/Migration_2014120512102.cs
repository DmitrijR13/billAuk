using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014113
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014120512102, MigrateDataBase.CentralBank)]
    public class Migration_2014120512102_CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Kernel);

            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CurrentSchema };
            if (Database.TableExists(s_typercl))
            {
                try
                {
                    Database.ExecuteNonQuery(Database.FormatSql("UPDATE {0:NAME} SET is_actual=1 WHERE type_rcl=163", s_typercl));
                }
                catch (Exception)
                {
                //                   
                }
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

        }
    }

}
