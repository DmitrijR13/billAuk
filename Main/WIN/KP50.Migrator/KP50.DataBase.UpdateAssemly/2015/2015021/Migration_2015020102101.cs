using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015020102101, MigrateDataBase.CentralBank)]
    public class Migration_2015020102101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName fn_percent_dom = new SchemaQualifiedObjectName() { Name = "fn_percent_dom", Schema = CurrentSchema };

            if (!Database.ColumnExists(fn_percent_dom, "changed_by"))
            {
                Database.AddColumn(fn_percent_dom, new Column("changed_by", DbType.Int32));              
                Database.Update(fn_percent_dom, new string[] { "changed_by" }, new string[] { "1" });
                Database.ChangeColumn(fn_percent_dom, "changed_by", DbType.Int32, true);
                Database.AddIndex("IX_fn_percent_dom_changed_by", false, fn_percent_dom, "changed_by");
            }

            if (!Database.ColumnExists(fn_percent_dom, "changed_on"))
            {
                Database.AddColumn(fn_percent_dom, new Column("changed_on", DbType.DateTime));
                Database.Update(fn_percent_dom, new string[] { "changed_on" }, new string[] { "'" + DateTime.Now.ToString() + "'" });
                Database.ChangeColumn(fn_percent_dom, "changed_on", DbType.DateTime, true);
            }

            SchemaQualifiedObjectName s_data_operation = new SchemaQualifiedObjectName() { Name = "s_data_operation", Schema = CurrentSchema };
            if (!Database.TableExists(s_data_operation))
            {
                Database.AddTable(s_data_operation, 
                    new Column("nzp_data_operation", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Unique),
                    new Column("data_operation", DbType.String.WithSize(40), ColumnProperty.NotNull));
                Database.AddPrimaryKey("PK_data_operation", s_data_operation, "nzp_data_operation");

                Database.AddIndex("IX_s_data_operation_nzp_data_operation", true, s_data_operation, "nzp_data_operation");
                Database.Insert(s_data_operation, new string[] { "nzp_data_operation", "data_operation" }, new string[] { "1", "Добавление" });
                Database.Insert(s_data_operation, new string[] { "nzp_data_operation", "data_operation" }, new string[] { "2", "Изменение" });
                Database.Insert(s_data_operation, new string[] { "nzp_data_operation", "data_operation" }, new string[] { "3", "Удаление" });
            }

            SchemaQualifiedObjectName fn_percent_dom_log = new SchemaQualifiedObjectName() { Name = "fn_percent_dom_log", Schema = CurrentSchema };

            if (!Database.TableExists(fn_percent_dom_log))
            {
                Database.AddTable(fn_percent_dom_log,
                    new Column("nzp_fp_log", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull| ColumnProperty.Unique),
                    new Column("nzp_fp", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_data_operation", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_area", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32),
                    new Column("perc_ud", DbType.Decimal.WithSize(10,2), 0),
                    new Column("dat_s", DbType.Date),
                    new Column("dat_po", DbType.Date),
                    new Column("nzp_rs", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_bank", DbType.Int32),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("minpl", DbType.Decimal.WithSize(16,0)),
                    new Column("nzp_serv_from", DbType.Int32),
                    new Column("nzp_supp_snyat", DbType.Int32),
                    new Column("changed_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_on", DbType.DateTime, ColumnProperty.NotNull)
                );

                Database.AddPrimaryKey("PK_fn_percent_dom_log", fn_percent_dom_log, "nzp_fp_log");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_fp_log", true, fn_percent_dom_log, "nzp_fp_log");

                Database.AddIndex("IX_fn_percent_dom_log_nzp_fp", false, fn_percent_dom_log, "nzp_fp");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_data_operation", false, fn_percent_dom_log, "nzp_data_operation");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_payer", false, fn_percent_dom_log, "nzp_payer");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_supp", false, fn_percent_dom_log, "nzp_supp");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_serv", false, fn_percent_dom_log, "nzp_serv");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_bank", false, fn_percent_dom_log, "nzp_bank");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_dom", false, fn_percent_dom_log, "nzp_dom");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_serv_from", false, fn_percent_dom_log, "nzp_serv_from");
                Database.AddIndex("IX_fn_percent_dom_log_nzp_supp_snyat", false, fn_percent_dom_log, "nzp_supp_snyat");

                Database.AddIndex("IX_fn_percent_dom_log_changed_by", false, fn_percent_dom_log, "changed_by");
                Database.AddIndex("IX_fn_percent_dom_log_changed_on", false, fn_percent_dom_log, "changed_on");
            }

            if (Database.ProviderName != "PostgreSQL") return;

            string tablePrefix = CurrentPrefix + "_data";

            Database.ExecuteNonQuery("drop function if exists trg_ins_fn_percent_dom() CASCADE;");
            Database.ExecuteNonQuery(@"CREATE function trg_ins_fn_percent_dom() RETURNS trigger AS  
$trg_ins_fn_percent_dom$
BEGIN
  insert into " + tablePrefix + @".fn_percent_dom_log (nzp_data_operation, nzp_fp, nzp_payer, nzp_supp, nzp_serv, nzp_area, nzp_geu, perc_ud,
    dat_s, dat_po, nzp_rs, nzp_bank, nzp_dom, minpl, nzp_serv_from, nzp_supp_snyat, 
    changed_by, changed_on) 
  values
  (1, NEW.nzp_fp, NEW.nzp_payer, NEW.nzp_supp, NEW.nzp_serv, NEW.nzp_area, NEW.nzp_geu, NEW.perc_ud,
    NEW.dat_s, NEW.dat_po, NEW.nzp_rs, NEW.nzp_bank, NEW.nzp_dom, NEW.minpl, NEW.nzp_serv_from, NEW.nzp_supp_snyat, 
    NEW.changed_by, NEW.changed_on);  
  return NEW;
END;
$trg_ins_fn_percent_dom$
LANGUAGE  plpgsql;");

            Database.ExecuteNonQuery("drop function if exists trg_upd_fn_percent_dom() CASCADE;");
            Database.ExecuteNonQuery(@"CREATE function trg_upd_fn_percent_dom() RETURNS trigger AS 
$trg_upd_fn_percent_dom$
BEGIN
  insert into " + tablePrefix + @".fn_percent_dom_log (nzp_data_operation, nzp_fp, nzp_payer, nzp_supp, nzp_serv, nzp_area, nzp_geu, perc_ud,
    dat_s, dat_po, nzp_rs, nzp_bank, nzp_dom, minpl, nzp_serv_from, nzp_supp_snyat, 
    changed_by, changed_on) 
  values
  (2, NEW.nzp_fp, NEW.nzp_payer, NEW.nzp_supp, NEW.nzp_serv, NEW.nzp_area, NEW.nzp_geu, NEW.perc_ud,
    NEW.dat_s, NEW.dat_po, NEW.nzp_rs, NEW.nzp_bank, NEW.nzp_dom, NEW.minpl, NEW.nzp_serv_from, NEW.nzp_supp_snyat, 
    NEW.changed_by, NEW.changed_on);  

  return NEW;
END;
$trg_upd_fn_percent_dom$
LANGUAGE  plpgsql;");

            Database.ExecuteNonQuery("CREATE TRIGGER ins_fn_percent_dom BEFORE INSERT ON fn_percent_dom FOR EACH ROW EXECUTE PROCEDURE trg_ins_fn_percent_dom();");
            Database.ExecuteNonQuery("CREATE TRIGGER upd_fn_percent_dom BEFORE UPDATE ON fn_percent_dom FOR EACH ROW EXECUTE PROCEDURE trg_upd_fn_percent_dom();");
        }

        public override void Revert()
        {
            
        }
    }

   
}

