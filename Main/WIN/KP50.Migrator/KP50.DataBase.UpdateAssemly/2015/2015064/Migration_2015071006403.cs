
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
    [Migration(2015071006403, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015071006403 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var formuls = new SchemaQualifiedObjectName() { Name = "formuls", Schema = CurrentSchema };

            if (NotExistRecord(formuls, " WHERE nzp_frm=1985"))
            {
                Database.Insert(formuls, new[] {"nzp_frm", "name_frm", "nzp_measure", "is_device"}, new[]
                {
                    "1985", "Пустой расчет", "8", "0"
                });
            }
            if (NotExistRecord(formuls, " WHERE nzp_frm=1986"))
            {
                Database.Insert(formuls, new[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new[]
                {
                    "1986", "Пустой расчет", "6", "0"
                });
            }
            if (NotExistRecord(formuls, " WHERE nzp_frm=1987"))
            {
                Database.Insert(formuls, new[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new[]
                {
                    "1987", "Пустой расчет", "2", "0"
                });
            }
            if (NotExistRecord(formuls, " WHERE nzp_frm=1988"))
            {
                Database.Insert(formuls, new[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new[]
                {
                    "1988", "Пустой расчет", "5", "0"
                });
            }
            if (NotExistRecord(formuls, " WHERE nzp_frm=1989"))
            {
                Database.Insert(formuls, new[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new[]
                {
                    "1989", "Пустой расчет", "4", "0"
                });
            }
            if (NotExistRecord(formuls, " WHERE nzp_frm=1990"))
            {
                Database.Insert(formuls, new[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new[]
                {
                    "1990", "Пустой расчет", "1", "0"
                });
            }
            if (NotExistRecord(formuls, " WHERE nzp_frm=1991"))
            {
                Database.Insert(formuls, new[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new[]
                {
                    "1991", "Пустой расчет", "3", "0"
                });
            }
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
       
    }
}
