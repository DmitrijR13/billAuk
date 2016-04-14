using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System.Data;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014110711101, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2014110711101_CentralBank : Migration
    {
         public override void Apply()
         {
             SetSchema(Bank.Data);
             var s_departure_types = new SchemaQualifiedObjectName() { Name = "s_departure_types", Schema = CurrentSchema };

             if (!Database.TableExists(s_departure_types))
             {
                 Database.AddTable(s_departure_types,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("type_name", DbType.StringFixedLength.WithSize(50)));

                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "на даче" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "в армии" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "на даче" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "на учебе" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "в тюрьме" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "дети в гос. учреждениях" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "в больнице" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "работа в др. регионах" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "за границей" });
                 Database.Insert(s_departure_types, new string[] { "type_name" }, new string[] { "временное отсутствие" });
             }

         }
    }

    [Migration(2014110711101, Migrator.Framework.DataBase.LocalBank)]
    public class Migration_2014110711101_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var gil_periods = new SchemaQualifiedObjectName() { Name = "gil_periods", Schema = CurrentSchema };

            if (Database.TableExists(gil_periods))
            {
                if (!Database.ColumnExists(gil_periods, "id_departure_types"))
                    Database.AddColumn(gil_periods, new Column("id_departure_types", DbType.Int32));
            }

        }
    }
}
