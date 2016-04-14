using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015071006401, MigrateDataBase.Charge)]
    public class Migration_2015071006401_Charge : Migration
    {
        public override void Apply()
        {
            int i;
            List<SchemaQualifiedObjectName> lstFnSupplier = new List<SchemaQualifiedObjectName>();
            for (i = 1; i <= 12; i++) lstFnSupplier.Add(new SchemaQualifiedObjectName() { Name = string.Format("fn_supplier{0}", i.ToString("00")), Schema = CurrentPrefix + "_charge_15" });
            foreach (SchemaQualifiedObjectName fn_supplier in lstFnSupplier)
            {
                string fk = string.Format("fk_fn_supplier{0}_nzp_pack_ls", i.ToString("00"));
                if (Database.ConstraintExists(fn_supplier, fk)) Database.RemoveConstraint(fn_supplier, fk);
            }
            lstFnSupplier.Clear();

            //SchemaQualifiedObjectName from_supplier = new SchemaQualifiedObjectName() { Name = "from_supplier", Schema = CurrentPrefix + "_charge_15" };
            //SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CentralPrefix + "_fin_15" };
            //if (!Database.ConstraintExists(from_supplier, "fk_from_supplier_nzp_pack_ls")) Database.AddForeignKey("fk_from_supplier_nzp_pack_ls", from_supplier, "nzp_pack_ls", pack_ls, "nzp_pack_ls");

            //List<SchemaQualifiedObjectName> LstFnSupplier = new List<SchemaQualifiedObjectName>();
            //for (i = 1; i <= 12; i++) LstFnSupplier.Add(new SchemaQualifiedObjectName() { Name = string.Format("fn_supplier{0}", i.ToString("00")), Schema = CurrentPrefix + "_charge_15" });
            //foreach (SchemaQualifiedObjectName fn_supplier in LstFnSupplier)
            //{
            //    string fk = string.Format("fk_fn_supplier{0}_nzp_pack_ls", i.ToString("00"));
            //    if (!Database.ConstraintExists(fn_supplier, fk)) Database.AddForeignKey(fk, fn_supplier, "nzp_pack_ls", pack_ls, "nzp_pack_ls");
            //}
            //LstFnSupplier.Clear();

            int j;
            List<SchemaQualifiedObjectName> lstFnSupplier1 = new List<SchemaQualifiedObjectName>();
            for (j = 1; j <= 12; j++) lstFnSupplier1.Add(new SchemaQualifiedObjectName() { Name = string.Format("fn_supplier{0}", j.ToString("00")), Schema = CurrentPrefix + "_charge_14" });
            foreach (SchemaQualifiedObjectName fn_supplier in lstFnSupplier1)
            {
                string fk = string.Format("fk_fn_supplier{0}_nzp_pack_ls", j.ToString("00"));
                if (Database.ConstraintExists(fn_supplier, fk)) Database.RemoveConstraint(fn_supplier, fk);
            }
            lstFnSupplier1.Clear();

            //SchemaQualifiedObjectName from_supplier1 = new SchemaQualifiedObjectName() { Name = "from_supplier", Schema = CurrentPrefix + "_charge_14" };
            //SchemaQualifiedObjectName pack_ls1 = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CentralPrefix + "_fin_14" };
            //if (!Database.ConstraintExists(from_supplier1, "fk_from_supplier_nzp_pack_ls")) Database.AddForeignKey("fk_from_supplier_nzp_pack_ls", from_supplier1, "nzp_pack_ls", pack_ls1, "nzp_pack_ls");

            //List<SchemaQualifiedObjectName> LstFnSupplier1 = new List<SchemaQualifiedObjectName>();
            //for (j = 1; i <= 12; j++) LstFnSupplier1.Add(new SchemaQualifiedObjectName() { Name = string.Format("fn_supplier{0}", j.ToString("00")), Schema = CurrentPrefix + "_charge_14" });
            //foreach (SchemaQualifiedObjectName fn_supplier in LstFnSupplier1)
            //{
            //    string fk = string.Format("fk_fn_supplier{0}_nzp_pack_ls", j.ToString("00"));
            //    if (!Database.ConstraintExists(fn_supplier, fk)) Database.AddForeignKey(fk, fn_supplier, "nzp_pack_ls", pack_ls, "nzp_pack_ls");
            //}
            //LstFnSupplier1.Clear();
        }

        public override void Revert()
        {
            // TODO: Downgrade Charges

        }
    }
}
