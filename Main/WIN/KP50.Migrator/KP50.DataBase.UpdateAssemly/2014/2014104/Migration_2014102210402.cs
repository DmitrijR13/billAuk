using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014102210402, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2014102210402 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Supg);
            // миграция таблицы nftul_supg.s_work_type
            var s_work_type = new SchemaQualifiedObjectName() { Name = "s_work_type", Schema = CurrentSchema };
            if (!Database.TableExists(s_work_type)) Database.AddTable(s_work_type,
               new Column("nzp_work_type", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
               new Column("name_work_type", DbType.String.WithSize(30))
               );
            string[] listWorkType =
            {
                "Плановые работы", "Аварийные работы", "Отключения по долгам", "Прочее",
                "Капитальный ремонт"
            };
            for (int i = 0; i < listWorkType.Length; i++)
            {
                var variable = i + 1;
                var count = Database.ExecuteScalar("Select count(*) from s_work_type where nzp_work_type=" + variable);
                int parsedCount;
                if (int.TryParse(count.ToString(), out parsedCount))
                {
                    if (parsedCount == 0)
                    {
                        Database.ExecuteNonQuery("INSERT INTO s_work_type VALUES (" + variable + ", '" + listWorkType[i] + "');");
                    }
                }
            }

            // миграция таблицы nftul_supg.s_dest 
            var s_dest = new SchemaQualifiedObjectName() { Name = "s_dest", Schema = CurrentSchema };
            if (!Database.TableExists(s_dest))
            {
                Database.AddTable(s_dest,
                  new Column("nzp_dest", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                  new Column("dest_name", DbType.String.WithSize(100)),
                  new Column("term_days", DbType.Int16, ColumnProperty.None, 0),
                  new Column("term_hours", DbType.Int16, ColumnProperty.None, 0),
                  new Column("nzp_serv", DbType.Int32),
                  new Column("num_nedop", DbType.Int32),
                  new Column("cur_unl", DbType.Int32)
                  );
                if (!Database.ConstraintExists(s_dest, "udest"))
                {
                    Database.AddPrimaryKey("udest", s_dest, new[] { "nzp_dest" });
                }
                if (!Database.IndexExists("ix_u_dest_no", s_dest))
                {
                    Database.AddIndex("ix_u_dest_no", true, s_dest, new[] { "nzp_dest" });
                }

                if (!Database.IndexExists("ix_u_dest_nedop", s_dest))
                {
                    Database.AddIndex("ix_u_dest_nedop", false, s_dest, new[] { "num_nedop" });
                }

                if (!Database.IndexExists("ix_u_dest_serv", s_dest))
                {
                    Database.AddIndex("ix_u_dest_serv", false, s_dest, new[] { "nzp_serv" });
                }
            }

            int[] keys =
            {
                7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33,
                34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 59, 60, 62, 63, 64,
                65, 66, 67, 70, 71, 72, 73, 74,
                75, 76, 77, 78, 79, 80, 81, 90, 91, 92, 93, 94, 100, 115, 128, 131, 1200, 1201, 1202, 1203, 1204, 1205,
                1207, 1210, 1211, 1215, 1230, 1231, 1243
            };
            List<string> inserIntoSdesttList = new List<string>
            {
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (7,'Повреждение кабелей', null, 3, 23, 28);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (8,'Течь в водопроводных трубах',1,null,21,26);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (9,'Повреждения аварийные трубопроводов и их сопряжений',null,1,21,26);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (10,'Неисправности мусоропровода',1,null,19,38);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (11,'Электрическая сеть - неисправности аварийного порядка',null,1,23,28);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (12,'Электроплита',null,3,23,28);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (13,'Неисправности в системе освещения общедомовых помещений',7,null,23,28);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (14,'Повреждения покрытия крыш зданий',1,null,2,18);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (15,'Отсутствие горячей воды',null, null,9,8);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (16,'Помехи сигналов с антенны',null, null,13,22);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (17,'Низкая температура помещения',1,null,8,6);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (18,'Лифт',1,null,5,19);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (19,'Неисправности канализационной сети',1,null,21,26);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (20,'Отсутствие холодной воды',null, null,6,1);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (21,'Отсутствие газа',1,null,10,13);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (22,'Отключение системы электропитания',null,2,25,16);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (23,'Отсутствие уборки в подъезде',1,null,17,24);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (24,'Отсутствие уборки двора',1,null,18,25);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (25,'Отсутствие вывоза ТБО',null, null,16,23);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (26,'Течь в системе отопления',1,null,22,27);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (27,'Повреждения стен зданий',null, null,2,18);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (28,'Повреждения внутри зданий',5,null,2,18);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (29,'Претензии к функционированию городского хозяйства',30,null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (30,'Неисправности в системе отопления',1,null,22,27);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (31,'Отсутствие телеантенны',0,0,13,22);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (32,'Слабый напор холодной воды',null, null,6,40);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (33,'Отсутствие дератизации',null, null,20,39);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (34,'Повреждения зданий',1,null,2,18);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (35,'Неисправности в электрической сети',null, null,23,28);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (36,'Неисправности водопроводной сети',null, null,21,26);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (37,'Отсутствие домофона',null, null,26,30);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (38,'Претензии по начислениям платы за жил-комм. услуги',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (39,'Отсутствие телефона',null, null,27,31);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (40,'Неисправности домофона',null, null,26,30);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (41,'Отсутствие текущего ремонта жилого здания',null, null,2,18);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (42,'Неудовлетворительное содержание двора',null, null,18,25);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (43,'Несоответствие температуры горячей воды нормативу',null, null,9,9);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (44,'Нерегулярный вывоз ТБО',null, null,16,23);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (45,'Неисправности радиоточки',null, null,12,21);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (46,'Неисправности газовой колонки',null, null,209,35);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (47,'Слабый напор горячей воды',null, null,9,41);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (48,'Отсутствие вывоза ЖБО',null, null,24,29);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (49,'Течь в канализационной сети',null, null,21,26);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (50,'Неудовлетворительная уборка подъезда',null, null,17,24);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (51,'Перебои с холодной водой',null, null,6,1);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (52,'Перебои с горячей водой',null, null,9,8);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (53,'Неисправность ливневки',1,null,2,18);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (54,'Отсутствие отопления',1,null,8,5);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (59,'Неудовлетворительное состояние экологии',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (60,'Неудовлетворительное содержание дорог',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (62,'Счетчики электрические - установка, ремонт',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (63,'Содержание собак',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (64,'Отсутствие освещения МОП',7,null,11,20);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (65,'Отсутствие отопления в подъездах',null, null,22,27);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (66,'Благодарности',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (67,'Благоустройство территорий, дорог',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (70,'Содержание насаждений, озеленение двора',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (71,'Отлов бродячих животных',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (72,'Отсутствие рам и стекол в подъездах',null, null,2,18);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (73,'Замена, установка сантехники',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (74,'Неисправности смывного бачка',null, null,21,26);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (75,'Счетчики водомерные - установка, ремонт',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (76,'Течь в водопроводных трубах (в подвальных помещениях)',null, null,21,26);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (77,'Течь в канализационной сети (в подвальных помещениях)',null, null,21,26);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (78,'Течь в системе отопления (в подвальных помещениях)',null, null,22,27);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (79,'Отсутствие вытяжки',null, null,2,18);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (80,'Претензии к функционированию ЖЭУ',null, null,1000,0);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (81,'Отсутствие освещения улиц и дворов',7,null,1000,20);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (90,'Неудовлетворительное качество холодной воды',null, null,6,2);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (91,'Низкое напряжение в электрической цепи',null, null,25,17);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (92,'Несоответствие свойств газа норме',null, null,10,14);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (93,'Неудовлетворительное качество горячей воды',null, null,9,11);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (94,'Отсутствие фасадного освещения',7,null,1000,20);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (100,'Отсутствие капитального ремонта жилых зданий',null, null,206,34);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (115,'Отсутствие найма',null, null,15,48);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (128,'Отсутствие налога на жилые помещения',null, null,28,32);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (131,'Отсутствие услуги содержание собак',null, null,31,49);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1200,'Отсутствие воды для полива',null, null,200,50);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1201,'Отсутствие воды для домашних животных',null, null,201,51);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1202,'Отсутствие воды для транспорта',null, null,202,52);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1203,'Отсутствие воды для бани',null, null,203,53);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1204,'Отсутствие кабельного телевидения',null, null,204,54);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1205,'Отсутствие управления жилым фондом',null, null,205,33);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1207,'Отсутствие отопления хозяйственных построек',null, null,207,55);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1210,'Отсутствие электроснабжения ночного',null, null,210,57);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1211,'Отсутствие жилищных услуг',null, null,211,58);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1215,'Отсутствие прочих расходов',null, null,215,62);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1230,'Отсутствие вахтера',null, null,230,36);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1231,'Отсутствие коменданта',null, null,231,37);",
"insert into "+s_dest+ " (nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1243,'Отсутствие лифта',null, null,243,72);",
            };

            for (int i = 0; i < keys.Length; i++)
            {
                var count = Database.ExecuteScalar("Select count(*) from s_dest where nzp_dest=" + keys[i]);
                int parsedCount;
                if (int.TryParse(count.ToString(), out parsedCount))
                {
                    if (parsedCount == 0)
                    {
                        Database.ExecuteNonQuery(inserIntoSdesttList[i]);
                    }
                }
            }
        }
    }
}
