using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    [Migration(2014122012401, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2014122012401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName simpleLoad = new SchemaQualifiedObjectName() { Name = "simple_load", Schema = CurrentSchema };
            if (Database.TableExists(simpleLoad))
            {
                if (!Database.ColumnExists(simpleLoad, "nzp"))
                {
                    Database.AddColumn(simpleLoad, 
                        new Column("nzp", DbType.Int32));
                }
                if (!Database.ColumnExists(simpleLoad, "tip"))
                {
                    Database.AddColumn(simpleLoad,
                        new Column("tip", DbType.Int32));
                }
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName simpleLoad = new SchemaQualifiedObjectName() { Name = "simple_load", Schema = CurrentSchema };
            if (Database.ColumnExists(simpleLoad, "nzp"))
            {
                Database.RemoveColumn(simpleLoad, "nzp");
            }
            if (Database.ColumnExists(simpleLoad, "tip"))
            {
                Database.RemoveColumn(simpleLoad, "tip");
            }
        }
    }
}
