using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003305, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015032003305 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var tulaSBank = new SchemaQualifiedObjectName() { Name = "tula_s_bank", Schema = CurrentSchema };
            if (!Database.ColumnExists(tulaSBank, "download_format_number"))
            {
                Database.AddColumn(tulaSBank, new Column("download_format_number", DbType.Int32));
                Database.ExecuteNonQuery("UPDATE " + CentralData + Database.TableDelimiter + "tula_s_bank SET download_format_number=0");
            }
        }
    }
}
