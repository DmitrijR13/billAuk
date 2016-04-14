database sysmaster;

--------------------------------------------------------
drop database websmr;
create database websmr with log;
--------------------------------------------------------

grant dba to webdb;
grant dba to public;


--------------------------------------------------------
--пользователи
--------------------------------------------------------
create table webdb.users
( nzp_user serial(1) not null,
  login    char(30) not null, 
  pwd      char(30) not null, 
  uname    char(60), 
  dat_log  datetime year to minute,
  ip_log   char(20), 
  browser  char(20)
);

insert into webdb.users (nzp_user,login,pwd,uname)
values (1,"websystem","123","Системный пользователь");

insert into webdb.users (nzp_user,login,pwd,uname)
values (2,"webuserc","userc","Пользователь РЦ");


create unique index webdb.ix_us_1 on webdb.users (nzp_user);
create unique index webdb.ix_us_2 on webdb.users (login,pwd);


--------------------------------------------------------
--справочник страниц
--------------------------------------------------------
create table webdb.pages
( nzp_page  serial not null,
  page_url  char(80),
  page_menu char(80),
  page_name char(80),
  hlp       char(255)
);

create unique index webdb.ix_pages_1 on pages (nzp_page);



insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (1, "Главная страница","~/default.aspx", "Главная страница", "предназначен для перехода на главную страницу программы");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (2, "Меню:","включает в себя следующие пункты:");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (3, "Помощь","предназначен для перехода на страницу «Помощь» в программе");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (4, "Предыдущая страница","предназначен для перехода на предыдущую страницу программы");





insert into webdb.pages (nzp_page,page_menu,hlp)
values (30, "Шаблоны поиска","осуществляет переход к формам для поиска информации в программе");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (31, "Поиск по адресу","~/kart/adres/findls.aspx","Поиск по адресу","выполняет поиск данных по адресу");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (32, "Поиск по параметрам","~/kart/prm/findprm.aspx","Поиск по характеристикам жилья","выполняет поиск данных по характеристикам жилья");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (33, "Поиск по начислениям","~/kart/charge/findch.aspx","Поиск по начислениям","выполняет поиск данных по начислениям");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (34, "Поиск по жителям","~/kart/gil/findgil.aspx","Поиск по персональным данным","выполняет поиск по персональным данным");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (35, "Поиск по показаниям","~/kart/counter/findcnt.aspx","Поиск по показаниям приборов учета","выполняет поиск по данным приборов учета");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (36, "Поиск по недопоставкам","~/kart/nedo/findnd.aspx","Поиск по недопоставкам","выполняет поиск данных по недопоставленным услугам");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (37, "Поиск по ОДН","~/kart/charge/findodn.aspx","Поиск по коэффициентам коррекции расходов дома","выполняет поиск данных по расходам для общедомовых нужд");



insert into webdb.pages (nzp_page,page_menu,hlp)
values (40, "Выбранные списки","осуществляет переход на ранее выбранные списки");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (41, "Лицевые счета","~/general/basepage/baselist.aspx","Список лицевых счетов","переходит на ранее выбранный список лицевых счетов");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (42, "Дома","~/general/basepage/baselist.aspx","Список домов","переходит на ранее выбранный список домов");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (43, "Улицы","~/general/basepage/baselist.aspx","Список улиц","переходит на список лицевых улиц");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (44, "Территории","~/general/basepage/baselist.aspx","Список территорий","переходит на список территорий (УК)");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (45, "Отделения","~/general/basepage/baselist.aspx","Список отделений","переходит на список территориальных отделений");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (46, "Банки данных","~/general/basepage/baselist.aspx","Список банков данных","переходит на список банков данных");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (47, "Дома улицы", "~/general/basepage/baselist.aspx", "Список домов улицы","переходит на список домов выбранной улицы");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (48, "Характеристики жилья", "~/kart/prm/spisprm.aspx", "Характеристики жилья","переходит на список характеристик жилья");


insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (51, "Характеристики жилья","~/kart/prm/spisprm.aspx","Характеристики жилья","переходит на список характеристик данной квартитры");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (52, "Поквартирная карточка","~/general/basepage/baselist.aspx","Поквартирная карточка","переходит на поквартирную карточку");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (53, "Приборы учета","~/kart/counter/counters.aspx","Список приборов учета","переходит на список выбранных приборов учета");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (54, "Показания приборов учета","~/kart/counter/spisval.aspx","Список показаний приборов учета","переходит на список показаний приборов учета для выбранной квартиры");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (55, "Недопоставки","~/kart/nedo/spisnd.aspx","Список недопоставок","переходит на список недопоставок");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (56, "Карточки жильцов", "~/general/basepage/baselist.aspx", "Список карточек жильцов","переходит на выбранный список карточек жильцов");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (57, "Лицевые счета дома", "~/general/basepage/baselist.aspx", "Список лицевых счетов дома","переходит на список лицевых счетов данного дома");



insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (59, "Параметры жилья дома", "~/kart/prm/spisprm.aspx", "Список параметров жилья дома","переходит на список характеристик данного дома");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (61, "Показания домовых приборов учета", "~/kart/counter/spisval.aspx", "Список показаний домовых приборов учета","переходит на список показаний приборов учета для выбранного дома");


insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (62, "Квартирные приборы учета","~/general/basepage/baselist.aspx","Квартирные приборы учета","переходит на список приборов учета для выбранной квартиры");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (63, "Домовые приборы учета","~/general/basepage/baselist.aspx","Домовые приборы учета","переходит на список приборов учета для выбранного дома");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (64, "Значения параметра", "~/kart/prm/prm.aspx", "Значения параметра","переходит на историю или архив значений параметра");

--65 код занят для списка жильцов для komplat 3.0

insert into webdb.pages (nzp_page,page_menu,hlp)
values (70, "Данные по квартире","осуществляет переход к данным квартиры");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (71, "Данные по дому","осуществляет переход к данным дома");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (72, "Аналитика","осуществляет переход к аналитическим данным");



insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (81, "Адресное пространство","~/general/basepage/aa.aspx","Адресное пространство","переходит к структуре адресного пространства");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (82, "Поставщики услуг","~/general/basepage/as.aspx","Поставщики услуг","переходит к структуре поставщиков услуг");


insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (83, "Начисления","~/general/basepage/aa.aspx","Начисления","переходит к структуре начисления в разрезе данных");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (84, "Интерактивная карта","~/general/basepage/baselist.aspx","Интерактивная карта","осуществляет переход к карте Яндекса");



insert into webdb.pages (nzp_page,page_menu,hlp)
values (120, "Начисления","осуществляет переход к начислениям");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (121, "Сальдо по лицевому счету","~/general/basepage/baselist.aspx","Сальдо по лицевому счету","осуществляет переход к сальдо по лицевому счету");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (122, "Список начислений","~/kart/charge/charges.aspx","Список начислений","осуществляет переход к начислениям лицевого счета");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (123, "Список платежей","~/kart/charge/listpays.aspx","Список платежей потребителей","осуществляет переход к платежам лицевого счета");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (124, "Расчет ОДН","~/kart/charge/odn.aspx","Расчет ОДН","осуществляет переход к расчету ОДН указанного дома");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (125, "Расчет","~/kart/charge/calc.aspx","Расчет начислений","осуществляет переход к начислениям указанного дома");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (126, "Сальдо по дому","~/general/basepage/baselist.aspx","Сальдо по дому","осуществляет переход к сальдо начислений по указанному дому");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (127, "Сальдо по управкомпании","~/general/basepage/baselist.aspx","Сальдо по управкомпании","осуществляет переход к сальдо начислений по указанной управкомпании");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (128, "Сальдо по участку","~/general/basepage/baselist.aspx","Сальдо по участку","осуществляет переход к сальдо начислений по указанному участку");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (129, "Счет-фактура","~/kart/bill/bill.aspx","Счет-фактура","переходит к счет-фактуре лицевого счета");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (130, "Сальдо по поставщику","~/general/basepage/baselist.aspx","Сальдо по поставщику","осуществляет переход к сальдо начислений по указанному поставщику");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (131, "Счет-фактура","~/kart/bill/billrt.aspx","Счет-фактура","переходит к счет-фактуре лицевого счета");

