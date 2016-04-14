using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{    
    [Migration(2014080608201, MigrateDataBase.Charge)]
    public class Migration_2014080608201_Charge : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName perekidka = new SchemaQualifiedObjectName() { Name = "perekidka", Schema = CurrentSchema };
            if (Database.TableExists(perekidka))
                Database.Update(perekidka, new string[] { "type_rcl" }, new string[] { "1" }, " type_rcl = 103");
        }
    }
}