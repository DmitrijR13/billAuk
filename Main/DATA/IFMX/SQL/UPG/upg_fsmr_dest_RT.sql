database #pref_data;
--------------------------------------------------------
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".s_dest;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
--     Справочники
--------------------------------------------------------
--------------------------------------------------------
--------------------------------------------------------
--     5. Справочник Претензии, заявки  (статичный (ведет разработчик))
--------------------------------------------------------
CREATE TABLE "are".s_dest(
   nzp_dest SERIAL NOT NULL,              -- ключ
   dest_name CHAR(100),             -- наименование претензии, заявки
   term_days SMALLINT default 0,    -- нормативный срок исполнения (дни)
   term_hours SMALLINT default 0,   -- нормативный срок исполнения (часы)
   nzp_serv INTEGER,              -- ссылка на услугу -> services.nzp_serv
   num_nedop INTEGER,               -- тип недопоставки по умолчанию (ссылка на upg_s_kind_nedop.nzp_kind)
   cur_unl INTEGER
   );
create unique index "are".ix_u_dest_no on "are".s_dest (nzp_dest);
create index "are".ix_u_dest_serv on "are".s_dest (nzp_serv);
create index "are".ix_u_dest_nedop on "are".s_dest (num_nedop);
ALTER TABLE "are".s_dest ADD CONSTRAINT PRIMARY KEY 
   (nzp_dest) CONSTRAINT "are".uDest;

insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (7,'Повреждение кабелей',0,3,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (8,'Течь в водопроводных трубах',1,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (9,'Повреждения аварийные трубопроводов и их сопряжений',0,1,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (10,'Неисправности мусоропровода',1,0,19,38);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (11,'Электрическая сеть - неисправности аварийного порядка',0,1,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (12,'Электроплита',0,3,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (13,'Неисправности в системе освещения общедомовых помещений',7,0,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (14,'Повреждения покрытия крыш зданий',1,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (15,'Отсутствие горячей воды',0,0,9,8);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (16,'Помехи сигналов с антенны',0,0,13,22);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (17,'Низкая температура помещения',1,0,8,6);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (18,'Лифт',1,0,5,19);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (19,'Неисправности канализационной сети',1,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (20,'Отсутствие холодной воды',0,0,6,1);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (21,'Отсутствие газа',1,0,10,13);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (22,'Отключение системы электропитания',0,2,25,16);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (23,'Отсутствие уборки в подъезде',1,0,17,24);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (24,'Отсутствие уборки двора',1,0,18,25);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (26,'Течь в системе отопления',1,0,22,27);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (27,'Повреждения стен зданий',0,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (28,'Повреждения внутри зданий',5,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (30,'Неисправности в системе отопления',1,0,22,27);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (31,'Отсутствие телеантенны',0,0,13,22);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (32,'Слабый напор холодной воды',0,0,6,40);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (33,'Отсутствие дератизации',0,0,20,39);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (34,'Повреждения зданий',1,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (35,'Неисправности в электрической сети',0,0,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (36,'Неисправности водопроводной сети',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (37,'Отсутствие домофона',0,0,26,30);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (39,'Отсутствие телефона',0,0,27,31);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (40,'Неисправности домофона',0,0,26,30);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (41,'Отсутствие текущего ремонта жилого здания',0,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (42,'Неудовлетворительное содержание двора',0,0,18,25);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (43,'Несоответствие температуры горячей воды нормативу',0,0,9,9);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (44,'Нерегулярный вывоз ТБО',0,0,16,23);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (45,'Неисправности радиоточки',0,0,12,21);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (46,'Неисправности газовой колонки',0,0,209,35);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (47,'Слабый напор горячей воды',0,0,9,41);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (29,'Претензии к функционированию городского хозяйства',30,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (38,'Претензии по начислениям платы за жил-комм. услуги',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (48,'Отсутствие вывоза ЖБО',0,0,24,29);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (49,'Течь в канализационной сети',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (50,'Неудовлетворительная уборка подъезда',0,0,17,24);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (51,'Перебои с холодной водой',0,0,6,1);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (52,'Перебои с горячей водой',0,0,9,8);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (53,'Неисправность ливневки',1,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (54,'Отсутствие отопления',1,0,8,5);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (90,'Неудовлетворительное качество холодной воды',0,0,6,2);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (59,'Неудовлетворительное состояние экологии',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (60,'Неудовлетворительное содержание дорог',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (62,'Счетчики электрические - установка, ремонт',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (63,'Содержание собак',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (64,'Отсутствие освещения МОП',7,0,11,20);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (65,'Отсутствие отопления в подъездах',0,0,22,27);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (66,'Благодарности',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (67,'Благоустройство территорий, дорог',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (70,'Содержание насаждений, озеленение двора',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (71,'Отлов бродячих животных',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (72,'Отсутствие рам и стекол в подъездах',0,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (73,'Замена, установка сантехники',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (74,'Неисправности смывного бачка',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (75,'Счетчики водомерные - установка, ремонт',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (76,'Течь в водопроводных трубах (в подвальных помещениях)',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (77,'Течь в канализационной сети (в подвальных помещениях)',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (78,'Течь в системе отопления (в подвальных помещениях)',0,0,22,27);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (79,'Отсутствие вытяжки',0,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (80,'Претензии к функционированию ЖЭУ',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (81,'Отсутствие освещения улиц и дворов',7,0,1000,20);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (91,'Низкое напряжение в электрической цепи',0,0,25,17);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (92,'Несоответствие свойств газа норме',0,0,10,14);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (93,'Неудовлетворительное качество горячей воды',0,0,9,11);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (25,'Отсутствие вывоза ТБО',0,0,16,23);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (94,'Отсутствие фасадного освещения',7,0,1000,20);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (128,'Отсутствие налога на жилые помещения',0,0,28,32);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1205,'Отсутствие управления жилым фондом',0,0,205,33);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (100,'Отсутствие капитального ремонта жилых зданий',0,0,206,34);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (115,'Отсутствие найма',0,0,15,48);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (131,'Отсутствие услуги содержание собак',0,0,31,49);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1200,'Отсутствие воды для полива',0,0,200,50);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1201,'Отсутствие воды для домашних животных',0,0,201,51);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1202,'Отсутствие воды для транспорта',0,0,202,52);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1203,'Отсутствие воды для бани',0,0,203,53);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1204,'Отсутствие кабельного телевидения',0,0,204,54);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1207,'Отсутствие отопления хозяйственных построек',0,0,207,55);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1210,'Отсутствие электроснабжения ночного',0,0,210,57);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1211,'Отсутствие жилищных услуг',0,0,211,58);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1215,'Отсутствие прочих расходов',0,0,215,62);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1230,'Отсутствие вахтера',0,0,230,36);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1231,'Отсутствие коменданта',0,0,231,37);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1243,'Отсутствие лифта',0,0,243,72);
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
update statistics for TABLE "are".s_dest;
-----------------------------------------------------------------------------------------------------------------------
