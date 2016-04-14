using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014081508401, MigrateDataBase.Fin)]
    public class Migration_2014081508401_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName reestr_perekidok = new SchemaQualifiedObjectName() { Name = "reestr_perekidok", Schema = CurrentSchema };
            if (!Database.TableExists(reestr_perekidok))
            {
                Database.AddTable(
                  reestr_perekidok,
                  new Column("nzp_reestr", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull), // Identity property is SERIAL and NOT NULL value default
                  new Column("dat_uchet", DbType.Date, ColumnProperty.NotNull),
                  new Column("comment",DbType.String.WithSize(250)),
                  new Column("sposob_raspr", DbType.Int32),
                  new Column("nzp_oper", DbType.Int32),
                  new Column("nzp_serv", DbType.Int32),
                  new Column("nzp_supp", DbType.Int32),
                  new Column("nzp_serv_on", DbType.Int32),
                  new Column("nzp_supp_on", DbType.Int32),
                  new Column("saldo_part", DbType.Int32),  
                  new Column("sum_oper", DbType.Decimal.WithSize(14, 2)), 
                  new Column("is_actual", DbType.Int32),
                  new Column("changed_by", DbType.Int32),
                  new Column("changed_on", DbType.Date, ColumnProperty.NotNull),
                  new Column("created_by", DbType.Int32),
                  new Column("created_on", DbType.Date, ColumnProperty.NotNull),
                  new Column("type_rcl", DbType.Int32),
                  new Column("nzp_doc_base", DbType.Int32)
                  );

                if (!Database.IndexExists("ix_reestr_perekidok1", reestr_perekidok))
                    Database.AddIndex("ix_reestr_perekidok1", false, reestr_perekidok, new string[] { "nzp_reestr" });
                if (!Database.ConstraintExists(reestr_perekidok, "pk_reestr_perekidok")) 
                    Database.AddPrimaryKey("pk_reestr_perekidok", reestr_perekidok, new string[] { "nzp_reestr" });
                SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CentralKernel };
                if (Database.ProviderName == "PostgreSQL")
                if (!Database.ConstraintExists(reestr_perekidok, "fk_reestr_perekidok_type_rcl")) 
                            Database.AddForeignKey("fk_reestr_perekidok_type_rcl", reestr_perekidok, "type_rcl", s_typercl, "type_rcl");
            
            }
        }
    }
}