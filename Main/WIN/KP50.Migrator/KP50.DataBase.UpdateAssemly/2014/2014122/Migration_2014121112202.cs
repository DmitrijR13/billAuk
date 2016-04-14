using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014122
{
    [Migration(2014121112202, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014121112202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_bankstr = new SchemaQualifiedObjectName()
            {
                Name = "s_bankstr",
                Schema = CurrentSchema
            };
            if (Database.TableExists(s_bankstr))
            {
                if (!Database.ColumnExists(s_bankstr, "sb18"))
                {
                    Database.AddColumn(s_bankstr, new Column("sb18", DbType.String.WithSize(100)));
                }
                if (!Database.ColumnExists(s_bankstr, "sb19"))
                {
                    Database.AddColumn(s_bankstr, new Column("sb19", DbType.String.WithSize(100)));
                }
            }
        }
    }

}
