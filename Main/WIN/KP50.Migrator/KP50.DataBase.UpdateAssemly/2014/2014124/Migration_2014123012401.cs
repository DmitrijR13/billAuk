using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    [Migration(2014123012401, MigrateDataBase.CentralBank)]
    public class Migration_2014123012401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName simple_pay_reestr = new SchemaQualifiedObjectName() { Name = "simple_pay_reestr", Schema = CurrentSchema };
            if (!Database.TableExists(simple_pay_reestr))
            {
                Database.AddTable(simple_pay_reestr,
                    new Column("nzp_pay", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_load", DbType.Int32),
                    new Column("pkod", DbType.Decimal.WithSize(13, 0)),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("date_pay", DbType.Date),
                    new Column("sum", DbType.Decimal.WithSize(14, 2)));
                Database.AddPrimaryKey("simple_pay_reestr_pkey", simple_pay_reestr, "nzp_pay");
            }

            SchemaQualifiedObjectName simple_cnt_reestr = new SchemaQualifiedObjectName() { Name = "simple_cnt_reestr", Schema = CurrentSchema };
            if (!Database.TableExists(simple_cnt_reestr))
            {
                Database.AddTable(simple_cnt_reestr,
                    new Column("nzp_simple", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_load", DbType.Int32),
                    new Column("date_pay", DbType.Date),
                    new Column("paccount", DbType.Decimal.WithSize(13,0)),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("num", DbType.Int32),
                    new Column("val_cnt", DbType.Decimal.WithSize(14,2)));
                Database.AddPrimaryKey("simple_cnt_reestr_pkey", simple_cnt_reestr, "nzp_simple");
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName simple_pay_reestr = new SchemaQualifiedObjectName() { Name = "simple_pay_reestr", Schema = CurrentSchema };
            if (Database.TableExists(simple_pay_reestr))
            {
                Database.RemoveTable(simple_pay_reestr);
            }

            SchemaQualifiedObjectName simple_cnt_reestr = new SchemaQualifiedObjectName() { Name = "simple_cnt_reestr", Schema = CurrentSchema };
            if (Database.TableExists(simple_cnt_reestr))
            {
                Database.RemoveTable(simple_cnt_reestr);
            }
        }
    }
}
