using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015041
{
    [Migration(2015042104101, MigrateDataBase.Fin)]
    public class Migration_2015042104101_Fin : Migration
    {
        public override void Apply()
        {
            var fn_sended = new SchemaQualifiedObjectName() { Name = "fn_sended", Schema = CurrentSchema };

            if (!Database.ColumnExists(fn_sended, "id_bc_file"))
            {
                Database.AddColumn(fn_sended, new Column("id_bc_file", DbType.Int32));
            }
        }
    }
}
