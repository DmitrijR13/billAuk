set search_path to public;

--страницы
create table if not exists pages
( nzp_page  serial not null,
  up_kod    integer,
  group_id  integer,
  page_url  char(80),
  page_menu char(80),
  page_name char(80),
  hlp       char(255)
  
) with oids;
--drop index if exists ix_pages_1;
--create unique index ix_pages_1 on pages (nzp_page);

--связи между страницами
create table if not exists page_links (
	page_from integer,
	group_from integer,
	page_to integer,
	group_to integer,
	sign char(120)
) with oids;

--страницы роли
create table if not exists role_pages (
	id serial not null,
	nzp_role integer not null,
	nzp_page integer not null,
	sign char(120)
) with oids;
drop index if exists ix_role_pages_1;
create unique index ix_role_pages_1 on role_pages(id);

drop index if exists ix_role_pages_2;
create unique index ix_role_pages_2 on role_pages(nzp_role, nzp_page);

--действия роли
create table if not exists role_actions (
	id serial not null,
	nzp_role integer not null,
	nzp_page integer not null,
	nzp_act integer not null,
	mod_act integer,
	sign char(120)
) with oids;
drop index if exists ix_role_actions_1;
create unique index ix_role_actions_1 on role_actions(id);

drop index if exists ix_role_actions_2;
create unique index ix_role_actions_2 on role_actions(nzp_role, nzp_page, nzp_act);

--предварительное удаление страниц, ссылок и т.п.    
delete from role_pages where 1=1;
delete from role_actions where 1=1;
delete from pages_show where 1=1;
delete from actions_show where 1=1;
delete from actions_lnk where 1=1;
delete from img_lnk where 1=1;
delete from s_help where 1=1;
delete from pages where 1=1;
delete from s_actions where 1=1;

--добавление страниц
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (949,null,null, 'Нет данных', '', 'Нет данных', '');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (999,null,2   , 'Завершить сеанс', '', '', 'предназначен для завершения работы в программе');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (1  ,null,2   , 'Главная страница', '~/default.aspx', 'Главная страница','предназначен для перехода на главную страницу программы');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (2  ,null,null, 'Меню:','','','включает в себя следующие пункты:');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (3  ,null,2   , 'Помощь','','','предназначен для перехода на страницу «Помощь» в программе');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (4  ,null,2   , 'Предыдущая страница','','','предназначен для перехода на предыдущую страницу программы');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (5  ,null,2   , 'Мои файлы', '~/kart/bill/myreport.aspx', 'Мои файлы','переходит на форму для просмотра и скачивания файлов пользователя');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (30 ,null,null, 'Поиск','','','осуществляет переход к формам для поиска информации');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (31 ,30  ,30  , 'Поиск по адресу','~/kart/adres/findls.aspx','Поиск по адресу','выполняет поиск данных по адресу');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (32 ,30  ,30  , 'Поиск по параметрам','~/kart/prm/findprm.aspx','Поиск по характеристикам жилья','выполняет поиск данных по характеристикам жилья');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (33 ,30  ,30  , 'Поиск по начислениям','~/kart/charge/findch.aspx','Поиск по начислениям','выполняет поиск данных по начислениям');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (34 ,30  ,30  , 'Поиск по жителям','~/kart/gil/findgil.aspx','Поиск по жителям','выполняет поиск по персональным данным');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (35 ,30  ,30  , 'Поиск по показаниям','~/kart/counter/findcnt.aspx','Поиск по показаниям приборов учета','выполняет поиск по данным приборов учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (36 ,30  ,30  , 'Поиск по недопоставкам','~/kart/nedo/findnd.aspx','Поиск по недопоставкам','выполняет поиск данных по недопоставленным услугам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (37 ,30  ,30  , 'Поиск по ОДН','~/kart/charge/findodn.aspx','Поиск по коэффициентам коррекции расходов дома','выполняет поиск данных по расходам для общедомовых нужд');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (38 ,30  ,30  , 'Поиск по услугам', '~/kart/serv/findserv.aspx', 'Поиск по услугам и поставщикам','выполняет поиск по услугам, поставщикам услуг');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (39 ,30  ,30  , 'Поиск по заявкам', '~/supg/findsupg.aspx', 'Поиск по заявкам','выполняет поиск по заявкам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (40 ,null,null, 'Выбранные списки', '', '', 'осуществляет переход на ранее выбранные списки');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (41 ,40  ,40  , 'Лицевые счета','~/general/basepage/baselist.aspx','Список лицевых счетов','переходит на ранее выбранный список лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (42 ,40  ,40  , 'Дома','~/general/basepage/baselist.aspx','Список домов','переходит на ранее выбранный список домов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (43 ,73  ,73  , 'Улицы','~/general/basepage/baselist.aspx','Список улиц','переходит на список лицевых улиц');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (44 ,73  ,73  , 'Управляющие организации', '~/kart/adres/spisar.aspx', 'Список управляющих организаций','переходит на список управляющих организаций');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (45 ,73  ,73  , 'Отделения', '~/kart/adres/spisgeu.aspx', 'Список отделений','переходит на список отделений');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (46 ,null,null, 'Банки данных','~/general/basepage/baselist.aspx','Список банков данных','переходит на список банков данных');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (47 ,null,null, 'Дома улицы', '~/general/basepage/baselist.aspx', 'Список домов улицы','переходит на список домов выбранной улицы');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (48 ,null,null, 'Характеристики жилья', '~/kart/prm/spisprm.aspx', 'Характеристики жилья','переходит на список характеристик жилья');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (49 ,73  ,73  , 'Поставщики', '~/kart/supp/spissupp.aspx', 'Поставщики','переходит на форму для просмотра поставщиков услуг');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (50 ,40  ,40  , 'Перечень счетов дома', '~/general/basepage/baselist.aspx', 'Перечень счетов дома','переходит на список лицевых счетов дома, сформированных на формах недопоставок дома и перечня услуг дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (51 ,70  ,70  , 'Характеристики жилья','~/kart/prm/spisprm.aspx','Характеристики жилья','переходит на список характеристик жилья выбранного лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (52 ,null,null, 'Поквартирная карточка','~/general/basepage/baselist.aspx','Поквартирная карточка','переходит на поквартирную карточку');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (53 ,40  ,40  , 'Приборы учета','~/kart/counter/counters.aspx','Список приборов учета','переходит на список выбранных приборов учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (54 ,70  ,70  , 'Показания индивидуал. ПУ','~/kart/counter/spisval.aspx', '', 'переходит на список показаний индивидуальных приборов учета для выбранного лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (55 ,70  ,70  , 'Недопоставки','~/kart/nedo/spisnd.aspx','Список недопоставок','переходит на список недопоставок');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (56 ,40  ,40  , 'Карточки жильцов', '~/kart/gil/spisgil.aspx', 'Список карточек жильцов', 'переходит на выбранный список карточек жильцов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (57 ,40  ,71  , 'Лицевые счета дома', '~/general/basepage/baselist.aspx', 'Список лицевых счетов дома','переходит на список лицевых счетов данного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (59 ,71  ,71  , 'Параметры дома', '~/kart/prm/spisprm.aspx', 'Список домовых параметров', 'переходит на список параметров данного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (61 ,71  ,71  , 'Показания общедомовых ПУ', '~/kart/counter/spisval.aspx', 'Показания общедомовых приборов учета', 'переходит на список показаний общедомовых приборов учета для выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (62 ,70  ,70  , 'Индивидуальные ПУ', '~/kart/counter/counters.aspx', 'Индивидуальные приборы учета', 'переходит на список индивидуальных приборов учета для выбранного лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (63 ,71  ,71  , 'Общедомовые ПУ', '~/kart/counter/counters.aspx', 'Общедомовые приборы учета', 'переходит на список приборов учета для выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (64 ,null,null, 'Значения параметра лицевого счета', '~/kart/prm/prm.aspx', 'Значения параметра лицевого счета','переходит к списку значений выбранного параметра лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (66 ,70  ,70  , 'Показания групповых ПУ', '~/kart/counter/spisval.aspx', 'Показания групповых приборов учета','переходит на список показаний групповых приборов учета для выбранного лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (67 ,71  ,71  , 'Показания групповых ПУ', '~/kart/counter/spisval.aspx', 'Показания групповых приборов учета','переходит на список показаний групповых приборов учета для выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (68 ,null,null, 'Индивидуальный ПУ','~/kart/counter/countercard.aspx','Индивидуальный прибор учета','переходит в карточку индивидуального прибора учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (69 ,null,null, 'Общедомовой ПУ','~/kart/counter/countercard.aspx','Общедомовой прибор учета','переходит в карточку общедомового прибора учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (70 ,null,null, 'Лицевой счет', '', '', 'содержит пункты меню с информацией о лицевом счете');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (71 ,null,null, 'Дом', '', '', 'содержит пункты меню с информацией о доме');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (72 ,null,null, 'Аналитика', '', '', 'осуществляет переход к аналитическим данным');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (73 ,null,null, 'Справочники','','','содержит пункты меню для перехода к справочникам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (74 ,null,null, 'Групповые операции','','','содержит пункты меню для перехода к групповым операциям');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (75 ,null,null, 'Отчеты','','','содержит пункты меню для формирования отчетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (76 ,null,null, 'Данные о пачке','','','содержит пункты меню для перехода к данным о выбранной пачке платежей');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (77 ,null,null, 'Операции','','','содержит пункты меню для выполнения различных операций');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (78 ,null,null, 'Данные по заявкам','','','содержит пункты меню для перехода к данным о заявках');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (79 ,null,null, 'Заявка','','','содержит пункты меню для выполнения операций с заявками');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (80 ,null,null, 'Списки','','','содержит пункты меню для отображения различных списков');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (81 ,72  ,72  , 'Адресное пространство','~/general/basepage/aa.aspx','Адресное пространство','переходит к структуре адресного пространства');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (82 ,72  ,72  , 'Поставщики услуг','~/general/basepage/as.aspx','Поставщики услуг','переходит к структуре поставщиков услуг');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (83 ,null,null, 'Начисления','~/general/basepage/aa.aspx','Начисления','переходит к структуре начисления в разрезе данных');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (84 ,null,null, 'Интерактивная карта','~/general/basepage/baselist.aspx','Интерактивная карта','осуществляет переход к карте Яндекса');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (91 ,null,null, 'Карточка жильца', '~/kart/gil/gil.aspx', 'Карточка жильца','переходит в карточку жильца');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (92 ,71  ,71  , 'Групповые ПУ','~/kart/counter/counters.aspx','Групповые приборы учета','переходит на список групповых приборов учета для выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (93 ,null,null, 'Групповой ПУ','~/kart/counter/countercard.aspx','Групповой прибор учета','переходит в карточку группового прибора учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (94 ,73  ,68  , 'Типы приборов учета','~/kart/counter/countertypes.aspx','Типы приборов учета','переходит на список типов приборов учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (95 ,70  ,70  , 'Перечень услуг', '~/kart/serv/spisserv.aspx', 'Перечень услуг','переходит на список услуг для выбранного лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (96 ,null,null, 'Поставщики и формулы расчета', '~/kart/serv/serv.aspx', 'Поставщики и формулы расчета','переходит на список периодов действия поставщиков и формул расчета для выбранной услуги и лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (97 ,70  ,70  , 'Группы лицевых счетов', '~/kart/adres/groupls.aspx', 'Группы лицевых счетов','переходит на список групп лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (98 ,70  ,70  , 'Реквизиты лицевого счета', '~/kart/adres/cardls.aspx', 'Реквизиты лицевого счета','переходит на форму с общей информацией о лицевом счете');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (99 ,71  ,71  , 'Реквизиты дома', '~/kart/adres/carddom.aspx', 'Реквизиты дома','переходит на форму с общей информацией о доме');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (100,74  ,41  , 'Характеристики жилья', '~/kart/prm/spisprm.aspx', 'Групповые операции с характеристиками жилья','переходит на список квартирных характеристик жилья');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (101,null,null, 'Характеристика жилья', '~/kart/prm/prm.aspx', 'Групповые операции с характеристикой жилья','переходит в окно добавления или удаления значений выбранной характеристики жилья для выбранных лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (102,74  ,42  , 'Параметры дома', '~/kart/prm/spisprm.aspx', 'Групповые операции с параметрами дома','переходит на список параметров дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (103,null,null, 'Параметр дома', '~/kart/prm/prm.aspx', 'Групповые операции с параметром дома','переходит в окно добавления или удаления значений выбранного параметра дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (104,74  ,41  , 'Перечень услуг', '~/kart/serv/spisserv.aspx', 'Групповые операции с услугами','переходит на список услуг для выполнения групповой операции над выбранными лицевыми счетами');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (105,null,null, 'Поставщики и формулы расчета', '~/kart/serv/serv.aspx', 'Групповые операции с услугой','переходит в окно добавления или удаления периода действия поставщиков и формул расчета для выбранной услуги и выбранных лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (106,72  ,72  , 'Статистика по начислениям', '~/kart/charge/statcharge.aspx', 'Статистика по начислениям','переходит в окно со сводными данными о начислениях');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (107,72  ,72  , 'Сальдо по перечислениям', '~/kart/charge/distrib.aspx', 'Сальдо по перечислениям','переходит на форму с данными о распределении оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (108,74  ,41  , 'Реквизиты лицевого счета', '~/kart/adres/cardls.aspx', 'Групповые операции с реквизитами лицевых счетов','переходит на форму назначения управляющей организации, ЖЭУ, участка, подъезда выбранным лицевым счетам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (109,74  ,42  , 'Реквизиты дома', '~/kart/adres/carddom.aspx', 'Групповые операции с реквизитами домов','переходит на форму назначения управляющей организации, ЖЭУ выбранным домам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (110,74  ,41  , 'Недопоставки', '~/kart/nedo/spisnd.aspx', 'Групповые операции с недопоставками','переходит на форму добавления или удаления недопоставок выбранным лицевым счетам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (111,70  ,70  , 'Состояние лицевого счета', '~/kart/prm/prm.aspx', 'Состояние лицевого счета','переходит на форму просмотра и изменения состояний лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (112,null,null, 'Статистика по начислениям', '~/kart/charge/statcharge.aspx', 'Статистика по начислениям','переходит в окно со сводными данными о начислениях по дому');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (120,null,null, 'Начисления', '', '', 'осуществляет переход к начислениям');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (121,70  ,70  , 'Сальдо по лицевому счету','~/general/basepage/baselist.aspx','Сальдо по лицевому счету','осуществляет переход к сальдо по лицевому счету');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (122,70  ,70  , 'Список начислений', '~/kart/charge/charges.aspx', 'Список начислений','осуществляет переход к начислениям лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (123,70  ,70  , 'Список платежей', '~/kart/charge/listpays.aspx', 'Список платежей потребителей', 'осуществляет переход к платежам лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (124,71  ,71  , 'Расчет ОДН','~/kart/charge/odn.aspx','Расчет ОДН','осуществляет переход к расчету ОДН указанного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (125,71  ,71 ,  'Расчет','~/kart/charge/calc.aspx','Расчет начислений','осуществляет переход к начислениям указанного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (126,71  ,71  , 'Сальдо по дому','~/general/basepage/baselist.aspx','Сальдо по дому','осуществляет переход к сальдо начислений по указанному дому');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (127,120 ,81  , 'Сальдо по управкомпании','~/general/basepage/baselist.aspx','Сальдо по управкомпании','осуществляет переход к сальдо начислений по указанной управкомпании');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (128,120 ,81  , 'Сальдо по участку','~/general/basepage/baselist.aspx','Сальдо по участку','осуществляет переход к сальдо начислений по указанному участку');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (129,70  ,70  , 'Счет-фактура','~/kart/bill/bill.aspx','Счет-фактура','переходит к счет-фактуре лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (130,120 ,82  , 'Сальдо по поставщику','~/general/basepage/baselist.aspx','Сальдо по поставщику','осуществляет переход к сальдо начислений по указанному поставщику');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (131,70  ,70  , 'Платежный документ','~/kart/bill/billrt.aspx','Платежный документ','переходит к платежному документу на оплату жилищно-коммунальных услуг для выбранного лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (133,70  ,70  , 'Отчеты','~/kart/bill/report.aspx','Отчеты для лицевого счета','переходит к списку отчетов для выбранного лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (134,75  ,41  , 'Отчеты по списку лицевых счетов','~/kart/bill/report.aspx','Отчеты по выбранному списку лицевых счетов','переходит к списку отчетов для выбранного списка лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (135,75  ,91  , 'Справки для жильца','~/kart/bill/report.aspx','Справки','переходит к списку справок для выбранного жильца');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (136,75  ,56  , 'Отчеты по списку карточек жильцов','~/kart/bill/report.aspx','Отчеты по списку карточек жильцов','переходит к списку отчетов (списков, сведений, реестров) для выбранного списка жильцов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (137,71  ,71  , 'Отчеты','~/kart/bill/report.aspx','Отчеты по выбранному дому','переходит к списку отчетов для выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (138,75  ,197 , 'Отчеты по списку заявок','~/kart/bill/report.aspx','Отчеты по выбранным заявкам','переходит к списку отчетов, формируемых для выбранных заявок');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (139,75  ,246 , 'Отчеты по списку плановых работ','~/kart/bill/report.aspx','Отчеты по списку плановых работ','переходит к списку отчетов, формируемых для выбранных плановых работ');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (150,null,null, 'Доступ','','','осуществляет переход к формам для управления доступом к сайту');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (151,150 ,150 , 'Пользователи','~/admin/access/users.aspx','Пользователи','переходит на список пользователей');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (152,150 ,150 , 'Роли','~/admin/access/roles.aspx','Роли','переходит на список ролей доступа');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (153,150 ,153 , 'Пользователь','~/admin/access/usercard.aspx','Пользователь','переходит в карточку пользователя');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (154,150 ,154 , 'Роль','~/admin/access/rolecard.aspx','Роль','переходит в карточку роли');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (155,150 ,150 , 'История','~/admin/access/access.aspx','История','переходит на список посещений и действий за период');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (160,null,null, 'Процессы','','','осуществляет переход к формам для управления процессами');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (161,160 ,160 , 'Список заданий', '~/admin/process/processes.aspx', 'Список заданий', 'переходит на список заданий');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (162,70  ,70  , 'Поквартирная карточка', '~/kart/gil/spisgil.aspx', 'Поквартирная карточка','переходит в список карточек жильцов квартиры');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (163,null,91  , 'Список периодов убытия', '~/kart/gil/spisglp.aspx', 'Список периодов временного убытия жильца','переходит в список периодов временного убытия жильца');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (164,null,164 , 'Период убытия', '~/kart/gil/glp.aspx', 'Период временного убытия жильца','переходит на форму с информацией о периоде временного убытия жильца');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (165,70  ,70  , 'Изменения сальдо', '~/kart/charge/perekidki.aspx', 'Изменения сальдо','переходит на форму с информацией об изменениях сальдо лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (166,70  ,70  , 'Расходы по лицевому счету', '~/kart/charge/rashod.aspx', 'Расходы по лицевому счету','переходит на форму с информацией о расходах по услугам для лицевого счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (167,71  ,71  , 'Расходы по дому', '~/kart/charge/rashod.aspx', 'Расходы по дому','переходит на форму с информацией о расходах по услугам для дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (168,73  ,73  , 'Тарифы', '~/kart/prm/tarifs.aspx', 'Тарифы','переходит на форму с информацией о тарифах');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (169,null,null, 'Значения общесистемного параметра', '~/kart/prm/prm.aspx', 'Значения общесистемного параметра','переходит на форму для просмотра и ввода значений общесистемного параметра');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (170,73  ,73  , 'Системные параметры', '~/kart/prm/sysparams.aspx', 'Системные параметры','переходит на форму просмотра значений системных параметров');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (171,null,null, 'Значения параметра прибора учета', '~/kart/prm/prm.aspx', 'Значения параметра прибора учета','переходит на форму просмотра значений параметра прибора учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (172,null,null, 'Значения параметра прибора учета', '~/kart/prm/prm.aspx', 'Значения параметра прибора учета','переходит на форму просмотра значений параметра прибора учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (173,null,null, 'Значения параметра лицевого счета', '~/kart/prm/prm.aspx', 'Значения параметра лицевого счета','переходит на форму просмотра значений параметра');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (174,null,null, 'Значения параметра поставщика', '~/kart/prm/prm.aspx', 'Значения параметра поставщика','переходит на форму просмотра значений параметра поставщика');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (175,null,null, 'Параметры поставщика', '~/kart/prm/suppparams.aspx', 'Параметры поставщика','переходит на форму просмотра значений параметров поставщика');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (176,null,null, 'Параметры формул расчета', '~/kart/prm/frmparams.aspx', 'Параметры формул расчета услуги','переходит на форму просмотра значений параметров, влияющих на расчет услуги');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (177,70  ,70  , 'Собственники', '~/kart/gil/spissobstw.aspx', 'Собственники квартиры','переходит на форму просмотра списка собственников квартиры');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (178,null,null, 'Собственник', '~/kart/gil/kartsobstw.aspx', 'Собственник квартиры','переходит на форму просмотра карточки собственника квартиры');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (179,74  ,42  , 'Недопоставки', '~/kart/nedo/spisnd.aspx', 'Групповые операции с домовыми недопоставками','переходит на форму добавления или удаления недопоставок лицевым счетам выбранных домов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (180,74  ,42  , 'Характеристики жилья', '~/kart/prm/spisprm.aspx', 'Групповые операции с характеристиками жилья','переходит на список квартирных характеристик жилья для лицевых счетов выбранных домов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (181,null,null, 'Характеристика жилья', '~/kart/prm/prm.aspx', 'Групповые операции с характеристикой жилья','переходит в окно добавления или удаления значений выбранной характеристики жилья для лицевых счетов выбранных домов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (182,74  ,42  , 'Перечень услуг', '~/kart/serv/spisserv.aspx', 'Групповые операции с услугами для выбранных домов','переходит на список услуг для выполнения групповой операции над лицевыми счетами выбранных домов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (183,null,null, 'Поставщики и формулы расчета', '~/kart/serv/serv.aspx', 'Групповые операции с услугой для выбранных домов','переходит в окно добавления или удаления периода действия поставщиков и формул расчета для выбранной услуги и лицевых счетов выбранных домов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (185,71  ,71  , 'Общеквартирные ПУ','~/kart/counter/counters.aspx','Общеквартирные приборы учета','переходит на список общеквартирных приборов учета выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (186,71  ,71  , 'Показания общеквартир. ПУ', '~/kart/counter/spisval.aspx', 'Показания общеквартирных приборов учета','переходит на список показаний общеквартирных приборов учета выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (187,null,null, 'Общеквартирный ПУ','~/kart/counter/countercard.aspx','Общеквартирный прибор учета','переходит в карточку общеквартирного прибора учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (188,null,null, 'Значения домового параметра', '~/kart/prm/prm.aspx', 'Значения домового параметра','переходит на форму просмотра значений домового параметра');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (189,70  ,70  , 'Заявки', '~/supg/orders.aspx', 'Заявки','переходит на форму просмотра списка заявок по выбранному лицевому счету');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (190,71  ,71  , 'Перечень услуг', '~/kart/serv/spisservdom.aspx', 'Перечень услуг дома','переходит на список услуг для выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (191,71  ,71  , 'Недопоставки', '~/kart/nedo/spisnddom.aspx', 'Недопоставки дома','переходит на список недопоставок для выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (192,71  ,71  , 'Настройки ОДН', '~/kart/prm/spisprmreg.aspx', 'Настройки ОДН для дома','переходит на список параметров, влияющих на расчет ОДН выбранного дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (193,78  ,193 , 'Наряд-заказ', '~/supg/joborder.aspx', 'Наряд-заказ','переходит на форму наряда-заказа');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (194,null,null, 'Досье', '~/kart/gil/spisgil.aspx', 'Досье','переходит на форму просмотра досье жильца');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (195,30  ,30  , 'Поиск по группам', '~/kart/adres/findgroupls.aspx', 'Поиск по группам','переходит на шаблон поиска по группам лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (196,74  ,41  , 'Группы лицевых счетов', '~/kart/adres/group.aspx', 'Групповые операции с группами лицевых счетов','переходит на форму добавления в группы или удаления из групп выбранных лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (197,40  ,40  , 'Заявки', '~/general/basepage/baselist.aspx', 'Список заявок','переходит на список выбранных заявок');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (198,76  ,198 , 'Пачка оплат', '~/kart/kassa/pack.aspx', 'Пачка оплат','переходит на форму пачки оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (199,null,199 , 'Оплата', '~/kart/kassa/packls.aspx', 'Оплата','переходит на форму просмотра и регистрации оплаты');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (200,null,null, 'Загрузить оплаты', '~/kart/kassa/uploadpack.aspx', 'Загрузка электронного реестра оплат','переходит на форму загрузки электронного реестра оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (201,30  ,30  , 'Пачки оплат', '~/finances/findpack.aspx', 'Пачки оплат','выполняет поиск и отображение списка оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (202,235 ,202 , 'Пачка оплат', '~/finances/pack.aspx', 'Пачка оплат','выполняет поиск и отображение списка оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (203,235 ,235 , 'Операционный день', '~/finances/operday.aspx', 'Операционный день','служит для просмотра и установки операционного дня');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (204,236 ,204 , 'Квитанция об оплате', '~/finances/packls.aspx', 'Квитанция об оплате','переходит на форму просмотра квитанции об оплате');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (205,78  ,78  , 'Заявка', '~/supg/order.aspx', 'Заявка','переходит на форму просмотра информации о выбранной заявке');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (206,75  ,75  , 'Отчеты','~/kart/bill/report.aspx','Отчеты','переходит к списку отчетов, выполняемых по всему банку данных');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (207,77  ,77  , 'Добавить новую заявку','~/supg/armoperator.aspx','Добавить новую заявку','переходит к форме добавления новой заявки');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (208,40  ,40  , 'Список необработанных заявок','~/supg/raworders.aspx','Список необработанных заявок','переходит к списку необработанных заявок');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (209,40  ,40  , 'Список нарядов-заказов на выполнение','~/supg/incomingjoborders.aspx','Список нарядов-заказов на выполнение','переходит к списку нарядов-заказов на выполнение');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (210,73  ,73  , 'Нормативы','~/kart/prm/norms.aspx','Нормативы','переходит к списку нормативов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (211,73  ,73  , 'Цели прибытия/убытия','~/pasp/sprav/sprav.aspx','Цели прибытия/убытия','переходит к справочнику целей прибытия и убытия жильцов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (212,73  ,73  , 'Документы','~/pasp/sprav/sprav.aspx','Документы','переходит к справочнику документов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (213,73  ,73  , 'Родственные отношения','~/pasp/sprav/sprav.aspx','Родственные отношения','переходит к справочнику родственных отношений');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (214,73  ,73  , 'Гражданство','~/pasp/sprav/sprav.aspx','Гражданство','переходит к справочнику гражданств');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (215,73  ,73  , 'Адреса','~/pasp/sprav/address.aspx','Адреса','переходит к справочникам стран, регионов, городов, районов, населенных пунктов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (216,73  ,73  , 'Районы дома','~/pasp/sprav/sprav.aspx','Районы дома','переходит к справочнику районов дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (217,73  ,73  , 'Органы регистрационного учета','~/pasp/sprav/sprav.aspx','Органы регистрационного учета','переходит к справочнику органов регистрационного учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (218,73  ,73  , 'Места выдачи документов','~/pasp/sprav/sprav.aspx','Места выдачи документа','переходит к справочнику мест выдачи документов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (219,73  ,73  , 'Документы о праве собственности','~/pasp/sprav/sprav.aspx','Документы о праве собственности','переходит к справочнику документов о праве собственности');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (220,77  ,77  , 'Формирование недопоставок','~/supg/nedop.aspx','Формирование недопоставок','переходит на страницу формирования недопоставок по нарядам-заказам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (221,73  ,73  , 'Услуги','~/kart/serv/services.aspx','Услуги','переходит к списку услуг');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (222,null,null, 'Параметры услуги','~/kart/prm/servparams.aspx','Параметры услуги','переходит к списку параметров выбранной услуги');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (223,null,null, 'Параметр услуги','~/kart/prm/prm.aspx','Параметр услуги','переходит на форму просмотра и редактирования значений выбранного параметра услуги');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (224,73  ,73  , 'Доступные услуги','~/kart/serv/availableservices.aspx','Доступные услуги','переходит на список услуг, которые могут оказываться в заданном периоде времени');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (225,null,null, 'Доступная услуга','~/kart/serv/availableservice.aspx','Доступная услуга','переходит на форму просмотра и редактирования периодов, в которые возможно оказание выбранной услуги');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (227,null,null, 'Параметры упр. организации','~/kart/prm/areaparams.aspx','Параметры управляющей организации','переходит к списку параметров выбранной управляющей организации');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (228,null,null, 'Параметр упр. организации','~/kart/prm/prm.aspx','Параметр управляющей организации','переходит на форму просмотра и редактирования выбранного параметра управляющей организации');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (229,null,null, 'Параметры участка','~/kart/prm/geuparams.aspx','Параметры участка','переходит к списку параметров выбранного участка');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (230,null,null, 'Параметр участка','~/kart/prm/prm.aspx','Параметры участка','переходит на форму просмотра и редактирования выбранного параметра участка');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (231,null,null, 'Реквизиты упр. организации','~/kart/prm/arearequisites.aspx','Реквизиты управляющей организации','переходит на форму просмотра и редактирования реквизитов выбранной управляющей организации');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (232,290 ,290 , 'Счета контрагентов','~/finances/payerrequisites.aspx','Счета контрагентов','переходит к списку расчетных счетов выбранного контрагента');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (233,290 ,290 , 'Договоры с УК','~/finances/contracts.aspx','Договоры с УК','переходит к списку договоров между управляющей компанией и подрядчиками');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (234,null,null, 'Перечисления подрядчикам','~/finances/payertransfer.aspx','Перечисления подрядчикам','переходит на форму перечисления подрядчикам оплаты за услуги');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (235,null,null, 'Операционный день','','','содержит пункты меню для перехода к формам, связанным с операционным днем');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (236,null,null, 'Данные об оплате','','','содержит пункты меню для перехода к данным о выбранной оплате');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (237,null,null, 'Реквизиты','','','содержит пункты меню для перехода к данным о реквизитах');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (238,70  ,70  , 'Наряды-заказы','~/supg/kvarjoborder.aspx','Наряды-заказы','отображает список нарядов-заказов по квартире');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (239,73  ,73  , 'Процент удержания','~/finances/percent.aspx','Процент удержания','отображает процент удержания');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (240,70  ,70  , 'Список договоров на ЛС','~/finances/lscontracts.aspx','Список договоров на ЛС','отображает договоры по ЛС');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (241,74  ,74  , 'Показания ПУ','~/kart/counter/counterreadings.aspx','Показания приборов учета','отображает форму списочного просмотра и ввода показаний приборов учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (242,null,null, 'Добавление периода убытия','~/kart/gil/glp.aspx','Добавление периода временного убытия','отображает форму для добавления периода убытия выбранным жильцам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (243,77  ,77  , 'Загрузка показаний ПУ','~/kart/counter/uploadcounters.aspx','Загрузка показаний приборов учета','отображает форму для загрузки из файла показаний приборов учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (244,70  ,70  , 'Рассрочка','~/kart/charge/credit.aspx','Рассрочка','отображает форму для просмотра рассрочки платежей за услуги');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (245,30  ,30  , 'Поиск по плановым работам','~/supg/plan/findplannedworks.aspx','Поиск по плановым работам','отображает форму поиска по информации о плановых, ремонтных и других работах');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (246,40  ,40  , 'Плановые работы','~/supg/plan/plannedworks.aspx','Список плановых работ','отображает список выбранных плановых, ремонтных и других работ');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (247,77  ,77  , 'Новая плановая работа','~/supg/plan/plannedwork.aspx','Плановая работа','позволяет добавить новую плановую работу');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (248,73  ,73  , 'Службы, организации','~/supg/sprav/serviceorgs.aspx','Справочник служб, организаций','отображает список служб и организаций, которым могут быть переадресованы поступающие заявки');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (249,null,null, 'Служба, организация','~/supg/sprav/serviceorg.aspx','Служба, организация','отображает информацию о выбранной службе, организации, которой могут быть переадресованы поступающие заявки');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (251,70  ,70  , 'Плановые работы','~/supg/plan/plannedworks.aspx','Список плановых работ','отображает список плановых, ремонтных и других работ по выбранному ЛС');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (252,73  ,73  , 'Справочник претензий','~/supg/plan/claimcatalog.aspx','Справочник претензий','отображает cправочник претензий');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (253,235 ,235 , 'Портфель','~/finances/case.aspx','Портфель','отображает портфель с квитанциями на оплату');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (256,73  ,73  , 'Контрагенты','~/finances/sprav/contractorcatalog.aspx','Контрагенты','отображает справочник контрагентов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (257,290 ,290 , 'Подразделения контрагентов','~/finances/sprav/bankcatalog.aspx','Подразделения контрагентов','отображает справочник подразделений контрагентов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (258,235 ,235 , 'Корзина','~/finances/basket.aspx','Корзина','отображает оплаты, которые не распределились вследствие ошибок');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (259,235 ,235 , 'Перекидки м/контрагентами','~/finances/payerreval.aspx','Перекидки между подрядчиками','отображает форму просмотра и редактирования перекидок сумм между подрядчиками');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (260,null,null, 'Генерация лицевых счетов','~/kart/adres/gendomls.aspx','Генерация лицевых счетов','отображает форму для генерации лицевых счетов по выбранному дому');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (261,73  ,73  , 'Улицы','~/kart/sprav/streetcatalog.aspx','Справочник улиц','отображает форму для редактирования справочника улиц');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (262,70  ,70  , 'Корректировка вх. сальдо','~/kart/charge/correctsaldo.aspx','Корректировка входящего сальдо','отображает форму для редактирования входящего сальдо лицевого счета по услугам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (263,74  ,41  , 'Групповой ввод характеристик жилья','~/kart/prm/groupprm.aspx','Групповой ввод характеристик жилья','отображает форму для группового ввода характеристик жилья');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (264,74  ,41  , 'Генерация индивидуальных ПУ','~/kart/counter/genlspu.aspx','Генерация индивидуальных приборов учета','отображает форму для генерации приборов учета по выбранным лицевым счетам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (265,235 ,235 , 'Контроль распред. оплат','~/finances/condistrpayments.aspx','Контроль распределения оплат','отображает форму для контроля распределения оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (266,null,null, 'Добавление заданий','~/admin/process/addtask.aspx','Добавление заданий','отображает форму для добавления заданий');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (267,40  ,40  , 'Список нарядов-заказов для опроса','~/supg/surveyjoborders.aspx','Список нарядов-заказов для опроса','отображает список нарядов-заказов для опроса');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (268,77  ,77  , 'Расчетный месяц','~/kart/charge/calcmonth.aspx','Расчетный месяц','отображает информацию о текущем расчетном месяце и предоставляет функции изменения расчетного месяца');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (269,270 ,270 , 'Приоритеты услуг','~/finances/settings/servpriority.aspx','Приоритетные услуги при учете оплат','отображает список услуг, имеющих приоритет при распределении оплаты');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (270,null,null, 'Сервис','','','содержит пункты меню для настройки работы системы');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (271,270 ,270 , 'Настройки','~/admin/settings/setups.aspx','Настройки','содержит настройки системы');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (272,291 ,291 , 'Списки к перечислению','~/subsidy/request/requests.aspx','Списки к перечислению','отображает форму для поиска и просмотра списков к перечислению');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (273,275 ,273 , 'Список к перечислению','~/subsidy/request/request.aspx','Список к перечислению','отображает форму для просмотра и редактирования списка к перечислению');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (274,291 ,291 , 'Приказы на перечисление','~/subsidy/payment/payments.aspx','Приказы на перечисление','отображает форму для просмотра и редактирования списка приказов на перечисление');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (275,null,null, 'Финансирование','','','содержит пункты меню для перехода к данным о перечислениях');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (276,null,null, 'Соглашение','','','содержит пункты меню для перехода к данным о выбранном субсидировании');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (277,275 ,277 , 'Приказ на перечисление','~/subsidy/payment/payment.aspx','Приказ на перечисление','отображает форму для просмотра и редактирования приказа на перечисление');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (278,72  ,72  , 'Сальдовая ведомость','~/subsidy/charge/subsidysaldo.aspx','Сальдовая ведомость','отображает форму для просмотра сводных данных о начислениях дотаций, финансировании и сальдовой информации');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (279,291 ,291 , 'Соглашения с подрядчиками','~/subsidy/agreement/agreements.aspx','Соглашения с подрядчиками','отображает форму для просмотра списка соглашений с подрядчиками');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (280,276 ,276 , 'Соглашение с подрядчиком','~/subsidy/agreement/agreement.aspx','Соглашение с подрядчиком','отображает форму для просмотра и редактирования соглашения с подрядчиком');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (281,70  ,70  , 'Рассчитанные дотации','~/subsidy/charge/lscharges.aspx','Рассчитанные дотации','отображает форму для просмотра рассчитанных дотаций по выбранному лицевому счету');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (282,77  ,77  , 'Рассылка сообщений','~/supg/messages/messagelist.aspx','Рассылка сообщений','отображает форму для просмотра списка сообщений для рассылки');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (283,73  ,73  , 'Муниципальные образования','~/kart/adres/vills.aspx','Муниципальные образования','отображает форму для просмотра списка муниципальных образований');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (284,73  ,73  , 'Уровни платежей граждан','~/subsidy/sprav/percpt.aspx','Уровни платежей граждан','отображает форму для просмотра и редактирования уровня платежей граждан');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (285,276 ,276 , 'Кассовые планы','~/subsidy/agreement/cashplan.aspx','Кассовые планы','отображает форму для просмотра кассовых планов и помесячных планов распределений субсидий');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (286,null,null, 'Новое сообщение','~/supg/messages/newmessage.aspx','Новое сообщение','отображает форму для рассылки нового сообщения');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (287,276 ,276 , 'Акты о фактической поставке','~/subsidy/agreement/actsofsupply.aspx','Акты о фактической поставке','отображает форму для просмотра актов о фактической поставке услуг поставщиками');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (288,276 ,276 , 'Характеристики жилого фонда','~/subsidy/agreement/housingstockdescrs.aspx','Характеристики жилого фонда','отображает форму для просмотра списка технических характеристик жилого фонда');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (289,73  ,73  , 'Справочник телефонов','~/supg/messages/phonesprav.aspx','Справочник телефонов','отображает форму телефонного справочника');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (290,null,null, 'Контрагент','','','отображает пункты меню для просмотра информации о контрагентах');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (291,null,null, 'Списки','','','отображает пункты меню для просмотра списочной информации');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (292,72  ,72  , 'OLAP','~/analytics/olap/olap.aspx','','отображает форму для просмотра информации с использованием технологии OLAP');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (293,270 ,270 , 'Загрузка из файла','~/admin/files/fload.aspx','','отображает форму для загрузки данных из файла');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (294,73  ,69  , 'Типы приборов учета','~/kart/counter/countertypes.aspx','Типы приборов учета','переходит на список типов приборов учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (295,74  ,41  , 'Признаки перерасчета','~/kart/mustcalc/mcgroup.aspx','Перерасчеты по услугам','переходит на форму добавления признаков перерасчетов по выбранному списку лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (296,70  ,70  , 'Признаки перерасчета','~/kart/mustcalc/mcls.aspx','Перерасчеты по услугам','переходит на форму просмотра списка признаков перерасчетов по выбранному лицевому счету');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (297,77  ,77  , 'Обновление данных','~/supg/ercaddress.aspx','Обновление данных','отображает форму для выполнения обновления адресного пространства и сальдо');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (298,73  ,73  , 'Классификация сообщений','~/supg/sprav/zthemes.aspx','Справочник Классификация сообщений','отображает форму справочника Классификация сообщений');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (299,74  ,41  , 'Изменение сальдо','~/kart/charge/groupperekidki.aspx','Изменение сальдо по списку лицевых счетов','отображает форму для выполнения групповой операции по изменению сальдо для выбранного списка лицевых счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (300,null,null, 'Реестр изменений сальдо','~/kart/charge/reestrperekidok.aspx','Реестр групповых изменений сальдо','отображает форму для просмотра списка выполненных групповых операций изменения сальдо');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (301,null,301 , 'Услуга','~/kart/serv/service.aspx','Услуга','отображает форму для просмотра информации об услуге');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (302,null,null, 'Перенос суммы между ЛС','~/kart/charge/perekidkalstols.aspx','Перенос суммы между лицевыми счетами','отображает форму для переноса суммы с выбранного лицевого счета на другой лицевой счет');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (303,291 ,null, 'Реестр изменений сальдо','~/kart/charge/reestrperekidok.aspx','Реестр изменений сальдо по выбранному лицевому счету','отображает форму для просмотра списка выполненных групповых операций изменения сальдо по выбранному лицевому счету');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (304,77  ,77  , 'Перенос переплат','~/finances/operations/moveoverpayments.aspx', 'Перенос переплат','Перенос переплат на открытые лицевые счета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (305,270 ,270 , 'Обмен данными','~/admin/files/exchange.aspx','','отображает форму для обмена данными');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (306,73  ,73  , 'Адрес по умолчанию','~/settings/address/defaultaddress.aspx', 'Адрес по умолчанию','Отображает форму для редактирования адреса по умолчанию');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (307,270 ,270 , 'КЛАДР','~/admin/files/kladr.aspx','','Загрузка адресного пространства');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (308,270  ,270  , 'Взаимодействие с Банком','~/kart/sprav/workwithbank.aspx','Взаимодействие с Банком','Отображает форму для загрузки оплат в формате Сбербанка');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (309,74  ,42  , 'Смена УК','~/kart/adres/changearea.aspx','Смена управляющей компании','Отображает форму для смены управляющей компании по списку выбранных домов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (310,77  ,77  , 'Выгрузка банк-клиент','~/finances/operations/payment_bank.aspx','Выгрузка банк-клиент','Выгрузка банк-клиент');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (311,null,null, 'Добавление пачки оплат','~/finances/pack/addpack.aspx','Добавление пачки оплат','отображает форму для добавления пачки оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (312,null,null, 'Добавление оплаты','~/finances/pack/addpackls.aspx','Добавление оплаты','отображает форму для добавления оплаты');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (314,null,null, 'Файлы системы банк-клиент','~/finances/operations/files_in_bank_cliend_load.aspx','Выгрузка банк-клиент','Выгрузка банк-клиент');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (315,null,null, 'Настройка расчета','~/admin/files/setchanges.aspx','Настройка расчета','отображает форму для настройки расчета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (316,null,null, 'Ввод изменений в сальдо','~/kart/charge/perekidkals.aspx','Ввод изменений в сальдо по услугам','отображает форму для ввода изменений сальдо по лицевому счету по услугам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (317,270 ,270 , 'Взаимодействие с социальной защитой','~/admin/files/exchangesz.aspx','Информационный обмен с соц. защитой','отображает форму для взаимодействия с органами социальной защиты');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (319,73  ,73  , 'Форматы банк-клиент','~/finances/sprav/bcformats.aspx','Форматы выгрузки в системы банк-клиент','отображает форму для просмотра и управления форматами выгрузки в системы банк-клиент');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (320,270 ,270 , 'Настройка уникальных кодов','~/admin/settings/codes.aspx','Настройка уникальных кодов','отображает форму для настройки уникальных кодов объектов учета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (322,30  ,30  , 'Поиск по долгу','~/debitor/find/finddebt.aspx','Поиск по долгу','отображает форму для поиска по задолженности');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (323,30  ,30  , 'Поиск по делам','~/debitor/find/finddeals.aspx','Поиск по делам','отображает форму для поиска по делам должников');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (324,40  ,40  , 'Должники','~/debitor/list/debitors.aspx','Должники','отображает выбранный список лицевых счетов с задолженностями');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (325,40  ,40  , 'Дела','~/debitor/list/deals.aspx','Дела','отображает выбранный список дел должников');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (326,null,null, 'Дело','~/debitor/deal/deal.aspx','Дело','переходит на форму выбранного дела');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (327,null,null, 'Соглашение','~/debitor/deal/agreement.aspx','Соглашение о рассрочке на оплату долга','переходит на форму выбранного соглашения о предоставлении рассрочки на оплату долга');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (328,null,null, 'Иск','~/debitor/deal/debtclaim.aspx','Иск','переходит на форму выбранного иска');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (329,null,326 , 'Изменение задолженности','~/debitor/deal/debtchange.aspx','Изменение задолженности','переходит на форму корректировки суммы долга');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (330,270 ,270 , 'Настройки','~/debitor/settings/debtsettings.aspx','Настройки','переходит на форму настроек системы должников');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (331,74  ,324 , 'С должниками','~/debitor/operations/debtoperations.aspx','Групповые операции с должниками','переходит на форму групповых операций с выбранным списком должников');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (332,74  ,325 , 'С делами','~/debitor/operations/dealoperations.aspx','Групповые операции с делами','переходит на форму групповых операций с выбранным списком дел');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (333,75  ,null, 'Отчеты','~/kart/bill/reports.aspx','Отчеты','переходит на форму выполнения отчетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (334,270 ,270 , 'Сверка оплат с банком','~/exchange/bank/upload_efs.aspx','Загрузка ежедневных файлов сверки оплат','переходит на форму загрузки ежедневных файлов сверки оплат'); --Астрахань
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (335,270 ,270 , 'Быстрый ввод показаний ПУ','~/kart/counter/fastpu.aspx','Ввод показаний приборов учета','переходит на форму ввода показаний приборов учета'); --Тула
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (336,270 ,270 , 'Загрузка оплат от ВТБ24','~/exchange/bank/upload_vtb24.aspx','Загрузка оплат от ВТБ24','переходит на форму загрузки оплат от ВТБ24'); --Тула
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (337,70  ,70  , 'Отчеты','~/kart/bill/reports.aspx','Отчеты для лицевого счета','переходит к списку отчетов для выбранного лицевого счета'); --extjs
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (338,75  ,41  , 'Отчеты по списку лицевых счетов 2','~/kart/bill/reports.aspx','Отчеты по выбранному списку лицевых счетов','переходит к списку отчетов для выбранного списка лицевых счетов'); --extjs
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (339,73  ,73  , 'Связанные услуги','~/kart/serv/servdependencies.aspx','Связанные услуги','переходит на форму связанных услуг');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (340,72  ,72  , 'Сальдо по перечислениям', '~/kart/charge/distrib_dom.aspx', 'Сальдо по перечислениям','переходит на форму с данными о распределении оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (341,73  ,73  , 'Формулы расчета','~/kart/sprav/formuls.aspx','Справочник формул расчета','отображает справочник формул расчета');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (342,77  ,77  , 'Подготовка данных для печати счетов','~/admin/process/prepareprintinvoices.aspx','Подготовка данных для печати счетов','Подготавливает данные для печати счетов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (344,70  ,70  , 'Коды ЛС у поставщиков', '~/kart/adres/supplierlslist.aspx', 'Коды ЛС у поставщиков','Коды ЛС у поставщиков');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (345,70  ,70  , 'События', '~/kart/adres/ls_events.aspx', 'История событий ЛС','История событий ЛС');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (346,null ,null  , 'ЛС поставщика', '~/kart/adres/supplierls.aspx', 'ЛС поставщика','ЛС поставщика');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (347,71  ,71  , 'События', '~/kart/adres/dom_events.aspx', 'История событий дома','История событий дома');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (348,150 ,150 , 'Логи','~/admin/files/download_logs.aspx','','Загрузка адресного пространства');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (349,73  ,73  , 'Параметры', '~/kart/prm/params.aspx', 'Параметры', 'переходит к справочнику параметров');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (350,73  ,73  , 'Правила удержания','~/finances/percent_dom.aspx','Правила удержания','отображает настройки процентов удержаний по домам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (351,70  ,162 , 'Справка для жильца','~/kart/bill/reports.aspx','Справка для жильца','переходит к списку отчетов для выбранной карточки жильца');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (352,73  ,73  , 'Договоры', '~/kart/sprav/contracts.aspx', 'Договоры','переходит на форму для просмотра и управления договорами на оказание жилищно-коммунальных услуг');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (353,72  ,72  , 'Сальдо по перечислениям', '~/kart/charge/distrib_dom_supp.aspx', 'Сальдо по перечислениям','переходит на форму с данными о распределении оплат');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (354,73  ,73  , 'Правила удержания','~/finances/percent_dom_supp.aspx','Правила удержания','отображает настройки процентов удержаний по домам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (355,73  ,73  , 'Контрагенты','~/finances/sprav/contractors.aspx','Контрагенты','отображает справочник контрагентов');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (356,235 ,235 , 'Перекидки м/контрагентами','~/finances/payerreval_supp.aspx','Перекидки между контрагентами','отображает форму просмотра и редактирования перекидок сумм между контрагентами');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (357,null,null, 'Договор','','','отображает пункты меню для просмотра информации о договоре');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (358,357 ,352 , 'Реквизиты договоров','~/finances/contracts/contract_details.aspx','Реквизиты договоров','переходит на форму просмотра и управления реквизитами договоров');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (359,235 ,235 , 'Перечисления контрагентам','~/finances/payertransfer_supp.aspx','Перечисления контрагентам','переходит на форму перечисления контрагентам средств по договорам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (360,73  ,73  , 'Управляющие организации', '~/kart/adres/areas.aspx', 'Список управляющих организаций','переходит на список управляющих организаций');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (361,72  ,72  , 'Статистика по начислениям', '~/kart/charge/statcharge_supp.aspx', 'Статистика по начислениям','переходит в окно со сводными данными о начислениях');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (362,71  ,71  , 'Сальдо по дому', '~/kart/charge/statcharge_supp.aspx', 'Статистика начислений по дому','переходит в окно с начислениями по дому');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (363,74  ,41  , 'Смена адреса', '~/kart/adres/change_ls_address.aspx', 'Смена адреса лицевых счетов', 'переходит на форму изменения адреса лицевых счетов');

-- в разработке
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (6  ,null,2   , 'Настройки', '~/general/setup/settings.aspx', 'Пользовательские настройки','переходит на форму пользовательских настроек');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (184,71  ,71  , 'Протокол расчета ОДН', '~/kart/charge/reportodn.aspx', 'Протокол расчета ОДН','переходит в окно просмотра протокола расчета ОДН');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (226,30  ,30  , 'Поиск по серверам','~/kart/adres/findserver.aspx','Поиск по серверам','переходит на форму поиска информации по серверам');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (254,null,null, 'Аналитика','','','содержит пункты меню для аналитики');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (255,254 ,254 , 'Статистика','~/supg/analisis.aspx','Статистика','отображает статистику');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (313,73  ,73  , 'Категории льгот','~/kart/lgota/lgotcategories.aspx','Категории льгот','отображает форму для просмотра справочника категорий льгот');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (318,270 ,270 , 'Реестр для Небо','~/admin/files/nebo_reestr.aspx','Веб-сервис для Небо','отображает форму для Веб-сервиса для Небо');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (321,270 ,270 , 'Учет сторонних начислений','~/exchange/service/suppexchange.aspx','Взаимодействие со сторонними биллинговыми системами','отображает форму для взаимодействия со сторонним биллинговыми системами');
insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (343,160 ,160 , 'Список задач', '~/admin/process/jobs.aspx', 'Список задач', 'переходит на список задач');

--добавление картинки для пункта меню                                                
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 1, 'homepage.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 2, 'kmenuedit.png'); 
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 3, 'help.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 4, 'back.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 30, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 40, 'docs_folder.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 70, 'users.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 71, 'house.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 72, 'analize.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 73, 'dictionary.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 74, 'list.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 75, 'print24.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 76, 'docs_folder.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 77, 'operations.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 78, 'supg24.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 120, 'ooo_calc.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 150, 'lock.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 160, 'screen_windows.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 235, 'date.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 236, 'users.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 237, 'account24.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 254, 'pie_chart_blue.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 270, 'tools.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 291, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 949, 'nodata.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 999, 'exit.png');

--добавление действий
insert into s_actions (nzp_act,act_name,hlp) values (1, 'Выполнить поиск'  ,'выполняет поиск после заполнения требуемых полей поиска');
insert into s_actions (nzp_act,act_name,hlp) values (2, 'Очистить шаблон'  ,'очищает ранее заполненные поля шаблона');
insert into s_actions (nzp_act,act_name,hlp) values (3, 'Открыть данные'   ,'открывает указанные данные на просмотр или для редактирования');
insert into s_actions (nzp_act,act_name,hlp) values (4, 'Добавить запись'  ,'добавляет запись в список');
insert into s_actions (nzp_act,act_name,hlp) values (5, 'Обновить список'  ,'обновляет ранее выбранный список');
insert into s_actions (nzp_act,act_name,hlp) values (6, 'Очистить список'  ,'очищает ранее заполненные поля формы');
insert into s_actions (nzp_act,act_name,hlp) values (7, 'Показать карту'   ,'показывает интерактивную карту Яndex');
insert into s_actions (nzp_act,act_name,hlp) values (8, 'На печать'        ,'открывает форму для печати');
insert into s_actions (nzp_act,act_name,hlp) values (9, 'Блокировка', 'блокирует или разблокирует запись в зависимости от текущего состояния');
insert into s_actions (nzp_act,act_name,hlp) values (10, 'Запустить', 'запускает фоновый процесс обработки заданий');
insert into s_actions (nzp_act,act_name,hlp) values (11, 'Приостановить', 'останавливает фоновый процесс обработки заданий');
insert into s_actions (nzp_act,act_name,hlp) values (12, 'Удалить все', 'удаляет все выбранные записи');
insert into s_actions (nzp_act,act_name,hlp) values (13, 'Удалить сессию', 'удаляет все активные сессии выбранного пользователя');
insert into s_actions (nzp_act,act_name,hlp) values (14, 'Открыть показания', 'открывает форму с информацией о показаниях прибора учета');
insert into s_actions (nzp_act,act_name,hlp) values (15, 'Сбросить пароль', 'удаляет пароль выбранного пользователя');
insert into s_actions (nzp_act,act_name,hlp) values (16, 'Установить значение', 'устанавливает значение параметра в заданном интервале времени');
insert into s_actions (nzp_act,act_name,hlp) values (17, 'Удалить значение', 'удалить значение параметра в заданном интервале времени');
insert into s_actions (nzp_act,act_name,hlp) values (18, 'Добавить недопоставку', 'добавляет недопоставку по услуге');
insert into s_actions (nzp_act,act_name,hlp) values (19, 'Удалить недопоставку', 'удаляет недопоставку по услуге');
insert into s_actions (nzp_act,act_name,hlp) values (20, 'Все параметры', 'показывает все параметры: и имеющие действующие значения и не имеющие установленных значений');
insert into s_actions (nzp_act,act_name,hlp) values (21, 'Установить услугу', 'назначает период оказания услуги, поставщика и формулу расчета услуги, действующие в этот период');
insert into s_actions (nzp_act,act_name,hlp) values (22, 'Закрыть услугу', 'сохраняет информацию о том, что услуга не оказывается в выбранном периоде времени');
insert into s_actions (nzp_act,act_name,hlp) values (23, 'Добавить жильца', 'добавляет нового жильца в поквартирную карточку');
insert into s_actions (nzp_act,act_name,hlp) values (24, 'Создать лицевой счет', 'создает новый лицевой счет');
insert into s_actions (nzp_act,act_name,hlp) values (25, 'Создать дом', 'создает новый дом');
insert into s_actions (nzp_act,act_name,hlp) values (26, 'Удалить запись', 'удаляет выбранную запись');
insert into s_actions (nzp_act,act_name,hlp) values (51, 'Изменить состояние', 'изменяет состояние счета');
insert into s_actions (nzp_act,act_name,hlp) values (61, 'Сохранить изменения','сохраняет введенные данные');
insert into s_actions (nzp_act,act_name,hlp) values (62, 'Добавить ПУ', 'добавляет запись прибоа учет');
insert into s_actions (nzp_act,act_name,hlp) values (63, 'Редактировать ПУ', 'редактирует запись прибора учета');
insert into s_actions (nzp_act,act_name,hlp) values (64, 'Удалить ПУ', 'удаляет запись прибора учета');
insert into s_actions (nzp_act,act_name,hlp) values (65, 'Выполнить подсчет', 'выполняет подсчет агрегированных данных');
insert into s_actions (nzp_act,act_name,hlp) values (66, 'Обновить данные', 'выполняет обновление текущих данных');
insert into s_actions (nzp_act,act_name,hlp) values (67, 'Показать все значения','показывает все значения одного параметра');
insert into s_actions (nzp_act,act_name,hlp) values (68, 'Обновить списки', 'обновляет списки доступных управляющей организаций, участков, услуг, поставщиков, банков данных, которые используются для задания прав доступа роли');
insert into s_actions (nzp_act,act_name,hlp) values (69, 'Получить отчет', 'формирует отчет');
insert into s_actions (nzp_act,act_name,hlp) values (70, 'Расчет', 'выполняет расчет начислений');
insert into s_actions (nzp_act,act_name,hlp) values (71, 'Показать утвержденные показания', 'показывает утвержденные показания приборов учета');
insert into s_actions (nzp_act,act_name,hlp) values (72, 'Показать введенные показания', 'показывает введенные показания приборов учета');
insert into s_actions (nzp_act,act_name,hlp) values (73, 'Выгрузить в Excel', 'выгружает данные в файл электронных таблиц (MS Excel)');
insert into s_actions (nzp_act,act_name,hlp) values (74, 'Перейти в архив', 'переходит в режим просмотра архивных данных');
insert into s_actions (nzp_act,act_name,hlp) values (75, 'Выйти из архива', 'выходит из режима просмотра архивных данных');
insert into s_actions (nzp_act,act_name,hlp) values (76, 'Показать периоды', 'переходит к просмотру всех периодов действия услуги, и соответствующих им поставщиков и формул расчета');
insert into s_actions (nzp_act,act_name,hlp) values (77, 'Показать параметры', 'переходит к просмотру параметров выбранной услуги');
insert into s_actions (nzp_act,act_name,hlp) values (78, 'Копировать', 'Копировать данные');
insert into s_actions (nzp_act,act_name,hlp) values (79, 'Вставить', 'Вставить скопированные данные');
insert into s_actions (nzp_act,act_name,hlp) values (80, 'Показать ЛС', 'Показать лицевые счета');
insert into s_actions (nzp_act,act_name,hlp) values (81, 'Добавить наряд-заказ', 'Добавить наряд-заказ');
insert into s_actions (nzp_act,act_name,hlp) values (82, 'Включить в группу', 'Включить лицевые счета в группу');
insert into s_actions (nzp_act,act_name,hlp) values (83, 'Исключить из группы', 'Исключить лицевые счета из группы');
insert into s_actions (nzp_act,act_name,hlp) values (84, 'К списку заявок', 'Перейти к списку заявок лицевого счета');
insert into s_actions (nzp_act,act_name,hlp) values (85, 'Создать пачку', 'Создать новую пачку с оплатами');
insert into s_actions (nzp_act,act_name,hlp) values (86, 'Закрыть пачку', 'Закрыть пачку оплат');
insert into s_actions (nzp_act,act_name,hlp) values (87, 'Добавить оплату', 'Добавить новую оплату в открытую пачку');
insert into s_actions (nzp_act,act_name,hlp) values (88, 'Загрузить оплаты', 'Загружает электронный файл оплат');
insert into s_actions (nzp_act,act_name,hlp) values (89, 'Найти лицевой счет', 'Переходит к шаблону поиска по адресу');
insert into s_actions (nzp_act,act_name,hlp) values (90, 'Удалить оплату', 'Удаляет выбранные оплаты из пачки');
insert into s_actions (nzp_act,act_name,hlp) values (91, 'Отменить распред.', 'Отменяет распределение оплат');
insert into s_actions (nzp_act,act_name,hlp) values (92, 'Удалить пачку', 'Удаляет пачку');
insert into s_actions (nzp_act,act_name,hlp) values (93, 'Распределить', 'Выполняет распределение оплат');
insert into s_actions (nzp_act,act_name,hlp) values (94, 'Добавить заявку', 'добавляет новую заявку');
insert into s_actions (nzp_act,act_name,hlp) values (95, 'Очистить списки', 'удаляет выбранные списки всех пользователей');
insert into s_actions (nzp_act,act_name,hlp) values (96, 'Добавить пользователя', 'добавляет нового пользователя');
insert into s_actions (nzp_act,act_name,hlp) values (97, 'Отправить', 'отправляет данный наряд-заказ исполнителю');
insert into s_actions (nzp_act,act_name,hlp) values (98, 'Отметить получение', 'отмечает данный наряд-заказ как полученный');
insert into s_actions (nzp_act,act_name,hlp) values (99, 'Сформировать', 'формирует недопоставки по указанным условиям');
insert into s_actions (nzp_act,act_name,hlp) values (100, 'Выставить признак', 'Выставляет признак формирования недопоставки по невыполненным наряд-заказам');
insert into s_actions (nzp_act,act_name,hlp) values (101, 'Снять признак', 'Снимает признак формирования недопоставки по невыполненным наряд-заказам');
insert into s_actions (nzp_act,act_name,hlp) values (102, 'Удалить', 'Удалить запись из журнала');
insert into s_actions (nzp_act,act_name,hlp) values (103, 'Показать параметры', 'переходит к просмотру параметров выбранной записи');
insert into s_actions (nzp_act,act_name,hlp) values (104, 'Реквизиты печати', 'переходит к просмотру реквизитов управляющей организации');
--insert into s_actions (nzp_act,act_name,hlp) values (105, 'Счета подрядчика', 'переходит к просмотру расчетных счетов выбранного подрядчика');
--insert into s_actions (nzp_act,act_name,hlp) values (106, 'Договоры с подряд.', 'переходит к просмотру договоров между выбранными управляющей компанией и подрядчиком');
insert into s_actions (nzp_act,act_name,hlp) values (107, 'Перечислить', 'переходит к перечислению оплат подрядчикам');
insert into s_actions (nzp_act,act_name,hlp) values (108, 'Расчет сальдо', 'выполняет расчет сальдо поставщика');
insert into s_actions (nzp_act,act_name,hlp) values (109, 'Добавить врем. убытие', 'добавляет временное убытие выделенным жильцам');
insert into s_actions (nzp_act,act_name,hlp) values (110, 'Загрузить показания', 'Загружает электронный файл с показаниями приборов учета');
insert into s_actions (nzp_act,act_name,hlp) values (111, 'Оператор', 'показывает показания, введенные оператором');
insert into s_actions (nzp_act,act_name,hlp) values (112, 'С сайта', 'показывает показания, загруженные из электронного файла с показаниями ПУ');
insert into s_actions (nzp_act,act_name,hlp) values (113, 'Перезапустить хостинг', 'Перезапустить хостинг');
insert into s_actions (nzp_act,act_name,hlp) values (114, 'Поместить в портфель', 'Поместить в портфель');
insert into s_actions (nzp_act,act_name,hlp) values (115, 'Убрать из портфеля', 'Исключает выбранную квитанцию об оплате из портфеля');
insert into s_actions (nzp_act,act_name,hlp) values (116, 'Отменить платежи', 'Отменяет все платежи портфеля текущим операционным днем');
insert into s_actions (nzp_act,act_name,hlp) values (117, 'Выполнить', 'Формирует статистику');
insert into s_actions (nzp_act,act_name,hlp) values (118, 'Исправить', 'Выполняет попытку исправления ошибок и повторно распределяет оплату');
insert into s_actions (nzp_act,act_name,hlp) values (119, 'Исправить все', 'Выполняет попытку исправления ошибок и повторно распределяет все оплаты');
insert into s_actions (nzp_act,act_name,hlp) values (120, 'Распределить', 'Выполняет распределение выбранной оплаты');
insert into s_actions (nzp_act,act_name,hlp) values (121, 'Распределить все', 'Выполняет распределение всех оплат');
insert into s_actions (nzp_act,act_name,hlp) values (122, 'Сгенерировать ЛС', 'Выполняет генерацию лицевых счетов по выбранному дому');
insert into s_actions (nzp_act,act_name,hlp) values (123, 'Сгенерировать ПУ', 'Выполняет генерацию приборов учета по выбранному списку лицевых счетов');
insert into s_actions (nzp_act,act_name,hlp) values (124, 'Контроль распред.', 'Контроль распределения оплат');
insert into s_actions (nzp_act,act_name,hlp) values (125, 'Ошибки распределения', 'Выполняет поиск оплат, распределение которых не равно сумме оплаты, и помещает их в корзину с ошибкой "Сумма оплаты не соответствует сумме распределения"');
insert into s_actions (nzp_act,act_name,hlp) values (126, 'Ошибки в оплатах', 'Выявляет учтенные в лицевых счетах оплаты, по которым отсутвует квитанция об оплате, и аннулирует их');
insert into s_actions (nzp_act,act_name,hlp) values (127, 'Открыть квитанцию', 'Открыть квитанцию');
insert into s_actions (nzp_act,act_name,hlp) values (128, 'Версия для печати', 'Отображает версию для печати');
insert into s_actions (nzp_act,act_name,hlp) values (129, 'Обновить адреса', 'Обновляет адресное пространство в центральном банке');
insert into s_actions (nzp_act,act_name,hlp) values (130, 'Закрыть', 'Закрывает наряд-заказ для подрядчика');
insert into s_actions (nzp_act,act_name,hlp) values (131, 'Выполнить проверки', 'Выполняет проверки на возможность закрытия расчетного месяца');
insert into s_actions (nzp_act,act_name,hlp) values (132, 'Закрыть месяц', 'Выполняет проверки на возможность закрытия расчетного месяца и закрывает расчетный месяц');
insert into s_actions (nzp_act,act_name,hlp) values (133, 'Переместить вверх', 'Перемещает выбранную услугу на одну позицию вверх в списке');
insert into s_actions (nzp_act,act_name,hlp) values (134, 'Переместить вниз', 'Перемещает выбранную услугу на одну позицию вниз в списке');
insert into s_actions (nzp_act,act_name,hlp) values (135, 'Редактировать адреса', 'Отображает блок адреса на редактирование');
insert into s_actions (nzp_act,act_name,hlp) values (136, 'Отменить', 'Отменяет получение наряда-заказа');
insert into s_actions (nzp_act,act_name,hlp) values (137, 'Обновить справочники', 'Обновляет справочники из основного банка данных');
insert into s_actions (nzp_act,act_name,hlp) values (140, 'Удалить', 'Удаляет список к перечислению');
insert into s_actions (nzp_act,act_name,hlp) values (141, 'Добавить', 'Добавляет новый приказ на перечисление');
insert into s_actions (nzp_act,act_name,hlp) values (142, 'Добавить список', 'Формирует новый список к перечислению за текущий расчетный месяц');
insert into s_actions (nzp_act,act_name,hlp) values (143, 'Обновить страницу', 'Обновляет данную страницу');
insert into s_actions (nzp_act,act_name,hlp) values (144, 'Открыть соглашение', 'Открывает карточку соглашения');
insert into s_actions (nzp_act,act_name,hlp) values (145, 'Добавить', 'Добавляет соглашение с подрядчиком');
insert into s_actions (nzp_act,act_name,hlp) values (146, 'Удалить', 'Удаляет соглашение с подрядчиком');
insert into s_actions (nzp_act,act_name,hlp) values (147, 'Распределить', 'Выполняет распределение сумм финансирования по лицевым счетам');
insert into s_actions (nzp_act,act_name,hlp) values (148, 'Отменить распред.', 'Выполняет отмену распределения сумм финансирования для выбранного приказа');
insert into s_actions (nzp_act,act_name,hlp) values (149, 'Удалить приказ', 'Выполняет удаление приказа на перечисление');
insert into s_actions (nzp_act,act_name,hlp) values (150, 'Новое сообщение', 'Создает новое сообщение');
insert into s_actions (nzp_act,act_name,hlp) values (151, 'Отправить', 'Отправляет сообщение');
insert into s_actions (nzp_act,act_name,hlp) values (152, 'Удалить', 'Удаляет сообщение');
insert into s_actions (nzp_act,act_name,hlp) values (153, 'Загрузить', 'Загрузить');
insert into s_actions (nzp_act,act_name,hlp) values (154, 'Загрузить акт', 'Загружает акт о фактической поставке из файла');
insert into s_actions (nzp_act,act_name,hlp) values (155, 'Учесть акт', 'Учитывает данные акта по лицевым счетам');
insert into s_actions (nzp_act,act_name,hlp) values (156, 'Загрузить характеристику', 'Загружает техническую характеристику жилого фонда из файла');
insert into s_actions (nzp_act,act_name,hlp) values (157, 'Учесть характеристику', 'Учитывает техническую характеристику жилого фонда по лицевым счетам');
insert into s_actions (nzp_act,act_name,hlp) values (158, 'Удалить', 'Удаляет выбранную запись');
insert into s_actions (nzp_act,act_name,hlp) values (159, 'К распределению', 'Ставит признак готовности к распределению по лицевым счетам недопоставки СУПГ ');
insert into s_actions (nzp_act,act_name,hlp) values (160, 'Распределить', 'Выполняет распределение недопоставок СУПГ по лицевым счетам');
insert into s_actions (nzp_act,act_name,hlp) values (161, 'Удалить кассовый', 'Уадалить кассовый план');
insert into s_actions (nzp_act,act_name,hlp) values (162, 'Удалить помесячный', 'Удалить помесячное распределение');
insert into s_actions (nzp_act,act_name,hlp) values (163, 'Загрузить', 'Загрузить файл');
insert into s_actions (nzp_act,act_name,hlp) values (164, 'Рассчитать', 'Выполняет расчет статистики по начислениям');
insert into s_actions (nzp_act,act_name,hlp) values (165, 'Скопировать', 'Создает новую карточку плановой работы и копирует данные из текущей карточки');
insert into s_actions (nzp_act,act_name,hlp) values (166, 'Разобрать', 'Выполняет разбор');
insert into s_actions (nzp_act,act_name,hlp) values (167, 'Присвоить пл-код', 'Выпоняет присвоение платежного кода лицевым счетам');
insert into s_actions (nzp_act,act_name,hlp) values (168, 'Файл выгрузки', 'Формирует файл выгрузки недопоставок для передачи его в систему КомПлат 2.0');
insert into s_actions (nzp_act,act_name,hlp) values (169, 'Добавить', 'Добавить новую запись');
insert into s_actions (nzp_act,act_name,hlp) values (170, 'Сохранить', 'Сохранить изменения');
insert into s_actions (nzp_act,act_name,hlp) values (171, 'Перераспределить', 'Выполняет перераспределение оплат на выделенные открытые лицевые счета');
insert into s_actions (nzp_act,act_name,hlp) values (172, 'Расчет ср.расх. ИПУ', 'Выполняет расчет средних расходов индивидуальных приборов учета');
insert into s_actions (nzp_act,act_name,hlp) values (173, 'Выполнить', 'выполняет обновление данных');
insert into s_actions (nzp_act,act_name,hlp) values (174, 'Выполнить', 'выполняет операцию');
insert into s_actions (nzp_act,act_name,hlp) values (175, 'Жилец', 'Показывает показания введенные жильцом');
insert into s_actions (nzp_act,act_name,hlp) values (176, 'Добавить формулу', 'Добавляет новую формулу расчета начислений по услуге');
insert into s_actions (nzp_act,act_name,hlp) values (177, 'Расчет расхода', 'Рассчитывает расход ОДПУ');
insert into s_actions (nzp_act,act_name,hlp) values (178, 'Перенести на ЛС', 'Переходит к форме переноса суммы с выбранного лицевого счета на другой лицевой счет');
insert into s_actions (nzp_act,act_name,hlp) values (179, 'Новая операция', 'Очищает форму, подготавливая ее к выполнению новой операции переноса суммы между лицевыми счетами');
insert into s_actions (nzp_act,act_name,hlp) values (180, 'Реестр операций', 'Переходит к форме просмотра реестра операций по изменению сальдо');
insert into s_actions (nzp_act,act_name,hlp) values (181, 'Учесть оплаты', 'Выполняет учет в начислениях распределенных оплат');
insert into s_actions (nzp_act,act_name,hlp) values (182, 'Создать пачку', 'Создает пачку с переплатами');
insert into s_actions (nzp_act,act_name,hlp) values (183, 'Обмен данными', 'Обменивается данными');
insert into s_actions (nzp_act,act_name,hlp) values (184, 'Перенести', 'Переносит все лицевые счета выбранных домов в новую УК');
insert into s_actions (nzp_act,act_name,hlp) values (185, 'Новый пакет', 'Новый пакет для системы платежных поручений банк-клиент');
insert into s_actions (nzp_act,act_name,hlp) values (186, 'Удалить пакет', 'Удаляет пакет из системы платежных поручений банк-клиент');
insert into s_actions (nzp_act,act_name,hlp) values (187, 'Загрузить адреса', 'Загрузить адресное пространство');
insert into s_actions (nzp_act,act_name,hlp) values (188, 'Выгрузить', 'Выгрузить файл');
insert into s_actions (nzp_act,act_name,hlp) values (189, 'Открыть выгрузку', 'Открыть выгрузку');
insert into s_actions (nzp_act,act_name,hlp) values (190, 'Ввод по услугам', 'Открывает форму для ввода изменений по услугам');
insert into s_actions (nzp_act,act_name,hlp) values (192, 'Открыть соглашение', 'Открывает выбранное соглашение');
insert into s_actions (nzp_act,act_name,hlp) values (193, 'Открыть иск', 'Открывает выбранный иск');
insert into s_actions (nzp_act,act_name,hlp) values (194, 'Изменить долг', 'Открывает форму для изменения задолженности');
insert into s_actions (nzp_act,act_name,hlp) values (195, 'Реестр по оплатам', 'Формирует реестр по оплатам для веб-сервиса Небо');
insert into s_actions (nzp_act,act_name,hlp) values (196, 'Настроить ключи', 'Выполняет настройку уникальных ключей');
insert into s_actions (nzp_act,act_name,hlp) values (197, 'Сформировать документы', 'Формирует пакет документов для иска');
insert into s_actions (nzp_act,act_name,hlp) values (198, 'Создать дело', 'Создает новое дело для должника');
insert into s_actions (nzp_act,act_name,hlp) values (199, 'Закрыть дело', 'Меняет статус дело на Закрыто');
insert into s_actions (nzp_act,act_name,hlp) values (200, 'Открыть дело', 'Открывает дело должника');

--для Самары
insert into s_actions (nzp_act,act_name,hlp) values (201, 'Справка нотариусу', 'Справка нотариусу');
--insert into s_actions (nzp_act,act_name,hlp) values (202, 'Справка о составе семьи ф2','Справка о составе семьи ф2');
insert into s_actions (nzp_act,act_name,hlp) values (203, 'Справка в суды','Справка в суды');
insert into s_actions (nzp_act,act_name,hlp) values (204, 'Справка на приватизацию','Справка на приватизацию');
insert into s_actions (nzp_act,act_name,hlp) values (205, 'Справка о прописке','Справка о прописке');
insert into s_actions (nzp_act,act_name,hlp) values (206, 'Лицевой счет','Лицевой счет');
insert into s_actions (nzp_act,act_name,hlp) values (214, 'Заявление о регистрации по месту пребывания', 'Заявление о регистрации по месту пребывания');
insert into s_actions (nzp_act,act_name,hlp) values (215, 'Заявление о снятии с регистрационного учета по месту жительства', 'Заявление о снятии с регистрационного учета по месту жительства');
insert into s_actions (nzp_act,act_name,hlp) values (216, 'Заявление о регистрации по месту жительства', 'Заявление о регистрации по месту жительства');
insert into s_actions (nzp_act,act_name,hlp) values (218, 'Адресный листок убытия', 'Адресный листок убытия');
insert into s_actions (nzp_act,act_name,hlp) values (219, 'Адресный листок прибытия', 'Адресный листок прибытия');
insert into s_actions (nzp_act,act_name,hlp) values (220, 'Сведения о регистрации граждан и снятии с регистрационного учета', 'Сведения о регистрации граждан и снятии с регистрационного учета');
insert into s_actions (nzp_act,act_name,hlp) values (221, 'Список юношей, подлежащих постановке на воинский учет', 'Список юношей, подлежащих постановке на воинский учет');
insert into s_actions (nzp_act,act_name,hlp) values (222, 'Реестр граждан, сменивших или получивших удостоверение личности', 'Реестр граждан, сменивших или получивших удостоверение личности');
insert into s_actions (nzp_act,act_name,hlp) values (223, 'Сведения о регистрации по месту жительства (Форма РФЛ1)', 'Сведения о регистрации гражданина по месту жительства по форме РФЛ1');
insert into s_actions (nzp_act,act_name,hlp) values (224, 'Листок статистического учета прибытия', 'Листок статистического учета прибытия');
insert into s_actions (nzp_act,act_name,hlp) values (225, 'Реестр граждан', 'Универсальный реестр граждан, построенный по выбранному списку карточек жильцов');
insert into s_actions (nzp_act,act_name,hlp) values (226, 'Заявление о выдаче/замене паспорта', 'Заявление о выдаче/замене паспорта');
insert into s_actions (nzp_act,act_name,hlp) values (227, 'Листок статистического учета выбытия', 'Листок статистического учета выбытия');
insert into s_actions (nzp_act,act_name,hlp) values (229, 'Карточка аналитического учета', 'Карта аналитического учета');
insert into s_actions (nzp_act,act_name,hlp) values (230, 'Сверка расчетов с жильцом', 'Сверка расчетов с жильцом');
insert into s_actions (nzp_act,act_name,hlp) values (231, 'Расшифровка по домам - общая', 'Расшифровка по домам - общая');
insert into s_actions (nzp_act,act_name,hlp) values (232, 'Справка по поставщикам коммунальных услуг (Форма 2)', 'Справка по поставщикам коммунальных услуг (Форма 2)');
insert into s_actions (nzp_act,act_name,hlp) values (233, 'Справка о наличии долга', 'Справка о наличии долга');
insert into s_actions (nzp_act,act_name,hlp) values (234, 'Справка по отключениям подачи коммунальных услуг', 'Справка по отключениям подачи коммунальных услуг');
insert into s_actions (nzp_act,act_name,hlp) values (235, 'Справка для предъявления в суд', 'Справка для предъявления в суд');
insert into s_actions (nzp_act,act_name,hlp) values (236, 'Справка по лицевому счету (Excel)', 'Справка по лицевому счету (Excel)');
insert into s_actions (nzp_act,act_name,hlp) values (238, 'Архивная справка', 'Архивная справка');
insert into s_actions (nzp_act,act_name,hlp) values (239, 'Справка по поставщикам коммунальных услуг (Форма 1)', 'Справка по поставщикам коммунальных услуг (Форма 1)');
insert into s_actions (nzp_act,act_name,hlp) values (240, 'Извещение за месяц', 'Извещение за месяц');
insert into s_actions (nzp_act,act_name,hlp) values (242, 'Справка с места жительства', 'Справка с места жительства');
insert into s_actions (nzp_act,act_name,hlp) values (243, 'Справка по запросам', 'Справка по запросам');
insert into s_actions (nzp_act,act_name,hlp) values (248, 'Справка о начислениях по квартирным приборам учета', 'Справка о начислениях по квартирным приборам учета');
insert into s_actions (nzp_act,act_name,hlp) values (249, 'Сводная ведомость по нормативам потребления КУ', 'Сводная ведомость по нормативам потребления КУ');
insert into s_actions (nzp_act,act_name,hlp) values (250, 'Отчет о жилом фонде', 'Отчет о жилом фонде');
insert into s_actions (nzp_act,act_name,hlp) values (251, 'Расшифровка по домам - начислено', 'Расшифровка по домам - начислено');
insert into s_actions (nzp_act,act_name,hlp) values (253, 'Список должников с указанием срока задолженности', 'Список должников с указанием срока задолженности');
insert into s_actions (nzp_act,act_name,hlp) values (254, 'Сведения о задолженности', 'Сведения о задолженности');
insert into s_actions (nzp_act,act_name,hlp) values (255, 'Справка о начислении платы по видам услуг', 'Справка о начислении платы по видам услуг');
insert into s_actions (nzp_act,act_name,hlp) values (256, 'Расшифровка по домам с ОДПУ', 'Расшифровка по домам с ОДПУ');
insert into s_actions (nzp_act,act_name,hlp) values (257, 'Ведомость оплаты', 'Ведомость оплаты');
insert into s_actions (nzp_act,act_name,hlp) values (258, 'Ведомость разовых начислений', 'Ведомость разовых начислений');
insert into s_actions (nzp_act,act_name,hlp) values (259, 'Формирование квитанций', 'Формирование квитанций для списка лицевых счетов');
insert into s_actions (nzp_act,act_name,hlp) values (260, 'Протокол расчета общедомового поправочного коэффициента', 'Протокол расчета общедомового поправочного коэффициента');
insert into s_actions (nzp_act,act_name,hlp) values (261, 'Карточка аналитического учета по услуге "содержание жилья"', 'Карта аналитического учета по услуге "содержание жилья"');
insert into s_actions (nzp_act,act_name,hlp) values (268, 'Справка для незарегистрированного собственника', 'Справка для незарегистрированного собственника');
insert into s_actions (nzp_act,act_name,hlp) values (269, 'Лицевой счет без долга','Лицевой счет без долга');
insert into s_actions (nzp_act,act_name,hlp) values (270, 'Справка по услугам группы "содержание жилья"','Справка по услугам группы "содержание жилья"');
insert into s_actions (nzp_act,act_name,hlp) values (271, 'Справка по отключениям подачи коммунальных услуг по домам', 'Справка по отключениям подачи коммунальных услуг по домам');
insert into s_actions (nzp_act,act_name,hlp) values (272, 'Справка по отключениям подачи коммунальных услуг сводная по ЖЭУ', 'Справка по отключениям подачи коммунальных услуг сводная по ЖЭУ');
insert into s_actions (nzp_act,act_name,hlp) values (283, 'Список жильцов модифицируемый', 'Список жильцов, построенный по выбранному списку карточек жильцов с возможностью выбора полей для отображения');
insert into s_actions (nzp_act,act_name,hlp) values (284, 'Архивная справка-2', 'Архивная справка');
insert into s_actions (nzp_act,act_name,hlp) values (285, 'Сверка поступлений за день', 'Сверка поступлений за день');
insert into s_actions (nzp_act,act_name,hlp) values (286, 'Сверка поступлений за период', 'Сверка поступлений за период');
insert into s_actions (nzp_act,act_name,hlp) values (300, 'Выписка из ЛС о поданных показаниях ИПУ', 'Выписка из ЛС');
insert into s_actions (nzp_act,act_name,hlp) values (301, 'Справка на приватизацию2', 'Справка на приватизацию2');
insert into s_actions (nzp_act,act_name,hlp) values (302, 'Протокол расчета значений ОДН расширенный', 'Протокол расчета ОДН');
insert into s_actions (nzp_act,act_name,hlp) values (305, 'Сверка поступлений за месяц', 'Сверка поступлений за месяц');
insert into s_actions (nzp_act,act_name,hlp) values (306, 'Сальдо в банк', 'Сальдо в банк');
insert into s_actions (nzp_act,act_name,hlp) values (309, 'Справка по поставщикам коммунальных услуг (Форма 3)', 'Справка по поставщикам коммунальных услуг (Форма 3)');
insert into s_actions (nzp_act,act_name,hlp) values (310, 'Справка о начислении платы по видам услуг (Форма 2)', 'Справка о начислении платы по видам услуг (Форма 2)');
insert into s_actions (nzp_act,act_name,hlp) values (312, 'Расписка в получении документов жильца', 'Расписка в получении документов жильца');
insert into s_actions (nzp_act,act_name,hlp) values (313, 'Свидетельство о регистрации по месту пребывания', 'Свидетельство о регистрации по месту пребывания');
insert into s_actions (nzp_act,act_name,hlp) values (320, 'Выгрузка показаний ПУ', 'Выгрузка показаний ПУ');
insert into s_actions (nzp_act,act_name,hlp) values (327, 'Сверка характеристик лицевых счетов и домов', 'Протокол сверки характеристик лицевых счетов и домов');
insert into s_actions (nzp_act,act_name,hlp) values (329, 'Настройка расчета', 'Настройка расчета');

--для Губкина
insert into s_actions (nzp_act,act_name,hlp) values (303, 'Выгрузка в Кассу 3.0', 'Выгрузка в Кассу 3.0');
insert into s_actions (nzp_act,act_name,hlp) values (304, 'Платежи.Ежедневная информация по платежам за ЖКУ по г.Губкину', 'Ежедневная информация по платежам за ЖКУ');
insert into s_actions (nzp_act,act_name,hlp) values (307, 'Состояние текущих начислений по домам', 'Состояние текущих начислений по домам');
insert into s_actions (nzp_act,act_name,hlp) values (308, 'Итоги оплат по домам (ЕПД)', 'Итоги оплат по домам (ЕПД)');
insert into s_actions (nzp_act,act_name,hlp) values (311, 'Статистика состояния жилищного фонда', 'Статистика состояния жилфонда');
insert into s_actions (nzp_act,act_name,hlp) values (314, 'Справка с места жительства', 'Справка с места жительства');
insert into s_actions (nzp_act,act_name,hlp) values (315, 'Выписка из лицевого счета', 'Выписка из лицевого счета');
insert into s_actions (nzp_act,act_name,hlp) values (316, 'Заявление на регистрацию', 'Заявление на регистрацию');
insert into s_actions (nzp_act,act_name,hlp) values (317, 'Статистика начислений по лицевым счетам', 'Статистика начислений по лицевым счетам');
insert into s_actions (nzp_act,act_name,hlp) values (318, 'Статистика начислений по домам', 'Статистика начислений по домам');
insert into s_actions (nzp_act,act_name,hlp) values (319, 'Статистика начислений по участкам', 'Статистика начислений по участкам');
insert into s_actions (nzp_act,act_name,hlp) values (323, 'Выгрузка начислений в орган социальной защиты населения', 'Выгрузка начислений в орган социальной защиты населения');

--для Зеленодольска
insert into s_actions (nzp_act,act_name,hlp) values (207, 'Финансово-лицевой счет','Финансово-лицевой счет');
insert into s_actions (nzp_act,act_name,hlp) values (208, 'Справка о составе семьи','Справка о составе семьи');
insert into s_actions (nzp_act,act_name,hlp) values (209, 'Справка по начислениям','Справка по начислениям');
insert into s_actions (nzp_act,act_name,hlp) values (210, 'Справка о задолженности','Справка о задолженности');
insert into s_actions (nzp_act,act_name,hlp) values (211, 'Выписка из домовой книги','Выписка из домовой книги');
insert into s_actions (nzp_act,act_name,hlp) values (217, 'Финансовый лицевой счет №__', 'Финансовый лицевой счет №__');
insert into s_actions (nzp_act,act_name,hlp) values (330, 'Справка на проживание и состав семьи', 'Справка на проживание и состав семьи');
insert into s_actions (nzp_act,act_name,hlp) values (331, 'Справка о смерти (г.Казань)', 'Справка о смерти (г.Казань)');
insert into s_actions (nzp_act,act_name,hlp) values (332, 'Информация о временно зарегистрированных', 'Информация о временно зарегистрированных');
insert into s_actions (nzp_act,act_name,hlp) values (333, 'Информация о собственниках', 'Информация о собственниках');
insert into s_actions (nzp_act,act_name,hlp) values (334, 'Список жильцов для военкомата', 'Список жильцов для военкомата');
insert into s_actions (nzp_act,act_name,hlp) values (335, 'Выписка на жилое помещение', 'Выписка на жилое помещение');
insert into s_actions (nzp_act,act_name,hlp) values (336, 'Выписка из домовой книги для горгаза', 'Выписка из домовой книги для горгаза');

--для РТ, Зеленодольска, Казани
insert into s_actions (nzp_act,act_name,hlp) values (244, 'Оплата гражданами-получателями коммунальных услуг за поставленные услуги', 'Оплата гражданами-получателями коммунальных услуг за поставленные услуги');
insert into s_actions (nzp_act,act_name,hlp) values (245, 'Информация  по расчетам с населением', 'Информация  по расчетам с населением');
insert into s_actions (nzp_act,act_name,hlp) values (246, 'Акт сверки по энергоснабжению', 'Акт сверки по энергоснабжению');
insert into s_actions (nzp_act,act_name,hlp) values (262, 'Оплата гражданами-получателями коммунальных услуг за поставленные услуги (сводный)', 'Оплата гражданами-получателями коммунальных услуг за поставленные услуги (сводный)');
insert into s_actions (nzp_act,act_name,hlp) values (324, '50.1 Сальдовая ведомость', '50.1 Сальдовая ведомость');
insert into s_actions (nzp_act,act_name,hlp) values (325, '50.2 Ведомость должников', '50.2 Ведомость должников');

--Астрахань
insert into s_actions (nzp_act,act_name,hlp) values (321, 'Выгрузка начислений в банк', 'Выгрузка начислений в банк');

--Тула
--insert into s_actions (nzp_act,act_name,hlp) values (322, 'Выгрузка реестра для загрузки в БС', 'Выгрузка реестра для загрузки в БС');

--Общие
insert into s_actions (nzp_act,act_name,hlp) values (212, 'Сальдовая ведомость по услугам 5.10','Сальдовая ведомость по услугам 5.10');
insert into s_actions (nzp_act,act_name,hlp) values (213, 'Сальдовая ведомость по услугам 5.20','Сальдовая ведомость по услугам 5.20');
insert into s_actions (nzp_act,act_name,hlp) values (228, 'Генератор по параметрам', 'Генератор отчетов по параметрам жилья');
insert into s_actions (nzp_act,act_name,hlp) values (237, 'Карточка регистрации', 'Карточка регистрации');
insert into s_actions (nzp_act,act_name,hlp) values (241, 'Поквартирная карточка', 'Поквартирная карточка');
insert into s_actions (nzp_act,act_name,hlp) values (247, 'Генератор по начислениям', 'Генератор по начислениям');
insert into s_actions (nzp_act,act_name,hlp) values (263, '1.5 Список заявок', '1.5 Список заявок');
insert into s_actions (nzp_act,act_name,hlp) values (264, '2.1 Количество нарядов - заказов по услугам', '2.1 Количество нарядов - заказов по услугам');
insert into s_actions (nzp_act,act_name,hlp) values (265, '2.2 Количество нарядов - заказов по подрядчикам', '2.2 Количество нарядов - заказов по подрядчикам');
insert into s_actions (nzp_act,act_name,hlp) values (273, '3.1 Сведения по отключениям услуг по поставщикам', 'Сведения по отключениям услуг по поставщикам');
insert into s_actions (nzp_act,act_name,hlp) values (274, '3.2 Сведения по отключениям услуг', 'Сведения по отключениям услуг');
insert into s_actions (nzp_act,act_name,hlp) values (275, '3.3 Акты по отключениям услуг', 'Акты по отключениям услуг');
insert into s_actions (nzp_act,act_name,hlp) values (276, '2.3 Список нарядов-заказов по неисправностям', 'Список нарядов-заказов по неисправностям');
insert into s_actions (nzp_act,act_name,hlp) values (266, 'Сальдовая оборотная ведомость (10.14.3)', 'Сальдовая оборотная ведомость (10.14.3)');
insert into s_actions (nzp_act,act_name,hlp) values (267, 'Сальдовая оборотная ведомость по поставщикам (10.14.1)', 'Сальдовая оборотная ведомость по поставщикам (10.14.1)');
insert into s_actions (nzp_act,act_name,hlp) values (277, '1.4 Список невыполненных нарядов-заказов к концу периода', '1.4 Список невыполненных нарядов-заказов к концу периода');
insert into s_actions (nzp_act,act_name,hlp) values (278, '1.2 Приложение к информации, полученной ОДДС', '1.2 Приложение к информации, полученной ОДДС');
insert into s_actions (nzp_act,act_name,hlp) values (279, '1.1 Информация, полученная ОДДС', '1.1 Информация, полученная ОДДС');
insert into s_actions (nzp_act,act_name,hlp) values (280, '2.4 Количество переадресаций заявок, принятых ОДДС', '2.4 Количество переадресаций заявок, принятых ОДДС');
insert into s_actions (nzp_act,act_name,hlp) values (281, '1.3.1 Список сообщений, зарегистрированных ОДДС', '1.3.1 Список сообщений, зарегистрированных ОДДС');
insert into s_actions (nzp_act,act_name,hlp) values (282, '1.3.2 Список сообщений, зарегистрированных ОДДС(опрос)', '1.3.2 Список сообщений, зарегистрированных ОДДС(опрос)');
insert into s_actions (nzp_act,act_name,hlp) values (287, 'Сводная информация по распределению оплат за период', 'Сводная информация по распределению оплат за период');
insert into s_actions (nzp_act,act_name,hlp) values (288, 'Реестр счетчиков по лицевым счетам', 'Реестр счетчиков по лицевым счетам');
insert into s_actions (nzp_act,act_name,hlp) values (289, 'Начисления по поставщикам', 'Начисления по поставщикам');
insert into s_actions (nzp_act,act_name,hlp) values (290, 'Сальдовая оборотная ведомость по услугам для всех поставщиков', 'Сальдовая оборотная ведомость по услугам для всех поставщиков');
insert into s_actions (nzp_act,act_name,hlp) values (291, 'Сводная ведомость начислений и оплат в разрезе поставщиков', 'Сводная ведомость начислений и оплат в разрезе поставщиков');
insert into s_actions (nzp_act,act_name,hlp) values (292, '4.16. Данные приборов учета по жилым домам', '4.16. Данные приборов учета по жилым домам');
insert into s_actions (nzp_act,act_name,hlp) values (293, '4.18. Отчет по расходу на дома', '4.18. Отчет по расходу на дома');
insert into s_actions (nzp_act,act_name,hlp) values (294, 'Протокол рассчитанных значений ОДН', 'Протокол рассчитанных значений ОДН');
insert into s_actions (nzp_act,act_name,hlp) values (295, 'Рассогласование с паспортисткой', 'Рассогласование с паспортисткой');
insert into s_actions (nzp_act,act_name,hlp) values (296, 'Печать', 'формирует отчет о сформированныъ недопоставках');
insert into s_actions (nzp_act,act_name,hlp) values (297, 'Статистика по начислениям', 'формирует статистику по начислениям');
insert into s_actions (nzp_act,act_name,hlp) values (298, 'Сальдо по перечислениям', 'формирует сальдо по перечислениям');
insert into s_actions (nzp_act,act_name,hlp) values (299, 'Справка по поставщикам коммунальных услуг форма 3', 'Справка по поставщикам коммунальных услуг форма 3');
insert into s_actions (nzp_act,act_name,hlp) values (340, '3.70 Сводный отчет по начислениям', '3.70 Сводный отчет по начислениям'); -- для Тулы
insert into s_actions (nzp_act,act_name,hlp) values (341, '3.71 Сводный отчет по поступлениям', '3.71 Сводный отчет по поступлениям'); -- для Тула
insert into s_actions (nzp_act,act_name,hlp) values (342, 'Выписка по счетчикам', 'Выписка по счетчикам'); -- для Самары
insert into s_actions (nzp_act,act_name,hlp) values (346, 'Реестр квитанций по домам', 'Реестр квитанций по домам');
insert into s_actions (nzp_act,act_name,hlp) values (347, 'Сведения о должниках', 'Сведения о должниках'); --Тула
insert into s_actions (nzp_act,act_name,hlp) values (348, 'Справка по должникам', 'Справка по должникам'); --Тула
insert into s_actions (nzp_act,act_name,hlp) values (349, 'Справочник поставщиков', 'Справочник поставщиков'); --Тула
insert into s_actions (nzp_act,act_name,hlp) values (350, 'Сведения по поступлениям и перечислениям', 'Сведения по поступлениям и перечислениям'); --Тула


insert into s_actions (nzp_act,act_name,hlp) values (501, 'Шаблон по адресам',    'учитывает при поиске данные, введенные в шаблоне поиска адресов');
insert into s_actions (nzp_act,act_name,hlp) values (502, 'Шаблон по параметрам', 'учитывает при поиске данные, введенные в шаблоне поиска характеристик жилья');
insert into s_actions (nzp_act,act_name,hlp) values (503, 'Шаблон по начислениям','учитывает при поиске данные, введенные в шаблоне поиска начислений и расходов');
insert into s_actions (nzp_act,act_name,hlp) values (504, 'Шаблон по жильцам',    'учитывает при поиске данные, введенные в шаблоне поиска житилей');
insert into s_actions (nzp_act,act_name,hlp) values (505, 'Шаблон по показаниям', 'учитывает при поиске данные, введенные в шаблоне поиска показаний приборов учета');
insert into s_actions (nzp_act,act_name,hlp) values (506, 'Шаблон недопоставок',  'учитывает при поиске данные, введенные в шаблоне поиска недопоставок');
insert into s_actions (nzp_act,act_name,hlp) values (507, 'Шаблон ОДН',           'учитывает при поиске данные, введенные в шаблоне поиска адресов');
insert into s_actions (nzp_act,act_name,hlp) values (508, 'Шаблон по услугам', 'учитывает при поиске данные, введенные в шаблоне поиска по услугам и поставщикам');
insert into s_actions (nzp_act,act_name,hlp) values (509, 'Шаблон по заявкам', 'учитывает при поиске данные, введенные в шаблоне поиска по заявкам');
insert into s_actions (nzp_act,act_name,hlp) values (510, 'Шаблон по группам', 'учитывает при поиске данные, введенные в шаблоне поиска по группам лицевых счетов');
insert into s_actions (nzp_act,act_name,hlp) values (511, 'Шаблон по оплатам', 'учитывает при поиске данные, введенные в шаблоне поиска по оплатам');
insert into s_actions (nzp_act,act_name,hlp) values (512, 'Шаблон по плановым работам', 'учитывает при поиске данные, введенные в шаблоне поиска по плановым работам');
insert into s_actions (nzp_act,act_name,hlp) values (513, 'Шаблон по долгам', 'учитывает при поиске данные, введенные в шаблоне поиска по долгам');
insert into s_actions (nzp_act,act_name,hlp) values (514, 'Шаблон по делам', 'учитывает при поиске данные, введенные в шаблоне поиска по делам должников');
insert into s_actions (nzp_act,act_name,hlp) values (520, 'По месяцам',    'выполняет выборку данных в помесячном разрезе');
insert into s_actions (nzp_act,act_name,hlp) values (521, 'По услугам',    'выполняет выборку данных в разрезе услуг');
insert into s_actions (nzp_act,act_name,hlp) values (522, 'По поставщикам','выполняет выборку данных в разрезе поставщиков');
insert into s_actions (nzp_act,act_name,hlp) values (523, 'По формулам',    'выполняет выборку данных разрезе формул расчета услуг');
insert into s_actions (nzp_act,act_name,hlp) values (524, 'По управляющим организациям','выполняет выборку данных разрезе управляющих организаций');
insert into s_actions (nzp_act,act_name,hlp) values (525, 'По отделениям',    'выполняет выборку данных в разрезе участков');
insert into s_actions (nzp_act,act_name,hlp) values (526, 'По банкам данных','выполняет выборку данных в разрезе банков данных');
insert into s_actions (nzp_act,act_name,hlp) values (527, 'По домам',    'выполняет выборку данных в подомовом разрезе');
insert into s_actions (nzp_act,act_name,hlp) values (528, 'Сальдовые',    'выполняет отображение сальдовых сумм');
insert into s_actions (nzp_act,act_name,hlp) values (529, 'Норматив/прибор учета','');
insert into s_actions (nzp_act,act_name,hlp) values (530, 'Сейчас на сайте','показывает только пользователей, находящихся сейчас на сайте');
insert into s_actions (nzp_act,act_name,hlp) values (531, 'Заблокированные','показывает только заблокированные записи');
insert into s_actions (nzp_act,act_name,hlp) values (532, 'В очереди','показывает зарегистрированные процессы, находящиеся в очереди на исполнение');
insert into s_actions (nzp_act,act_name,hlp) values (533, 'Выполняется','показывает выполняющиеся процессы');
insert into s_actions (nzp_act,act_name,hlp) values (534, 'Выполнен','показыает успешно выполненные процессы');
insert into s_actions (nzp_act,act_name,hlp) values (535, 'Есть ошибки','показывает процессы с ошибками в результате выполнения');
insert into s_actions (nzp_act,act_name,hlp) values (536, 'По плательщикам', 'группирует записи по плательщикам');
insert into s_actions (nzp_act,act_name,hlp) values (537, 'По банкам', 'группирует записи по банкам');
insert into s_actions (nzp_act,act_name,hlp) values (538, 'По датам', 'группирует записи по датам');
insert into s_actions (nzp_act,act_name,hlp) values (539, 'По районам', 'группирует записи по районам');
insert into s_actions (nzp_act,act_name,hlp) values (540, 'Перейти к делу', 'переходит к форме дела');
insert into s_actions (nzp_act,act_name,hlp) values (541, 'Сформировать пачки', 'Формирует пачки');
insert into s_actions (nzp_act,act_name,hlp) values (542, 'Портал', 'показывает показания, введенные с портала');
insert into s_actions (nzp_act,act_name,hlp) values (543, 'Утвердить', 'Переносит выбранные показания в утвержденные');
insert into s_actions (nzp_act,act_name,hlp) values (544, 'Учесть к перечислению', 'Учесть к перечислению');
insert into s_actions (nzp_act,act_name,hlp) values (545, 'Закрыть месяц', 'Запускает процессы подготовки данных и закрывает месяц');
insert into s_actions (nzp_act,act_name,hlp) values (546, 'Перезапустить', 'Перезапустить задачу');
insert into s_actions (nzp_act,act_name,hlp) values (547, 'Отменить', 'Отменить задачу');
insert into s_actions (nzp_act,act_name,hlp) values (548, 'Закрыть день', 'Закрывает операционный день');
insert into s_actions (nzp_act,act_name,hlp) values (549, 'Вернуться назад', 'Возвращается на предыдущий операционный день');
insert into s_actions (nzp_act,act_name,hlp) values (550, 'Перераспределить', 'Повторно распределяет оплаты, считающиеся распределенными, но без расщепления по услугам');
insert into s_actions (nzp_act,act_name,hlp) values (551, 'Скопировать в собст.', 'Копирует данные по жильцу в список собственников');
insert into s_actions (nzp_act,act_name,hlp) values (552, 'Сделать ответственным', 'проверяет наличие помеченной карточки, задает вопрос на подтверждение');
insert into s_actions (nzp_act,act_name,hlp) values (553, 'Удалить данные', 'Удаляет данные');
insert into s_actions (nzp_act,act_name,hlp) values (554, 'Обновить статус', 'Обновляет статус выбранной пачке');
insert into s_actions (nzp_act,act_name,hlp) values (555, 'Скачать', 'Скачать файл');
insert into s_actions (nzp_act,act_name,hlp) values (601, 'Сортировать по адресу',    'сортирует список по адресу');
insert into s_actions (nzp_act,act_name,hlp) values (602, 'Сортировать по лс',    'сортирует список по лицевым счетам');
insert into s_actions (nzp_act,act_name,hlp) values (603, 'Сортировать по улице',    'сортирует список по улице');
insert into s_actions (nzp_act,act_name,hlp) values (604, 'Сортировать по управл. орг.','сортирует список по управляющим организациям');
insert into s_actions (nzp_act,act_name,hlp) values (605, 'Сортировать по услуге',    'сортирует список по услугам');
insert into s_actions (nzp_act,act_name,hlp) values (606, 'Сортировать по поставщику','сортирует список по поставщикам');
insert into s_actions (nzp_act,act_name,hlp) values (607, 'Сортировать по ФИОДР',    'сортирует список по людям');
insert into s_actions (nzp_act,act_name,hlp) values (608, 'Сортировать по логину', 'сортирует список по логину');
insert into s_actions (nzp_act,act_name,hlp) values (609, 'Сортировать по имени пользователя', 'сортирует список по имени пользователя');
insert into s_actions (nzp_act,act_name,hlp) values (610, 'На просмотр',        'открывает данные на просмотр');
insert into s_actions (nzp_act,act_name,hlp) values (611, 'Для изменения',        'открывает данные для редактирования');
insert into s_actions (nzp_act,act_name,hlp) values (612, 'Сортировать по дате последнего посещения', 'сортирует пользователей по убыванию даты последнего посещения');
insert into s_actions (nzp_act,act_name,hlp) values (701, 'Выводить по 20 записей','выводить список по 20 записей');
insert into s_actions (nzp_act,act_name,hlp) values (702, 'Выводить по 50 записей','выводить список по 50 записей');
insert into s_actions (nzp_act,act_name,hlp) values (703, 'Выводить по 100 записей','выводить список по 100 записей');
insert into s_actions (nzp_act,act_name,hlp) values (704, 'Выводить все записи','выводить все записи на странице');
insert into s_actions (nzp_act,act_name,hlp) values (705, 'Выводить по 10 записей','выводить список по 10 записей');
insert into s_actions (nzp_act,act_name,hlp) values (721, 'Управляющая компания / Улица / Дом',  'отображает данные в разрезе управляющих компаний');
insert into s_actions (nzp_act,act_name,hlp) values (722, 'Банк данных / Участок / Улица / Дом', 'отображает данные в разрезе банков данных');
insert into s_actions (nzp_act,act_name,hlp) values (723, 'Улица / Дом',                           'отображает данные в разрезе улиц');
insert into s_actions (nzp_act,act_name,hlp) values (724, 'Поставщик / Услуга / Формула',        'отображает данные в разрезе поставщиков услуг');
insert into s_actions (nzp_act,act_name,hlp) values (725, 'Услуга / Поставщик / Управляющая компания / Участок ','отображает данные в разрезе услуг');
insert into s_actions (nzp_act,act_name,hlp) values (726, 'Управляющая компания / Поставщик / Услуга / Формула', 'отображает данные в разрезе УК и поставщиков');
insert into s_actions (nzp_act,act_name,hlp) values (801, 'Новый поиск', 'выполняет новый поиск данных');
insert into s_actions (nzp_act,act_name,hlp) values (802, 'Поиск по выбранным лс', 'выполняет поиск данных по ранее выбранному списку счетов');
insert into s_actions (nzp_act,act_name,hlp) values (803, 'Поиск по выбранным домам', 'выполняет поиск данных по ранее выбранному списку домов');
insert into s_actions (nzp_act,act_name,hlp) values (851, 'Убывших', '');
insert into s_actions (nzp_act,act_name,hlp) values (852, 'Историю', '');
insert into s_actions (nzp_act,act_name,hlp) values (853, 'Удаленных', 'позоволяет просматривать удаленные карточки жильцов');
insert into s_actions (nzp_act,act_name,hlp) values (854, 'Архив', 'позоволяет просматривать карточки жильцов из архива');
insert into s_actions (nzp_act,act_name,hlp) values (861, 'Карточка жильца', 'открывает карточку жильца');
insert into s_actions (nzp_act,act_name,hlp) values (862, 'Периоды убытия', 'открывает периоды временного убытия жильца');
insert into s_actions (nzp_act,act_name,hlp) values (863, 'Досье', 'открывает досье жильца');
insert into s_actions (nzp_act,act_name,hlp) values (870, 'Стандартный формат', 'описывает формат загрузки информации в систему (характеристики жилого фонда, начисления и др.');
insert into s_actions (nzp_act,act_name,hlp) values (871, 'Генерация П.кодов', 'Запускает фоновую процедуру генерации платежных кодов');
insert into s_actions (nzp_act,act_name,hlp) values (875, 'Пересчитать комиссию', 'Запускает фоновую процедуру');

--Тула
--insert into s_actions (nzp_act,act_name,hlp) values (322, 'Выгрузка реестра для загрузки в БС', 'Выгрузка реестра для загрузки в БС');

--в разработке
insert into s_actions (nzp_act,act_name,hlp) values (191, 'Сальдовый реестр', 'Формирует сальдовый реестр для веб-сервиса Небо');
insert into s_actions (nzp_act,act_name,hlp) values (326, 'Сверка характеристик и начислений', 'Протокол сверка характеристик лицевых счетов и начислений с данными ЕИРЦ');
insert into s_actions (nzp_act,act_name,hlp) values (328, 'Рассогласование с паспортисткой', 'Рассогласование с паспортисткой');
insert into s_actions (nzp_act,act_name,hlp) values (337, 'Справка для незарегистрированного собственника', 'Справка для незарегистрированного собственника'); -- для Губкина
insert into s_actions (nzp_act,act_name,hlp) values (338, 'Справка на приватизацию2', 'Справка на приватизацию2'); -- для Губкина
insert into s_actions (nzp_act,act_name,hlp) values (339, 'Справка по запросам', 'Справка по запросам'); -- для Губкина
insert into s_actions (nzp_act,act_name,hlp) values (343, 'Общий', 'Общий'); -- по Должникам
insert into s_actions (nzp_act,act_name,hlp) values (344, 'Досудебной работы', 'Досудебной работы'); -- по Должникам
insert into s_actions (nzp_act,act_name,hlp) values (345, 'Судебной работы', 'Судебной работы'); -- по Должникам
insert into s_actions (nzp_act,act_name,hlp) values (351, 'Выписка из лицевого счета', 'Выписка из лицевого счета'); --Тула

insert into s_actions (nzp_act,act_name,hlp) values (876, 'Редактировать пачку', 'Открывает не распределенную пачку на редактирование');

--картинки к действиям
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 1, 'binoculars.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 2, 'edit_clear.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 3, 'folder_open.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 4, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 5, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 6, 'edit_clear.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 7, 'show_map.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 8, 'print.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 9, 'lock_closed.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 10, 'play.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 11, 'pause.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 12, 'delete.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 13, 'delete.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 14, 'folder_open.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 15, 'delete.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 16, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 17, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 18, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 19, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 21, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 22, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 23, 'user_male_add.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 24, 'add_new_ls.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 25, 'addhome.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 26, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 51, 'edit_state.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 61, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 62, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 63, 'edit_state.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 64, 'delete.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 65, 'calc32.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 66, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 67, 'archive.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 68, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 69, 'print.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 70, 'calc32.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 73, 'file_excel.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 74, 'archive.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 75, 'archive_out.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 76, 'archive.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 77, 'archive.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 78, 'copy.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 79, 'paste.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 80, 'showls.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 81, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 82, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 83, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 84, 'go_back.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 85, 'add_pack.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 86, 'close_pack.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 87, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 88, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 89, 'binoculars.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 90, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 91, 'go_back.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 92, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 93, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 94, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 95, 'edit_clear.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 96, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 97, 'send.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 98, 'check2.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 99, 'operations.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 100, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 101, 'panel_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 102, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 103, 'archive.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 104, 'bill32.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 107, 'table.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 108, 'calc32.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 109, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 110, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 113, 'restart.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 114, 'incase.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 115, 'outcase.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 116, 'cancelpay.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 117, 'action.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 118, 'repair.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 119, 'repair.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 120, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 121, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 122, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 123, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 124, 'print.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 125, 'search_warning.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 126, 'search_warning.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 127, 'receipt.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 128, 'print.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 129, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 130, 'close_zakaz.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 131, 'check_task.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 132, 'close_month.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 133, 'moveup.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 134, 'movedown.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 135, 'check_task.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 136, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 137, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 140, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 141, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 142, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 143, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 144, 'folder_open.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 145, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 146, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 147, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 148, 'go_back.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 149, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 150, 'new_sms.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 151, 'send.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 152, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 153, 'operations.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 154, 'uploadfile.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 155, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 156, 'uploadfile.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 157, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 158, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 159, 'success.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 160, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 161, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 162, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 163, 'uploadfile.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 164, 'charge.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 165, 'copy2.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 166, 'edit_state.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 167, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 168, 'incase.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 169, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 170, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 171, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 172, 'calc32.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 173, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 174, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 176, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 177, 'calc32.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 178, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 179, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 180, 'folder_open.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 181, 'play.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 182, 'add_pack.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 183, 'edit_state.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 184, 'play.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 185, 'add_pack.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 186, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 187, 'uploadfile.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 188, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 190, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 191, 'uploadfile.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 192, 'folder_open.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 193, 'folder_open.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 194, 'repair.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 195, 'uploadfile.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 196, 'play.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 296, 'print.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 197, 'add_new_ls.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 198, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 199, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 200, 'folder_open.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 551, 'copy2.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 552, 'send.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 540, 'go_back.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 541, 'add_pack.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 543, 'min_window.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 544, 'play.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 545, 'play.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 546, 'restart.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 547, 'delete.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 548, 'go_back.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 549, 'close_month.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 550, 'search_warning.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 553, 'del.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 554, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 555, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 871, 'play.png');

insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 875, 'calc32.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 876, 'edit.png');

-------------Создание табл. Report (отчеты)---------------------------------------------------------------------------------------------------------------------
--drop table report;
create table if not exists report
(
	nzp_rep serial not null,
	nzp_act int,
	name varchar(255),
	file_name varchar(255)
);

drop index if exists ix_report_1;
CREATE UNIQUE INDEX ix_report_1 on report(nzp_rep);

delete from report where 1=1;

--Самара   
insert into report (nzp_act, name, file_name) values (201,'Справка нотариусу', 'Web_sprav_bilZareg_f_5.frx');
--insert into report (nzp_act, name, file_name) values (202, 'Справка о составе семьи ф2', 'Web_spravOsostaveSem_f_2.frx');
insert into report (nzp_act, name, file_name) values (203, 'Справка в суды', 'Web_sprav_v_Sudi.frx');
insert into report (nzp_act, name, file_name) values (204,'Справка на приватизацию', 'Web_sprav_Privatizaciya.frx');
insert into report (nzp_act, name, file_name) values (205,'Справка о прописке', 'Web_sprav_Propiska.frx');
insert into report (nzp_act, name, file_name) values (206,'Лицевой счет','Web_licSchet.frx');
insert into report (nzp_act, name, file_name) values (207, 'Финансово-лицевой счет','Web_fin_ls.frx');
insert into report (nzp_act, name, file_name) values (208, 'Справка о составе семьи', 'Web_sost_fam.frx');
insert into report (nzp_act, name, file_name) values (209, 'Справка по начислениям', 'Web_sparv_nach.frx');
insert into report (nzp_act, name, file_name) values (210, 'Справка о задолженности', 'Web_s_nodolg.frx');
insert into report (nzp_act, name, file_name) values (211, 'Выписка из домовой книги', 'Web_vip_dom.frx');
insert into report (nzp_act, name, file_name) values (212, 'Сальдовая ведомость по услугам 5.10', 'Web_saldo_rep5_10.frx');
insert into report (nzp_act, name, file_name) values (213, 'Сальдовая ведомость по услугам 5.20', 'Web_saldo_rep5_20.frx');
insert into report (nzp_act, name, file_name) values (214, 'Заявление о регистрации по месту пребывания','Web_Zay_reg_git.frx');
insert into report (nzp_act, name, file_name) values (215, 'Заявление о снятии с регистрационного учета по месту жительства','Web_Zay_reg_git_f6_1.frx');
insert into report (nzp_act, name, file_name) values (216, 'Заявление о регистрации по месту жительства','Web_Zay_reg_git_f6_2.frx');
insert into report (nzp_act, name, file_name) values (217, 'Финансовый лицевой счет №__', 'Web_pasp_fils.frx');
insert into report (nzp_act, name, file_name) values (218, 'Адресный листок убытия','web_listok_ubit.frx');
insert into report (nzp_act, name, file_name) values (219, 'Адресный листок прибытия','web_listok_pribit.frx');
insert into report (nzp_act, name, file_name) values (220, 'Сведения о регистрации граждан и снятии с регистрационного учета','web_spis_reg_snyat.frx');
insert into report (nzp_act, name, file_name) values (221, 'Список юношей, подлежащих постановке на воинский учет','web_spis_vuchet.frx');
insert into report (nzp_act, name, file_name) values (222, 'Реестр граждан, сменивших или получивших удостоверение личности за период','web_spis_smena_dok.frx');
insert into report (nzp_act, name, file_name) values (223, 'Сведения о регистрации по месту жительства (форма РФЛ1)','web_rfl1.frx');
insert into report (nzp_act, name, file_name) values (224, 'Листок статистического учета прибытия','web_listok_stat_prib.frx');
insert into report (nzp_act, name, file_name) values (225, 'Реестр граждан','web_spis.frx');
insert into report (nzp_act, name, file_name) values (226, 'Заявление о выдаче/замене паспорта','web_form1p.frx');
insert into report (nzp_act, name, file_name) values (227, 'Листок статистического учета выбытия','web_listok_stat_ubit.frx');
insert into report (nzp_act, name, file_name) values (228, 'Генератор по параметрам','web_otchet_param.frx');
insert into report (nzp_act, name, file_name) values (229, 'Карточка аналитического учета','web_kart_analit_uch.frx');
insert into report (nzp_act, name, file_name) values (230, 'Сверка расчетов с жильцом','web_230.frx');
insert into report (nzp_act, name, file_name) values (231, 'Расшифровка по домам - начисления','web_231.frx');
insert into report (nzp_act, name, file_name) values (232, 'Справка по поставщикам коммунальных услуг','web_232.frx');
insert into report (nzp_act, name, file_name) values (233, 'Справка о наличии долга','web_233.frx');
insert into report (nzp_act, name, file_name) values (234, 'Справка по отключениям подачи коммунальных услуг','web_234.frx');
insert into report (nzp_act, name, file_name) values (235, 'Справка для предъявления в суд','web_235.frx');
insert into report (nzp_act, name, file_name) values (236, 'Справка по лицевому счету (Excel)','web_236.frx');
insert into report (nzp_act, name, file_name) values (237, 'Карточка регистрации','web_kart_registr.frx');
insert into report (nzp_act, name, file_name) values (238, 'Архивная справка','web_sprav_reg.frx');
insert into report (nzp_act, name, file_name) values (239, 'Справка по поставщикам с характеристиками','web_239.frx');
insert into report (nzp_act, name, file_name) values (240, 'Извещение за месяц','web_240.frx');
insert into report (nzp_act, name, file_name) values (241, 'Поквартирная карточка','web_kvar_kart.frx');
insert into report (nzp_act, name, file_name) values (242, 'Справка с места жительства','Web_sprav_smg_smr.frx');
insert into report (nzp_act, name, file_name) values (243, 'Справка по запросам','Web_spravpozapr_smr.frx');
insert into report (nzp_act, name, file_name) values (244, 'Оплата гражданами-получателями коммунальных услуг за поставленные услуги','web_244.frx');
insert into report (nzp_act, name, file_name) values (245, 'Информация  по расчетам с населением','web_245.frx');
insert into report (nzp_act, name, file_name) values (246, 'Акт сверки по энергоснабжению','web_246.frx');
insert into report (nzp_act, name, file_name) values (247, 'Генератор по начислениям','web_247.frx');
insert into report (nzp_act, name, file_name) values (248, 'Справка о начислениях по индивидуальным ПУ','Web_248.frx');
insert into report (nzp_act, name, file_name) values (249, 'Сводная ведомость по нормативам потребления','Web_249.frx');
insert into report (nzp_act, name, file_name) values (250, 'Отчет о жилом фонде','Web_250.frx');
insert into report (nzp_act, name, file_name) values (251, 'Расшифровка по домовым начислениям с перерасчетами','Web_251.frx');
insert into report (nzp_act, name, file_name) values (253, 'Список должников от 3-х месяцев','Web_253.frx');
insert into report (nzp_act, name, file_name) values (254, 'Сведения о просроченной задолженности','Web_254.frx');
insert into report (nzp_act, name, file_name) values (255, 'Справка по начислению платы за услуги','Web_255.frx');
insert into report (nzp_act, name, file_name) values (256, 'Расшифровка по домам с ОДПУ','Web_256.frx');
insert into report (nzp_act, name, file_name) values (257, 'Ведомость оплаты','Web_257.frx');
insert into report (nzp_act, name, file_name) values (258, 'Ведомость разовых начислений','Web_258.frx');
insert into report (nzp_act, name, file_name) values (259, 'Формирование квитанций','Web_259.frx');
insert into report (nzp_act, name, file_name) values (260, 'Протокол расчета общедомового поправочного коэффициента','web_protokolODN.frx');
insert into report (nzp_act, name, file_name) values (261, 'Калькуляция тарифа по услуге Содержание жилья','web_261.frx');
insert into report (nzp_act, name, file_name) values (262, 'Оплата гражданами-получателями коммунальных услуг за поставленные услуги (сводный)','web_262.frx');
insert into report (nzp_act, name, file_name) values (263, 'Список заявок','web_263.frx');
insert into report (nzp_act, name, file_name) values (264, '2.1. Количество нарядов - заказов по услугам','web_264.frx');
insert into report (nzp_act, name, file_name) values (265, '2.2. Количество нарядов - заказов по подрядчикам','web_265.frx');
insert into report (nzp_act, name, file_name) values (266, 'Сальдовая оборотная ведомость (10.14.3)','web_266.frx');
insert into report (nzp_act, name, file_name) values (267, 'Сальдовая оборотная ведомость по поставщикам (10.14.1)','web_267.frx');
insert into report (nzp_act, name, file_name) values (268, 'Справка для незарегистрированного собственника','Web_sprav_smg_smr.frx');
insert into report (nzp_act, name, file_name) values (269, 'Лицевой счет без долга','Web_licSchetwd.frx');
insert into report (nzp_act, name, file_name) values (270, 'Справка по услугам группы "Содержание жилья"','Web_270.frx');
insert into report (nzp_act, name, file_name) values (271, 'Справка по отключениям подачи коммунальных услуг по домам','web_271.frx');
insert into report (nzp_act, name, file_name) values (272, 'Справка по отключениям подачи коммунальных услуг по ЖЭУ','web_272.frx');
insert into report (nzp_act, name, file_name) values (273, 'Сведения по отключениям услуг по поставщикам','web_273.frx');
insert into report (nzp_act, name, file_name) values (274, 'Сведения по отключениям услуг','web_274.frx');
insert into report (nzp_act, name, file_name) values (275, 'Акты по отключениям услуг','web_275.frx');
insert into report (nzp_act, name, file_name) values (276, 'Список нарядов-заказов по неисправностям','web_276.frx');
insert into report (nzp_act, name, file_name) values (277, 'Список невыполненных нарядов-заказов к концу','web_277.frx');
insert into report (nzp_act, name, file_name) values (278, 'Приложение к информации, полученной ОДДС','web_278.frx');
insert into report (nzp_act, name, file_name) values (279, 'Информация, полученная ОДДС','web_279.frx');
insert into report (nzp_act, name, file_name) values (280, '2.4. Количество переадресаций заявок, принятых ОДДС','web_280.frx');
insert into report (nzp_act, name, file_name) values (281, '1.3.1 Список сообщений, зарегистрированных ОДДС','web_281.frx');
insert into report (nzp_act, name, file_name) values (282, '1.3.2 Список сообщений, зарегистрированных ОДДС(опрос)','web_282.frx');
insert into report (nzp_act, name, file_name) values (283, 'Список жильцов модифицируемый','web_spis_gil_mod.frx');
insert into report (nzp_act, name, file_name) values (284, 'Архивная справка-2','web_sprav_reg_old.frx');
insert into report (nzp_act, name, file_name) values (285, 'Сверка поступлений за день','web_285.frx');
insert into report (nzp_act, name, file_name) values (286, 'Сверка поступлений за период','web_286.frx');
insert into report (nzp_act, name, file_name) values (287, 'Сводная информация по распределению оплат за период','web_287.frx');
insert into report (nzp_act, name, file_name) values (288, 'Реестр счетчиков по лицевым счетам','web_288.frx');
insert into report (nzp_act, name, file_name) values (289, 'Начисления по поставщикам','web_289.frx');
insert into report (nzp_act, name, file_name) values (290, 'Сальдовая оборотная ведомость по услугам для всех поставщиков','web_290.frx');
insert into report (nzp_act, name, file_name) values (291, 'Сводная ведомость начислений и оплат в разрезе поставщиков','web_291.frx');
insert into report (nzp_act, name, file_name) values (292, '4.16. Данные приборов учета по жилым домам','pu_data.frx');
insert into report (nzp_act, name, file_name) values (293, '4.18. Отчет по расходу на дома','web_rashod_pu.frx');
insert into report (nzp_act, name, file_name) values (294, 'Протокол рассчитанных значений ОДН','web_294.frx');
insert into report (nzp_act, name, file_name) values (295, 'Рассогласование с паспортисткой','web_295.frx');
insert into report (nzp_act, name, file_name) values (296, 'Список недопоставок (УПГ)','web_296.frx');
insert into report (nzp_act, name, file_name) values (299, 'Справка по поставщикам коммунальных услуг форма 3','web_299.frx');
insert into report (nzp_act, name, file_name) values (300, 'Выписка из ЛС о поданных показаниях ИПУ','web_300.frx');
insert into report (nzp_act, name, file_name) values (301, 'Справка на приватизацию2', 'Web_sprav_Privatizaciya2_smr.frx');
insert into report (nzp_act, name, file_name) values (302, 'Протокол расчета значений ОДН расширенный', 'Web_protocol_ODN_rash.frx');
insert into report (nzp_act, name, file_name) values (303, 'Выгрузка в Кассу 3.0', 'UnloadingToKassa.frx');
insert into report (nzp_act, name, file_name) values (304, 'Платежи.Ежедневная информация по платежам за ЖКУ по г.Губкину', 'web_304.frx');
insert into report (nzp_act, name, file_name) values (305, 'Сверка поступлений за месяц', 'web_305.frx');
insert into report (nzp_act, name, file_name) values (306, 'Сальдо в банк', 'web_306.frx');
insert into report (nzp_act, name, file_name) values (307, 'Состояние текущих начислений по домам','web_307.frx');
insert into report (nzp_act, name, file_name) values (308, 'Итоги оплат по домам (ЕПД)','web_308.frx');
insert into report (nzp_act, name, file_name) values (309, 'Справка по поставщикам форма 3','web_309.frx');
insert into report (nzp_act, name, file_name) values (310, 'Справка о начислении платы по видам услуг (Форма 2)','web_310.frx');
insert into report (nzp_act, name, file_name) values (311, 'Статистика состояния жилфонда', 'web_311.frx');
insert into report (nzp_act, name, file_name) values (312, 'Расписка в получении документов жильца', 'web_312.frx');
insert into report (nzp_act, name, file_name) values (313, 'Свидетельство о регистрации по месту пребывания', 'web_sprav_reg_preb.frx');
insert into report (nzp_act, name, file_name) values (314, 'Справка с места жительства','Web_sprav_smg_gub.frx');
insert into report (nzp_act, name, file_name) values (315, 'Выписка из лицевого счета','Web_vip_ls.frx');
insert into report (nzp_act, name, file_name) values (316, 'Заявление на приватизацию','Web_zay_privat.frx');
insert into report (nzp_act, name, file_name) values (317, 'Статистика начислений по лицевым счетам','Web_nachisl_ls.frx');
insert into report (nzp_act, name, file_name) values (318, 'Статистика начислений по домам','Web_nachisl_dom.frx');
insert into report (nzp_act, name, file_name) values (319, 'Статистика начислений по участкам','Web_nachisl_uch.frx');
insert into report (nzp_act, name, file_name) values (320, 'Выгрузка показаний ПУ', 'web_320.frx');
insert into report (nzp_act, name, file_name) values (321, 'Выгрузка начислений в банк', 'web_321.frx');
--insert into report (nzp_act, name, file_name) values (322, 'Выгрузка реестра для загрузки в БС', 'web_322.frx');
insert into report (nzp_act, name, file_name) values (323, 'Выгрузка начислений в орган социальной защиты населения', 'UnloadingToKassa.frx');
insert into report (nzp_act, name, file_name) values (324, '50.1 Сальдовая ведомость', 'web_324.frx');
insert into report (nzp_act, name, file_name) values (325, '50.2 Ведомость должников', 'web_325.frx');
insert into report (nzp_act, name, file_name) values (326, 'Сверка характеристик и начислений', 'web_326.frx');
insert into report (nzp_act, name, file_name) values (327, 'Сверка характеристик лицевых счетов и домов', 'web_327.frx');
insert into report (nzp_act, name, file_name) values (328, 'Рассогласование с паспортисткой', 'web_328.frx');
insert into report (nzp_act, name, file_name) values (330, 'Справка на проживание и состав семьи','Web_sprav_na_prog.frx');
insert into report (nzp_act, name, file_name) values (331, 'Справка о смерти (г.Казань)','Web_sprav_o_smert.frx');
insert into report (nzp_act, name, file_name) values (332, 'Информация о временно зарегистрированных','pasp/list/vrem_zareg.frx');
insert into report (nzp_act, name, file_name) values (333, 'Информация о собственниках','pasp/list/sobstv.frx');
insert into report (nzp_act, name, file_name) values (334, 'Список жильцов для военкомата','pasp/list/voenkomat.frx');
insert into report (nzp_act, name, file_name) values (335, 'Выписка на жилое помещение','pasp/gil/vip_kvar.frx');
insert into report (nzp_act, name, file_name) values (336, 'Выписка из домовой книги для горгаза','pasp/gil/vip_dom_gas.frx');
insert into report (nzp_act, name, file_name) values (337, 'Справка для незарегистрированного собственника','Web_sprav_smg.frx');
insert into report (nzp_act, name, file_name) values (338, 'Справка на приватизацию2', 'Web_sprav_Privatizaciya2.frx');
insert into report (nzp_act, name, file_name) values (339, 'Справка по запросам','Web_spravpozapr.frx');
insert into report (nzp_act, name, file_name) values (340, '3.70 Сводный отчет по начислениям','tula_1.frx');
insert into report (nzp_act, name, file_name) values (341, '3.71 Сводный отчет по поступлениям','tula_2.frx');
insert into report (nzp_act, name, file_name) values (342, 'Выписка по счетчикам','samara_1.frx');
insert into report (nzp_act, name, file_name) values (343, 'Общий','AllOver.frx');
insert into report (nzp_act, name, file_name) values (344, 'Досудебной работы','AllAgreement.frx');
insert into report (nzp_act, name, file_name) values (345, 'Судебной работы','AllPrikaz.frx');
insert into report (nzp_act, name, file_name) values (346, 'Реестр квитанций по домам','samara_2.frx');
insert into report (nzp_act, name, file_name) values (347, 'Сведения о должниках','tula_5.frx');
insert into report (nzp_act, name, file_name) values (348, 'Справка по должникам','tula_6.frx');
insert into report (nzp_act, name, file_name) values (349, 'Справочник поставщиков','tula_7.frx');
insert into report (nzp_act, name, file_name) values (350, 'Сведения по поступлениям и перечислениям','tula_4.frx');
insert into report (nzp_act, name, file_name) values (351, 'Выписка из лицевого счета','tula_3.frx');

-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


--Добавление справочной информации
insert into s_help (cur_page,tip,kod,sort,hlp) values (0,0,11,1,'Форма NAME_FORM состоит из заголовка формы, меню выбора режима работы, области "Действия" в правой части окна формы и полей для поиска информации.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (0,0,12,2,'Меню располагается в верхней части формы и предназначено для перехода между формами (режимами работы) программы. Выбор позиции меню формы производится нажатием левой кнопки мыши на наименовании позиции.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (0,1,0,1,'В меню формы расположены следующие позиции:');
insert into s_help (cur_page,tip,kod,sort,hlp) values (0,2,0,1,'Область "Действия" располагается в правой части формы и позволяет выбрать следующие операции (выбор операции производится нажатием левой кнопки мыши на наименовании операции):');
insert into s_help (cur_page,tip,kod,sort,hlp) values (5,0,0,1,'Форма предназначена для скачивания файлов, подготовленных для пользователя');
insert into s_help (cur_page,tip,kod,sort,hlp) values (31,0,0,1,'Форма <b>NAME_FORM</b> предназначена для поиска <b>лицевых счетов, домов, приборов учета и карточек жильцов</b> на основе данных о лицевом счете, адресе, а также с учетом информации из других шаблонов поиска.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (31,3,0,1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp) values (31,3,0,2,'Для того, чтобы <b>найти</b> интересующие лицевые счета (дома / приборы учета / карточки жильцов) необходимо:</br>');
insert into s_help (cur_page,tip,kod,sort,hlp) values (31,3,0,3,'<p><b>1.</b> Задать параметры для поиска.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp) values (31,3,0,4,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Найти и показать" выбрать интересующий список (Список лицевых счетов или Список домов, или Список приборов учета, или Список карточек жильцов).</p>');
insert into s_help (cur_page,tip,kod,sort,hlp) values (31,3,0,5,'<p><b>3.</b> Если нужно найти Список лицевых счетов или др. удовлетворяющий дополнительным параметрам, тогда в в <i>Меню действий</i> в разделе "Учесть при поиске" выберите те шаблоны поиска, которые Вас интересуют.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp) values (31,3,0,6,'<p><b>4.</b> В <i>Меню действий</i> в разделе "Действия" нажать на строку "Выполнить поиск".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,0,0,1,'Форма предназначена для поиска лицевых счетов и домов, имеющих заданные начисления по услугам. При поиске могут учитываться дополнительные шаблоны поиска. Для поиска нужно выбрать расчетный месяц, добавить услугу и заполнить требуемые поля.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,1,'Поиск по расходам и начислениям позволяет выполнять поиск адресов, имеющих заданные расходы и начисления по услугам в заданном месяце. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,2,'По каждой услуге возможен поиск по следующим параметрам: начислено, перерасчет, полный расчет, тариф, расчет, недопоставка, льгота, подневной расчет, входящее сальдо, изменения, оплата, исходящее сальдо, к оплате. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,3,'При вводе только одного значения в строке "С" значения параметра проверяется на равенство введенному значению, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,4,'при вводе двух значений в строках "С" и "ПО" значение параметра должно входить в указанный диапазон, включая границы диапазона. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,5,'При наличии условий поиска по нескольким услугам достаточно, чтобы выполнялись условия хотя бы одной услуге. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (34,0,0,1,'Форма предназначена для поиска карточек жильцов по персональным данным.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (35,0,0,1,'Форма предназначена для поиска лицевых счетов, домов и приборов учета на основе информации о приборах учета и показаниях. При поиске могут учитываться дополнительные шаблоны поиска.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,0,0,1,'Форма предназначена для поиска домов на основе рассчитанных коэффициентов коррекции и расходов на общедомовые нужды.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,1,'Поиск адресов, имеющих заданные коэффициенты коррекции расходов дома, осуществляется по следующим параметрам:');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,2,'услуга, период (за который рассчитан коэффициент коррекции), коэффициент коррекции расхода для лицевых счетов с квартирными приборами учета, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,3,'коэффициент коррекции расхода по площади, расход домового прибора учета, сумма расходов по лицевым счетам с приборами учета, сумма расходов по лицевым счетам без приборов учета, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,4,'тип алгоритма расчета коэффициента коррекции.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (38,0,0,1,'Форма предназначена для поиска лицевых счетов, домов на основе информации о том, какие услуги действуют (или не действуют) в заданный период времени и какие поставщики их оказывают. При поиске могут учитываться дополнительные шаблоны поиска.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (39,0,0,1,'Форма предназначена для поиска лицевых счетов на основе информации о заявках жильцов. При поиске могут учитываться дополнительные шаблоны поиска.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (41,0,0,1,'Форма предназначена для просмотра выбранных лицевых счетов. Список лицевых счетов формируется при поиске, вызванном из шаблонов поиска.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (42,0,0,1,'Форма предназначена для просмотра выбранных домов. Список домов формируется при поиске домов и лицевых счетов, вызванном из шаблонов поиска.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (44,0,0,1,'Форма предназначена для просмотра списка управляющих организаций.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (45,0,0,1,'Форма предназначена для просмотра списка отделений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (49,0,0,1,'Форма предназначена для просмотра списка поставщиков услуг');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,0,0,1,'Форма предназначена для просмотра характеристик жилья выбранного лицевого счета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,1,'Над панелью с таблицей расположена информационная панель, показывающая данные выбранной квартиры.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,2,'Над таблицей располагается панель, отображающая общее количество параметров, номер текущей страницы параметров и стрелки перехода к предыдущей и следующей странице (если данные разбиты на несколько страниц), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,3,'а также элементы управления для выбора количества записей, отображаемых на экране, и типа параметров.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,4,'Таблица включает в себя следующие колонки: "Наименование" - наименование параметра, "Вид" - вид параметра, "Значение в расчетном месяце", "Поставщик/услуга" - показывается для некоторых типов параметров.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,5,'Для выбора параметра кликните на строку таблицы.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (53,0,0,1,'Форма предназначена для просмотра выбранных приборов учета. Список приборов учета формируется при поиске, вызванном из шаблонов поиска.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (54,0,0,1,'Форма предназначена для просмотра и редактирования показаний индивидуальных приборов учета выбранной квартиры или показаний одного выбранного индивидуального прибора учета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (54,0,0,2,'На форме можно просматривать введенные показания и показания, учтенные при расчете начислений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,1,'Под меню располагается информационная панель, показывающая информацию о лицевом счете.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,2,'Над таблицей располагается панель, отображающая общее количество недопоставок, номер текущей страницы и стрелки перехода к предыдущей и следующей странице (если данные разбиты на несколько страниц), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,3,'а также элементы управления для выбора количества записей, отображаемых на экране, и просмотра недействующих значений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,4,'В таблице отражены недопоставки по услугам для выбранного лицевого счета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (56,0,0,1,'Форма предназначена для просмотра выбранных карточек жильцов. Список карточек формируется при поиске, вызванном из шаблонов поиска.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,1,'Над панелью с таблицей расположена информационная панель, показывающая данные выбранного дома.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,2,'Над таблицей располагается панель, отображающая общее количество параметров, номер текущей страницы параметров и стрелки перехода к предыдущей и следующей странице (если данные разбиты на несколько страниц), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,3,'а также элементы управления для выбора количества записей, отображаемых на экране, и типа параметров.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,4,'Таблица включает в себя следующие колонки: "Наименование" - наименование параметра, "Вид" - вид параметра, "Значение в расчетном месяце", "Поставщик/услуга" - показывается для некоторых типов параметров.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,5,'Для выбора параметра кликните на строку таблицы.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (61,0,0,1,'Форма предназначена для просмотра и редактирования показаний общедомовых приборов учета выбранного дома или показаний одного выбранного общедомового прибора учета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (61,0,0,2,'На форме можно просматривать введенные показания и показания, учтенные при расчете начислений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (62,0,0,1,'Форма предназначена для просмотра, добавления и редактирования индивидуальных приборов учёта выбранной квартиры. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (63,0,0,1,'Форма предназначена для просмотра, добавления и редактирования общедомовых приборов учёта выбранного дома.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,1,'Над таблицей расположена информационная панель.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,2,'Таблица состоит из следующих полей: ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,3,'"Дата с" - дата начала действия параметра, "Дата по" - дата окончания действия параметра, если это поле пусто, то параметр действует бессрочно, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,4,'"Значение" - значение параметра, "Дата изменения" - отображает дату создания или последней модификации значения параметра, а также имя пользователя, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,5,'"Дата удаления" - дата удаления значения параметра и имя пользователя, показывается, если отмечен параметр "показывать недействующие значения"');
insert into s_help (cur_page,tip,kod,sort,hlp) values (66,0,0,1,'Форма предназначена для просмотра и редактирования показаний групповых приборов учета, обслуживающих выбранную квартиру.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (66,0,0,2,'На форме можно просматривать введенные показания и показания, учтенные при расчете начислений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (67,0,0,1,'Форма предназначена для просмотра и редактирования показаний групповых приборов учета выбранного дома или показаний одного выбранного группового прибора учета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (67,0,0,2,'На форме можно просматривать введенные показания и показания, учтенные при расчете начислений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,0,0,1,'Форма предназначена для просмотра и редактирования значений выбранной характеристики жилья.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (91,0,0,1,'Форма предназначена для просмотра, добавления и редактирования карточки жильца.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (92,0,0,1,'Форма предназначена для просмотра, добавления и редактирования групповых приборов учёта выбранного дома.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (100,0,0,1,'Форма предназначена для выбора характеристики жилья с целью добавления или удаления ее значений для выбранного списка лицевых счетов.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (101,0,0,1,'Форма предназначена для добавления или удаления значений выбранной характеристики жилья для выбранного списка лицевых счетов.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (106,0,0,1,'Форма предназначена для просмотра начислений за услуги в разрезе управляющих организаций, участков, услуг, поставщиков за выбранный период времени.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (107,0,0,1,'Форма предназначена для просмотра поступлений оплат и распределения их по поставщикам услуг. Данные можно просматривать в разрезе управляющих организаций, услуг, подрядчиков и банков за заданный период.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,1,'Под меню располагается информационная панель, показывающая информацию о доме.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,2,'Ниже располагается строка параметров, позволяющая выбрать год, за который отображаются данные расчета ОДН, услугу и отобразить колонки с прошлыми расчетами.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,3,'В таблице отражены результаты расчета коэффициентов коррекции по постановлениям.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (137,0,0,1,'Форма предназначена для формирования отчетов по выбранному дому');
insert into s_help (cur_page,tip,kod,sort,hlp) values (138,0,0,1,'Форма предназначена для формирования отчетов, полученных для выбранного списка заявок');
insert into s_help (cur_page,tip,kod,sort,hlp) values (139,0,0,1,'Форма предназначена для формирования отчетов, полученных для выбранного списка плановых работ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (162,0,0,1,'Форма предназначена для просмотра поквартирной карточки. В форме можно редактировать выбранную карточку в режиме просмотра, добавлять новую карточку на основе выбранной и добавлять карточку для нового жильца в режиме изменения.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (163,0,0,1,'Форма предназначена для просмотра списка периодов временного убытия выбранного жильца.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (164,0,0,1,'Форма предназначена для просмотра выбранного периода временного убытия жильца.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (165,0,0,1,'Форма предназначена для просмотра введенных изменений сальдо лицевого счета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (166,0,0,1,'Форма предназначена для просмотра расходов по услугам для лицевого счета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (167,0,0,1,'Форма предназначена для просмотра расходов по услугам для дома.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (168,0,0,1,'Форма предназначена для просмотра тарифов на услуги');
insert into s_help (cur_page,tip,kod,sort,hlp) values (169,0,0,1,'Форма предназначена для просмотра и ввода значений параметра');
insert into s_help (cur_page,tip,kod,sort,hlp) values (170,0,0,1,'Форма предназначена для просмотра системных параметров');
insert into s_help (cur_page,tip,kod,sort,hlp) values (171,0,0,1,'Форма предназначена для просмотра и ввода значений параметра прибора учета');
insert into s_help (cur_page,tip,kod,sort,hlp) values (172,0,0,1,'Форма предназначена для просмотра и ввода значений параметра прибора учета');
insert into s_help (cur_page,tip,kod,sort,hlp) values (173,0,0,1,'Форма предназначена для просмотра и ввода значений параметра и позволяет переходить на данные о квартире и доме');
insert into s_help (cur_page,tip,kod,sort,hlp) values (174,0,0,1,'Форма предназначена для просмотра и ввода значений параметра поставщика');
insert into s_help (cur_page,tip,kod,sort,hlp) values (175,0,0,1,'Форма предназначена для просмотра параметров поставщика');
insert into s_help (cur_page,tip,kod,sort,hlp) values (176,0,0,1,'Форма предназначена для просмотра параметров, влияющих на расчет начислений по услуге');
insert into s_help (cur_page,tip,kod,sort,hlp) values (177,0,0,1,'Форма предназначена просмотра списка собственников квартиры');
insert into s_help (cur_page,tip,kod,sort,hlp) values (178,0,0,1,'Форма предназначена для просмотра и редактирования карточки собственника квартиры');
insert into s_help (cur_page,tip,kod,sort,hlp) values (179,0,0,1,'Форма предназначена для добавления и удаления недопоставок лицевым счетам выбранных домов');
insert into s_help (cur_page,tip,kod,sort,hlp) values (180,0,0,1,'Форма предназначена для выбора характеристики жилья с целью добавления или удаления ее значений для лицевых счетов выбранных домов');
insert into s_help (cur_page,tip,kod,sort,hlp) values (181,0,0,1,'Форма предназначена для добавления или удаления значений выбранной характеристики жилья для лицевых счетов выбранных домов');
insert into s_help (cur_page,tip,kod,sort,hlp) values (184,0,0,1,'Форма предназначена для формирования протокола расчета ОДН');
insert into s_help (cur_page,tip,kod,sort,hlp) values (185,0,0,1,'Форма предназначена для просмотра, добавления и редактирования общеквартирных приборов учета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (186,0,0,1,'Форма предназначена для просмотра и редактирования показаний общеквартирных приборов учета выбранного дома или показаний одного выбранного общеквартирного прибора учета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (186,0,0,2,'На форме можно просматривать введенные показания и показания, учтенные при расчете начислений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (187,0,0,1,'Форма предназначена для просмотра и редактирования информации об общеквартирном приборе учета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (190,0,0,1,'Форма предназначена для просмотра и редактирования информации об услугах, оказываемых выбранному дому.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (191,0,0,1,'Форма предназначена для просмотра и редактирования информации о недопоставках услуг по выбранному дому.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (192,0,0,1,'Форма предназначена для просмотра и редактирования параметров, влияющих на расчет ОДН для выбранного дома.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (198,0,0,1,'Форма предназначена для просмотра и работы с пачкой принятых оплат.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (199,0,0,1,'Форма предназначена для просмотра и редактирования оплаты по лицевому счету.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (200,0,0,1,'Форма предназначена для загрузки файла электронного реестра оплат.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (201,0,0,1,'Форма предназначена для поиска информации по данным об оплатах за жилищно-коммунальные услуги.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (202,0,0,1,'Форма предназначена для просмотра списка выбранных оплат.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (203,0,0,1,'Форма предназначена для просмотра и установки операционного дня.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (204,0,0,1,'Форма предназначена для просмотра квитанции об оплате по лицевому счету.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (205,0,0,1,'Форма предназначена для просмотра информации о заявке.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (207,0,0,1,'Форма предназначена для добавления новой заявки.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (208,0,0,1,'Форма предназначена для отображения списка необработанных заявок.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (209,0,0,1,'Форма предназначена для отображения списка поступивших нарядов-заказов.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (210,0,0,1,'Форма предназначена для отображения списка нормативов.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (211,0,0,1,'Форма предназначена для просмотра и редактирования справочника целей прибытия/убытия.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (212,0,0,1,'Форма предназначена для просмотра и редактирования справочника документов.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (213,0,0,1,'Форма предназначена для просмотра и редактирования справочника родственных отношений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (214,0,0,1,'Форма предназначена для просмотра и редактирования справочника гражданств.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (215,0,0,1,'Форма предназначена для просмотра и редактирования справочников стран, регионов, городов, районов, населенных пунктов.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (216,0,0,1,'Форма предназначена для просмотра и редактирования справочника районов дома.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (217,0,0,1,'Форма предназначена для просмотра и редактирования справочника органов регистрационного учета.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (218,0,0,1,'Форма предназначена для просмотра и редактирования справочника мест выдачи документов.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (219,0,0,1,'Форма предназначена для просмотра и редактирования справочника документов о праве собственности.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (221,0,0,1,'Форма предназначена для просмотра справочника услуг.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (222,0,0,1,'Форма предназначена для просмотра и редактирования параметров выбранной услуги.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (223,0,0,1,'Форма предназначена для просмотра и редактирования значений выбранного параметра услуги.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (224,0,0,1,'Форма предназначена для просмотра услуг, поставщиков и формул расчета, которые могут действовать в заданном периоде.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (225,0,0,1,'Форма предназначена для просмотра и редактирования периодов, в которые возможно оказание выбранной услуги.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (226,0,0,1,'Форма предназначена для поиска информации по серверам.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (227,0,0,1,'Форма предназначена для просмотра и редактирования параметров выбранной управляющей организации.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (228,0,0,1,'Форма предназначена для просмотра и редактирования значений выбранного параметра управляющей организации.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (229,0,0,1,'Форма предназначена для просмотра и редактирования параметров выбранного участка.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (230,0,0,1,'Форма предназначена для просмотра и редактирования значений выбранного параметра участка.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (231,0,0,1,'Форма предназначена для просмотра и редактирования реквизитов управляющей организации для печати счетов.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (232,0,0,1,'Форма предназначена для просмотра и редактирования расчетных счетов выбранного подрядчика.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (233,0,0,1,'Форма предназначена для просмотра и редактирования договоров между управляющей компанией и подрядчиком.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (234,0,0,1,'Форма предназначена для просмотра и редактирования перечислений подрядчикам оплаты за услуги.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (239,0,0,1,'Форма предназначена для просмотра процентов удержания подрядчиком.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (240,0,0,1,'Форма предназначена для просмотра и редактирования cписка договоров на лицевых счетах');
insert into s_help (cur_page,tip,kod,sort,hlp) values (241,0,0,1,'Форма предназначена для списочного просмотра и ввода показаний приборов учета');
insert into s_help (cur_page,tip,kod,sort,hlp) values (267,0,0,1,'Форма предназначена для списочного просмотра нарядов-заказов для опроса');
insert into s_help (cur_page,tip,kod,sort,hlp) values (273,0,0,1,'Форма предназначена для редактирования и просмотра заявки на финансирование');
insert into s_help (cur_page,tip,kod,sort,hlp) values (279,0,0,1,'Форма предназначена для просмотра списка соглашений с подрядчиками');
insert into s_help (cur_page,tip,kod,sort,hlp) values (280,0,0,1,'Форма соглашения с подрядчиком');
insert into s_help (cur_page,tip,kod,sort,hlp) values (282,0,0,1,'Форма рассылки сообщений');
insert into s_help (cur_page,tip,kod,sort,hlp) values (284,0,0,1,'Форма справочника уровня платежей граждан');
insert into s_help (cur_page,tip,kod,sort,hlp) values (285,0,0,1,'Форма загрузка кассового плана');
insert into s_help (cur_page,tip,kod,sort,hlp) values (286,0,0,1,'Форма предназначена для рассылки нового сообщения');
insert into s_help (cur_page,tip,kod,sort,hlp) values (287,0,0,1,'Форма предназначена для просмотра списка актов о фактической поставке услуг');
insert into s_help (cur_page,tip,kod,sort,hlp) values (288,0,0,1,'Форма предназначена для просмотра акта о фактической поставке услуг');
insert into s_help (cur_page,tip,kod,sort,hlp) values (313,0,0,1,'Форма предназначена для просмотра справочника категорий льгот');
insert into s_help (cur_page,tip,kod,sort,hlp) values (340,0,0,1,'Форма предназначена для просмотра поступлений оплат и распределения их по поставщикам услуг. Данные можно просматривать в разрезе управляющих организаций, услуг, подрядчиков и банков за заданный период.');

delete from s_help where cur_page = 41 and tip=3;                                     
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,1,'<p><b><b>Как работать с формой:</b></b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,2,'<p><b>Перейти к данным по квартире или по дому можно одним из следующих способов:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,3,'<p><i><b>1. Через Главное меню:</b></i></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,4,'<p>1.1. Кликнуть на строку таблицы, чтобы выбрать лицевой счет.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,5,'<p>1.2. Выбрать пункт "Данные по квартире" и нажать интересующий пункт подменю / Выбрать пункт "Данные по дому" и нажать интересующий пункт подменю.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,6,'<p><i><b>2. Через Меню действий:</b></i></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,7,'<p>2.1. Кликнуть на строку таблицы, чтобы выбрать лицевой счет.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,8,'<p>2.1. Выбрать из выпадающего списка в разделе "Данные" интересующую форму.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,9,'<p>2.2. Нажать "Открыть данные" в разделе "Действия".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,10,'<p><i><b>3. В Меню действий в разделе "Данные" выбрать интересующую позицию и кликнуть на строку таблицы.</b></i></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,11,'<p><b>Для того, чтобы создать новый лицевой счет необходимо:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,12,'<p>1. В Меню действий, в разделе "Открыть данные" в выпадающем списке выбрать позицию "Для изменения".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (41,3,0,13,'<p>2. В разделе "Действия" Меню действий нажать строку "Создать лицевой счет".</p>');

--Добавление справочной информации для Паспортистки
delete from s_help where cur_page in (34, 56, 91, 106, 107, 112, 162, 163, 164) and tip = 3;

--поиск карточек жильцов
insert into s_help (cur_page,tip,kod,sort,hlp)  values (34,3,0,1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (34,3,0,2,'<p>Для того, чтобы <b>найти</b> интересующие карточки жильцов необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (34,3,0,3,'<p><b>1.</b> Задать параметры для поиска.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (34,3,0,4,'<p><b>Внимание!</b> При задании одной даты поиск осуществляется по сторогому соответствию дате.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (34,3,0,5,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Найти и показать" выбрать интересующий список (Список лицевых счетов или Список карточек жильцов).</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (34,3,0,6,'<p><b>3.</b> Если нужно найти список карточек жильцов, удовлетворяющий дополнительным параметрам, тогда в в <i>Меню действий</i> в разделе "Учесть при поиске" выберите интересующие Вас шаблоны поиска.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (34,3,0,7,'<p><b>4.</b> В <i>Меню действий</i> в разделе "Действия" нажать на строку "Выполнить поиск".</p>');

--список карточек жильцов
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,2,'<p>Для того чтобы <b>просмотреть периоды временного убытия</b> жильца необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,3,'<p><b>1.</b> Нажать на строку таблицы, чтобы выбрать карточку жильца.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,4,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Данные" в выпадающем списке выбрать позицию "Периоды убытия".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,5,'<p><b>3.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Открыть данные".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,6,'<p>Для того чтобы <b>просмотреть карточку жильца</b> необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,7,'<p><b>1.</b> Нажать на строку таблицы, чтобы выбрать карточку жильца.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,8,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Данные" в выпадающем списке выбрать позицию "Карточка жильца".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (56,3,0,9,'<p><b>3.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Открыть данные".</p>');

--карточка жильца
insert into s_help (cur_page,tip,kod,sort,hlp)  values (91,3,0,1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (91,3,0,2,'<p>Для того чтобы <b>изменить</b> карточку жильца в <i>Меню действий</i> в разделе "Открыть данные" в выпадающем списке необходимо выбрать позицию "Для изменения".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (91,3,0,3,'<p>Для <b>сохранения</b> изменений в разделе "Действия" необходимо нажать кнопку "Сохранить изменения".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (91,3,0,4,'<p><b>Внимание!</b> При добавлении новой карточки жильца или нового жильца кнопка "Открыть" становится доступной только после сохранения карточки жильца.</p>');

--Статистика по начислениям
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,2,'<p>При открытии формы показываются сводные данные о начислениях за текущий расчетный месяц.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,3,'<p>Поиск начислений может осуществляться по расчетному месяцу, управляющим организациям, участкам (ЖЭУ), услугам и поставщикам услуг.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,4,'<p>Расчетный месяц задается путем выбора первого и последнего месяца начислений.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,5,'<p>Для ограничения списка управляющих организаций, отметьте их на вкладке "Управляющая организация", где отображается список доступных управляющих организаций.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,6,'<p>Для сужения списка задайте часть наименования в поле "Фильтр по управляющим организациям" и нажмите на кнопку с изображением лупы. Если ни одна управляющая организация не отмечена, поиск будет выполняться по всем доступным управляющим организациям.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,7,'<p>Аналогичным образом можно ограничить поиск заданными участками, услугами и поставщиками.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,8,'<p>Для просмотра данных в разрезе управляющих организаций, участков, услуг и поставщиков, надо на вкладке "Группировка и порядок сортировки" отметить нужные поля.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,9,'<p>Порядок группировки соответствует тому, в каком порядке отмеченные поля показаны на экране. Этот порядок можно изменить путем перетаскивания мышью элементов вверх или вниз.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (106,3,0,10,'<p>После задания параметров поиска нажмите кнопку "Выполнить поиск" в меню действий. Найденные данные отображаются во вкладке "Результат". Используемые при этом параметры поиска отображаются над таблицей на панели "Условия поиска".</p>');

--Статистика поступлений
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,2,'<p>При открытии формы показываются сводные данные о поступлении и распределении оплат за текущий операционный день.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,3,'<p>При поиске оплат можно уточнить период оплат, ограничить список управляющих организаций (управляющих компаний), услуг, подрядчиков и банков.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,4,'<p>Операционный день задается путем выбора первого и последнего операционного дня. Если вторая дата не указана, то выполняется поиск за один операционный день.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,5,'<p>Для ограничения списка управляющих организаций, отметьте их на вкладке "управляющая организация", где отображается список доступных управляющих организаций.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,6,'<p>Для сужения списка задайте часть наименования в поле "Фильтр по управляющим организациям" и нажмите на кнопку с изображением лупы. Если ни одна управляющая организация не отмечена, поиск будет выполняться по всем доступным управляющим организациям.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,7,'<p>Аналогичным образом можно ограничить поиск заданными услугами, подрядчиками и банками.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,8,'<p>Для просмотра данных в разрезе управляющих организаций, услуг, подрядчиков и банков, надо на вкладке "Группировка и порядок сортировки" отметить нужные поля.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,9,'<p>Порядок группировки соответствует тому, в каком порядке отмеченные поля показаны на экране. Этот порядок можно изменить путем перетаскивания мышью элементов вверх или вниз.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (107,3,0,10,'<p>После задания параметров поиска нажмите кнопку "Выполнить поиск" в меню действий. Найденные данные отображаются во вкладке "Результат". Используемые при этом параметры поиска отображаются над таблицей на панели "Условия поиска".</p>');

--Статистика по начислениям по дому
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,2,'<p>При открытии формы показываются сводные данные о начислениях по дому за текущий расчетный месяц.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,3,'<p>Поиск начислений может осуществляться по расчетному месяцу, управляющим организациям (управляющим компаниям), участкам (ЖЭУ), услугам и поставщикам услуг.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,4,'<p>Расчетный месяц задается путем выбора первого и последнего месяца начислений.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,5,'<p>Для ограничения списка управляющих организаций, отметьте их на вкладке "Управляющая организация", где отображается список доступных управляющих организаций.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,6,'<p>Для сужения списка задайте часть наименования в поле "Фильтр по управляющим организациям" и нажмите на кнопку с изображением лупы. Если ни одна управляющая организация не отмечена, поиск будет выполняться по всем доступным управляющим организациям.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,7,'<p>Аналогичным образом можно ограничить поиск заданными участками, услугами и поставщиками.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,8,'<p>Для просмотра данных в разрезе управляющих организаций, участков, услуг и поставщиков, надо на вкладке "Группировка и порядок сортировки" отметить нужные поля.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,9,'<p>Порядок группировки соответствует тому, в каком порядке отмеченные поля показаны на экране. Этот порядок можно изменить путем перетаскивания мышью элементов вверх или вниз.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (112,3,0,10,'<p>После задания параметров поиска нажмите кнопку "Выполнить поиск" в меню действий. Найденные данные отображаются во вкладке "Результат". Используемые при этом параметры поиска отображаются над таблицей на панели "Условия поиска".</p>');

--поквартирная карточка
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 2,'<p>Для того чтобы <b>просмотреть периоды временного убытия</b> жильца необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 3,'<p><b>1.</b> Нажать на строку таблицы, чтобы выбрать карточку жильца.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 4,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Данные" в выпадающем списке выбрать позицию "Периоды убытия".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 5,'<p><b>3.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Открыть данные".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 6,'<p>Для того чтобы <b>просмотреть карточку жильца</b> необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 7,'<p><b>1.</b> Нажать на строку таблицы, чтобы выбрать карточку жильца.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 8,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Данные" в выпадающем списке выбрать позицию "Карточка жильца".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0, 9,'<p><b>3.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Открыть данные".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0,10,'<p>Для того чтобы <b>добавить новую карточку жильца</b> необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0,11,'<p><b>1.</b> Нажать на строку таблицы, чтобы выбрать карточку жильца.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0,12,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Открыть данные" в выпадающем списке выбрать позицию "Для изменения".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0,13,'<p><b>3.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Добавить запись".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0,14,'<p>Для того чтобы <b>добавить нового жильца</b> необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0,15,'<p><b>1.</b> В разделе "Открыть данные" в выпадающем списке выбрать позицию "Для изменения".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (162,3,0,16,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Добавить жильца".</p>');

--список периодов временного убытия
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 2,'<p>Для того чтобы <b>просмотреть</b> данные о периоде временного убытия необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 3,'<p><b>1.</b> Нажать на строку таблицы, чтобы выбрать период временного убытия.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 4,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Открыть данные".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 5,'<p>Для того чтобы <b>добавить</b> новый период временного убытия необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 6,'<p><b>1.</b> В <i>Меню действий</i> в разделе "Открыть данные" в выпадающем списке выбрать позицию "Для изменения".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 7,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Добавить запись".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 8,'<p>Для того чтобы <b>удалить</b> период временного убытия необходимо:</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0, 9,'<p><b>1.</b> Нажать на строку таблицы, чтобы выбрать период временного убытия.</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0,10,'<p><b>2.</b> В <i>Меню действий</i> в разделе "Открыть данные" в выпадающем списке выбрать позицию "Для изменения".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (163,3,0,11,'<p><b>3.</b> В <i>Меню действий</i> в разделе "Действия" нажать кнопку "Удалить запись".</p>');

--период временного убытия
insert into s_help (cur_page,tip,kod,sort,hlp)  values (164,3,0,1,'<p><b>Как работать с формой:</b></p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (164,3,0,2,'<p>Для того чтобы <b>изменить</b> период временного убытия в разделе "Открыть данные" в выпадающем списке необходимо выбрать позицию "Для изменения".</p>');
insert into s_help (cur_page,tip,kod,sort,hlp)  values (164,3,0,3,'<p>Для <b>сохранения</b> изменений в разделе "Действия" необходимо нажать кнопку "Сохранить изменения".</p>');


--------------------------------------------------------
--myreport.aspx мои файлы
--------------------------------------------------------
--!5
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 5, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 5, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page, nzp_act, page_url) values (5, 5, 5);


--------------------------------------------------------
--general/setup/settings.aspx настройки
--------------------------------------------------------
-- в разработке
--!6
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 6, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 6, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (6, 5, 6);
insert into actions_lnk (cur_page, nzp_act, page_url) values (6, 61, 6);


--------------------------------------------------------
--findls.aspx шаблон поиска по адресу
--------------------------------------------------------
--!31
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 31, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 31, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (502,503,504,505,506,507,508,509,510,512,513,514);

insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,41);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,42);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,43);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,44);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,45);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,46);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,53);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,56);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,197);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,246);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,324);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,1,325);
insert into actions_lnk (cur_page,nzp_act,page_url) values (31,2,31);


--------------------------------------------------------
--findprm.aspx шаблон поиска по параметрам
--------------------------------------------------------
--!32
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 32, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 32, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,503,504,505,506,507,508,510);

insert into actions_lnk (cur_page, nzp_act, page_url) values (32, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (32, 2, 32);


--------------------------------------------------------
--findch.aspx шаблон поиска по начислениям
--------------------------------------------------------
--!33
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 33, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 33, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,502,504,505,506,507,508,510);

insert into actions_lnk (cur_page,nzp_act,page_url) values (33,1,41);
insert into actions_lnk (cur_page,nzp_act,page_url) values (33,1,42);
insert into actions_lnk (cur_page,nzp_act,page_url) values (33,2,33);


--------------------------------------------------------
--findgil.aspx шаблон поиска по жителям
--------------------------------------------------------
--!34
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 34, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 34, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,510);

insert into actions_lnk (cur_page, nzp_act, page_url) values (34, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (34, 2, 34);


--------------------------------------------------------
--findcnt.aspx шаблон поиска по показаниям
--------------------------------------------------------
--!35
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 35, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 35, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,502,503,504,506,507,508,510);

insert into actions_lnk (cur_page,nzp_act,page_url) values (35,1,41);
insert into actions_lnk (cur_page,nzp_act,page_url) values (35,1,42);
insert into actions_lnk (cur_page,nzp_act,page_url) values (35,1,53);
insert into actions_lnk (cur_page,nzp_act,page_url) values (35,2,35);


--------------------------------------------------------
--findnd.aspx шаблон поиска по недопоставкам
--------------------------------------------------------
--!36
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 36, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 72,81,82,106,107, 74,241, 77,220,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 36, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 36, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,502,503,504,505,507,508,510);

insert into actions_lnk (cur_page, nzp_act, page_url) values (36, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (36, 2, 36);


--------------------------------------------------------
--findodn.aspx шаблон поиска по ОДН
--------------------------------------------------------
--!37
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 37, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 37, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,502,503,504,505,506,508,510);

insert into actions_lnk (cur_page, nzp_act, page_url) values (37, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (37, 2, 37);


--------------------------------------------------------
--findserv.aspx шаблон поиска по услугам и поставщикам
--------------------------------------------------------
--!38
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 38, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 72,81,82,106,107, 74,241, 77,220,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 38, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 38, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,502,503,504,505,506,507,510);

insert into actions_lnk (cur_page, nzp_act, page_url) values (38, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (38, 1, 42);
insert into actions_lnk (cur_page, nzp_act, page_url) values (38, 2, 38);


--------------------------------------------------------
--findsupg.aspx шаблон поиска по заявкам
--------------------------------------------------------
--!39
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 39, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,248,252,289, 77,220,207,247, 80,208,209, 254,255,267,282) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 39, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 39, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,512);

insert into actions_lnk (cur_page, nzp_act, page_url) values (39, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (39, 1, 197);
insert into actions_lnk (cur_page, nzp_act, page_url) values (39, 2, 39);


--!41
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, 1, 0, 0 from s_actions where nzp_act in (3);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, 4, 0, 0 from s_actions where nzp_act in (6);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, 5, 0, 0 from s_actions where nzp_act in (24);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, 6, 0, 0 from s_actions where nzp_act in (51);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, 7, 0, 0 from s_actions where nzp_act in (73);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, 8, 0, 0 from s_actions where nzp_act in (172);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, 9, 0, 0 from s_actions where nzp_act in (70); 
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, 10, 0, 0 from s_actions where nzp_act in (871);

insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, nzp_act, 2, 2 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (701,702,703,704,705);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 41, nzp_act, nzp_act, 2, 4 from s_actions where nzp_act in (601,602);

insert into actions_lnk (cur_page, nzp_act, page_url) values (41, 24, 98);
insert into actions_lnk (cur_page, nzp_act, page_url) values (41, 172, 41); 

--!42
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, 1, 0, 0 from s_actions where nzp_act in (3);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, 4, 0, 0 from s_actions where nzp_act in (6);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, 5, 0, 0 from s_actions where nzp_act in (24);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, 6, 0, 0 from s_actions where nzp_act in (25);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, 7, 0, 0 from s_actions where nzp_act in (122);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, 8, 0, 0 from s_actions where nzp_act in (515);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, 9, 0, 0 from s_actions where nzp_act in (70);

insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, nzp_act, 2, 2 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (701,702,703,704,705);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 42, nzp_act, nzp_act, 2, 4 from s_actions where nzp_act in (603,604);

insert into actions_lnk (cur_page, nzp_act, page_url) values (42, 24, 98);
insert into actions_lnk (cur_page, nzp_act, page_url) values (42, 25, 99);
insert into actions_lnk (cur_page, nzp_act, page_url) values (42, 122, 260);


--!43
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 43, nzp_act, 1, 0, 0 from s_actions where nzp_act in (3);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 43, nzp_act, 2, 0, 0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 43, nzp_act, 3, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 43, nzp_act, 4, 0, 0 from s_actions where nzp_act in (6);

insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 43, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 43, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (701,702,703);

insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 43, nzp_act, nzp_act, 2, 4 from s_actions where nzp_act in (603);


--------------------------------------------------------
--spisar.aspx список территорий
--------------------------------------------------------
--!44
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 44, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 44, nzp_act, 2, 0, 0 from s_actions where nzp_act in (103);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 44, nzp_act, 3, 0, 0 from s_actions where nzp_act in (104);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 44, nzp_act, 4, 0, 0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 44, nzp_act, 5, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 44, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (44, 4, 44);
insert into actions_lnk (cur_page, nzp_act, page_url) values (44, 5, 44);
insert into actions_lnk (cur_page, nzp_act, page_url) values (44, 61, 44);
insert into actions_lnk (cur_page, nzp_act, page_url) values (44, 103, 227);
insert into actions_lnk (cur_page, nzp_act, page_url) values (44, 104, 231);


--------------------------------------------------------
--spisgeu.aspx список отделений
--------------------------------------------------------
--!45
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 45, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,33,34,35,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 45, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 45, nzp_act, 2, 0, 0 from s_actions where nzp_act in (103);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 45, nzp_act, 3, 0, 0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 45, nzp_act, 4, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 45, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (45, 4, 45);
insert into actions_lnk (cur_page, nzp_act, page_url) values (45, 5, 45);
insert into actions_lnk (cur_page, nzp_act, page_url) values (45, 61, 45);
insert into actions_lnk (cur_page, nzp_act, page_url) values (45, 103, 229);


--------------------------------------------------------
--spissupp.aspx список поставщиков
--------------------------------------------------------
--!49
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 49, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,33,34,35,37,38,195, 40,41,42,50,53,56, 73,44,45,49,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 49, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 49, nzp_act, 2, 0, 0 from s_actions where nzp_act in (103);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 49, nzp_act, 3, 0, 0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 49, nzp_act, 4, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 49, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 49, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page, nzp_act, page_url) values (49, 4, 49);
insert into actions_lnk (cur_page, nzp_act, page_url) values (49, 5, 49);
insert into actions_lnk (cur_page, nzp_act, page_url) values (49, 61, 49);
insert into actions_lnk (cur_page, nzp_act, page_url) values (49, 103, 175);


--------------------------------------------------------
--baselist.aspx лицевые счета дома
--------------------------------------------------------
--!50
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 50,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 50,nzp_act,nzp_act,2,2 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 50,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,704,705);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 50,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (601,602);

insert into actions_lnk (cur_page, nzp_act, page_url) values (50, 5, 50);
insert into actions_lnk (cur_page, nzp_act, page_url) values (50, 70, 50);

--------------------------------------------------------
--spisprm.aspx квартирные параметры
--------------------------------------------------------
--!51
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 51,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 51,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 51,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 51,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 51,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page,nzp_act,page_url) values (51,67,173);


--!52
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 52,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 52,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (851,852);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 52,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 52,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 52,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (607);


--------------------------------------------------------
--counters.aspx выбранный список приборов учета
--------------------------------------------------------
--!53
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 53,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,37,38,195, 40,41,42,50,53,56, 72,81,82,106,107, 74,241, 77,220,243,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 53,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (3,5,14);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 53,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 53,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,704,705);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 53,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (601);


insert into actions_lnk (cur_page, nzp_act, page_url) values (53, 3, 68);
insert into actions_lnk (cur_page, nzp_act, page_url) values (53, 5, 53);
insert into actions_lnk (cur_page, nzp_act, page_url) values (53, 14, 54);

--------------------------------------------------------
--spisval.aspx показания квартирных приборов учета выбранной квартиры
--------------------------------------------------------
--!54
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 54,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 54,nzp_act,2,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 54,nzp_act,3,0,0 from s_actions where nzp_act in (70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 54,nzp_act,4,0,0 from s_actions where nzp_act in (543);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 54,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 54,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (71,72);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 54,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (111,112,175,542);

insert into actions_lnk (cur_page,nzp_act,page_url) values (54,5,54);
insert into actions_lnk (cur_page,nzp_act,page_url) values (54,61,54);
insert into actions_lnk (cur_page,nzp_act,page_url) values (54,543,54);


--------------------------------------------------------
--spisnd.aspx
--------------------------------------------------------
--!55
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 55,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,189,238,244,251,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,247,340)
   or nzp_page >= 950 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 55,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,18,19,70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 55,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 55,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (55,5,55);
insert into actions_lnk (cur_page,nzp_act,page_url) values (55,18,55);
insert into actions_lnk (cur_page,nzp_act,page_url) values (55,19,55);   


--------------------------------------------------------
--spisgil.aspx выбранный список карточек жильцов
--------------------------------------------------------
--!56
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 56, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,37,38,195, 40,41,42,50,53,56, 70,162, 75,135,136, 72,81,82,106,107,340,351) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 56, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (3, 5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 56, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610, 611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 56, nzp_act, nzp_act, 2, 2 from s_actions where nzp_act in (861, 862, 863);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 56, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (701, 702, 703,704,705);

insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 56, nzp_act, nzp_act, 2, 4 from s_actions where nzp_act in (601, 602, 607);

insert into actions_lnk (cur_page, nzp_act, page_url) values (56, 3, 91);
insert into actions_lnk (cur_page, nzp_act, page_url) values (56, 5, 56);


--------------------------------------------------------
--spisprm.aspx домовые параметры
--------------------------------------------------------
--!59
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 59,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340)
   or nzp_page >= 999 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 59,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 59,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 59,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 59,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (59,67,188);


--------------------------------------------------------
--spisval.aspx показания домовых ПУ для выбранного дома
--------------------------------------------------------
--!61
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 61,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 61,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 61,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (71,72);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 61,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (111,112);


insert into actions_lnk (cur_page,nzp_act,page_url) values (61,5,61);
insert into actions_lnk (cur_page,nzp_act,page_url) values (61,61,61);

--------------------------------------------------------
--список квартирных ПУ заданного ЛС
--------------------------------------------------------
--!62
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 62,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (3,4,5,14,64,70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 62,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (62, 3, 68);
insert into actions_lnk (cur_page, nzp_act, page_url) values (62, 4, 68);
insert into actions_lnk (cur_page, nzp_act, page_url) values (62, 5, 62);
insert into actions_lnk (cur_page, nzp_act, page_url) values (62, 14, 54);
insert into actions_lnk (cur_page, nzp_act, page_url) values (62, 81, 192);

--------------------------------------------------------
--список домовых ПУ заданного дома
--------------------------------------------------------
--!63
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 63,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (3,4,5,14,64);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 63,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (63, 3, 69);
insert into actions_lnk (cur_page, nzp_act, page_url) values (63, 4, 69);
insert into actions_lnk (cur_page, nzp_act, page_url) values (63, 5, 63);
insert into actions_lnk (cur_page, nzp_act, page_url) values (63, 14, 61);
insert into actions_lnk (cur_page, nzp_act, page_url) values (63, 81, 193);

--------------------------------------------------------
--prm.aspx значения квартирного параметра
--------------------------------------------------------
--!64
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 64,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 64,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 64,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (64, 5, 64);
insert into actions_lnk (cur_page, nzp_act, page_url) values (64, 16, 64);
insert into actions_lnk (cur_page, nzp_act, page_url) values (64, 17, 64);


--------------------------------------------------------
--spisval.aspx показания групповых ПУ для выбранного ЛС
--------------------------------------------------------
--!66
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 66,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 66,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 66,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (71,72);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 66,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (111,112);

insert into actions_lnk (cur_page,nzp_act,page_url) values (66,5,66);

--------------------------------------------------------
--spisval.aspx показания групповых ПУ для выбранного дома
--------------------------------------------------------
--!67
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 67,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 67,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 67,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (71,72);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 67,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (111,112);

insert into actions_lnk (cur_page,nzp_act,page_url) values (67,5,67);
insert into actions_lnk (cur_page,nzp_act,page_url) values (67,61,67);

--------------------------------------------------------
--countercard.aspx карточка квартирного ПУ
--------------------------------------------------------
--!68
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 68,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107, 73,94,240,340) 
   or nzp_page >= 999 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 68,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (14,61,70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 68,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (68, 14, 54);
insert into actions_lnk (cur_page, nzp_act, page_url) values (68, 61, 68);
insert into actions_lnk (cur_page, nzp_act, page_url) values (68, 67, 171);
insert into actions_lnk (cur_page, nzp_act, page_url) values (68, 70, 68);

--------------------------------------------------------
--countercard.aspx карточка домового ПУ
--------------------------------------------------------
--!69
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 69,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (14,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 69,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (69, 14, 61);
insert into actions_lnk (cur_page, nzp_act, page_url) values (69, 61, 69);
insert into actions_lnk (cur_page, nzp_act, page_url) values (69, 67, 172);


--------------------------------------------------------
--aa.aspx адресное пространство
--------------------------------------------------------
--!81
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 81,nzp_page,0,nzp_page from pages
where nzp_page in (71,57,59,61,63,67,92,99,124,167,184,185,186,190,191,192, 72,81,82,106,107, 75,206,340)
   or nzp_page >= 999 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 81,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (7,65,66);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 81,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (721,722,723);


--!82
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 82,nzp_page,0,nzp_page from pages
where nzp_page in (71,57,59,61,63,67,92,99,124,167,184,185,186,190,191,192, 72,81,82,106,107, 75,206,340)
   or nzp_page >= 999 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 82,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (65,66);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 82,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (724,725,726);


--------------------------------------------------------
--gil.aspx карточка жильца
--------------------------------------------------------
--!91
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 91, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,34, 40,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 75,135, 72,81,82,106,107,240,340,338) 
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 91, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (3,61,78,79);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 91, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 91, nzp_act, nzp_act, 2, 2 from s_actions where nzp_act in (862);

insert into actions_lnk (cur_page, nzp_act, page_url) values (91,  3, 163);
insert into actions_lnk (cur_page, nzp_act, page_url) values (91, 61, 91);
insert into actions_lnk (cur_page, nzp_act, page_url) values (91, 78, 91);
insert into actions_lnk (cur_page, nzp_act, page_url) values (91, 79, 91);

--------------------------------------------------------
--список групповых ПУ для заданного дома
--------------------------------------------------------
--!92
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 92,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (3,4,5,14,64);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 92,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (92, 3, 93);
insert into actions_lnk (cur_page, nzp_act, page_url) values (92, 4, 93);
insert into actions_lnk (cur_page, nzp_act, page_url) values (92, 5, 92);
insert into actions_lnk (cur_page, nzp_act, page_url) values (92, 14, 67);
insert into actions_lnk (cur_page, nzp_act, page_url) values (92, 64, 92);

--------------------------------------------------------
--countercard.aspx карточка группового ПУ
--------------------------------------------------------
--!93
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 93,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (14,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 93,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (93, 14, 67);
insert into actions_lnk (cur_page, nzp_act, page_url) values (93, 61, 93);
insert into actions_lnk (cur_page, nzp_act, page_url) values (93, 67, 172);

--------------------------------------------------------
--counttypes.aspx типы приборов учета
--------------------------------------------------------
--!94
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 94,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 94,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (94, 4, 94);
insert into actions_lnk (cur_page, nzp_act, page_url) values (94, 5, 94);
insert into actions_lnk (cur_page, nzp_act, page_url) values (94, 61, 94);


--------------------------------------------------------
--spisserv.aspx перечень услуг
--------------------------------------------------------
--!95
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 95,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 95,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,70,76,77);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 95,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page, nzp_act, page_url) values (95, 5, 95);
insert into actions_lnk (cur_page, nzp_act, page_url) values (95, 76, 96);
insert into actions_lnk (cur_page, nzp_act, page_url) values (95, 77, 176);


--------------------------------------------------------
--serv.aspx поставщики и формулы расчета услуг
--------------------------------------------------------
--!96
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 96,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 96,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,21,22,70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 96,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (96, 5, 96);
insert into actions_lnk (cur_page, nzp_act, page_url) values (96, 21, 96);
insert into actions_lnk (cur_page, nzp_act, page_url) values (96, 22, 96);


--------------------------------------------------------
--groupls.aspx группы ЛС
--------------------------------------------------------
--!97
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 97,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 97,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 97,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (97, 4, 97);
insert into actions_lnk (cur_page, nzp_act, page_url) values (97, 5, 97);
insert into actions_lnk (cur_page, nzp_act, page_url) values (97, 61, 97);


--------------------------------------------------------
--cardls.aspx карточка ЛС
--------------------------------------------------------
--!98
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 98,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 98,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61,70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 98,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (98,61,98);
insert into actions_lnk (cur_page,nzp_act,page_url) values (98,67,173);


--------------------------------------------------------
--cardls.aspx карточка дома
--------------------------------------------------------
--!99
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 99,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 99,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 99,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (99,61,99);
insert into actions_lnk (cur_page,nzp_act,page_url) values (99,67,188);


--------------------------------------------------------
--spisprm.aspx квартирные параметры для групповых операций с лицевыми счетами
--------------------------------------------------------
--!100
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 100,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 100,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 100,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page,nzp_act,page_url) values (100,5,100);
insert into actions_lnk (cur_page,nzp_act,page_url) values (100,67,101);


--------------------------------------------------------
--prm.aspx квартирные параметры для групповых операций с лицевыми счетами
--------------------------------------------------------
--!101
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 101,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,100,104,108,110,196,241,263,264, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 101,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 101,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (101,16,101);
insert into actions_lnk (cur_page,nzp_act,page_url) values (101,17,101);


--------------------------------------------------------
--spisprm.aspx домовые параметры для групповых операций с домами
--------------------------------------------------------
--!102
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 102,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,102,109,179,180,182,241, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 102,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 102,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 102,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (102,5,102);
insert into actions_lnk (cur_page,nzp_act,page_url) values (102,67,103);


--------------------------------------------------------
--prm.aspx групповые операций с параметром дома
--------------------------------------------------------
--!103
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 103,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,102,109,179,180,182,241, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 103,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 103,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (103,16,103);
insert into actions_lnk (cur_page,nzp_act,page_url) values (103,17,103);


--------------------------------------------------------
--spisserv.aspx список услуг для групповых операций с ЛС
--------------------------------------------------------
--!104
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 104,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 104,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (104,5,104);


--------------------------------------------------------
--serv.aspx групповые операции с выбранной услугой по ЛС
--------------------------------------------------------
--!105
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 105,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,100,104,108,110,196,241,263,264, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 105,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (21,22);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 105,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (105,21,105);
insert into actions_lnk (cur_page,nzp_act,page_url) values (105,22,105);


--------------------------------------------------------
--statcharge.aspx статистика по начислениям
--------------------------------------------------------
--!106
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 106,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 72,81,82,106,107,340, 75,206, 77,200,203) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 106,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 106,nzp_act,2,0,0 from s_actions where nzp_act in (164);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 106,nzp_act,3,0,0 from s_actions where nzp_act in (8);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 106,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 106,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (106,1,106);


--------------------------------------------------------
--distrib.aspx сальдо перечислений
--------------------------------------------------------
--!107
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 107,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 72,81,82,106,107, 75,206, 235,107,203,253,258,259,265, 73,232,233,239,256,257) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 107,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (1,107,108);
--insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 107,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (521,524,536,537,538);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 107,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 107,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (107,1,107);
insert into actions_lnk (cur_page,nzp_act,page_url) values (107,107,234);
insert into actions_lnk (cur_page,nzp_act,page_url) values (107,108,107);


--------------------------------------------------------
--distrib_dom.aspx сальдо перечислений
--------------------------------------------------------
--!340
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 340,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (1,107,108,124,875);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 340,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 340,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page,nzp_act,page_url) values (340,1,340);
insert into actions_lnk (cur_page,nzp_act,page_url) values (340,875,340);
insert into actions_lnk (cur_page,nzp_act,page_url) values (340,107,234);
insert into actions_lnk (cur_page,nzp_act,page_url) values (340,108,340);
insert into actions_lnk (cur_page,nzp_act,page_url) values (340,124,265);


--------------------------------------------------------
--distrib_dom_supp.aspx сальдо перечислений по договорам
--------------------------------------------------------
--!353
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 353,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (1,108,124,875);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 353,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 353,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page,nzp_act,page_url) values (353,1,353);
insert into actions_lnk (cur_page,nzp_act,page_url) values (353,875,353);
insert into actions_lnk (cur_page,nzp_act,page_url) values (353,108,353);
--insert into actions_lnk (cur_page,nzp_act,page_url) values (353,124,265);


--------------------------------------------------------
--cardls.aspx групповые операции с реквизитами ЛС
--------------------------------------------------------
--!108
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 108,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 108,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);


--------------------------------------------------------
--carddom.aspx групповые операции с реквизитами домов
--------------------------------------------------------
--!109
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 109,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,102,109,179,180,182,241, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 109,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 109,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);


--------------------------------------------------------
--spisnd.aspx групповые операции с недопоставками
--------------------------------------------------------
--!110
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 110,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (18,19);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 110,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);


--------------------------------------------------------
--prm.aspx состояние лицевого счета
--------------------------------------------------------
--!111
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 111,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950                 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 111,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 111,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (111, 5, 111);
insert into actions_lnk (cur_page, nzp_act, page_url) values (111, 16, 111);


--------------------------------------------------------
--statcharge.aspx статистика по начислениям по дому
--------------------------------------------------------
--!112
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 112,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53, 71,57,59,61,63,67,92,99,124,167,184,185,186,190,191,192) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 112,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 112,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 112,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (112,1,112);


--------------------------------------------------------
-- baselist.aspx Сальдо по ЛС
--------------------------------------------------------
--!121
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 121,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 121,nzp_act,2,0,0 from s_actions where nzp_act in (128);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 121,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (520,521,522);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 121,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 121,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (605,606);


--!122
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 122,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 122,nzp_act,1,0,0 from s_actions where nzp_act in (70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 122,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (520,521,522,523,528);

insert into actions_lnk (cur_page,nzp_act,page_url) values (122,5,122);
insert into actions_lnk (cur_page, nzp_act, page_url) values (122, 70, 122);


--!123
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 123,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 123,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 123,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 123,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (601,602);


--!124
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 124,nzp_act,1,0,0 from s_actions where nzp_act in (5);

insert into actions_lnk (cur_page,nzp_act,page_url) values (124,5,124);


--!126
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 126,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 126,nzp_act,2,0,0 from s_actions where nzp_act in (8);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 126,nzp_act,3,0,0 from s_actions where nzp_act in (65);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 126,nzp_act,4,0,0 from s_actions where nzp_act in (66);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 126,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (520,521,522);


--!127
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 127,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 127,nzp_act,3,0,0 from s_actions where nzp_act in (65);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 127,nzp_act,4,0,0 from s_actions where nzp_act in (66);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 127,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (520,521,522);


--!130
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 130,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 130,nzp_act,3,0,0 from s_actions where nzp_act in (65);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 130,nzp_act,4,0,0 from s_actions where nzp_act in (66);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 130,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (520,521,522);


--------------------------------------------------------
--billrt.aspx Счет-фактура
--------------------------------------------------------
--!131
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 131,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);


--------------------------------------------------------
--report.aspx Отчеты для выбранного ЛС
--------------------------------------------------------
--!133
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 133,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950                 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 133,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 133,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 133,nzp_act,nzp_act,2,5 from s_actions where nzp_act in (204,206,207,209,210,211,214,215,216,217,230,233,235,236,240,241,243,268,269,300,301,315,316,335,336,337,338,339,342,351);
  

--------------------------------------------------------
--report.aspx Отчеты для выбранного списка ЛС
--------------------------------------------------------
--!134
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 134,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 134,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 134,nzp_act,nzp_act,2,5 from s_actions where nzp_act in (212,213,228,229,231,232,234,239,244,245,246,247,248,249,250,251,253,254,255,256,257,258,259,261,262,266,267,270,271,272,288,289,290,294,295,302,304,306,309,310,311,317,318,319,328,333,346);


--------------------------------------------------------
--report.aspx Справки для выбранного жильца
--------------------------------------------------------
--!135
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 135,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 135,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 135,nzp_act,nzp_act,2,5 from s_actions where nzp_act in (201,202,203,204,205,208,214,215,216,218,219,223,224,226,227,237,238,241,242,243,268,284,301,312,313,314,330,331,337,338,339);


--------------------------------------------------------
--report.aspx Справки для выбранного списка жильцов
--------------------------------------------------------
--!136
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 136,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 136,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 136,nzp_act,nzp_act,2,5 from s_actions where nzp_act in (220,221,222,225,283,332,334);


--------------------------------------------------------
--report.aspx Отчеты для выбранного дома
--------------------------------------------------------
--!137
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 137,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107, 75,137,340) 
   or nzp_page >= 950                 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 137,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 137,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 137,nzp_act,nzp_act,2,5 from s_actions where nzp_act in (260);
  

--------------------------------------------------------
--report.aspx Отчеты для выбранных заявок
--------------------------------------------------------
--!138
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 138,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 77,207,247, 80,208,209, 254,255,267,282)
   or nzp_page >= 950                 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 138,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 138,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 138,nzp_act,nzp_act,2,5 from s_actions where nzp_act in (263,264,265,276,277,278,279,280,281,282);

--------------------------------------------------------
--report.aspx Отчеты для выбранных плановых работ
--------------------------------------------------------
--!139
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 139,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 77,207,247, 80,208,209, 254,255,267,282)
   or nzp_page >= 950                 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 139,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 139,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 139,nzp_act,nzp_act,2,5 from s_actions where nzp_act in (273,274,275);  


--------------------------------------------------------
--users.aspx
--------------------------------------------------------
--!151
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 151,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155 ,160,161, 73,44,256, 77,268, 270,271,293,342) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,2,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,3,0,0 from s_actions where nzp_act in (3);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,4,0,0 from s_actions where nzp_act in (9);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,5,0,0 from s_actions where nzp_act in (13);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,6,0,0 from s_actions where nzp_act in (95);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,7,0,0 from s_actions where nzp_act in (96);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (530, 531);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (608,609,612);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 151,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page, nzp_act, page_url) values (151, 3, 153);
insert into actions_lnk (cur_page, nzp_act, page_url) values (151, 4, 153);
insert into actions_lnk (cur_page, nzp_act, page_url) values (151, 5, 151);
insert into actions_lnk (cur_page, nzp_act, page_url) values (151, 95, 151);
insert into actions_lnk (cur_page, nzp_act, page_url) values (151, 96, 153);


--------------------------------------------------------
--roles.aspx
--------------------------------------------------------
--!152
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 152,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155 ,160,161, 73,44,256, 77,268, 270,271,293,342) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 152,nzp_act,1,0,0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 152,nzp_act,2,0,0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 152,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (152, 3, 154);
insert into actions_lnk (cur_page, nzp_act, page_url) values (152, 169, 154);
insert into actions_lnk (cur_page, nzp_act, page_url) values (152, 170, 155);

--------------------------------------------------------
--usercard.aspx
--------------------------------------------------------
--!153
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 153,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155, 160,161, 73,44,256, 77,268, 270,271,293,342) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 153,nzp_act,1,0,0 from s_actions where nzp_act in (96);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 153,nzp_act,2,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 153,nzp_act,3,0,0 from s_actions where nzp_act in (15);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 153,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 153,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page, nzp_act, page_url) values (153, 15, 153);
insert into actions_lnk (cur_page, nzp_act, page_url) values (153, 61, 153);
insert into actions_lnk (cur_page, nzp_act, page_url) values (153, 96, 153);


--------------------------------------------------------
--rolecard.aspx
--------------------------------------------------------
--!154
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 154,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155, 160,161, 73,44,256, 77,268, 270,271,293,342) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 154,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61,68);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 154,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 154,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


--------------------------------------------------------
--access.aspx
--------------------------------------------------------
--!155
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 155,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155, 160,161, 73,44,256, 77,268, 270,271,342) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 155,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 155,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page, nzp_act, page_url) values (155, 5, 155);

--------------------------------------------------------
--processes.aspx
--------------------------------------------------------
--!161
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 161,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155, 160,161, 73,44,256, 77,268, 270,271,293,342) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 161,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,5,10,11,12,61,70,113,129,137,167);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 161,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (532, 533, 534, 535);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 161,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 161,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 4, 266);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 5, 161);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 10, 161);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 11, 161);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 12, 161);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 61, 161);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 70, 161);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 113, 161);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 129, 161);
insert into actions_lnk (cur_page, nzp_act, page_url) values (161, 137, 161);


--------------------------------------------------------
--spisgil.aspx поквартирная карточки
--------------------------------------------------------
--!162
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 162, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 75,135, 72,81,82,106,107,240,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 162, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (3,4,5,23,74,75,109,551,552);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 162, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 162, nzp_act, nzp_act, 2, 2 from s_actions where nzp_act in (861,862,863);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 162, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (851,852,853,854);


insert into actions_lnk (cur_page, nzp_act, page_url) values (162, 3, 91);
insert into actions_lnk (cur_page, nzp_act, page_url) values (162, 4, 91);
insert into actions_lnk (cur_page, nzp_act, page_url) values (162, 5, 162);
insert into actions_lnk (cur_page, nzp_act, page_url) values (162, 23, 91);
insert into actions_lnk (cur_page, nzp_act, page_url) values (162, 74, 162);
insert into actions_lnk (cur_page, nzp_act, page_url) values (162, 75, 162);
insert into actions_lnk (cur_page, nzp_act, page_url) values (162, 109, 242);

--------------------------------------------------------
--spisglp.aspx список периодов временного убытия
--------------------------------------------------------
--!163
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 163, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 70,91,162) 
   or nzp_page >= 950 
   or nzp_page <  10;
   
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 163, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (3, 4, 5, 26, 70);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 163, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610, 611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 163, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (852);

insert into actions_lnk (cur_page, nzp_act, page_url) values (163,  3, 164);
insert into actions_lnk (cur_page, nzp_act, page_url) values (163,  4, 164);
insert into actions_lnk (cur_page, nzp_act, page_url) values (163,  5, 163);
insert into actions_lnk (cur_page, nzp_act, page_url) values (163, 26, 163);

--------------------------------------------------------
--glp.aspx период временного убытия
--------------------------------------------------------
--!164
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 164, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56,163, 70,91,162) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip,act_dd) select 164, nzp_act,nzp_act, 0, 0 from s_actions where nzp_act in (61, 70);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip,act_dd) select 164, nzp_act,nzp_act, 2, 1 from s_actions where nzp_act in (610, 611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (164, 61, 164);
      
--------------------------------------------------------
--perekidka.aspx изменения сальдо ЛС
--------------------------------------------------------
--!165
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 165,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 165,nzp_act,1,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 165,nzp_act,2,0,0 from s_actions where nzp_act in (178);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 165,nzp_act,3,0,0 from s_actions where nzp_act in (190);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 165,nzp_act,4,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 165,nzp_act,5,0,0 from s_actions where nzp_act in (180);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 165,nzp_act,6,0,0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 165,nzp_act,7,0,0 from s_actions where nzp_act in (70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 165,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (165,4,165);
insert into actions_lnk (cur_page,nzp_act,page_url) values (165,61,165);
insert into actions_lnk (cur_page,nzp_act,page_url) values (165,70,165);
insert into actions_lnk (cur_page,nzp_act,page_url) values (165,158,165);
insert into actions_lnk (cur_page,nzp_act,page_url) values (165,178,302);
insert into actions_lnk (cur_page,nzp_act,page_url) values (165,180,303);
insert into actions_lnk (cur_page,nzp_act,page_url) values (165,190,316);

--------------------------------------------------------
--rashod.aspx расходы по квартире
-------------------------------------------------------
--!166
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 166,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 166,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 166,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

--------------------------------------------------------
--rashod.aspx расходы по дому
-------------------------------------------------------
--!167
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 167,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 167,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 167,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

--------------------------------------------------------
--tarifs.aspx тарифы
-------------------------------------------------------
--!168
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 168,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 168,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61,67);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 168,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 168,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (168,67,169);

--------------------------------------------------------
--prm.aspx Значения одного параметра (тарифа, системного, т.е. не зависящего от квартиры, дома и т.д.)
-------------------------------------------------------
--!169
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 169,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,221,224,256,257,261, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 169,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 169,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (169,5,169);
insert into actions_lnk (cur_page,nzp_act,page_url) values (169,16,169);
insert into actions_lnk (cur_page,nzp_act,page_url) values (169,17,169);


--------------------------------------------------------
--sysparams.aspx системные параметры
-------------------------------------------------------
--!170
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 170,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 170,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 170,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 170,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (170,61,170);
insert into actions_lnk (cur_page,nzp_act,page_url) values (170,67,169);


--------------------------------------------------------
--prm.aspx значения параметра прибора учета (из данных о квартире)
-------------------------------------------------------
--!171
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 171,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 171,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 171,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (171,5,171);
insert into actions_lnk (cur_page,nzp_act,page_url) values (171,16,171);
insert into actions_lnk (cur_page,nzp_act,page_url) values (171,17,171);


--------------------------------------------------------
--prm.aspx значения параметра прибора учета (из данных о доме)
-------------------------------------------------------
--!172
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 172,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 172,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 172,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (172,5,172);
insert into actions_lnk (cur_page,nzp_act,page_url) values (172,16,172);
insert into actions_lnk (cur_page,nzp_act,page_url) values (172,17,172);


--------------------------------------------------------
--prm.aspx значения квартирного параметра
-------------------------------------------------------
--!173
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 173,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 173,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 173,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (173,5,173);
insert into actions_lnk (cur_page,nzp_act,page_url) values (173,16,173);
insert into actions_lnk (cur_page,nzp_act,page_url) values (173,17,173);


--------------------------------------------------------
--prm.aspx Архив значений параметра поставщика
-------------------------------------------------------
--!174
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 174,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,175,168,170,221,224,256,257,261, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 174,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 174,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (174,5,174);
insert into actions_lnk (cur_page,nzp_act,page_url) values (174,16,174);
insert into actions_lnk (cur_page,nzp_act,page_url) values (174,17,174);


--------------------------------------------------------
--suppparams.aspx параметры поставщика
-------------------------------------------------------
--!175
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 175,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 175,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 175,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page,nzp_act,page_url) values (175,5,175);
insert into actions_lnk (cur_page,nzp_act,page_url) values (175,67,174);


--------------------------------------------------------
--frmparams.aspx параметры формул расчета услуги
--------------------------------------------------------
--!176
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 176,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 176,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 176,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (176,67,173);


--------------------------------------------------------
--spissobstw.aspx собственники квартиры
--------------------------------------------------------
--!177
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 177, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,34, 40,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 177, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (3,4,5,26,552);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 177, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610, 611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 177, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (852);

insert into actions_lnk (cur_page, nzp_act, page_url) values (177, 3, 178);
insert into actions_lnk (cur_page, nzp_act, page_url) values (177, 4, 178);
insert into actions_lnk (cur_page, nzp_act, page_url) values (177, 5, 177);
insert into actions_lnk (cur_page, nzp_act, page_url) values (177, 26, 177);


--------------------------------------------------------
--kartsobstw.aspx собственник квартиры
--------------------------------------------------------
--!178
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 178, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,34, 40,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 178, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 178, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610, 611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (178, 61, 178);


--------------------------------------------------------
--spisnd.aspx групповые операции с домовыми недопоставками
--------------------------------------------------------
--!179
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 179,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,102,109,179,180,182,241, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 179,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (18,19);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 179,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);


--------------------------------------------------------
--spisprm.aspx квартирные параметры для групповых операций с лицевыми счетами выбранных домов
--------------------------------------------------------
--!180
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 180,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,102,109,179,180,182,241, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 180,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 180,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 180,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (180,5,180);
insert into actions_lnk (cur_page,nzp_act,page_url) values (180,67,181);


--------------------------------------------------------
--prm.aspx групповые операций с квартирными параметрами лицевых счетов выбранных домов
--------------------------------------------------------
--!181
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 181,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,102,109,179,180,182,241, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 181,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 181,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (181,16,181);
insert into actions_lnk (cur_page,nzp_act,page_url) values (181,17,181);

--------------------------------------------------------
--spisserv.aspx список услуг для групповых операций с ЛС выбранных домов
--------------------------------------------------------
--!182
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 182,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,102,109,179,180,182,241, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 182,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 182,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (182,5,182);


--------------------------------------------------------
--serv.aspx групповые операции с выбранной услугой по ЛС из выбранных домов
--------------------------------------------------------
--!183
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 183,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 74,102,109,179,180,182,241, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 183,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (21,22);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 183,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (183,21,183);
insert into actions_lnk (cur_page,nzp_act,page_url) values (183,22,183);


--------------------------------------------------------
--reportodn.aspx протокол расчета ОДН
-------------------------------------------------------
--!184
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 184,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select p.nzp_page,nzp_act,nzp_act,0,0 from s_actions a, pages p where nzp_act in (5) and p.nzp_page = 184;
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select p.nzp_page,nzp_act,nzp_act,2,1 from s_actions a, pages p where nzp_act in (610,611) and p.nzp_page = 184;


--------------------------------------------------------
--список коммунальных ПУ
--------------------------------------------------------
--!185
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 185,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340, 73,94) 
   or nzp_page >= 999 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 185,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (3,4,5,14,64);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 185,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (185, 3, 187);
insert into actions_lnk (cur_page, nzp_act, page_url) values (185, 4, 187);
insert into actions_lnk (cur_page, nzp_act, page_url) values (185, 5, 185);
insert into actions_lnk (cur_page, nzp_act, page_url) values (185, 14, 186);
insert into actions_lnk (cur_page, nzp_act, page_url) values (185, 81, 193);


--------------------------------------------------------
--spisval.aspx показания коммунальных ПУ
--------------------------------------------------------
--!186
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 186,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 999 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 186,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 186,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 186,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (71,72);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 186,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (111,112);


insert into actions_lnk (cur_page,nzp_act,page_url) values (186,5,186);
insert into actions_lnk (cur_page,nzp_act,page_url) values (186,61,186);


--------------------------------------------------------
--countercard.aspx карточка коммунального ПУ
--------------------------------------------------------
--!187
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 187,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340, 73,94) 
   or nzp_page >= 999 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 187,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (14,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 187,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (187, 14, 186);
insert into actions_lnk (cur_page, nzp_act, page_url) values (187, 61, 187);
insert into actions_lnk (cur_page, nzp_act, page_url) values (187, 67, 172);


--------------------------------------------------------
--prm.aspx значения домового параметра
-------------------------------------------------------
--!188
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 188,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 188,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 188,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (188,5,188);
insert into actions_lnk (cur_page,nzp_act,page_url) values (188,16,188);
insert into actions_lnk (cur_page,nzp_act,page_url) values (188,17,188);


--------------------------------------------------------
--orders.aspx заявки по ЛС
-------------------------------------------------------
--!189
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 189,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 70,55,122,189,238,251, 73,252,289, 78,205, 77,207,247, 80,208,209, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 189,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (84,94);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 189,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (189,84,189);
insert into actions_lnk (cur_page,nzp_act,page_url) values (189,94,189);
insert into actions_lnk (cur_page,nzp_act,page_url) values (189,3,205);


--------------------------------------------------------
--spisservdom.aspx перечень услуг
--------------------------------------------------------
--!190
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 190,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 190,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 190,nzp_act,2,0,0 from s_actions where nzp_act in (80);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 190,nzp_act,3,0,0 from s_actions where nzp_act in (70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 190,nzp_act,4,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 190,nzp_act,5,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 190,nzp_act,6,0,0 from s_actions where nzp_act in (22);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 190,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (190, 5, 190);
insert into actions_lnk (cur_page, nzp_act, page_url) values (190, 22, 190);
insert into actions_lnk (cur_page, nzp_act, page_url) values (190, 4, 190);
insert into actions_lnk (cur_page, nzp_act, page_url) values (190, 61, 190);
insert into actions_lnk (cur_page, nzp_act, page_url) values (190, 70, 190);
insert into actions_lnk (cur_page, nzp_act, page_url) values (190, 80, 50);


--------------------------------------------------------
--spisnddom.aspx недопоставки дома
--------------------------------------------------------
--!191
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 191,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 191,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,18,19,70,80);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 191,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 191,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (191,5,191);
insert into actions_lnk (cur_page,nzp_act,page_url) values (191,18,191);
insert into actions_lnk (cur_page,nzp_act,page_url) values (191,19,191);
insert into actions_lnk (cur_page,nzp_act,page_url) values (191,80,50);


--------------------------------------------------------
--spisprmreg.aspx настройки ОДН
--------------------------------------------------------
--!192
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 192,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 192,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 192,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (192,61,192);
insert into actions_lnk (cur_page,nzp_act,page_url) values (192,67,188);


--------------------------------------------------------
--joborder.aspx наряд-заказ
-------------------------------------------------------
--!193
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 193,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 70,189,238,55,122,251, 73,252,289, 77,207,247, 78,205, 80,208,209, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 193,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61,66,97,98,130,136);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 193,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (193,5,193);
insert into actions_lnk (cur_page,nzp_act,page_url) values (193,66,193);
insert into actions_lnk (cur_page,nzp_act,page_url) values (193,136,193);


--------------------------------------------------------
--spisgil.aspx история карточек жильца
--------------------------------------------------------
--!194
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 194, nzp_page, 0, nzp_page from pages
where nzp_page in (40,31,41,56, 70,162, 75,135) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 194, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (3, 5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 194, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610, 611);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 194, nzp_act, nzp_act, 2, 2 from s_actions where nzp_act in (861, 862);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 194, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (701, 702, 703,704,705);

insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 194, nzp_act, nzp_act, 2, 4 from s_actions where nzp_act in (601, 602, 607);

insert into actions_lnk (cur_page, nzp_act, page_url) values (194, 3, 91);
insert into actions_lnk (cur_page, nzp_act, page_url) values (194, 5, 194);


--------------------------------------------------------
--findgroupls.aspx шаблон поиска по группам ЛС
--------------------------------------------------------
--!195
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 195, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 72,81,82,106,107, 73,44,45,211,212,213,214,215,216,217,218,219,221,224,256,257,261, 74,241,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 195, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 195, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,502,503,504,505,506,507,508);

insert into actions_lnk (cur_page, nzp_act, page_url) values (195, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (195, 1, 42);
insert into actions_lnk (cur_page, nzp_act, page_url) values (195, 1, 56);
insert into actions_lnk (cur_page, nzp_act, page_url) values (195, 2, 195);


--------------------------------------------------------
--group.aspx групповые операции с группами лицевых счетов
--------------------------------------------------------
--!196
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 196,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (82,83);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 196,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (196,82,196);
insert into actions_lnk (cur_page,nzp_act,page_url) values (196,83,196);


--------------------------------------------------------
--baselist.aspx список выбранных заявок
--------------------------------------------------------
--!197
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 197,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,197 ,41,246, 70,189,238,55,122,251, 73,248,252,289, 75,138, 77,220,247, 78,193,205,207, 80,208,209, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 197,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,100,101);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 197,nzp_act,nzp_act,2,2 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 197,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 197,nzp_act,nzp_act,2,4 from s_actions where nzp_act in (601,602,607);

insert into actions_lnk (cur_page, nzp_act, page_url) values (197, 5, 197);


--------------------------------------------------------
--kassa/pack.aspx пачка оплат
--------------------------------------------------------
--!198
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 198,nzp_page,0,nzp_page from pages
where nzp_page in (76,198, 77,200, 235,203) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 198,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (3,61,85,86,87,90);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 198,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 198,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page, nzp_act, page_url) values (198, 3, 199);
insert into actions_lnk (cur_page, nzp_act, page_url) values (198, 85, 198);
insert into actions_lnk (cur_page, nzp_act, page_url) values (198, 86, 198);
insert into actions_lnk (cur_page, nzp_act, page_url) values (198, 87, 199);
insert into actions_lnk (cur_page, nzp_act, page_url) values (198, 90, 198);


--------------------------------------------------------
--kassa/packls.aspx оплата
--------------------------------------------------------
--!199
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 199,nzp_page,0,nzp_page from pages
where nzp_page in (76,198) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 199,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61,89);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 199,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (199, 61, 199);
insert into actions_lnk (cur_page, nzp_act, page_url) values (199, 89, 31);


--------------------------------------------------------
--uploadpack.aspx загрузка электронного реестра оплат
--------------------------------------------------------
--!200
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 200,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 235,107,203,253,258,259,265, 73,232,233,239,256,257, 270,269,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 200,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (88);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 200,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (200, 88, 200);


--------------------------------------------------------
--findpack.aspx шаблон поиска по оплатам
--------------------------------------------------------
--!201
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 1, 0, 0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 2, 0, 0 from s_actions where nzp_act in (2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 3, 0, 0 from s_actions where nzp_act in (3);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 4, 0, 0 from s_actions where nzp_act in (85);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 5, 0, 0 from s_actions where nzp_act in (88);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 6, 0, 0 from s_actions where nzp_act in (91);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 7, 0, 0 from s_actions where nzp_act in (93);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 8, 0, 0 from s_actions where nzp_act in (92);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 9, 0, 0 from s_actions where nzp_act in (554);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, 10, 0, 0 from s_actions where nzp_act in (876);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, nzp_act, 2, 2 from s_actions where nzp_act in (610);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 201, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 1, 201);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 2, 201);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 3, 202);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 85, 311);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 88, 200);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 91, 201);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 92, 201);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 93, 201);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 554, 201);
insert into actions_lnk (cur_page, nzp_act, page_url) values (201, 876, 311);

--------------------------------------------------------
--finances/pack.aspx пачка оплат
--------------------------------------------------------
--!202
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 202, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 202, nzp_act, 2, 0, 0 from s_actions where nzp_act in (91);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 202, nzp_act, 3, 0, 0 from s_actions where nzp_act in (93);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 202, nzp_act, 4, 0, 0 from s_actions where nzp_act in (92);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 202, nzp_act, 5, 0, 0 from s_actions where nzp_act in (90);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 202, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 202, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (701,702,703);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 202, nzp_act, nzp_act, 2, 3 from s_actions where nzp_act in (705);

insert into actions_lnk (cur_page, nzp_act, page_url) values (202, 3, 204);
insert into actions_lnk (cur_page, nzp_act, page_url) values (202, 5, 202);
insert into actions_lnk (cur_page, nzp_act, page_url) values (202, 90, 202);
insert into actions_lnk (cur_page, nzp_act, page_url) values (202, 91, 202);
insert into actions_lnk (cur_page, nzp_act, page_url) values (202, 92, 201);
insert into actions_lnk (cur_page, nzp_act, page_url) values (202, 93, 202);
insert into actions_lnk (cur_page, nzp_act, page_url) values (202, 94, 202);


--------------------------------------------------------
--operday.aspx операционный день
--------------------------------------------------------
--!203
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 203,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 235,107,203,253,258,259,265, 73,232,233,239,256,257, 270,269, 76,198,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 203,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61,548,549);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 203,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (203, 548, 203);
insert into actions_lnk (cur_page, nzp_act, page_url) values (203, 549, 203);


--------------------------------------------------------
--finances/packls.aspx квитанция об оплате
--------------------------------------------------------
--!204
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 204,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 73,232,233,239,256,257, 235,107,202,203,253,258,259,265, 70,121, 270,269,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 204,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61,90,91,93,114,115);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 204,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (204, 61, 204);
insert into actions_lnk (cur_page, nzp_act, page_url) values (204, 90, 202);
insert into actions_lnk (cur_page, nzp_act, page_url) values (204, 91, 204);
insert into actions_lnk (cur_page, nzp_act, page_url) values (204, 93, 204);
insert into actions_lnk (cur_page, nzp_act, page_url) values (204, 114, 204);
insert into actions_lnk (cur_page, nzp_act, page_url) values (204, 115, 204);


--------------------------------------------------------
--order.aspx одна заявка
-------------------------------------------------------
--!205
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 205,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 70,189,238,55,122,251, 73,252,289, 77,207,247, 80,208,209, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 205,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61,81);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 205,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (205,81,193);


--------------------------------------------------------
--report.aspx отчеты по всему банку данных
-------------------------------------------------------
--!206
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 206,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195,201, 72,81,82,106,107, 75,206, 73,232,233,239,256,257, 235,107,202,203,253,258,259,265,303,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 206,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 206,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 206,nzp_act,nzp_act,2,5 from s_actions where nzp_act in (212,244,245,246,266,267,285,286,287,291,292,293,303,305,307,308,320,321,323,324,325,326,327,340,341,343,344,345,347,348,349,350);

insert into actions_lnk (cur_page,nzp_act,page_url) values (206,69,206);


--------------------------------------------------------
--armoperator.aspx добавление новой заявки
-------------------------------------------------------
--!207
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 207,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,252,289, 77,207,220,247, 80,208,209, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 207,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 207,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (207,61,207);


--------------------------------------------------------
--raworders.aspx необработанные заявки
-------------------------------------------------------
--!208
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 208,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,208 ,41,197,246, 73,248,252,289, 78,205, 77,207,247, 80,209, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 208,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 208,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page,nzp_act,page_url) values (208,5,208);


--------------------------------------------------------
--incomingjoborders.aspx поступившие наряды-заказы
-------------------------------------------------------
--!209
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 209,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,209,41,197,246, 73,248,252,289, 77,207,247, 80,208, 254,255,267)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 209,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 209,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page,nzp_act,page_url) values (209,5,209);

----------------------------------------------------------------
--norms.aspx список нормативов
----------------------------------------------------------------
--!210
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 210,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 210,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 210,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 210,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);


----------------------------------------------------------------
--sprav.aspx справочник целей прибытия/убытия
----------------------------------------------------------------
--!211
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 211,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 211,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 211,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 211 and nzp_act in (4,26,61);


----------------------------------------------------------------
--sprav.aspx справочник документов
----------------------------------------------------------------
--!212
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 212,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 212,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 212,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 212 and nzp_act in (4,26,61);


----------------------------------------------------------------
--sprav.aspx справочник родственных отношений
----------------------------------------------------------------
--!213
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 213,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 213,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 213,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 213 and nzp_act in (4,26,61);


----------------------------------------------------------------
--sprav.aspx справочник гражданств
----------------------------------------------------------------
--!214
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 214,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 214,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 214,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 214 and nzp_act in (4,26,61);


----------------------------------------------------------------
--sprav.aspx справочник адресов
----------------------------------------------------------------
--!215
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 215,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 215,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 215,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 215 and nzp_act in (4,26,61);


----------------------------------------------------------------
--sprav.aspx справочник районов дома
----------------------------------------------------------------
--!216
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 216,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 216,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 216,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 216 and nzp_act in (4,26,61);


----------------------------------------------------------------
--sprav.aspx справочник органов регистрационного учета
----------------------------------------------------------------
--!217
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 217,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 217,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 217,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 217 and nzp_act in (4,26,61);


----------------------------------------------------------------
--sprav.aspx справочник мест выдачи документов
----------------------------------------------------------------
--!218
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 218,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 218,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 218,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 218 and nzp_act in (4,26,61);


----------------------------------------------------------------
--sprav.aspx справочник документов о праве собственности
----------------------------------------------------------------
--!219
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 219,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,211,212,213,214,215,216,217,218,219,221,224,256,257,261)
   or nzp_page >= 950 
   or nzp_page <  10;

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 219,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 219,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) select cur_page, nzp_act, cur_page from actions_show where cur_page = 219 and nzp_act in (4,26,61);


--------------------------------------------------------
--nedop.aspx формирование недопоставок в СУПГ
--------------------------------------------------------
--!220
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 220, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195,245, 40,41,42,50,53,56,197,246, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,252,289, 77,207,220,247, 80,208,209, 254,255,267,282, 72,81,82,106,107,256,257,261,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 220, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (99,102,159,160,168,296); 
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 220, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (220, 99, 220);
insert into actions_lnk (cur_page, nzp_act, page_url) values (220, 102, 220);
insert into actions_lnk (cur_page, nzp_act, page_url) values (220, 159, 220);
insert into actions_lnk (cur_page, nzp_act, page_url) values (220, 160, 220);
insert into actions_lnk (cur_page, nzp_act, page_url) values (220, 168, 220);
insert into actions_lnk (cur_page, nzp_act, page_url) values (220, 296, 220);


--------------------------------------------------------
--services.aspx список услуг
--------------------------------------------------------
--!221
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 221, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224, 72,81,82,106,107,256,257,261,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 221, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 221, nzp_act, 2, 0, 0 from s_actions where nzp_act in (3);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 221, nzp_act, 3, 0, 0 from s_actions where nzp_act in (77);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 221, nzp_act, 4, 0, 0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 221, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610
,611 -- в разработке
);

insert into actions_lnk (cur_page, nzp_act, page_url) values (221, 3, 301);
insert into actions_lnk (cur_page, nzp_act, page_url) values (221, 4, 221);
insert into actions_lnk (cur_page, nzp_act, page_url) values (221, 5, 221);
insert into actions_lnk (cur_page, nzp_act, page_url) values (221, 77, 222);
insert into actions_lnk (cur_page, nzp_act, page_url) values (221, 169, 301);


--------------------------------------------------------
--servparams.aspx параметры услуги
-------------------------------------------------------
--!222
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 222,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 222,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 222,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 222,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (222,5,222);
insert into actions_lnk (cur_page,nzp_act,page_url) values (222,61,222);
insert into actions_lnk (cur_page,nzp_act,page_url) values (222,67,223);


--------------------------------------------------------
--prm.aspx значения выбранного параметра услуги
-------------------------------------------------------
--!223
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 223,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 223,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 223,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (223,5,223);
insert into actions_lnk (cur_page,nzp_act,page_url) values (223,16,223);
insert into actions_lnk (cur_page,nzp_act,page_url) values (223,17,223);


--------------------------------------------------------
--availableservices.aspx доступные услуги
--------------------------------------------------------
--!224
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 224,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 224,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,76);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 224,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 224,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page, nzp_act, page_url) values (224, 5, 224);
insert into actions_lnk (cur_page, nzp_act, page_url) values (224, 76, 225);


--------------------------------------------------------
--availableservice.aspx одна доступная услуга
--------------------------------------------------------
--!225
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 225,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 225,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,21,26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 225,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (225, 21, 225);
insert into actions_lnk (cur_page, nzp_act, page_url) values (225, 26, 225);

--------------------------------------------------------
--findserver.aspx поиск по серверам
--------------------------------------------------------
--!226
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select a.nzp_page, b.nzp_page, 0, b.nzp_page from pages a, pages b
where a.nzp_page = 226
   and (b.nzp_page in (30,31,33,34,35,36,37,38,195, 40,41,42,50,53,56, 72,81,82,106,107,340, 75,206)
   or b.nzp_page >= 950 
   or b.nzp_page <  10);
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select p.nzp_page, a.nzp_act, a.nzp_act, 0, 0 from s_actions a, pages p where a.nzp_act in (1, 5) and p.nzp_page = 226;
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select p.nzp_page, a.nzp_act, a.nzp_act, 1, 1 from s_actions a, pages p where a.nzp_act in (501) and p.nzp_page = 226;

insert into actions_lnk (cur_page, nzp_act, page_url) values (226, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (226, 5, 226);


--------------------------------------------------------
--areaparams.aspx параметры территории
-------------------------------------------------------
--!227
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 227,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 227,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 227,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 227,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (227,5,227);
insert into actions_lnk (cur_page,nzp_act,page_url) values (227,61,227);
insert into actions_lnk (cur_page,nzp_act,page_url) values (227,67,228);


--------------------------------------------------------
--prm.aspx значения выбранного параметра территории
-------------------------------------------------------
--!228
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 228,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 228,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 228,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (228,5,228);
insert into actions_lnk (cur_page,nzp_act,page_url) values (228,16,228);
insert into actions_lnk (cur_page,nzp_act,page_url) values (228,17,228);


--------------------------------------------------------
--geuparams.aspx параметры участка
-------------------------------------------------------
--!229
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 229,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 229,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 229,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (20);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 229,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (229,5,229);
insert into actions_lnk (cur_page,nzp_act,page_url) values (229,61,229);
insert into actions_lnk (cur_page,nzp_act,page_url) values (229,67,230);


--------------------------------------------------------
--prm.aspx значения выбранного параметра участка
-------------------------------------------------------
--!230
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 230,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 230,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,16,17);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 230,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (230,5,230);
insert into actions_lnk (cur_page,nzp_act,page_url) values (230,16,230);
insert into actions_lnk (cur_page,nzp_act,page_url) values (230,17,230);


--------------------------------------------------------
--arearequisites.aspx реквизиты территории
-------------------------------------------------------
--!231
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 231,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261,284, 72,81,82,106,107,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 231,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 231,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);



insert into actions_lnk (cur_page,nzp_act,page_url) values (231,61,231);


--------------------------------------------------------
--payerrequisites.aspx счета подрядчиков
--------------------------------------------------------
--!232
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 232,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 72,81,82,106,107,278, 73,239,256, 75,206, 77,203, 235,107,203,253,258,259,265, 270,269, 290,232,233,257, 291,272,274,279,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 232,nzp_act,1,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 232,nzp_act,2,0,0 from s_actions where nzp_act in (61);
--insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 232,nzp_act,3,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 232,nzp_act,4,0,0 from s_actions where nzp_act in (5);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 232,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (232,4,232);
insert into actions_lnk (cur_page,nzp_act,page_url) values (232,26,232);
insert into actions_lnk (cur_page,nzp_act,page_url) values (232,5,232);
insert into actions_lnk (cur_page,nzp_act,page_url) values (232,61,232);


--------------------------------------------------------
--contracts.aspx договоры между УК и подрядчиком
--------------------------------------------------------
--!233
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 233,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 72,81,82,106,107,278, 73,239,256, 75,206, 77,203, 235,107,203,253,258,259,265, 270,269, 290,232,233,257, 291,272,274,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 233,nzp_act,1,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 233,nzp_act,2,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 233,nzp_act,3,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 233,nzp_act,4,0,0 from s_actions where nzp_act in (5);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 233,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (233,4,233);
insert into actions_lnk (cur_page,nzp_act,page_url) values (233,26,233);
insert into actions_lnk (cur_page,nzp_act,page_url) values (233,5,233);
insert into actions_lnk (cur_page,nzp_act,page_url) values (233,61,233);


--------------------------------------------------------
--payertransfer.aspx перечисления подрядчикам
--------------------------------------------------------
--!234
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 234,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 73,232,233,239,256,257, 235,107,203,253,258,259,265, 270,269,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 234,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 234,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (234,61,234);

--------------------------------------------------------
--kvarjoborder.aspx заявки по ЛС
-------------------------------------------------------
--!238
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 238,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 70,55,122,189,251,238, 73,252,289, 78,205, 77,207,247, 80,208,209, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 238,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,69);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 238,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_lnk (cur_page,nzp_act,page_url) values (238,5,238);


--------------------------------------------------------
--percent.aspx процент удержания
--------------------------------------------------------
--!239
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 239,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 72,81,82,106,107, 73,232,233,239,256,257, 75,206, 77,203, 235,107,203,253,258,259,265, 270,269,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 239,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 239,nzp_act,2,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 239,nzp_act,3,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 239,nzp_act,4,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 239,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 239,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page,nzp_act,page_url) values (239,5,239);
insert into actions_lnk (cur_page,nzp_act,page_url) values (239,4,239);
insert into actions_lnk (cur_page,nzp_act,page_url) values (239,61,239);
insert into actions_lnk (cur_page,nzp_act,page_url) values (239,26,239);

--------------------------------------------------------
--lscontracts.aspx договора на лицевых счетах 
--------------------------------------------------------
--!240
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 240,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,43,44,45,46,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,240,244,262, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 

   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 240,nzp_act,1,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 240,nzp_act,2,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 240,nzp_act,3,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 240,nzp_act,4,0,0 from s_actions where nzp_act in (5);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 240,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (240,4,240);
insert into actions_lnk (cur_page,nzp_act,page_url) values (240,26,240);
insert into actions_lnk (cur_page,nzp_act,page_url) values (240,5,240);
insert into actions_lnk (cur_page,nzp_act,page_url) values (240,61,240);


--------------------------------------------------------
--counterreadings.aspx ввод показаний ПУ списком
--------------------------------------------------------
--!241
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 241,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 72,81,82,106,107, 74,241, 77,243,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 241,nzp_act,1,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 241,nzp_act,2,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 241,nzp_act,3,0,0 from s_actions where nzp_act in (177);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 241,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 241,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);


insert into actions_lnk (cur_page,nzp_act,page_url) values (241,5,241);
insert into actions_lnk (cur_page,nzp_act,page_url) values (241,61,241);
insert into actions_lnk (cur_page,nzp_act,page_url) values (241,177,241);

--------------------------------------------------------
--glp.aspx добавление периода временного убытия выбранным жильцам
--------------------------------------------------------
--!242
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 242, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56,163, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip,act_dd) select 242, nzp_act,nzp_act, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip,act_dd) select 242, nzp_act,nzp_act, 2, 1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (242, 61, 242);
      

--------------------------------------------------------
--uploadcounters.aspx загрузка показаний ПУ из файла
--------------------------------------------------------
--!243
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 243,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 72,81,82,106,107, 74,241, 77,243,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 243,nzp_act,1,0,0 from s_actions where nzp_act in (110);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 243,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (243,110,243);


--------------------------------------------------------
--credit.aspx Рассрочка
--------------------------------------------------------
--!244
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 244,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,240,244,262, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240) 
   or nzp_page >= 950                 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select p.nzp_page,nzp_act,nzp_act,0,0 from s_actions a, pages p where nzp_act in (5,61) and p.nzp_page = 244;
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select p.nzp_page,nzp_act,nzp_act,2,3 from s_actions a, pages p where nzp_act in (701,702,703,705) and p.nzp_page = 244;
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 244,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (244,5,244);
insert into actions_lnk (cur_page,nzp_act,page_url) values (244,61,244);
  

--------------------------------------------------------
--findplannedworks.aspx шаблон поиска по плановым работам
--------------------------------------------------------
--!245
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 245, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,39, 40,41,197,245,246, 73,248,252,289, 77,220,207,247, 80,208,209 ,254,255,267,282) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 245, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 245, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501);

insert into actions_lnk (cur_page, nzp_act, page_url) values (245, 1, 41);
insert into actions_lnk (cur_page, nzp_act, page_url) values (245, 1, 197);
insert into actions_lnk (cur_page, nzp_act, page_url) values (245, 1, 246);
insert into actions_lnk (cur_page, nzp_act, page_url) values (245, 2, 245);


--------------------------------------------------------
--plannedworks.aspx список выбранных плановых работ
--------------------------------------------------------
--!246
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 246,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,246,41,197, 73,248,252,289, 75,139, 77,220, 80,208,209 ,247, 254,255,267)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 246,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 246,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 246,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page, nzp_act, page_url) values (246, 5, 246);
insert into actions_lnk (cur_page, nzp_act, page_url) values (246, 3, 247);


--------------------------------------------------------
--plannedwork.aspx Плановая работа
--------------------------------------------------------
--!247
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 247,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,252,289, 77, 207 ,220,247, 80,208,209, 254,255,267,282)                             
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 247,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61,135,165);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 247,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (247, 5, 247);


--------------------------------------------------------
--serviceorgs.aspx справочник служб / организаций
--------------------------------------------------------
--!248
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 248,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,248,252,289,  77,220, 80,208,209,267)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 248,nzp_act,1,0,0 from s_actions where nzp_act in (4,5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 248,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (248, 4, 248);
insert into actions_lnk (cur_page, nzp_act, page_url) values (248, 5, 248);
insert into actions_lnk (cur_page, nzp_act, page_url) values (248, 61, 248);
insert into actions_lnk (cur_page, nzp_act, page_url) values (248, 135, 248);



--------------------------------------------------------
--serviceorg.aspx Служба, организация
--------------------------------------------------------
--!249
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 249,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,248,252,289, 77,220, 80,208,209,267)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 249,nzp_act,1,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 249,nzp_act,nzp_act,2,2 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (249, 61, 249);

--------------------------------------------------------
--plannedworks.aspx список выбранных плановых работ по данному ЛС
--------------------------------------------------------
--!251
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 251,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197, 70,55,122,189,238,251, 73,248,252,289, 75,139, 77,220, 80,208,209 ,247, 254,255,267)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 251,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 251,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 251,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page, nzp_act, page_url) values (251, 5, 251);
insert into actions_lnk (cur_page, nzp_act, page_url) values (251, 3, 247);

--------------------------------------------------------
--claimcatalog.aspx справочник претензий
-------------------------------------------------------
--!252
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 252,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,248,252,289,  77,207,247, 80,208,209, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 252,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 252,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (252,5,252);
insert into actions_lnk (cur_page,nzp_act,page_url) values (252,61,252);


--------------------------------------------------------
--finances/case.aspx портфель
--------------------------------------------------------
--!253
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 253,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 73,232,233,239,256,257, 235,107,203,253,258,259,265, 270,269,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 253,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (115,116);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 253,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (253, 115, 253);
insert into actions_lnk (cur_page, nzp_act, page_url) values (253, 116, 253);


--------------------------------------------------------
--analisis.aspx статистика в СУПГ
--------------------------------------------------------
--!255
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 255,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,252,289, 77, 207 ,220,247, 80,208,209,267,282)                             
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select p.nzp_page,a.nzp_act,a.nzp_act,0,0 from s_actions a, pages p where nzp_act in (5,117) and p.nzp_page = 255;
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select p.nzp_page,a.nzp_act,a.nzp_act,2,1 from s_actions a, pages p where nzp_act in (610) and p.nzp_page = 255;

insert into actions_lnk (cur_page, nzp_act, page_url) values (255, 5, 255);


--------------------------------------------------------
--contractorcatalog.aspx справочник контрагентов
--------------------------------------------------------
--!256
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 256, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195,201, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,224,256, 72,81,82,106,107,278, 150,151,152,155 ,160,161, 270,269, 290,232,233,257, 291,272,274,279,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 256, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 256, nzp_act, 2, 0, 0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 256, nzp_act, 3, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 256, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (256, 4, 256);
insert into actions_lnk (cur_page, nzp_act, page_url) values (256, 5, 256);
insert into actions_lnk (cur_page, nzp_act, page_url) values (256, 61, 256);


--------------------------------------------------------
--bankcatalog.aspx справочник банков
--------------------------------------------------------
--!257
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 257, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195,201, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,224,256, 72,81,82,106,107,278, 270,269, 290,232,233,257, 291,272,274,279,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 257, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 257, nzp_act, 2, 0, 0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 257, nzp_act, 3, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 257, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (257, 4, 257);
insert into actions_lnk (cur_page, nzp_act, page_url) values (257, 5, 257);
insert into actions_lnk (cur_page, nzp_act, page_url) values (257, 61, 257);


--------------------------------------------------------
--finances/basket.aspx Корзина
--------------------------------------------------------
--!258
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 258,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 73,232,233,239,256,257, 235,107,203,253,258,259,265, 270,269,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,2,0,0 from s_actions where nzp_act in (2);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,3,0,0 from s_actions where nzp_act in (127);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,4,0,0 from s_actions where nzp_act in (118);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,5,0,0 from s_actions where nzp_act in (119);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,6,0,0 from s_actions where nzp_act in (120);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,7,0,0 from s_actions where nzp_act in (121);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,8,0,0 from s_actions where nzp_act in (171);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,1,0,0 from s_actions where nzp_act in (90);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 258,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (258, 1, 258);
insert into actions_lnk (cur_page, nzp_act, page_url) values (258, 2, 258);
insert into actions_lnk (cur_page, nzp_act, page_url) values (258, 90, 258);
insert into actions_lnk (cur_page, nzp_act, page_url) values (258, 118, 258);
insert into actions_lnk (cur_page, nzp_act, page_url) values (258, 119, 258);
insert into actions_lnk (cur_page, nzp_act, page_url) values (258, 120, 258);
insert into actions_lnk (cur_page, nzp_act, page_url) values (258, 121, 258);
insert into actions_lnk (cur_page, nzp_act, page_url) values (258, 127, 204);


--------------------------------------------------------
--finances/payerreval.aspx Перекидки между подрядчиками
--------------------------------------------------------
--!259
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 259,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 73,232,233,239,256,257, 235,107,203,253,258,259,265, 270,269,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 259,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 259,nzp_act,2,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 259,nzp_act,3,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 259,nzp_act,4,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 259,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (259, 1, 259);
insert into actions_lnk (cur_page, nzp_act, page_url) values (259, 4, 259);
insert into actions_lnk (cur_page, nzp_act, page_url) values (259, 26, 259);
insert into actions_lnk (cur_page, nzp_act, page_url) values (259, 61, 259);


--------------------------------------------------------
--gendomls.aspx генерация ЛС
--------------------------------------------------------
--!260
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 260, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107, 73,44,45,211,212,213,214,215,216,217,218,219,221,224,256,257,261, 74,241,340) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 260, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (122);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 260,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (260, 122, 50);

--------------------------------------------------------
--streetcatalog.aspx справочник улиц
--------------------------------------------------------
--!261
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 261, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224, 72,81,82,106,107,256,257,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 261, nzp_act, 1, 0, 0 from s_actions where nzp_act in (1,4,61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 261, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (261, 1, 261);
insert into actions_lnk (cur_page, nzp_act, page_url) values (261, 4, 261);
insert into actions_lnk (cur_page, nzp_act, page_url) values (261, 61, 261);


--------------------------------------------------------
--correctsaldo.aspx корректировка входящего сальдо
--------------------------------------------------------
--!262
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 262,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,240,244,262, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,240,340) 
   or nzp_page >= 950                 
   or nzp_page <  10;
   
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 262,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61,70);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 262,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (262,5,262);
insert into actions_lnk (cur_page,nzp_act,page_url) values (262,61,262);

--------------------------------------------------------
--groupprm.aspx групповой ввод характеристик жилья
--------------------------------------------------------
--!263
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 263,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 263,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (263,5,263);
insert into actions_lnk (cur_page,nzp_act,page_url) values (263,61,263);
  

--------------------------------------------------------
--genlspu.aspx генерация ИПУ по списку ЛС
--------------------------------------------------------
--!264
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 264,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (123);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 264,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (264,123,264);

--------------------------------------------------------
--condistrpayments.aspx Контроль распределения оплат
--------------------------------------------------------
--!265
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 265,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 73,232,233,239,256,257, 235,107,203,253,258,259,265, 270,269,340)
   or nzp_page >= 950                                 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select p.nzp_page,a.nzp_act,a.nzp_act,0,0 from s_actions a, pages p where a.nzp_act in (124,125,126,181,544,550) and p.nzp_page = 265;
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select p.nzp_page,a.nzp_act,a.nzp_act,2,1 from s_actions a, pages p where nzp_act in (610) and p.nzp_page = 265;

insert into actions_lnk (cur_page, nzp_act, page_url) values (265, 124, 265);
insert into actions_lnk (cur_page, nzp_act, page_url) values (265, 125, 265);
insert into actions_lnk (cur_page, nzp_act, page_url) values (265, 126, 265);
insert into actions_lnk (cur_page, nzp_act, page_url) values (265, 181, 265);
insert into actions_lnk (cur_page, nzp_act, page_url) values (265, 544, 265);
insert into actions_lnk (cur_page, nzp_act, page_url) values (265, 550, 265);


--------------------------------------------------------
--addtask.aspx добавление заданий
--------------------------------------------------------
--!266
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 266,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155, 160,161)
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 266,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 266,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (266, 61, 266);

--------------------------------------------------------
--surveyjoborders.aspx Список нарядов-заказов для опроса
-------------------------------------------------------
--!267
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 267,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,209,41,197,246, 73,248,252,289, 77,207,247, 80,208, 254,255,267,282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 267,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 267,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page,nzp_act,page_url) values (267,5,267);


--------------------------------------------------------
--kart/charge/calcmonth.aspx расчетный месяц
--------------------------------------------------------
--!268
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 268, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195,201, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,224,256,257, 72,81,82,106,107,257, 77,268,293, 150,151,152,155 ,160,161, 270,271,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 268, nzp_act, 1, 0, 0 from s_actions where nzp_act in (131);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 268, nzp_act, 2, 0, 0 from s_actions where nzp_act in (132);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 268, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (268, 131, 268);
insert into actions_lnk (cur_page, nzp_act, page_url) values (268, 132, 268);


--------------------------------------------------------
--servpriority.aspx приоритеты услуг
--------------------------------------------------------
--!269
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 269,nzp_page,0,nzp_page from pages
where nzp_page in (30,201, 73,232,233,239,256,257, 235,107,203,253,258,259,265, 270,269)
   or nzp_page >= 950                                 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 269,nzp_act,1,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 269,nzp_act,2,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 269,nzp_act,3,0,0 from s_actions where nzp_act in (133);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 269,nzp_act,4,0,0 from s_actions where nzp_act in (134);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 269,nzp_act,5,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 269,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (269, 4, 269);
insert into actions_lnk (cur_page, nzp_act, page_url) values (269, 61, 269);
insert into actions_lnk (cur_page, nzp_act, page_url) values (269, 26, 269);
insert into actions_lnk (cur_page, nzp_act, page_url) values (269, 133, 269);
insert into actions_lnk (cur_page, nzp_act, page_url) values (269, 134, 269);


--------------------------------------------------------
--admin/settings/setups.aspx
--------------------------------------------------------
--!271
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 271,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155 ,160,161, 73,44,256, 77,268,293, 270,271,342) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 271,nzp_act,1,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 271,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (271, 5, 271);
insert into actions_lnk (cur_page, nzp_act, page_url) values (271, 61, 271);


--------------------------------------------------------
--subsidy/request/requests.aspx
--------------------------------------------------------
--!272
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 272,nzp_page,0,nzp_page from pages
where nzp_page in (275,273, 72,278, 73,44,232,233,256,257,283,284, 291,272,274,279) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 272,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 272,nzp_act,2,0,0 from s_actions where nzp_act in (142);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 272,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page, nzp_act, page_url) values (272, 5, 272);
insert into actions_lnk (cur_page, nzp_act, page_url) values (272, 142, 273);


--------------------------------------------------------
--subsidy/request/request.aspx
--------------------------------------------------------
--!273
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 273,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 275,273, 72,278, 73,44,256,283) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 273,nzp_act,1,0,0 from s_actions where nzp_act in (143);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 273,nzp_act,3,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 273,nzp_act,4,0,0 from s_actions where nzp_act in (140);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 273,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (273, 143, 273);
insert into actions_lnk (cur_page, nzp_act, page_url) values (273, 61, 273);



--------------------------------------------------------
--subsidy/payment/payments.aspx
--------------------------------------------------------
--!274
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 274,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 275,277, 72,278, 73,44,256,283) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 274,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 274,nzp_act,2,0,0 from s_actions where nzp_act in (141);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 274,nzp_act,3,0,0 from s_actions where nzp_act in (147);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 274,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (274, 5, 274);
insert into actions_lnk (cur_page, nzp_act, page_url) values (274, 141, 277);
insert into actions_lnk (cur_page, nzp_act, page_url) values (274, 141, 274);


--------------------------------------------------------
--subsidy/payment/payment.aspx
--------------------------------------------------------
--!277
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 277,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 275,277, 72,278, 73,44,256,283) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 277,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 277,nzp_act,2,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 277,nzp_act,3,0,0 from s_actions where nzp_act in (147);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 277,nzp_act,4,0,0 from s_actions where nzp_act in (148);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 277,nzp_act,5,0,0 from s_actions where nzp_act in (149);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 277,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (277, 5, 277);
insert into actions_lnk (cur_page, nzp_act, page_url) values (277, 61, 277);
insert into actions_lnk (cur_page, nzp_act, page_url) values (277, 147, 277);
insert into actions_lnk (cur_page, nzp_act, page_url) values (277, 148, 277);
insert into actions_lnk (cur_page, nzp_act, page_url) values (277, 149, 277);


--------------------------------------------------------
--subsidy/charge/subsidysaldo.aspx
--------------------------------------------------------
--!278
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 278,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 72,278, 73,44,256,283) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 278,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 278,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page, nzp_act, page_url) values (278, 5, 278);

--------------------------------------------------------
--subsidy/agreement/agreements.aspx
--------------------------------------------------------
--!279
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 279,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 72,278, 73,44,256,273,283,284, 276,277,280,285,287,288) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 279,nzp_act,1,0,0 from s_actions where nzp_act in (145);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 279,nzp_act,2,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 279,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page, nzp_act, page_url) values (279, 5, 279);
insert into actions_lnk (cur_page, nzp_act, page_url) values (279, 145, 280);

--------------------------------------------------------
--subsidy/agreement/agreement.aspx
--------------------------------------------------------
--!280
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 280,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 72,278, 73,44,256,273, 276,280,285, 277,283,287,288) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 280,nzp_act,1,0,0 from s_actions where nzp_act in (143);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 280,nzp_act,3,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 280,nzp_act,4,0,0 from s_actions where nzp_act in (146);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 280,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (280, 143, 280);
insert into actions_lnk (cur_page, nzp_act, page_url) values (280, 61, 280);
insert into actions_lnk (cur_page, nzp_act, page_url) values (280, 146, 280);


--------------------------------------------------------
--subsidy/charge/lscharges.aspx
--------------------------------------------------------
--!281
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 281,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,43,44,45,46,50,53,56, 70,51,54,55,62,66,95,97,98,111,121,122,123,131,133,162,165,166,177,240,244,262,281, 71,57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192, 72,81,82,106,107,340) 

   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 281,nzp_act,1,0,0 from s_actions where nzp_act in (5);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 281,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (520);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 281,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (521);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 281,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (522);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 281,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (523);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 281,nzp_act,nzp_act,1,1 from s_actions where nzp_act in (528);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 281,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page,nzp_act,page_url) values (281,61,281);

--------------------------------------------------------
--messagelist.aspx рассылка сообщений
-------------------------------------------------------
--!282
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 282,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,252,289, 77,207,220,247, 80,208,209, 254,255,267, 282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 282,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,150,151,152);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 282,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (282,5,282);
insert into actions_lnk (cur_page,nzp_act,page_url) values (282,151,282);
insert into actions_lnk (cur_page,nzp_act,page_url) values (282,152,282);

insert into actions_lnk (cur_page,nzp_act,page_url) values (282,150,286);

--------------------------------------------------------
--kart/adres/vills.aspx список мун. образований
-------------------------------------------------------
--!283
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 283,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 72,278, 73,44,256,283) 
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 283,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 283,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (283,5,283);
insert into actions_lnk (cur_page,nzp_act,page_url) values (283,61,283);

--------------------------------------------------------
--percpt.aspx --Справочник уровня платежей граждан
--------------------------------------------------------
--!284
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 284,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 72,278, 73,44,256,283,284) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 284,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 284,nzp_act,2,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 284,nzp_act,3,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 284,nzp_act,4,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 284,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (284, 5, 284);

--------------------------------------------------------
--cashplan.aspx -- страница кассового плана
--------------------------------------------------------
--!285
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 285,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 72,278, 73,44,256,273, 276,280,285,287,288, 277,283,284) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 285,nzp_act,1,0,0 from s_actions where nzp_act in (153);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 285,nzp_act,2,0,0 from s_actions where nzp_act in (161);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 285,nzp_act,2,0,0 from s_actions where nzp_act in (162);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 285,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (285, 153, 285);

--------------------------------------------------------
--newmessage.aspx новое сообщение
-------------------------------------------------------
--!286
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 286,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,252,289, 77,207,220,247, 80,208,209, 254,255,267, 282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 286,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (151);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 286,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (286,151,286);


--------------------------------------------------------
--subsidy/agreement/actsofsupply.aspx
--------------------------------------------------------
--!287
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 287,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 72,278, 73,44,256,273,283,284, 276,277,280,285,287,288) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 287,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 287,nzp_act,2,0,0 from s_actions where nzp_act in (154);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 287,nzp_act,3,0,0 from s_actions where nzp_act in (155);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 287,nzp_act,4,0,0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 287,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page, nzp_act, page_url) values (287, 5, 287);
insert into actions_lnk (cur_page, nzp_act, page_url) values (287, 154, 287);
insert into actions_lnk (cur_page, nzp_act, page_url) values (287, 155, 287);
insert into actions_lnk (cur_page, nzp_act, page_url) values (287, 158, 287);


--------------------------------------------------------
--subsidy/agreement/housingstockdescrs.aspx
--------------------------------------------------------
--!288
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 288,nzp_page,0,nzp_page from pages
where nzp_page in (291,272,274,279, 72,278, 73,44,256,273,283,284, 276,277,280,285,287,288) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 288,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 288,nzp_act,2,0,0 from s_actions where nzp_act in (156);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 288,nzp_act,3,0,0 from s_actions where nzp_act in (157);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 288,nzp_act,4,0,0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 288,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610);

insert into actions_lnk (cur_page, nzp_act, page_url) values (288, 5, 288);
insert into actions_lnk (cur_page, nzp_act, page_url) values (288, 156, 288);
insert into actions_lnk (cur_page, nzp_act, page_url) values (288, 157, 288);
insert into actions_lnk (cur_page, nzp_act, page_url) values (288, 158, 288);

--------------------------------------------------------
--phonesprav.aspx новое сообщение
-------------------------------------------------------
--!289
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 289,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,252,289, 77,207,220,247, 80,208,209, 254,255,267, 282)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 289,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 289,nzp_act,2,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 289,nzp_act,3,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 289,nzp_act,4,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 289,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (289,  5, 289);
insert into actions_lnk (cur_page, nzp_act, page_url) values (289,  4, 289);
insert into actions_lnk (cur_page, nzp_act, page_url) values (289, 61, 289);
insert into actions_lnk (cur_page, nzp_act, page_url) values (289, 26, 289);

--------------------------------------------------------
--olap.aspx OLAP
-------------------------------------------------------
--!292
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 289,nzp_act,1,2,5 from s_actions where nzp_act in (297);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 289,nzp_act,2,2,5 from s_actions where nzp_act in (298);

--------------------------------------------------------
--admin/files/fload.aspx загрузка данных
--------------------------------------------------------
--!293
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 293, nzp_act, 1, 0, 0 from s_actions where nzp_act in (163);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 293, nzp_act, 2, 0, 0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 293, nzp_act, 3, 0, 0 from s_actions where nzp_act in (166);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 293, nzp_act, 4, 0, 0 from s_actions where nzp_act in (329);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 293, nzp_act, 5, 2, 5 from s_actions where nzp_act in (870);

insert into actions_lnk (cur_page, nzp_act, page_url) values (293, 158, 293);
insert into actions_lnk (cur_page, nzp_act, page_url) values (293, 163, 293);
insert into actions_lnk (cur_page, nzp_act, page_url) values (293, 329, 315);

--------------------------------------------------------
--counttypes.aspx типы приборов учета с переходом на дом
--------------------------------------------------------
--!294
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 294,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 294,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (294, 4, 294);
insert into actions_lnk (cur_page, nzp_act, page_url) values (294, 5, 294);
insert into actions_lnk (cur_page, nzp_act, page_url) values (294, 61, 294);


--------------------------------------------------------
--mustcalc/mcgroup.aspx групповая операция установки перерасчетов по списку ЛС
--------------------------------------------------------
--!295
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 295,nzp_act,1,0,0 from s_actions where nzp_act = 169;

insert into actions_lnk (cur_page, nzp_act, page_url) values (295, 169, 295);


--------------------------------------------------------
--mustcalc/mcls.aspx перерасчеты по ЛС
--------------------------------------------------------
--!296
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 296,nzp_act,1,0,0 from s_actions where nzp_act = 169;
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 296,nzp_act,2,0,0 from s_actions where nzp_act = 170;
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 296,nzp_act,3,0,0 from s_actions where nzp_act = 158;

insert into actions_lnk (cur_page, nzp_act, page_url) values (296, 169, 296);
insert into actions_lnk (cur_page, nzp_act, page_url) values (296, 170, 296);
insert into actions_lnk (cur_page, nzp_act, page_url) values (296, 158, 296);


--------------------------------------------------------
--ercadress.aspx обновление адресов и сальдо по лиц.счетам
--------------------------------------------------------
--!297
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 297, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195,245, 40,41,42,50,53,56,197,246, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,221,224,252,289, 77,207,220,247, 80,208,209, 254,255,267,282, 72,81,82,106,107,256,257,261,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 297, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (173); 
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 297, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (297, 173, 297);

--------------------------------------------------------
--zthemes.aspx справочник классификация сообщений
--------------------------------------------------------
--!298
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 298,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,39,245, 40,41,197,246, 73,248,252,289,  77,220, 80,208,209,267)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 298,nzp_act,1,0,0 from s_actions where nzp_act in (4,5,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 298,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (298, 4, 298);
insert into actions_lnk (cur_page, nzp_act, page_url) values (298, 5, 298);
insert into actions_lnk (cur_page, nzp_act, page_url) values (298, 61, 298);
insert into actions_lnk (cur_page, nzp_act, page_url) values (298, 135, 298);


--------------------------------------------------------
--groupperekidki.aspx изменение сальдо по списку ЛС
--------------------------------------------------------
--!299
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 299,nzp_act,1,0,0 from s_actions where nzp_act in (174);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 299,nzp_act,2,0,0 from s_actions where nzp_act in (180);
insert into actions_lnk (cur_page,nzp_act,page_url) values (299,174,299);
insert into actions_lnk (cur_page,nzp_act,page_url) values (299,180,300);


--------------------------------------------------------
--reestrperekidok.aspx реестр изменений сальдо по списку ЛС
--------------------------------------------------------
--!300
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 300,nzp_act,1,0,0 from s_actions where nzp_act in (80);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 300,nzp_act,2,0,0 from s_actions where nzp_act in (158);
insert into actions_lnk (cur_page,nzp_act,page_url) values (300,80,41);
insert into actions_lnk (cur_page,nzp_act,page_url) values (300,158,300);


--------------------------------------------------------
--service.aspx услуга
--------------------------------------------------------
--!301

--в разработке
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 301,nzp_act,1,0,0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 301,nzp_act,2,0,0 from s_actions where nzp_act in (176);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 301,nzp_act,3,0,0 from s_actions where nzp_act in (170);

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 301,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_lnk (cur_page,nzp_act,page_url) values (301,169,301);
insert into actions_lnk (cur_page,nzp_act,page_url) values (301,176,301);
insert into actions_lnk (cur_page,nzp_act,page_url) values (301,170,301);


--------------------------------------------------------
--perekidkalstols.aspx перенос суммы с ЛС на ЛС
--------------------------------------------------------
--!302
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 302,nzp_act,1,0,0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 302,nzp_act,2,0,0 from s_actions where nzp_act in (179);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 302,nzp_act,3,0,0 from s_actions where nzp_act in (180);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 302,nzp_act,4,0,0 from s_actions where nzp_act in (70);

insert into actions_lnk (cur_page,nzp_act,page_url) values (302,70,302);
insert into actions_lnk (cur_page,nzp_act,page_url) values (302,170,302);
insert into actions_lnk (cur_page,nzp_act,page_url) values (302,179,302);
insert into actions_lnk (cur_page,nzp_act,page_url) values (302,180,303);


--------------------------------------------------------
--reestrperekidok.aspx реестр изменений сальдо по выбранному ЛС
--------------------------------------------------------
--!303
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 303,nzp_act,1,0,0 from s_actions where nzp_act in (80);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 303,nzp_act,2,0,0 from s_actions where nzp_act in (158);
insert into actions_lnk (cur_page,nzp_act,page_url) values (303,80,41);
insert into actions_lnk (cur_page,nzp_act,page_url) values (303,158,303);


--------------------------------------------------------
--moveoverpayments.aspx перенос переплат
--------------------------------------------------------
--!304
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 304,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 304,nzp_act,2,0,0 from s_actions where nzp_act in (182);
insert into actions_lnk (cur_page,nzp_act,page_url) values (304,1,304);
insert into actions_lnk (cur_page,nzp_act,page_url) values (304,182,304);

--------------------------------------------------------
--/settings/address/defaultaddress.aspx Адрес по умолчанию
--------------------------------------------------------
--!306
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 306,nzp_act,1,0,0 from s_actions where nzp_act in (170);
insert into actions_lnk (cur_page,nzp_act,page_url) values (306,170,306);

--------------------------------------------------------
--kladr.aspx КЛАДР
--------------------------------------------------------
--!307
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 307,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (187);
insert into actions_lnk (cur_page,nzp_act,page_url) values (307,187,307);

--------------------------------------------------------
--workwithbank.aspx
--------------------------------------------------------
--!308
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 308,nzp_act,3,0,0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 308,nzp_act,1,0,0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 308,nzp_act,2,0,0 from s_actions where nzp_act in (170);

--------------------------------------------------------
--changearea.aspx смена УК
--------------------------------------------------------
--!309  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 309,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (184);
insert into actions_lnk (cur_page,nzp_act,page_url) values (309,184,309);


--------------------------------------------------------
--payment_bank.aspx клиент-банк
--------------------------------------------------------
--!310
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 310,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (185);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 310,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (186);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 310,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (189);

insert into actions_lnk (cur_page,nzp_act,page_url) values (310,61,310);
insert into actions_lnk (cur_page,nzp_act,page_url) values (310,185,310);
insert into actions_lnk (cur_page,nzp_act,page_url) values (310,186,310);
insert into actions_lnk (cur_page,nzp_act,page_url) values (310,189,314);

--------------------------------------------------------
--finances/pack/addpack.aspx добавление пачки
--------------------------------------------------------
--!311
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 311,nzp_act,1,0,0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 311,nzp_act,2,0,0 from s_actions where nzp_act in (86);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 311,nzp_act,3,0,0 from s_actions where nzp_act in (92);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 311,nzp_act,4,0,0 from s_actions where nzp_act in (87);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 311,nzp_act,5,0,0 from s_actions where nzp_act in (90);
insert into actions_lnk (cur_page,nzp_act,page_url) values (311,86,311);
insert into actions_lnk (cur_page,nzp_act,page_url) values (311,87,311);
insert into actions_lnk (cur_page,nzp_act,page_url) values (311,90,311);
insert into actions_lnk (cur_page,nzp_act,page_url) values (311,92,311);
insert into actions_lnk (cur_page,nzp_act,page_url) values (311,170,311);


--------------------------------------------------------
--finances/pack/addpackls.aspx добавление оплаты
--------------------------------------------------------
--!312
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 312,nzp_act,1,0,0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 312,nzp_act,2,0,0 from s_actions where nzp_act in (87);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 312,nzp_act,3,0,0 from s_actions where nzp_act in (90);
insert into actions_lnk (cur_page,nzp_act,page_url) values (312,170,312);
insert into actions_lnk (cur_page,nzp_act,page_url) values (312,87,312);
insert into actions_lnk (cur_page,nzp_act,page_url) values (312,90,312);


--------------------------------------------------------
--kart/lgota/lgotcategories.aspx категории льгот
--------------------------------------------------------
--!313
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 313,nzp_act,1,0,0 from s_actions where nzp_act in (153);
insert into actions_lnk (cur_page,nzp_act,page_url) values (313,153,313);

--------------------------------------------------------
--files_in_bank_cliend_load.aspx файлы с выгрузками клиент-банк
--------------------------------------------------------
--!314
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 314,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 314,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (186);
insert into actions_lnk (cur_page,nzp_act,page_url) values (314,186,314);

--------------------------------------------------------
--admin/files/setchanges.aspx загрузка данных
--------------------------------------------------------
--!315
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 315, nzp_act, 1, 0, 0 from s_actions where nzp_act in (329);

--------------------------------------------------------
--perekidkals.aspx ввод изменения сальдо по лицевому счету по услугам
--------------------------------------------------------
--!316
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 316,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (170);
insert into actions_lnk (cur_page,nzp_act,page_url) values (316,170,165);

--------------------------------------------------------
--admin/files/exchangesz.aspx взаим с СЗ Тула
--------------------------------------------------------
--!317
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 317,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (188,158);
insert into actions_lnk (cur_page,nzp_act,page_url) values (317,188,317);
insert into actions_lnk (cur_page,nzp_act,page_url) values (317,158,317);

--------------------------------------------------------
--admin/files/nebo_reestr.aspx веб-сервис для НЕБО
--------------------------------------------------------
--!318
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 318,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (191,195);
insert into actions_lnk (cur_page,nzp_act,page_url) values (318,191,318);
insert into actions_lnk (cur_page,nzp_act,page_url) values (318,195,318);

--------------------------------------------------------
--finances/sprav/bcformats.aspx форматы банк-клиент
--------------------------------------------------------
--!319
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 319,nzp_act,1,0,0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 319,nzp_act,2,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 319,nzp_act,3,0,0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 319,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_lnk (cur_page,nzp_act,page_url) values (319,61,319);
insert into actions_lnk (cur_page,nzp_act,page_url) values (319,158,319);
insert into actions_lnk (cur_page,nzp_act,page_url) values (319,169,319);


--------------------------------------------------------
--admin/settings/codes.aspx настройки уникальных кодов
--------------------------------------------------------
--!320
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 320,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 320,nzp_act,2,0,0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 320,nzp_act,3,0,0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 320,nzp_act,4,0,0 from s_actions where nzp_act in (196);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 320,nzp_act,5,0,0 from s_actions where nzp_act in (170);
insert into actions_lnk (cur_page,nzp_act,page_url) values (320,1,320);
insert into actions_lnk (cur_page,nzp_act,page_url) values (320,158,320);
insert into actions_lnk (cur_page,nzp_act,page_url) values (320,169,320);
insert into actions_lnk (cur_page,nzp_act,page_url) values (320,170,320);
insert into actions_lnk (cur_page,nzp_act,page_url) values (320,196,320);


--------------------------------------------------------
--exchange/service/suppexchange.aspx Взаимодействие со сторонними биллинговыми системами
--------------------------------------------------------
--!321
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 321,nzp_act,1,0,0 from s_actions where nzp_act in (188);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 321,nzp_act,2,0,0 from s_actions where nzp_act in (158);
insert into actions_lnk (cur_page,nzp_act,page_url) values (321,188,321);
insert into actions_lnk (cur_page,nzp_act,page_url) values (321,158,321);


--------------------------------------------------------
--finddebt.aspx шаблон поиска по долгам
--------------------------------------------------------
--!322
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 322, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 322, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,514);

insert into actions_lnk (cur_page,nzp_act,page_url) values (322,1,324);
insert into actions_lnk (cur_page,nzp_act,page_url) values (322,2,322);


--------------------------------------------------------
--findcase.aspx шаблон поиска по делам
--------------------------------------------------------
--!323
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 323, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (1, 2);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 323, nzp_act, nzp_act, 1, 1 from s_actions where nzp_act in (501,513);

insert into actions_lnk (cur_page,nzp_act,page_url) values (323,1,325);
insert into actions_lnk (cur_page,nzp_act,page_url) values (323,2,323);


--------------------------------------------------------
--debitors.aspx список должников
--------------------------------------------------------
--!324
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 324, nzp_act, 1, 0, 0 from s_actions where nzp_act in (198);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 324, nzp_act, 2, 0, 0 from s_actions where nzp_act in (200);

insert into actions_lnk (cur_page,nzp_act,page_url) values (324,198,326);
insert into actions_lnk (cur_page,nzp_act,page_url) values (324,200,326);


--------------------------------------------------------
--debitor/list/deals.aspx выбранный список дел
--------------------------------------------------------
--!325
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 325, nzp_act, nzp_act, 0, 0 from s_actions where nzp_act in (200);
insert into actions_lnk (cur_page,nzp_act,page_url) values (325,200,326);


--------------------------------------------------------
--debitor/deal/deal.aspx дело
--------------------------------------------------------
--!326
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 326, nzp_act, 1, 0, 0 from s_actions where nzp_act in (192);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 326, nzp_act, 2, 0, 0 from s_actions where nzp_act in (193);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 326, nzp_act, 3, 0, 0 from s_actions where nzp_act in (194);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 326, nzp_act, 4, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 326, nzp_act, 5, 0, 0 from s_actions where nzp_act in (199);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 326, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page,nzp_act,page_url) values (326,61,326);
insert into actions_lnk (cur_page,nzp_act,page_url) values (326,192,327);
insert into actions_lnk (cur_page,nzp_act,page_url) values (326,193,328);
insert into actions_lnk (cur_page,nzp_act,page_url) values (326,194,329);
insert into actions_lnk (cur_page,nzp_act,page_url) values (326,199,326);

--------------------------------------------------------
--debitor/deal/agreement.aspx соглашение
--------------------------------------------------------
--!327
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 327, nzp_act, 1, 0, 0 from s_actions where nzp_act in (540);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 327, nzp_act, 2, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 327, nzp_act, 3, 0, 0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 327, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);


--------------------------------------------------------
--debitor/deals/debtClaim.aspx иск
--------------------------------------------------------
--!328
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 328, nzp_act, 1, 0, 0 from s_actions where nzp_act in (540);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 328, nzp_act, 2, 0, 0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 328, nzp_act, 3, 0, 0 from s_actions where nzp_act in (197);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 328, nzp_act, 4, 0, 0 from s_actions where nzp_act in (158);


--------------------------------------------------------
--debitor/deal/debtchange.aspx изменение задолженности
--------------------------------------------------------
--!329
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 329, nzp_act, 1, 0, 0 from s_actions where nzp_act in (540);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 329, nzp_act, 2, 0, 0 from s_actions where nzp_act in (170);


--------------------------------------------------------
--debitor/settings/debtsettings.aspx настройки системы должников
--------------------------------------------------------
--!330
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 330, nzp_act, 1, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 330, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);


--------------------------------------------------------
--dealoperations.aspx груп опер с делами
--------------------------------------------------------
--!332
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 332, nzp_act, 1, 0, 0 from s_actions where nzp_act in (174);


--------------------------------------------------------
--exchange/bank/upload_efs.aspx загрузка ежедневных файлов сверки оплат (Астрахань)
--------------------------------------------------------
--!334
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 334, nzp_act, 1, 0, 0 from s_actions where nzp_act in (163);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 334, nzp_act, 2, 0, 0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 334, nzp_act, 3, 0, 0 from s_actions where nzp_act in (541);


--------------------------------------------------------
--kart/counter/fastpu.aspx быстрый ввод оплат
--------------------------------------------------------
--!335
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 335, nzp_act, 1, 0, 0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 335, nzp_act, nzp_act, 2, 4 from s_actions where nzp_act in (601,602);


--------------------------------------------------------
--exchange/bank/upload_vtb24.aspx загрузка оплат от ВТБ24 (Тула)
--------------------------------------------------------
--!336
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 336, nzp_act, 1, 0, 0 from s_actions where nzp_act in (158);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 336, nzp_act, 2, 0, 0 from s_actions where nzp_act in (85);

--------------------------------------------------------
--kart/serv/servdependencies.aspx зависимости услуг
--------------------------------------------------------
--!339
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 339, nzp_act, 1, 0, 0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 339, nzp_act, 1, 0, 0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 339, nzp_act, 2, 0, 0 from s_actions where nzp_act in (158);


--------------------------------------------------------
--kart/sprav/formuls.aspx формулы расчета
--------------------------------------------------------
--!341
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 341,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 341,nzp_act,1,0,0 from s_actions where nzp_act in (2);
insert into actions_lnk (cur_page,nzp_act,page_url) values (341,1,341);
insert into actions_lnk (cur_page,nzp_act,page_url) values (341,2,341);

--------------------------------------------------------
--admin/process/prepareprintinvoices.aspx Подготовка данных для печати счетов
--------------------------------------------------------
--!342
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 342, nzp_page, 0, nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36,37,38,195,201, 40,41,42,50,53,56, 73,44,45,168,170,210,211,212,213,214,215,216,217,218,219,224,256,257, 72,81,82,106,107,257, 77,268,293, 150,151,152,155 ,160,161, 270,271,340)
   or nzp_page >= 950 
   or nzp_page <  10;
  
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 342, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 342, nzp_act, 2, 0, 0 from s_actions where nzp_act in (545);

insert into actions_lnk (cur_page, nzp_act, page_url) values (342, 5, 342);
insert into actions_lnk (cur_page, nzp_act, page_url) values (342, 545, 342);

--------------------------------------------------------
--admin/processes/jobs.aspx
--------------------------------------------------------
--!343
insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select 343,nzp_page,0,nzp_page from pages
where nzp_page in (150,151,152,155, 160,161, 73,44,256, 77,268, 270,271,293) 
   or nzp_page >= 950 
   or nzp_page < 10; 

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 343,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (546, 547);

insert into actions_lnk (cur_page,nzp_act,page_url) values (343,546,343);
insert into actions_lnk (cur_page,nzp_act,page_url) values (343,547,343);

--------------------------------------------------------
--supplierlslist.aspx список кодов лс поставщика
--------------------------------------------------------
--!344
  
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 344,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (4,26,61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 344,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (344, 4, 344);
insert into actions_lnk (cur_page, nzp_act, page_url) values (344, 26, 344);
insert into actions_lnk (cur_page, nzp_act, page_url) values (344, 61, 344);
--------------------------------------------------------
--ls_events.aspx история событий лс
--------------------------------------------------------
--!345

--------------------------------------------------------
--supplierls.aspx лс поставщика
--------------------------------------------------------
--!346
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 346,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 346,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (346, 61, 346);

--------------------------------------------------------
--dom_events.aspx история событий дома
--------------------------------------------------------
--!347

--Ссылки со внешних страниц (те, которые не редактировались в этом скрипте) на страницы из данного скрипта
insert into pages_show (cur_page,page_url,up_kod,sort_kod) 
select a.nzp_page, b.nzp_page, coalesce(b.up_kod,0), b.nzp_page 
from pages a, pages b
where ((a.nzp_page in (31,33,35,37,41,42) and b.nzp_page in (72,73,74,75,77,79,80,106,107,220,241,284,293,340))
    or (a.nzp_page in (31,33,35,37,41,42,52,82,122,123,124,130) and b.nzp_page in (5,6))
    or (a.nzp_page in (31) and b.nzp_page in (226,252,289))
    or (a.nzp_page in (31,33,35,37,41,42) and b.nzp_page in (32,34,36,38,39,195))
    or (a.nzp_page in (31,33,35,37,41,42,57,122,123,124,126) and b.nzp_page in (50,53,56,197)) 
    or (a.nzp_page in (41,122,123) and b.nzp_page in (51,54,55,62,66,95,97,98,111,121,131,133,162,165,166,177,189,238,244,251,262,281))
    or (a.nzp_page in (41,42,122,123,124,126) and b.nzp_page in (57,59,61,63,67,92,99,137,167,184,185,186,190,191,192))
    or (a.nzp_page in (41,82) and b.nzp_page in (81))
    or (a.nzp_page in (41) and b.nzp_page in (252,289))
    or (a.nzp_page in (82) and b.nzp_page in (106,107,340))
    or (a.nzp_page in (31,41,42) and b.nzp_page in (44,45,49,168,170,210,211,212,213,214,215,216,217,218,219,221,224,256,257,261))
    or (a.nzp_page in (41) and b.nzp_page in (100,104,108,110,196,247,263,264))
    or (a.nzp_page in (42) and b.nzp_page in (102,109,179,180,182))
    or (a.nzp_page in (41) and b.nzp_page in (134))
    or (a.nzp_page in (41) and b.nzp_page in (240))
    or (a.nzp_page in (31,33,35,37,82) and b.nzp_page in (206))
    or (a.nzp_page in (31,41) and b.nzp_page in (73,77,207,208,209,220,243,245,246,247,248, 254,255,267,282))
    or (a.nzp_page in (35,42) and b.nzp_page in (77,243))
    or (a.nzp_page = 31 and b.nzp_page = 31)
    or (a.nzp_page = 33 and b.nzp_page = 33)
    or (a.nzp_page = 35 and b.nzp_page = 35)
    or (a.nzp_page = 37 and b.nzp_page = 37)
    or (a.nzp_page = 41 and b.nzp_page = 41)
    or (a.nzp_page = 42 and b.nzp_page = 42)
    or (a.nzp_page = 52 and b.nzp_page = 52)
    or (a.nzp_page = 57 and b.nzp_page = 57)
    or (a.nzp_page = 82 and b.nzp_page = 82)
    or (a.nzp_page = 122 and b.nzp_page = 122)
    or (a.nzp_page = 123 and b.nzp_page = 123)
    or (a.nzp_page = 124 and b.nzp_page = 124)
    or (a.nzp_page = 126 and b.nzp_page = 126)
    or (a.nzp_page = 130 and b.nzp_page = 130)
) and not exists (select 1 from pages_show c where c.cur_page = a.nzp_page and c.page_url = b.nzp_page);



insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select distinct a.cur_page, b.nzp_page, coalesce(b.up_kod,0), b.nzp_page 
from pages_show a, pages b
where a.page_url in (70,71,72)
and b.up_kod is not null
and a.page_url = b.up_kod
and not exists (select 1 from pages_show c where c.cur_page = a.cur_page and c.page_url = b.nzp_page);

--------------------------------------------------------
--download_logs.aspx скачать логи
--------------------------------------------------------
--!348
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 348,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (555);
insert into actions_lnk (cur_page,nzp_act,page_url) values (348,555,348);

--------------------------------------------------------
--kart/prm/params.aspx справочник параметров
--------------------------------------------------------
--!349
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 349,nzp_act,1,0,0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 349,nzp_act,2,0,0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 349,nzp_act,3,0,0 from s_actions where nzp_act in (158);
insert into actions_lnk (cur_page,nzp_act,page_url) values (349,158,349);
insert into actions_lnk (cur_page,nzp_act,page_url) values (349,169,349);
insert into actions_lnk (cur_page,nzp_act,page_url) values (349,170,349);

--------------------------------------------------------
--percent_dom.aspx процент удержания по домам
--------------------------------------------------------
--!350

insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 350,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 350,nzp_act,2,0,0 from s_actions where nzp_act in (2);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 350,nzp_act,3,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 350,nzp_act,4,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 350,nzp_act,5,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 350,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 350,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page,nzp_act,page_url) values (350,2,350);
insert into actions_lnk (cur_page,nzp_act,page_url) values (350,5,350);
insert into actions_lnk (cur_page,nzp_act,page_url) values (350,4,350);
insert into actions_lnk (cur_page,nzp_act,page_url) values (350,61,350);
insert into actions_lnk (cur_page,nzp_act,page_url) values (350,26,350);

--------------------------------------------------------
--kart/spravm/contracts.aspx справочник договоров
--------------------------------------------------------
--!352
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 352,nzp_act,1,0,0 from s_actions where nzp_act in (103);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 352,nzp_act,2,0,0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 352,nzp_act,3,0,0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 352,nzp_act,4,0,0 from s_actions where nzp_act in (158);
insert into actions_lnk (cur_page,nzp_act,page_url) values (352,103,175);
insert into actions_lnk (cur_page,nzp_act,page_url) values (352,158,352);
insert into actions_lnk (cur_page,nzp_act,page_url) values (352,169,352);
insert into actions_lnk (cur_page,nzp_act,page_url) values (352,170,352);

--------------------------------------------------------
--percent_dom_supp.aspx процент удержания по домам по договорам
--------------------------------------------------------
--!354
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 354,nzp_act,1,0,0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 354,nzp_act,2,0,0 from s_actions where nzp_act in (2);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 354,nzp_act,3,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 354,nzp_act,4,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 354,nzp_act,5,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 354,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 354,nzp_act,nzp_act,2,3 from s_actions where nzp_act in (701,702,703,705);

insert into actions_lnk (cur_page,nzp_act,page_url) values (354,2,354);
insert into actions_lnk (cur_page,nzp_act,page_url) values (354,5,354);
insert into actions_lnk (cur_page,nzp_act,page_url) values (354,4,354);
insert into actions_lnk (cur_page,nzp_act,page_url) values (354,61,354);
insert into actions_lnk (cur_page,nzp_act,page_url) values (354,26,354);

--------------------------------------------------------
--contractors.aspx справочник контрагентов
--------------------------------------------------------
--!355
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 355, nzp_act, 1, 0, 0 from s_actions where nzp_act in (5);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 355, nzp_act, 2, 0, 0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 355, nzp_act, 3, 0, 0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 355, nzp_act, nzp_act, 2, 1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (355, 4, 355);
insert into actions_lnk (cur_page, nzp_act, page_url) values (355, 5, 355);
insert into actions_lnk (cur_page, nzp_act, page_url) values (355, 61, 355);


--------------------------------------------------------
--finances/payerreval_supp.aspx Перекидки между контрагентами по договорам
--------------------------------------------------------
--!356
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 356,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 356,nzp_act,2,0,0 from s_actions where nzp_act in (4);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 356,nzp_act,3,0,0 from s_actions where nzp_act in (61);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 356,nzp_act,4,0,0 from s_actions where nzp_act in (26);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 356,nzp_act,nzp_act,2,1 from s_actions where nzp_act in (610,611);

insert into actions_lnk (cur_page, nzp_act, page_url) values (356, 1, 356);
insert into actions_lnk (cur_page, nzp_act, page_url) values (356, 4, 356);
insert into actions_lnk (cur_page, nzp_act, page_url) values (356, 26, 356);
insert into actions_lnk (cur_page, nzp_act, page_url) values (356, 61, 356);


--------------------------------------------------------
--contract_details.aspx реквизиты договоров
--------------------------------------------------------
--!358
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 358,nzp_act,1,0,0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 358,nzp_act,2,0,0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 358,nzp_act,3,0,0 from s_actions where nzp_act in (158);

insert into actions_lnk (cur_page,nzp_act,page_url) values (358,158,358);
insert into actions_lnk (cur_page,nzp_act,page_url) values (358,169,358);
insert into actions_lnk (cur_page,nzp_act,page_url) values (358,170,358);


--------------------------------------------------------
--payertransfer_supp.aspx перечисления контрагентам по договорам
--------------------------------------------------------
--!359
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 359,nzp_act,nzp_act,0,0 from s_actions where nzp_act in (170);
insert into actions_lnk (cur_page,nzp_act,page_url) values (359,170,359);


--------------------------------------------------------
--kart/adres/areas.aspx управляющие организации
--------------------------------------------------------
--!360
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 360, nzp_act, 1, 0, 0 from s_actions where nzp_act in (103);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 360, nzp_act, 2, 0, 0 from s_actions where nzp_act in (104);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 360, nzp_act, 3, 0, 0 from s_actions where nzp_act in (170);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 360, nzp_act, 4, 0, 0 from s_actions where nzp_act in (169);
insert into actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) select 360, nzp_act, 5, 0, 0 from s_actions where nzp_act in (158);

insert into actions_lnk (cur_page, nzp_act, page_url) values (360, 103, 227);
insert into actions_lnk (cur_page, nzp_act, page_url) values (360, 104, 231);
insert into actions_lnk (cur_page, nzp_act, page_url) values (360, 158, 360);
insert into actions_lnk (cur_page, nzp_act, page_url) values (360, 169, 360);
insert into actions_lnk (cur_page, nzp_act, page_url) values (360, 170, 360);


--------------------------------------------------------
--statcharge_supp.aspx статистика по начислениям по договорам
--------------------------------------------------------
--!361
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 361,nzp_act,1,0,0 from s_actions where nzp_act in (1);
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 361,nzp_act,2,0,0 from s_actions where nzp_act in (164);

insert into actions_lnk (cur_page,nzp_act,page_url) values (361,1,361);
insert into actions_lnk (cur_page,nzp_act,page_url) values (361,164,361);


--------------------------------------------------------
--statcharge_supp.aspx статистика по начислениям по договорам по дому
--------------------------------------------------------
--!362
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 362,nzp_act,1,0,0 from s_actions where nzp_act in (1);

insert into actions_lnk (cur_page,nzp_act,page_url) values (362,1,362);


--------------------------------------------------------
--change_ls_address.aspx смена адреса выбранных лицевых счетов
--------------------------------------------------------
--!363
insert into actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd) select 363,nzp_act,1,0,0 from s_actions where nzp_act in (170);
insert into actions_lnk (cur_page,nzp_act,page_url) values (363,170,363);


--Построение иерархии пунктов меню
update pages_show set up_kod = 0 where page_url in (1,3,4,5,6,7,8,9) or page_url >= 950;
update pages_show set up_kod = 30 where page_url in (31,32,33,34,35,36,37,38,39,195,201,226,245);
update pages_show set up_kod = 40 where page_url in (41,42,48,50,53,56,197,208,209,246,267);
update pages_show set up_kod = 70 where page_url in (51,54,55,62,66,91,95,97,98,111,121,122,123,131,133,162,165,166,177,189,238,240,244,251,262,281);
update pages_show set up_kod = 71 where page_url in (57,59,61,63,67,92,99,124,126,137,167,184,185,186,190,191,192);
update pages_show set up_kod = 72 where page_url in (81,82,106,107,278,340);
update pages_show set up_kod = 235 where page_url in (107,340) and cur_page in (select cur_page from pages_show where page_url = 235);
update pages_show set up_kod = 73 where page_url in (44,45,49,94,168,170,210,211,212,213,214,215,216,217,218,219,221,224,239,248,252,256,261,283,284,289,294);
update pages_show set up_kod = 74 where page_url in (100,102,104,108,109,110,179,180,182,196,241,263,264);
update pages_show set up_kod = 75 where page_url in (134,135,136,138,139,206);
update pages_show set up_kod = 76 where page_url in (198);
update pages_show set up_kod = 77 where page_url in (207,220,243,247,268,282,342);
update pages_show set up_kod = 78 where page_url in (193,205);
update pages_show set up_kod = 150 where page_url in (151,152,153,154,155);
update pages_show set up_kod = 160 where page_url in (161);
--update pages_show set up_kod = 79 where page_url in (207);
update pages_show set up_kod = 235 where page_url in (202,203,253,258,259,265);
update pages_show set up_kod = 236 where page_url in (204);
update pages_show set up_kod = 254 where page_url in (255);
update pages_show set up_kod = 270 where page_url in (269,271,293);
update pages_show set up_kod = 275 where page_url in (273,277);
update pages_show set up_kod = 276 where page_url in (280,285,287,288);
update pages_show set up_kod = 290 where page_url in (232,233,257);
update pages_show set up_kod = 291 where page_url in (272,274,279);


delete from page_links where 1=1;
insert into page_links (page_to, group_to) values (2, 2);
insert into page_links (page_to, group_to) values (30, 30);
insert into page_links (page_to, group_to) values (40, 40);
insert into page_links (page_to, group_to) values (72, 72);
insert into page_links (page_to, group_to) values (73, 73);
insert into page_links (page_to, group_to) values (74, 74);
insert into page_links (page_to, group_to) values (75, 75);
insert into page_links (page_to, group_to) values (77, 77);
insert into page_links (page_to, group_to) values (150, 150);
insert into page_links (page_to, group_to) values (160, 160);
insert into page_links (page_to, group_to) values (270, 270);
insert into page_links (page_to, group_to) values (291, 291);
insert into page_links (page_from, group_from, page_to, group_to) values (41, 41, null, 41);
insert into page_links (page_from, group_from, page_to, group_to) values (41, null, 70, 70);
insert into page_links (page_from, group_from, page_to, group_to) values (41, null, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (50, null, 104, null);
insert into page_links (page_from, group_from, page_to, group_to) values (50, null, 108, null);
insert into page_links (page_from, group_from, page_to, group_to) values (50, null, 70, 70);
insert into page_links (page_from, group_from, page_to, group_to) values (50, null, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (42, 42, null, 42);
insert into page_links (page_from, group_from, page_to, group_to) values (42, null, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (56, 56, null, 56);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 70, 70, 70);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 70, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 71, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (68, 68, null, 68);
insert into page_links (page_from, group_from, page_to, group_to) values (68, 68, 70, 70);
insert into page_links (page_from, group_from, page_to, group_to) values (68, 68, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (69, 69, null, 69);
insert into page_links (page_from, group_from, page_to, group_to) values (81, 81, 81, 81);
insert into page_links (page_from, group_from, page_to, group_to) values (82, 82, 82, 82);
insert into page_links (page_from, group_from, page_to, group_to) values (93, null, null, 69);
insert into page_links (page_from, group_from, page_to, group_to) values (175, null, 175, null);
insert into page_links (page_from, group_from, page_to, group_to) values (187, null, null, 69);
insert into page_links (page_from, group_from, page_to, group_to) values (69, 69, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (78, 78, null, 78);
insert into page_links (page_from, group_from, page_to, group_to) values (91, 91, null, 91);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 153, null, 153);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 154, null, 154);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 164, null, 164);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 164, null, 164);
insert into page_links (page_from, group_from, page_to, group_to) values (201, 235, null, 235);
insert into page_links (page_from, group_from, page_to, group_to) values (353, null, null, 235);
insert into page_links (page_from, group_from, page_to, group_to) values (273, 273, null, 273);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 276, null, 276);
insert into page_links (page_from, group_from, page_to, group_to) values (null, 277, null, 277);
insert into page_links (page_from, group_from, page_to, group_to) values (256, 290, null, 290);
insert into page_links (page_from, group_from, page_to, group_to) values (355, null, null, 290);
insert into page_links (page_from, group_from, page_to, group_to) values (279, 276, null, 276);
insert into page_links (page_from, group_from, page_to, group_to) values (221, null, null, 301);
insert into page_links (page_from, group_from, page_to, group_to) values (302, null, 70, 70);
insert into page_links (page_from, group_from, page_to, group_to) values (302, null, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (303, null, 70, 70);
insert into page_links (page_from, group_from, page_to, group_to) values (303, null, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (316, null, 70, 70);
insert into page_links (page_from, group_from, page_to, group_to) values (316, null, 71, 71);
insert into page_links (page_from, group_from, page_to, group_to) values (324, 324, 74, 324);
insert into page_links (page_from, group_from, page_to, group_to) values (325, 325, 74, 325);
insert into page_links (page_from, group_from, page_to, group_to) values (352, 352, 352, 352);
insert into page_links (page_from, group_from, page_to, group_to) values (353, null, 356, null);


insert into pages_show (cur_page,page_url,up_kod,sort_kod)
select distinct a.nzp_page, b.nzp_page, coalesce(b.up_kod,0), b.nzp_page
from pages a, pages b, page_links  pl
where (pl.page_from = a.nzp_page or pl.group_from = a.group_id or (pl.page_from is null and pl.group_from is null))
and (pl.page_to = b.nzp_page or pl.group_to = b.group_id)
and (select count(*) from pages_show ps where ps.cur_page = a.nzp_page and ps.page_url = b.nzp_page) = 0;

--добавить недостающие пункты меню, у которых есть подменю
create temp table ps (cur_page integer, up_kod integer);
insert into ps select distinct cur_page, up_kod from pages_show a where up_kod > 0 and not exists (select 1 from pages_show where cur_page = a.cur_page and page_url = a.up_kod);
insert into pages_show (cur_page, page_url, up_kod, sort_kod) select cur_page, up_kod, 0, 0 from ps;
drop table ps;

--удалить пункты меню, у которых нет подменю
delete from pages_show where page_url in (2,70,71,72,73,74,75,76,77,78,79,80,235,236,237,270,275,276) 
    and cur_page||'-'||page_url not in (select cur_page||'-'||up_kod from pages_show where up_kod in (2,70,71,72,73,74,75,76,77,78,79,80,235,236,237,270,275,276));
delete from pages_show where cur_page not in (select nzp_page from pages) or page_url not in (select nzp_page from pages);


--упорядочение пунктов меню на страницах
update pages_show set sort_kod = 3, up_kod = 0 where page_url = 70;
update pages_show set sort_kod = 4, up_kod = 0 where page_url = 254;
update pages_show set sort_kod = 5, up_kod = 0 where page_url = 71;
update pages_show set sort_kod = 6, up_kod = 0 where page_url = 235;
update pages_show set sort_kod = 7, up_kod = 0 where page_url = 236;
update pages_show set sort_kod = 8, up_kod = 0 where page_url = 275;
update pages_show set sort_kod = 9, up_kod = 0 where page_url = 276;
update pages_show set sort_kod = 10, up_kod = 0 where page_url = 78;
update pages_show set sort_kod = 11, up_kod = 0 where page_url = 79;
update pages_show set sort_kod = 12, up_kod = 0 where page_url = 80;
update pages_show set sort_kod = 13, up_kod = 0 where page_url = 76;
update pages_show set sort_kod = 14, up_kod = 0 where page_url = 74;
update pages_show set sort_kod = 15, up_kod = 0 where page_url = 77;
update pages_show set sort_kod = 16, up_kod = 0 where page_url = 75;
update pages_show set sort_kod = 17, up_kod = 0 where page_url = 40;
update pages_show set sort_kod = 18, up_kod = 0 where page_url = 30;
update pages_show set sort_kod = 19, up_kod = 0 where page_url = 291;
update pages_show set sort_kod = 20, up_kod = 0 where page_url = 150;
update pages_show set sort_kod = 21, up_kod = 0 where page_url = 160;
update pages_show set sort_kod = 22, up_kod = 0 where page_url = 357;
update pages_show set sort_kod = 23, up_kod = 0 where page_url = 73;
update pages_show set sort_kod = 24, up_kod = 0 where page_url = 72;

update pages_show set sort_kod = 1 where page_url = 41;         -- список выбранных лицевых счетов
update pages_show set sort_kod = 2 where page_url = 50;         -- список лицевых счетов дома
update pages_show set sort_kod = 3 where page_url = 42;         -- список выбранных домов
update pages_show set sort_kod = 4 where page_url = 53;         -- список выбранных приборов учета
update pages_show set sort_kod = 5 where page_url = 56;         -- список выбранных жильцов
update pages_show set sort_kod = 6 where page_url = 197;        -- список заявок

update pages_show set sort_kod = 1 where page_url = 121;        -- сальдо ЛС
update pages_show set sort_kod = 2 where page_url = 98;         -- реквизиты ЛС
update pages_show set sort_kod = 3 where page_url = 95;         -- перечень услуг
update pages_show set sort_kod = 4 where page_url = 51;         -- характеристики жилья
update pages_show set sort_kod = 5 where page_url = 54;         -- показания КПУ
update pages_show set sort_kod = 6 where page_url = 66;         -- показания ГПУ
update pages_show set sort_kod = 7 where page_url = 189;        -- заявки по ЛС
update pages_show set sort_kod = 8 where page_url = 238;        -- наряд-заказы по ЛС
update pages_show set sort_kod = 9 where page_url = 62;         -- квартирные ПУ
update pages_show set sort_kod = 10 where page_url = 55;        -- недопоставки
update pages_show set sort_kod = 11 where page_url = 165;       -- изменения сальдо
update pages_show set sort_kod = 12 where page_url = 162;       -- поквартирная карточка
update pages_show set sort_kod = 13 where page_url = 122;       -- список начислений
update pages_show set sort_kod = 14 where page_url = 281;       -- список рассчитанных дотаций
update pages_show set sort_kod = 15 where page_url = 123;       -- список оплат
update pages_show set sort_kod = 16 where page_url = 166;       -- расходы на квартиру
update pages_show set sort_kod = 17 where page_url = 97;        -- группы ЛС
update pages_show set sort_kod = 18 where page_url = 111;       -- состояние ЛС
update pages_show set sort_kod = 19 where page_url = 131;       -- счет-фактуры
update pages_show set sort_kod = 20 where page_url = 177;       -- собственники квартиры
update pages_show set sort_kod = 21 where page_url = 240;       -- договоры между ЛС и УК или поставщиком услуг
update pages_show set sort_kod = 22 where page_url = 133;       -- отчеты для лицевого счета
update pages_show set sort_kod = 23 where page_url = 244;       -- рассрочка
update pages_show set sort_kod = 24 where page_url = 262;       -- корректировка входящего сальдо
update pages_show set sort_kod = 25 where page_url = 296;       -- корректировка входящего сальдо
update pages_show set sort_kod = 26 where page_url = 344;       -- список лс для поставщиков
update pages_show set sort_kod = 27 where page_url = 345;       -- история событий лс
update pages_show set sort_kod = 28 where page_url = 347;       -- история событий дома

update pages_show set sort_kod = 1 where page_url = 126;        -- сальдо по дому
update pages_show set sort_kod = 1 where page_url = 362;        -- сальдо по дому
update pages_show set sort_kod = 2 where page_url = 99;         -- реквизиты дома
update pages_show set sort_kod = 3 where page_url = 190;        -- перечень услуг дома
update pages_show set sort_kod = 4 where page_url = 59;
update pages_show set sort_kod = 5 where page_url = 63;
update pages_show set sort_kod = 6 where page_url = 92;         -- список ГПУ
update pages_show set sort_kod = 7 where page_url = 185;        -- список ПУ для коммуналок
update pages_show set sort_kod = 8 where page_url = 61;
update pages_show set sort_kod = 9 where page_url = 67;         -- показания ГПУ для дома
update pages_show set sort_kod = 10 where page_url = 186;       -- показания ПУ для коммуналок
update pages_show set sort_kod = 11 where page_url = 191;       -- недопоставки по дому
update pages_show set sort_kod = 12 where page_url = 124;       -- расчет ОДН
update pages_show set sort_kod = 13 where page_url = 167;       -- расходы по дому
update pages_show set sort_kod = 14 where page_url = 184;       -- протокол расчета ОДН
update pages_show set sort_kod = 15 where page_url = 192;       -- настройки ОДН
update pages_show set sort_kod = 16 where page_url = 57;
update pages_show set sort_kod = 17 where page_url = 137;       -- отчеты для дома

update pages_show set sort_kod = 1 where page_url = 205;        -- одна заявка
update pages_show set sort_kod = 2 where page_url = 193;        -- наряд-заказ

update pages_show set sort_kod = 1 where page_url = 108;
update pages_show set sort_kod = 2 where page_url = 109;        -- груп опер с реквизитами дома
update pages_show set sort_kod = 3 where page_url = 104;        -- груп опер с услугами ЛС
update pages_show set sort_kod = 4 where page_url = 182;        -- груп опер с услугами ЛС выбранных домов
update pages_show set sort_kod = 5 where page_url = 100;
update pages_show set sort_kod = 6 where page_url = 102;        -- груп опер с параметрами дома
update pages_show set sort_kod = 7 where page_url = 180;        -- груп опер с характеристиками жилья для ЛС выбранных домов
update pages_show set sort_kod = 8 where page_url = 110;        -- груп опер с недопоставками
update pages_show set sort_kod = 9 where page_url = 179;        -- груп опер с домовыми недопоставками
update pages_show set sort_kod = 10 where page_url = 196;       -- груп опер с группами ЛС
update pages_show set sort_kod = 11 where page_url = 241;       -- списочный ввод показаний ПУ
update pages_show set sort_kod = 12 where page_url = 263;       -- гпупповой ввод характеристик жилья
update pages_show set sort_kod = 13 where page_url = 264;       -- генерация ИПУ по списку ЛС

update pages_show set sort_kod = 1 where page_url = 168;        -- тарифы
update pages_show set sort_kod = 2 where page_url = 170;        -- системные параметры
update pages_show set sort_kod = 3 where page_url = 49;         -- список поставщиков
update pages_show set sort_kod = 4 where page_url = 256;         -- контрагенты (РТ)
update pages_show set sort_kod = 5 where page_url = 355;         -- контрагенты
update pages_show set sort_kod = 6 where page_url = 352;         -- договоры

-- упорядочение отчетов по алфавиту
drop table if exists t_report;
create temp table t_report (id serial, nzp_page integer, nzp_act integer, act_name character(100));
insert into t_report (nzp_page, nzp_act, act_name) select distinct a.cur_page, a.nzp_act, b.act_name from actions_show a, s_actions b where a.nzp_act > 200 and a.nzp_act < 400 and a.nzp_act = b.nzp_act order by b.act_name;
update actions_show a set sort_kod = (select id from t_report b where b.nzp_page = a.cur_page and b.nzp_act = a.nzp_act) where exists (select * from t_report c where nzp_page = cur_page and c.nzp_act = a.nzp_act);
drop table t_report;

--удаление ролей
delete from role_pages where nzp_role >= 10 and nzp_role < 1000;
delete from role_actions where nzp_role >= 10 and nzp_role < 1000;
delete from img_lnk where tip = 3 and kod >= 10 and kod < 1000;
delete from roleskey where tip = 105 and kod >= 10 and kod < 1000;
delete from roleskey where nzp_role >= 10 and nzp_role < 1000;
delete from s_roles where nzp_role >= 10 and nzp_role < 1000;

--подсистемы/роли
insert into s_roles (nzp_role, role, page_url, sort) values (10, 'Картотека', 31, 1);
insert into s_roles (nzp_role, role, page_url, sort) values (11, 'Аналитика', 81, 4);
insert into s_roles (nzp_role, role, page_url, sort) values (12, 'Администратор', 151, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (13, 'Приборы учета', 31, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (14, 'Паспортистка', 31, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (15, 'Финансы', 201, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (16, 'Картотека домов', 31, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (17, 'Полный доступ', 0, 17);
insert into s_roles (nzp_role, role, page_url, sort) values (18, 'Скрывать персональные данные', 0, 18);
insert into s_roles (nzp_role, role, page_url, sort) values (19, 'Учет претензий', 31, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (20, 'Касса', 198, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (21, 'Аналитический центр', 226, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (22, 'Выплаты ЮЛ', 278, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (23, 'Субсидии ЮЛ', 31, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (24, 'Субсидии ФЛ', 31, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (25, 'Картотека (Внешние БД)', 31, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (26, 'Настройка системы', 306, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (27, 'Обмен данными', 321, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (28, 'Должники', 31, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (30, 'Отчеты', 333, 2);
insert into s_roles (nzp_role, role, page_url, sort) values (31, 'Абонентский отдел', 31, 1);

--упорядочение подсистем
update s_roles set sort = 1 where nzp_role = 10; --Картотека
update s_roles set sort = 2 where nzp_role = 11; --Аналитика
update s_roles set sort = 3 where nzp_role = 14; --Паспортистка
update s_roles set sort = 4 where nzp_role = 19; --Учет претензий
update s_roles set sort = 5 where nzp_role = 15; --Финансы
update s_roles set sort = 6 where nzp_role = 20; --Касса
update s_roles set sort = 7 where nzp_role = 16; --Картотека домов
update s_roles set sort = 8 where nzp_role = 13; --Приборы учета
update s_roles set sort = 9 where nzp_role = 21; --Аналитический центр
update s_roles set sort = 10 where nzp_role = 24; --Субсидии ФЛ
update s_roles set sort = 11 where nzp_role = 23; --Расчет Дотации-Редактирование
update s_roles set sort = 12 where nzp_role = 22; --Дотации
update s_roles set sort = 13 where nzp_role = 27; --Настройка системы
update s_roles set sort = 14 where nzp_role = 28; --Должники
update s_roles set sort = 15 where nzp_role = 30; --Отчеты
update s_roles set sort = 16 where nzp_role = 12; --Администратор
update s_roles set sort = 17 where nzp_role = 26; --Настройка системы
update s_roles set sort = 18 where nzp_role = 31; --Сотрудник абонентского отдела

--иконки подсистем
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 10, 'specialist_rc.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 11, 'analitics.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 12, 'system_lock.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 13, 'counters.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 14, 'people.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 15, 'finance.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 16, 'folder_home.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 19, 'supg.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 20, 'cash_register.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 21, 'analyticcenter.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 22, 'subsidy.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 23, 'specialist_rc.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 24, 'subsidy.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 25, 'folder_home.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 26, 'folder_home.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 27, 'folder_home.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 28, 'folder_home.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 30, 'folder_home.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 31, 'folder_home.png');

--добавление ролей
insert into s_roles (nzp_role, role, page_url, sort) values (901, 'Картотека (Редактирование)'      , 0, 901);
insert into s_roles (nzp_role, role, page_url, sort) values (902, 'Администратор (Редактирование)'  , 0, 902);
insert into s_roles (nzp_role, role, page_url, sort) values (903, 'Приборы учета (Редактирование)'  , 0, 903);
insert into s_roles (nzp_role, role, page_url, sort) values (904, 'Паспортистка (Редактирование)'   , 0, 904); -- для Самары
insert into s_roles (nzp_role, role, page_url, sort) values (905, 'Аналитика (Редактирование)'      , 0, 905);
insert into s_roles (nzp_role, role, page_url, sort) values (915, 'Финансы (Редактирование)'        , 0, 915);
insert into s_roles (nzp_role, role, page_url, sort) values (919, 'Администратор УПГ' , 0, 919);
insert into s_roles (nzp_role, role, page_url, sort) values (920, 'Касса (Редактирование)'          , 0, 920);
insert into s_roles (nzp_role, role, page_url, sort) values (921, 'Расчет начислений'               , 0, 921);
insert into s_roles (nzp_role, role, page_url, sort) values (922, 'Картотека-Специфика-Самара'      , 0, 922);
insert into s_roles (nzp_role, role, page_url, sort) values (923, 'Паспортистка-Специфика-Самара'   , 0, 923);
insert into s_roles (nzp_role, role, page_url, sort) values (924, 'Картотека-Специфика-Зеленодольск', 0, 924);
insert into s_roles (nzp_role, role, page_url, sort) values (925, 'Паспортистка-Специфика-Зеленодольск', 0, 925);
insert into s_roles (nzp_role, role, page_url, sort) values (926, 'Картотека-Специфика-РТ'          , 0, 926);
insert into s_roles (nzp_role, role, page_url, sort) values (927, 'Картотека-Изменение-Специфика-Самара', 0, 927);
insert into s_roles (nzp_role, role, page_url, sort) values (928, 'Картотека-Изменение-Специфика-Зеленодольск', 0, 928);
insert into s_roles (nzp_role, role, page_url, sort) values (929, 'Паспортистка-Изменение-Специфика-Самара', 0, 929);
insert into s_roles (nzp_role, role, page_url, sort) values (930, 'Аналитика-Специфика-Самара', 0, 930);
insert into s_roles (nzp_role, role, page_url, sort) values (931, 'Аналитика-Специфика-Зеленодольск-РТ', 0, 931);
insert into s_roles (nzp_role, role, page_url, sort) values (932, 'Администратор-Специфика-Самара', 0, 932);
insert into s_roles (nzp_role, role, page_url, sort) values (933, 'Справочники (Редактирование)', 0, 933);
insert into s_roles (nzp_role, role, page_url, sort) values (934, 'Оператор УПГ', 0, 934);
insert into s_roles (nzp_role, role, page_url, sort) values (935, 'Диспетчер УПГ', 0, 935);
insert into s_roles (nzp_role, role, page_url, sort) values (936, 'Подрядчик УПГ', 0, 936);
insert into s_roles (nzp_role, role, page_url, sort) values (937, 'УК УПГ', 0, 937);
insert into s_roles (nzp_role, role, page_url, sort) values (938, 'Аналитика-Специфика-РТ-Без статистики поступлений', 0, 938);
insert into s_roles (nzp_role, role, page_url, sort) values (939, 'Приборы учета-Редактирование-Самара', 0, 939);
insert into s_roles (nzp_role, role, page_url, sort) values (940, 'Приборы учета (Редактирование)-Списочный ввод показаний', 0, 940);
insert into s_roles (nzp_role, role, page_url, sort) values (941, 'Приборы учета-Списочный ввод показаний', 0, 940);
insert into s_roles (nzp_role, role, page_url, sort) values (942, 'Финансы-Справочники (Редактирование)', 0, 942);
insert into s_roles (nzp_role, role, page_url, sort) values (943, 'Администратор-Справочники', 0, 943);
insert into s_roles (nzp_role, role, page_url, sort) values (944, 'Дотации (Редактирование)', 0, 944);
insert into s_roles (nzp_role, role, page_url, sort) values (945, 'Расчет дотаций (Редактирование)', 0, 945);
insert into s_roles (nzp_role, role, page_url, sort) values (946, 'Картотека (Распределение недопоставок УПГ)', 0, 946);
insert into s_roles (nzp_role, role, page_url, sort) values (947, 'Картотека-Саха', 0, 947);
insert into s_roles (nzp_role, role, page_url, sort) values (948, 'Картотека Саха (Редактирование)', 0, 948);
insert into s_roles (nzp_role, role, page_url, sort) values (949, 'Рассылка сообщений (УПГ)', 0, 949);
insert into s_roles (nzp_role, role, page_url, sort) values (950, 'Выгрузка недопоставок (УПГ)', 0, 950);
insert into s_roles (nzp_role, role, page_url, sort) values (951, 'Обновление адресов (УПГ)', 0, 951);
insert into s_roles (nzp_role, role, page_url, sort) values (952, 'Аналитика (Платежный документ)', 0, 952);
insert into s_roles (nzp_role, role, page_url, sort) values (953, 'Картотека-Специфика-Губкин', 0, 953);
insert into s_roles (nzp_role, role, page_url, sort) values (954, 'Паспортистка-Специфика-Губкин', 0, 954);
insert into s_roles (nzp_role, role, page_url, sort) values (955, 'Картотека-Астрахань', 0, 955);
--956 свободно
--insert into s_roles (nzp_role, role, page_url, sort) values (957, 'Льготы (Просмотр)', 0, 957);
insert into s_roles (nzp_role, role, page_url, sort) values (958, 'Картотека-Специфика-Тула', 0, 958);
insert into s_roles (nzp_role, role, page_url, sort) values (959, 'Обмен данными(Губкин)', 0, 959);
insert into s_roles (nzp_role, role, page_url, sort) values (960, 'Загрузка наследуемой информации', 0, 960);
insert into s_roles (nzp_role, role, page_url, sort) values (961, 'Картотека-Специфика-Казань-НСав', 0, 961);
insert into s_roles (nzp_role, role, page_url, sort) values (962, 'Паспортистка-Специфика-Казань-НСав', 0, 962);
insert into s_roles (nzp_role, role, page_url, sort) values (963, 'Картотека-Изменение-Специфика-Казань-НСав', 0, 963);
insert into s_roles (nzp_role, role, page_url, sort) values (964, 'Паспортистка-Изменение-Специфика-Казань-НСав', 0, 964);
insert into s_roles (nzp_role, role, page_url, sort) values (965, 'Картотека-Изменение-Специфика-Тула', 0, 965);
insert into s_roles (nzp_role, role, page_url, sort) values (966, 'Просмотр индивидуальных приборов учета', 0, 966);
insert into s_roles (nzp_role, role, page_url, sort) values (967, 'Редактирование индивидуальных приборов учета', 0, 967);
insert into s_roles (nzp_role, role, page_url, sort) values (968, 'Просмотр общеквартирных приборов учета', 0, 968);
insert into s_roles (nzp_role, role, page_url, sort) values (969, 'Редактирование общеквартирных приборов учета', 0, 969);
insert into s_roles (nzp_role, role, page_url, sort) values (970, 'Просмотр общедомовых приборов учета', 0, 970);
insert into s_roles (nzp_role, role, page_url, sort) values (971, 'Редактирование общедомовых приборов учета', 0, 971);
insert into s_roles (nzp_role, role, page_url, sort) values (972, 'Просмотр групповых приборов учета', 0, 972);
insert into s_roles (nzp_role, role, page_url, sort) values (973, 'Редактирование групповых приборов учета', 0, 973);
insert into s_roles (nzp_role, role, page_url, sort) values (974, 'Генерация лицевых счетов', 0, 974);
insert into s_roles (nzp_role, role, page_url, sort) values (975, 'Генерация индивидуальных приборов учета', 0, 975);
insert into s_roles (nzp_role, role, page_url, sort) values (976, 'Взаимодействие с соц.защитой', 0, 976); -- для Тулы
insert into s_roles (nzp_role, role, page_url, sort) values (977, 'Небо-реестр', 0, 977); -- для Web-сервиса
insert into s_roles (nzp_role, role, page_url, sort) values (978, 'Взаимодействие с системами банк-клиент', 0, 978);
insert into s_roles (nzp_role, role, page_url, sort) values (979, 'Настройка форматов выгрузки в системы банк-клиент', 0, 979);
insert into s_roles (nzp_role, role, page_url, sort) values (980, 'Настройка уникальных кодов', 0, 980);
insert into s_roles (nzp_role, role, page_url, sort) values (981, 'Обмен данными с поставщиками услуг', 0, 981);
insert into s_roles (nzp_role, role, page_url, sort) values (982, 'Должники - настройки', 0, 982);
insert into s_roles (nzp_role, role, page_url, sort) values (983, 'Взаимодействие с банком', 0, 983); --Астрахань
insert into s_roles (nzp_role, role, page_url, sort) values (984, 'Быстрый ввод показаний ПУ', 0, 984); --Тула
insert into s_roles (nzp_role, role, page_url, sort) values (985, 'Загрузка оплат от ВТБ24', 0, 985); --Тула
insert into s_roles (nzp_role, role, page_url, sort) values (986, 'Управление связанными услугами', 0, 986);
insert into s_roles (nzp_role, role, page_url, sort) values (987, 'Сальдо по перечислениям в разрезе домов', 0, 987);
insert into s_roles (nzp_role, role, page_url, sort) values (988, 'Управление очередями задач', 0, 988);
insert into s_roles (nzp_role, role, page_url, sort) values (989, 'Исправление ошибок в распределения оплат', 0, 989);
insert into s_roles (nzp_role, role, page_url, sort) values (990, 'Закрытие месяца(подготовка данный для печати счетов)', 0, 990);
insert into s_roles (nzp_role, role, page_url, sort) values (991, 'Генерация платежных кодов', 0, 991);
insert into s_roles (nzp_role, role, page_url, sort) values (992, 'Картотека-Специфика-РСО', 0, 992);
insert into s_roles (nzp_role, role, page_url, sort) values (993, 'Справочник параметров', 0, 993);
insert into s_roles (nzp_role, role, page_url, sort) values (994, 'Смена управляющей организации', 0, 994);
insert into s_roles (nzp_role, role, page_url, sort) values (995, 'Просмотр процентов удержания по домам', 0, 995);
insert into s_roles (nzp_role, role, page_url, sort) values (996, 'Редактирование процентов удержания по домам', 0, 996);
insert into s_roles (nzp_role, role, page_url, sort) values (997, 'Сотрудник абонентского отдела (Редактирование)', 0, 997);
insert into s_roles (nzp_role, role, page_url, sort) values (998, 'Взаимодействие с Банком', 0, 998);
insert into s_roles (nzp_role, role, page_url, sort) values (999, 'Сальдо по перечислениям по договорам', 0, 999);
insert into s_roles (nzp_role, role, page_url, sort) values (800, 'Проценты удержания по домам', 0, 800); --по договорам
insert into s_roles (nzp_role, role, page_url, sort) values (801, 'Редактирование процентов удержания по домам', 0, 801); --по договорам
insert into s_roles (nzp_role, role, page_url, sort) values (802, 'Просмотр договоров на оказание ЖКУ', 0, 802);
insert into s_roles (nzp_role, role, page_url, sort) values (803, 'Редактирование договоров на оказание ЖКУ', 0, 803);
insert into s_roles (nzp_role, role, page_url, sort) values (804, 'Просмотр контрагентов', 0, 804);
insert into s_roles (nzp_role, role, page_url, sort) values (805, 'Редактирование контрагентов', 0, 805);
insert into s_roles (nzp_role, role, page_url, sort) values (806, 'Редактирование сальдо по перечислениям', 0, 806);
insert into s_roles (nzp_role, role, page_url, sort) values (807, 'Финансы-Специфика-Без договоров', 0, 807);
insert into s_roles (nzp_role, role, page_url, sort) values (808, 'Финансы-Редактирование-Специфика-Без договоров', 0, 808);
insert into s_roles (nzp_role, role, page_url, sort) values (809, 'Просмотр управляющих организаций', 0, 809);
insert into s_roles (nzp_role, role, page_url, sort) values (810, 'Редактирование управляющих организаций', 0, 810);
insert into s_roles (nzp_role, role, page_url, sort) values (811, 'Просмотр статистики по начислениям', 0, 811); -- по договорам
insert into s_roles (nzp_role, role, page_url, sort) values (812, 'Редактирование параметров договоров', 0, 812);
insert into s_roles (nzp_role, role, page_url, sort) values (813, 'Изменение адреса лицевых счетов', 0, 813);
insert into s_roles (nzp_role, role, page_url, sort) values (814, 'Проверки перед закрытие месяца', 0, 814);


insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 901, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 921, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 933, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 946, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 957, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 966, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 967, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 968, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 969, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 970, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 971, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 972, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 973, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 974, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 975, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 976, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 977, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 984, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 986, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 991, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 993, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 994, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 813, '');
insert into roleskey (nzp_role, tip, kod, sign) values (10, 105, 814, '');
insert into roleskey (nzp_role, tip, kod, sign) values (11, 105, 905, '');
insert into roleskey (nzp_role, tip, kod, sign) values (11, 105, 921, '');
insert into roleskey (nzp_role, tip, kod, sign) values (11, 105, 952, '');
insert into roleskey (nzp_role, tip, kod, sign) values (12, 105, 902, '');
insert into roleskey (nzp_role, tip, kod, sign) values (12, 105, 921, '');
insert into roleskey (nzp_role, tip, kod, sign) values (12, 105, 960, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 903, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 966, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 967, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 968, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 969, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 970, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 971, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 972, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 973, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 975, '');
insert into roleskey (nzp_role, tip, kod, sign) values (13, 105, 984, '');
insert into roleskey (nzp_role, tip, kod, sign) values (14, 105, 904, ''); -- для Самары
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 805, '');
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 806, '');
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 915, '');
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 942, '');
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 978, '');
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 979, '');
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 983, '');
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 985, '');
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 989, '');
insert into roleskey (nzp_role, tip, kod, sign) values (19, 105, 919, '');
insert into roleskey (nzp_role, tip, kod, sign) values (19, 105, 934, '');
insert into roleskey (nzp_role, tip, kod, sign) values (19, 105, 935, '');
insert into roleskey (nzp_role, tip, kod, sign) values (19, 105, 936, '');
insert into roleskey (nzp_role, tip, kod, sign) values (19, 105, 937, '');
insert into roleskey (nzp_role, tip, kod, sign) values (19, 105, 951, '');
insert into roleskey (nzp_role, tip, kod, sign) values (20, 105, 920, '');
insert into roleskey (nzp_role, tip, kod, sign) values (22, 105, 944, ''); -- финансирование дотаций
insert into roleskey (nzp_role, tip, kod, sign) values (23, 105, 945, ''); -- расчет дотаций
insert into roleskey (nzp_role, tip, kod, sign) values (19, 105, 949, '');
insert into roleskey (nzp_role, tip, kod, sign) values (19, 105, 950, '');
insert into roleskey (nzp_role, tip, kod, sign) values (23, 105, 921, '');
insert into roleskey (nzp_role, tip, kod, sign) values (27, 105, 981, '');
insert into roleskey (nzp_role, tip, kod, sign) values (28, 105, 982, '');
insert into roleskey (nzp_role, tip, kod, sign) values (31, 105, 997, ''); --  сотрудник абонентского отдела
insert into roleskey (nzp_role, tip, kod, sign) values (15, 105, 998, ''); -- Взаимодействие с Банком
insert into roleskey (nzp_role, tip, kod, sign) values (31, 105, 921, ''); --  сотрудник абонентского отдела
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2009, ''); -- параметры, доступные в Паспортистке (Самара)
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2015, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2016, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2017, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2018, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2019, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2020, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2021, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2022, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 2023, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 578, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 1047, '');
insert into roleskey (nzp_role, tip, kod, sign) values (923, 106, 1048, '');


--настройки слияния ролей: nzp_role_from вливается в роль nzp_role_to и перестает существовать как самостоятельная роль
create temp table t_role_merging (nzp_role_from integer, nzp_role_to integer);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 10 and a.nzp_role in (802,809,811,922,924,926,947,953,955,958,961,992);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 11 and a.nzp_role in (811,930,931,938,987,999);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 12 and a.nzp_role in (932,943,959,980,988,990);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 13 and a.nzp_role in (941);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 14 and a.nzp_role in (923,925,954,962);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 15 and a.nzp_role in (802,995,987,999,800,804,807);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 901 and a.nzp_role in (812,927,928,948,963,965);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 903 and a.nzp_role in (939,940);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 904 and a.nzp_role in (929,964);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 915 and a.nzp_role in (996,801,808);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 933 and a.nzp_role in (810);
insert into t_role_merging (nzp_role_from, nzp_role_to) select a.nzp_role, b.nzp_role from s_roles a, s_roles b where b.nzp_role = 942 and a.nzp_role in (803,812);


--Добавление страниц в подсистему Картотека
insert into role_pages (nzp_role, nzp_page,sign) values (10,0,'');

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,15,999,30,31,32,33,35,36,37,38,41,42,44,45,49,50,51,53,54,55,59,61,62,63,64,66,67,68,69,70,71,73,75,92,93,94,95,96,97,98,99,111,112,121,122,123,124,126,131,133,134,137,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,184,185,186,187,188,190,191,192,195,206,210,221,222,223,224,225,227,228,229,230,231,240,261,294,296,230,231,240,244,261,294,296,300,301,303,337,338,341,344,345
,6,226,323,346,347,351 -- в разработке
) 
and b.nzp_role = 10;


insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where (a.cur_page in (5,44,45,49,50,51,53,54,55,59,61,62,63,64,66,67,68,69,92,93,94,95,96,97,98,99,111,112,121,122,123,124,126,131,133,134,137,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,184,185,186,187,188,190,191,192,206,210,221,222,223,224,225,227,228,229,230,231,240,244,261,294,296,300,301,303,306,337,338,341
,6,226,321,323,345,346,347,351 -- в разработке
)
or (a.cur_page = 31 and a.nzp_act in (1,2,502,503,505,506,507,508,510))
or (a.cur_page = 32 and a.nzp_act in (1,2,501,503,505,506,507,508,510))
or (a.cur_page = 33 and a.nzp_act in (1,2,501,502,505,506,507,508,510))
or (a.cur_page = 35 and a.nzp_act in (1,2,501))
or (a.cur_page = 36 and a.nzp_act in (1,2,501,502,503,505,507,508,510))
or (a.cur_page = 37 and a.nzp_act in (1,2,501,502,503,505,506,508,510))
or (a.cur_page = 38 and a.nzp_act in (1,2,501,502,503,505,506,507,510))
or (a.cur_page = 41 and a.nzp_act in (5,73,601,602,610,701,702,703,705,704))
or (a.cur_page = 42 and a.nzp_act in (5,603,604,610,701,702,703,705,704))
or (a.cur_page = 195 and a.nzp_act in (1,2,501,502,503,505,506,507,508))
)
and a.nzp_act in (1,2,3,5,8,14,20,66,67,69,71,72,76,77,80,103,104,128,175,180
,206,207,209,210,212,213,228,229,230,231,232,233,234,235,236,239,240,244,245,246,247,248,249,250,251,253,254,255,257,258,259,260,261,266,267,269,270,271,272,294,295,300,302,303,304,306,307,308,309,310,311,317,318,319,320,321,324,325,327,340,341,342,346,347,348,349,350,351
,256,288,289,290,299,323,326,553 -- в разработке
,501,502,503,505,506,507,508,510,520,521,522,523,528,542,601,602,603,604,605,606,607,610,701,702,703,705,851,852,862)
and b.nzp_role = 10;

--Заполнение роли Картотека-Специфика-Самара

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 922 and r.nzp_role = 10
and r.nzp_page in (35,36,44,49,112,126,133,134,137,166,167,168,174,175,192,206,227,228,296);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 922 and r.nzp_role = 10
and (
        (r.nzp_page in (35,36,44,49,126,166,167,174,175,192,227,228,296) and r.nzp_act not in (124)) or
        (r.nzp_page in (133,134,137,206) and r.nzp_act in (69,610)) or
		nzp_act in (206,228,230,231,232,233,234,235,236,239,240,247,248,250,251,253,254,255,259,260,269,271,272,294,299,300,306,310,320,326,327,342)
)
and nzp_act not in (507);

--Заполнение роли Картотека-Специфика-Казань-НСав

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 961 and r.nzp_role = 10
and r.nzp_page in (35,36,37,44,49,112,126,133,134,137,166,167,168,174,175,192,206,296,66,67,92,93,185,186,187,227,228);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 961 and r.nzp_role = 10
and (
        (r.nzp_page in (35,36,37,44,49,126,166,167,192,296,66,67,92,93,174,175,185,186,187,227,228) and r.nzp_act not in (124)) or
        (r.nzp_page in (133,134,137,206) and r.nzp_act in (69,610)) or
	nzp_act in (206,207,209,210,212,213,228,229,230,231,232,233,234,235,236,239,240,244,245,246,247,248,249,250,251,253,254,255,256,257,258,259,260,261,266,267,269,270,271,272,288,289,290,294,295,299,300,302,306,309,310,320,324,325,326,327,507)
)
and nzp_act not in (507);

--Заполнение роли Картотека-Специфика-Зеленодольск

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 924 and r.nzp_role = 10
and r.nzp_page in (35,36,37,44,112,124,126,133,134,137,66,67,92,93,174,175,185,186,187,206,227,228,296);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 924 and r.nzp_role = 10
and (
        r.nzp_page in (35,36,37,44,49,112,124,126,66,67,92,93,174,175,185,186,187,227,228,296) or
        (r.nzp_page in (133,134,137,206) and r.nzp_act in (69,610)) or
	nzp_act in (207,209,210,212,213,228,244,245,246,247,260,266,267,294,295,324,325,507)
);

--Заполнение роли Картотека-Специфика-Губкин

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 953 and r.nzp_role = 10
and r.nzp_page in (35,36,44,49,112,126,134,137,166,167,174,175,192,206,227,228,296);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 953 and r.nzp_role = 10
and (
	r.nzp_page in (35,36,44,49,166,167,174,175,192,227,228,296) or
	(r.nzp_page in (134,137,206) and r.nzp_act in (69,610)) or
	nzp_act in (207,209,210,228,247,260,303,317,318,319,328,259,295)
)
and r.nzp_act not in (507);

--Заполнение роли Картотека-Специфика-РТ

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 926 and r.nzp_role = 10
and r.nzp_page in (35,36,37,44,49,112,124,126,134,206,166,167,174,175,66,67,92,93,185,186,187,227,228);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 926 and r.nzp_role = 10
and (
        r.nzp_page in (35,36,44,49,37,112,124,126,166,167,174,175,206,66,67,92,93,185,186,187,227,228) or
        r.nzp_act in (507) or
	(r.nzp_page in (134,206) and r.nzp_act in (69,610)) or
        nzp_act in (212,213,228,244,245,246,247,266,267,294,295,324,325)
);

--Заполнение роли Картотека-Саха

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 947 and r.nzp_role = 10
and r.nzp_page in (35,36,44,112,126,133,134,137,166,167,168,174,175,192,206,227,228);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 947 and r.nzp_role = 10
and (
        (r.nzp_page in (35,36,44,112,126,166,167,168,174,175,192,227,228)) or
        (r.nzp_page in (133,134,137,206)  and r.nzp_act in (69,610)) or
		r.nzp_act in (175,207,208,209,210,211,212,229,230,232,234,247,248,250,251,253,254,257,260,266,267,271,289,288,290,291)
)
and r.nzp_act not in (507);

--Заполнение роли Картотека-Астрахань

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 955 and r.nzp_role = 10
and r.nzp_page in (35,36,134,137,166,167,192,296,66,67,92,93,185,186,187,337);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 955 and r.nzp_role = 10
and (
	r.nzp_page in (35,36,166,167,192,296,66,67,92,93,185,186,187,337) or
	(r.nzp_page in (134,137,206)  and r.nzp_act in (69,610)) or
        nzp_act in (207,209,210,228,244,245,246,247,260,266,267,294,295,303,304,311,321,323)
)
and r.nzp_act not in (507);

--Заполнение роли Картотека-Специфика Тула

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 958 and r.nzp_role = 10
and r.nzp_page in (35,36,134,137,166,167,192,206,296,66,67,92,93,185,186,187,337,338,344,347);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b

where b.nzp_role = 958 and r.nzp_role = 10
and (
	r.nzp_page in (35,36,166,167,192,296,66,67,92,93,185,186,187,337,338,344,347) or
	(r.nzp_page in (134,137,206) and r.nzp_act in (69,610)) or
	nzp_act in (207,209,210,228,247,259,260,295,303,323,349,351,553)
)
and r.nzp_act not in (507);

--Заполнение роли Картотека-Специфика-РСО

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 992 and r.nzp_role = 10
and r.nzp_page in (35,36,44,49,112,126,134,137,166,167,174,175,192,206,227,228,296,337);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 992 and r.nzp_role = 10
and (
        (r.nzp_page in (35,36,44,49,112,126,166,167,192,227,228,296) and r.nzp_act not in (124)) or
        (r.nzp_page in (134,137,206) and r.nzp_act in (69,610)) or
		nzp_act in (206,212,228,229,230,231,232,233,234,235,236,239,240,247,248,249,250,251,253,254,255,256,257,258,259,260,261,266,267,269,270,271,272,288,289,290,294,295,299,300,302,306,309,310,320,326,327,342,346)
)
and nzp_act not in (507);

--удаление из картотеки специфики

delete from  role_pages where nzp_role = 10
and nzp_page in (35,36,37,44,49,66,67,92,93,112,124,126,133,134,137,166,167,174,175,185,186,187,192,206,227,228,296,337,338);

delete from role_actions where nzp_role = 10
and (
        nzp_page in (35,36,37,44,49,66,67,92,93,112,124,126,133,134,137,166,167,174,175,185,186,187,192,206,227,228,296,337,338) or
        nzp_act in (175,507)
);

--Добавление страниц и действий к роли Картотека (Редактирование)

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (74,100,101,102,103,104,105,108,109,110,179,180,181,182,183,196,242,243,262,260,264,295,299,302,316,344
,263,346 -- для показа
)
and b.nzp_role = 901;


insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where (a.cur_page in (100,101,102,103,104,105,108,109,110,179,180,181,182,183,196,262,295,299,302,316,344
,263,323,346 -- для показа
)
or (a.cur_page in (5,31,32,33,35,36,37,38,41,42,44,45,49,50,51,53,54,55,59,61,62,63,64,66,67,68,69,92,93,94,95,96,97,98,99,111,121,122,123,124,131,133,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,184,185,186,187,188,190,191,192,195,196,210,221,222,223,224,225,227,228,229,230,231,240,242,243,261,294,295,296,300,301,303,344
,6,244,323,346 -- в разработке
)
and a.nzp_act in (4,9,13,15,16,17,18,19,21,22,24,25,26,61,64,109,110,111,112,128,158,169,170,172,176,178,179,190,543,611
,323 -- для показа
))
)
and b.nzp_role = 901;

delete from role_actions where nzp_role = 901 and nzp_page = 162 and nzp_act = 4;


--Заполнение роли Картотека-Изменение-Специфика-Самара

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 927 and r.nzp_role = 901
and r.nzp_page in (166,167,168,243,295,296);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 927 and r.nzp_role = 901
and (
        (r.nzp_page in (44,166,167,168,174,227,228,243,295,296)) or
        (nzp_act = 112) or
        (r.nzp_page = 41 and nzp_act = 172)
);

delete from role_actions where nzp_role = 927 and 
(
	nzp_page in (37)
);      

--Заполнение роли Картотека-Изменение-Специфика-Казань-НСав

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 963 and r.nzp_role = 901
and r.nzp_page in (243,295);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 963 and r.nzp_role = 901
and (
        (r.nzp_page in (37,44,166,167,168,174,227,228,243,295,296)) or
        (nzp_act = 112) or
        (r.nzp_page = 41 and nzp_act = 172)
);

delete from role_actions where nzp_role = 963 and 
(
    nzp_page in (37) 
);      

--Заполнение роли Картотека-Изменение-Специфика-Тула

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 965 and r.nzp_role = 901
and r.nzp_page in (166,167,168,243,295,296,308);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 965 and r.nzp_role = 901
and (
	(r.nzp_page in (44,166,167,168,227,228,243,295,296,308)) or
	(nzp_act = 112) or
	(r.nzp_page = 41 and nzp_act = 172)
);

--Заполнение роли Картотека-Изменение-Специфика-Астрахань

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 995 and r.nzp_role = 901
and r.nzp_page in (296,308);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 995 and r.nzp_role = 901
and r.nzp_page in (296,308);


--Заполнение роли Картотека-Изменение-Специфика-Зеленодольск

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 928 and r.nzp_role = 901
and (
        r.nzp_page in (37,166,167,168,296)
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 928 and r.nzp_role = 901
and (
        r.nzp_page in (37,44,166,167,168,174,227,228,296)
);

--Заполнение роли Картотека-Редактирование (Саха)

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 948 and r.nzp_role = 901
and (
        (r.nzp_page in (44,166,167,168,227,228,243))
);

delete from role_pages where nzp_role = 948 and 
(
    nzp_page in (37)
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 948 and r.nzp_role = 901
and (
        (r.nzp_page in (44,166,167,168,174,227,228,243)) or
        (nzp_act = 112)
);

delete from role_actions where nzp_role = 948 and 
(
    nzp_page in (37) 
);

--Добавление страниц и действий к роли Картотека (Администратор)

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (220,10,901)
and b.nzp_role = 946;


insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (220)
and a.nzp_act in (160,296,610,611)
and b.nzp_role = 946;


--Заполнение роли Справочники (Редактирование)

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 933 and r.nzp_role = 901
and r.nzp_page in (45,49,168,169,170,175,210,221,222,223,224,229,230,231,261,301);

--удаление из картотеки-изменение специфики

delete from role_pages where nzp_role = 901
and (
        nzp_page in (37,44,124,166,167,168,227,228,243,295,296) 
);

delete from role_actions where nzp_role = 901
and (
        nzp_page in (37,44,45,49,124,166,167,168,169,170,175,210,221,222,223,224,227,228,229,230,231,243,261,295,296,301) or
        nzp_act = 112 or
        (nzp_page = 41 and nzp_act = 172) or
        (nzp_page = 174 and nzp_act in (16,17,611))
);

--Добавление страниц в подсистему Аналитика
insert into role_pages (nzp_role, nzp_page,sign) values (11,0,'');

insert into role_pages(nzp_role, nzp_page,sign)
select distinct 11,a.cur_page,''
from pages_show a
where a.cur_page in (1,2,3,4,5,41,51,54,55,57,59,61,62,63,64,66,67,68,69,70,71,72,81,82,92,93,95,96,97,98,99,106,107,111,112,121,122,123,124,126,127,130,131,165,166,167,171,172,173,188,206,999
,292 -- в разработке
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct 11,cur_page,nzp_act,'',cast(null as integer)
from actions_show a
where (a.cur_page in (1,2,3,4,5,41,51,54,55,59,61,62,63,64,66,67,68,69,71,72,81,82,92,93,95,96,97,98,99,106,107,111,112,121,122,123,124,126,127,130,131,165,166,167,171,172,173,188,999,206,306
,292,297,298,303,321 -- в разработке
)
or (a.cur_page = 41 and a.nzp_act in (5,610,601,602,701,702,703,705)))
and a.nzp_act in (1,3,5,6,7,14,20,69,71,76,175,520,521,522,523,528,601,602,605,606,610,701,702,703,705,721,722,723,724,725,726
,212,244,245,246,266,267,306
,291 -- в разработке
);

--Заполнение роли Аналитика-Специфика-Самара
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 930 and r.nzp_role = 11
and r.nzp_page in (106,112,126,166,167,206);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 930 and r.nzp_role = 11
and (
        r.nzp_page in (106,112,126,166,167) or
        (r.nzp_page in (206) and r.nzp_act in (69,610,266,267,306 
,291 -- в разработке
))
);


--Заполнение роли Аналитика-Специфика-Зеленодольск-РТ
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from  role_pages r, s_roles b
where b.nzp_role = 931 and r.nzp_role = 11
and r.nzp_page in (106,107,112,124,126,206);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 931 and r.nzp_role = 11
and (
        r.nzp_page in (106,107,112,124,126) or
        (r.nzp_page in (206)  and r.nzp_act in (69,610,212,244,245,246,266,267,306
,291 -- в разработке
))
);

--Заполнение роли Аналитика-Специфика-РТ-Без статистики поступлений
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 938 and r.nzp_role = 11
and r.nzp_page in (124,126,206);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 938 and r.nzp_role = 11
and (
        r.nzp_page in (124,126) or
        (r.nzp_page in (206) and r.nzp_act in (69,610,212,244,245,246,266,267,306
,291 -- в разработке
))
);

--Заполнение роли Аналитика (Платежный документ)
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,'' from  role_pages r, s_roles b where b.nzp_role = 952 and r.nzp_role = 11 and r.nzp_page in (131);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act from role_actions r, s_roles b
where b.nzp_role = 952 and r.nzp_role = 11
and r.nzp_page in (131) and r.nzp_act in (610);


--удаление из Аналитики специфики
delete from role_pages where nzp_role = 11
and (
        nzp_page in (106,107,112,124,126,131,166,167,206) 
);

delete from  role_actions where nzp_role = 11
and nzp_page in (106,107,112,124,126,131,166,167,206);


--Добавление страниц и действий к роли Аналитика-Изменение
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (81,82,107,127,130) and a.nzp_act in (65,66,108)
and b.nzp_role = 905;


--заполнение подсистемы Администрирование
insert into role_pages(nzp_role, nzp_page,sign) values (12,0,'');

insert into role_pages (nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,44,49,73,77,150,151,152,153,154,155,160,161,256,266,268,270,271,307,348) 
and b.nzp_role = 12;


insert into role_actions (nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,44,49,150,151,152,153,154,155,160,161,256,266,268,270,271,307,348) 
and a.nzp_act in (1,3,4,5,9,10,11,12,13,15,61,68,70,95,96,129,131,132,153,158,163,169,170,187,530,531,532,533,534,535,555,608,609,610,611,612,701,702,703,705
,113,137,167,183 -- в разработке
)
and b.nzp_role = 12;


--Заполнение роли Администрирование-Специфика-Самара
	
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 932 and r.nzp_role = 12 
and ( (r.nzp_page in (268)) or
        (r.nzp_page in (77,268)));

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 932 and r.nzp_role = 12 
and (( r.nzp_act in (70)) or
        (r.nzp_page in (268)));


--Заполнение роли Администратор-Справочники

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 943 and r.nzp_role = 12 and (
        r.nzp_page in (44,49,73,256) 
);
delete from role_pages where nzp_role = 943 
and ((nzp_page in (268)) or
        ( nzp_page in (77,268)));
	
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 943 and r.nzp_role = 12 and (
        r.nzp_page in (44,49,73,256)
);
delete from role_actions where nzp_role = 943 
and ((nzp_act in (70)) or
        (nzp_page in (268)));

--удаление из Администрирования специфики

delete from role_pages where nzp_role = 12 and (
        (nzp_page in (44,49,73,256,268)) 
);

delete from role_actions where nzp_role = 12 and (
        ( nzp_act in (70)) or
        (nzp_page in (44,49,73,256,268)) 
);

--Добавление страниц в подсистему "Приборы учета" 

insert into role_pages (nzp_role, nzp_page, sign) values (13,0,'');

insert into role_pages (nzp_role, nzp_page, sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,30,31,35,40,41,42,53,54,61,62,63,68,69,70,71,73,74,75,94,171,172,206,241)
and b.nzp_role = 13;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,30,31,35,40,41,42,53,54,61,62,63,68,69,70,71,73,74,94,171,172,206,241) 
and a.nzp_act in (1,2,3,5,14,69,71,292,293,501,505,601,602,603,604,610,701,702,703,705,801,802,803, 177)
and (case when a.cur_page = 41 then case when a.nzp_act in (5,610, 701,702,703,705, 601,602) then 1 else 0 end else 1 end) = 1
and (case when a.cur_page = 42 then case when a.nzp_act in (5,610, 701,702,703,705, 603,604) then 1 else 0 end else 1 end) = 1
 and b.nzp_role = 13;

--Заполнение роли "Приборы учета-Списочный ввод показаний"
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 941 and r.nzp_role = 13
and (
        r.nzp_page in (241)
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 941 and r.nzp_role = 13
and (
        r.nzp_page in (241) 
);

--удаление из "Приборы учета" специфики

delete from role_pages where nzp_role = 13
and nzp_page in (241);

delete from role_actions where nzp_role = 13
and nzp_page in (241);


--Добавление страниц и действий к роли "Приборы учета-Изменение"

insert into role_pages (nzp_role, nzp_page, sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,999,30,31,35,40,41,42,53,54,61,62,63,68,69,70,71,73,74,77,94,171,172,241,243)
and b.nzp_role = 903;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (53,54,61,62,63,68,69,94,171,172,241,243) 
and a.nzp_act in (4,61,64,72,110,111,112,611)
and b.nzp_role = 903;


--Заполнение роли "Приборы учета-Редактирование-Самара"

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 939 and r.nzp_role = 903
and (
        r.nzp_page in (243) 
);
delete from role_pages where nzp_role = 939 and (nzp_page = 241 );

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 939 and r.nzp_role = 903
and (
        r.nzp_page in (243) or
        ( nzp_act = 112)
);
delete from role_actions where nzp_role = 939 and (nzp_page = 241 );

--Заполнение роли "Приборы учета (Редактирование)-Списочный ввод показаний"

insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,r.nzp_page,'' from role_pages r, s_roles b
where b.nzp_role = 940 and r.nzp_role = 903
and r.nzp_page in (241);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act from role_actions r, s_roles b
where b.nzp_role = 940 and r.nzp_role = 903
and r.nzp_page in (241);

--удаление из "Приборы учета-Изменение" специфики

delete from role_actions where nzp_role = 903
and (
        nzp_page in (241,243) or
        (nzp_act = 112)
);

delete from role_pages where nzp_role = 903
and nzp_page in (241,243);


--Добавление страниц в подсистему Паспортистка 
insert into role_pages (nzp_role, nzp_page, sign) values (14,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,30,31,34,40,41,51,56,64,70,73,75,91,98,133,134,135,136,162,163,164,169,170,173,177,178,194,195,211,212,213,214,215,216,217,218,219,345,338)
and b.nzp_role = 14;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,30,31,34,40,41,51,56,64,91,98,133,134,135,136,162,163,164,169,170,173,177,178,194,195,211,212,213,214,215,216,217,218,219,345)
and (case when a.cur_page = 41 then case when a.nzp_act in (5,610, 601,602, 701,702,703,705) then 1 else 0 end else 1 end) = 1
and a.nzp_act in (1,2,3,5,20,69,501,504,510,601,602,607,610,701,702,703,705,851,852,853,854,861,862,863
,74,75 -- не используется (не удалено на случай, если придется вернуть как было)
,201,202,203,204,205,208,211,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,237,238,241,242,243,268,283,284,301,312,313,314,315,316,330,331,332,333,334,335,336,337,338,339,551,552
)
and b.nzp_role = 14;


--Заполнение роли Паспортистка-Специфика-Самара
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 923 and r.nzp_role = 14
and r.nzp_page in (51,64,98,133,134,135,136,169,170,173,177,178,194);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 923 and r.nzp_role = 14
and (
        (r.nzp_page in (51,64,98,169,170,173,177,178,194)) or
		(r.nzp_page in (133,134,135,136) and r.nzp_act in (69,610)) or
        nzp_act in (201,202,203,204,205,214,215,216,218,219,220,221,222,223,224,225,226,227,228,237,238,241,242,243,268,283,284,301,312,313,316) or
        nzp_act in (74,75,854,863)
);


--Заполнение роли Паспортистка-Специфика-Казань-НСав
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 962 and r.nzp_role = 14
and r.nzp_page in (51,64,98,133,135,136,169,170,173,177,178,194);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 962 and r.nzp_role = 14
and (
        (r.nzp_page in (51,64,98,169,170,173,177,178,194)) or
	(r.nzp_page in (133,134,135,136) and r.nzp_act in (69,610)) or
        nzp_act in (202,211,217,218,219,223,224,225,227,237,241,283,313,316,330,331,332,333,334,335,336) or
        nzp_act in (863)
);

--Заполнение роли Паспортистка-Специфика-Зеленодольск
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 925 and r.nzp_role = 14
and nzp_page in (133,135,136);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 925 and r.nzp_role = 14
and (
        (r.nzp_page in (133,134,135,136)  and r.nzp_act in (69,610)) or
        r.nzp_act in (208,211,217,218,219,223,224,225,227,237,241,283,313,330,331,333,334)
);

--Заполнение роли Паспортистка-Специфика-Губкин
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 954 and r.nzp_role = 14
and r.nzp_page in (51,64,98,133,135,136,169,170,173,177,178,194);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 954 and r.nzp_role = 14
and (
        r.nzp_page in (51,64,98,169,170,173,177,178,194) or
	(r.nzp_page in (133,135,136) and r.nzp_act in (69,610)) or
        nzp_act in (201,202,203,204,205,208,211,214,215,216,218,219,223,224,225,226,227,237,238,241,283,284,312,313,314,315,316,337,338,339) or
        nzp_act in (863)
);

--удаление из Паспортистки специфики

delete from role_pages where nzp_role = 14
and nzp_page in (51,64,98,133,135,136,169,170,173,177,178,194);

delete from role_actions where nzp_role = 14
and (
        nzp_page in (51,64,98,133,134,135,136,169,170,173,177,178,194) or
        nzp_act in (74,75,854,863)
);


--Добавление страниц и действий к роли Паспортистка-Изменение
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (196,242)
and b.nzp_role = 904;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select nzp_role, cur_page,  nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (51,56,64,91,98,162,163,164,169,170,173,177,178,194,196,211,212,213,214,215,216,217,218,219,242)
and a.nzp_act in (4,16,17,23,26,61,78,79,82,83,611)
and b.nzp_role = 904;

--Заполнение роли Паспортистка-Изменение-Специфика-Самара
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 929 and r.nzp_role = 904
and r.nzp_page in (51,64,98,169,170,173,177,178,194);

--Заполнение роли Паспортистка-Изменение-Специфика-Казань-НСав
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 964 and r.nzp_role = 904
and r.nzp_page in (51,64,98,169,170,173,177,178,194);

--удаление из Паспортистка-Изменение специфики
delete from role_actions where nzp_role = 904
and nzp_page in (51,64,98,169,170,173,177,178,194);


--Добавление страниц в подсистему Финансы 
insert into role_pages (nzp_role, nzp_page, sign) values (15,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,30,70,73,75,76,121,999,201,202,203,204,206,232,235,236,237,253,257,258,265,270,290,304
,269 -- в разработке
)
and b.nzp_role = 15;

insert into role_actions(nzp_role, nzp_page,nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,73,121,201,202,203,204,206,232,235,236,253,257,258,265,290,304
,269 -- в разработке
) 
and a.nzp_act in (1,2,3,5,8,69,124,127,128,520,521,522,605,606,610,701,702,703,705,285,286,305
,287 -- в разработке
)
and b.nzp_role = 15;


--Добавление страниц и действий к роли Финансы-Изменение
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (200,311,312)
and b.nzp_role = 915;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (200,201,202,203,204,232,233,234,253,257,258,265,304,311,312,340
,269 -- в разработке
)
and a.nzp_act in (4,26,61,85,86,87,88,90,91,92,93,94,105,106,107,108,128,114,115,116,118,120,170,171,181,182,544,548,549,554,611,876,124
,133,134 -- в разработке
)
and b.nzp_role = 915;


--Заполнение роли Финансы-Справочники (Редактирование)
insert into role_actions(nzp_role, nzp_page,nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 942 and r.nzp_role = 915
and r.nzp_page in (257);


--удаление из Финансы-Изменение специфики
 delete from role_actions where nzp_role = 915
and (
        nzp_page in (256,257) 
);


--Добавление страниц в подсистему Картотека домов
insert into role_pages (nzp_role, nzp_page, sign) values (16,0,'');

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,30,31,32,33,35,37,40,42,48,61,63,67,68,69,70,92,93,94,96,99,112,126,134,206,999) 
and b.nzp_role = 16;


insert into role_actions(nzp_role, nzp_page,nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where (a.cur_page in (5,57,61,63,67,68,69,92,93,94,96,99,112,126,134,206)
or (a.cur_page = 31 and a.nzp_act in (1,2,502,503,505,507))
or (a.cur_page = 32 and a.nzp_act in (1,2,501,503,505,507))
or (a.cur_page = 33 and a.nzp_act in (1,2,501,502,505,507))
or (a.cur_page = 35 and a.nzp_act in (1,2,501))
or (a.cur_page = 37 and a.nzp_act in (1,2,501,502,503,505))
or (a.cur_page = 42 and a.nzp_act in (5,603,604,610,701,702,703,705,704))
)
and a.nzp_act in (1,2,3,5,8,14,20,65,66,69,128,206,207,209,210,212,213,244,245,246,501,502,505,507,520,521,522,523,528,601,602,603,604,605,606,607,610,701,702,703,705,704,851,852)
and b.nzp_role = 16;


--Добавление страниц в подсистему Учет претензий 
insert into role_pages (nzp_role, nzp_page, sign) values (19,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,30,31,39,40,41,55,70,73,75,77,78,122,138,139,189,193,197,205,207,208,209,122,220,238,245,246,247,248,249,251,252,267,282,286,289,297,298
,254,255 -- в разработке
)
and b.nzp_role = 19;


insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,30,31,39,40,41,55,122,138,139,189,193,197,205,207,208,209,220,238,245,246,247,248,249,251,252,267,282,286,289,297,298
,254,255 -- в разработке
)
and (case when a.cur_page = 41 then case when a.nzp_act in (5,610,601,602,701,702,703,705) then 1 else 0 end else 1 end) = 1
and a.nzp_act in (1,2,4,5,8,26,61,66,69,81,84,94,97,98,99,100,101,102,130,135,136, 150,151,152, 159,160,165,168, 173, 263,264,265,273,274,275,276,277,278,279,280,281,282,296, 501,509,512, 520,521,522,523,528,601,602,607,610,611,701,702,703,705
,117 -- в разработке
)
and b.nzp_role = 19;


--Добавление страниц и действий к роли Администратор УПГ
			   
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 919 and r.nzp_role = 19
and (
        r.nzp_page in (55,122,207,208,209,220,245,247,248,249,251,252,267,298) or
        ( r.nzp_page in (70,77,40,55,122,207,208,209,220,245,247,248,249,251,252,267,298)) 
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 919 and r.nzp_role = 19
and (
        r.nzp_page in (55,122,207,208,209,220,245,247,248,249,251,267,298) or
        (r.nzp_page in (122,189,193,197,205,247,252,298) and r.nzp_act in (4,61,81,94,97,98,100,101,130,135,136,150,151,152,165,173, 520,521,522,523,528,611)) or
        (r.nzp_act = 512)
);

--Добавление страниц и действий к роли Рассылка сообщений (УПГ)
			   
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 949 and r.nzp_role = 19
and r.nzp_page in (282,286,289);

-- Удаление рассылки сообщений (152) оставить администратору
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 949 and r.nzp_role = 19
and (
(r.nzp_page in (282,286,289)) or
(r.nzp_act = 512)
);

--Добавление страниц и действий к роли Выгрузка недопоставок (УПГ)

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 950 and r.nzp_role = 19
and (
        r.nzp_page in (220) or
        ( r.nzp_page in (77,220)) 
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 950 and r.nzp_role = 19
and (
        (r.nzp_page in (220) and r.nzp_act in (168)) or
        (r.nzp_act = 512)
);

--Добавление страниц и действий к роли Обновление адресов (УПГ)

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 951 and r.nzp_role = 19
and (
        r.nzp_page in (297) or
        ( r.nzp_page in (77,297)) 
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 951 and r.nzp_role = 19
and (
        (r.nzp_page in (297) and r.nzp_act in (173)) or
        ( r.nzp_act = 512)
);

--Добавление страниц и действий к роли Оператор УПГ

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 934 and r.nzp_role = 19
and (
        r.nzp_page in (55,122,207,209,245,251,267) or
        ( r.nzp_page in (40,55,70,77,122,207,209,245,251,267)) 
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 934 and r.nzp_role = 19
and (
        r.nzp_page in (55,122,207,209,245,251,267) or
        (r.nzp_page in (122,189,193,197) and r.nzp_act in (4,61,81,94,98,130,135,136,165,520,521,522,523,528 ,611)) or
         (r.nzp_act = 512)
);

--Добавление страниц и действий к роли Диспетчер УПГ

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,r.nzp_page,''
from role_pages r, s_roles b
where b.nzp_role = 935 and r.nzp_role = 19
and (
        r.nzp_page in (55,122,207,208,209,245,247,251,267) or
        ( r.nzp_page in (70,77,40,55,122,207,208,209,245,247,251,267)) 
);

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 935 and r.nzp_role = 19
and (
        r.nzp_page in (55,122,207,208,209,245,247,251,267) or
        (r.nzp_page in (122,189,193,197,205,247) and r.nzp_act in (4,61,81,94,97,98,100,101,130,135,136,165, 520,521,522,523,528 ,611)) or
        ( r.nzp_act = 512)
);

--удаление из Учета претензий специфики изменения
delete from role_pages where nzp_role = 19
and (
        nzp_page in (55,122,207,208,209,220,247,248,249,251,252,267,282,286,289,297,298) or
        ( nzp_page in (70,77,40,55,122,207,208,209,220,248,249,251,252,267,282,286,289,297,298)) 
);

delete from role_actions where nzp_role = 19
and (
        nzp_page in (55,122,207,208,209,220,247,248,249,251,252,267,282,286,289,297,298) or
        (nzp_page in (122,189,193,197,205,247,252,282,286,289,297,298) and nzp_act in (4,61,81,94,97,98,100,101,130,135,136,150,151,152,165, 173, 520,521,522,523,528 ,611)) or
        ( nzp_act = 512)
);

--Добавление страниц в подсистему Касса
insert into role_pages (nzp_role, nzp_page, sign) values (20,0,'');

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,30,31,40,41,76,999,198,199,203,235)
and b.nzp_role = 20;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role, cur_page, nzp_act, '', cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,2,3,4,5,31,41,999,198,199,203)
and a.nzp_act in (1,2,3,5,601,602,610,701,702,703,705)
and b.nzp_role = 20;            


--Добавление страниц и действий к роли Касса - Изменение

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (198,199,203) 
and a.nzp_act in (61,85,86,87,89,90,91,611)
and b.nzp_role = 920;


--Добавление страниц в подсистему Аналитический центр
insert into role_pages (nzp_role, nzp_page, sign) values (21,0,'');

insert into role_pages(nzp_role, nzp_page,  sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,30,31,40,41,42,226,999)
and b.nzp_role = 21;           

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,2,3,4,5,31,41,42,226,999)
and a.nzp_act in (1,2,3,5,501,601,602,610,701,702,703,705)
and b.nzp_role = 21;  

--Добавление страниц в подсистему Финансирование дотаций
insert into role_pages (nzp_role, nzp_page, sign) values (22,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,30,44,72,73,232,256,257,272,273,274,275,276,277,278,279,280,283,285,287,288,290,291)
and b.nzp_role = 22;              

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,2,5,944,999,30,72,73,232,256,257,272,273,274,275,276,277,278,279,280,283,285,287,288,290,291)
and a.nzp_act in (1,2,3,4,5,26,61,140,141,142,143,144,145,146,147,148,149,153,154,155,156,157,158,161,162,610,611)
and b.nzp_role = 22;  

--Добавление страниц и действий к роли Финансирование дотаций - Изменение
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct b.nzp_role,r.nzp_page,r.nzp_act,'',r.mod_act
from role_actions r, s_roles b
where b.nzp_role = 944 and r.nzp_role = 22
and (
        (r.nzp_page in (232,256,257,272,273,274,277,278,279,280,283,287,288)  and r.nzp_act in (4,26,61,140,141,142,145,146,147,148,149,154,155,156,157,158,611))
);


--удаление из Финансирование дотаций функций редактирования
delete from role_actions where nzp_role = 22
and (
        (nzp_page in (232,233,256,257,272,273,274,277,278,279,280,283,287,288) and nzp_act in (4,26,61,140,141,142,145,146,147,148,149,154,155,156,157,158,611))
);


--Добавление страниц в подсистему Расчет дотаций
insert into role_pages (nzp_role, nzp_page, sign) values (23,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,999,30,31,32,35,38,40,41,42,44,45,48,49,50,51,52,53,54,59,62,64,68,69,70,71,72,73,93,94,95,96,97,98,99,106,111,134,137,163,164,165,166,169,170,171,172,173,174,175,176,187,188,190,195,210,221,222,223,224,225,227,228,229,230,231,261,281,284
,232,256,257,276,279,280,285,287,288,290,291
) 
and b.nzp_role = 23;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where (a.cur_page in (5,44,45,49,50,51,52,53,54,59,62,64,68,69,93,94,95,96,97,98,99,106,111,134,137,163,164,165,166,169,170,171,172,173,174,175,176,187,188,190,210,221,222,223,224,225,227,228,229,230,231,261,281,284
,232,256,257,279,280,285,287,288,290
)
or (a.cur_page = 31 and a.nzp_act in (1,2,502,503,505,506,508,510))
or (a.cur_page = 32 and a.nzp_act in (1,2,501,503,505,506,508,510))
or (a.cur_page = 35 and a.nzp_act in (1,2,501))
or (a.cur_page = 38 and a.nzp_act in (1,2,501,502,503,505,506,510))
or (a.cur_page = 41 and a.nzp_act in (5,73,601,602,610,701,702,703,705,704))
or (a.cur_page = 42 and a.nzp_act in (5,603,604,610,701,702,703,705,704))
or (a.cur_page = 195 and a.nzp_act in (1,2,501,502,503,505,506,508))
)
and a.nzp_act in (1,2,3,5,8,14,20,66,69,71,76,77,80,103,104,128,143
,206,207,209,210,212,213,228,229,230,231,232,233,234,235,236,239,240,244,245,246,247,248,249,250,251,253,254,255,257,258,259,260,261,266,267,269,270,271,272,294
,256 -- в разработке
,288,289,290 -- в разработке
,501,502,505,506,508,510,520,521,522,523,528,601,602,603,604,605,606,607,610,701,702,703,705,851,852,862)
and b.nzp_role = 23;

--Добавление страниц и действий к роли Расчет дотаций (Редактирование)

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where ((a.cur_page in (1,2,3,4,5,999,30,31,32,35,38,40,41,42,44,45,48,49,50,51,53,54,59,62,64,68,69,70,71,73,74,93,94,95,96,97,98,99,100,101,102,103,104,105,106,108,109,111,122,163,164,165,166,169,170,171,172,173,174,175,176,179,180,181,182,183,187,188,190,195,196,210,221,222,223,224,225,227,228,229,230,231,242,243,244,261,262,263,264,281,284))
or (a.cur_page in (1,5,31,32,35,38,41,42,44,45,49,50,51,52,53,54,59,62,64,68,69,93,94,95,96,97,98,99,106,111,112,122,163,164,165,166,169,170,171,172,173,174,175,176,179,180,181,182,183,187,188,190,195,196,210,221,222,223,224,225,227,228,229,230,231,242,243,244,261,262,263,264,281,284
,232,256,257,276,279,280,285,287,288,290,291
)))
and b.nzp_role = 945;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where (a.cur_page in (100,101,102,103,104,105,108,109,179,180,181,182,183,196,262,263,264)
or (a.cur_page in (5,31,32,35,38,41,42,44,45,49,50,51,52,53,54,59,62,64,68,69,93,94,95,96,97,98,99,106,111,122,163,164,165,166,169,170,171,172,173,174,175,176,187,188,190,195,196,210,221,222,223,224,225,227,228,229,230,231,242,243,244,261,262,263,264,281,284
,232,256,257,279,280,285,287,288,290
)
    and a.nzp_act in (4,9,13,15,16,17,18,19,21,22,24,25,26,61,64,72,109,110,111,128,145,146,153,154,155,156,157,158,161,162,611,123)))
and b.nzp_role = 945;


--Заполнение роли "Расчет начислений"

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.nzp_act in (70,164) and b.nzp_role = 921;


--Добавление страниц в подсистему Субсидии ФЛ
insert into role_pages (nzp_role, nzp_page, sign)
select distinct nzp_role, page_url, '' from s_roles where nzp_role = 24;

insert into role_actions (nzp_role, nzp_page, nzp_act, sign, mod_act) 
select distinct nzp_role, page_url,0, '', cast(null as integer) from s_roles where nzp_role = 24;

--Добавление страниц в подсистему Картотека (Внешние БД)

insert into role_pages (nzp_role, nzp_page,sign) 
select distinct nzp_role, 0, '' from s_roles where nzp_role = 25;

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,15,999,30,31,41,42,98,99,108,111) 
and b.nzp_role = 25;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where ((a.cur_page in (1,2,3,4,5,15,999,30,99,108))
or (a.cur_page = 31 and a.nzp_act in (1,2))
or (a.cur_page = 41 and a.nzp_act in (5,73,601,602,610,701,702,703,705,704))
or (a.cur_page = 42 and a.nzp_act in (5,603,604,610,701,702,703,704,705))
or (a.cur_page = 98 and a.nzp_act in (610))
or (a.cur_page = 111 and a.nzp_act in (610))
) and b.nzp_role = 25;

-- Назначение для роли Картотека (Внешние БД) банка данных "Внешние"
insert into roleskey ( nzp_role, tip, kod, sign) 
select distinct 25, 101, 99, '' from s_roles where nzp_role=25;


--Добавление страниц в подсистему Настройка системы
insert into role_pages (nzp_role, nzp_page, sign) values (26,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,5,306) 
and b.nzp_role = 26;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (1,5,306)
and a.nzp_act in (170)
and b.nzp_role = 26;


--Добавление страниц в подсистему Обмен данными
insert into role_pages (nzp_role, nzp_page,sign) values (27,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (5)
and b.nzp_role = 27;


--Добавление страниц в подсистему Должники
insert into role_pages (nzp_role, nzp_page, sign) values (28,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (5,31,75,206,322,323,324,325,326,327,328,329,332) 
and b.nzp_role = 28;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (5,31,75,206,322,323,324,325,326,327,328,329,332)
and a.nzp_act in (1,2,3,5,26,61,69,158,170,174,192,193,194,197,198,199,200, 343,344,345,347 ,501,513,514,540,610,611)
and b.nzp_role = 28;


--Добавление страниц в подсистему Отчеты
insert into role_pages (nzp_role, nzp_page, sign) values (30,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (5,333)
and b.nzp_role = 30;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (5,333)
and a.nzp_act in (5,69,610)
and b.nzp_role = 30;


--Добавление страниц в подсистему Должники - Настройки
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (330)
and b.nzp_role = 982;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (330)
and a.nzp_act in (61,610,611)
and b.nzp_role = 982;


--Добавление страниц Взаимодействие с банком (Астрахань)
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (334)
and b.nzp_role = 983;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (334)
and a.nzp_act in (158,163,541)
and b.nzp_role = 983;


--Добавление страниц в Льготы (Просмотр)
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (313) 
and b.nzp_role = 957;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (313)
and a.nzp_act in (0)
and b.nzp_role = 957;

--Добавление страниц в Обмен данными (Губкин)
insert into role_pages( nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (305) 
and b.nzp_role = 959;

--Добавление страниц в Загрузка наследуемой информации
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (293,315) 
and b.nzp_role = 960;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (293,315)
and a.nzp_act in (158,163,166,329,870)
and b.nzp_role = 960;

--Роль "Просмотр ИПУ"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (54,62,68,94)
and b.nzp_role = 966;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (54,62,68,94)
and a.nzp_act in (3,5,14,71,72,111,112,175,610)
and b.nzp_role = 966;

--Роль "Редактирование ИПУ"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (264)
and b.nzp_role = 967;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (54,62,68,94,264)
and a.nzp_act in (4,61,64,123,611)
and b.nzp_role = 967;

--Роль "Просмотр ОКПУ"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (185,186,187,294)
and b.nzp_role = 968;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (185,186,187,294)
and a.nzp_act in (3,5,14,71,72,111,112,175,610)
and b.nzp_role = 968;

--Роль "Редактирование ОКПУ"
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (185,186,187,294)
and a.nzp_act in (4,61,64,611)
and b.nzp_role = 969;

--Роль "Просмотр ОДПУ"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (61,63,69,294)
and b.nzp_role = 970;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (61,63,69,294)
and a.nzp_act in (3,5,14,71,72,111,112,610)
and b.nzp_role = 970;

--Роль "Редактирование ОДПУ"
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (61,63,69,294)
and a.nzp_act in (4,61,64,611)
and b.nzp_role = 971;

--Роль "Просмотр ГПУ"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (66,67,92,93,294)
and b.nzp_role = 972;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (66,67,92,93,294)
and a.nzp_act in (3,5,14,71,72,111,112,610)
and b.nzp_role = 972;

--Роль "Редактирование ГПУ"
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (66,67,92,93,294)
and a.nzp_act in (4,61,64,611)
and b.nzp_role = 973;

--Роль "Генерация лицевых счетов"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (260)
and b.nzp_role = 974;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where ((a.cur_page in (260) and a.nzp_act in (122,611))
    or (a.cur_page in (42) and a.nzp_act in (122)))
and b.nzp_role = 974;

--Роль "Генерация приборов учета"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (264)
and b.nzp_role = 975;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (264)
and a.nzp_act in (123,611)
and b.nzp_role = 975;

--Роль "Взаимодействие с соц. защитой" (Тула)
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (317)
and b.nzp_role = 976;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (317)
and a.nzp_act in (611,188,158)
and b.nzp_role = 976;

--Роль "Реестр для "Небо" 
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (318)
and b.nzp_role = 977;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (318)
and a.nzp_act in (611,191,195)
and b.nzp_role = 977;

--Роль "Выгрузка в банк-клиент"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (310,314)
and b.nzp_role = 978;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (310,314)
and a.nzp_act in (170,185,186,189)
and b.nzp_role = 978;

--Роль "Настройка формата банк-клиент"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (319)
and b.nzp_role = 979;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (319)
and a.nzp_act in (61,158,169,610,611)
and b.nzp_role = 979;

--Роль "Настройка уникальных кодов"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (320)
and b.nzp_role = 980;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (320)
and a.nzp_act in (1,158,169,170,196)
and b.nzp_role = 980;

--Роль "Взаимодействие с внешними биллинговыми системами"
insert into role_pages(nzp_role, nzp_page,sign) select distinct b.nzp_role,a.cur_page,'' from pages_show a, s_roles b
where a.cur_page in (321)
and b.nzp_role = 981;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act) select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer) from actions_show a, s_roles b
where a.cur_page in (321)
and a.nzp_act in (188,158)
and b.nzp_role = 981;

--Добавление страниц Быстрый ввод показаний ПУ
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (335)
and b.nzp_role = 984;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (335)
and a.nzp_act in (170,601,602)
and b.nzp_role = 984;

--Загрузка оплат от ВТБ24
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (336)
and b.nzp_role = 985;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (336)
and a.nzp_act in (85,158)
and b.nzp_role = 985;


--Управление связанными услугами
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (339)
and b.nzp_role = 986;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (339)
and a.nzp_act in (158,169,170)
and b.nzp_role = 986;

--Сальдо по перечислениям по домам
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (340)
and b.nzp_role = 987;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (340)
and a.nzp_act in (1,107,875,610,701,702,703,705)
and b.nzp_role = 987;

--Управление очередями
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (343)
and b.nzp_role = 988;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (343)
and a.nzp_act in (546, 547)
and b.nzp_role = 988;

--Исправление ошибок в распределении оплат
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (265,201)
and a.nzp_act in (125,126,550)
and b.nzp_role = 989;

--Закрытие месяца(подготовка данных для печати счетов)
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (342)
and b.nzp_role = 990;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (342)
and a.nzp_act in (5,545)
and b.nzp_role = 990;

--Генерация платежных кодов
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (41)
and a.nzp_act in (871)
and b.nzp_role = 991;

--Справочник параметров
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (349)
and b.nzp_role = 993;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (349)
and a.nzp_act in (158,169,170)
and b.nzp_role = 993;

--Смена управляющей организации
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (309)
and b.nzp_role = 994;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (309)
and a.nzp_act in (184)
and b.nzp_role = 994;

--Просмотр процентов удержания по домам
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (350)
and b.nzp_role = 995;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (350)
and a.nzp_act in (2,5,610,701,702,703,705)
and b.nzp_role = 995;

--Редактирование процентов удержания по домам
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (350)
and a.nzp_act in (4,26,61,611)
and b.nzp_role = 996;

--Взаимодействие с Банком
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (308)
and b.nzp_role = 998;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (308)
and a.nzp_act in (158,169,170)
and b.nzp_role = 998;

--Сальдо по перечислениям по домам по договорам
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (353,356,359)
and b.nzp_role = 999;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (353,356,359)
and a.nzp_act in (1,107,610,701,702,703,705)
and b.nzp_role = 999;

--Просмотр процентов удержания по домам
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (354)
and b.nzp_role = 800;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (354)
and a.nzp_act in (2,5,610,701,702,703,705)
and b.nzp_role = 800;

--Редактирование процентов удержания по домам по договорам
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (354)
and a.nzp_act in (4,26,61,611)
and b.nzp_role = 801;

--Просмотр договоров на оказание ЖКУ
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (174,175,352,358)
and b.nzp_role = 802;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (174,175,352,358)
and a.nzp_act in (5,20,610,103)
and b.nzp_role = 802;

--Редактирование договоров на оказание ЖКУ
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (352,358)
and a.nzp_act in (158,169,170)
and b.nzp_role = 803;

--Просмотр контрагентов
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (355)
and b.nzp_role = 804;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (355)
and a.nzp_act in (5,610)
and b.nzp_role = 804;

--Редактирование контрагентов
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (355)
and a.nzp_act in (4,61,611)
and b.nzp_role = 805;

--Редактирование сальдо по перечислениям по договорам
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (353,356,358,359)
and a.nzp_act in (4,26,61,108,170,611,875)
and b.nzp_role = 806;


--Роль Финансы-Специфика-Без договоров
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (233,234,256,259)
and b.nzp_role = 807;

insert into role_actions(nzp_role, nzp_page,nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (233,234,256,259) 
and a.nzp_act in (1,5,610)
and b.nzp_role = 807;


--Роль Финансы-Специфика-Редактирование-Без договоров
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (233,234,256,259)
and a.nzp_act in (4,26,61,611)
and b.nzp_role = 808;


--Роль Просмотр управляющих организаций
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (360,227,228)
and b.nzp_role = 809;

insert into role_actions(nzp_role, nzp_page,nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (360,227,228)
and a.nzp_act in (5,20,103,104,610)
and b.nzp_role = 809;


--Роль Редактирование управляющих организаций
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (360)
and a.nzp_act in (16,17,61,158,169,170,611)
and b.nzp_role = 810;

--Редактирование параметров договоров
insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (174)
and a.nzp_act in (16,17,611)
and b.nzp_role = 812;

--Добавление страниц в подсистему Сотрудник абонентского отдела
insert into role_pages (nzp_role, nzp_page,sign) values (31,0,'');

insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (1,2,3,4,5,15,999,30,31,32,33,35,36,37,38,41,42,45,49,50,51,53,54,62,64,66,68,69,70,71,73,74,75,93,94,95,96,98,100,101,102,
103,104,105,108,109,110,111,122,123,
131,133,134,162,163,164,165,166,168,169,170,171,172,173,174,175,176,179,180,181,182,183,187,188,195,196,206,210,221,222,223,224,225,227,228,229,
230,231,261,242,243,260,264,294,295,299,296,300,301,302,308,316,303,337,338,341
,6,226,323,345,346,347 -- в разработке
,263,309 -- для показа
) 
and b.nzp_role = 31;


insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where (a.cur_page in (5,45,49,50,51,53,54,55,62,64,66,68,69,93,94,95,96,98,111,122,123,131,133,134,162,163,164,165,166,168,169,170,171,
172,173,174,175,176,187,188,206,210,221,222,223,224,225,227,228,229,230,231,261,294,296,300,301,303,306,337,338,341
,74,100,101,102,103,104,105,108,109,110,179,180,181,182,183,196,242,243,260,264,295,299,302,308,316
,6,226,321,323,345,346,347 -- в разработке
,263,309 -- для пока
)
or (a.cur_page = 31 and a.nzp_act in (1,2,502,503,505,506,507,508,510))
or (a.cur_page = 32 and a.nzp_act in (1,2,501,503,505,506,507,508,510))
or (a.cur_page = 33 and a.nzp_act in (1,2,501,502,505,506,507,508,510))
or (a.cur_page = 35 and a.nzp_act in (1,2,501))
or (a.cur_page = 36 and a.nzp_act in (1,2,501,502,503,505,507,508,510))
or (a.cur_page = 37 and a.nzp_act in (1,2,501,502,503,505,506,508,510))
or (a.cur_page = 38 and a.nzp_act in (1,2,501,502,503,505,506,507,510))
or (a.cur_page = 41 and a.nzp_act in (5,73,601,602,610,701,702,703,705,704))
or (a.cur_page = 42 and a.nzp_act in (5,603,604,610,701,702,703,705,704))
or (a.cur_page = 195 and a.nzp_act in (1,2,501,502,503,505,506,507,508))
)
and a.nzp_act in (1,2,3,5,8,14,20,66,67,69,71,72,76,77,80,103,104,128,175,180
,206,207,209,210,212,213,228,229,230,231,232,233,234,235,236,239,240,244,245,246,247,248,249,250,251,253,254,255,257,258,259,260,261,266,267,269,270,271,272,294,295,300,302,303,304,306,307,308,309,310,311,317,318,319,320,321,324,325,327,340,341,342,346,347,348,349,350,351
,256,288,289,290,299,323,326,553 -- в разработке
,501,502,503,505,506,507,508,510,520,521,522,523,528,542,601,602,603,604,605,606,607,610,701,702,703,705,851,852,862
,4,9,13,15,16,17,18,19,21,22,24,25,26,61,64,109,110,111,112,158,169,170,172,176,178,179,190,543,611
)
and b.nzp_role = 31;


--Добавление страниц в подсистему Сотрудник абонентского отдела (Редактирование)
insert into role_pages(nzp_role, nzp_page,sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (74,100,101,102,103,104,105,108,109,110,179,180,181,182,183,196,242,243,262,260,264,295,299,302,316
,263,346 -- для показа
)
and b.nzp_role = 997;


insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where (a.cur_page in (100,101,102,103,104,105,108,109,110,179,180,181,182,183,196,262,295,299,302,316
,263,323,346 -- для показа
)
or (a.cur_page in (5,31,32,33,35,36,37,38,41,42,45,49,50,51,53,54,55,59,61,62,63,64,66,68,69,93,94,95,96,97,98,99,111,121,122,123,126,131,133,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,184,185,187,188,190,191,192,195,196,210,221,222,223,224,225,227,228,229,230,231,240,242,243,261,294,295,296,300,301,303
,6,244,323,346 -- в разработке
)
and a.nzp_act in (4,9,13,15,16,17,18,19,21,22,24,25,26,61,64,109,110,111,112,128,158,169,170,172,176,178,179,190,543,611
,323 -- для показа
))
)
and b.nzp_role = 997;

delete from role_actions where nzp_role = 997 and nzp_page = 162 and nzp_act = 4;


--Роль Просмотр статистики по начислениям
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (361,362)
and b.nzp_role = 811;

insert into role_actions(nzp_role, nzp_page,nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (361,362)
and a.nzp_act in (1)
and b.nzp_role = 811;


--Роль Изменение адреса лицевых счетов
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (363)
and b.nzp_role = 813;

insert into role_actions(nzp_role, nzp_page,nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (363)
and a.nzp_act in (170)
and b.nzp_role = 813;


--Роль Изменение адреса лицевых счетов
insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (268)
and b.nzp_role = 814;

insert into role_actions(nzp_role, nzp_page,nzp_act, sign, mod_act)
select nzp_role, cur_page, nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (268)
and a.nzp_act in (131,611)
and b.nzp_role = 814;


update role_actions set mod_act = 611 where  nzp_act in (4,10,11,12,15,16,17,18,19,21,22,23,24,25,26,61,64,68,79,81,82,83,85,86,87,89,94,96,97,98,113,130,135,136,138,139,140,146,150,151,152,165,871);
update role_actions set mod_act = null where nzp_page in (201,311,312,336) and nzp_act in (85,86,87);
update role_actions set mod_act = null where  nzp_act in (9,13);

--Удаление ролей, страниц и действий, не входящих в конфигурацию системы
delete from role_pages where nzp_role not in (select nzp_role from config where sign = nzp_role||'-'||nzp_config||'config');
delete from role_actions where nzp_role not in (select nzp_role from config where sign = nzp_role||'-'||nzp_config||'config');
delete from roleskey where nzp_role < 1000 and nzp_role not in (select nzp_role from config where sign = nzp_role||'-'||nzp_config||'config');
delete from roleskey where tip = 105 and kod < 1000 and kod not in (select nzp_role from config where sign = nzp_role||'-'||nzp_config||'config');
delete from s_roles where nzp_role < 1000 and nzp_role not in (select nzp_role from config where sign = nzp_role||'-'||nzp_config||'config');
delete from actions_show where cur_page||'-'||nzp_act not in (select nzp_page||'-'||nzp_act from role_actions );
delete from pages_show where cur_page not in ( select nzp_page from role_pages )  and  page_url not in (select nzp_page from role_pages);
delete from s_actions where nzp_act not in (select nzp_act from actions_show);
delete from pages where not exists (select * from pages_show where cur_page = nzp_page or page_url = nzp_page);
delete from report where nzp_act not in (select nzp_act from s_actions);


--Добавление функционала вспомогательных ролей к основным ролям и удаление вспомогательных ролей
delete from role_pages where nzp_role in (922,924,926,947,953,955,958,961) and nzp_page in (select nzp_page from role_pages where nzp_role = 10);
delete from role_pages where nzp_role in (930,931,938) and nzp_page in (select nzp_page from role_pages where nzp_role = 11);
delete from role_pages where nzp_role in (932,943,959,980) and nzp_page in (select nzp_page from role_pages where nzp_role = 12);
delete from role_pages where nzp_role in (941) and nzp_page in (select nzp_page from role_pages where nzp_role = 13);
delete from role_pages where nzp_role in (923,925,954,962) and nzp_page in (select nzp_page from role_pages where nzp_role = 14);
delete from role_pages where nzp_role in (927,928,948,963,965) and nzp_page in (select nzp_page from role_pages where nzp_role = 901);
delete from role_pages where nzp_role in (939,940) and nzp_page in (select nzp_page from role_pages where nzp_role = 903);
delete from role_pages where nzp_role in (929,964) and nzp_page in (select nzp_page from role_pages where nzp_role = 904);

insert into roleskey (nzp_role, tip, kod) select a.nzp_role_to, b.tip, b.kod from t_role_merging a, roleskey b, s_roles c where a.nzp_role_from = b.nzp_role and c.nzp_role = a.nzp_role_to;
insert into role_pages (nzp_role, nzp_page) select a.nzp_role_to, b.nzp_page from t_role_merging a, role_pages b, s_roles c where a.nzp_role_from = b.nzp_role and c.nzp_role = a.nzp_role_to;
insert into role_actions (nzp_role, nzp_page, nzp_act, mod_act) select a.nzp_role_to, b.nzp_page, b.nzp_act, b.mod_act from t_role_merging a, role_actions b, s_roles c where a.nzp_role_from = b.nzp_role and c.nzp_role = a.nzp_role_to;

delete from role_actions where nzp_role in (select nzp_role_from from t_role_merging);
delete from role_pages where nzp_role in (select nzp_role_from from t_role_merging);
delete from roleskey where nzp_role in (select nzp_role_from from t_role_merging);
delete from s_roles where nzp_role in (select nzp_role_from from t_role_merging);
drop table t_role_merging;

--Шифрование данных
update pages_show set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'pages_show';
update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show';

update roleskey set sign = tip::varchar||kod::varchar||nzp_role::varchar||'-'||nzp_rlsv::varchar||'roles' 
where (nzp_role >= 10 and nzp_role < 1000) or (tip = 105 and kod >= 10 and kod < 1000);

update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages'
where nzp_role >= 10 and nzp_role < 1000;

update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions'
where nzp_role >= 10 and nzp_role < 1000;

delete from s_roles where nzp_role = 1000;
insert into s_roles (nzp_role,role,page_url,sort) values (1000, 'Фиктивная роль! Можно удалить!', 0,1000);
delete from s_roles where nzp_role = 1000;

drop index if exists ix_lacc_3;
create index ix_lacc_3 on log_access(idses, acc_kod);
analyze log_access;

drop index if exists ix_srlc_2;
create index ix_srlc_2 on s_roles (nzp_role,  page_url);
analyze s_roles;


CREATE TABLE if not exists users
(
  nzp_user serial NOT NULL,
  login character(30) NOT NULL,
  pwd character(200) NOT NULL,
  uname character(200) NOT NULL,
  dat_log timestamp without time zone,
  ip_log character(20),
  browser character(20),
  email character(200),
  is_blocked character(1),
  date_begin date,
  is_remote integer DEFAULT 0,
  nzp_payer integer,
  appointment character(200),
  nzp_bank integer
)
WITH OIDS;

------------------Таблица для отчетов---------------------------------------------
create table if not exists excel_utility
( nzp_exc      serial not null,
  nzp_user     integer not null,
  prms         char(200) not null,             --параметры для запроса
  stats        integer default 0,              --статус выгрузки
  dat_in       timestamp,        --дата начала выгрузки
  dat_start    timestamp,        --дата начала обработки выгрузки
  dat_out      timestamp,        --дата окончания выгрузки
  tip          integer default 0 not null,     --тип выгрузки
  rep_name     char(100),                      --название отчета
  exc_path     char(200),                      --ссылка на выгрузку
  exc_comment  char(200),                      --комментарий к типу выгрузки
  dat_today    date,
  progress     decimal(6,4) default 0,
  is_shared    integer default 0
);

drop index if exists ix_exc_1;
create unique index ix_exc_1 on excel_utility (nzp_exc);

drop index if exists ix_exc_2;
create        index ix_exc_2 on excel_utility (nzp_user);

CREATE TABLE if not exists updater
(
	nzp_up SERIAL PRIMARY KEY,
	dateup timestamp,
	typeUp CHAR(50),
	version double precision,
	status INT,
	path CHAR(200),
	key CHAR(100),
	soup CHAR(20),
	web_path CHAR(80),
	report TEXT
);

INSERT INTO updater (dateup, typeUp, version, status, path) select '1990-01-01 00:00:00', 'web',       '0' , 1, 'Запись по умолчанию' where not exists (select 1 from updater where typeUp='web' and version ='0');
INSERT INTO updater (dateup, typeUp, version, status, path) select '1990-01-01 00:00:00', 'host',      '0' , 1, 'Запись по умолчанию' where not exists (select 1 from updater where typeUp='host' and version ='0');
INSERT INTO updater (dateup, typeUp, version, status, path) select '1990-01-01 00:00:00', 'broker',    '0' , 1, 'Запись по умолчанию' where not exists (select 1 from updater where typeUp='broker' and version ='0');
INSERT INTO updater (dateup, typeUp, version, status, path) select '1990-01-01 00:00:00', 'script',    '0' , 1, 'Запись по умолчанию' where not exists (select 1 from updater where typeUp='script' and version ='0');
INSERT INTO updater (dateup, typeUp, version, status, path) select '1990-01-01 00:00:00', 'updhost',   '0' , 1, 'Запись по умолчанию' where not exists (select 1 from updater where typeUp='updhost' and version ='0');
INSERT INTO updater (dateup, typeUp, version, status, path) select '1990-01-01 00:00:00', 'updbroker', '0' , 1, 'Запись по умолчанию' where not exists (select 1 from updater where typeUp='updbroker' and version ='0');

create table if not exists users_links
(
	nzp_user_link serial not null,
	nzp_user int not null,
	nzp_role int default 0 not null,
	nzp_server int not null,
	login char(200),
	sign char(90)
);

drop index if exists ix_users_links_1;
CREATE UNIQUE INDEX ix_users_links_1 on users_links(nzp_user_link);

drop index if exists ix_users_links_2;
create index ix_users_links_2 on users_links(nzp_user);

CREATE TABLE if not exists pack(
   nzp_pack SERIAL NOT NULL primary key,
   nzp_bank INTEGER not null,
   num_pack integer not null,
   dat_pack DATE,
   count_kv INTEGER,
   sum_pack DECIMAL(14,2),
   flag SMALLINT default 0,
   erc_code CHAR(12),
   nzp_user int not null,
   file_name char(100)
);

drop index if exists ix_pack_1;
create unique index ix_pack_1 on pack(nzp_pack);

drop index if exists ix_pack_2;
create unique index ix_pack_2 on pack(nzp_bank, num_pack);

CREATE TABLE if not exists pack_ls(
   nzp_pack_ls SERIAL NOT NULL,
   nzp_pack INTEGER not null references pack(nzp_pack),
   prefix_ls INTEGER,      -- первые 3 символа плат кода
   nzp_kvar INTEGER,
   pref char(20),
   g_sum_ls DECIMAL(10,2) default 0 not null,
   sum_ls DECIMAL(10,2) default 0 not null,
   sum_peni DECIMAL(10,2) default 0 not null,
   dat_month DATE,
   kod_sum SMALLINT,  -- 33
   paysource INTEGER default 0,  -- 1
   id_bill INTEGER default 0,    -- 0
   dat_vvod DATE,
   info_num INTEGER not null,
   unl INTEGER, -- платеж перегружен / не перегружен - при выгрузке 1, по умолчанию 0   -- -1
   erc_code CHAR(12), --fsmr_kernel:s_erc_code  
   nzp_user INTEGER not null  
);

drop index if exists ix_pack_ls_1;
create unique index ix_pack_ls_1 on pack_ls(nzp_pack_ls);

drop index if exists ix_pack_ls_2;
create unique index ix_pack_ls_2 on pack_ls(nzp_pack, info_num);

create table if not exists bill_fon (
	nzp_key            serial  not null,
	nzp_area           integer default 0 not null, 
	nzp_geu            integer default 0 not null, 
	nzp_wp             integer default 0 not null, 
	year_              integer default 0 not null, 
	month_             integer default 0 not null, 
	kod_info           integer default 0 not null, 
	dat_in             timestamp, 
	dat_work           timestamp,  
	dat_out            timestamp, 
	txt                char(255),
	nzp_user           integer default 0 not null,
	count_list_in_pack integer default 0 not null,
	kod_sum_faktura    integer default 0 not null, 
	result_file_type   char(10),
	id_faktura         integer default 0 not null,
	with_dolg          smallint,
	file_name          char(200),
	progress           decimal(6,4) default 0
);

drop index if exists ix_bill_fon_1;
create unique index ix_bill_fon_1 on bill_fon (nzp_key);

drop index if exists ix_bill_fon_2;
Create        index ix_bill_fon_2 on bill_fon (nzp_key,year_,month_,kod_info);

drop index if exists ix_bill_fon_3;
Create        index ix_bill_fon_3 on bill_fon (nzp_user,kod_info);

create table if not exists saldo_fon(
	nzp_key SERIAL NOT NULL,
	nzp_area INTEGER default 0 NOT NULL,
	year_ INTEGER default 0 NOT NULL,
	month_ INTEGER default 0 NOT NULL,
	kod_info INTEGER default 0 NOT NULL,
	dat_in timestamp,
	dat_work timestamp,
	dat_out timestamp,
	txt CHAR(255));

drop index if exists ix_sfon_1;
create unique index ix_sfon_1 on saldo_fon(nzp_key);

drop index if exists ix_sfon_2;
CREATE        INDEX ix_sfon_2 on saldo_fon(nzp_area, year_, month_, kod_info);

drop index if exists ix_sfon_3;
CREATE        INDEX ix_sfon_3 on saldo_fon(kod_info);

create table if not exists s_setups
(	nzp_setup serial not null,
	nzp_param integer,
	param_name char(250),
	param_type char(50) default 'char',
	value_ char(250),
	nzp_user integer,
	dat_when timestamp 
);

drop index if exists ix_s_setups_1;
create unique index ix_s_setups_1 on s_setups(nzp_setup);

drop index if exists ix_s_setups_2;
create unique index ix_s_setups_2 on s_setups(nzp_param);

drop index if exists ix_s_setups_3;
create unique index ix_s_setups_3 on s_setups(param_name);

insert into s_setups (nzp_param, param_name, param_type, value_, nzp_user, dat_when) 
select 7, 'Работать только с центральным банком данных', 'bool', '2', null, null
where not exists (select 1 from s_setups where nzp_param = 7);

create table if not exists user_processes
(	nzp_key serial not null,
	nzp_user integer,
	table_name char(250),
	procId char(250)
);

drop index if exists ix_user_processes_1;
drop index if exists ix_user_processes_2;
drop index if exists ix_user_processes_3;
create unique index ix_user_processes_1 on user_processes(nzp_key);       
create unique index ix_user_processes_2 on user_processes(procId);

create table if not exists log_sessions
( 	nzp_ses   serial not null,
	nzp_user  integer default 0,
	dat_log   timestamp,
	ip_log    char(20), 
	browser   char(20),
	idses     char(30),
	session_id char(32),
	left_on timestamp
);

drop index if exists ix_lses_1;
drop index if exists ix_lses_2;
drop index if exists ix_lses_3;
create unique index ix_lses_1 on log_sessions (nzp_ses);
create        index ix_lses_2 on log_sessions (nzp_user,dat_log,idses);
create        index ix_lses_3 on log_sessions (idses);

create table if not exists s_roles
( 	nzp_role serial not null,
	role     char(120),
	page_url integer default 0,
	sort     integer default 0,
	is_active integer default 1
);

drop index if exists ix_srls_1;
create unique index ix_srls_1 on s_roles (nzp_role);

create table if not exists s_accesskod
(
	nzp_acc serial not null,
	name char(250)
);

drop index if exists ix_s_accesskod_1;
create unique index ix_s_accesskod_1 on s_accesskod(nzp_acc); 

insert into s_accesskod(nzp_acc,name) select 1, 'Аутентификация прошла успешно' where not exists (select 1 from s_accesskod where nzp_acc = 1);
insert into s_accesskod(nzp_acc,name) select 2, 'Выход из системы' where not exists (select 1 from s_accesskod where nzp_acc = 2);
insert into s_accesskod(nzp_acc,name) select 3, 'Несанкционированный доступ' where not exists (select 1 from s_accesskod where nzp_acc = 3);


create table if not exists calc_fon_0
(	nzp_key   serial  not null, 
	nzp       integer default 0 not null, 
	nzp_user  integer default 0 not null, 
	nzpt      integer default 0 not null, 
	year_     integer default 0 not null, 
	month_    integer default 0 not null, 
	task      integer default 0 not null, 
	prior     integer default 0 not null, 
	kod_info  integer default 0 not null, 
	dat_in    timestamp, 
	dat_work  timestamp,  
	dat_out   timestamp, 
	txt       char(255),
	parameters varchar(255)
);


alter table calc_fon_0 drop if exists progress;
alter table calc_fon_0 add progress numeric(6,4);

drop index if exists ix_calc_fon_0_1;
drop index if exists ix_calc_fon_0_2;
drop index if exists ix_calc_fon_0_3;
drop index if exists ix_calc_fon_0_4;
create unique index ix_calc_fon_0_1 on calc_fon_0(nzp_key);
create        index ix_calc_fon_0_2 on calc_fon_0(nzp,nzpt,year_,month_,kod_info,nzp_user);
create        index ix_calc_fon_0_3 on calc_fon_0(nzpt,kod_info);
create        index ix_calc_fon_0_4 on calc_fon_0(prior);

create table if not exists calc_fon_1
(	nzp_key   serial  not null, 
	nzp       integer default 0 not null, 
	nzp_user  integer default 0 not null, 
	nzpt      integer default 0 not null, 
	year_     integer default 0 not null, 
	month_    integer default 0 not null, 
	task      integer default 0 not null, 
	prior     integer default 0 not null, 
	kod_info  integer default 0 not null, 
	dat_in    timestamp, 
	dat_work  timestamp,  
	dat_out   timestamp, 
	txt       char(255),
	parameters varchar(255) 
);

alter table calc_fon_1 drop if exists progress;
alter table calc_fon_1 add progress numeric(6,4);

drop index if exists ix_calc_fon_1_1;
drop index if exists ix_calc_fon_1_2;
drop index if exists ix_calc_fon_1_3;
drop index if exists ix_calc_fon_1_4;
create unique index ix_calc_fon_1_1 on calc_fon_1(nzp_key);
create        index ix_calc_fon_1_2 on calc_fon_1(nzp,nzpt,year_,month_,kod_info,nzp_user);
create        index ix_calc_fon_1_3 on calc_fon_1(nzpt,kod_info);
create        index ix_calc_fon_1_4 on calc_fon_1(prior);

create table if not exists calc_fon_2
(	nzp_key   serial  not null, 
	nzp       integer default 0 not null, 
	nzp_user  integer default 0 not null, 
	nzpt      integer default 0 not null, 
	year_     integer default 0 not null, 
	month_    integer default 0 not null, 
	task      integer default 0 not null, 
	prior     integer default 0 not null, 
	kod_info  integer default 0 not null, 
	dat_in    timestamp, 
	dat_work  timestamp,  
	dat_out   timestamp, 
	txt       char(255),
	parameters varchar(255) 
);

alter table calc_fon_2 drop if exists progress;
alter table calc_fon_2 add progress numeric(6,4);

drop index if exists ix_calc_fon_2_1;
drop index if exists ix_calc_fon_2_2;
drop index if exists ix_calc_fon_2_3;
drop index if exists ix_calc_fon_2_4;
create unique index ix_calc_fon_2_1 on calc_fon_2(nzp_key);
create        index ix_calc_fon_2_2 on calc_fon_2(nzp,nzpt,year_,month_,kod_info,nzp_user);
create        index ix_calc_fon_2_3 on calc_fon_2(nzpt,kod_info);
create        index ix_calc_fon_2_4 on calc_fon_2(prior);

create table if not exists calc_fon_3
(	nzp_key   serial  not null, 
	nzp       integer default 0 not null, 
	nzp_user  integer default 0 not null, 
	nzpt      integer default 0 not null, 
	year_     integer default 0 not null, 
	month_    integer default 0 not null, 
	task      integer default 0 not null, 
	prior     integer default 0 not null, 
	kod_info  integer default 0 not null, 
	dat_in    timestamp, 
	dat_work  timestamp,  
	dat_out   timestamp, 
	txt       char(255),
	parameters varchar(255) 
);

alter table calc_fon_3 drop if exists progress;
alter table calc_fon_3 add progress numeric(6,4);

drop index if exists ix_calc_fon_3_1;
drop index if exists ix_calc_fon_3_2;
drop index if exists ix_calc_fon_3_3;
drop index if exists ix_calc_fon_3_4;
create unique index ix_calc_fon_3_1 on calc_fon_3(nzp_key);
create        index ix_calc_fon_3_2 on calc_fon_3(nzp,nzpt,year_,month_,kod_info,nzp_user);
create        index ix_calc_fon_3_3 on calc_fon_3(nzpt,kod_info);
create        index ix_calc_fon_3_4 on calc_fon_3(prior);

create table if not exists user_actions (id serial not null, nzp_user integer, nzp_page integer, page_name varchar(255), nzp_act integer, act_name varchar(255), changed_on timestamp);
drop index if exists ix_user_actions_1;
drop index if exists ix_user_actions_2;
drop index if exists ix_user_actions_3;
create unique index ix_user_actions_1 on user_actions(id);
create index ix_user_actions_2 on user_actions(changed_on);
create index ix_user_actions_3 on user_actions(nzp_user, changed_on);



alter table  calc_fon_0   alter column  parameters  type   character(2000);
alter table  calc_fon_1   alter column  parameters  type   character(2000);
alter table  calc_fon_2   alter column  parameters  type   character(2000);
alter table  calc_fon_3   alter column  parameters  type   character(2000);

