CREATE table if not exists config
(
  nzp_config serial NOT NULL,
  nzp_role integer,
  sign character(100)
)
WITH OIDS;

ALTER TABLE config OWNER TO postgres;
GRANT ALL ON TABLE config TO postgres;
GRANT SELECT, UPDATE, INSERT, DELETE ON TABLE config TO public;

drop index if exists ix_config_1;
CREATE INDEX ix_config_1 ON config USING btree (nzp_role);

delete from config;
insert into config(nzp_role) values (10); -- ���������
insert into config(nzp_role) values (11); -- ���������
insert into config(nzp_role) values (12); -- �������������
insert into config(nzp_role) values (13); -- ������� �����
insert into config(nzp_role) values (14); -- ������������
insert into config(nzp_role) values (15); -- �������
insert into config(nzp_role) values (18); -- �������� ������������ ������
insert into config(nzp_role) values (19); -- ���� ���������
insert into config(nzp_role) values (20); -- �����
insert into config(nzp_role) values (26); -- ��������� �������
insert into config(nzp_role) values (901); -- ���������-���������
insert into config(nzp_role) values (903); -- ������� �����-���������
insert into config(nzp_role) values (904); -- ������������-���������
insert into config(nzp_role) values (905); -- ���������-���������
insert into config(nzp_role) values (915); -- �������-���������
insert into config(nzp_role) values (919); -- ������������� ���
insert into config(nzp_role) values (920); -- �����-���������
insert into config(nzp_role) values (921); -- ������ ����������
insert into config(nzp_role) values (931); -- ���������-���������-������������
insert into config(nzp_role) values (933); -- ����������� � ��������� (��������������)
insert into config(nzp_role) values (935); -- ��������� ���
insert into config(nzp_role) values (940); -- ������� ����� (��������������)-��������� ���� ���������
insert into config(nzp_role) values (941); -- ������� �����-��������� ���� ���������
insert into config(nzp_role) values (946); -- ��������� (�������������)
insert into config(nzp_role) values (961); -- ���������-���������-������-����
insert into config(nzp_role) values (962); -- ������������-���������-������-����
insert into config(nzp_role) values (963); -- ���������-���������-���������-������-����
insert into config(nzp_role) values (964); -- ������������-���������-���������-������-����
insert into config(nzp_role) values (968); -- �������� ����
insert into config(nzp_role) values (969); -- �������������� ����
insert into config(nzp_role) values (972); -- �������� ���
insert into config(nzp_role) values (973); -- �������������� ���

update config set sign = nzp_role::varchar||'-'||nzp_config::varchar||'config';

--������� ������� �������������� Excel : 1 - ����������, 0 - �� ����������
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,1);
