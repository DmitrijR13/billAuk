database webdata;

set encryption password "IfmxPwd2";


--------------------------------------------------------
--районы
--------------------------------------------------------
drop table s_rajon;
create table webdb.s_rajon
( nzp_raj  serial(1) not null,
  rajon    char(20),
  ordering integer default 0
);

 delete from s_rajon where 1=1;

 insert into s_rajon (nzp_raj,rajon) values (1,"Казань");
 insert into s_rajon (nzp_raj,rajon) values (2,"Набережные Челны");
 insert into s_rajon (nzp_raj,rajon) values (3,"Нижнекамский");
 insert into s_rajon (nzp_raj,rajon) values (4,"Агрызский");
 insert into s_rajon (nzp_raj,rajon) values (5,"Азнакаевский");
 insert into s_rajon (nzp_raj,rajon) values (6,"Аксубаевский");
 insert into s_rajon (nzp_raj,rajon) values (7,"Актанышский");
 insert into s_rajon (nzp_raj,rajon) values (8,"Алексеевский");
 insert into s_rajon (nzp_raj,rajon) values (9,"Алькеевский");
 insert into s_rajon (nzp_raj,rajon) values (10,"Альметьевский");
 insert into s_rajon (nzp_raj,rajon) values (11,"Апастовский");
 insert into s_rajon (nzp_raj,rajon) values (12,"Арский");
 insert into s_rajon (nzp_raj,rajon) values (13,"Атнинский");
 insert into s_rajon (nzp_raj,rajon) values (14,"Бавлинский");
 insert into s_rajon (nzp_raj,rajon) values (15,"Балтасинский");
 insert into s_rajon (nzp_raj,rajon) values (16,"Бугульминский");
 insert into s_rajon (nzp_raj,rajon) values (17,"Буинский");
 insert into s_rajon (nzp_raj,rajon) values (18,"Верхнеуслонский");
 insert into s_rajon (nzp_raj,rajon) values (19,"Высокогорский");
 insert into s_rajon (nzp_raj,rajon) values (20,"Дербышки");
 insert into s_rajon (nzp_raj,rajon) values (21,"Дрожжановский");
 insert into s_rajon (nzp_raj,rajon) values (22,"Елабужский");
 insert into s_rajon (nzp_raj,rajon) values (23,"Заинский");
 insert into s_rajon (nzp_raj,rajon) values (24,"Зеленодольск");
 insert into s_rajon (nzp_raj,rajon) values (25,"Кайбицкий");
 insert into s_rajon (nzp_raj,rajon) values (26,"Камско-Устинский");
 insert into s_rajon (nzp_raj,rajon) values (27,"Кукморский");
 insert into s_rajon (nzp_raj,rajon) values (28,"Лаишевский");
 insert into s_rajon (nzp_raj,rajon) values (29,"Лениногорский");
 insert into s_rajon (nzp_raj,rajon) values (30,"Мамадышский");
 insert into s_rajon (nzp_raj,rajon) values (31,"Менделеевский");
 insert into s_rajon (nzp_raj,rajon) values (32,"Мензелинский");
 insert into s_rajon (nzp_raj,rajon) values (33,"Муслюмовский");
 insert into s_rajon (nzp_raj,rajon) values (34,"Новошешминский");
 insert into s_rajon (nzp_raj,rajon) values (35,"Нурлатский");
 insert into s_rajon (nzp_raj,rajon) values (36,"Пестречинский");
 insert into s_rajon (nzp_raj,rajon) values (37,"Рыбно-Слободский");
 insert into s_rajon (nzp_raj,rajon) values (38,"Сабинский");
 insert into s_rajon (nzp_raj,rajon) values (39,"Сармановский");
 insert into s_rajon (nzp_raj,rajon) values (40,"Спасский");
 insert into s_rajon (nzp_raj,rajon) values (41,"Танкодром");
 insert into s_rajon (nzp_raj,rajon) values (42,"Тетюшский");
 insert into s_rajon (nzp_raj,rajon) values (43,"Тукаевский");
 insert into s_rajon (nzp_raj,rajon) values (44,"Тюлячинский");
 insert into s_rajon (nzp_raj,rajon) values (45,"Черемшанский");
 insert into s_rajon (nzp_raj,rajon) values (46,"Чистопольский");
 insert into s_rajon (nzp_raj,rajon) values (47,"Ютазинский");
                                              
 update s_rajon
 set ordering = nzp_raj;

