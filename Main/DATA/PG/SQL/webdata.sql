set search_path to public;

--------------------------------------------------------
--пользователи
--------------------------------------------------------
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




--65 код занят для списка жильцов для komplat 3.0



-- 132 для WebKomplat3
--INSERT INTO webdb.pages (NZP_PAGE, PAGE_URL, PAGE_MENU, PAGE_NAME, HLP)
--VALUES (132, '~/general/basepage/baselist.aspx', 'Распределение оплат по услугам', 'Распределение оплат по услугам', 'переходит к распределению оплат лицевого счета по услугам');







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
--checkboxlist
--------------------------------------------------------


                                             
                                             
--------------------------------------------------------
--dropdownlist"s (1,2)
--------------------------------------------------------









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





