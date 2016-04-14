using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015011
{
    [Migration(2015012201101, MigrateDataBase.CentralBank)]
    public class Migration_2015012201101_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Data);
            var szWP = new SchemaQualifiedObjectName
            {
                Name = string.Format("tula_ex_sz_wp"), 
                Schema = CurrentSchema
            };
            if (!Database.TableExists(szWP))
            {
                Database.AddTable(szWP,
                    new Column("nzp_ex_sz", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull));
            }
        }

        public override void Revert() {
            SetSchema(Bank.Data);
            var szWP = new SchemaQualifiedObjectName
            {
                Name = string.Format("tula_ex_sz_wp"),
                Schema = CurrentSchema
            };
            if (Database.TableExists(szWP))
                Database.RemoveTable(szWP);
        }
    }
}