create unique index webdb.ix_raj_1 on webdb.s_rajon (nzp_raj);
create        index webdb.ix_raj_2 on webdb.s_rajon (ordering);



--------------------------------------------------------
--расчетные центры
--------------------------------------------------------
drop table s_rcentr;
create table webdb.s_rcentr
(  nzp_rc     serial(1) not null,
   rcentr     char(60),
   pref       integer not null,
   rc_adr     char(90),
   rc_email   char(60),
   rc_ruk     char(60),
   nzp_raj    integer default 0 not null 
);


 delete from s_rcentr where 1=1;

 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (1,24,'МАУ ИРЦ г.Зеленодольска',201,'422550 Республика Татарстан, г. Зеленодольск, ул.Ленина 42','irc-zelendol1@yandex.ru','Измайлова Елена Григорьевна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (2,2,'ООО ГРЦ г.Набережные Челны',31,'423834 Республика Татарстан, г.Набержные Челны, бульвар Школьный, д.3','office@grc-chelny.ru','Фадеев Владимир Иванович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (3,4,'ООО ЕРЦ Агрызского района и г.Агрыз',202,'422230 Республика Татарстан, г. Агрыз, ул. Гагарина 70','Gulshat31@yandex.ru','Ахметзянов Зульфат Зульфирович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (4,5,'МХ ООО ЕРЦ - Азнакаево',203,' Республика Татарстан, г. Азнакаево, ул. А.Гурьянова д.18','departament_azn@mail.ru','Авзалов Марат Марсович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (5,6,'ООО Коммун-сервис Аксубаево',204,' РТ Аксубаевский район п.г.т. Аксубаево ул.Краснопаритизанская д.3А','erz_aksu@mail.ru','Петрова Людмила Геннадьевна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (6,7,'ООО Актанышский РЦ',205,'423740 Республика Татарстан, г.Актаныш, ул. Проспект Ленина д.58','aktan-ahmat@yandex.ru','Ахматнабиева Рамиля Динфиковна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (7,8,'ООО УК Алексеевского района',206,'422900 Республика Татарстан, Алексеевский район,п.г.т. Алексеевское, ул. Павёлкина д.5','ykaralerc@rambler.ru','Колоколов Павел Владимирович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (8,9,'МУ ЕРЦ с. Базарные Матаки',207,' Республика Татарстан, Алькеевский район село Базарные Матаки ул.Ленина д.9','alk-ers@bk.ru,alk-ers@mail.ru','Шарапова Кристина Аветиковна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (9,11,'Апастовское МУ ЕРЦ',209,'422350 Республика Татарстан, Апастовский район, п.г.т. Апастово   ул. М. Джалиля д.13','gulnaz-erc@rambler.ru','Газимова Эльвира Фагимовна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (10,12,'ООО ЕРЦ Арского района',210,' Республика Татарстан, г. Арск, ул. Почтовая д.9','uk_erc_arsk@mail.ru','Ахмадиев Линар Хамитович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (11,13,'МУП Атнинское ЖКФ',211,'422750 Республика Татарстан, Атнинский район, с.Большая Атня, ул.Советская д.42','mupgkxatnia@bk.ru','Сафаров Рамил Рафаэлович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (12,14,'ООО ЕРЦ Бавлы',212,' Республика Татарстан, Бавлинский район','ers-bav@yandex.ru','Файзуллин Дамир Галиевич');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (13,15,'ООО УК Балтасинского района',213,'422250 Республика Татарстан, Балтасинский район, пгт. Балтаси, ул.Мира, д.9','mppbaltasi@mail.ru,mppgkh@baltash.gov.tatarstan.ru','Заляев Шамил Шафикович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (14,17,'ООО УК Буинск',215,'422430 Республика Татарстан, г.Буинск, Р.Люксембург д.50','erz_buinsk@rambler.ru','Чернов Юрий Петрович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (15,18,'ОАО Коммунальные сети Верхнеуслонского района',216,' Республика Татарстан, с. В. Услон, ул. Чехова, д.11.','kom-seti@mail.ru','Харитонов Михаил Андреевич');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (16,19,'МУ ИРЦ Высокогорского района',217,'422700 Республика Татарстан, Высокогорский район, станция Высокая Гора ул. Пролетарская д.13','irc.vgora@rambler.ru','Шарипова Маргарита Фимиевна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (17,19,'ОАО ТАТМЕТАЛЛ',252,' Республика Татарстан, Высокогорский район','tatmet@tatmetall.com','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (18,21,'ООО Жилище Дрожжановский район',218,'422470 Республика Татарстан, Дрожжановский район,с.Старое Дрожжаное,ул.Техническая д.26','veliullov@mail.ru,ooo-zhil@mail.ru','Хисамов Рамиль Мидехатович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (19,22,'ООО ЕРКЦ Елабуга',219,' Республика Татарстан, г.Елабуга','erc_alabuga@rambler.ru','Сайфуллин Руслан Фаризович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (20,44,'ООО ЕРЦ Тюлячи',240,' Республика Татарстан, Тюлячинский район, с.Тюлячи, ул.Ленина д.46','erc-tulyachi@yandex.ru, Iskander.Garipov@tatar.ru','Хабибрахманов Анас Мухаметханович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (21,25,'Кайбицкое МПП ЖКХ',221,'422330 Республика Татарстан, Кайбицкий район, с.Б.Кайбицы, ул.Гисматуллина д.2','kaibici-gkh@rambler.ru','Сингатуллин Ринат Рахимович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (22,26,'ООО ЕРЦ Камско-Устьинского района',222,'422820 Республика Татарстан, пгт. Камское-Устье, ул.К.Маркса,105','kam-ust-raschet@mail.ru','Миннизянова Завгария Гариповна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (23,27,'ООО ЕРЦ Кукмор',223,'422110 Республика Татарстан, г.Кукмор,п. Кукмор ул. Ленина д.24','erc_kukmor@mail.ru','Михайлов Владислав Григорьевич');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (24,28,'РЦ ЖКХ Лаишевский район',224,' Республика Татарстан, г.Лаишевский район','Tanzilya.Tavrizova@tatar.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (25,29,'ОАО Центр информационных ресурсов ЖКХ Лениногорск',225,'423250 Республика Татарстан, г.Лениногорск, ул.Заварыкина д.2','gkhlen@yandex.ru','Садриев Исхак Аюпович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (26,30,'ООО УК Мамадышского района',226,'422190 Республика Татарстан, г.Мамадыш, ул. К. Маркса д. 18/23','ukmam@mail.ru','Галлямов Рустам Мияссарович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (27,31,'МУ ЕРЦ Менделеевск',227,'423650 Республика Татарстан, Менделеевский район,г.Менделеевск ул.Химиков дом 3А','mend_rc@mail.ru','Тимофеева Роза Михайловна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (28,32,'ООО ЕРЦ Менезелинск',228,' Республика Татарстан, Мензелинский район,г.Мензелинск, ул.М.Джалиля , д.15','meli2004@mail.ru, menzelyk@mail.ru','Фатихова Рамзия Ильсуровна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (29,33,'ООО Муслюмовский ЕРЦ',229,'423970 Республика Татарстан, Муслюмовский район, с.Муслюмово, ул.Банковская д.58','erts.musl@yandex.ru','Гизетдинов Назиф Насихович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (30,3,'МАУ ЕРЦ г.Нижнекамска',32,'423570 Республика Татарстан, г. Нижнекамск,пр.Строителей, д.6а','erc@newmail.ru,erc@inbox.ru','Султангареева Илюза Салиховна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (31,3,'ОАО Нижнекамскнефтехим г.Нижнекамск',34,' Республика Татарстан, г. Нижнекамск','usrneftehim@rambler.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (32,3,'ОАО Нижнекамскшина г.Нижнекамск',33,' Республика Татарстан, г. Нижнекамск','gkh_shin@mail.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (33,34,'ООО Новошешминский ЕРЦ',230,'423190 Республика Татарстан, Новошешминский район, Новошешминск, ул.Буреева д.6','ukkristall@yandex.ru','Нестеров Петр Васильевич');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (34,36,'ООО Единый расчетный центр Пестрецы',232,' Республика Татарстан, Пестречинский район, Пестрецы, ул.Мелиораторов, д.17','uk-pest@mail.ru. erc-pestr@yandex.ru','Свиридов Юрий Сергеевич');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (35,38,'ООО Центр обслуживания ТСЖ Сабы',234,' Республика Татарстан, Сабинский район, Бог.Сабы, ул.Тукая, 87','saz@newmail.ru,aidarshax@mail.ru. so_tcj@mail.ru','Шайхутдинов Айдар Зуфарович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (36,39,'ООО Сармановский ЕРЦ',235,'423350 Республика Татарстан, Сармановский район,с.Сарманово, ул. Профсоюзная  дом  30','uksarman@mail.ru','Гайсин Габбас Мухтарович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (37,39,'РЦ ЖКХ п.Джалиль',236,' Республика Татарстан, пгт Джалиль','','');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (38,40,'ООО ЕРЦ г.Болгар и Спасского района',237,'422840 Республика Татарстан, Спасский район, г.Болгар, ул.Пионерская д.21','allakaznacheeva@mail.ru','Казначеева Алла Анатольевна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (39,42,'МУП МПП Тетюшское',238,'422370 Республика Татарстан, г. Тетюши,ул. Школьная д. №14','rif_tet@rambler.ru','Ахметов Рифкат Талгатович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (40,43,'ООО Тукайжилсервис',239,'423897 Республика Татарстан, Тукаевский район, д.Старые Ерыклы ул.Дуслык д.48','ol-gkh-tuk@mail.ru','Игнатьева Валентина Васильевна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (41,45,'ОАО Коммунальные сети Черемшанского района',241,'423100 Республика Татарстан, Черемшанский район,с. Черемшан, ул. Титова, 25','KOMSETI@MAIL.RU','Мингазов Ильнар Мингалеевич');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (42,46,'МУ УК Жилищный комплекс Чистополь',242,'422980 Республика Татарстан, г. Чистополь, ул. Ленина, д. 68','dimych@mail.ru,ptgh@mail.ru','Губеев Марат Хуснуллович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (43,47,'ООО ЕРЦ Ютазы',243,'423950 Республика Татарстан, Ютазинский район, рп Уруссу ул.Ленина, д.1','OOOERC11@mail.ru','Арисова Венера Набиулловна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (44,10,'РЦ ЖКХ г.Альметьевска',208,' Республика Татарстан, г.Альметьевск','errc@mail.ru','Гудков Евгений Петрович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (45,16,'ООО ЕРЦ г.Бугульмы',214,' Республика Татарстан, г.Бугульма','vitalik2003a@yandex.ru','Халиуллин Марат Фаридович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (46,23,'ООО Заинская Управляющая компания',220,'423520 Республика Татарстан, г.Заинск, ул. Рафикова д.7','zaiuk@mail.ru','Хузина Роза Файзелахатовна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (47,2,'ЗАО Камазжилбыт г.Набережные Челны',253,' Республика Татарстан, г. Набережные Челны , ул. Рубаненко, д.6','eremenko@kamgb.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (48,1,'ЗАО КВАРТ г.Казань',260,' Республика Татарстан, г. Казань','kvart@bancorp.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (49,1,'Студенческий городок КГУ',259,' Республика Татарстан, г. Казань','marina-iyudina@yandex.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (50,35,'ООО ЕРЦ Нурлат',231,'423040 Республика Татарстан, г.Нурлат,ул. Ленинградская, дом 15','lil_nur@mail.ru','Хаджимуратов Рамис Фатыхович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (51,18,'ТАТТРАНСГАЗ Верхне-Услонский р-н',251,' Республика Татарстан, Верхнеуслонский район п. Пустые Моркваши','simen1975@rambler.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (52,3,'ООО ТОС ЖКХ Камских полян',35,'423564 Республика Татарстан, пгт.Камские поляны, д.1/38 офис 23','erc-kpl@mail.ru,leon1978@mail.ru','Егоров Олег Николаевич');

 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (53,1,'РЦ Авиастроительного района',270,'420127 Республика Татарстан, г.Казань, ул.Побежимова, д-28','','Мухаметзянова С.И.');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (54,1,'РЦ Азино-1',271,'420100 г. Казань, ул.Закиева д.9а','azino-1@mail.ru','Шелуханов В.П.');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (55,1,'РЦ Азино-2',272,'420140 Республика Татарстан, г.Казань, пр.Победы, д114','rustam988@mail.ru','Тришин Николай Павлович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (56,1,'РЦ Уютный дом Ново-Савиновский',273,'420126 Республика Татарстан, г.Казань, ул.Четаева, д.17','','Халикова Гузаль Мингазовна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (57,1,'РЦ Приволжского района',275,'420110 Республика Татарстан, г.Казань, проспект Победы д.39','priv_rc@mail.ru','Климов Ефим Сергеевич');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (58,1,'РЦ Дербышки',277,'420075 Республика Татарстан, г.Казань, ул.Халезова, д.20','','Файзрахманова Эльза Ильгизаровна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (59,1,'РЦ Заречье',279,'420033 Республика Татарстан, г.Казань,ул.Кулахметова,д.5','','Антин Евгений Самуилович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (60,1,'РЦ Московского района г.Казани',278,'420095 Республика Татарстан, г.Казань, ул.Ш. Усманова д.28А','and_sh@mail.ru','Гарифуллин Исмагил Салихзянович');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (61,1,'РЦ Танкодром',283,'420087 г. Казань, ул.Латышских стрелков д.2а','erctank@mail.ru','Файзрахманова Эльза Ильгизаровна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (63,1,'РЦ Сервис-Гарант',284,'420061 г.Казань, ул.Космонавтов 29Б','sd2000km.ru','Саляхова Гельсария Райсовна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (64,1,'РЦ Гвардейская',286,'420073 г.Казань, ул.Гвардейская д.22','rodik_lis@mail.ru','Завьялова Мария Михайловна');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (65,1,'РИЦ',285,'420110 г. Казань, пр. Победы, д. 39','','Климов Ефим Сергеевич');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (66,1,'РЦ Вахитовского района',274,'420110 Республика Татарстан, г.Казань, проспект Победы д.39','priv_rc@mail.ru','Климов Ефим Сергеевич');

 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (999,1,'РЦ Тест',273,'420126 РТ, г.Казань, ул.Четаева, д.17','','Халикова Гузаль Мингазовна');



create unique index webdb.ix_rс_1 on webdb.s_rcentr (nzp_rc);
create        index webdb.ix_rс_2 on webdb.s_rcentr (pref);
create        index webdb.ix_rс_3 on webdb.s_rcentr (nzp_raj);



--------------------------------------------------------
--серверы рц
--------------------------------------------------------
drop table servers;
create table webd.servers
(  nzp_server serial(1) not null,
   hadr       char(140),   --адрес службы
   hadr2      char(140),   --логины доступа
   nzp_rc     integer      --РЦ
);


create unique index are.ix_srvs_1 on servers (nzp_server);
create        index are.ix_srvs_2 on servers (nzp_rc);


delete from servers where 1=1;

insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (1, 'net.tcp://localhost:8018/srv', 'Administrator; rubin', 1);

insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (2, 'net.tcp://localhost:8011/srv', 'Administrator; rubin', 63);

set encryption password "IfmxPwd2";
update servers 
set hadr = encrypt_aes(hadr), hadr2 = encrypt_aes(hadr2)
where 1=1;




--------------------------------------------------------
--локальные пользователи
--------------------------------------------------------
--надо хранить в rolesval
--






delete from servers where 1=1;

--zarech
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (1, 'net.tcp://178.205.97.176:7047/srv', 'usernet; userweb', 59);

--azino-1
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (2, 'net.tcp://78.138.147.141:7047/srv', 'usernet; userweb', 54);

--azino-2
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (3, 'net.tcp://78.138.130.127:7047/srv', 'webbrk; n2LMDs9', 55);

--derb
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (4, 'net.tcp://81.23.150.68:7047/srv', 'brobro; jmdfJs^2S', 58);

--tank
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (5, 'net.tcp://92.255.193.116:7047/srv', 'webbro; cDafs78s@', 61);



--garant
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (6, 'net.tcp://92.255.194.235:7047/srv', 'wbbrkr; s@#ds4ah9', 63);

--udom
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (7, 'net.tcp://89.184.21.39:7047/srv', 'usrbrok; jFG8nf3we%', 56);



--priv
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (8, 'net.tcp://89.184.21.39:7047/srv', 'usrbro; &sdA342sd', 57);

--vah
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (9, 'net.tcp://89.184.21.39:7048/srv', 'usrbro; &sdA342sd', 66);

--mos          
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (10, 'net.tcp://88.82.75.151:7048/srv', 'brouser; s&jfgw%23', 60);



set encryption password "IfmxPwd2";
update servers 
set hadr = encrypt_aes(hadr), hadr2 = encrypt_aes(hadr2)
where 1=1;

