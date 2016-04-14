using System.Data;
using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015071406401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015071406401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            var upg_s_kind_nedop = new SchemaQualifiedObjectName() { Name = "upg_s_kind_nedop", Schema = CurrentSchema };

            if (NotExistRecord(upg_s_kind_nedop, " WHERE nzp_kind=2023"))
            {
                Database.Insert(upg_s_kind_nedop, 
                    new[] { "nzp_kind","kod_kind","nzp_parent","kod_parent","name","value_","is_param","is_show" },
                    new[] { "2023","1","200","0","Снять процент от начисления",null,"1","1" }
                );
            }
            var upg_s_nedop_type = new SchemaQualifiedObjectName() { Name = "upg_s_nedop_type", Schema = CurrentSchema };

            if (NotExistRecord(upg_s_nedop_type, " WHERE num_nedop=2023"))
            {
                Database.Insert(upg_s_nedop_type,
                    new[] { "nzp_nedop_type","nzp_serv","num_nedop","subtype_nedop","kolhour","step","percent","nzp_msr","is_show","is_rgu","is_param" },
                    new[] { "2023","200","2023","Снять процент от начисления","0","0","100","2","-2","1","1" }
                );
            }

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
        }

        private bool NotExistRecord(SchemaQualifiedObjectName table, string where)
        {
            return Convert.ToInt32(
                Database.ExecuteScalar("SELECT count(*) FROM " + GetFullTableName(table) + " " + where)) ==
                   0;
        }
        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
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