-- 132 для WebKomplat3
--INSERT INTO webdb.pages (NZP_PAGE, PAGE_URL, PAGE_MENU, PAGE_NAME, HLP)
--VALUES (132, '~/general/basepage/baselist.aspx', 'Распределение оплат по услугам', 'Распределение оплат по услугам', 'переходит к распределению оплат лицевого счета по услугам');

insert into webdb.pages (nzp_page,page_menu,page_name)
values (949, "Нет данных", "Нет данных");


insert into webdb.pages (nzp_page,page_menu,hlp)
values (999, "Завершить сеанс","предназначен для завершения работы в программе");




--------------------------------------------------------
--главное меню: навигация страниц
--------------------------------------------------------
create table webdb.pages_show
( nzp_psh   serial  not null,
  cur_page  integer not null,           --текущая страница
  page_url  integer default 0 not null, --вызывается nzp_page>0
  up_kod    integer default 0 not null, --ссылка на заголовок
  sort_kod  integer default 0 not null  --порядок отображения
);

create unique index webdb.ix_pagesh_1 on pages_show (nzp_psh);
create unique index webdb.ix_pagesh_2 on pages_show (cur_page, page_url, up_kod);



--------------------------------------------------------
--справочник действий
--------------------------------------------------------
create table webdb.s_actions
( nzp_act   serial not null,
  act_name  char(80) not null,
  hlp       char(255)
);

create unique index webdb.ix_sact_1 on s_actions (nzp_act);



--------------------------------------------------------
--actions
--------------------------------------------------------
insert into webdb.s_actions (nzp_act,act_name,hlp) values (1, "Выполнить поиск"  ,"выполняет поиск после заполнения требуемых полей поиска");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (2, "Очистить шаблон"  ,"очищает ранее заполненные поля шаблона");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (3, "Открыть данные"   ,"открывает указанные данные на просмотр или для редактирования");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (4, "Добавить запись"  ,"добавляет запись в список");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (5, "Обновить список"  ,"обновляет ранее выбранный список");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (6, "Очистить список"  ,"очищает ранее заполненные поля формы");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (7, "Показать карту"   ,"показывает интерактивную карту Яndex");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (8, "На печать"        ,"открывает форму для печати");

insert into webdb.s_actions (nzp_act,act_name,hlp) values (51,"Изменить состояние", "изменяет состояние счета");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (61,"Сохранить изменения","сохраняет введенные данные");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (62,"Добавить ПУ",        "добавляет запись прибоа учет");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (63,"Редактировать ПУ",   "редактирует запись прибора учета");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (64,"Удалить ПУ",         "удаляет запись прибора учета");

insert into webdb.s_actions (nzp_act,act_name,hlp) values (65,"Выполнить подсчет",    "выполняет подсчет агрегированных данных");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (66,"Обновить данные",      "выполняет обновление текущих данных");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (67,"Показать все значения","показывает все значения одного параметра");


--------------------------------------------------------
--checkboxlist
--------------------------------------------------------
insert into webdb.s_actions (nzp_act,act_name,hlp) values (501,"Шаблон по адресам",    "учитывает при поиске данные, введенные в шаблоне поиска адресов");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (502,"Шаблон по параметрам", "учитывает при поиске данные, введенные в шаблоне поиска характеристик жилья");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (503,"Шаблон по начислениям","учитывает при поиске данные, введенные в шаблоне поиска начислений и расходов");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (504,"Шаблон по жильцам",    "учитывает при поиске данные, введенные в шаблоне поиска житилей");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (505,"Шаблон по показаниям", "учитывает при поиске данные, введенные в шаблоне поиска показаний приборов учета");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (506,"Шаблон недопоставок",  "учитывает при поиске данные, введенные в шаблоне поиска недопоставок");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (507,"Шаблон ОДН",           "учитывает при поиске данные, введенные в шаблоне поиска адресов");


insert into webdb.s_actions (nzp_act,act_name,hlp) values (520,"По месяцам",    "выполняет выборку данных в помесячном разрезе");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (521,"По услугам",    "выполняет выборку данных в разрезе услуг");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (522,"По поставщикам","выполняет выборку данных в разрезе поставщиков");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (523,"По формулам",    "выполняет выборку данных разрезе формул расчета услуг");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (524,"По территориям","выполняет выборку данных разрезе УК");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (525,"По отделениям",    "выполняет выборку данных в разрезе участков");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (526,"По банкам данных","выполняет выборку данных в разрезе банков данных");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (527,"По домам",    "выполняет выборку данных в подомовом разрезе");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (528,"Сальдовые",    "выполняет отображение сальдовых сумм");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (529,"Норматив/прибор учета","");
                                             
                                             
--------------------------------------------------------
--dropdownlist"s (1,2)
--------------------------------------------------------
insert into webdb.s_actions (nzp_act,act_name,hlp) values (601,"Сортировать по адресу",    "сортирует список по адресу");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (602,"Сортировать по лс",    "сортирует список по лицевым счетам");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (603,"Сортировать по улице",    "сортирует список по улице");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (604,"Сортировать по территории","сортирует список по управкомпаниям");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (605,"Сортировать по услуге",    "сортирует список по услугам");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (606,"Сортировать по поставщику","сортирует список по поставщикам");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (607,"Сортировать по ФИОДР",    "сортирует список по людям");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (610,"На просмотр",        "открывает данные на просмотр");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (611,"Для изменения",        "открывает данные для редактирования");


insert into webdb.s_actions (nzp_act,act_name,hlp) values (701,"Выводить по 20 записей","выводить список по 20 записей");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (702,"Выводить по 50 записей","выводить список по 50 записей");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (703,"Выводить по 100 записей","выводить список по 100 записей");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (721,"Управляющая компания / Улица / Дом",  "отображает данные в разрезе управляющих компаний");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (722,"Банк данных / Участок / Улица / Дом", "отображает данные в разрезе банков данных");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (723,"Улица / Дом",                           "отображает данные в разрезе улиц");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (724,"Поставщик / Услуга / Формула",        "отображает данные в разрезе поставщиков услуг");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (725,"Услуга / Поставщик / Управляющая компания / Участок ","отображает данные в разрезе услуг");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (726,"Управляющая компания / Поставщик / Услуга / Формула", "отображает данные в разрезе УК и поставщиков");


insert into webdb.s_actions (nzp_act,act_name,hlp) values (801,"Новый поиск",        "выполняет новый поиск данных");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (802,"Поиск по выбранным лс",    "выполняет поиск данных по ранее выбранному списку счетов");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (803,"Поиск по выбранным домам","выполняет поиск данных по ранее выбранному списку домов");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (851,"Показать убывших",    "");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (852,"Показать историю",    "");




--------------------------------------------------------
--меню действий: отображение 
--------------------------------------------------------
create table webdb.actions_show
( nzp_ash   serial  not null,
  cur_page  integer not null,           --текущая страница      
  nzp_act   integer default 0 not null, --действие
  act_tip   integer default 0 not null, --тип отображения
  act_dd    integer default 0 not null, --местоположение отображения
  sort_kod  integer default 0 not null  --порядок отображения   
);

create unique index webdb.ix_actsh_1 on actions_show (nzp_ash);
create unique index webdb.ix_actsh_2 on actions_show (cur_page,nzp_act,act_tip,act_dd);



--------------------------------------------------------
--меню действий: навигация 
--------------------------------------------------------
create table webdb.actions_lnk
( nzp_al    serial  not null,
  cur_page  integer not null,           --текущая страница      
  nzp_act   integer default 0 not null, --действие
  page_url  integer default 0 not null  --ссылка на вызываемую страницу
);

create unique index webdb.ix_actl_1 on actions_lnk (nzp_al);
create        index webdb.ix_actl_2 on actions_lnk (cur_page,nzp_act);



