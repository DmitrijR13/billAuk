using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014091
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014090509101, MigrateDataBase.CentralBank)]
    public class Migration_2014090509101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };

            if (Database.TableExists(file_serv))
            {
                if (!Database.ColumnExists(file_serv, "id_serv_epd"))
                {
                    Database.AddColumn(file_serv, new Column("id_serv_epd", DbType.Int32));
                }

                if (!Database.ColumnExists(file_serv, "sum_type_epd"))
                {
                    Database.AddColumn(file_serv, new Column("sum_type_epd", DbType.Int32));
                }

                if (!Database.ColumnExists(file_serv, "sum_recalc"))
                {
                    Database.AddColumn(file_serv, new Column("sum_recalc", DbType.Decimal));
                }

                if (!Database.ColumnExists(file_serv, "sum_perekidka"))
                {
                    Database.AddColumn(file_serv, new Column("sum_perekidka", DbType.Decimal));
                }

                if (!Database.ColumnExists(file_serv, "sum_uch_nedop"))
                {
                    Database.AddColumn(file_serv, new Column("sum_uch_nedop", DbType.Decimal));
                }

                if (!Database.ColumnExists(file_serv, "sum_hour_nedop"))
                {
                    Database.AddColumn(file_serv, new Column("sum_hour_nedop", DbType.Decimal));
                }
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            
        }
    }
}
