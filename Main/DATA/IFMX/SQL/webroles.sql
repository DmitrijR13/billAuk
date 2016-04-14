database webpriv;

set encryption password "IfmxPwd2";

delete from roleskey  where nzp_role > 999;
delete from roles     where nzp_role > 999;
delete from s_roles   where nzp_role > 999;
delete from userp     where nzp_user > 999;
delete from users     where nzp_user > 999;


--full system
insert into webdb.s_roles values (1000, 'Картотека', 0,1000);


insert into webdb.s_roles 
select unique nzp_area + 1000, 'УК'||' '||nzp_area + 1000, 0, nzp_area + 1000
from anl2011;

insert into webdb.users (nzp_user,login,pwd,uname)
select unique nzp_area + 1000,'uk'||nzp_area + 1000,'ukw'||nzp_area + 1000,'uk'||nzp_area + 1000
from anl2011;



insert into webdb.s_roles 
select unique nzp_supp + 10000, 'Поставщик'||' '||nzp_supp, 0, nzp_supp + 10000
from anl2011;

insert into webdb.users (nzp_user,login,pwd,uname)
select unique nzp_supp + 10000,'pu'||nzp_supp + 10000,'puw'||nzp_supp + 10000,'pu'||nzp_supp + 10000
from anl2011;



update users
set uname = ( select max(area) from anl2011_supp where nzp_user = nzp_area + 1000 and area is not null)
where nzp_user > 999 
  and nzp_user in ( select nzp_area + 1000 from anl2011_supp where area is not null);

update s_roles
set role = ( select max(area) from anl2011_supp where nzp_role = nzp_area + 1000 and area is not null )
where nzp_role > 999 
  and nzp_role in ( select nzp_area + 1000 from anl2011_supp where area is not null);


update users
set uname = ( select max(name_supp) from anl2011_supp where nzp_user = nzp_supp + 10000 and name_supp is not null)
where nzp_user > 999 
  and nzp_user in ( select nzp_supp + 10000 from anl2011_supp where name_supp is not null);

update s_roles
set role = ( select max(name_supp) from anl2011_supp where nzp_role = nzp_supp + 10000 and name_supp is not null )
where nzp_role > 999 
  and nzp_role in ( select nzp_supp + 10000 from anl2011_supp where name_supp is not null);




update s_roles
set role   = encrypt_aes(role)
where nzp_role > 999;

update users
set pwd   = encrypt_aes(nzp_user||'-'||pwd),
    uname = encrypt_aes(uname)
where nzp_user > 999;




update webdb.roles
set sign = encrypt_aes(tip||kod||cur_page||nzp_role||'-'||nzp_rls||'roles')
where 1 = 1;




--доступы по пользователям
insert into webdb.userp
select 0,nzp_role,nzp_user,''
from users a, s_roles b 
where nzp_role > 999
  and nzp_role = nzp_user;

insert into webdb.userp
select unique 0,nzp_role,nzp_user,''
from users a, s_roles b 
where nzp_user > 999
  and nzp_role = 11;

--Подпись
update webdb.userp
set sign = encrypt_aes(nzp_user||nzp_role||'-'||nzp_usp||'userp')
where nzp_user > 999;



drop table roleskey;

create table webdb.roleskey
( nzp_rlsv serial  not null,
  nzp_role integer not null,
  tip      integer not null,
  kod      integer not null,
  sign     char(90)
);

create unique index webdb.ix_rlsk_1 on webdb.roleskey (nzp_rlsv);
create unique index webdb.ix_rlsk_2 on webdb.roleskey (nzp_role,tip,kod);

--УК
--nzp_wp (101)
insert into roleskey (nzp_role,tip,kod)
select unique nzp_area + 1000, 101, nzp_wp
from anl2011;

--nzp_area (102)
insert into roleskey (nzp_role,tip,kod)
select unique nzp_area + 1000, 102, nzp_area
from anl2011;

--nzp_geu (103)
insert into roleskey (nzp_role,tip,kod)
select unique nzp_area + 1000, 103, nzp_geu
from anl2011;

--nzp_supp (120)
--insert into roleskey (nzp_role,tip,kod)
--select unique nzp_area + 1000, 120, nzp_supp
--from anl2011;

--nzp_serv (121)
--insert into roleskey (nzp_role,tip,kod)
--select unique nzp_area + 1000, 121, nzp_serv
--from anl2011;




