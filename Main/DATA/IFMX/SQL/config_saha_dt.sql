database websaha;
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

insert into config(nzp_role) values (12); -- �������������
insert into config(nzp_role) values (22); -- ������� ��
insert into config(nzp_role) values (23); -- ������ ������� ��
insert into config(nzp_role) values (24); -- �������� ��
insert into config(nzp_role) values (921); -- ������ ����������
insert into config(nzp_role) values (924); -- ���������-���������-������������
insert into config(nzp_role) values (928); -- ���������-���������-���������-������������
insert into config(nzp_role) values (932); -- �������������-���������-������
insert into config(nzp_role) values (933); -- ����������� � ��������� (��������������)
insert into config(nzp_role) values (942); -- �������-����������� (��������������)
insert into config(nzp_role) values (944); -- �������������� �������-��������������
insert into config(nzp_role) values (945); -- ������ �������-��������������

update config set sign = encrypt_aes(nzp_role||'-'||nzp_config||'config');

--������� ������� �������������� Excel : 1 - ����������, 0 - �� ����������
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,encrypt_aes(1));