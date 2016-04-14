using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003312, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015032003312_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var fn_dogovor_bank_lnk = new SchemaQualifiedObjectName {Name = "fn_dogovor_bank_lnk", Schema = CurrentSchema};
            if (!Database.ColumnExists(fn_dogovor_bank_lnk, "priznak_perechisl"))
            {
                Database.AddColumn(fn_dogovor_bank_lnk, new Column("priznak_perechisl", new ColumnType(DbType.Int32), ColumnProperty.None, 1));
            }
            if (!Database.ColumnExists(fn_dogovor_bank_lnk, "max_sum"))
            {
                Database.AddColumn(fn_dogovor_bank_lnk, new Column("max_sum", new ColumnType(DbType.Decimal, 13, 2), ColumnProperty.None, 0));
            }
            if (!Database.ColumnExists(fn_dogovor_bank_lnk, "min_sum"))
            {
                Database.AddColumn(fn_dogovor_bank_lnk, new Column("min_sum", new ColumnType(DbType.Decimal, 13, 2), ColumnProperty.None, 0));
            }
            if (!Database.ColumnExists(fn_dogovor_bank_lnk, "naznplat"))
            {
                Database.AddColumn(fn_dogovor_bank_lnk, new Column("naznplat", new ColumnType(DbType.String, 1000)));
            }
            Database.ExecuteNonQuery("UPDATE " + CentralData + Database.TableDelimiter + "fn_dogovor_bank_lnk f SET naznplat=" +
                                     "(Select max(naznplat) from " + CentralData + Database.TableDelimiter + "fn_dogovor where nzp_fd=f.nzp_fd)");
        }
    }

    [Migration(2015032003312,  Migrator.Framework.DataBase.Fin)]
    public class Migration_2015032003312_Fin : Migration
    {
        public override void Apply()
        {
            var fn_sended = new SchemaQualifiedObjectName { Name = "fn_sended", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_sended, "naznplat"))
            {
                Database.AddColumn(fn_sended, new Column("naznplat", new ColumnType(DbType.String,1000)));
            }
            Database.ExecuteNonQuery("UPDATE " + CurrentSchema + Database.TableDelimiter + "fn_sended f SET naznplat=" +
                                    "(Select max(naznplat) from " + CentralData + Database.TableDelimiter + "fn_dogovor where nzp_fd=f.nzp_fd)");
            if (!Database.ColumnExists(fn_sended, "fn_dogovor_bank_lnk_id"))
            {
                Database.AddColumn(fn_sended, new Column("fn_dogovor_bank_lnk_id", new ColumnType(DbType.Int32)));
            }
            var fn_sended_dom = new SchemaQualifiedObjectName { Name = "fn_sended_dom", Schema = CurrentSchema };
            if (Database.ColumnExists(fn_sended, "nzp_fd"))
            {
                Database.ChangeColumn(fn_sended_dom, "nzp_fd", new ColumnType(DbType.Int32),false);
            }
        }
    }
}
