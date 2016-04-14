using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    
    [Migration(2015081706401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015081706401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName { Name = "prm_name", Schema = CurrentSchema };

            if (Database.TableExists(prm_name))
            {
                Database.ExecuteNonQuery(" DELETE FROM " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                                         " WHERE nzp_prm in (1464); ");

                Database.ExecuteNonQuery(" INSERT INTO " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                                         " (nzp_prm, name_prm, type_prm, nzp_res, prm_num, low_, high_, digits_) " +
                                         " VALUES (1464, 'Тип дома для ХВС на ОДН', 'sprav', 38, 2, Null, Null, Null); ");
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            
        }
    }
}
