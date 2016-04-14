using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015052704203, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015052704203_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_area = new SchemaQualifiedObjectName() { Name = "s_area", Schema = CurrentSchema };

            object obj = Database.ExecuteScalar("SELECT count(*) FROM " + s_area.Schema + Database.TableDelimiter + s_area.Name +
                " WHERE nzp_area = 1;");
            var count = Convert.ToInt32(obj);

            if (count == 0)
            {
                Database.Insert(s_area, new string[]
                {
                    "nzp_area", "area", "nzp_payer"
                }, new string[]
                {
                    "1", "Об УК нет сведений", "0"
                });
            }

            SchemaQualifiedObjectName s_geu = new SchemaQualifiedObjectName() { Name = "s_geu", Schema = CurrentSchema };

            object obj1 = Database.ExecuteScalar("SELECT count(*) FROM " + s_geu.Schema + Database.TableDelimiter + s_geu.Name +
                " WHERE nzp_geu = 1;");
            var count1 = Convert.ToInt32(obj1);

            if (count1 == 0)
            {
                Database.Insert(s_geu, new string[]
                {
                    "nzp_geu", "geu"
                }, new string[]
                {
                    "1", "О ЖЭУ нет сведений"
                });
            }

            SchemaQualifiedObjectName dom = new SchemaQualifiedObjectName() { Name = "dom", Schema = CurrentSchema };

            if (Database.TableExists(dom))
            {
                if (Database.ColumnExists(dom, "nzp_area"))
                {
                    Database.ChangeDefaultValue(dom, "nzp_area", 1);
                }
                if (Database.ColumnExists(dom, "nzp_geu"))
                {
                    Database.ChangeDefaultValue(dom, "nzp_geu", 1);
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