delete from pages_show   where 1 = 1;
delete from actions_show where 1 = 1;
delete from actions_lnk  where 1 = 1;


--------------------------------------------------------
--нет данных
--------------------------------------------------------
insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 949,nzp_page,0,nzp_page from pages
where nzp_page >= 950 
   or nzp_page < 10; 

update pages_show
set up_kod = 2
where page_url >=1 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 950
where page_url > 950 ;


--------------------------------------------------------
--default.aspx
--------------------------------------------------------
insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 1,nzp_page,0,nzp_page from pages
where nzp_page >= 10 and nzp_page < 30 or nzp_page >= 950; 

update pages_show
set up_kod = 2
where page_url >=1 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 10
where page_url > 10 and page_url <= 19;


--------------------------------------------------------
--findls.aspx
--------------------------------------------------------
delete from pages_show   where cur_page = 31;
delete from actions_show where cur_page = 31;
delete from actions_lnk  where cur_page = 31;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 31,nzp_page,0,nzp_page from pages
where nzp_page in (30,32,33,34,35,36,37, 40,41,42,43,44,45,46,53, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page < 10; 

update pages_show
set up_kod = 2
where cur_page = 31 and (page_url>=1 and page_url < 10 and page_url <> 2);

update pages_show
set up_kod = 30
where cur_page = 31 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 40
where cur_page = 31 and (page_url >= 41 and page_url <= 59);

update pages_show
set up_kod = 72 
where cur_page = 31 and page_url in (81,82);

update pages_show
set sort_kod = sort_kod + 100
where cur_page = 31 and up_kod =40 and page_url in (43,44,45,46);


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 31,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (1,2, 502,503,504,505,506,507);


update actions_show
set sort_kod = sort_kod + 1000
where cur_page = 31
  and nzp_act in (673,674,675,676);


insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (31,1,41);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (31,1,42);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (31,1,43);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (31,1,44);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (31,1,45);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (31,1,46);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (31,1,53);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (31,2,31);


--------------------------------------------------------
--findch.aspx
--------------------------------------------------------
delete from pages_show   where cur_page = 33;
delete from actions_show where cur_page = 33;
delete from actions_lnk  where cur_page = 33;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 33,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,34,35,36,37, 40,41,42) 
   or nzp_page >= 950 
   or nzp_page < 10; 

update pages_show
set up_kod = 2
where cur_page = 33 and (page_url>=1 and page_url < 10 and page_url <> 2);

update pages_show
set up_kod = 30
where cur_page = 33 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 40
where cur_page = 33 and (page_url >= 41 and page_url <= 59);


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 33,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (1,2, 501,502,504,505,506,507);

update actions_show
set sort_kod = sort_kod + 1000
where cur_page = 33
  and nzp_act in (673,674,675,676);


insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (33,1,41);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (33,1,42);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (33,2,33);


--------------------------------------------------------
--findcnt.aspx
--------------------------------------------------------
delete from pages_show   where cur_page = 35;
delete from actions_show where cur_page = 35;
delete from actions_lnk  where cur_page = 35;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 35,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,36,37, 40,41,42,53) 
   or nzp_page >= 950 
   or nzp_page < 10; 

update pages_show
set up_kod = 2
where cur_page = 35 and (page_url>=1 and page_url < 10 and page_url <> 2);

update pages_show
set up_kod = 30
where cur_page = 35 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 40
where cur_page = 35 and (page_url >= 41 and page_url <= 59);


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 35,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (1,2, 501, 801,802,803);

insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (35,1,41);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (35,1,42);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (35,2,35);


--------------------------------------------------------
--findodn.aspx
--------------------------------------------------------
--!37
delete from pages_show   where cur_page = 37;
delete from actions_show where cur_page = 37;
delete from actions_lnk  where cur_page = 37;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 37,nzp_page,0,nzp_page from pages
where nzp_page in (30,31,32,33,34,35,36, 40,41,42) 
   or nzp_page >= 950 
   or nzp_page < 10; 

update pages_show
set up_kod = 2
where cur_page = 37 and (page_url>=1 and page_url < 10 and page_url <> 2);

update pages_show
set up_kod = 30
where cur_page = 37 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 40
where cur_page = 37 and (page_url >= 41 and page_url <= 59);

update pages_show
set sort_kod = sort_kod + 100
where cur_page = 37 and up_kod =40 and page_url in (43,44,45,46);


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 37,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (1,2, 501,502,503,504,505,506);

update actions_show
set sort_kod = sort_kod + 1000
where cur_page = 37
  and nzp_act in (673,674,675,676);


insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (37,1,41);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (37,1,42);
insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (37,2,37);


--------------------------------------------------------
--spisls.aspx
--------------------------------------------------------
--!41
delete from pages_show   where cur_page = 41;
delete from actions_show where cur_page = 41;
delete from actions_lnk  where cur_page = 41;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 41,nzp_page,0,nzp_page from pages
where nzp_page in (30, 31,33,35,37, 40, 42,43,44,45,46,53, 70,71, 51,52,54,55,56,57,59,61,62,63, 121,122,123,124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 41 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 30
where cur_page = 41 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 40
where cur_page = 41 and page_url >= 41 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 41 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 41 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 41 and page_url in (81,82);

update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 41 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 41 and page_url in (40); 

update pages_show
set up_kod = 2
where cur_page = 41 and page_url >= 950 ;

insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 41,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (3,4,5,6, 51,610,611);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 41,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 41,nzp_act,nzp_act,2,4
from webdb.s_actions
where nzp_act in (601,602);

--------------------------------------------------------
--spisdom.aspx
--------------------------------------------------------
--!42
delete from pages_show   where cur_page = 42;
delete from actions_show where cur_page = 42;
delete from actions_lnk  where cur_page = 42;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 42,nzp_page,0,nzp_page from pages
where nzp_page in (30, 31,33,35,37, 40, 41,43,44,45,46, 71, 53, 57,59,61,63, 124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10; 

update pages_show
set up_kod = 2
where cur_page = 42 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 30
where cur_page = 42 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 40
where cur_page = 42 and page_url >= 41 and page_url <=53;

update pages_show
set up_kod = 71
where cur_page = 42 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 42 and page_url in (81,82);


update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 42 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 42 and page_url in (40); 



insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 42,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (3,4,5,6, 515, 610,611);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 42,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 42,nzp_act,nzp_act,2,4
from webdb.s_actions
where nzp_act in (603,604);


--------------------------------------------------------
--spisul.aspx
--------------------------------------------------------
delete from pages_show   where cur_page = 43;
delete from actions_show where cur_page = 43;
delete from actions_lnk  where cur_page = 43;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 43,nzp_page,0,nzp_page from pages
where nzp_page in (40,30,31, 41,42,44,45,46,47, 70) 
   or nzp_page >= 950 
   or nzp_page <  10; 

update pages_show
set up_kod = 2
where cur_page = 43 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 30
where cur_page = 43 and page_url >= 31 and page_url <= 39;

update pages_show
set up_kod = 40
where cur_page = 43 and page_url >= 41 and page_url <=46;

update pages_show
set up_kod = 70
where cur_page = 43 and page_url = 47;

update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 43 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 43 and page_url in (40); 



insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 43,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (3,4,5,6, 610,611);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 43,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 43,nzp_act,nzp_act,2,4
from webdb.s_actions
where nzp_act in (603);


--------------------------------------------------------
--spisar.aspx
--------------------------------------------------------
delete from pages_show   where cur_page = 44;
delete from actions_show where cur_page = 44;
delete from actions_lnk  where cur_page = 44;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 44,nzp_page,0,nzp_page from pages
where nzp_page in (40,30,31, 41,42,43,45,46) 
   or nzp_page >= 950 
   or nzp_page <  10; 

update pages_show
set up_kod = 2
where cur_page = 44 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 30
where cur_page = 44 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 40
where cur_page = 44 and page_url >= 41 and page_url <=46;


update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 44 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 44 and page_url in (40); 



insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 44,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (3,4,5,6, 610,611);



--------------------------------------------------------
--spisgeu.aspx
--------------------------------------------------------
delete from pages_show   where cur_page = 45;
delete from actions_show where cur_page = 45;
delete from actions_lnk  where cur_page = 45;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 45,nzp_page,0,nzp_page from pages
where nzp_page in (40,30,31, 41,42,43,44,46) 
   or nzp_page >= 950 
   or nzp_page <  10; 

update pages_show
set up_kod = 2
where cur_page = 45 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 30
where cur_page = 45 and page_url >= 31 and page_url <= 39;

update pages_show
set up_kod = 40
where cur_page = 45 and page_url >= 41 and page_url <=46;


update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 45 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 45 and page_url in (40); 


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 45,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (3,4,5,6, 610,611);



--------------------------------------------------------
--spispu.aspx список приборов
--------------------------------------------------------
--!53
delete from pages_show   where cur_page = 53;
delete from actions_show where cur_page = 53;
delete from actions_lnk  where cur_page = 53;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 53,nzp_page,0,nzp_page from pages
where nzp_page in (30, 31, 40, 41,42,43,44,45,46, 70,71, 51,52,54,55,56,57,59,61,62,63, 121,122,123,124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 53 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 53 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 53 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 53 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 53 and page_url in (81,82);

update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 53 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 53 and page_url in (40); 



delete from webdb.actions_show where cur_page = 53;

insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 53,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (3,4,5,6, 610,611, 601, 701,702,703);

--------------------------------------------------------
--квартирные ПУ
--------------------------------------------------------
--!62
delete from pages_show   where cur_page = 62;
delete from actions_show where cur_page = 62;
delete from actions_lnk  where cur_page = 62;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 62,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 41, 54) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 62 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 62 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 62 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 62 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 62 and page_url in (81,82);

update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 62 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 62 and page_url in (40); 



delete from webdb.actions_show where cur_page = 62;

insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 62,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 610);

--------------------------------------------------------
--домовые ПУ
--------------------------------------------------------
--!63
delete from pages_show   where cur_page = 63;
delete from actions_show where cur_page = 63;
delete from actions_lnk  where cur_page = 63;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 63,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 42, 61) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 63 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 63 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 63 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 63 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 63 and page_url in (81,82);

