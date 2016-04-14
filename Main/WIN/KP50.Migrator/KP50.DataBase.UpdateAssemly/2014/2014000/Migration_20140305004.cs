using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System.Collections.Generic;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305004, MigrateDataBase.CentralBank)]
    public class Migration_20140305004_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName services_sg = new SchemaQualifiedObjectName() { Name = "services_sg", Schema = CurrentSchema };
            if (!Database.TableExists(services_sg))
            {
                Database.AddTable(services_sg, new Column("nzp_serv", DbType.Int32));
                Database.AddIndex("ix_services_sg", false, services_sg, "nzp_serv");
                Database.Delete(services_sg);
                if (Database.ProviderName == "Informix" || Database.ProviderName == "PostgreSQL") 
                    Database.ExecuteNonQuery("INSERT INTO services_sg (nzp_serv) SELECT nzp_serv FROM services WHERE nzp_serv IN (16, 19, 205, 211, 221, 234, 242, 243, 256, 259, 290, 315)");
            }

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };
            if (!Database.ColumnExists(kvar, "area_code"))
                Database.AddColumn(kvar, new Column("area_code", DbType.Int16, ColumnProperty.None, 0));

            SchemaQualifiedObjectName area_codes = new SchemaQualifiedObjectName() { Name = "area_codes", Schema = CurrentSchema };
            if (!Database.TableExists(area_codes))
            {
                Database.AddTable(area_codes,
                    new Column("code", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_area", DbType.Int32),
                    new Column("changed_by", DbType.Int32),
                    new Column("changed_on", DbType.DateTime),
                    new Column("is_active", DbType.Int16, ColumnProperty.None, 0));
                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("GRANT select, update, insert, delete, index ON area_codes TO public AS informix");
            }

            SchemaQualifiedObjectName tula_ex_sz = new SchemaQualifiedObjectName() { Name = "tula_ex_sz", Schema = CurrentSchema };
            if (!Database.TableExists(tula_ex_sz))
            {
                Database.AddTable(tula_ex_sz,
                    new Column("nzp_ex_sz", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("file_name", DbType.StringFixedLength.WithSize(50)),
                    new Column("dat_upload", DbType.DateTime),
                    new Column("nzp_user", DbType.Int32));
                Database.AddIndex("ix_tula_ex_sz_1", true, tula_ex_sz, "nzp_ex_sz");
            }

            SchemaQualifiedObjectName tula_ex_sz_file = new SchemaQualifiedObjectName() { Name = "tula_ex_sz_file", Schema = CurrentSchema };
            if (!Database.TableExists(tula_ex_sz_file))
            {
                #warning Индусский код detected.
                Database.AddTable(tula_ex_sz_file,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("famil", DbType.StringFixedLength.WithSize(50)),
                    new Column("otch", DbType.StringFixedLength.WithSize(50)),
                    new Column("drog", DbType.Date),
                    new Column("strahnm", DbType.StringFixedLength.WithSize(14)),
                    new Column("nasp", DbType.StringFixedLength.WithSize(50)),
                    new Column("nylic", DbType.StringFixedLength.WithSize(50)),
                    new Column("ndom", DbType.StringFixedLength.WithSize(7)),
                    new Column("nkorp", DbType.StringFixedLength.WithSize(3)),
                    new Column("nkw", DbType.StringFixedLength.WithSize(15)),
                    new Column("nkomn", DbType.StringFixedLength.WithSize(15)),
                    new Column("kolk", DbType.Int32),
                    new Column("lchet", DbType.StringFixedLength.WithSize(24)),
                    new Column("vidgf", DbType.StringFixedLength.WithSize(25)),
                    new Column("privat", DbType.StringFixedLength.WithSize(1)),
                    new Column("opl", DbType.Decimal.WithSize(8, 2)),
                    new Column("otpl", DbType.Decimal.WithSize(8, 2)),
                    new Column("otplj", DbType.Decimal.WithSize(8, 2)),
                    new Column("kolzr", DbType.Int32),
                    new Column("kolpr", DbType.Int32),
                    new Column("prz", DbType.Int32),
                    new Column("prn", DbType.Int32),
                    new Column("prk", DbType.Int32),
                    new Column("gku1", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif1", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum1", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt1", DbType.Decimal.WithSize(14, 4)),
                    new Column("org1", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar1", DbType.Int32),
                    new Column("koef1", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet1", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz1", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz1", DbType.Int32),
                    new Column("ozs1", DbType.Int32),
                    new Column("sumozs1", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku2", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif2", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum2", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt2", DbType.Decimal.WithSize(14, 4)),
                    new Column("org2", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar2", DbType.Int32),
                    new Column("koef2", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet2", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz2", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz2", DbType.Int32),
                    new Column("ozs2", DbType.Int32),
                    new Column("sumozs2", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku3", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif3", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum3", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt3", DbType.Decimal.WithSize(14, 4)),
                    new Column("org3", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar3", DbType.Int32),
                    new Column("koef3", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet3", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz3", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz3", DbType.Int32),
                    new Column("ozs3", DbType.Int32),
                    new Column("sumozs3", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku4", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif4", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum4", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt4", DbType.Decimal.WithSize(14, 4)),
                    new Column("org4", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar4", DbType.Int32),
                    new Column("koef4", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet4", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz4", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz4", DbType.Int32),
                    new Column("ozs4", DbType.Int32),
                    new Column("sumozs4", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku5", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif5", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum5", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt5", DbType.Decimal.WithSize(14, 4)),
                    new Column("org5", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar5", DbType.Int32),
                    new Column("koef5", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet5", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz5", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz5", DbType.Int32),
                    new Column("ozs5", DbType.Int32),
                    new Column("sumozs5", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku6", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif6", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum6", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt6", DbType.Decimal.WithSize(14, 4)),
                    new Column("org6", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar6", DbType.Int32),
                    new Column("koef6", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet6", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz6", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz6", DbType.Int32),
                    new Column("ozs6", DbType.Int32),
                    new Column("sumozs6", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku7", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif7", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum7", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt7", DbType.Decimal.WithSize(14, 4)),
                    new Column("org7", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar7", DbType.Int32),
                    new Column("koef7", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet7", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz7", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz7", DbType.Int32),
                    new Column("ozs7", DbType.Int32),
                    new Column("sumozs7", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku8", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif8", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum8", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt8", DbType.Decimal.WithSize(14, 4)),
                    new Column("org8", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar8", DbType.Int32),
                    new Column("koef8", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet8", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz8", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz8", DbType.Int32),
                    new Column("ozs8", DbType.Int32),
                    new Column("sumozs8", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku9", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif9", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum9", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt9", DbType.Decimal.WithSize(14, 4)),
                    new Column("org9", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar9", DbType.Int32),
                    new Column("koef9", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet9", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz9", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz9", DbType.Int32),
                    new Column("ozs9", DbType.Int32),
                    new Column("sumozs9", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku10", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif10", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum10", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt10", DbType.Decimal.WithSize(14, 4)),
                    new Column("org10", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar10", DbType.Int32),
                    new Column("koef10", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet10", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz10", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz10", DbType.Int32),
                    new Column("ozs10", DbType.Int32),
                    new Column("sumozs10", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku11", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif11", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum11", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt11", DbType.Decimal.WithSize(14, 4)),
                    new Column("org11", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar11", DbType.Int32),
                    new Column("koef11", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet11", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz11", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz11", DbType.Int32),
                    new Column("ozs11", DbType.Int32),
                    new Column("sumozs11", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku12", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif12", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum12", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt12", DbType.Decimal.WithSize(14, 4)),
                    new Column("org12", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar12", DbType.Int32),
                    new Column("koef12", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet12", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz12", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz12", DbType.Int32),
                    new Column("ozs12", DbType.Int32),
                    new Column("sumozs12", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku13", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif13", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum13", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt13", DbType.Decimal.WithSize(14, 4)),
                    new Column("org13", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar13", DbType.Int32),
                    new Column("koef13", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet13", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz13", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz13", DbType.Int32),
                    new Column("ozs13", DbType.Int32),
                    new Column("sumozs13", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku14", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif14", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum14", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt14", DbType.Decimal.WithSize(14, 4)),
                    new Column("org14", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar14", DbType.Int32),
                    new Column("koef14", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet14", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz14", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz14", DbType.Int32),
                    new Column("ozs14", DbType.Int32),
                    new Column("sumozs14", DbType.Decimal.WithSize(14, 4)),
                    new Column("gku15", DbType.StringFixedLength.WithSize(100)),
                    new Column("tarif15", DbType.Decimal.WithSize(15, 5)),
                    new Column("sum15", DbType.Decimal.WithSize(14, 4)),
                    new Column("fakt15", DbType.Decimal.WithSize(14, 4)),
                    new Column("org15", DbType.StringFixedLength.WithSize(30)),
                    new Column("vidtar15", DbType.Int32),
                    new Column("koef15", DbType.Decimal.WithSize(12, 2)),
                    new Column("lchet15", DbType.StringFixedLength.WithSize(24)),
                    new Column("sumz15", DbType.Decimal.WithSize(14, 4)),
                    new Column("klmz15", DbType.Int32),
                    new Column("ozs15", DbType.Int32),
                    new Column("sumozs15", DbType.Decimal.WithSize(14, 4)),
                    new Column("nzp_ex_sz", DbType.Int32),
                    new Column("nzp_kvar", DbType.Int32));

                Database.AddIndex("ix_tula_ex_sz_file_1", true, tula_ex_sz_file, "id");
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName services_sg = new SchemaQualifiedObjectName() { Name = "services_sg", Schema = CurrentSchema };
            if (Database.TableExists(services_sg)) Database.RemoveTable(services_sg);

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };
            SchemaQualifiedObjectName area_codes = new SchemaQualifiedObjectName() { Name = "area_codes", Schema = CurrentSchema };
            if (Database.ColumnExists(kvar, "area_code")) Database.RemoveColumn(kvar, "area_code");
            if (Database.TableExists(area_codes)) Database.RemoveTable(area_codes);
        }
    }

    [Migration(20140305004, MigrateDataBase.LocalBank)]
    public class Migration_20140305004_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_calc_trf = new SchemaQualifiedObjectName() { Name = "s_calc_trf", Schema = CurrentSchema };
            SchemaQualifiedObjectName s_calc_trf_lnk = new SchemaQualifiedObjectName() { Name = "s_calc_trf_lnk", Schema = CurrentSchema };
            if (!Database.ColumnExists(s_calc_trf, "nzp_area")) Database.AddColumn(s_calc_trf, new Column("nzp_area", DbType.Int32));
            if (!Database.ColumnExists(s_calc_trf_lnk, "nzp_trfl")) Database.AddColumn(s_calc_trf_lnk, new Column("nzp_trfl", DbType.Int32));

            if (!Database.SequenceExists(CurrentSchema, "seqtrf")) Database.AddSequence(CurrentSchema, "seqtrf");
            if (Database.ProviderName == "Informix")
            {
                Database.ExecuteNonQuery("ALTER TABLE s_calc_trf_lnk MODIFY nzp_trfl INTEGER NOT NULL");
                Database.ExecuteNonQuery("UPDATE s_calc_trf_lnk SET nzp_trfl = seqtrf.nextval");
                Database.ExecuteNonQuery("ALTER TABLE s_calc_trf_lnk MODIFY nzp_trfl SERIAL NOT NULL");
            }
            if (Database.ProviderName == "PostgreSQL")
            {
                Database.ExecuteNonQuery("ALTER TABLE s_calc_trf_lnk ALTER COLUMN nzp_trfl TYPE INTEGER");
                Database.ExecuteNonQuery("UPDATE s_calc_trf_lnk SET nzp_trfl = NEXTVAL('seqtrf')");
                Database.ExecuteNonQuery("ALTER TABLE s_calc_trf_lnk ALTER COLUMN nzp_trfl SET DEFAULT NEXTVAL('seqtrf')");
            }
            if (Database.SequenceExists(CurrentSchema, "seqtrf")) Database.RemoveSequence(CurrentSchema, "seqtrf");
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_calc_trf = new SchemaQualifiedObjectName() { Name = "s_calc_trf", Schema = CurrentSchema };
            SchemaQualifiedObjectName s_calc_trf_lnk = new SchemaQualifiedObjectName() { Name = "s_calc_trf_lnk", Schema = CurrentSchema };
            if (Database.ColumnExists(s_calc_trf, "nzp_area")) Database.RemoveColumn(s_calc_trf, "nzp_area");
            if (Database.ColumnExists(s_calc_trf_lnk, "nzp_trfl")) Database.RemoveColumn(s_calc_trf_lnk, "nzp_trfl");
            if (Database.SequenceExists(CurrentSchema, "seqtrf")) Database.RemoveSequence(CurrentSchema, "seqtrf");
        }
    }

    [Migration(20140305004, MigrateDataBase.Charge)]
    public class Migration_20140305004_Charge : Migration
    {
        public override void Apply()
        {
            List<SchemaQualifiedObjectName> lstTables = new List<SchemaQualifiedObjectName>();
            for (int i = 1; i <= 12; i++) lstTables.Add(string.Format("calc_gku_{0}", i.ToString("00")));
            lstTables.ForEach(Table =>
            {
                if (!Database.ColumnExists(Table, "dlt_reval"))
                    Database.AddColumn(Table, new Column("dlt_reval", DbType.Decimal.WithSize(14, 7), ColumnProperty.None, 0.0000000));
            });
        }

        public override void Revert()
        {
            List<SchemaQualifiedObjectName> lstTables = new List<SchemaQualifiedObjectName>();
            for (int i = 1; i <= 12; i++) lstTables.Add(string.Format("calc_gku_{0}", i.ToString("00")));
            lstTables.ForEach(Table =>
            {
                if (Database.ColumnExists(Table, "dlt_reval")) Database.RemoveColumn(Table, "dlt_reval");
            });
        }
    }

    [Migration(20140305004, MigrateDataBase.Fin)]
    public class Migration_20140305004_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName pu_vals = new SchemaQualifiedObjectName() { Name = "pu_vals", Schema = CurrentSchema };
            if (!Database.ColumnExists(pu_vals, "nzp_counter")) Database.AddColumn(pu_vals, new Column("nzp_counter", DbType.Int32));
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName pu_vals = new SchemaQualifiedObjectName() { Name = "pu_vals", Schema = CurrentSchema };
            if (Database.ColumnExists(pu_vals, "nzp_counter")) Database.RemoveColumn(pu_vals, "nzp_counter");
        }
    }
}
