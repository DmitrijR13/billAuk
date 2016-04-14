using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015062606402, MigrateDataBase.CentralBank)]
    public class Migration_2015062606402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName transfer_data_log = new SchemaQualifiedObjectName() { Name = "transfer_data_log", Schema = CurrentSchema };

            if (!Database.TableExists(transfer_data_log))
            {
                Database.AddTable(transfer_data_log,
                    new Column("transfer_id ", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("progress", DbType.Decimal.WithSize(14, 2), ColumnProperty.None),
                    new Column("status ", DbType.Int32),
                    new Column("created_on", DbType.DateTime), 
                    new Column("nzp_user", DbType.Int32)
                    );
            }

            SchemaQualifiedObjectName transfer_status = new SchemaQualifiedObjectName() { Name = "transfer_status", Schema = CurrentSchema };
            if (!Database.TableExists(transfer_status))
            {
                Database.AddTable(transfer_status,
                    new Column("id ", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("status ", DbType.String.WithSize(40))
                    );
            }

            object obj = Database.ExecuteScalar("SELECT count(*) FROM " + transfer_status.Schema + Database.TableDelimiter + transfer_status.Name +
                " WHERE id = 1;");
            var count = Convert.ToInt32(obj);

            if (count == 0)
            {
                Database.Insert(transfer_status, new string[] {"status", "id"}, new string[]
                {
                    "Выполняется", "1"
                });
            }

            object obj1 = Database.ExecuteScalar("SELECT count(*) FROM " + transfer_status.Schema + Database.TableDelimiter + transfer_status.Name +
                " WHERE id = 2;");
            var count1 = Convert.ToInt32(obj1);
            if (count1 == 0)
            {
                Database.Insert(transfer_status, new string[] { "status", "id" }, new string[]
                {
                    "Выполнен", "2"
                });
            } 

            object obj2 = Database.ExecuteScalar("SELECT count(*) FROM " + transfer_status.Schema + Database.TableDelimiter + transfer_status.Name +
                 " WHERE id = 3;");
            var count2 = Convert.ToInt32(obj2);
            if (count2 == 0)
            {
                Database.Insert(transfer_status, new string[] { "status", "id" }, new string[]
                {
                    "Ошибка", "3"
                });
            }

            object obj3 = Database.ExecuteScalar("SELECT count(*) FROM " + transfer_status.Schema + Database.TableDelimiter + transfer_status.Name +
                 " WHERE id = 4;");
            var count3 = Convert.ToInt32(obj3);
            if (count3 == 0)
            {
                Database.Insert(transfer_status, new string[] { "status", "id" }, new string[]
                {
                    "Ошибка, перенос не возможен, разные структуры таблиц", "4"
                });
            }
            

            SchemaQualifiedObjectName transfer_house_log = new SchemaQualifiedObjectName() { Name = "transfer_house_log", Schema = CurrentSchema };
            if (!Database.TableExists(transfer_house_log))
            {
                Database.AddTable(transfer_house_log,
                    new Column("id ", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("transfer_id", DbType.Int32),
                    new Column("nzp_dom  ", DbType.Int32),
                    new Column("is_transfer", DbType.Int32),
                    new Column("error ", DbType.String.WithSize(250))
                    );
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }
}
