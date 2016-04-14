using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014063
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014062406301, MigrateDataBase.CentralBank)]
    public class Migration_2014062406301_CentralBank : Migration
    {
        public override void Apply()
        { 
            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data
            SchemaQualifiedObjectName fn_percent_dom = new SchemaQualifiedObjectName() { Name = "fn_percent_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };

            if (!Database.ColumnExists(fn_percent_dom, "nzp_serv_from"))
            {
                Database.AddColumn(fn_percent_dom, new Column("nzp_serv_from", DbType.Int32));
                Database.ChangeDefaultValue(fn_percent_dom, "nzp_serv_from", 0);
            }
            if (!Database.ColumnExists(fn_percent_dom, "perc_ud"))
            {
                Database.ChangeColumn(fn_percent_dom, "perc_ud", DbType.Decimal.WithSize(10, 3), false);
                Database.ChangeDefaultValue(fn_percent_dom, "perc_ud", 0);
            }
            if (!Database.ColumnExists(fn_dogovor, "nzp_supp"))
            {
                Database.AddColumn(fn_dogovor, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_dogovor, "nzp_supp", 0);
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data
            SchemaQualifiedObjectName fn_percent_dom = new SchemaQualifiedObjectName() { Name = "fn_percent_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };
            
            if (Database.ColumnExists(fn_percent_dom, "nzp_serv_from")) Database.RemoveColumn(fn_percent_dom, "nzp_serv_from");
            if (Database.ColumnExists(fn_dogovor, "nzp_supp")) Database.RemoveColumn(fn_dogovor, "nzp_supp");
        }
    }

 
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014062406301, MigrateDataBase.Fin)]
    public class Migration_2014062406301_Fin : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Fins
            SchemaQualifiedObjectName fn_distrib_dom_01 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_01", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_02 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_02", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_03 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_03", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_04 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_04", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_05 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_05", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_06 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_06", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_07 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_07", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_08 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_08", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_09 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_09", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_10 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_10", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_11 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_11", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_12 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_12", Schema = CurrentSchema };

            if (Database.IndexExists("ix_fnd_dom_3_01", fn_distrib_dom_01) && !Database.ColumnExists(fn_distrib_dom_01, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_01", fn_distrib_dom_01);
            if (Database.IndexExists("ix_fnd_dom_01_3", fn_distrib_dom_01) && !Database.ColumnExists(fn_distrib_dom_01, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_01_3", fn_distrib_dom_01);
            if (Database.IndexExists("ix_fnd_dom_01_4", fn_distrib_dom_01) && !Database.ColumnExists(fn_distrib_dom_01, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_01_4", fn_distrib_dom_01);
            if (Database.IndexExists("ix_fnd_dom_01_8", fn_distrib_dom_01) && !Database.ColumnExists(fn_distrib_dom_01, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_01_8", fn_distrib_dom_01);


            if (Database.IndexExists("ix_fnd_dom_3_02", fn_distrib_dom_02) && !Database.ColumnExists(fn_distrib_dom_02, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_02", fn_distrib_dom_02);
            if (Database.IndexExists("ix_fnd_dom_02_3", fn_distrib_dom_02) && !Database.ColumnExists(fn_distrib_dom_02, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_02_3", fn_distrib_dom_02);
            if (Database.IndexExists("ix_fnd_dom_02_4", fn_distrib_dom_02) && !Database.ColumnExists(fn_distrib_dom_02, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_02_4", fn_distrib_dom_02);
            if (Database.IndexExists("ix_fnd_dom_02_8", fn_distrib_dom_02) && !Database.ColumnExists(fn_distrib_dom_02, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_02_8", fn_distrib_dom_02);

            if (Database.IndexExists("ix_fnd_dom_3_03", fn_distrib_dom_03) && !Database.ColumnExists(fn_distrib_dom_03, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_03", fn_distrib_dom_03);
            if (Database.IndexExists("ix_fnd_dom_03_3", fn_distrib_dom_03) && !Database.ColumnExists(fn_distrib_dom_03, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_03_3", fn_distrib_dom_03);
            if (Database.IndexExists("ix_fnd_dom_03_4", fn_distrib_dom_03) && !Database.ColumnExists(fn_distrib_dom_03, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_03_4", fn_distrib_dom_03);
            if (Database.IndexExists("ix_fnd_dom_03_8", fn_distrib_dom_03) && !Database.ColumnExists(fn_distrib_dom_03, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_03_8", fn_distrib_dom_01);

            if (Database.IndexExists("ix_fnd_dom_3_04", fn_distrib_dom_04) && !Database.ColumnExists(fn_distrib_dom_04, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_04", fn_distrib_dom_04);
            if (Database.IndexExists("ix_fnd_dom_04_3", fn_distrib_dom_04) && !Database.ColumnExists(fn_distrib_dom_04, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_04_3", fn_distrib_dom_04);
            if (Database.IndexExists("ix_fnd_dom_04_4", fn_distrib_dom_04) && !Database.ColumnExists(fn_distrib_dom_04, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_04_4", fn_distrib_dom_04);
            if (Database.IndexExists("ix_fnd_dom_04_8", fn_distrib_dom_04) && !Database.ColumnExists(fn_distrib_dom_04, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_04_8", fn_distrib_dom_04);

            if (Database.IndexExists("ix_fnd_dom_3_05", fn_distrib_dom_05) && !Database.ColumnExists(fn_distrib_dom_05, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_05", fn_distrib_dom_05);
            if (Database.IndexExists("ix_fnd_dom_05_3", fn_distrib_dom_05) && !Database.ColumnExists(fn_distrib_dom_05, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_05_3", fn_distrib_dom_05);
            if (Database.IndexExists("ix_fnd_dom_05_4", fn_distrib_dom_05) && !Database.ColumnExists(fn_distrib_dom_05, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_05_4", fn_distrib_dom_05);
            if (Database.IndexExists("ix_fnd_dom_05_8", fn_distrib_dom_05) && !Database.ColumnExists(fn_distrib_dom_05, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_05_8", fn_distrib_dom_05);

            if (Database.IndexExists("ix_fnd_dom_3_06", fn_distrib_dom_06) && !Database.ColumnExists(fn_distrib_dom_06, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_06", fn_distrib_dom_06);
            if (Database.IndexExists("ix_fnd_dom_06_3", fn_distrib_dom_06) && !Database.ColumnExists(fn_distrib_dom_06, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_06_3", fn_distrib_dom_06);
            if (Database.IndexExists("ix_fnd_dom_06_4", fn_distrib_dom_06) && !Database.ColumnExists(fn_distrib_dom_06, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_06_4", fn_distrib_dom_06);
            if (Database.IndexExists("ix_fnd_dom_06_8", fn_distrib_dom_06) && !Database.ColumnExists(fn_distrib_dom_06, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_06_8", fn_distrib_dom_06);

            if (Database.IndexExists("ix_fnd_dom_3_07", fn_distrib_dom_07) && !Database.ColumnExists(fn_distrib_dom_07, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_07", fn_distrib_dom_07);
            if (Database.IndexExists("ix_fnd_dom_07_3", fn_distrib_dom_07) && !Database.ColumnExists(fn_distrib_dom_07, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_07_3", fn_distrib_dom_07);
            if (Database.IndexExists("ix_fnd_dom_07_4", fn_distrib_dom_07) && !Database.ColumnExists(fn_distrib_dom_07, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_07_4", fn_distrib_dom_07);
            if (Database.IndexExists("ix_fnd_dom_07_8", fn_distrib_dom_07) && !Database.ColumnExists(fn_distrib_dom_07, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_07_8", fn_distrib_dom_07);

            if (Database.IndexExists("ix_fnd_dom_3_08", fn_distrib_dom_08) && !Database.ColumnExists(fn_distrib_dom_08, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_08", fn_distrib_dom_08);
            if (Database.IndexExists("ix_fnd_dom_08_3", fn_distrib_dom_08) && !Database.ColumnExists(fn_distrib_dom_08, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_08_3", fn_distrib_dom_08);
            if (Database.IndexExists("ix_fnd_dom_08_4", fn_distrib_dom_08) && !Database.ColumnExists(fn_distrib_dom_08, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_08_4", fn_distrib_dom_08);
            if (Database.IndexExists("ix_fnd_dom_08_8", fn_distrib_dom_08) && !Database.ColumnExists(fn_distrib_dom_08, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_08_8", fn_distrib_dom_08);

            if (Database.IndexExists("ix_fnd_dom_3_09", fn_distrib_dom_09) && !Database.ColumnExists(fn_distrib_dom_09, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_09", fn_distrib_dom_09);
            if (Database.IndexExists("ix_fnd_dom_09_3", fn_distrib_dom_09) && !Database.ColumnExists(fn_distrib_dom_09, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_09_3", fn_distrib_dom_09);
            if (Database.IndexExists("ix_fnd_dom_09_4", fn_distrib_dom_09) && !Database.ColumnExists(fn_distrib_dom_09, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_09_4", fn_distrib_dom_09);
            if (Database.IndexExists("ix_fnd_dom_09_8", fn_distrib_dom_09) && !Database.ColumnExists(fn_distrib_dom_09, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_09_8", fn_distrib_dom_09);

            if (Database.IndexExists("ix_fnd_dom_3_10", fn_distrib_dom_10) && !Database.ColumnExists(fn_distrib_dom_10, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_10", fn_distrib_dom_10);
            if (Database.IndexExists("ix_fnd_dom_10_3", fn_distrib_dom_10) && !Database.ColumnExists(fn_distrib_dom_10, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_10_3", fn_distrib_dom_10);
            if (Database.IndexExists("ix_fnd_dom_10_4", fn_distrib_dom_10) && !Database.ColumnExists(fn_distrib_dom_10, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_10_4", fn_distrib_dom_10);
            if (Database.IndexExists("ix_fnd_dom_10_8", fn_distrib_dom_10) && !Database.ColumnExists(fn_distrib_dom_10, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_10_8", fn_distrib_dom_10);

            if (Database.IndexExists("ix_fnd_dom_3_11", fn_distrib_dom_11) && !Database.ColumnExists(fn_distrib_dom_11, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_11", fn_distrib_dom_11);
            if (Database.IndexExists("ix_fnd_dom_11_3", fn_distrib_dom_11) && !Database.ColumnExists(fn_distrib_dom_11, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_11_3", fn_distrib_dom_11);
            if (Database.IndexExists("ix_fnd_dom_11_4", fn_distrib_dom_11) && !Database.ColumnExists(fn_distrib_dom_11, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_11_4", fn_distrib_dom_11);
            if (Database.IndexExists("ix_fnd_dom_11_8", fn_distrib_dom_11) && !Database.ColumnExists(fn_distrib_dom_11, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_11_8", fn_distrib_dom_11);

            if (Database.IndexExists("ix_fnd_dom_3_12", fn_distrib_dom_12) && !Database.ColumnExists(fn_distrib_dom_12, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_3_12", fn_distrib_dom_12);
            if (Database.IndexExists("ix_fnd_dom_12_3", fn_distrib_dom_12) && !Database.ColumnExists(fn_distrib_dom_12, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_12_3", fn_distrib_dom_12);
            if (Database.IndexExists("ix_fnd_dom_12_4", fn_distrib_dom_12) && !Database.ColumnExists(fn_distrib_dom_12, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_12_4", fn_distrib_dom_12);
            if (Database.IndexExists("ix_fnd_dom_12_8", fn_distrib_dom_12) && !Database.ColumnExists(fn_distrib_dom_12, "nzp_supp")) Database.RemoveIndex("ix_fnd_dom_12_8", fn_distrib_dom_12);


            if (!Database.ColumnExists(fn_distrib_dom_01, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_01, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_01, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_02, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_02, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_02, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_03, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_03, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_03, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_04, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_04, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_04, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_05, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_05, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_05, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_06, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_06, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_06, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_07, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_07, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_07, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_08, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_08, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_08, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_09, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_09, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_09, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_10, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_10, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_10, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_11, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_11, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_11, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_distrib_dom_12, "nzp_supp"))
            {
                Database.AddColumn(fn_distrib_dom_12, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_distrib_dom_12, "nzp_supp", 0);
            }



            
            if (!Database.IndexExists("ix_fnd_dom_3_01", fn_distrib_dom_01)) Database.AddIndex("ix_fnd_dom_3_01", false, fn_distrib_dom_01, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_01_3", fn_distrib_dom_01)) Database.AddIndex("ix_fnd_dom_01_3", false, fn_distrib_dom_01, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_01_4", fn_distrib_dom_01)) Database.AddIndex("ix_fnd_dom_01_4", false, fn_distrib_dom_01, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_01_8", fn_distrib_dom_01)) Database.AddIndex("ix_fnd_dom_01_8", false, fn_distrib_dom_01, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_01_9", fn_distrib_dom_01)) Database.AddIndex("ix_fnd_dom_01_9", false, fn_distrib_dom_01, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_02", fn_distrib_dom_02)) Database.AddIndex("ix_fnd_dom_3_02", false, fn_distrib_dom_02, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_02_3", fn_distrib_dom_02)) Database.AddIndex("ix_fnd_dom_02_3", false, fn_distrib_dom_02, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_02_4", fn_distrib_dom_02)) Database.AddIndex("ix_fnd_dom_02_4", false, fn_distrib_dom_02, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_02_8", fn_distrib_dom_02)) Database.AddIndex("ix_fnd_dom_02_8", false, fn_distrib_dom_02, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_02_9", fn_distrib_dom_02)) Database.AddIndex("ix_fnd_dom_02_9", false, fn_distrib_dom_02, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_03", fn_distrib_dom_03)) Database.AddIndex("ix_fnd_dom_3_03", false, fn_distrib_dom_03, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_03_3", fn_distrib_dom_03)) Database.AddIndex("ix_fnd_dom_03_3", false, fn_distrib_dom_03, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_03_4", fn_distrib_dom_03)) Database.AddIndex("ix_fnd_dom_03_4", false, fn_distrib_dom_03, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_03_8", fn_distrib_dom_03)) Database.AddIndex("ix_fnd_dom_03_8", false, fn_distrib_dom_03, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_03_9", fn_distrib_dom_03)) Database.AddIndex("ix_fnd_dom_03_9", false, fn_distrib_dom_03, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_04", fn_distrib_dom_04)) Database.AddIndex("ix_fnd_dom_3_04", false, fn_distrib_dom_04, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_04_3", fn_distrib_dom_04)) Database.AddIndex("ix_fnd_dom_04_3", false, fn_distrib_dom_04, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_04_4", fn_distrib_dom_04)) Database.AddIndex("ix_fnd_dom_04_4", false, fn_distrib_dom_04, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_04_8", fn_distrib_dom_04)) Database.AddIndex("ix_fnd_dom_04_8", false, fn_distrib_dom_04, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_04_9", fn_distrib_dom_04)) Database.AddIndex("ix_fnd_dom_04_9", false, fn_distrib_dom_04, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_05", fn_distrib_dom_05)) Database.AddIndex("ix_fnd_dom_3_05", false, fn_distrib_dom_05, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_05_3", fn_distrib_dom_05)) Database.AddIndex("ix_fnd_dom_05_3", false, fn_distrib_dom_05, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_05_4", fn_distrib_dom_05)) Database.AddIndex("ix_fnd_dom_05_4", false, fn_distrib_dom_05, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_05_8", fn_distrib_dom_05)) Database.AddIndex("ix_fnd_dom_05_8", false, fn_distrib_dom_05, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_05_9", fn_distrib_dom_05)) Database.AddIndex("ix_fnd_dom_05_9", false, fn_distrib_dom_05, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_06", fn_distrib_dom_06)) Database.AddIndex("ix_fnd_dom_3_06", false, fn_distrib_dom_06, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_06_3", fn_distrib_dom_06)) Database.AddIndex("ix_fnd_dom_06_3", false, fn_distrib_dom_06, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_06_4", fn_distrib_dom_06)) Database.AddIndex("ix_fnd_dom_06_4", false, fn_distrib_dom_06, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_06_8", fn_distrib_dom_06)) Database.AddIndex("ix_fnd_dom_06_8", false, fn_distrib_dom_06, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_06_9", fn_distrib_dom_06)) Database.AddIndex("ix_fnd_dom_06_9", false, fn_distrib_dom_06, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_07", fn_distrib_dom_07)) Database.AddIndex("ix_fnd_dom_3_07", false, fn_distrib_dom_07, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_07_3", fn_distrib_dom_07)) Database.AddIndex("ix_fnd_dom_07_3", false, fn_distrib_dom_07, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_07_4", fn_distrib_dom_07)) Database.AddIndex("ix_fnd_dom_07_4", false, fn_distrib_dom_07, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_07_8", fn_distrib_dom_07)) Database.AddIndex("ix_fnd_dom_07_8", false, fn_distrib_dom_07, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_07_9", fn_distrib_dom_07)) Database.AddIndex("ix_fnd_dom_07_9", false, fn_distrib_dom_07, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_08", fn_distrib_dom_08)) Database.AddIndex("ix_fnd_dom_3_08", false, fn_distrib_dom_08, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_08_3", fn_distrib_dom_08)) Database.AddIndex("ix_fnd_dom_08_3", false, fn_distrib_dom_08, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_08_4", fn_distrib_dom_08)) Database.AddIndex("ix_fnd_dom_08_4", false, fn_distrib_dom_08, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_08_8", fn_distrib_dom_08)) Database.AddIndex("ix_fnd_dom_08_8", false, fn_distrib_dom_08, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_08_9", fn_distrib_dom_08)) Database.AddIndex("ix_fnd_dom_08_9", false, fn_distrib_dom_08, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_09", fn_distrib_dom_09)) Database.AddIndex("ix_fnd_dom_3_09", false, fn_distrib_dom_09, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_09_3", fn_distrib_dom_09)) Database.AddIndex("ix_fnd_dom_09_3", false, fn_distrib_dom_09, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_09_4", fn_distrib_dom_09)) Database.AddIndex("ix_fnd_dom_09_4", false, fn_distrib_dom_09, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_09_8", fn_distrib_dom_09)) Database.AddIndex("ix_fnd_dom_09_8", false, fn_distrib_dom_09, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_09_9", fn_distrib_dom_09)) Database.AddIndex("ix_fnd_dom_09_9", false, fn_distrib_dom_09, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_10", fn_distrib_dom_10)) Database.AddIndex("ix_fnd_dom_3_10", false, fn_distrib_dom_10, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_10_3", fn_distrib_dom_10)) Database.AddIndex("ix_fnd_dom_10_3", false, fn_distrib_dom_10, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_10_4", fn_distrib_dom_10)) Database.AddIndex("ix_fnd_dom_10_4", false, fn_distrib_dom_10, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_10_8", fn_distrib_dom_10)) Database.AddIndex("ix_fnd_dom_10_8", false, fn_distrib_dom_10, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_10_9", fn_distrib_dom_10)) Database.AddIndex("ix_fnd_dom_10_9", false, fn_distrib_dom_10, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_11", fn_distrib_dom_11)) Database.AddIndex("ix_fnd_dom_3_11", false, fn_distrib_dom_11, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_11_3", fn_distrib_dom_11)) Database.AddIndex("ix_fnd_dom_11_3", false, fn_distrib_dom_11, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_11_4", fn_distrib_dom_11)) Database.AddIndex("ix_fnd_dom_11_4", false, fn_distrib_dom_11, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_11_8", fn_distrib_dom_11)) Database.AddIndex("ix_fnd_dom_11_8", false, fn_distrib_dom_11, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_11_9", fn_distrib_dom_11)) Database.AddIndex("ix_fnd_dom_11_9", false, fn_distrib_dom_11, "nzp_supp");

            if (!Database.IndexExists("ix_fnd_dom_3_12", fn_distrib_dom_12)) Database.AddIndex("ix_fnd_dom_3_12", false, fn_distrib_dom_12, "nzp_supp", "nzp_payer", "nzp_serv", "nzp_dom");
            if (!Database.IndexExists("ix_fnd_dom_12_3", fn_distrib_dom_12)) Database.AddIndex("ix_fnd_dom_12_3", false, fn_distrib_dom_12, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fnd_dom_12_4", fn_distrib_dom_12)) Database.AddIndex("ix_fnd_dom_12_4", false, fn_distrib_dom_12, "nzp_supp", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_12_8", fn_distrib_dom_12)) Database.AddIndex("ix_fnd_dom_12_8", false, fn_distrib_dom_12, "nzp_supp", "nzp_dom", "nzp_serv");
            if (!Database.IndexExists("ix_fnd_dom_12_9", fn_distrib_dom_12)) Database.AddIndex("ix_fnd_dom_12_9", false, fn_distrib_dom_12, "nzp_supp");

            SchemaQualifiedObjectName fn_sended_dom = new SchemaQualifiedObjectName() { Name = "fn_sended_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_sended = new SchemaQualifiedObjectName() { Name = "fn_sended", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_reval_dom = new SchemaQualifiedObjectName() { Name = "fn_reval_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_naud_dom = new SchemaQualifiedObjectName() { Name = "fn_naud_dom", Schema = CurrentSchema };

            if (!Database.ColumnExists(fn_sended_dom, "nzp_supp"))
            {
                Database.AddColumn(fn_sended_dom, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_sended_dom, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_sended, "nzp_supp"))
            {
                Database.AddColumn(fn_sended, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_sended, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_reval_dom, "nzp_supp"))
            {
                Database.AddColumn(fn_reval_dom, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_reval_dom, "nzp_supp", 0);
            }
            if (!Database.ColumnExists(fn_naud_dom, "nzp_supp"))
            {
                Database.AddColumn(fn_naud_dom, new Column("nzp_supp", DbType.Int32));
                Database.ChangeDefaultValue(fn_naud_dom, "nzp_supp", 0);
            }

            SchemaQualifiedObjectName fn_perc_dom = new SchemaQualifiedObjectName() { Name = "fn_perc_dom", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_perc_dom, "perc_ud"))
            {
                Database.ChangeColumn(fn_perc_dom, "perc_ud", DbType.Decimal.WithSize(10, 3), false);
                Database.ChangeDefaultValue(fn_perc_dom, "perc_ud", 0);
            }
        }

        public override void Revert()
        {
            // TODO: Downgrade Fins

            SchemaQualifiedObjectName fn_distrib_dom_01 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_01", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_02 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_02", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_03 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_03", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_04 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_04", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_05 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_05", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_06 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_06", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_07 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_07", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_08 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_08", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_09 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_09", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_10 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_10", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_11 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_11", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_distrib_dom_12 = new SchemaQualifiedObjectName() { Name = "fn_distrib_dom_12", Schema = CurrentSchema };

            if (Database.ColumnExists(fn_distrib_dom_01, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_01, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_02, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_02, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_03, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_03, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_04, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_04, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_05, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_05, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_06, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_06, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_07, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_07, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_08, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_08, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_09, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_09, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_10, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_10, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_11, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_11, "nzp_supp");
            if (Database.ColumnExists(fn_distrib_dom_12, "nzp_supp")) Database.RemoveColumn(fn_distrib_dom_12, "nzp_supp");

            SchemaQualifiedObjectName fn_sended_dom = new SchemaQualifiedObjectName() { Name = "fn_sended_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_sended = new SchemaQualifiedObjectName() { Name = "fn_sended", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_reval_dom = new SchemaQualifiedObjectName() { Name = "fn_reval_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_naud_dom = new SchemaQualifiedObjectName() { Name = "fn_naud_dom", Schema = CurrentSchema };

            if (Database.ColumnExists(fn_sended_dom, "nzp_supp")) Database.RemoveColumn(fn_sended_dom, "nzp_supp");
            if (Database.ColumnExists(fn_sended, "nzp_supp")) Database.RemoveColumn(fn_sended, "nzp_supp");
            if (Database.ColumnExists(fn_reval_dom, "nzp_supp")) Database.RemoveColumn(fn_reval_dom, "nzp_supp");
            if (Database.ColumnExists(fn_naud_dom, "nzp_supp")) Database.RemoveColumn(fn_naud_dom, "nzp_supp");
            }
    }
}