update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 63 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 63 and page_url in (40); 



delete from webdb.actions_show where cur_page = 63;

insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 63,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 610);


--------------------------------------------------------
--spisval.aspx показания домовых ПУ
--------------------------------------------------------
--!61
delete from pages_show   where cur_page = 61;
delete from actions_show where cur_page = 61;
delete from actions_lnk  where cur_page = 61;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 61,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,43,44,45, 57,59,63, 71, 124,126, 72,81,82) 
   or nzp_page >= 999 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 61 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 61 and page_url >= 31 and page_url <=46 and page_url <> 40;

update pages_show
set up_kod = 71
where cur_page = 61 and page_url in (57,59,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 61 and page_url in (81,82);


update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 61 and page_url in (40); 

update pages_show
set up_kod = 2
where cur_page = 61 and page_url >=950; 

insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 61,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 610);

update webdb.actions_show
set act_tip  = (case when nzp_act < 600 then 1 else 2 end),
    act_dd = (case when nzp_act < 700 then 1 else 2 end)
where nzp_act>=500
 and cur_page = 61;

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 61,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 61,nzp_act,nzp_act,2,4
from webdb.s_actions
where nzp_act in (601,602);

insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (61,5,61);

--------------------------------------------------------
--spisval.aspx
--------------------------------------------------------
--!54
delete from pages_show   where cur_page = 54;
delete from actions_show where cur_page = 54;
delete from actions_lnk  where cur_page = 54;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 54,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 41,42,43,44,45,46,53, 70,71, 51,52,55,56,57,59,61,62,63, 121,122,123,124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 54 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 54 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 54 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 54 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 54 and page_url in (81,82);

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 54 and page_url in (40); 



insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 54,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5,61, 610,611);


insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (54,5,54);


--------------------------------------------------------
--saldols.aspx
--------------------------------------------------------
--!121
delete from pages_show   where cur_page = 121;
delete from actions_show where cur_page = 121;
delete from actions_lnk  where cur_page = 121;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 121,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 41,42,43,44,45,46,53, 70,71, 51,52,54,55,56,57,59,61,62,63, 122,123,124,126, 131, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 121 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 121 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 121 and page_url in (51,52,54,55,56,62, 121,122,123, 131);

update pages_show
set up_kod = 71
where cur_page = 121 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 121 and page_url in (81,82);


update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 121 and page_url in (40); 


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 121,nzp_act,nzp_act
from webdb.s_actions
where nzp_act in (5, 8, 605,606, 520,521,522 );


--------------------------------------------------------
--billrt.aspx
--------------------------------------------------------
--!131
delete from pages_show   where cur_page = 131;
delete from actions_show where cur_page = 131;
delete from actions_lnk  where cur_page = 131;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 131,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,43,44,45, 51,54,55, 70,120,121,122,123) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 131 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 131 and page_url >= 31 and page_url <=46 and page_url <> 40;

update pages_show
set up_kod = 70
where cur_page = 131 and page_url >= 51 and page_url <=59;

update pages_show
set up_kod = 120
where cur_page = 131 and page_url >= 121 and page_url <= 131;

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 131 and page_url in (40); 




--------------------------------------------------------
--saldodom.aspx
--------------------------------------------------------
--!126
delete from pages_show   where cur_page = 126;
delete from actions_show where cur_page = 126;
delete from actions_lnk  where cur_page = 126;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 126,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,43,44,45, 59,61,63,71, 124, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 126 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 126 and page_url >= 31 and page_url <=46 and page_url <> 40;

update pages_show
set up_kod = 71
where cur_page = 126 and page_url in (124,59,61,63);

update pages_show
set up_kod = 72
where cur_page = 126 and page_url in (81,82);


update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 126 and page_url in (40); 


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 126,nzp_act,nzp_act
from webdb.s_actions
where nzp_act in (5,8, 65,66, 520,521,522 );


--------------------------------------------------------
--saldouk.aspx
--------------------------------------------------------
--!127
delete from pages_show   where cur_page = 127;
delete from actions_show where cur_page = 127;
delete from actions_lnk  where cur_page = 127;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 127,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 127 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 127 and page_url >= 31 and page_url <=46 and page_url <> 40;

update pages_show
set up_kod = 72
where cur_page = 127 and page_url in (81,82);

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 127 and page_url in (40); 


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 127,nzp_act,nzp_act
from webdb.s_actions
where nzp_act in (5,65,66, 520,521,522 );


--------------------------------------------------------
--saldosupp.aspx
--------------------------------------------------------
--!130
delete from pages_show   where cur_page = 130;
delete from actions_show where cur_page = 130;
delete from actions_lnk  where cur_page = 130;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 130,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 130 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 130 and page_url >= 31 and page_url <=46 and page_url <> 40;

update pages_show
set up_kod = 72
where cur_page = 130 and page_url in (81,82);

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 130 and page_url in (40); 


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 130,nzp_act,nzp_act
from webdb.s_actions
where nzp_act in (5,65,66, 520,521,522 );



--------------------------------------------------------
--charges.aspx
--------------------------------------------------------
--!122
delete from pages_show   where cur_page = 122;
delete from actions_show where cur_page = 122;
delete from actions_lnk  where cur_page = 122;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 122,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 41,42,43,44,45,46,53, 70,71, 51,52,54,55,56,57,59,61,62,63, 121,123,124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 122 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 122 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 122 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 122 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 122 and page_url in (81,82);

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 122 and page_url in (40); 


insert into actions_show (cur_page,nzp_act,sort_kod)
select 122,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 520, 521, 522, 523, 528);



