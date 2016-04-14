using System;
using System.Data;
using System.Net.NetworkInformation;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014103110402, MigrateDataBase.LocalBank)]
    public class Migration_2014103110402 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName doc_sobstw = new SchemaQualifiedObjectName() { Name = "doc_sobstw", Schema = CurrentSchema };
            if (!Database.TableExists(doc_sobstw))
            {
                Database.AddTable(doc_sobstw,
                    new Column("nzp_doc", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_sobstw", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dolya_up", DbType.Int32),
                    new Column("dolya_down", DbType.Int32),
                    new Column("nzp_dok_sv", DbType.Int32),
                    new Column("serij_sv", DbType.StringFixedLength.WithSize(10)),
                    new Column("nomer_sv", DbType.StringFixedLength.WithSize(15)),
                    new Column("vid_mes_sv", DbType.StringFixedLength.WithSize(70)),
                    new Column("vid_dat_sv", DbType.Date)
                );
            }

            var count = Database.ExecuteScalar("Select count(*) from "+ doc_sobstw.Schema + Database.TableDelimiter + doc_sobstw.Name);
            int pCount;
            if (int.TryParse(count.ToString(), out pCount))
            {
                if (pCount == 0)
                {
                    Database.ExecuteNonQuery(" insert into " + doc_sobstw.Schema + Database.TableDelimiter + doc_sobstw.Name +
                        " (nzp_sobstw, dolya_up, dolya_down, nzp_dok_sv, serij_sv, nomer_sv, vid_mes_sv, vid_dat_sv) " +
                        " select nzp_sobstw, dolya_up, dolya_down, nzp_dok_sv, serij_sv, nomer_sv, vid_mes_sv, vid_dat_sv from  " + doc_sobstw.Schema + Database.TableDelimiter + "sobstw;");
                }
            }
        }
    }
}
