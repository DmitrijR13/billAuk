database sysmaster;

--------------------------------------------------------
drop database websmr;
create database websmr with log;
--------------------------------------------------------

grant dba to webdb;
grant dba to public;


--------------------------------------------------------
--������������
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
values (1,"websystem","123","��������� ������������");

insert into webdb.users (nzp_user,login,pwd,uname)
values (2,"webuserc","userc","������������ ��");


create unique index webdb.ix_us_1 on webdb.users (nzp_user);
create unique index webdb.ix_us_2 on webdb.users (login,pwd);


--------------------------------------------------------
--���������� �������
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
values (1, "������� ��������","~/default.aspx", "������� ��������", "������������ ��� �������� �� ������� �������� ���������");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (2, "����:","�������� � ���� ��������� ������:");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (3, "������","������������ ��� �������� �� �������� �������� � ���������");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (4, "���������� ��������","������������ ��� �������� �� ���������� �������� ���������");





insert into webdb.pages (nzp_page,page_menu,hlp)
values (30, "������� ������","������������ ������� � ������ ��� ������ ���������� � ���������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (31, "����� �� ������","~/kart/adres/findls.aspx","����� �� ������","��������� ����� ������ �� ������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (32, "����� �� ����������","~/kart/prm/findprm.aspx","����� �� ��������������� �����","��������� ����� ������ �� ��������������� �����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (33, "����� �� �����������","~/kart/charge/findch.aspx","����� �� �����������","��������� ����� ������ �� �����������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (34, "����� �� �������","~/kart/gil/findgil.aspx","����� �� ������������ ������","��������� ����� �� ������������ ������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (35, "����� �� ����������","~/kart/counter/findcnt.aspx","����� �� ���������� �������� �����","��������� ����� �� ������ �������� �����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (36, "����� �� �������������","~/kart/nedo/findnd.aspx","����� �� �������������","��������� ����� ������ �� ���������������� �������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (37, "����� �� ���","~/kart/charge/findodn.aspx","����� �� ������������� ��������� �������� ����","��������� ����� ������ �� �������� ��� ����������� ����");



insert into webdb.pages (nzp_page,page_menu,hlp)
values (40, "��������� ������","������������ ������� �� ����� ��������� ������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (41, "������� �����","~/general/basepage/baselist.aspx","������ ������� ������","��������� �� ����� ��������� ������ ������� ������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (42, "����","~/general/basepage/baselist.aspx","������ �����","��������� �� ����� ��������� ������ �����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (43, "�����","~/general/basepage/baselist.aspx","������ ����","��������� �� ������ ������� ����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (44, "����������","~/general/basepage/baselist.aspx","������ ����������","��������� �� ������ ���������� (��)");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (45, "���������","~/general/basepage/baselist.aspx","������ ���������","��������� �� ������ ��������������� ���������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (46, "����� ������","~/general/basepage/baselist.aspx","������ ������ ������","��������� �� ������ ������ ������");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (47, "���� �����", "~/general/basepage/baselist.aspx", "������ ����� �����","��������� �� ������ ����� ��������� �����");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (48, "�������������� �����", "~/kart/prm/spisprm.aspx", "�������������� �����","��������� �� ������ ������������� �����");


insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (51, "�������������� �����","~/kart/prm/spisprm.aspx","�������������� �����","��������� �� ������ ������������� ������ ���������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (52, "������������ ��������","~/general/basepage/baselist.aspx","������������ ��������","��������� �� ������������ ��������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (53, "������� �����","~/kart/counter/counters.aspx","������ �������� �����","��������� �� ������ ��������� �������� �����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (54, "��������� �������� �����","~/kart/counter/spisval.aspx","������ ��������� �������� �����","��������� �� ������ ��������� �������� ����� ��� ��������� ��������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (55, "������������","~/kart/nedo/spisnd.aspx","������ ������������","��������� �� ������ ������������");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (56, "�������� �������", "~/general/basepage/baselist.aspx", "������ �������� �������","��������� �� ��������� ������ �������� �������");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (57, "������� ����� ����", "~/general/basepage/baselist.aspx", "������ ������� ������ ����","��������� �� ������ ������� ������ ������� ����");



insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (59, "��������� ����� ����", "~/kart/prm/spisprm.aspx", "������ ���������� ����� ����","��������� �� ������ ������������� ������� ����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (61, "��������� ������� �������� �����", "~/kart/counter/spisval.aspx", "������ ��������� ������� �������� �����","��������� �� ������ ��������� �������� ����� ��� ���������� ����");


insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (62, "���������� ������� �����","~/general/basepage/baselist.aspx","���������� ������� �����","��������� �� ������ �������� ����� ��� ��������� ��������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (63, "������� ������� �����","~/general/basepage/baselist.aspx","������� ������� �����","��������� �� ������ �������� ����� ��� ���������� ����");

insert into pages (nzp_page, page_menu, page_url, page_name,hlp)
values (64, "�������� ���������", "~/kart/prm/prm.aspx", "�������� ���������","��������� �� ������� ��� ����� �������� ���������");

--65 ��� ����� ��� ������ ������� ��� komplat 3.0

insert into webdb.pages (nzp_page,page_menu,hlp)
values (70, "������ �� ��������","������������ ������� � ������ ��������");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (71, "������ �� ����","������������ ������� � ������ ����");

insert into webdb.pages (nzp_page,page_menu,hlp)
values (72, "���������","������������ ������� � ������������� ������");



insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (81, "�������� ������������","~/general/basepage/aa.aspx","�������� ������������","��������� � ��������� ��������� ������������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (82, "���������� �����","~/general/basepage/as.aspx","���������� �����","��������� � ��������� ����������� �����");


insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (83, "����������","~/general/basepage/aa.aspx","����������","��������� � ��������� ���������� � ������� ������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (84, "������������� �����","~/general/basepage/baselist.aspx","������������� �����","������������ ������� � ����� �������");



insert into webdb.pages (nzp_page,page_menu,hlp)
values (120, "����������","������������ ������� � �����������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (121, "������ �� �������� �����","~/general/basepage/baselist.aspx","������ �� �������� �����","������������ ������� � ������ �� �������� �����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (122, "������ ����������","~/kart/charge/charges.aspx","������ ����������","������������ ������� � ����������� �������� �����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (123, "������ ��������","~/kart/charge/listpays.aspx","������ �������� ������������","������������ ������� � �������� �������� �����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (124, "������ ���","~/kart/charge/odn.aspx","������ ���","������������ ������� � ������� ��� ���������� ����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (125, "������","~/kart/charge/calc.aspx","������ ����������","������������ ������� � ����������� ���������� ����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (126, "������ �� ����","~/general/basepage/baselist.aspx","������ �� ����","������������ ������� � ������ ���������� �� ���������� ����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (127, "������ �� �������������","~/general/basepage/baselist.aspx","������ �� �������������","������������ ������� � ������ ���������� �� ��������� �������������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (128, "������ �� �������","~/general/basepage/baselist.aspx","������ �� �������","������������ ������� � ������ ���������� �� ���������� �������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (129, "����-�������","~/kart/bill/bill.aspx","����-�������","��������� � ����-������� �������� �����");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (130, "������ �� ����������","~/general/basepage/baselist.aspx","������ �� ����������","������������ ������� � ������ ���������� �� ���������� ����������");

insert into webdb.pages (nzp_page,page_menu,page_url,page_name,hlp)
values (131, "����-�������","~/kart/bill/billrt.aspx","����-�������","��������� � ����-������� �������� �����");

-- 132 ��� WebKomplat3
--INSERT INTO webdb.pages (NZP_PAGE, PAGE_URL, PAGE_MENU, PAGE_NAME, HLP)
--VALUES (132, '~/general/basepage/baselist.aspx', '������������� ����� �� �������', '������������� ����� �� �������', '��������� � ������������� ����� �������� ����� �� �������');

insert into webdb.pages (nzp_page,page_menu,page_name)
values (949, "��� ������", "��� ������");


insert into webdb.pages (nzp_page,page_menu,hlp)
values (999, "��������� �����","������������ ��� ���������� ������ � ���������");




--------------------------------------------------------
--������� ����: ��������� �������
--------------------------------------------------------
create table webdb.pages_show
( nzp_psh   serial  not null,
  cur_page  integer not null,           --������� ��������
  page_url  integer default 0 not null, --���������� nzp_page>0
  up_kod    integer default 0 not null, --������ �� ���������
  sort_kod  integer default 0 not null  --������� �����������
);

create unique index webdb.ix_pagesh_1 on pages_show (nzp_psh);
create unique index webdb.ix_pagesh_2 on pages_show (cur_page, page_url, up_kod);



--------------------------------------------------------
--���������� ��������
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
insert into webdb.s_actions (nzp_act,act_name,hlp) values (1, "��������� �����"  ,"��������� ����� ����� ���������� ��������� ����� ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (2, "�������� ������"  ,"������� ����� ����������� ���� �������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (3, "������� ������"   ,"��������� ��������� ������ �� �������� ��� ��� ��������������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (4, "�������� ������"  ,"��������� ������ � ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (5, "�������� ������"  ,"��������� ����� ��������� ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (6, "�������� ������"  ,"������� ����� ����������� ���� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (7, "�������� �����"   ,"���������� ������������� ����� �ndex");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (8, "�� ������"        ,"��������� ����� ��� ������");

insert into webdb.s_actions (nzp_act,act_name,hlp) values (51,"�������� ���������", "�������� ��������� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (61,"��������� ���������","��������� ��������� ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (62,"�������� ��",        "��������� ������ ������ ����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (63,"������������� ��",   "����������� ������ ������� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (64,"������� ��",         "������� ������ ������� �����");

insert into webdb.s_actions (nzp_act,act_name,hlp) values (65,"��������� �������",    "��������� ������� �������������� ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (66,"�������� ������",      "��������� ���������� ������� ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (67,"�������� ��� ��������","���������� ��� �������� ������ ���������");


--------------------------------------------------------
--checkboxlist
--------------------------------------------------------
insert into webdb.s_actions (nzp_act,act_name,hlp) values (501,"������ �� �������",    "��������� ��� ������ ������, ��������� � ������� ������ �������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (502,"������ �� ����������", "��������� ��� ������ ������, ��������� � ������� ������ ������������� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (503,"������ �� �����������","��������� ��� ������ ������, ��������� � ������� ������ ���������� � ��������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (504,"������ �� �������",    "��������� ��� ������ ������, ��������� � ������� ������ �������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (505,"������ �� ����������", "��������� ��� ������ ������, ��������� � ������� ������ ��������� �������� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (506,"������ ������������",  "��������� ��� ������ ������, ��������� � ������� ������ ������������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (507,"������ ���",           "��������� ��� ������ ������, ��������� � ������� ������ �������");


insert into webdb.s_actions (nzp_act,act_name,hlp) values (520,"�� �������",    "��������� ������� ������ � ���������� �������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (521,"�� �������",    "��������� ������� ������ � ������� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (522,"�� �����������","��������� ������� ������ � ������� �����������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (523,"�� ��������",    "��������� ������� ������ ������� ������ ������� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (524,"�� �����������","��������� ������� ������ ������� ��");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (525,"�� ����������",    "��������� ������� ������ � ������� ��������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (526,"�� ������ ������","��������� ������� ������ � ������� ������ ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (527,"�� �����",    "��������� ������� ������ � ��������� �������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (528,"���������",    "��������� ����������� ��������� ����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (529,"��������/������ �����","");
                                             
                                             
--------------------------------------------------------
--dropdownlist"s (1,2)
--------------------------------------------------------
insert into webdb.s_actions (nzp_act,act_name,hlp) values (601,"����������� �� ������",    "��������� ������ �� ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (602,"����������� �� ��",    "��������� ������ �� ������� ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (603,"����������� �� �����",    "��������� ������ �� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (604,"����������� �� ����������","��������� ������ �� ��������������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (605,"����������� �� ������",    "��������� ������ �� �������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (606,"����������� �� ����������","��������� ������ �� �����������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (607,"����������� �� �����",    "��������� ������ �� �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (610,"�� ��������",        "��������� ������ �� ��������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (611,"��� ���������",        "��������� ������ ��� ��������������");


insert into webdb.s_actions (nzp_act,act_name,hlp) values (701,"�������� �� 20 �������","�������� ������ �� 20 �������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (702,"�������� �� 50 �������","�������� ������ �� 50 �������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (703,"�������� �� 100 �������","�������� ������ �� 100 �������");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (721,"����������� �������� / ����� / ���",  "���������� ������ � ������� ����������� ��������");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (722,"���� ������ / ������� / ����� / ���", "���������� ������ � ������� ������ ������");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (723,"����� / ���",                           "���������� ������ � ������� ����");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (724,"��������� / ������ / �������",        "���������� ������ � ������� ����������� �����");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (725,"������ / ��������� / ����������� �������� / ������� ","���������� ������ � ������� �����");

insert into webdb.s_actions (nzp_act,act_name,hlp) 
   values (726,"����������� �������� / ��������� / ������ / �������", "���������� ������ � ������� �� � �����������");


insert into webdb.s_actions (nzp_act,act_name,hlp) values (801,"����� �����",        "��������� ����� ����� ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (802,"����� �� ��������� ��",    "��������� ����� ������ �� ����� ���������� ������ ������");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (803,"����� �� ��������� �����","��������� ����� ������ �� ����� ���������� ������ �����");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (851,"�������� �������",    "");
insert into webdb.s_actions (nzp_act,act_name,hlp) values (852,"�������� �������",    "");




--------------------------------------------------------
--���� ��������: ����������� 
--------------------------------------------------------
create table webdb.actions_show
( nzp_ash   serial  not null,
  cur_page  integer not null,           --������� ��������      
  nzp_act   integer default 0 not null, --��������
  act_tip   integer default 0 not null, --��� �����������
  act_dd    integer default 0 not null, --�������������� �����������
  sort_kod  integer default 0 not null  --������� �����������   
);

create unique index webdb.ix_actsh_1 on actions_show (nzp_ash);
create unique index webdb.ix_actsh_2 on actions_show (cur_page,nzp_act,act_tip,act_dd);



--------------------------------------------------------
--���� ��������: ��������� 
--------------------------------------------------------
create table webdb.actions_lnk
( nzp_al    serial  not null,
  cur_page  integer not null,           --������� ��������      
  nzp_act   integer default 0 not null, --��������
  page_url  integer default 0 not null  --������ �� ���������� ��������
);

create unique index webdb.ix_actl_1 on actions_lnk (nzp_al);
create        index webdb.ix_actl_2 on actions_lnk (cur_page,nzp_act);



delete from pages_show   where 1 = 1;
delete from actions_show where 1 = 1;
delete from actions_lnk  where 1 = 1;


--------------------------------------------------------
--��� ������
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
--spispu.aspx ������ ��������
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
--���������� ��
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
--������� ��
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
--spisval.aspx ��������� ������� ��
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
--spisprm.aspx ��������� ���������
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
--spisnd.aspx ���������� ������������
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
--spisprm.aspx ������� ���������
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






--��������� ����� ����������
update pages_show
set sort_kod = 201
where page_url in (72) and up_kod = 0; 


--����� � ����� ����� �������
update pages_show
set up_kod = 950
where page_url > 950 ;

delete from pages_show where page_url = 950;

update pages_show
set up_kod = 2
where up_kod = 950 ;



--------------------------------------------------------
--�������������� action"s
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
-- ���-������ �������
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
-- ���-������ ��������
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
-- ���-������ ������� (������)
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
--�����������
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
--����������
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

--���� ��� Localhost
insert into map_objects (tip, note) values (-1, 'AMFVQU0BAAAAtxGSWQQAOoqHoUJaK82iDDgSVtmtnfS4WgwAAAAAAAAAAAAjB_JJ_HiAFsR91Y4Sd7H3B4ZnyQ==');
--���� ��� stcline.ru
--insert into map_objects (tip, note) values (-1, 'ANxfQU0BAAAA-5JTKQIAZqDm6jpY7AMd5oxJ_0cDue0pX9EAAAAAAAAAAACfHJffTds2p14Rtjd4WeeUBjTFGA==');
--���� ��� webkomplat.ru
--insert into map_objects (tip, note) values (-1, 'AFVXfE4BAAAAe1h1dAIAKsuSSyr1V3ynFH8zDvTG3Ji43MsAAAAAAAAAAAA-O7BneyqNN9SlkTHJoFyRsO3wCg==');

--���������� �������������
insert into map_objects (tip, note) values (-2, '�. ������������');
insert into map_points (nzp_mo, x, y) select nzp_mo, 48.519507, 55.845776 from map_objects where tip = -2;

--���������� ������
--insert into map_objects (tip, note) values (-2, '�. ������');
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
'����� NAME_FORM ������� �� ��������� �����, ���� ������ ������ ������, ������� "��������" � ������ ����� ���� ����� � ����� ��� ������ ����������.'
);
insert into s_help (nzp_hlp,cur_page,tip,kod,sort,hlp) values (2,0,0,0,2,
'���� ������������� � ������� ����� ����� � ������������� ��� �������� ����� ������� (�������� ������) ���������. ����� ������� ���� ����� ������������ �������� ����� ������ ���� �� ������������ �������.'
);

insert into s_help (nzp_hlp,cur_page,tip,kod,sort,hlp) values (3,0,1,0,1,
'� ���� ����� ����������� ��������� �������:'
);

insert into s_help (nzp_hlp,cur_page,tip,kod,sort,hlp) values (4,0,2,0,1,
'������� "��������" ������������� � ������ ����� ����� � ��������� ������� ��������� �������� (����� �������� ������������ �������� ����� ������ ���� �� ������������ ��������):'
);

insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,1,'��� �������� ����������� �������������� ������.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,2,'������� ������� �� ��������� �����: ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,3,'"���� �" - ���� ������ �������� ���������, "���� ��" - ���� ��������� �������� ���������, ���� ��� ���� �����, �� �������� ��������� ���������, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,4,'"��������" - �������� ���������, "���� ���������" - ���������� ���� �������� ��� ��������� ����������� �������� ���������, � ����� ��� ������������, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (64,3,0,5,'"���� ��������" - ���� �������� �������� ��������� � ��� ������������, ������������, ���� ������� �������� "���������� ������������� ��������"');

insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,1,'��� ������� � �������� ����������� �������������� ������, ������������ ������ ��������� ��������.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,2,'��� �������� ������������� ������, ������������ ����� ���������� ����������, ����� ������� �������� ���������� � ������� �������� � ���������� � ��������� �������� (���� ������ ������� �� ��������� �������), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,3,'� ����� �������� ���������� ��� ������ ���������� �������, ������������ �� ������, � ���� ����������.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,4,'������� �������� � ���� ��������� �������: "������������" - ������������ ���������, "���" - ��� ���������, "�������� � ��������� ������", "���������/������" - ������������ ��� ��������� ����� ����������.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (51,3,0,5,'��� ������ ��������� �������� �� ������ �������.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,1,'��� ������� � �������� ����������� �������������� ������, ������������ ������ ���������� ����.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,2,'��� �������� ������������� ������, ������������ ����� ���������� ����������, ����� ������� �������� ���������� � ������� �������� � ���������� � ��������� �������� (���� ������ ������� �� ��������� �������), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,3,'� ����� �������� ���������� ��� ������ ���������� �������, ������������ �� ������, � ���� ����������.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,4,'������� �������� � ���� ��������� �������: "������������" - ������������ ���������, "���" - ��� ���������, "�������� � ��������� ������", "���������/������" - ������������ ��� ��������� ����� ����������.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (59,3,0,5,'��� ������ ��������� �������� �� ������ �������.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,1,'��� ���� ������������� �������������� ������, ������������ ���������� � ����.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,2,'���� ������������� ������ ����������, ����������� ������� ���, �� ������� ������������ ������ ������� ���, ������ � ���������� ������� � �������� ���������.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (124,3,0,3,'� ������� �������� ���������� ������� ������������� ��������� �� ��������������.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,1,'��� ���� ������������� �������������� ������, ������������ ���������� � ������� �����.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,2,'��� �������� ������������� ������, ������������ ����� ���������� ������������, ����� ������� �������� � ������� �������� � ���������� � ��������� �������� (���� ������ ������� �� ��������� �������), ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,3,'� ����� �������� ���������� ��� ������ ���������� �������, ������������ �� ������, � ��������� ������������� ��������.');
insert into s_help (cur_page,tip,kod,sort,hlp) values (55,3,0,4,'� ������� �������� ������������ �� ������� ��� ���������� �������� �����.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,1,'����� �������, ������� �������� ������������ ��������� �������� ����, �������������� �� ��������� ����������:');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,2,'������, ������ (�� ������� ��������� ����������� ���������), ����������� ��������� ������� ��� ������� ������ � ����������� ��������� �����, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,3,'����������� ��������� ������� �� �������, ������ �������� ������� �����, ����� �������� �� ������� ������ � ��������� �����, ����� �������� �� ������� ������ ��� �������� �����, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (37,3,0,4,'��� ��������� ������� ������������ ���������.');

insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,1,'����� �� �������� � ����������� ��������� ��������� ����� �������, ������� �������� ������� � ���������� �� ������� � �������� ������. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,2,'�� ������ ������ �������� ����� �� ��������� ����������: ���������, ����������, ������ ������, �����, ������, ������������, ������, ��������� ������, �������� ������, ���������, ������, ��������� ������, � ������. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,3,'��� ����� ������ ������ �������� � ������ "�" �������� ��������� ����������� �� ��������� ���������� ��������, ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,4,'��� ����� ���� �������� � ������� "�" � "��" �������� ��������� ������ ������� � ��������� ��������, ������� ������� ���������. ');
insert into s_help (cur_page,tip,kod,sort,hlp) values (33,3,0,5,'��� ������� ������� ������ �� ���������� ������� ����������, ����� ����������� ������� ���� �� ����� ������. ');





--------------------------------------------------------
--��������
--------------------------------------------------------
--drop table webdb.img_lnk;

create table webdb.img_lnk
( nzp_img  serial  not null, --���������� ���
  cur_page integer not null, --��� ������������ ��������, ���� 0 
  tip      integer not null, --1 - ������ ����, 2 - �������� 
  kod      integer not null, --nzp_page - ���� tip=1, nzp_act - ���� tip=2
  img_url  char(255)         --��� ����� �������� 
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
--��������� ������������
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
--���� ��-���������
--------------------------------------------------------
create table webdb.s_roles
( nzp_role serial not null,
  role     char(120),
  page_url integer default 0,
  sort     integer default 0
);

create unique index webdb.ix_srls_1 on webdb.s_roles (nzp_role);


insert into webdb.s_roles values (10, encrypt_aes('���������'), 31, 1);
insert into webdb.s_roles values (11, encrypt_aes('���������'), 81, 4);
insert into webdb.s_roles values (12, encrypt_aes('�������������'), 151, 5);


--------------------------------------------------------
--������ � ��������� �� �����
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

--���������� �� (������ ������)
insert into webdb.roles
select 0,b.nzp_role,a.cur_page,1,a.page_url,'',cast(null as integer)
from pages_show a, s_roles b
where 1 = 1;

insert into webdb.roles
select 0,nzp_role,cur_page,2,nzp_act,'',cast(null as integer)
from actions_show, s_roles
where 1 = 1;



--������� � �����
insert into webdb.roles
select 0,nzp_role,0,0,0,'',cast(null as integer)
from s_roles where 1 = 1;



update webdb.roles
set sign = encrypt_aes(tip||kod||cur_page||nzp_role||'-'||nzp_rls||'roles')
where 1 = 1;




--------------------------------------------------------
--�������������� ������������� �� �����
--------------------------------------------------------
create table webdb.userp
( nzp_usp  serial  not null,
  nzp_role integer not null,
  nzp_user integer not null,
  sign     char(90)
);

create unique index webdb.ix_usp_1 on webdb.userp (nzp_usp);
create unique index webdb.ix_usp_2 on webdb.userp (nzp_role,nzp_user);

--������ ������
insert into webdb.userp
select 0,nzp_role,nzp_user,''
from users a, s_roles b 
where 1 = 1;


--�������
update webdb.userp
set sign = encrypt_aes(nzp_user||nzp_role||'-'||nzp_usp||'userp')
where 1 = 1;



--------------------------------------------------------
--����������� �����
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
--����������� ������
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
--������ ������
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
--��������� ���������� �������
--------------------------------------------------------
create table informix.sysprtdata
( id_prtd   serial not null,
  num_prtd  integer not null,
  val_prtd  char(255)
);

create unique index informix.ix_prtd_1 on sysprtdata (id_prtd);
create 	      index informix.ix_prtd_2 on sysprtdata (num_prtd);

-- num_prtd - �������������
--	1  - ������������ �����������
--	11 - ���������� �����������
--	20 - ���������� ����������
--	21 - ���������� ����������

-- val_prtd - ��������



set encryption password "IfmxPwd2";


delete from sysprtdata where 1=1;
insert into sysprtdata values (0,11,encrypt_aes('cnt'||'-'||'3000'));
insert into sysprtdata values (0,20,encrypt_aes('prt'||'-'||'www.stcline.ru'));
--insert into sysprtdata values (0,21,encrypt_aes('cli'||'-'||'00D1')); --https


alter table users add email char(200);
alter table users add is_blocked nchar(1);



--------------------------------------------------------
--���������� ������� �������� �� �������������� �������
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

insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 1, 'SMTP-������', 'char', 'mail.stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 2, '���� ��� �������� �����', 'char', '25',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 3, '��� ������������ ��� ��������� ������� ', 'char', 'portal@stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 4, '������ ������������ ��� ��������� �������', 'char', 'stcline',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 5, '������������ ����������� ��� ��������� ���������', 'char', 'STC Line',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 6, 'Email �����������', 'char', 'portal@stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 7, '�������� ������ � ����������� ������ ������', 'bool', '2', null, null);


set encryption password "IfmxPwd2";

update s_setups set param_name = encrypt_aes(param_name)
, param_type = encrypt_aes(param_type)
, value_ = encrypt_aes(value_);

--------------------------------------------------------
--���������� ������� report
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





