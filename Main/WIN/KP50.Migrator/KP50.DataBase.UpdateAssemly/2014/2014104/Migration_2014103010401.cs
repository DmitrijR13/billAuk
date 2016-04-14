using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014103010401, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2014103010401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var tula_s_bank = new SchemaQualifiedObjectName() { Name = "tula_s_bank", Schema = CurrentSchema };
            if (Database.TableExists(tula_s_bank))
            {
                if (!Database.ColumnExists(tula_s_bank, "branch_id_reestr"))
                {
                    Database.AddColumn(tula_s_bank, new Column("branch_id_reestr", DbType.String.WithSize(5)));
                    Database.ExecuteNonQuery("UPDATE " + CentralData + Database.TableDelimiter + "tula_s_bank SET branch_id_reestr=branch_id");
                    Database.ExecuteNonQuery("UPDATE " + CentralData + Database.TableDelimiter + "tula_s_bank SET branch_id='V'||branch_id");
                    Database.ExecuteNonQuery("UPDATE " + CentralData + Database.TableDelimiter + "tula_s_bank SET branch_id_reestr='0'||branch_id_reestr");
                }
            }
            var tula_kvit_reestr = new SchemaQualifiedObjectName() { Name = "tula_kvit_reestr", Schema = CurrentSchema };
            if (Database.TableExists(tula_kvit_reestr))
            {
                if (!Database.ColumnExists(tula_kvit_reestr, "nzp_download"))
                {
                    Database.AddColumn(tula_kvit_reestr, new Column("nzp_download", DbType.Int32));
                }
            }
        }
    }
}