--------------------------------------------------------
--odn.aspx
--------------------------------------------------------
--!124
delete from pages_show   where cur_page = 124;
delete from actions_show where cur_page = 124;
delete from actions_lnk  where cur_page = 124;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 124,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,43,44,45, 59,61,63,71, 126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 124 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 124 and page_url >= 31 and page_url <=46 and page_url <> 40;

update pages_show
set up_kod = 71
where cur_page = 124 and page_url in (59,61,63, 126);

update pages_show
set up_kod = 72
where cur_page = 124 and page_url in (81,82);

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 124 and page_url in (40); 

insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 124,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5);

insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (124,5,124);

--------------------------------------------------------
--spisnd.aspx
--------------------------------------------------------
--!55
delete from pages_show   where cur_page = 55;
delete from actions_show where cur_page = 55;
delete from actions_lnk  where cur_page = 55;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 55,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 41,42,43,44,45,46,53, 70,71, 51,52,54,56,57,59,61,62,63, 121,122,123,124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 55 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 55 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 55 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 55 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 55 and page_url in (81,82);

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 55 and page_url in (40); 


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 55,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (3,4,5,6, 51,610,611);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 55,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 55,nzp_act,nzp_act,2,4
from webdb.s_actions
where nzp_act in (601,602);



--------------------------------------------------------
--spisprm.aspx картирные параметры
--------------------------------------------------------
--!51
delete from pages_show   where cur_page = 51;
delete from actions_show where cur_page = 51;
delete from actions_lnk  where cur_page = 51;


--update  pages set  page_url="~/general/basepage/baselist.aspx" where  nzp_page=51;
update  pages set  page_url="~/kart/prm/spisprm.aspx" where nzp_page=51;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 51,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 41,42,43,44,45,46,53, 70,71, 52,54,55,56,57,59,61,62,63, 121,122,123,124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 51 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 51 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 51 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 51 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 51 and page_url in (81,82);


update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 51 and page_url in (40); 



insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 51,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 67, 610);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 51,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 51,nzp_act,nzp_act,2,4
from webdb.s_actions
where nzp_act in (601,602);

insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (51,67,64);

--------------------------------------------------------
--spisnd.aspx квартирные недопоставки
--------------------------------------------------------
--!55
delete from pages_show   where cur_page = 55;
delete from actions_show where cur_page = 55;
delete from actions_lnk  where cur_page = 55;

insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 55,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 41,42,43,44,45,46,53, 70,71, 51,52,54,56,57,59,61,62,63, 121,122,123,124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 55 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 55 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 55 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 55 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 55 and page_url in (81,82);


update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 55 and page_url in (40); 



insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 55,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 610);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 55,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

--------------------------------------------------------
--spisprm.aspx домовые параметры
--------------------------------------------------------
--!59
delete from pages_show   where cur_page = 59;
delete from actions_show where cur_page = 59;
delete from actions_lnk  where cur_page = 59;


--update  pages set  page_url="~/general/basepage/baselist.aspx" where  nzp_page=58;
update  pages set  page_url="~/kart/prm/spisprm.aspx" where nzp_page=59;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 59,nzp_page,0,nzp_page from pages
where nzp_page in (40,31,41,42,43,44,45, 57,61,63, 71, 124,126, 72,81,82) 
   or nzp_page >= 999 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 59 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 59 and page_url >= 31 and page_url <=46 and page_url <> 40;

update pages_show
set up_kod = 71
where cur_page = 59 and page_url in (57,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 59 and page_url in (81,82);



update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 59 and page_url in (40); 

update pages_show
set up_kod = 2
where cur_page = 59 and page_url >=950; 

insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 59,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 67, 610);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 59,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 59,nzp_act,nzp_act,2,4
from webdb.s_actions
where nzp_act in (601,602);

insert into webdb.actions_lnk (cur_page,nzp_act,page_url) values (59,67,64);

--------------------------------------------------------
--prm.aspx
--------------------------------------------------------
--!64
delete from pages_show   where cur_page = 64;
delete from actions_show where cur_page = 64;
delete from actions_lnk  where cur_page = 64;

insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 64,nzp_page,0,nzp_page from pages
where nzp_page in (40,41,42,48) 
   or nzp_page >= 950 
   or nzp_page <  10;

update pages_show       
set up_kod = 2
where cur_page = 64 and ((page_url < 10 and page_url <> 2) or (page_url >= 950));

update pages_show
set up_kod = 40
where cur_page = 64 and page_url >= 31 and page_url <=48 and page_url <> 40;

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 64 and page_url in (40); 

insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 64,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 610);

update webdb.actions_show
set act_tip  = (case when nzp_act < 600 then 1 else 2 end),
    act_dd = (case when nzp_act < 700 then 1 else 2 end)
where nzp_act>=500
 and cur_page = 64;



--------------------------------------------------------
--listpays.aspx
--------------------------------------------------------
delete from pages_show   where cur_page = 123;
delete from actions_show where cur_page = 123;
delete from actions_lnk  where cur_page = 123;


--update  pages set  page_url="~/general/basepage/baselist.aspx" where  nzp_page=51;
update  pages set  page_url="~/kart/charge/listpays.aspx" where nzp_page=123;

insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 123,nzp_page,0,nzp_page from pages
where nzp_page in (31, 40, 41,42,43,44,45,46,53, 70,71, 51,52,54,55,56,57,59,61,62,63, 121,122,124,126, 72,81,82) 
   or nzp_page >= 950 
   or nzp_page <  10;
  

update pages_show
set up_kod = 2
where cur_page = 123 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 40
where cur_page = 123 and page_url >= 31 and page_url <=53;

update pages_show
set up_kod = 70
where cur_page = 123 and page_url in (51,52,54,55,56,62, 121,122,123);

update pages_show
set up_kod = 71
where cur_page = 123 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72 
where cur_page = 123 and page_url in (81,82);

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 123 and page_url in (40); 

update pages_show
set up_kod = 2
where cur_page = 123 and page_url >= 950 ;


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 123,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (5, 610);

update webdb.actions_show
set act_tip  = (case when nzp_act < 600 then 1 else 2 end),
    act_dd = (case when nzp_act < 700 then 1 else 2 end)
where nzp_act>=500
 and cur_page = 123;

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 123,nzp_act,nzp_act,2,3
from webdb.s_actions
where nzp_act in (701,702,703);

insert into webdb.actions_show (cur_page,nzp_act,sort_kod,act_tip,act_dd)
select 123,nzp_act,nzp_act,2,4
from webdb.s_actions
where nzp_act in (601,602);


--------------------------------------------------------
--spisgil.aspx
--------------------------------------------------------
delete from pages_show   where cur_page = 52;
delete from actions_show where cur_page = 52;
delete from actions_lnk  where cur_page = 52;

delete from pages_show   where cur_page = 56;
delete from actions_show where cur_page = 56;
delete from actions_lnk  where cur_page = 56;

insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 1, 2, 1);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 2, 0, 2);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 3, 2, 3);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 4, 2, 4);

insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 30, 0, 20);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 31, 30, 5);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 40, 0, 7);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 41, 40, 8);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 42, 40, 9);

insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 70,  0, 10);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 51, 70, 51);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 54, 70, 54);

insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 52, 999, 2, 999);


insert into actions_show ( cur_page, nzp_act, act_tip, act_dd, sort_kod) values (52, 5, 0, 0, 5);

insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (52, 610, 2, 1, 610);
insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (52, 611, 2, 1, 611);

insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (52, 701, 2, 3, 701);
insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (52, 702, 2, 3, 702);
insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (52, 703, 2, 3, 703);

insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (52, 607, 2, 4, 607);

insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (52, 851, 1, 1, 851);
insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (52, 852, 1, 1, 852);

--

insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 1, 2, 1);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 2, 0, 2);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 3, 2, 3);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 4, 2, 4);

insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 31, 40, 5);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 40, 0, 7);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 41, 40, 8);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 42, 40, 9);

insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 70,  0, 10);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 51, 70, 51);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 54, 70, 54);
insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 52, 70, 52);

