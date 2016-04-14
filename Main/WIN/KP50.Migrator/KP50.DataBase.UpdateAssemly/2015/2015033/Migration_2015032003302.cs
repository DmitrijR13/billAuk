using System;
using System.Data;
using System.Net.NetworkInformation;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003302, MigrateDataBase.LocalBank | MigrateDataBase.CentralBank)]
    public class Migration_2015032003302_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName bankAccounts = new SchemaQualifiedObjectName();
            bankAccounts.Name = "bank_accounts";
            bankAccounts.Schema = CurrentSchema;
            if (!Database.TableExists(bankAccounts))
            {
                Database.AddTable(bankAccounts,
                    new Column("nzp_ba_d", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("bank_account_number", DbType.StringFixedLength.WithSize(20))
                    );
                if (Database.TableExists(bankAccounts))
                {
                    Database.AddIndex("ix_bank_accounts_1", true, bankAccounts, "nzp_ba_d");
                }
            }
        }
    }

}
