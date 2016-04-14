using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015102706401, MigrateDataBase.Web)]
    public class Migration_2015102706401_Web : Migration
    {
        public override void Apply()
        {
            var CalculatedPersonalAccounts = new SchemaQualifiedObjectName() { Schema = CurrentSchema, Name = "calculatedpersonalaccounts" };
            if (!Database.TableExists(CalculatedPersonalAccounts))
            {
                Database.AddTable(CalculatedPersonalAccounts,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("personalaccountid", DbType.Int32, ColumnProperty.NotNull),
                    new Column("datestart", DbType.DateTime));
            }
            if (!Database.IndexExists("ix1_" + CalculatedPersonalAccounts.Name, CalculatedPersonalAccounts))
            {
                Database.AddIndex("ix1_" + CalculatedPersonalAccounts.Name, false, CalculatedPersonalAccounts,
                    new[] { "personalaccountid", "datestart" });
            }
        }

        
    }
}