insert into pages_show( cur_page, page_url, up_kod, sort_kod) values ( 56, 999, 2, 999);


insert into actions_show ( cur_page, nzp_act, act_tip, act_dd, sort_kod) values (56, 5, 0, 0, 5);

insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (56, 610, 2, 1, 610);
insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (56, 611, 2, 1, 611);

insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (56, 701, 2, 3, 701);
insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (56, 702, 2, 3, 702);
insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (56, 703, 2, 3, 703);

insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (56, 601, 2, 4, 601);
insert into actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod)  values (56, 602, 2, 4, 602);


update pages_show
set sort_kod = 200, up_kod = 0
where cur_page in (52,56) and page_url in (40); 

update pages_show
set up_kod = 2
where cur_page in (52,56) and page_url >= 950 ;


--------------------------------------------------------
--a_adres.aspx
--------------------------------------------------------
--!81
delete from pages_show   where cur_page = 81;
delete from actions_show where cur_page = 81;
delete from actions_lnk  where cur_page = 81;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 81,nzp_page,0,nzp_page from pages
where nzp_page in (30, 31,33,35,37, 71, 57,59,61,63, 124,126, 120,127,128,130, 72,82) 
   or nzp_page >= 950 
   or nzp_page <  10; 

update pages_show
set up_kod = 2
where cur_page = 81 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 30
where cur_page = 81 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 71
where cur_page = 81 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72
where cur_page = 81 and page_url in (82);

update pages_show
set up_kod = 120 
where cur_page = 81 and page_url in (127,128,130);

update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 81 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 81 and page_url in (72) and up_kod = 0; 


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 81,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (7, 65,66, 721,722,723);



--------------------------------------------------------
--a_supp.aspx
--------------------------------------------------------
--!82
delete from pages_show   where cur_page = 82;
delete from actions_show where cur_page = 82;
delete from actions_lnk  where cur_page = 82;


insert into webdb.pages_show (cur_page,page_url,up_kod,sort_kod)
select 82,nzp_page,0,nzp_page from pages
where nzp_page in (30, 31,33,35,37, 120,127,128,130, 72,81) 
   or nzp_page >= 950 
   or nzp_page <  10; 

update pages_show
set up_kod = 2
where cur_page = 82 and page_url < 10 and page_url <> 2;

update pages_show
set up_kod = 30
where cur_page = 82 and (page_url >= 31 and page_url <= 39);

update pages_show
set up_kod = 71
where cur_page = 82 and page_url in (57,59,61,63, 124,126);

update pages_show
set up_kod = 72
where cur_page = 82 and page_url in (81);

update pages_show
set up_kod = 120 
where cur_page = 82 and page_url in (127,128,130);

update pages_show
set sort_kod = 199, up_kod = 0
where cur_page = 82 and page_url in (30); 

update pages_show
set sort_kod = 200, up_kod = 0
where cur_page = 82 and page_url in (72) and up_kod = 0; 


insert into webdb.actions_show (cur_page,nzp_act,sort_kod)
select 82,nzp_act,nzp_act from webdb.s_actions
where nzp_act in (65,66, 724,725,726);






--аналитика после начислений
update pages_show
set sort_kod = 201
where page_url in (72) and up_kod = 0; 


--выход в самый конец подменю
update pages_show
set up_kod = 950
where page_url > 950 ;

delete from pages_show where page_url = 950;

update pages_show
set up_kod = 2
where up_kod = 950 ;



--------------------------------------------------------
--местоположение action"s
--------------------------------------------------------
update webdb.actions_show
set act_tip  = 0,
    act_dd   = 0
where nzp_act < 100;

update webdb.actions_show
set act_tip  = (case when nzp_act < 600 then 1 else 2 end),
    act_dd   = (case when nzp_act < 600 then 1 else 2 end)
where nzp_act >100;

update webdb.actions_show
set act_dd   = 3
where nzp_act >= 701 and nzp_act < 740;

update webdb.actions_show
set act_dd = 4
where nzp_act >= 601 and nzp_act < 610;

update actions_show  
   set (act_tip, act_dd) = ( 1, 1)
where nzp_act in ( 851,852 ) ;



--------------------------------------------------------
-- лог-журнал доступа
--------------------------------------------------------
create table webdb.log_access
( nzp_lacc  serial not null,
  nzp_user  integer default 0,
  acc_kod   integer default 0,
  dat_log   datetime year to minute,
  ip_log    char(20), 
  browser   char(20),
  login     char(30), 
  pwd       char(30),
  idses     char(30)
);

create unique index webdb.ix_lacc_1 on log_access (nzp_lacc);
create        index webdb.ix_lacc_2 on log_access (nzp_user,dat_log);


--------------------------------------------------------
-- лог-журнал запросов
--------------------------------------------------------
create table webdb.log_sql
( nzp_lsql  serial not null,
  nzp_user  integer default 0,
  dat_log   datetime year to minute,
  err_kod   integer,
  sql_txt   char(255),
  sql_err   char(255)
);

create unique index webdb.ix_lsql_1 on log_sql (nzp_lsql);
create        index webdb.ix_lsql_2 on log_sql (nzp_user,dat_log);


--------------------------------------------------------
-- лог-журнал истории (сессии)
--------------------------------------------------------
create table webdb.log_history
( nzp_lhis  serial not null,
  nzp_user  integer default 0,
  nzp_page  integer default 0,
  idses     char(30) not null,
  kod1      integer default 0,
  kod2      integer default 0,
  kod3      integer default 0,
  dat_log   datetime year to minute
);

create unique index webdb.ix_lhis_1 on log_history (nzp_lhis);
create        index webdb.ix_lhis_2 on log_history (nzp_user,idses);



--------------------------------------------------------
--констрейнты
--------------------------------------------------------
alter table pages   add constraint primary key (nzp_page)  constraint "are".pk_pages;

alter table pages_show add constraint
  (foreign key (page_url) references pages constraint "are".fk_pagel_01);

alter table s_actions   add constraint primary key (nzp_act)  constraint "are".pk_actions;

alter table actions_show add constraint
  (foreign key (nzp_act) references s_actions constraint "are".fk_actl_01);

alter table actions_show add constraint
  (foreign key (cur_page) references pages constraint "are".fk_actl_02);



--------------------------------------------------------
--координаты
--------------------------------------------------------
create table webdb.map_objects
( nzp_mo  serial not null,
  tip integer,
  kod integer,
  nzp_wp  integer,
  object_type integer,
  note    char(160)
);

create unique index webdb.ix_mo_1 on webdb.map_objects (nzp_mo);
create index webdb.ix_mo_2 on webdb.map_objects (tip, kod, nzp_wp);

create table webdb.map_points
( nzp_mp serial not null,
  nzp_mo integer not null,
  x       float,
  y       float,
  ordering integer
);

create unique index webdb.ix_mp_1 on webdb.map_points (nzp_mp);
create index webdb.ix_mp_2 on webdb.map_points (nzp_mo);

--ключ для Localhost
insert into map_objects (tip, note) values (-1, 'AMFVQU0BAAAAtxGSWQQAOoqHoUJaK82iDDgSVtmtnfS4WgwAAAAAAAAAAAAjB_JJ_HiAFsR91Y4Sd7H3B4ZnyQ==');
--ключ для stcline.ru
--insert into map_objects (tip, note) values (-1, 'ANxfQU0BAAAA-5JTKQIAZqDm6jpY7AMd5oxJ_0cDue0pX9EAAAAAAAAAAACfHJffTds2p14Rtjd4WeeUBjTFGA==');
--ключ для webkomplat.ru
--insert into map_objects (tip, note) values (-1, 'AFVXfE4BAAAAe1h1dAIAKsuSSyr1V3ynFH8zDvTG3Ji43MsAAAAAAAAAAAA-O7BneyqNN9SlkTHJoFyRsO3wCg==');

--координаты Зеленодольска
insert into map_objects (tip, note) values (-2, 'г. Зеленодольск');
insert into map_points (nzp_mo, x, y) select nzp_mo, 48.519507, 55.845776 from map_objects where tip = -2;

