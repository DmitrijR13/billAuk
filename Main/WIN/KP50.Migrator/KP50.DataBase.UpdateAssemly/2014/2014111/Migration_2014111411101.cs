using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014111411101, MigrateDataBase.CentralBank)]
    public class Migration_2014111411101 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName peni_prov_arch = new SchemaQualifiedObjectName();
            peni_prov_arch.Schema = CurrentSchema;
            peni_prov_arch.Name = "peni_provodki_arch";
            if (Database.TableExists(peni_prov_arch))
            {
                if (!Database.ColumnExists(peni_prov_arch, "peni_debt_id"))
                {
                    Database.AddColumn(peni_prov_arch, new Column("peni_debt_id", DbType.Int32));
                }
            }
         
        }
    }
}
