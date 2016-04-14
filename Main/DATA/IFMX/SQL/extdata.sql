database websmr2;



CREATE PROCEDURE tshu_drp()
  on exception return; 
  end exception with resume 
  
  drop table ext_mm;
  drop table ext_pm;

  delete from roleskey where tip in (201, 202);

END PROCEDURE;
EXECUTE PROCEDURE tshu_drp();
DROP PROCEDURE tshu_drp;


--------------------------------------------------------
--пункты главного меню подсистемы
--------------------------------------------------------
create table webdb.ext_mm
( nzp_mm  serial(1) not null,
  mm_text char(20),
  mm_sort integer   --сортировка
);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (1,"Лицевой счет", 1);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (2,"Дом", 2);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (3,"Отчеты", 3);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (4,"Карточки жильцов", 4);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (5,"Доступ", 5);

create unique index webdb.ix1_ext_mm on webdb.ext_mm (nzp_mm);



--------------------------------------------------------
--подпункты главного меню подсистемы
--------------------------------------------------------
create table webdb.ext_pm
( nzp_pm     serial(1) not null,
  nzp_mm     integer not null, 	-->ext_mm.nzp_mm
  pm_text    char(40), 		--text, например 'Список лицевых счетов'
  pm_action  char(40), 		--action, например 'on_AccountList', 'on_' добавляется при записи
  pm_control char(40), 		--контроллер, например 'account.AccountList'
  pm_sort    integer   		--сортировка
);

--лс
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (1,1,'Шаблон поиска','AccountSearch','account.AccountSearch', 1);

insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (2,1,'Список лицевых счетов','AccountList','account.AccountList', 2);

--дом
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (3,2,'Шаблон поиска','AccountSearch','account.AccountSearch', 1);

insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (4,2,'Список домов','HouseList','house.HouseList', 2);

--отчеты
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (5,3,'Отчеты по списку лицевых счетов','AccountSearch','account.AccountSearch', 1);

--карточки жильцов
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (6,4,'Шаблон поиска по жителям','FindGil','gil.FindGil', 1);

insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (7,4,'Список карточек жителей','GilList','gil.GilList', 2);

--доступ
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (8,5,'Пользователи','Users','admin.Users', 1);

create unique index webdb.ix1_ext_pm on webdb.ext_pm (nzp_pm);
create 	      index webdb.ix2_ext_pm on webdb.ext_pm (nzp_mm, pm_sort);


--------------------------------------------------------
--ограничение по ролям для ext_mm (tip=201)
--------------------------------------------------------
insert into roleskey (nzp_role,tip,kod) values (10, 201, 1);  --Картотека:Лицевой счет
insert into roleskey (nzp_role,tip,kod) values (10, 201, 2);  --Картотека:Дом
insert into roleskey (nzp_role,tip,kod) values (10, 201, 3);  --Картотека:Отчеты
insert into roleskey (nzp_role,tip,kod) values (12, 201, 5);  --Администратор:Доступ
insert into roleskey (nzp_role,tip,kod) values (14, 201, 4);  --Паспортистка:Карточки жильцов

--------------------------------------------------------
--ограничение по ролям для ext_pp (tip=202)
--------------------------------------------------------
--insert into roleskey (nzp_role,tip,kod) values (10, 202, 1);  --Шаблон поиска
--insert into roleskey (nzp_role,tip,kod) values (10, 202, 2);  --Список лицевых счетов
--insert into roleskey (nzp_role,tip,kod) values (10, 202, 3);  --Шаблон поиска
--insert into roleskey (nzp_role,tip,kod) values (10, 202, 4);  --Список домов



set encryption password "IfmxPwd2";

update webdb.roleskey
set sign = encrypt_aes(tip||kod||nzp_role||'-'||nzp_rlsv||'roles')
where tip in (201,202);



--------------------------------------------------------
--ограничение по id (rolesval)
--------------------------------------------------------
-- nzp_role
-- tip = 211
-- kod = номер Ext-window
-- val = Id Ext-window




--------------------------------------------------------
update statistics;
--------------------------------------------------------

database sysmaster;

