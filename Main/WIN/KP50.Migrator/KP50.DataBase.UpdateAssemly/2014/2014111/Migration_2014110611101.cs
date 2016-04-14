using System.Globalization;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
	[Migration(2014110611101, MigrateDataBase.CentralBank)]
	public class Migration_2014110611101_CentralBank : Migration
	{
		public override void Apply() {
			SetSchema(Bank.Kernel);
			#region объявление таблиц
			var prmName = new SchemaQualifiedObjectName
			{
				Name = "prm_name",
				Schema = CurrentSchema
			};
			var resolution = new SchemaQualifiedObjectName
			{
				Name = "resolution",
				Schema = CurrentSchema
			};
			var resY = new SchemaQualifiedObjectName
			{
				Name = "res_y",
				Schema = CurrentSchema
			};
			var resX = new SchemaQualifiedObjectName
			{
				Name = "res_x",
				Schema = CurrentSchema
			};
			var resValues = new SchemaQualifiedObjectName
			{
				Name = "res_values",
				Schema = CurrentSchema
			};
			var sPoint = new SchemaQualifiedObjectName
			{
				Name = "s_point",
				Schema = CurrentSchema
			};
			#endregion
			if (Database.TableExists(prmName) &&
				Database.TableExists(resolution) &&
				Database.TableExists(resY) &&
				Database.TableExists(resX) &&
				Database.TableExists(resValues) &&
				Database.TableExists(sPoint))
			{
				int rowCount;
				var value = Database.ExecuteScalar(" SELECT COUNT(nzp_wp) FROM " + sPoint.Name + " WHERE bd_kernel = 'nftul' ");
				if (int.TryParse(value.ToString(), out rowCount) && rowCount > 0)
				{
					#region занесение параметров в таблицу prm_name
					Database.Delete(prmName, "nzp_prm IN (1401,1402,1403,1404,1405,1406,1407,1408,1409,1410,1411,1412)");
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1401", "Тип дома для СЗ-Содержание жилья               ", "sprav", "3101", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1402", "Тип дома для СЗ-Капитальный ремонт             ", "sprav", "3102", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1403", "Тип дома для СЗ-Электроотопление               ", "sprav", "3103", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1404", "Тип дома для СЗ-Электроэнергия                 ", "sprav", "3104", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1405", "Тип дома для СЗ-Газ                            ", "sprav", "3105", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1406", "Тип дома для СЗ-Тепловая энергия на нагрев воды", "sprav", "3106", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1407", "Тип дома для СЗ-Холодная вода                  ", "sprav", "3107", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1408", "Тип дома для СЗ-Горячая вода                   ", "sprav", "3108", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1409", "Тип дома для СЗ-Канализация                    ", "sprav", "3109", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1410", "Тип дома для СЗ-Отопление                      ", "sprav", "3110", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1411", "Тип дома для СЗ-Вывоз мусора                   ", "sprav", "3111", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1412", "Тип дома для СЗ-Уборка МОП                     ", "sprav", "3112", "2" });
					#endregion

					#region занесение параметров в таблицу resolution
					Database.Delete(resolution, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");

					for (int i = 3101; i <= 3112; i++)
					{
						Database.Insert(resolution, new[] { "nzp_res", "name_short", "name_res" }, new[] { i.ToString(CultureInfo.InvariantCulture), "ТТип" + i + "СЗтула", "Тип дома СЗ " + i + " для Тулы" });
					}
					#endregion

					#region занесение параметров в таблицу res_x
					Database.Delete(resX, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");

					for (int i = 3101; i <= 3112; i++)
					{
						Database.Insert(resX, new[] { "nzp_res", "nzp_x", "name_x" }, new[] { i.ToString(CultureInfo.InvariantCulture), "1", "-" });
					}
					#endregion

					#region занесение параметров в таблицу res_y
					Database.Delete(resY, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");

					#region Содержание жилья
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "1", "1.1. Ветхий жилой фонд" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "2", "2.1. Дома пониженной капитальности" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "3", "2.2. Дома пониженной капитальности ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "4", "3.1. Дома без лифта и мусоропр" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "5", "3.2. Дома без лифта и мусоропр. ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "6", "4.1. Дома с мусоропров." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "7", "4.2. Дома с мусоропров. ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "8", "5.1. Дома с лифтом и мусоропр.1эт." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "9", "5.2. Дома с лифтом и мусоропр.2эт." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "10", "5.3. Дома с лифтом и мусоропр.3эт.и выше" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "11", "5.4. Дома с лифтом и мусоропр.1эт. ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "12", "5.5. Дома с лифтом и мусоропр.2эт. ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "13", "5.6. Дома с лифтом и мусоропр.3эт. ком.кв. или общеж." });
					#endregion

					#region Капитальный ремонт
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "1", "2.1.-Капремонт в домах неполн.благоус.или неблаг." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "2", "2.2.-Капремонт общ,ком/кв.непол.благ.или неблаг" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "3", "3.1.-Капремонт в домах без лифта и мусорпр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "4", "3.2.-Капремонт общ,ком/кв без лифта и мусорпр" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "5", "4.1.-Капремонт в домах без лифта,но с мусоропр" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "6", "4.2.-Капремонт общ,ком/кв без лифта,но с мусороп." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "7", "5.1.-Капремонт в домах с лифтом и мусорпр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "8", "5.2. Капремонт общ,ком/кв с лифт.и мусорпр." });
					#endregion

					#region Электроотопление
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3103", "1", "1.1. Электроотопл. в домах до 1999-1этаж" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3103", "2", "2.1. Электроотопл. в домах до 1999-2этаж и выше" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3103", "3", "3.1. Электроотопл. в домах после 1999-1этаж" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3103", "4", "4.1. Электроотопл. в домах после 1999-2этаж и выше" });
					#endregion

					#region Электроэнергия
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "1", "1.1. Электроэнергия в домах с газ.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "2", "1.2. Электроэнергия (дневная зона) в домах с газ.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "3", "1.3. Электроэнергия (ночная зона) в домах с газ.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "4", "3.1. Электроэнергия в домах с эл.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "5", "3.2. Электроэнергия (дневная зона) в домах с эл.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "6", "3.3. Электроэнергия (ночная зона) в домах с эл.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "7", "7.1. Электроэн.в домах с газ.плит и электр.водонагр" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "8", "7.2. Электроэн.(дн.зона)в домах с газ.плит и электр.водонагр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "9", "7.3. Электроэн. (ноч.зона) в домах с газ.плит и электр.водонагр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "10", "8.1. Электроэн.в домах с эл.плит и электр.водонагр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "11", "8.2. Электроэн.(дн.зона) в домах с эл.плит и электр.водонагр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "12", "8.3. Электроэн.(ноч.зона) в домах с эл.плит и электр.водонагр." });

					#endregion

					#region Газ
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "1", "1. Потребление газа без приборов учета при наличии центрального отопления и ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "2", "2. Потребление газа без приборов учета при использовании газ.водонагревателя и отсутствия ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "3", "3. Потребление газа без приборов учета при отсутствии газ.водонагревателя и ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "4", "4. Потребление газа без приборов учета для отопления жил.помещений" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "5", "7.1. Потребление газа по приборам учета при наличии центрального отопления и ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "6", "7.2. Потребление газа по приборам учета при использовании газ.водонагревателя и отсутствия ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "7", "7.3. Потребление газа по приборам учета при отсутствии газ.водонагревателя и ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "8", "7.4. Потребление газа по приборам учета для отопления жил.помещений в гор.мест." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "9", "7.7. Потребление газа по приборам учета для газ.водонагревателя и отсутствием газ.плиты" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "10", "8.1. Потребление газа для педагогов по приборам учета." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "11", "8.2. Потребление газа для педагогов без приборов учета." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "12", "9. Потребление газа без приборов учета для газ.водонагревателя и отсутствием газ.плиты" });
					#endregion

					#region Тепловая энергия на подогрев холодной воды
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "1", "2. Тепл. энер. на нагрев воды с мойкой" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "2", "3. Тепл. энер. на нагрев воды с раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "3", "4. Тепл. энер. на нагрев воды с душем" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "4", "5. Тепл. энер. на нагрев воды с ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "5", "6. Тепл. энер. на нагрев воды с ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "6", "7. Тепл. энер. на нагрев воды с унит.и мойк." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "7", "8. Тепл. энер. на нагрев воды с унит.и рак." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "8", "9. Тепл. энер. на нагрев воды с унит.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "8", "10. Тепл. энер. на нагрев воды с унит.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "10", "11. Тепл. энер. на нагрев воды с унит.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "11", "12. Тепл. энер. на нагрев воды с мойк.и раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "12", "13. Тепл. энер. на нагрев воды с мойк.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "13", "14. Тепл. энер. на нагрев воды с мойк.и ванн. без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "14", "15. Тепл. энер. на нагрев воды с мойк.и ванн. с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "15", "16. Тепл. энер. на нагрев воды с раков.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "16", "17. Тепл. энер. на нагрев воды с раков.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "17", "18. Тепл. энер. на нагрев воды с раков.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "18", "19. Тепл. энер. на нагрев воды с унит., мойк., раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "19", "20. Тепл. энер. на нагрев воды с унит., мойк., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "10", "21. Тепл. энер. на нагрев воды с унит., мойк., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "21", "22. Тепл. энер. на нагрев воды с унит., мойк., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "22", "23. Тепл. энер. на нагрев воды с унит., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "23", "24. Тепл. энер. на нагрев воды с унит., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "24", "25. Тепл. энер. на нагрев воды с унит., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "25", "26. Тепл. энер. на нагрев воды с мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "26", "27. Тепл. энер. на нагрев воды с мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "27", "28. Тепл. энер. на нагрев воды с мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "28", "29. Тепл. энер. на нагрев воды с унит., мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "29", "30. Тепл. энер. на нагрев воды с унит., мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "30", "31. Тепл. энер. на нагрев воды с унит., мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "31", "32. Тепл. энер. на нагрев воды с унит., мойк., раков., душ., ванн.с душ." });
					#endregion

					#region Холодная вода
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "1", "1.1. Холодная вода из улич.водоразб.колонки" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "2", "1.2. Холодная вода из собств.водоразб.колонки" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "3", "1.3. Холодная вода с унитазом без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "4", "2.1. Холодная вода с мойкой без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "5", "2.2. Холодная вода с мойкой с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "6", "3.1. Холодная вода с раков. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "7", "3.2. Холодная вода с раков. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "8", "4.1. Холодная вода с душем без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "9", "4.2. Холодная вода с душем с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "10", "5.1. Холодная вода с ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "11", "5.2. Холодная вода с ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "12", "6.1. Холодная вода с ванн.с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "13", "6.2. Холодная вода с ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "14", "7.1. Холодная вода с унит.и мойк. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "15", "7.2. Холодная вода с унит.и мойк. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "16", "8.1. Холодная вода с унит.и рак. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "17", "8.2. Холодная вода с унит.и рак. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "18", "9.1. Холодная вода с унит.и душ. без гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "19", "9.2. Холодная вода с унит.и душ. с гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "20", "10.1. Холодная вода с унит.и ванн.без душ. без гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "21", "10.2. Холодная вода с унит.и ванн.без душ. с гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "22", "11.1. Холодная вода с унит.и ванн.с душ. без гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "23", "11.2. Холодная вода с унит.и ванн.с душ. с гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "24", "12.1. Холодная вода с мойк.и раков. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "25", "12.2. Холодная вода с мойк.и раков. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "26", "13.1. Холодная вода с мойк.и душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "27", "13.2. Холодная вода с мойк.и душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "28", "14.1. Холодная вода с мойк.и ванн. без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "29", "14.2. Холодная вода с мойк.и ванн. без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "30", "15.1. Холодная вода с мойк.и ванн. с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "31", "15.2. Холодная вода с мойк.и ванн. с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "32", "16.1. Холодная вода с раков.и душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "33", "16.2. Холодная вода с раков.и душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "34", "17.1. Холодная вода с раков.и ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "35", "17.2. Холодная вода с раков.и ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "36", "18.1. Холодная вода с раков.и ванн.с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "37", "18.2. Холодная вода с раков.и ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "38", "19.1. Холодная вода с унит., мойк., раков. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "39", "19.2. Холодная вода с унит., мойк., раков. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "40", "20.1. Холодная вода с унит., мойк., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "41", "20.2. Холодная вода с унит., мойк., душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "42", "21.1. Холодная вода с унит., мойк., ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "43", "21.2. Холодная вода с унит., мойк., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "44", "22.1. Холодная вода с унит., мойк., ванн.с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "45", "22.2. Холодная вода с унит., мойк., ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "46", "23.1. Холодная вода с унит., раков., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "47", "23.2. Холодная вода с унит., раков., душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "48", "24.1. Холодная вода с унит., раков., ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "49", "24.2. Холодная вода с унит., раков., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "50", "24.2. Холодная вода с унит., раков., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "51", "25.2. Холодная вода с унит., раков., ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "52", "26.1. Холодная вода с мойк., раков., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "53", "26.2. Холодная вода с мойк., раков., душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "54", "27.1. Холодная вода с мойк., раков., ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "55", "27.2. Холодная вода с мойк., раков., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "56", "28.1. Холодная вода с мойк., раков., ванн.с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "57", "28.2. Холодная вода с мойк., раков., ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "58", "29.1. Холодная вода с унит., мойк., раков., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "59", "29.2. Холодная вода с унит., мойк., раков., душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "60", "30.1. Холодная вода с унит., мойк., раков., ванн. без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "61", "30.2. Холодная вода с унит., мойк., раков., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "62", "31.1. Холодная вода с унит., мойк., раков., ванн. с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "63", "31.2. Холодная вода с унит., мойк., раков., ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "64", "32.1. Холодная вода с унит., мойк., раков., душ., ванн. с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "65", "32.2. Холодная вода с унит., мойк., раков., душ., ванн.с душ. с гор.вод." });
					#endregion

					#region Горячая вода
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "1", "2. Горячая вода с мойкой" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "2", "3. Горячая вода с раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "3", "4. Горячая вода с душем" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "4", "5. Горячая вода с ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "5", "6. Горячая вода с ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "6", "7. Горячая вода с унит.и мойк." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "7", "8. Горячая вода с унит.и рак." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "8", "9. Горячая вода с унит.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "9", "10. Горячая вода с унит.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "10", "11. Горячая вода с унит.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "11", "12. Горячая вода с мойк.и раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "12", "13. Горячая вода с мойк.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "13", "14. Горячая вода с мойк.и ванн. без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "14", "15. Горячая вода с мойк.и ванн. с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "15", "16. Горячая вода с раков.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "16", "17. Горячая вода с раков.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "17", "18. Горячая вода с раков.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "18", "19. Горячая вода с унит., мойк., раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "19", "20. Горячая вода с унит., мойк., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "20", "21. Горячая вода с унит., мойк., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "21", "22. Горячая вода с унит., мойк., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "22", "23. Горячая вода с унит., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "23", "24. Горячая вода с унит., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "24", "25. Горячая вода с унит., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "25", "26. Горячая вода с мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "26", "27. Горячая вода с мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "27", "28. Горячая вода с мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "28", "29. Горячая вода с унит., мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "29", "30. Горячая вода с унит., мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "30", "31. Горячая вода с унит., мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "31", "32. Горячая вода с унит., мойк., раков., душ., ванн.с душ." });
					#endregion

					#region Водоотведение
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "1", "1. Водоотведение с унитазом" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "2", "2. Водоотведение с мойкой" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "3", "3. Водоотведение с раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "4", "4. Водоотведение с душем" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "5", "5. Водоотведение с ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "6", "6. Водоотведение с ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "7", "7. Водоотведение с унит.и мойк." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "8", "8. Водоотведение с унит.и рак." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "9", "9. Водоотведение с унит.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "10", "10. Водоотведение с унит.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "11", "11. Водоотведение с унит.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "12", "12. Водоотведение с мойк.и раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "13", "13. Водоотведение с мойк.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "14", "14. Водоотведение с мойк.и ванн. без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "15", "15. Водоотведение с мойк.и ванн. с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "16", "16. Водоотведение с раков.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "17", "17. Водоотведение с раков.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "18", "18. Водоотведение с раков.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "19", "19. Водоотведение с унит., мойк., раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "20", "20. Водоотведение с унит., мойк., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "21", "21. Водоотведение с унит., мойк., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "22", "22. Водоотведение с унит., мойк., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "23", "23. Водоотведение с унит., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "24", "24. Водоотведение с унит., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "25", "25. Водоотведение с унит., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "26", "26. Водоотведение с мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "27", "27. Водоотведение с мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "28", "28. Водоотведение с мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "29", "29. Водоотведение с унит., мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "30", "30. Водоотведение с унит., мойк., раков., ванн. без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "31", "31. Водоотведение с унит., мойк., раков., ванн. с душ" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "32", "32. Водоотведение с унит., мойк., раков., душ., ванн. с душ." });
					#endregion

					#region Отопление
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "1", "3.1. Отопл. с опл. в отопит. период1эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "2", "3.2. Отопл. с опл. в отопит. период2эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "3", "3.3. Отопл. с опл. в отопит. период3-4эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "4", "3.4. Отопл. с опл. в отопит. период5-9эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "5", "3.5. Отопл. с опл. в отопит. период10эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "6", "3.6. Отопл. с опл. в отопит. период12эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "7", "3.7. Отопл. с опл. в отопит. период13эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "8", "3.8. Отопл. с опл. в отопит. период14эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "9", "3.9. Отопл. с опл. в отопит. период16эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "10", "3.10. Отопл. с опл. в отопит. период1эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "11", "3.11. Отопл. с опл. в отопит. период2эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "12", "3.12. Отопл. с опл. в отопит. период3эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "13", "3.13. Отопл. с опл. в отопит. период4-5эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "14", "3.14. Отопл. с опл. в отопит. период6-7эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "15", "3.15. Отопл. с опл. в отопит. период8эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "16", "3.16. Отопл. с опл. в отопит. период9эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "17", "3.17. Отопл. с опл. в отопит. период10эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "18", "3.18. Отопл. с опл. в отопит. период11эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "19", "3.19. Отопл. с опл. в отопит. период12эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "20", "4.1. Отопл. с круглогод.опл. 1эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "21", "4.2. Отопл. с круглогод.опл. 2эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "22", "4.3. Отопл. с круглогод.опл. 4-4эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "23", "4.4. Отопл. с круглогод.опл. 5-9эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "24", "4.5. Отопл. с круглогод.опл. 10эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "25", "4.6. Отопл. с круглогод.опл. 12эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "26", "4.7. Отопл. с круглогод.опл. 14эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "27", "4.8. Отопл. с круглогод.опл. 14эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "28", "4.9. Отопл. с круглогод.опл. 16эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "29", "4.10. Отопл. с круглогод.опл. 1эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "30", "4.11. Отопл. с круглогод.опл. 2эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "31", "4.12. Отопл. с круглогод.опл. 4эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "32", "4.13. Отопл. с круглогод.опл. 4-5эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "33", "4.14. Отопл. с круглогод.опл. 6-7эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "34", "4.15. Отопл. с круглогод.опл. 8эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "35", "4.16. Отопл. с круглогод.опл. 9эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "36", "4.17. Отопл. с круглогод.опл. 10эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "37", "4.18. Отопл. с круглогод.опл. 11эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "38", "4.19. Отопл. с круглогод.опл. 12эт.дом после 1999" });
					#endregion

					#region Вывоз мусора
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "1", "1.1.Вывоз мусора из БЖФ без КГО" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "2", "2.1.Вывоз мусора из ЧС без КГО" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "3", "3.1. Вывоз мусора из БЖФ с КГО" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "4", "4.1. Вывоз мусора из ЧС с КГО" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "5", "5.1. Вывоз мусора с площади (за ед.измерения)" });
					#endregion

					#region Уборка МОП
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3112", "1", "Уборака МОП с чел." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3112", "2", "Уборка МОП за ед.изм." });
					#endregion

					#endregion

					#region занесение параметров в таблицу res_values
					Database.Delete(resValues, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");

					for (int i = 1; i <= 13; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3101", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 8; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3102", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 4; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3103", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 12; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3104", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 12; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3105", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 31; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3106", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 65; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3107", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}


					for (int i = 1; i <= 32; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3108", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 32; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3109", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 38; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3110", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "1", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "2", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "3", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "4", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "5", "1", " " });

					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3112", "1", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3112", "2", "1", " " });

					#endregion
				}
			}
		}

		public override void Revert() {
			SetSchema(Bank.Kernel);

			#region объявление таблиц
			var prmName = new SchemaQualifiedObjectName
			{
				Name = "prm_name",
				Schema = CurrentSchema
			};
			var resolution = new SchemaQualifiedObjectName
			{
				Name = "resolution",
				Schema = CurrentSchema
			};
			var resY = new SchemaQualifiedObjectName
			{
				Name = "res_y",
				Schema = CurrentSchema
			};
			var resX = new SchemaQualifiedObjectName
			{
				Name = "res_x",
				Schema = CurrentSchema
			};
			var resValues = new SchemaQualifiedObjectName
			{
				Name = "res_values",
				Schema = CurrentSchema
			};
			var sPoint = new SchemaQualifiedObjectName
			{
				Name = "s_point",
				Schema = CurrentSchema
			};
			#endregion

			if (Database.TableExists(prmName) &&
				Database.TableExists(resolution) &&
				Database.TableExists(resY) &&
				Database.TableExists(resX) &&
				Database.TableExists(resValues) &&
				Database.TableExists(sPoint))
			{
				int rowCount;
				var value = Database.ExecuteScalar(" SELECT COUNT(nzp_wp) FROM " + sPoint.Name + " WHERE bd_kernel = 'nftul' ");
				if (int.TryParse(value.ToString(), out rowCount) && rowCount > 0)
				{
					Database.Delete(prmName, "nzp_prm IN (1401,1402,1403,1404,1405,1406,1407,1408,1409,1410,1411,1412)");
					Database.Delete(resolution, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");
					Database.Delete(resX, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");
					Database.Delete(resY, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");
					Database.Delete(resValues, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");
				}

			}
		}
	}

	[Migration(2014110611101, MigrateDataBase.LocalBank)]
	public class Migration_2014110611101_LocalBank : Migration
	{
		public override void Apply() {
			SetSchema(Bank.Kernel);
			#region объявление таблиц
			var prmName = new SchemaQualifiedObjectName
			{
				Name = "prm_name",
				Schema = CurrentSchema
			};
			var resolution = new SchemaQualifiedObjectName
			{
				Name = "resolution",
				Schema = CurrentSchema
			};
			var resY = new SchemaQualifiedObjectName
			{
				Name = "res_y",
				Schema = CurrentSchema
			};
			var resX = new SchemaQualifiedObjectName
			{
				Name = "res_x",
				Schema = CurrentSchema
			};
			var resValues = new SchemaQualifiedObjectName
			{
				Name = "res_values",
				Schema = CurrentSchema
			};
			var sPoint = new SchemaQualifiedObjectName
			{

				Name = "s_point",
				Schema = CentralKernel
			};
			#endregion
			if (Database.TableExists(prmName) &&
				Database.TableExists(resolution) &&
				Database.TableExists(resY) &&
				Database.TableExists(resX) &&
				Database.TableExists(resValues) &&
				Database.TableExists(sPoint))
			{
				int rowCount;
				var value = Database.ExecuteScalar(" SELECT COUNT(nzp_wp) " +
												   " FROM " + CentralKernel + Database.TableDelimiter + sPoint.Name + " WHERE bd_kernel = 'nftul' ");
				if (int.TryParse(value.ToString(), out rowCount) && rowCount > 0)
				{
					#region занесение параметров в таблицу prm_name
					Database.Delete(prmName, "nzp_prm IN (1401,1402,1403,1404,1405,1406,1407,1408,1409,1410,1411,1412)");
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1401", "Тип дома для СЗ-Содержание жилья               ", "sprav", "3101", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1402", "Тип дома для СЗ-Капитальный ремонт             ", "sprav", "3102", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1403", "Тип дома для СЗ-Электроотопление               ", "sprav", "3103", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1404", "Тип дома для СЗ-Электроэнергия                 ", "sprav", "3104", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1405", "Тип дома для СЗ-Газ                            ", "sprav", "3105", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1406", "Тип дома для СЗ-Тепловая энергия на нагрев воды", "sprav", "3106", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1407", "Тип дома для СЗ-Холодная вода                  ", "sprav", "3107", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1408", "Тип дома для СЗ-Горячая вода                   ", "sprav", "3108", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1409", "Тип дома для СЗ-Канализация                    ", "sprav", "3109", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1410", "Тип дома для СЗ-Отопление                      ", "sprav", "3110", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1411", "Тип дома для СЗ-Вывоз мусора                   ", "sprav", "3111", "2" });
					Database.Insert(prmName,
						new[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
						new[] { "1412", "Тип дома для СЗ-Уборка МОП                     ", "sprav", "3112", "2" });
					#endregion

					#region занесение параметров в таблицу resolution
					Database.Delete(resolution, " nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");

					for (int i = 3101; i <= 3112; i++)
					{
						Database.Insert(resolution, new[] { "nzp_res", "name_short", "name_res" }, new[] { i.ToString(CultureInfo.InvariantCulture), "ТТип" + i + "СЗтула", "Тип дома СЗ " + i + " для Тулы" });
					}
					#endregion

					#region занесение параметров в таблицу res_x
					Database.Delete(resX, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");

					for (int i = 3101; i <= 3112; i++)
					{
						Database.Insert(resX, new[] { "nzp_res", "nzp_x", "name_x" }, new[] { i.ToString(CultureInfo.InvariantCulture), "1", "-" });
					}
					#endregion

					#region занесение параметров в таблицу res_y
					Database.Delete(resY, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");

					#region Содержание жилья
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "1", "1.1. Ветхий жилой фонд" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "2", "2.1. Дома пониженной капитальности" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "3", "2.2. Дома пониженной капитальности ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "4", "3.1. Дома без лифта и мусоропр" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "5", "3.2. Дома без лифта и мусоропр. ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "6", "4.1. Дома с мусоропров." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "7", "4.2. Дома с мусоропров. ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "8", "5.1. Дома с лифтом и мусоропр.1эт." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "9", "5.2. Дома с лифтом и мусоропр.2эт." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "10", "5.3. Дома с лифтом и мусоропр.3эт.и выше" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "11", "5.4. Дома с лифтом и мусоропр.1эт. ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "12", "5.5. Дома с лифтом и мусоропр.2эт. ком.кв. или общеж." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3101", "13", "5.6. Дома с лифтом и мусоропр.3эт. ком.кв. или общеж." });
					#endregion

					#region Капитальный ремонт
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "1", "2.1.-Капремонт в домах неполн.благоус.или неблаг." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "2", "2.2.-Капремонт общ,ком/кв.непол.благ.или неблаг" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "3", "3.1.-Капремонт в домах без лифта и мусорпр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "4", "3.2.-Капремонт общ,ком/кв без лифта и мусорпр" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "5", "4.1.-Капремонт в домах без лифта,но с мусоропр" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "6", "4.2.-Капремонт общ,ком/кв без лифта,но с мусороп." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "7", "5.1.-Капремонт в домах с лифтом и мусорпр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3102", "8", "5.2. Капремонт общ,ком/кв с лифт.и мусорпр." });
					#endregion

					#region Электроотопление
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3103", "1", "1.1. Электроотопл. в домах до 1999-1этаж" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3103", "2", "2.1. Электроотопл. в домах до 1999-2этаж и выше" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3103", "3", "3.1. Электроотопл. в домах после 1999-1этаж" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3103", "4", "4.1. Электроотопл. в домах после 1999-2этаж и выше" });
					#endregion

					#region Электроэнергия
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "1", "1.1. Электроэнергия в домах с газ.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "2", "1.2. Электроэнергия (дневная зона) в домах с газ.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "3", "1.3. Электроэнергия (ночная зона) в домах с газ.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "4", "3.1. Электроэнергия в домах с эл.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "5", "3.2. Электроэнергия (дневная зона) в домах с эл.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "6", "3.3. Электроэнергия (ночная зона) в домах с эл.пл." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "7", "7.1. Электроэн.в домах с газ.плит и электр.водонагр" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "8", "7.2. Электроэн.(дн.зона)в домах с газ.плит и электр.водонагр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "9", "7.3. Электроэн. (ноч.зона) в домах с газ.плит и электр.водонагр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "10", "8.1. Электроэн.в домах с эл.плит и электр.водонагр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "11", "8.2. Электроэн.(дн.зона) в домах с эл.плит и электр.водонагр." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3104", "12", "8.3. Электроэн.(ноч.зона) в домах с эл.плит и электр.водонагр." });

					#endregion

					#region Газ
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "1", "1. Потребление газа без приборов учета при наличии центрального отопления и ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "2", "2. Потребление газа без приборов учета при использовании газ.водонагревателя и отсутствия ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "3", "3. Потребление газа без приборов учета при отсутствии газ.водонагревателя и ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "4", "4. Потребление газа без приборов учета для отопления жил.помещений" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "5", "7.1. Потребление газа по приборам учета при наличии центрального отопления и ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "6", "7.2. Потребление газа по приборам учета при использовании газ.водонагревателя и отсутствия ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "7", "7.3. Потребление газа по приборам учета при отсутствии газ.водонагревателя и ГВС" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "8", "7.4. Потребление газа по приборам учета для отопления жил.помещений в гор.мест." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "9", "7.7. Потребление газа по приборам учета для газ.водонагревателя и отсутствием газ.плиты" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "10", "8.1. Потребление газа для педагогов по приборам учета." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "11", "8.2. Потребление газа для педагогов без приборов учета." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3105", "12", "9. Потребление газа без приборов учета для газ.водонагревателя и отсутствием газ.плиты" });
					#endregion

					#region Тепловая энергия на подогрев холодной воды
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "1", "2. Тепл. энер. на нагрев воды с мойкой" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "2", "3. Тепл. энер. на нагрев воды с раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "3", "4. Тепл. энер. на нагрев воды с душем" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "4", "5. Тепл. энер. на нагрев воды с ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "5", "6. Тепл. энер. на нагрев воды с ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "6", "7. Тепл. энер. на нагрев воды с унит.и мойк." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "7", "8. Тепл. энер. на нагрев воды с унит.и рак." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "8", "9. Тепл. энер. на нагрев воды с унит.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "8", "10. Тепл. энер. на нагрев воды с унит.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "10", "11. Тепл. энер. на нагрев воды с унит.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "11", "12. Тепл. энер. на нагрев воды с мойк.и раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "12", "13. Тепл. энер. на нагрев воды с мойк.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "13", "14. Тепл. энер. на нагрев воды с мойк.и ванн. без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "14", "15. Тепл. энер. на нагрев воды с мойк.и ванн. с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "15", "16. Тепл. энер. на нагрев воды с раков.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "16", "17. Тепл. энер. на нагрев воды с раков.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "17", "18. Тепл. энер. на нагрев воды с раков.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "18", "19. Тепл. энер. на нагрев воды с унит., мойк., раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "19", "20. Тепл. энер. на нагрев воды с унит., мойк., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "10", "21. Тепл. энер. на нагрев воды с унит., мойк., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "21", "22. Тепл. энер. на нагрев воды с унит., мойк., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "22", "23. Тепл. энер. на нагрев воды с унит., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "23", "24. Тепл. энер. на нагрев воды с унит., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "24", "25. Тепл. энер. на нагрев воды с унит., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "25", "26. Тепл. энер. на нагрев воды с мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "26", "27. Тепл. энер. на нагрев воды с мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "27", "28. Тепл. энер. на нагрев воды с мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "28", "29. Тепл. энер. на нагрев воды с унит., мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "29", "30. Тепл. энер. на нагрев воды с унит., мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "30", "31. Тепл. энер. на нагрев воды с унит., мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3106", "31", "32. Тепл. энер. на нагрев воды с унит., мойк., раков., душ., ванн.с душ." });
					#endregion

					#region Холодная вода
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "1", "1.1. Холодная вода из улич.водоразб.колонки" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "2", "1.2. Холодная вода из собств.водоразб.колонки" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "3", "1.3. Холодная вода с унитазом без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "4", "2.1. Холодная вода с мойкой без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "5", "2.2. Холодная вода с мойкой с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "6", "3.1. Холодная вода с раков. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "7", "3.2. Холодная вода с раков. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "8", "4.1. Холодная вода с душем без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "9", "4.2. Холодная вода с душем с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "10", "5.1. Холодная вода с ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "11", "5.2. Холодная вода с ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "12", "6.1. Холодная вода с ванн.с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "13", "6.2. Холодная вода с ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "14", "7.1. Холодная вода с унит.и мойк. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "15", "7.2. Холодная вода с унит.и мойк. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "16", "8.1. Холодная вода с унит.и рак. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "17", "8.2. Холодная вода с унит.и рак. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "18", "9.1. Холодная вода с унит.и душ. без гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "19", "9.2. Холодная вода с унит.и душ. с гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "20", "10.1. Холодная вода с унит.и ванн.без душ. без гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "21", "10.2. Холодная вода с унит.и ванн.без душ. с гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "22", "11.1. Холодная вода с унит.и ванн.с душ. без гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "23", "11.2. Холодная вода с унит.и ванн.с душ. с гор.вод" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "24", "12.1. Холодная вода с мойк.и раков. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "25", "12.2. Холодная вода с мойк.и раков. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "26", "13.1. Холодная вода с мойк.и душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "27", "13.2. Холодная вода с мойк.и душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "28", "14.1. Холодная вода с мойк.и ванн. без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "29", "14.2. Холодная вода с мойк.и ванн. без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "30", "15.1. Холодная вода с мойк.и ванн. с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "31", "15.2. Холодная вода с мойк.и ванн. с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "32", "16.1. Холодная вода с раков.и душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "33", "16.2. Холодная вода с раков.и душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "34", "17.1. Холодная вода с раков.и ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "35", "17.2. Холодная вода с раков.и ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "36", "18.1. Холодная вода с раков.и ванн.с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "37", "18.2. Холодная вода с раков.и ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "38", "19.1. Холодная вода с унит., мойк., раков. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "39", "19.2. Холодная вода с унит., мойк., раков. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "40", "20.1. Холодная вода с унит., мойк., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "41", "20.2. Холодная вода с унит., мойк., душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "42", "21.1. Холодная вода с унит., мойк., ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "43", "21.2. Холодная вода с унит., мойк., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "44", "22.1. Холодная вода с унит., мойк., ванн.с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "45", "22.2. Холодная вода с унит., мойк., ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "46", "23.1. Холодная вода с унит., раков., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "47", "23.2. Холодная вода с унит., раков., душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "48", "24.1. Холодная вода с унит., раков., ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "49", "24.2. Холодная вода с унит., раков., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "50", "24.2. Холодная вода с унит., раков., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "51", "25.2. Холодная вода с унит., раков., ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "52", "26.1. Холодная вода с мойк., раков., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "53", "26.2. Холодная вода с мойк., раков., душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "54", "27.1. Холодная вода с мойк., раков., ванн.без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "55", "27.2. Холодная вода с мойк., раков., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "56", "28.1. Холодная вода с мойк., раков., ванн.с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "57", "28.2. Холодная вода с мойк., раков., ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "58", "29.1. Холодная вода с унит., мойк., раков., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "59", "29.2. Холодная вода с унит., мойк., раков., душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "60", "30.1. Холодная вода с унит., мойк., раков., ванн. без душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "61", "30.2. Холодная вода с унит., мойк., раков., ванн.без душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "62", "31.1. Холодная вода с унит., мойк., раков., ванн. с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "63", "31.2. Холодная вода с унит., мойк., раков., ванн.с душ. с гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "64", "32.1. Холодная вода с унит., мойк., раков., душ., ванн. с душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3107", "65", "32.2. Холодная вода с унит., мойк., раков., душ., ванн.с душ. с гор.вод." });
					#endregion

					#region Горячая вода
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "1", "2. Горячая вода с мойкой" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "2", "3. Горячая вода с раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "3", "4. Горячая вода с душем" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "4", "5. Горячая вода с ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "5", "6. Горячая вода с ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "6", "7. Горячая вода с унит.и мойк." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "7", "8. Горячая вода с унит.и рак." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "8", "9. Горячая вода с унит.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "9", "10. Горячая вода с унит.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "10", "11. Горячая вода с унит.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "11", "12. Горячая вода с мойк.и раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "12", "13. Горячая вода с мойк.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "13", "14. Горячая вода с мойк.и ванн. без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "14", "15. Горячая вода с мойк.и ванн. с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "15", "16. Горячая вода с раков.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "16", "17. Горячая вода с раков.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "17", "18. Горячая вода с раков.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "18", "19. Горячая вода с унит., мойк., раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "19", "20. Горячая вода с унит., мойк., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "20", "21. Горячая вода с унит., мойк., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "21", "22. Горячая вода с унит., мойк., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "22", "23. Горячая вода с унит., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "23", "24. Горячая вода с унит., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "24", "25. Горячая вода с унит., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "25", "26. Горячая вода с мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "26", "27. Горячая вода с мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "27", "28. Горячая вода с мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "28", "29. Горячая вода с унит., мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "29", "30. Горячая вода с унит., мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "30", "31. Горячая вода с унит., мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3108", "31", "32. Горячая вода с унит., мойк., раков., душ., ванн.с душ." });
					#endregion

					#region Водоотведение
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "1", "1. Водоотведение с унитазом" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "2", "2. Водоотведение с мойкой" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "3", "3. Водоотведение с раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "4", "4. Водоотведение с душем" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "5", "5. Водоотведение с ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "6", "6. Водоотведение с ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "7", "7. Водоотведение с унит.и мойк." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "8", "8. Водоотведение с унит.и рак." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "9", "9. Водоотведение с унит.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "10", "10. Водоотведение с унит.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "11", "11. Водоотведение с унит.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "12", "12. Водоотведение с мойк.и раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "13", "13. Водоотведение с мойк.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "14", "14. Водоотведение с мойк.и ванн. без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "15", "15. Водоотведение с мойк.и ванн. с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "16", "16. Водоотведение с раков.и душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "17", "17. Водоотведение с раков.и ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "18", "18. Водоотведение с раков.и ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "19", "19. Водоотведение с унит., мойк., раков." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "20", "20. Водоотведение с унит., мойк., душ. без гор.вод." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "21", "21. Водоотведение с унит., мойк., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "22", "22. Водоотведение с унит., мойк., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "23", "23. Водоотведение с унит., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "24", "24. Водоотведение с унит., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "25", "25. Водоотведение с унит., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "26", "26. Водоотведение с мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "27", "27. Водоотведение с мойк., раков., ванн.без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "28", "28. Водоотведение с мойк., раков., ванн.с душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "29", "29. Водоотведение с унит., мойк., раков., душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "30", "30. Водоотведение с унит., мойк., раков., ванн. без душ." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "31", "31. Водоотведение с унит., мойк., раков., ванн. с душ" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3109", "32", "32. Водоотведение с унит., мойк., раков., душ., ванн. с душ." });
					#endregion

					#region Отопление
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "1", "3.1. Отопл. с опл. в отопит. период1эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "2", "3.2. Отопл. с опл. в отопит. период2эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "3", "3.3. Отопл. с опл. в отопит. период3-4эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "4", "3.4. Отопл. с опл. в отопит. период5-9эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "5", "3.5. Отопл. с опл. в отопит. период10эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "6", "3.6. Отопл. с опл. в отопит. период12эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "7", "3.7. Отопл. с опл. в отопит. период13эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "8", "3.8. Отопл. с опл. в отопит. период14эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "9", "3.9. Отопл. с опл. в отопит. период16эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "10", "3.10. Отопл. с опл. в отопит. период1эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "11", "3.11. Отопл. с опл. в отопит. период2эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "12", "3.12. Отопл. с опл. в отопит. период3эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "13", "3.13. Отопл. с опл. в отопит. период4-5эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "14", "3.14. Отопл. с опл. в отопит. период6-7эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "15", "3.15. Отопл. с опл. в отопит. период8эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "16", "3.16. Отопл. с опл. в отопит. период9эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "17", "3.17. Отопл. с опл. в отопит. период10эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "18", "3.18. Отопл. с опл. в отопит. период11эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "19", "3.19. Отопл. с опл. в отопит. период12эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "20", "4.1. Отопл. с круглогод.опл. 1эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "21", "4.2. Отопл. с круглогод.опл. 2эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "22", "4.3. Отопл. с круглогод.опл. 4-4эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "23", "4.4. Отопл. с круглогод.опл. 5-9эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "24", "4.5. Отопл. с круглогод.опл. 10эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "25", "4.6. Отопл. с круглогод.опл. 12эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "26", "4.7. Отопл. с круглогод.опл. 14эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "27", "4.8. Отопл. с круглогод.опл. 14эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "28", "4.9. Отопл. с круглогод.опл. 16эт.дом до 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "29", "4.10. Отопл. с круглогод.опл. 1эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "30", "4.11. Отопл. с круглогод.опл. 2эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "31", "4.12. Отопл. с круглогод.опл. 4эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "32", "4.13. Отопл. с круглогод.опл. 4-5эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "33", "4.14. Отопл. с круглогод.опл. 6-7эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "34", "4.15. Отопл. с круглогод.опл. 8эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "35", "4.16. Отопл. с круглогод.опл. 9эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "36", "4.17. Отопл. с круглогод.опл. 10эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "37", "4.18. Отопл. с круглогод.опл. 11эт.дом после 1999" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3110", "38", "4.19. Отопл. с круглогод.опл. 12эт.дом после 1999" });
					#endregion

					#region Вывоз мусора
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "1", "1.1.Вывоз мусора из БЖФ без КГО" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "2", "2.1.Вывоз мусора из ЧС без КГО" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "3", "3.1. Вывоз мусора из БЖФ с КГО" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "4", "4.1. Вывоз мусора из ЧС с КГО" });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3111", "5", "5.1. Вывоз мусора с площади (за ед.измерения)" });
					#endregion

					#region Уборка МОП
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3112", "1", "Уборака МОП с чел." });
					Database.Insert(resY, new[] { "nzp_res", "nzp_y", "name_y" },
												new[] { "3112", "2", "Уборка МОП за ед.изм." });
					#endregion

					#endregion

					#region занесение параметров в таблицу res_values
					Database.Delete(resValues, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112)");

					for (int i = 1; i <= 13; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3101", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 8; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3102", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 4; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3103", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 12; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3104", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 12; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3105", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 31; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3106", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 65; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3107", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}


					for (int i = 1; i <= 32; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3108", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 32; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3109", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					for (int i = 1; i <= 38; i++)
					{
						Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3110", i.ToString(CultureInfo.InvariantCulture), "1", " " });
					}

					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "1", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "2", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "3", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "4", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3111", "5", "1", " " });

					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3112", "1", "1", " " });
					Database.Insert(resValues, new[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new[] { "3112", "2", "1", " " });

					#endregion
				}
			}
		}

		public override void Revert() {
			SetSchema(Bank.Kernel);

			#region объявление таблиц
			var prmName = new SchemaQualifiedObjectName
			{
				Name = "prm_name",
				Schema = CurrentSchema
			};
			var resolution = new SchemaQualifiedObjectName
			{
				Name = "resolution",
				Schema = CurrentSchema
			};
			var resY = new SchemaQualifiedObjectName
			{
				Name = "res_y",
				Schema = CurrentSchema
			};
			var resX = new SchemaQualifiedObjectName
			{
				Name = "res_x",
				Schema = CurrentSchema
			};
			var resValues = new SchemaQualifiedObjectName
			{
				Name = "res_values",
				Schema = CurrentSchema
			};
			var sPoint = new SchemaQualifiedObjectName
			{
				Name = "s_point",
				Schema = CentralKernel
			};
			#endregion

			if (Database.TableExists(prmName) &&
				Database.TableExists(resolution) &&
				Database.TableExists(resY) &&
				Database.TableExists(resX) &&
				Database.TableExists(resValues) &&
				Database.TableExists(sPoint))
			{
				int rowCount;
				var value = Database.ExecuteScalar(" SELECT COUNT(nzp_wp) FROM " + CentralKernel + Database.TableDelimiter + sPoint.Name + " WHERE bd_kernel = 'nftul' ");
				if (int.TryParse(value.ToString(), out rowCount) && rowCount > 0)
				{
					Database.Delete(prmName, "nzp_prm IN (1401,1402,1403,1404,1405,1406,1407,1408,1409,1410,1411,1412) ");
					Database.Delete(resolution, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112) ");
					Database.Delete(resX, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112) ");
					Database.Delete(resY, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112) ");
					Database.Delete(resValues, "nzp_res IN (3101,3102,3103,3104,3105,3106,3107,3108,3109,3110,3111,3112) ");
				}

			}
		}
	}
}
