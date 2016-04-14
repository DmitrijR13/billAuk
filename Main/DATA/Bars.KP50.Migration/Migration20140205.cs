using System.Data;
using ECM7.Migrator.Framework;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Migration
{
    [Migration(20140205)]
    public class Migration20140205 : ECM7.Migrator.Framework.Migration
    {
        public override void Apply()
        {
            foreach (var point in Points.PointList)
            {
                var prefData = point.pref + "_data" + DBManager.tableDelimiter;

                #region kart
                var kart = new SchemaQualifiedObjectName()
                {
                    Name = "kart",
                    Schema = point.pref + "_data"
                };

                Database.AddColumn(kart, new Column("dat_fio_c", DbType.Date)); //date before genotip
                Database.AddColumn(kart, new Column("rodstvo", DbType.String.WithSize(30))); //char(30) before nzp_celp

                Database.ExecuteNonQuery("update " + prefData +
                                         "kart set rodstvo = (select r.rod from s_rod r where r.nzp_rod=" + prefData  + "kart.nzp_rod)");

                Database.RemoveColumn(kart, "photo");
                Database.RemoveColumn(kart, "fam_t");
                Database.RemoveColumn(kart, "ima_t");
                Database.RemoveColumn(kart, "otch_t");
                Database.RemoveColumn(kart, "rem_mr_t");
                Database.RemoveColumn(kart, "vid_mes_t");

                Database.AddColumn(kart, new Column("dat_smert", DbType.Date)); //date before fam_c
                
                Database.AddColumn(kart, new Column("strana_mr", DbType.String.WithSize(30))); //char(30) before rem_mr
                Database.AddColumn(kart, new Column("region_mr", DbType.String.WithSize(30))); //char(30) before rem_mr
                Database.AddColumn(kart, new Column("okrug_mr", DbType.String.WithSize(30))); //char(30) before rem_mr
                Database.AddColumn(kart, new Column("gorod_mr", DbType.String.WithSize(30))); //char(30) before rem_mr
                Database.AddColumn(kart, new Column("npunkt_mr", DbType.String.WithSize(30))); //char(30) before rem_mr

                Database.AddColumn(kart, new Column("strana_op", DbType.String.WithSize(30))); //char(30) before rem_op
                Database.AddColumn(kart, new Column("region_op", DbType.String.WithSize(30))); //char(30) before rem_op
                Database.AddColumn(kart, new Column("okrug_op", DbType.String.WithSize(30))); //char(30) before rem_op
                Database.AddColumn(kart, new Column("gorod_op", DbType.String.WithSize(30))); //char(30) before rem_op
                Database.AddColumn(kart, new Column("npunkt_op", DbType.String.WithSize(30))); //char(30) before rem_mr

                Database.AddColumn(kart, new Column("strana_ku", DbType.String.WithSize(30))); //char(30) before rem_ku
                Database.AddColumn(kart, new Column("region_ku", DbType.String.WithSize(30))); //char(30) before rem_ku
                Database.AddColumn(kart, new Column("okrug_ku", DbType.String.WithSize(30))); //char(30) before rem_ku
                Database.AddColumn(kart, new Column("gorod_ku", DbType.String.WithSize(30))); //char(30) before rem_ku
                Database.AddColumn(kart, new Column("npunkt_ku", DbType.String.WithSize(30))); //char(30) before rem_mr
                #endregion

                #region s_strana
                var strana = new SchemaQualifiedObjectName()
                {
                    Schema = point.pref,
                    Name = "s_strana"
                };
                Database.AddTable(strana, new Column[]
                {
                    new Column("nzp_strana", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("strana", DbType.String.WithSize(30))
                });

                Database.AddIndex("ix_strn1", true, strana, new string[] {"nzp_strana"});
                Database.ExecuteNonQuery("insert into " + prefData +
                                         "s_strana select nzp_land,land from s_land where nzp_land=1");
                #endregion

                #region s_region
                var region = new SchemaQualifiedObjectName()
                {
                    Schema = point.pref,
                    Name = "s_region"
                };
                Database.AddTable(region, new Column[]
                {
                    new Column("nzp_region", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("region", DbType.String.WithSize(30))
                });

                Database.AddIndex("ix_rgn1", true, region, new string[] { "nzp_region" });
                Database.ExecuteNonQuery("insert into " + prefData +
                                         "s_region select nzp_stat,stat from s_stat where nzp_land=1");

                #endregion

                #region s_okrug
                var okrug = new SchemaQualifiedObjectName()
                {
                    Schema = point.pref,
                    Name = "s_okrug"
                };
                Database.AddTable(okrug, new Column[]
                {
                    new Column("nzp_okrug", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("okrug", DbType.String.WithSize(30))
                });

                Database.AddIndex("ix_okrg1", true, okrug, new string[] { "nzp_okrug" });
                //--insert into s_okrug select nzp_town,town from s_town where nzp_stat=104259 and town matches "* Р-Н";
                //--insert into s_okrug select  nzp_raj_dom, rajon_dom from  s_rajon_dom r;
                #endregion

                #region s_gorod
                var gorod = new SchemaQualifiedObjectName()
                {
                    Schema = point.pref,
                    Name = "s_gorod"
                };
                Database.AddTable(gorod, new Column[]
                {
                    new Column("nzp_gorod", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("gorod", DbType.String.WithSize(30))
                });

                Database.AddIndex("ix_grd1", true, gorod, new string[] { "nzp_gorod" });
                //--insert into s_gorod select nzp_town,town from s_town where nzp_stat=104259 and town matches "* Г";
                #endregion

                #region s_npunkt
                var npunkt = new SchemaQualifiedObjectName()
                {
                    Schema = point.pref,
                    Name = "s_npunkt"
                };
                Database.AddTable(npunkt, new Column[]
                {
                    new Column("nzp_npunkt", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("npunkt", DbType.String.WithSize(30))
                });

                Database.AddIndex("ix_npnkt1", true, npunkt, new string[] { "nzp_npunkt" });
                //--insert into s_npunkt select nzp_raj,rajon from s_rajon r where r.nzp_town in 
                //--(select t.nzp_town from s_town t
                //--where t.nzp_stat=104259);
                #endregion
            }
        }

        public override void Revert()
        {
            foreach (var point in Points.PointList)
            {
                var strana = new SchemaQualifiedObjectName()
                {
                    Schema = point.pref,
                    Name = "s_strana"
                };
                if (Database.TableExists(strana))
                    Database.ExecuteNonQuery("delete from " + point.pref + DBManager.tableDelimiter + "s_strana");

                var region = new SchemaQualifiedObjectName()
                {
                    Schema = point.pref,
                    Name = "s_region"
                };
                if (Database.TableExists(region))
                    Database.ExecuteNonQuery("delete from " + point.pref + DBManager.tableDelimiter + "s_region");
            }
        }
    }
}