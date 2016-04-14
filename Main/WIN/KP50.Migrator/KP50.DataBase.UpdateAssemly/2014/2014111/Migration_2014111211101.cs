using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014111211101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014111211101 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            Database.SetSchema(CentralKernel);

            SchemaQualifiedObjectName s_point = new SchemaQualifiedObjectName() { Name = "s_point", Schema = CentralKernel };
            //только для Тулы
            object obj = Database.ExecuteScalar("select count(*) from " + s_point.Name+ " where flag = 1 and substring(cast(bank_number as varchar (250)), 1,2) = '71' ");

            if (Convert.ToInt32(obj) > 0)
            {
                SetSchema(Bank.Data);
                SchemaQualifiedObjectName upg_s_kind_nedop = new SchemaQualifiedObjectName() { Name = "upg_s_kind_nedop", Schema = CurrentSchema };
                if (Database.TableExists("upg_s_kind_nedop"))
                {
                    Database.Update(upg_s_kind_nedop, new string[] { "value_" }, new string[] { "1" }, "nzp_kind=9 and kod_kind=1 and nzp_parent = 9");
                }
            }
        }
    }
}
