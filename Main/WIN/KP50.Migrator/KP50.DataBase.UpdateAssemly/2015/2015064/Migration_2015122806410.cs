using System;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806410, MigrateDataBase.CentralBank)]
    public class Migration_2015122806410 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var peni_settings = new SchemaQualifiedObjectName() { Name = "peni_settings", Schema = CurrentSchema };
            if (Database.TableExists(peni_settings) && !Database.ColumnExists(peni_settings, "is_old_calc"))
            {
                Database.AddColumn(peni_settings, new Column("is_old_calc", DbType.Boolean, ColumnProperty.NotNull, "false"));
                Database.ExecuteNonQuery("UPDATE " + GetFullTableName(peni_settings) +
                                         " SET is_old_calc=TRUE " +
                                         " WHERE nzp_serv=206");


                Database.AddIndex("ix3_" + peni_settings.Name, false, peni_settings, "is_old_calc");
            }
        }
        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }
    }
}
