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
--������ �������� ���� ����������
--------------------------------------------------------
create table webdb.ext_mm
( nzp_mm  serial(1) not null,
  mm_text char(20),
  mm_sort integer   --����������
);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (1,"������� ����", 1);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (2,"���", 2);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (3,"������", 3);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (4,"�������� �������", 4);

insert into webdb.ext_mm (nzp_mm,mm_text, mm_sort)
values (5,"������", 5);

create unique index webdb.ix1_ext_mm on webdb.ext_mm (nzp_mm);



--------------------------------------------------------
--��������� �������� ���� ����������
--------------------------------------------------------
create table webdb.ext_pm
( nzp_pm     serial(1) not null,
  nzp_mm     integer not null, 	-->ext_mm.nzp_mm
  pm_text    char(40), 		--text, �������� '������ ������� ������'
  pm_action  char(40), 		--action, �������� 'on_AccountList', 'on_' ����������� ��� ������
  pm_control char(40), 		--����������, �������� 'account.AccountList'
  pm_sort    integer   		--����������
);

--��
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (1,1,'������ ������','AccountSearch','account.AccountSearch', 1);

insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (2,1,'������ ������� ������','AccountList','account.AccountList', 2);

--���
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (3,2,'������ ������','AccountSearch','account.AccountSearch', 1);

insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (4,2,'������ �����','HouseList','house.HouseList', 2);

--������
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (5,3,'������ �� ������ ������� ������','AccountSearch','account.AccountSearch', 1);

--�������� �������
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (6,4,'������ ������ �� �������','FindGil','gil.FindGil', 1);

insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (7,4,'������ �������� �������','GilList','gil.GilList', 2);

--������
insert into webdb.ext_pm (nzp_pm,nzp_mm,pm_text,pm_action,pm_control, pm_sort)
values (8,5,'������������','Users','admin.Users', 1);

create unique index webdb.ix1_ext_pm on webdb.ext_pm (nzp_pm);
create 	      index webdb.ix2_ext_pm on webdb.ext_pm (nzp_mm, pm_sort);


--------------------------------------------------------
--����������� �� ����� ��� ext_mm (tip=201)
--------------------------------------------------------
insert into roleskey (nzp_role,tip,kod) values (10, 201, 1);  --���������:������� ����
insert into roleskey (nzp_role,tip,kod) values (10, 201, 2);  --���������:���
insert into roleskey (nzp_role,tip,kod) values (10, 201, 3);  --���������:������
insert into roleskey (nzp_role,tip,kod) values (12, 201, 5);  --�������������:������
insert into roleskey (nzp_role,tip,kod) values (14, 201, 4);  --������������:�������� �������

--------------------------------------------------------
--����������� �� ����� ��� ext_pp (tip=202)
--------------------------------------------------------
--insert into roleskey (nzp_role,tip,kod) values (10, 202, 1);  --������ ������
--insert into roleskey (nzp_role,tip,kod) values (10, 202, 2);  --������ ������� ������
--insert into roleskey (nzp_role,tip,kod) values (10, 202, 3);  --������ ������
--insert into roleskey (nzp_role,tip,kod) values (10, 202, 4);  --������ �����



set encryption password "IfmxPwd2";

update webdb.roleskey
set sign = encrypt_aes(tip||kod||nzp_role||'-'||nzp_rlsv||'roles')
where tip in (201,202);



--------------------------------------------------------
--����������� �� id (rolesval)
--------------------------------------------------------
-- nzp_role
-- tip = 211
-- kod = ����� Ext-window
-- val = Id Ext-window




--------------------------------------------------------
update statistics;
--------------------------------------------------------

database sysmaster;

