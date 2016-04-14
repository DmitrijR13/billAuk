using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015100706402, Migrator.Framework.DataBase.LocalBank | Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015100706402 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName formuls = new SchemaQualifiedObjectName { Name = "formuls", Schema = CurrentSchema };
            if (!Database.TableExists(formuls))
            {
                return;
            }

            if (Database.TableExists(formuls))
            {
                Database.Update(formuls, new string[] { "name_frm" }, new string[] { "Управление жилым фондом (ЛС)" }, "nzp_frm = 500");
            }
           
        }
    }
}

