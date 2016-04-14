database websmr;
set encryption password "IfmxPwd2";

CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
  
	drop table config;

END PROCEDURE;
EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
  
	create table "webdb".config
	(
		nzp_config serial not null,
		nzp_role int,
		sign char(100)
	);
	CREATE INDEX "webdb".ix_config_1 ON "webdb".config(nzp_role);

END PROCEDURE;
EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

delete from config;

insert into config(nzp_role) values (10); -- ���������
insert into config(nzp_role) values (11); -- ���������
insert into config(nzp_role) values (12); -- �������������
insert into config(nzp_role) values (13); -- ������� �����
insert into config(nzp_role) values (14); -- ������������
insert into config(nzp_role) values (15); -- �������
insert into config(nzp_role) values (18); -- �������� ������������ ������
insert into config(nzp_role) values (20); -- �����
insert into config(nzp_role) values (26); -- ��������� �������
insert into config(nzp_role) values (30); -- ������
insert into config(nzp_role) values (901); -- ���������-���������
insert into config(nzp_role) values (903); -- ������� �����-���������
insert into config(nzp_role) values (904); -- ������������-���������
insert into config(nzp_role) values (905); -- ���������-���������
insert into config(nzp_role) values (915); -- �������-���������
insert into config(nzp_role) values (920); -- �����-���������
insert into config(nzp_role) values (921); -- ������ ����������
insert into config(nzp_role) values (922); -- ���������-���������-������
insert into config(nzp_role) values (923); -- ������������-���������-������
insert into config(nzp_role) values (927); -- ���������-���������-���������-������
insert into config(nzp_role) values (929); -- ������������-���������-���������-������
insert into config(nzp_role) values (930); -- ���������-���������-������
insert into config(nzp_role) values (932); -- �������������-���������-������
insert into config(nzp_role) values (933); -- ����������� � ��������� (��������������)
insert into config(nzp_role) values (939); -- ������� �����-��������������-������
insert into config(nzp_role) values (940); -- ������� ����� (��������������)-��������� ���� ���������
insert into config(nzp_role) values (941); -- ������� �����-��������� ���� ���������
insert into config(nzp_role) values (942); -- �������-����������� (��������������)
insert into config(nzp_role) values (978); -- �������� � ����-������
insert into config(nzp_role) values (979); -- ��������� �������� ����-������
insert into config(nzp_role) values (980); -- ���������� ����������� �������
insert into config(nzp_role) values (987); -- ������ �� ������������� �� �����
insert into config(nzp_role) values (988); -- ���������� ���������
insert into config(nzp_role) values (993); -- ���������� ����������
insert into config(nzp_role) values (994); -- ����� ��

update config set sign = encrypt_aes(nzp_role||'-'||nzp_config||'config');

--������� ������� �������������� Excel : 1 - ����������, 0 - �� ����������
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,encrypt_aes(1));


--���������� ��������� �� �����
delete from map_points where nzp_mo in (select nzp_mo from map_objects where tip in (-1,-2));
delete from map_objects where tip in (-1,-2);
-- ���� ��� stcline.ru
insert into map_objects (tip, note) values (-1, 'ANxfQU0BAAAA-5JTKQIAZqDm6jpY7AMd5oxJ_0cDue0pX9EAAAAAAAAAAACfHJffTds2p14Rtjd4WeeUBjTFGA==');
insert into map_objects (tip, note) values (-2, '�. ������');
insert into map_points (nzp_mo, x, y) select nzp_mo, 50.196333, 53.244172 from map_objects where tip = -2;


