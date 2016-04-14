using KP50.DataBase.Migrator.Framework;
using System.Collections.Generic;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305006, MigrateDataBase.CentralBank)]
    public class Migration_20140305006_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            SchemaQualifiedObjectName pack_types = new SchemaQualifiedObjectName() { Name = "pack_types", Schema = CurrentSchema };
            if (!Database.TableExists(pack_types))
            {
                Database.AddTable(pack_types,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("type_name", DbType.String.WithSize(20)));

                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("ALTER TABLE pack_types LOCK MODE (ROW)");
                Database.AddIndex("ix_pack_types_1", true, pack_types, "id");
                Database.Insert(pack_types, new string[] { "id", "type_name" }, new string[] { "10", "Оплаты на счет РЦ" });
                Database.Insert(pack_types, new string[] { "id", "type_name" }, new string[] { "20", "Оплаты УК и ПУ" });
                Database.Insert(pack_types, new string[] { "id", "type_name" }, new string[] { "21", "Переплаты" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName pack_types = new SchemaQualifiedObjectName() { Name = "pack_types", Schema = CurrentSchema };
            if (Database.TableExists(pack_types)) Database.RemoveTable(pack_types);
        }
    }

    [Migration(20140305006, MigrateDataBase.Fin)]
    public class Migration_20140305006_Fin : Migration
    {
        public override void Apply()
        {
            List<string> lstQuerys = new List<string>();
            if (!Database.ProcedureExists(CurrentSchema, "reparation_gil_sum"))
            {
                if (Database.ProviderName == "Informix")
                {
                    lstQuerys.Add(
                        "create procedure \"are\".reparation_gil_sum(pnzp_pack_ls integer,pnzp_serv integer,psum_prih decimal(14,2), pis_union integer) " +
                        "delete from t_gil_sums where 1=1; " +
                        "insert into  t_gil_sums (nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, isum_charge, sum_prih, sum_prih_u,koeff) " +
                        "select nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, sum_charge as isum_charge, psum_prih, 0.00 as sum_u, 0 as koeff " +
                        "from t_opl where nzp_pack_ls = pnzp_pack_ls  and nzp_serv = pnzp_serv; " +
                        "if pis_union=1 then " +
                        "insert into  t_gil_sums (nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, isum_charge, sum_prih, sum_prih_u,koeff ) " +
                        "select nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, sum_charge as isum_charge, psum_prih as sum_prih, 0.00 as sum_u, 0 as koeff  from t_opl " +
                        "where nzp_pack_ls = pnzp_pack_ls  and nzp_serv in (select nzp_serv_uni from service_union where nzp_serv_base = pnzp_serv) and nzp_serv <> pnzp_serv; " +
                        "end if; " +
                        "update t_gil_sums set isum_charge = (select sum(sum_charge) from t_gil_sums); " +
                        "update t_gil_sums set koeff = sum_charge/isum_charge; " +
                        "update t_gil_sums set sum_prih_u = koeff*sum_prih; " +
                        "update t_opl set sum_prih_u = (select a.sum_prih_u from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp) " +
                        "  where (select  count(*) from t_gil_sums b where b.nzp_pack_ls = t_opl.nzp_pack_ls and b.nzp_serv = t_opl.nzp_serv and b.nzp_supp = t_opl.nzp_supp)>0; " +
                        "update t_opl set sum_prih = sum_prih+sum_prih_u where nzp_pack_ls = pnzp_pack_ls and sum_prih_u <>0; " +
                        "END PROCEDURE; "
                        );
                    lstQuerys.Add("grant execute on procedure \"are\".reparation_gil_sum(integer,integer,decimal,integer) to public as are");
                }
                if (Database.ProviderName == "PostgreSQL")
                {
                    lstQuerys.Add(
                        "create function " + CurrentSchema + ".reparation_gil_sum(pnzp_pack_ls integer,pnzp_serv integer,psum_prih numeric(14,2), pis_union integer) returns INTEGER as $$ " +
                        "begin " +
                        "set search_path to " + CurrentSchema + "; " +
                        "delete from t_gil_sums where 1=1; " +
                        "insert into  t_gil_sums (nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, isum_charge, sum_prih, sum_prih_u,koeff) " +
                        "select nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, sum_charge as isum_charge, psum_prih, 0.00 as sum_u, 0 as koeff " +
                        "from t_opl where nzp_pack_ls = pnzp_pack_ls  and nzp_serv = pnzp_serv; " +
                        "if pis_union=1 then " +
                        "insert into  t_gil_sums (nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, isum_charge, sum_prih, sum_prih_u,koeff ) " +
                        "select nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, sum_charge as isum_charge, psum_prih as sum_prih, 0.00 as sum_u, 0 as koeff  from t_opl " +
                        "where nzp_pack_ls = pnzp_pack_ls  and nzp_serv in (select nzp_serv_uni from service_union where nzp_serv_base = pnzp_serv) and nzp_serv <> pnzp_serv; " +
                        "end if; " +
                        "update t_gil_sums set isum_charge = (select sum(sum_charge) from t_gil_sums); " +
                        "update t_gil_sums set koeff = sum_charge/isum_charge; " +
                        "update t_gil_sums set sum_prih_u = koeff*sum_prih; " + 
                        "update t_opl set sum_prih_u = (select a.sum_prih_u from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp) " +
                        "  where (select  count(*) from t_gil_sums b where b.nzp_pack_ls = t_opl.nzp_pack_ls and b.nzp_serv = t_opl.nzp_serv and b.nzp_supp = t_opl.nzp_supp)>0; " +
                        "update t_opl set sum_prih = sum_prih+sum_prih_u where nzp_pack_ls = pnzp_pack_ls and sum_prih_u <>0; " +
                        "END PROCEDURE; "
                        );

                    lstQuerys.Add("grant execute on function reparation_gil_sum(integer,integer,decimal,integer) to public");
                }
                lstQuerys.ForEach(Query => Database.ExecuteNonQuery(Query));
            }
        }
    }

    [Migration(20140305006, MigrateDataBase.Web)]
    public class Migration_20140305006_Web : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName excel_utility = new SchemaQualifiedObjectName() { Name = "excel_utility", Schema = CurrentSchema };
            if (!Database.ColumnExists(excel_utility, "file_extension")) Database.AddColumn(excel_utility, new Column("file_extension", DbType.StringFixedLength.WithSize(10)));
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName excel_utility = new SchemaQualifiedObjectName() { Name = "excel_utility", Schema = CurrentSchema };
            if (Database.ColumnExists(excel_utility, "file_extension")) Database.RemoveColumn(excel_utility, "file_extension");
        }
    }
}
