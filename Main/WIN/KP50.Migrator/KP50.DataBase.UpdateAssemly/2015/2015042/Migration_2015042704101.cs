using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015042704201, MigrateDataBase.CentralBank)]
    public class Migration_2015042704201 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var prohibited_recalc = new SchemaQualifiedObjectName()
            {
                Name = "prohibited_recalc",
                Schema = CurrentSchema
            };
            if (!Database.IndexExists("ix_data_prohibited_recalc", prohibited_recalc)) Database.ExecuteNonQuery("create index ix_data_prohibited_recalc on prohibited_recalc (nzp_kvar,nzp_serv,nzp_supp) ");
        }
    }
}
