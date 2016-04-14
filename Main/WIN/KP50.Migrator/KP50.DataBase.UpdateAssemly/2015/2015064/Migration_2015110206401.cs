using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015110206401, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015110206401:Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_bank = new SchemaQualifiedObjectName { Name = "s_bank", Schema = CurrentSchema};
            if (!Database.TableExists(s_bank)) return;
            SchemaQualifiedObjectName pack_types = new SchemaQualifiedObjectName { Name = "pack_types", Schema = CurrentSchema};
            if (!Database.TableExists(pack_types)) return;
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_packsettings = new SchemaQualifiedObjectName { Name = "s_packsettings", Schema = CurrentSchema};
            if (Database.TableExists(s_packsettings)) return;

            Database.AddTable(s_packsettings, new Column("nzp_bank", DbType.Int32, ColumnProperty.NotNull),
                new Column("pack_type", DbType.Int32, ColumnProperty.NotNull));
            Database.AddForeignKey("FK_nzp_bank",s_packsettings,"nzp_bank",s_bank,"nzp_bank");
            Database.AddForeignKey("FK_pack_type", s_packsettings, "pack_type", pack_types, "id");
        }
    }
}