--ПУ
--nzp_wp (101)
insert into roleskey (nzp_role,tip,kod)
select unique nzp_supp + 10000, 101, nzp_wp
from anl2011;

--nzp_area (102)
--insert into roleskey (nzp_role,tip,kod)
--select unique nzp_supp + 10000, 102, nzp_area
--from anl2011;

--nzp_geu (103)
--insert into roleskey (nzp_role,tip,kod)
--select unique nzp_supp + 10000, 103, nzp_geu
--from anl2011;

--nzp_supp (120)
insert into roleskey (nzp_role,tip,kod)
select unique nzp_supp + 10000, 120, nzp_supp
from anl2011;

--nzp_serv (121)
insert into roleskey (nzp_role,tip,kod)
select unique nzp_supp + 10000, 121, nzp_serv
from anl2011;


--full system
select unique tip,kod from roleskey
into temp ttt;

insert into roleskey (nzp_role,tip,kod)
select 1000, tip,kod
from ttt;

drop table ttt;


update webdb.roleskey
set sign = encrypt_aes(tip||kod||nzp_role||'-'||nzp_rlsv||'roles')
where 1 = 1;





--nzp_wp
--insert into webdb.rolesval (nzp_rlsv,nzp_role,tip,kod,val)
--values (0,1001,101,101,encrypt_aes('13') );

--area
--insert into webdb.rolesval (nzp_rlsv,nzp_role,tip,kod,val)
--values (0,1001,101,102,encrypt_aes('56') );

--поставщик вода
--insert into webdb.rolesval (nzp_rlsv,nzp_role,tip,kod,val)
--values (0,1101,101,120,encrypt_aes('115') );

--услуги вода
--insert into webdb.rolesval (nzp_rlsv,nzp_role,tip,kod,val)
--values (0,1101,101,121,encrypt_aes('6,9,200,203,253') );



--услуги
--insert into webdb.roles2 (nzp_rlsv,nzp_role,tip,kod,val)
--values (0,2,101,102,encrypt_aes('nzp_serv in (25,210,11,242)') );

--поставщик
--insert into webdb.roles2 (nzp_rls2,nzp_role,tip,kod,val)
--values (0,2,101,101,encrypt_aes('nzp_supp in (1)') );







--------------------------------------------------------
--удалить пустые ссылки и недействующие 
--------------------------------------------------------
delete from roles where nzp_role = 11 and tip=2 and kod in 
(
  65
);


delete from roles where cur_page in 
(
  12,13,14,15,17, 32,34,36, 43,44,45,46,47, 60, 125, 949,951,952, 53,62,63, 128
);
delete from roles where tip=1 and kod in 
(
  12,13,14,15,17, 32,34,36, 43,44,45,46,47, 52,56,60, 125, 949,951,952, 53,62,63, 128
);

delete from roles where tip=2 and kod in
(
  4,6,51,61,62,62,63,64,611, 32,33,34,36, 502,504,506, 7
);
delete from roles where cur_page=31 and tip=1 and kod in 
(
  51,54,56
);
delete from roles where cur_page=31 and tip=2 and kod in 
(
  502,504,506
);

delete from roles where cur_page=35 and tip=1 and kod in 
(
  51,54
);


--выставим задания на расчет 2010 и 2011 годов
 drop table saldo_fon;
 create table webdb.saldo_fon 
 ( nzp_key   serial  not null,
   nzp_area  integer default 0 not null, 
   year_     integer default 0 not null, 
   month_    integer default 0 not null, 
   kod_info  integer default 0 not null, 
   dat_in    datetime year to minute, 
   dat_work  datetime year to minute
   dat_out   datetime year to minute, 
   txt       char(255) 
 );

 create unique index webdb.ix_sfon_1 on saldo_fon (nzp_key) ;
 create        index webdb.ix_sfon_2 on saldo_fon (nzp_area,year_,month_,kod_info) ;
 create        index webdb.ix_sfon_3 on saldo_fon (kod_info) ;

 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,1,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,2,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,3,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,4,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,5,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,6,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,7,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,8,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,9,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,10,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,11,3,current
 from anl2011;
 insert into saldo_fon (nzp_area,year_,month_,kod_info,dat_in)
 select unique nzp_area ,2010,12,3,current
 from anl2011;

 update statistics for table saldo_fon ;

