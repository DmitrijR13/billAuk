using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014080708301, MigrateDataBase.CentralBank)]
    public class Migration_2014080708301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010137");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new[] { "nzp_prm", "name_prm", "old_field", "type_prm", "prm_num" }, new[] { "1010137", "Водозабор", "0", "'char'", "2" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010141");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new[] { "nzp_prm", "name_prm", "old_field", "type_prm", "prm_num" }, new[] { "1010141", "Котельная по горячей воде", "0", "'char'", "2" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010142");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new[] { "nzp_prm", "name_prm", "old_field", "type_prm", "prm_num" }, new[] { "1010142", "Котельная по отоплению", "0", "'char'", "2" });
            
        }
        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010137");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010141");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010142");
        }
    }

    [Migration(2014080708301, MigrateDataBase.LocalBank)]
    public class Migration_2014080708301_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010137");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new[] { "nzp_prm", "name_prm", "old_field", "type_prm", "prm_num" }, new[] { "1010137", "Водозабор", "0", "'char'", "2" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010141");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new[] { "nzp_prm", "name_prm", "old_field", "type_prm", "prm_num" }, new[] { "1010141", "Котельная по горячей воде", "0", "'char'", "2" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010142");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new[] { "nzp_prm", "name_prm", "old_field", "type_prm", "prm_num" }, new[] { "1010142", "Котельная по отоплению", "0", "'char'", "2" });
            
        }
        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010137");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010141");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1010142");
        }
    }
}
