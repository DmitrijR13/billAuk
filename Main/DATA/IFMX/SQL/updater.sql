grant dba to updb;
grant dba to public;

--�������� ������� ��� ���������� ( � ��� ����)
CREATE TABLE updb.updater
 (
    nzp_up SERIAL PRIMARY KEY,
	typeUp CHAR(50),
	version double precision,
	status INT,
	path CHAR(200),
	key CHAR(100),
	soup CHAR(20),
	web_path CHAR(80),
	report TEXT
 );
--------------------�������� �� ���������--------------------------------------------------------
INSERT INTO updb.updater (typeUp,version, status, path) values ('web','0' , 1,'������ �� ���������');
INSERT INTO updb.updater (typeUp,version, status, path) values ('host','0' , 1, '������ �� ���������');