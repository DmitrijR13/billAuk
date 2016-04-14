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
    [Migration(2014090609103, MigrateDataBase.CentralBank)]
    public class Migration_2014090609103_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName simple_load = new SchemaQualifiedObjectName() { Name = "simple_load", Schema = CurrentSchema };
            if (!Database.TableExists(simple_load))
            {
                Database.AddTable(simple_load,
                   new Column("nzp_load", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.NotNull),
                   new Column("nzp_exc", DbType.Int32 ),
                   new Column("SourceOrg", DbType.String.WithSize(200)),
                   new Column("UserSourceOrg", DbType.String.WithSize(200)),
                   new Column("file_name", DbType.String.WithSize(200)),
                   new Column("temp_file", DbType.String.WithSize(200)),
                   new Column("percent", DbType.Decimal.WithSize(8,4)),
                   new Column("created_on", DbType.DateTime),
                   new Column("created_by", DbType.Int32)
                   );
                Database.AddIndex("ix_sll_01",false, simple_load,"nzp_exc");
            }


            SchemaQualifiedObjectName simple_counters = new SchemaQualifiedObjectName() { Name = "simple_counters", Schema = CurrentSchema };
            if (!Database.TableExists(simple_counters))
            {
                Database.AddTable(simple_counters,
                   new Column("nzp_simple", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.NotNull),
                   new Column("nzp_load", DbType.Int32),
                   new Column("uk", DbType.String.WithSize(10)),
                   new Column("date_pay", DbType.Date),
                   new Column("paccount", DbType.Decimal.WithSize(13,0)),
                   new Column("nzp_serv", DbType.Int32),
                   new Column("num", DbType.Int32),
                   new Column("val_cnt", DbType.Decimal.WithSize(12,4))
                   );
                Database.AddIndex("ix_scl_01", false, simple_counters, "nzp_load");
                Database.AddIndex("ix_scl_02", false, simple_counters, "paccount");
                Database.AddIndex("ix_scl_03", false, simple_counters, "nzp_serv");
            }
         
        }

        public override void Revert()
        {
         
        }
    }
    }