--координаты Казани
--insert into map_objects (tip, note) values (-2, 'г. Казань');
--insert into map_points (nzp_mo, x, y) select nzp_mo, 49.152336, 55.790825 from map_objects where tip = -2;

--------------------------------------------------------
--s_help
--------------------------------------------------------
create table webdb.s_help
( nzp_hlp  serial  not null,
  cur_page integer not null,
  tip      integer not null,
  kod      integer not null,
  sort     integer not null,
  hlp      char(255)
);

create unique index webdb.ix_hlp_1 on webdb.s_help (nzp_hlp);
create unique index webdb.ix_hlp_2 on webdb.s_help (cur_page,tip,kod,sort);

insert into s_help (nzp_hlp,cur_page,tip,kod,sort,hlp) values (1,0,0,0,1,
'Форма NAME_FORM состоит из заголовка формы, меню выбора режима работы, области "Действия" в правой части окна формы и полей для поиска информации.'
);
insert into s_help (nzp_hlp,cur_page,tip,kod,sort,hlp) values (2,0,0,0,2,
'Меню располагается в верхней части формы и предназначено для перехода между формами (режимами работы) программы. Выбор позиции меню формы производится нажатием левой кнопки мыши на наименовании позиции.'
);

insert into s_help (nzp_hlp,cur_page,tip,kod,sort,hlp) values (3,0,1,0,1,
'В меню формы расположены следующие позиции:'
);

insert into s_help (nzp_hlp,cur_page,tip,kod,sort,hlp) values (4,0,2,0,1,
'Область "Действия" располагается в правой части формы и позволяет выбрать следующие операции (выбор операции производится нажатием левой кнопки мыши на наименовании операции):'
);

insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,1,'Над таблицей расположена информационная панель.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,2,'Таблица состоит из следующих полей: ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,3,'"Дата с" - дата начала действия параметра, "Дата по" - дата окончания действия параметра, если это поле пусто, то параметр действует бессрочно, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,4,'"Значение" - значение параметра, "Дата изменения" - отображает дату создания или последней модификации значения параметра, а также имя пользователя, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,5,'"Дата удаления" - дата удаления значения параметра и имя пользователя, показывается, если отмечен параметр "показывать недействующие значения"');

insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,1,'Над панелью с таблицей расположена информационная панель, показывающая данные выбранной квартиры.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,2,'Над таблицей располагается панель, отображающая общее количество параметров, номер текущей страницы параметров и стрелки перехода к предыдущей и следующей странице (если данные разбиты на несколько страниц), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,3,'а также элементы управления для выбора количества записей, отображаемых на экране, и типа параметров.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,4,'Таблица включает в себя следующие колонки: "Наименование" - наименование параметра, "Вид" - вид параметра, "Значение в расчетном месяце", "Поставщик/услуга" - показывается для некоторых типов параметров.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,5,'Для выбора параметра кликните на строку таблицы.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,1,'Над панелью с таблицей расположена информационная панель, показывающая данные выбранного дома.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,2,'Над таблицей располагается панель, отображающая общее количество параметров, номер текущей страницы параметров и стрелки перехода к предыдущей и следующей странице (если данные разбиты на несколько страниц), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,3,'а также элементы управления для выбора количества записей, отображаемых на экране, и типа параметров.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,4,'Таблица включает в себя следующие колонки: "Наименование" - наименование параметра, "Вид" - вид параметра, "Значение в расчетном месяце", "Поставщик/услуга" - показывается для некоторых типов параметров.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,5,'Для выбора параметра кликните на строку таблицы.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,1,'Под меню располагается информационная панель, показывающая информацию о доме.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,2,'Ниже располагается строка параметров, позволяющая выбрать год, за который отображаются данные расчета ОДН, услугу и отобразить колонки с прошлыми расчетами.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,3,'В таблице отражены результаты расчета коэффициентов коррекции по постановлениям.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,1,'Под меню располагается информационная панель, показывающая информацию о лицевом счете.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,2,'Над таблицей располагается панель, отображающая общее количество недопоставок, номер текущей страницы и стрелки перехода к предыдущей и следующей странице (если данные разбиты на несколько страниц), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,3,'а также элементы управления для выбора количества записей, отображаемых на экране, и просмотра недействующих значений.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,4,'В таблице отражены недопоставки по услугам для выбранного лицевого счета.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,1,'Поиск адресов, имеющих заданные коэффициенты коррекции расходов дома, осуществляется по следующим параметрам:');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,2,'услуга, период (за который рассчитан коэффициент коррекции), коэффициент коррекции расхода для лицевых счетов с квартирными приборами учета, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,3,'коэффициент коррекции расхода по площади, расход домового прибора учета, сумма расходов по лицевым счетам с приборами учета, сумма расходов по лицевым счетам без приборов учета, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,4,'тип алгоритма расчета коэффициента коррекции.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,1,'Поиск по расходам и начислениям позволяет выполнять поиск адресов, имеющих заданные расходы и начисления по услугам в заданном месяце. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,2,'По каждой услуге возможен поиск по следующим параметрам: начислено, перерасчет, полный расчет, тариф, расчет, недопоставка, льгота, подневной расчет, входящее сальдо, изменения, оплата, исходящее сальдо, к оплате. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,3,'При вводе только одного значения в строке "С" значения параметра проверяется на равенство введенному значению, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,4,'при вводе двух значений в строках "С" и "ПО" значение параметра должно входить в указанный диапазон, включая границы диапазона. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,5,'При наличии условий поиска по нескольким услугам достаточно, чтобы выполнялись условия хотя бы одной услуге. ');





--------------------------------------------------------
--картинки
--------------------------------------------------------
--drop table webdb.img_lnk;

create table webdb.img_lnk
( nzp_img  serial  not null, --уникальный код
  cur_page integer not null, --код отображаемой страницы, либо 0 
  tip      integer not null, --1 - пункты меню, 2 - действия 
  kod      integer not null, --nzp_page - если tip=1, nzp_act - если tip=2
  img_url  char(255)         --имя файла картинки 
);

create unique index webdb.ix_img_lnk_1 on webdb.img_lnk (nzp_img);
create unique index webdb.ix_img_lnk_2 on webdb.img_lnk (cur_page,tip,kod);

insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 1, 'homepage.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 2, 'kmenuedit.png'); 
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 3, 'help.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 4, 'back.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 30, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 31, 'find_adr.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 32, 'find_har.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 33, 'find_nach.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 34, 'find_gil.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 35, 'find_pu.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 36, 'find_nedop.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 37, 'find_odn.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 40, 'docs_folder.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 41, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 42, 'dom.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 43, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 44, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 45, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 46, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 47, 'find.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 51, 'task.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 52, 'card.png');
insert into img_lnk (cur_page, tip, kod, img_url) select 0, 1, nzp_page, 'list.png' from pages where nzp_page in (53,54,55,56,57,59,61);
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 62, 'counter.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 63, 'counter_house.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 70, 'users.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 71, 'house.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 72, 'analize.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 81, 'aa.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 82, 'service.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 83, 'calculator.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 84, 'map.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 120, 'ooo_calc.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 121, 'saldo.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 122, 'calculator.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 123, 'opl.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 124, 'calculator.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 125, 'calculator.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 126, 'saldo.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 127, 'saldo.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 128, 'saldo.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 129, 'bill.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 949, 'nodata.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 1, 999, 'exit.png');
                                                                     
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 1, 'binoculars.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 2, 'edit_clear.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 3, 'folder_open.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 4, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 5, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 6, 'edit_clear.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 7, 'show_map.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 8, 'print.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 51, 'edit_state.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 61, 'save.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 62, 'add_new.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 63, 'edit_state.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 64, 'delete.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 65, 'calc32.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 66, 'refresh.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 67, 'archive.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 501, 'binoculars.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 503, 'binoculars.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 505, 'binoculars.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 2, 507, 'binoculars.png');

insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 10, 'specialist_rc.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 11, 'analitics.png');
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 12, 'system_lock.png');


--------------------------------------------------------
--Структуры безопасности
--------------------------------------------------------

set encryption password "IfmxPwd2";

alter table users modify 
 ( pwd   char(200) not null,
   uname char(200) not null
 );

alter table pages modify 
 ( page_url  char(200),
   page_menu char(200),
   page_name char(200)
 );

alter table s_actions modify 
 ( act_name char(200) not null
 );




update users
set pwd   = encrypt_aes(nzp_user||'-'||pwd),
    uname = encrypt_aes(uname)
where 1 = 1;

update pages
set page_url  = encrypt_aes(page_url),
    page_menu = encrypt_aes(page_menu),
    page_name = encrypt_aes(page_name),
    hlp       = encrypt_aes(hlp)
where 1 = 1;

update s_actions
set act_name = encrypt_aes(act_name),
    hlp      = encrypt_aes(hlp)
where 1 = 1;



alter table pages_show   add sign char(120);
alter table actions_show add sign char(120);

update pages_show
set sign = encrypt_aes(sort_kod||up_kod||page_url||cur_page||'-'||nzp_psh||'pages_show')
where 1 = 1; 

update actions_show
set sign = encrypt_aes(sort_kod||act_dd||act_tip||nzp_act||cur_page||'-'||nzp_ash||'actions_show')
where 1 = 1; 


--------------------------------------------------------
--Роли по-умолчанию
--------------------------------------------------------
create table webdb.s_roles
( nzp_role serial not null,
  role     char(120),
  page_url integer default 0,
  sort     integer default 0
);

create unique index webdb.ix_srls_1 on webdb.s_roles (nzp_role);


insert into webdb.s_roles values (10, encrypt_aes('Картотека'), 31, 1);
insert into webdb.s_roles values (11, encrypt_aes('Аналитика'), 81, 4);
insert into webdb.s_roles values (12, encrypt_aes('Администратор'), 151, 5);


--------------------------------------------------------
--Доступ к страницам по ролям
--------------------------------------------------------
create table webdb.roles
( nzp_rls  serial  not null,
  nzp_role integer not null,
  cur_page integer not null,
  tip      integer not null,
  kod      integer not null,
  sign     char(120),
  mod_act  integer
);

create unique index webdb.ix_rls_1 on webdb.roles (nzp_rls);
create unique index webdb.ix_rls_2 on webdb.roles (nzp_role,cur_page,tip,kod);
create index webdb.ix_rls_3 on webdb.roles (cur_page);

--Специалист РЦ (полный доступ)
insert into webdb.roles
select 0,b.nzp_role,a.cur_page,1,a.page_url,'',cast(null as integer)
from pages_show a, s_roles b
where 1 = 1;

insert into webdb.roles
select 0,nzp_role,cur_page,2,nzp_act,'',cast(null as integer)
from actions_show, s_roles
where 1 = 1;



--Доступы к АРМам
insert into webdb.roles
select 0,nzp_role,0,0,0,'',cast(null as integer)
from s_roles where 1 = 1;



update webdb.roles
set sign = encrypt_aes(tip||kod||cur_page||nzp_role||'-'||nzp_rls||'roles')
where 1 = 1;




--------------------------------------------------------
--Принадлежность пользователей по ролям
--------------------------------------------------------
create table webdb.userp
( nzp_usp  serial  not null,
  nzp_role integer not null,
  nzp_user integer not null,
  sign     char(90)
);

create unique index webdb.ix_usp_1 on webdb.userp (nzp_usp);
create unique index webdb.ix_usp_2 on webdb.userp (nzp_role,nzp_user);

--Полный доступ
insert into webdb.userp
select 0,nzp_role,nzp_user,''
from users a, s_roles b 
where 1 = 1;


--Подпись
update webdb.userp
set sign = encrypt_aes(nzp_user||nzp_role||'-'||nzp_usp||'userp')
where 1 = 1;



--------------------------------------------------------
--Ограничения ролей
--------------------------------------------------------
create table webdb.rolesval
( nzp_rlsv serial  not null,
  nzp_role integer not null,
  tip      integer not null,
  kod      integer not null,
  val      char(255)
);

create unique index webdb.ix_rlsv_1 on webdb.rolesval (nzp_rlsv);
create unique index webdb.ix_rlsv_2 on webdb.rolesval (nzp_role,tip,kod);



--------------------------------------------------------
--Ограничения ключей
--------------------------------------------------------
create table webdb.roleskey
( nzp_rlsv serial  not null,
  nzp_role integer not null,
  tip      integer not null,
  kod      integer not null,
  sign     char(90)
);

create unique index webdb.ix_rlsk_1 on webdb.roleskey (nzp_rlsv);
create unique index webdb.ix_rlsk_2 on webdb.roleskey (nzp_role,tip,kod);




--------------------------------------------------------
--Список сессий
--------------------------------------------------------
create table webdb.log_sessions
( nzp_ses   serial not null,
  nzp_user  integer default 0,
  dat_log   datetime year to minute,
  ip_log    char(20), 
  browser   char(20),
  idses     char(30)
);

create unique index webdb.ix_lses_1 on log_sessions (nzp_ses);
create        index webdb.ix_lses_2 on log_sessions (nzp_user,dat_log,idses);
create        index webdb.ix_lses_3 on log_sessions (idses);




--------------------------------------------------------
--Системная информация портала
--------------------------------------------------------
create table informix.sysprtdata
( id_prtd   serial not null,
  num_prtd  integer not null,
  val_prtd  char(255)
);

create unique index informix.ix_prtd_1 on sysprtdata (id_prtd);
create 	      index informix.ix_prtd_2 on sysprtdata (num_prtd);

-- num_prtd - идентификатор
--	1  - наименование организации
--	11 - количество подключений
--	20 - портальный сетрификат
--	21 - клиентский сертификат

-- val_prtd - значение



set encryption password "IfmxPwd2";


delete from sysprtdata where 1=1;
insert into sysprtdata values (0,11,encrypt_aes('cnt'||'-'||'3000'));
insert into sysprtdata values (0,20,encrypt_aes('prt'||'-'||'www.stcline.ru'));
--insert into sysprtdata values (0,21,encrypt_aes('cli'||'-'||'00D1')); --https


alter table users add email char(200);
alter table users add is_blocked nchar(1);



--------------------------------------------------------
--Добавление таблицы запросов на восстановление паролей
--drop table webdb.s_setups;
 
create table webdb.s_setups
( nzp_setup serial not null,
    nzp_param integer,
    param_name char(250),
    param_type char(50),
    value_ char(250),
    nzp_user integer,
    dat_when datetime year to second 
);

create unique index webdb.ix_s_setups_1 on webdb.s_setups(nzp_setup);
create unique index webdb.ix_s_setups_2 on webdb.s_setups(nzp_param);
create unique index webdb.ix_s_setups_3 on webdb.s_setups(param_name);

insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 1, 'SMTP-сервер', 'char', 'mail.stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 2, 'Порт для отправки почты', 'char', '25',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 3, 'Имя пользователя для почтового сервера ', 'char', 'portal@stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 4, 'Пароль пользователя для почтового сервера', 'char', 'stcline',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 5, 'Наименование отправителя для исходящих сообщений', 'char', 'STC Line',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 6, 'Email отправителя', 'char', 'portal@stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 7, 'Работать только с центральным банком данных', 'bool', '2', null, null);


set encryption password "IfmxPwd2";

update s_setups set param_name = encrypt_aes(param_name)
, param_type = encrypt_aes(param_type)
, value_ = encrypt_aes(value_);

--------------------------------------------------------
--Добавление таблицы report
--------------------------------------------------------
create table webdb.report 
   ( nzp_rep  serial not null,
     nzp_act integer, 
     name char(50),
     file_name char(50)
   );
   
 create unique index webdb.ix_report_1 on webdb.report(nzp_rep);

--------------------------------------------------------
update statistics;
--------------------------------------------------------



database sysmaster;





