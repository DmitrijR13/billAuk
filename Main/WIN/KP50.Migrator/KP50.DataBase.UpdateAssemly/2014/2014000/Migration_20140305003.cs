using KP50.DataBase.Migrator.Framework;
using System.Collections.Generic;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssemly
{
    [Migration(20140305003, MigrateDataBase.CentralBank)]
    public class Migration_20140305003_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CurrentSchema };
            if(!Database.ColumnExists(s_typercl, "is_volum")) Database.AddColumn(s_typercl, new Column("is_volum", DbType.Int32, ColumnProperty.None, 0));
            Database.Delete(s_typercl, "type_rcl = 63");
            Database.Delete(s_typercl, "type_rcl = 163");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename" }, new string[] { "63", "0", "Снятие ОДН" });
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename" }, new string[] { "163", "1", "Изменение расхода + Учет ОДН" });

            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            Database.Delete(resolution, "nzp_res = 9999");
            Database.Insert(resolution, 
                new string[] { "nzp_res", "name_short", "name_res", "is_readonly" }, 
                new string[] { "9999", "ТЛС", "таблица типов лицевых счетов", "1" });

            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            Database.Delete(res_y, "nzp_res = 9999");
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "9999", "1", "население" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "9999", "2", "бюджет" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "9999", "3", "арендаторы" });

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName link_ls_lit = new SchemaQualifiedObjectName() { Name = "link_ls_lit", Schema = CurrentSchema };
            if (!Database.TableExists(link_ls_lit))
            {
                Database.AddTable(
                    link_ls_lit,
                    new Column("nzp_ls", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull), // Identity property is SERIAL and NOT NULL value default
                    new Column("nzp_dom", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_kvar_base", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull)
                    );
            }

            if (!Database.IndexExists("ix0_link_ls_lit", link_ls_lit)) Database.AddIndex("ix0_link_ls_lit", true, link_ls_lit, "nzp_ls");
            if (!Database.IndexExists("ix1_link_ls_lit", link_ls_lit)) Database.AddIndex("ix1_link_ls_lit", true, link_ls_lit, "nzp_kvar_base", "nzp_kvar");
            if (!Database.IndexExists("ix2_link_ls_lit", link_ls_lit)) Database.AddIndex("ix2_link_ls_lit", true, link_ls_lit, "nzp_dom", "nzp_kvar");
            if (!Database.IndexExists("ix3_link_ls_lit", link_ls_lit)) Database.AddIndex("ix3_link_ls_lit", true, link_ls_lit, "nzp_kvar");
            if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("UPDATE STATISTICS FOR TABLE link_ls_lit");
            if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery("ANALYZE link_ls_lit");

            SchemaQualifiedObjectName tarif_calculation = new SchemaQualifiedObjectName() { Name = "tarif_calculation", Schema = CurrentSchema };
            if (!Database.TableExists(tarif_calculation))
            {
                Database.AddTable(tarif_calculation,
                    new Column("nzp_prm_calc", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("dat_s", DbType.Date),
                    new Column("dat_po", DbType.Date),
                    new Column("nzp_area", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("tarif", DbType.Decimal.WithSize(12, 3)),
                    new Column("nzp_prm", DbType.Int32));

                if (!Database.IndexExists("ix_prm_calculation_nzp_area", tarif_calculation)) Database.AddIndex("ix_prm_calculation_nzp_area", false, tarif_calculation, "nzp_area");
                if (!Database.IndexExists("ix_prm_calculation_nzp_serv", tarif_calculation)) Database.AddIndex("ix_prm_calculation_nzp_serv", false, tarif_calculation, "nzp_serv");
                if (!Database.IndexExists("ix_prm_calculation_nzp_prm", tarif_calculation)) Database.AddIndex("ix_prm_calculation_nzp_prm", false, tarif_calculation, "nzp_prm");

                List<string> lstQuerys = new List<string>();
                if (Database.ProviderName == "Informix" || Database.ProviderName == "PostgreSQL")
                {
                    /*
                    lstQuerys.Add(
                        "SELECT s.nzp_serv, " +
                        "se.service, " +
                        "s.nzp_convbd * 1000000 + 17 * 1000 + a.ktr nzp_prm, " +
                        "v.val_prm, " +
                        "v.dat_s, " +
                        "v.dat_po, " +
                        "a.dt, " +
                        "a.kkst, " +
                        "a.ktr, " +
                        "a.nzp_conv_db, " +
                        "max(a.sum) tarif " +
                        "FROM " + CentralData + ":arx9 a, " +
                        CentralData + ":s_calc_line s, " +
                        CentralData + ":prm_5 v, " +
                        CentralKernel + ":services se " + 
                        "WHERE s.nzp_convbd = a.nzp_conv_db " +
                        "AND s.nzp_serv = se.nzp_serv " +
                        "AND a.kkst=s.kodin " +
                        "AND a.sum > 0.001 " +
                        "AND v.nzp_prm = (s.nzp_convbd * 1000000 + 17 * 1000 + a.ktr) " +
                        "AND a.dt between dat_s and dat_po " +
                        "AND v.is_actual <> 100 " +
                        "GROUP BY 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 " +
                        "ORDER BY 3, 4 " + 
                        "INTO TEMP temp_tarif_calculation WITH NO LOG");

                    lstQuerys.Add(
                        "INSERT INTO " + CentralData + ".tarif_calculation ( dat_s, dat_po, nzp_area, nzp_serv, tarif, nzp_prm)" +
                        "SELECT UNIQUE dat_s, dat_po, nzp_conv_db, nzp_serv, tarif, nzp_prm FROM temp_tarif_calculation");
                     */
                }

                SchemaQualifiedObjectName temp_tarif_calculation = new SchemaQualifiedObjectName() { Name = "temp_tarif_calculation", Schema = CurrentSchema };
                if (Database.TableExists(temp_tarif_calculation)) Database.RemoveTable(temp_tarif_calculation);
                foreach (string strQuery in lstQuerys) Database.ExecuteNonQuery(strQuery);
                lstQuerys.Clear();
                if (Database.TableExists(temp_tarif_calculation)) Database.RemoveTable(temp_tarif_calculation);
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CurrentSchema };
            if (Database.ColumnExists(s_typercl, "is_volum")) Database.RemoveColumn(s_typercl, "is_volum");

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName link_ls_lit = new SchemaQualifiedObjectName() { Name = "link_ls_lit", Schema = CurrentSchema };
            if (Database.IndexExists("ix0_link_ls_lit", link_ls_lit)) Database.RemoveIndex("ix0_link_ls_lit", link_ls_lit);
            if (Database.IndexExists("ix1_link_ls_lit", link_ls_lit)) Database.RemoveIndex("ix1_link_ls_lit", link_ls_lit);
            if (Database.IndexExists("ix2_link_ls_lit", link_ls_lit)) Database.RemoveIndex("ix2_link_ls_lit", link_ls_lit);
            if (Database.IndexExists("ix3_link_ls_lit", link_ls_lit)) Database.RemoveIndex("ix3_link_ls_lit", link_ls_lit);
            if (Database.TableExists(link_ls_lit)) Database.RemoveTable(link_ls_lit);

            SchemaQualifiedObjectName tarif_calculation = new SchemaQualifiedObjectName() { Name = "tarif_calculation", Schema = CurrentSchema };
            if (Database.TableExists(tarif_calculation)) Database.RemoveTable(tarif_calculation);
        }
    }

    [Migration(20140305003, MigrateDataBase.LocalBank)]
    public class Migration_20140305003_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CurrentSchema };
            if (!Database.ColumnExists(s_typercl, "is_volum")) Database.AddColumn(s_typercl, new Column("is_volum", DbType.Int32, ColumnProperty.None, 0));
            Database.Delete(s_typercl, "type_rcl = 63");
            Database.Delete(s_typercl, "type_rcl = 163");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename" }, new string[] { "63", "0", "Снятие ОДН" });
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename" }, new string[] { "163", "1", "Изменение расхода + Учет ОДН" });

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName link_ls_lit = new SchemaQualifiedObjectName() { Name = "link_ls_lit", Schema = CurrentSchema };
            if (!Database.TableExists(link_ls_lit))
            {
                Database.AddTable(
                    link_ls_lit,
                    new Column("nzp_ls", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull), // Identity property is NOT NULL value default
                    new Column("nzp_dom", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_kvar_base", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull)
                    );
            }

            if (!Database.IndexExists("ix0_link_ls_lit", link_ls_lit)) Database.AddIndex("ix0_link_ls_lit", true, link_ls_lit, "nzp_ls");
            if (!Database.IndexExists("ix1_link_ls_lit", link_ls_lit)) Database.AddIndex("ix1_link_ls_lit", true, link_ls_lit, "nzp_kvar_base", "nzp_kvar");
            if (!Database.IndexExists("ix2_link_ls_lit", link_ls_lit)) Database.AddIndex("ix2_link_ls_lit", true, link_ls_lit, "nzp_dom", "nzp_kvar");
            if (!Database.IndexExists("ix3_link_ls_lit", link_ls_lit)) Database.AddIndex("ix3_link_ls_lit", true, link_ls_lit, "nzp_kvar");
            if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("UPDATE STATISTICS FOR TABLE link_ls_lit");
            if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery("ANALYZE link_ls_lit");
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CurrentSchema };
            if (Database.ColumnExists(s_typercl, "is_volum")) Database.RemoveColumn(s_typercl, "is_volum");

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName link_ls_lit = new SchemaQualifiedObjectName() { Name = "link_ls_lit", Schema = CurrentSchema };
            if (Database.IndexExists("ix0_link_ls_lit", link_ls_lit)) Database.RemoveIndex("ix0_link_ls_lit", link_ls_lit);
            if (Database.IndexExists("ix1_link_ls_lit", link_ls_lit)) Database.RemoveIndex("ix1_link_ls_lit", link_ls_lit);
            if (Database.IndexExists("ix2_link_ls_lit", link_ls_lit)) Database.RemoveIndex("ix2_link_ls_lit", link_ls_lit);
            if (Database.IndexExists("ix3_link_ls_lit", link_ls_lit)) Database.RemoveIndex("ix3_link_ls_lit", link_ls_lit);
            if (Database.TableExists(link_ls_lit)) Database.RemoveTable(link_ls_lit);
        }
    }

    [Migration(20140305003, MigrateDataBase.Charge)]
    public class Migration_20140305003_Charge : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName perekidka = new SchemaQualifiedObjectName() { Name = "perekidka", Schema = CurrentSchema };
            if (!Database.ColumnExists(perekidka, "tarif")) Database.AddColumn(perekidka, new Column("tarif", DbType.Decimal.WithSize(14, 3), ColumnProperty.None, 0));
            if (!Database.ColumnExists(perekidka, "volum")) Database.AddColumn(perekidka, new Column("volum", DbType.Decimal.WithSize(14, 6), ColumnProperty.None, 0));

            List<SchemaQualifiedObjectName> lstCounters = new List<SchemaQualifiedObjectName>();
            for (int i = 1; i <= 12; i++) lstCounters.Add(new SchemaQualifiedObjectName() { Name = string.Format("counters_{0}", i.ToString("00")), Schema=CurrentSchema });
            foreach (SchemaQualifiedObjectName counters in lstCounters)
            {
                if (!Database.ColumnExists(counters, "ngp_cnt")) Database.AddColumn(counters, new Column("ngp_cnt", DbType.Decimal, ColumnProperty.None, 0.0000000));
                if (!Database.ColumnExists(counters, "rash_norm_one")) Database.AddColumn(counters, new Column("rash_norm_one", DbType.Decimal, ColumnProperty.None, 0.0000000));
            }
            lstCounters.Clear();

            List<SchemaQualifiedObjectName> lstCalcGku = new List<SchemaQualifiedObjectName>();
            for (int i = 1; i <= 12; i++) lstCalcGku.Add(new SchemaQualifiedObjectName() { Name = string.Format("calc_gku_{0}", i.ToString("00")), Schema = CurrentSchema });
            foreach (SchemaQualifiedObjectName calc_gku in lstCalcGku)
            {
                if (!Database.ColumnExists(calc_gku, "rash_norm_one")) Database.AddColumn(calc_gku, new Column("rash_norm_one", DbType.Decimal.WithSize(14, 7), ColumnProperty.NotNull, 0.0000));
                if (!Database.ColumnExists(calc_gku, "valm")) Database.AddColumn(calc_gku, new Column("valm", DbType.Decimal.WithSize(15, 7), ColumnProperty.NotNull, 0.0000));
                if (!Database.ColumnExists(calc_gku, "dop87")) Database.AddColumn(calc_gku, new Column("dop87", DbType.Decimal.WithSize(15, 7), ColumnProperty.NotNull, 0.0000));
                if (!Database.ColumnExists(calc_gku, "is_device")) Database.AddColumn(calc_gku, new Column("is_device", DbType.Int32, ColumnProperty.NotNull, 0));
            }
            lstCalcGku.Clear();
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName perekidka = new SchemaQualifiedObjectName() { Name = "perekidka", Schema = CurrentSchema };
            if (Database.ColumnExists(perekidka, "tarif")) Database.RemoveColumn(perekidka, "tarif");
            if (Database.ColumnExists(perekidka, "volum")) Database.RemoveColumn(perekidka, "volum");

            List<SchemaQualifiedObjectName> lstCounters = new List<SchemaQualifiedObjectName>();
            for (int i = 1; i <= 12; i++) lstCounters.Add(new SchemaQualifiedObjectName() { Name = string.Format("counters_{0}", i.ToString("00")), Schema = CurrentSchema });
            foreach (SchemaQualifiedObjectName counters in lstCounters)
            {
                if (Database.ColumnExists(counters, "ngp_cnt")) Database.RemoveColumn(counters, "ngp_cnt");
                if (Database.ColumnExists(counters, "rash_norm_one")) Database.RemoveColumn(counters, "rash_norm_one");
            }
            lstCounters.Clear();

            List<SchemaQualifiedObjectName> lstCalcGku = new List<SchemaQualifiedObjectName>();
            for (int i = 1; i <= 12; i++) lstCalcGku.Add(new SchemaQualifiedObjectName() { Name = string.Format("calc_gku_{0}", i.ToString("00")), Schema = CurrentSchema });
            foreach (SchemaQualifiedObjectName calc_gku in lstCalcGku)
            {
                if (Database.ColumnExists(calc_gku, "rash_norm_one")) Database.RemoveColumn(calc_gku, "rash_norm_one");
                if (Database.ColumnExists(calc_gku, "valm")) Database.RemoveColumn(calc_gku, "valm");
                if (Database.ColumnExists(calc_gku, "dop87")) Database.RemoveColumn(calc_gku, "dop87");
                if (Database.ColumnExists(calc_gku, "is_device")) Database.RemoveColumn(calc_gku, "is_device");
            }
            lstCalcGku.Clear();
        }
    }

    [Migration(20140305003, MigrateDataBase.Fin)]
    public class Migration_20140305003_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName gil_sums = new SchemaQualifiedObjectName() { Name = "gil_sums", Schema = CurrentSchema };
            if (!Database.ColumnExists(gil_sums, "nzp_supp")) Database.AddColumn(gil_sums, new Column("nzp_supp", DbType.Int32));

            SchemaQualifiedObjectName pu_vals = new SchemaQualifiedObjectName() { Name = "pu_vals", Schema = CurrentSchema };
            if (!Database.ColumnExists(pu_vals, "nzp_counter")) Database.AddColumn(pu_vals, new Column("nzp_counter", DbType.Int32));
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName gil_sums = new SchemaQualifiedObjectName() { Name = "gil_sums", Schema = CurrentSchema };
            if (Database.ColumnExists(gil_sums, "nzp_supp")) Database.RemoveColumn(gil_sums, "nzp_supp");

            SchemaQualifiedObjectName pu_vals = new SchemaQualifiedObjectName() { Name = "pu_vals", Schema = CurrentSchema };
            if (Database.ColumnExists(pu_vals, "nzp_counter")) Database.RemoveColumn(pu_vals, "nzp_counter");
        }
    }
}
