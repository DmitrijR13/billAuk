﻿using System;
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
	[Migration(2016042606401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
	public class Migration_2016042606401_CentralBank : Migration
	{
		public override void Apply()
		{
			SetSchema(Bank.Kernel);
			var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

			if (Database.TableExists(prm_name))
			{
				if (NotExistRecord(prm_name, " WHERE nzp_prm=1700"))
				{
					Database.Insert(prm_name, new[] {"nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_"},
						new[] {"1700", "Количество знаков для округления расчета пени", "int", "10", "0", "10", "1"});
				}
				else
				{
					Database.Update(prm_name, new[] {"nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_"},
						new[] {"1700", "Количество знаков для округления расчета пени", "int", "10", "0", "10", "1" },
						"nzp_prm=1700");
				}
			}
		}

		private bool NotExistRecord(SchemaQualifiedObjectName table, string where)
		{
			return Convert.ToInt32(
				Database.ExecuteScalar("SELECT count(*) FROM " + GetFullTableName(table) + " " + where)) ==
			       0;
		}

		private string GetFullTableName(SchemaQualifiedObjectName table)
		{
			return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
		}
	}
}
