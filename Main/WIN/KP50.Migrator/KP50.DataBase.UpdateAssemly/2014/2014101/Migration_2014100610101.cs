using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014100610101, MigrateDataBase.CentralBank)]
    public class Migration_2014100610101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            var transfer_data_log = new SchemaQualifiedObjectName() { Name = "transfer_data_log", Schema = CurrentSchema };
            if (!Database.TableExists(transfer_data_log)) Database.AddTable(transfer_data_log,
               new Column("transfer_id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
               new Column("progress", DbType.Decimal),
               new Column("status", DbType.Int32),
               new Column("created_on", DbType.DateTime),
               new Column("nzp_user", DbType.Int32)
               );

            var transfer_status = new SchemaQualifiedObjectName() { Name = "transfer_status", Schema = CurrentSchema };
            if (!Database.TableExists(transfer_status))
            {
                Database.AddTable(transfer_status,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("status", DbType.String)
                    );
                Database.ExecuteNonQuery("INSERT INTO transfer_status VALUES (1, 'Выполняется');");
                Database.ExecuteNonQuery("INSERT INTO transfer_status VALUES (2, 'Выполнен');");
                Database.ExecuteNonQuery("INSERT INTO transfer_status VALUES (3, 'Ошибка');");
                Database.ExecuteNonQuery("INSERT INTO transfer_status VALUES (4, 'Ошибка, перенос не возможен, разные структуры таблиц');");
            }

            var transfer_house_log = new SchemaQualifiedObjectName() { Name = "transfer_house_log", Schema = CurrentSchema };
            if (!Database.TableExists(transfer_house_log)) Database.AddTable(transfer_house_log,
             new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
             new Column("transfer_id", DbType.Int32),
             new Column("nzp_dom", DbType.Int32),
             new Column("is_transfer", DbType.Int32),
             new Column("error", DbType.String)
             );
        }
    }

}
