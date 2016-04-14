using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015051904201, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015051904201:Migration

    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName {Schema = CurrentSchema, Name = "s_typercl"};
            if (!Database.TableExists(s_typercl)) return;
            string sql = "select exists (select 1 from " + s_typercl.ToString() + " where type_rcl=255) ";
            bool res= (bool)Database.ExecuteScalar(sql);
            if (res) return;
            sql= "insert into "+s_typercl.ToString()+" (type_rcl, is_volum, typename, nzp_type_uchet, is_auto, is_actual, comment) " +
"values (255,0,'Изменение долга в ПС \"Должники\"',6,0,1,'Изменение долга в ПС \"Должники\"')";
            Database.ExecuteNonQuery(sql);
        }
    }
}
