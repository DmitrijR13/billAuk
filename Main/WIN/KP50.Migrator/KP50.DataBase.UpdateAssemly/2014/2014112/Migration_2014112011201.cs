using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014112011201, MigrateDataBase.CentralBank)]
    public class Migration_2014112011201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_gilec = new SchemaQualifiedObjectName()
            {
                Name = "file_gilec",
                Schema = CurrentSchema
            };

            if (Database.TableExists(file_gilec))
            {
                Database.ChangeColumn(file_gilec, "num_ls", DbType.String.WithSize(20), false);
            }

            SchemaQualifiedObjectName file_pack = new SchemaQualifiedObjectName()
            {
                Name = "file_pack",
                Schema = CurrentSchema
            };

            if (Database.TableExists(file_pack))
            {
                Database.ChangeColumn(file_pack, "id", DbType.Int64, false);
            }

            if (Database.TableExists(file_gilec))
            {
                Database.ChangeColumn(file_gilec, "num_ls", DbType.String.WithSize(20), false);
            }

            SchemaQualifiedObjectName file_oplats = new SchemaQualifiedObjectName()
            {
                Name = "file_oplats",
                Schema = CurrentSchema
            };

            if (Database.TableExists(file_oplats))
            {
                Database.ChangeColumn(file_oplats, "nzp_pack", DbType.Int64, false);
            }


            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kvar_factura_msg = new SchemaQualifiedObjectName()
            {
                Name = "kvar_factura_msg",
                Schema = CurrentSchema
            };

            if (!Database.TableExists(kvar_factura_msg))
            {
                Database.AddTable(kvar_factura_msg,
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("period", DbType.Date, ColumnProperty.NotNull),
                    new Column("msg", DbType.String.WithSize(2000)),
                    new Column("dog", DbType.String)
                    );
                if (Database.TableExists(kvar_factura_msg))
                {
                    Database.AddIndex("ix_kvar_factura_msg", false, kvar_factura_msg, new[] {"nzp_kvar", "period"});
                    Database.AddIndex("ix_kvar_factura_msg_period", false, kvar_factura_msg, new[] {"period"});
                }
            }
        }
    }
}
