using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014081808405, Migrator.Framework.DataBase.Charge)]
    public class Migration_2014081808405_Charge : Migration
    {
        public override void Apply()
        {
            IDataReader reader;
            // ...
            if (Database.ProviderName == "PostgreSQL")
            {
                reader = Database.ExecuteReader("select tablename from pg_tables where schemaname='" + CurrentSchema +
                                                "' and tablename like 'calc_gku%' order by 1");

                try
                {
                    while (reader.Read())
                    {
                        string stbl = (string) reader["tablename"];

                        SchemaQualifiedObjectName calc_gku_xx = new SchemaQualifiedObjectName()
                        {
                            Name = stbl,
                            Schema = CurrentSchema
                        };
                        if (Database.TableExists(calc_gku_xx))
                        {
                            if (!Database.ColumnExists(calc_gku_xx, "rashod_full"))
                            {
                                Database.AddColumn(calc_gku_xx, new Column("rashod_full", DbType.Decimal.WithSize(15, 7), ColumnProperty.NotNull, 0.0000000));
                                Database.ExecuteNonQuery(" update " + stbl + " set rashod_full = rashod ");
                            }
                            if (!Database.ColumnExists(calc_gku_xx, "stek"))
                                Database.AddColumn(calc_gku_xx, new Column("stek", DbType.Int32, ColumnProperty.NotNull, 3));

                            int iyr;
                            try
                            {
                                iyr = Convert.ToInt32(CurrentSchema.Substring(CurrentSchema.Length - 2, 2)) + 2000;
                            }
                            catch  { iyr = 2000; }
                            if (iyr < 0) { iyr = 2000; }

                            int imn;
                            try
                            {
                                imn = Convert.ToInt32(stbl.Substring(stbl.Length - 2, 2));
                            }
                            catch  { imn = 1; }
                            if (imn < 0)  { imn = 1;  }
                            if (imn > 12) { imn = 12; }

                            if (!Database.ColumnExists(calc_gku_xx, "dat_s"))
                            {
                                Database.AddColumn(calc_gku_xx, new Column("dat_s", DbType.Date, ColumnProperty.NotNull, "'01.01.1900'"));
                                Database.ExecuteNonQuery(" update " + stbl + " set dat_s ='01." + imn + "." + iyr + "' ");
                            }
                            if (!Database.ColumnExists(calc_gku_xx, "dat_po"))
                            {
                                Database.AddColumn(calc_gku_xx, new Column("dat_po", DbType.Date, ColumnProperty.NotNull, "'01.01.1900'"));
                                Database.ExecuteNonQuery(" update " + stbl + " set dat_po ='"+DateTime.DaysInMonth(iyr,imn)+"." + imn + "." + iyr + "' ");
                            }
                            if (!Database.ColumnExists(calc_gku_xx, "cntd"))
                            {
                                Database.AddColumn(calc_gku_xx, new Column("cntd", DbType.Int32, ColumnProperty.NotNull, 0));
                                Database.ExecuteNonQuery(" update " + stbl + " set cntd = " + DateTime.DaysInMonth(iyr, imn) + " ");
                            }
                            if (!Database.ColumnExists(calc_gku_xx, "cntd_mn"))
                            {
                                Database.AddColumn(calc_gku_xx, new Column("cntd_mn", DbType.Int32, ColumnProperty.NotNull, 0));
                                Database.ExecuteNonQuery(" update " + stbl + " set cntd_mn = " + DateTime.DaysInMonth(iyr, imn) + " ");
                            }

                            if (!Database.IndexExists("ix4_" + stbl, calc_gku_xx))
                                Database.AddIndex("ix4_" + stbl, false, calc_gku_xx, new string[] {"nzp_kvar","stek","dat_s","dat_po"});

                        }
                    }
                }
                finally { reader.Close(); }
                
            }
            // ...
        }
    }
}
