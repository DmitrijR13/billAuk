grant dba to updb;
grant dba to public;

----------------�������� �� ��� ��������� ����. ����������----------------------------------------
CREATE DATABASE updater;
--������� ip �������
CREATE TABLE updb.rajon_ip
(rajon_number INT,
 rajon_name char(50),
 rajon_ip char(30));
 
 --������� ���������� ��� ������� ������
CREATE TABLE updb.rajon_history
(rajon_name char(50),
 update_status char(5),
 update_type char(20),
 update_version char(50),
 update_date datetime year to minute,
 update_report TEXT);
 
 -------------���������� ������� ���. rajon_ip--------------------------
INSERT INTO updb.rajon_ip VALUES (0,"STCLINE", "www.stcline.ru:81");
INSERT INTO updb.rajon_ip VALUES (1,"����", "192.168.1.102:80");
INSERT INTO updb.rajon_ip VALUES (2,"�������", "178.205.133.45:80");
INSERT INTO updb.rajon_ip VALUES (3,"����������", "178.206.227.10:80");
INSERT INTO updb.rajon_ip VALUES (4,"����������", "178.206.227.70:80");
INSERT INTO updb.rajon_ip VALUES (5,"�����������", "217.23.188.14:80");
INSERT INTO updb.rajon_ip VALUES (6,"�������������", "178.205.135.30:80");
INSERT INTO updb.rajon_ip VALUES (7,"����������", "178.205.133.205:80");
INSERT INTO updb.rajon_ip VALUES (8,"������������", "178.204.136.141:80");
INSERT INTO updb.rajon_ip VALUES (9,"������", "178.205.131.127:80");
INSERT INTO updb.rajon_ip VALUES (10,"��������������", "178.205.135.13:80");
INSERT INTO updb.rajon_ip VALUES (11,"�����������", "178.206.227.125:80");
INSERT INTO updb.rajon_ip VALUES (12,"��������", "178.205.135.146:80");
INSERT INTO updb.rajon_ip VALUES (13,"��������", "178.205.133.140:80");
INSERT INTO updb.rajon_ip VALUES (14,"���������������", "81.25.173.44:80");
INSERT INTO updb.rajon_ip VALUES (15,"������� �����", "178.205.134.209:80");
INSERT INTO updb.rajon_ip VALUES (16,"����� (��� ���)", "178.207.11.4:80");
INSERT INTO updb.rajon_ip VALUES (17,"��������", "178.207.8.241:80");
INSERT INTO updb.rajon_ip VALUES (18,"�����", "178.207.8.176:80");
INSERT INTO updb.rajon_ip VALUES (19,"������", "178.205.133.98:80");
INSERT INTO updb.rajon_ip VALUES (20,"������", "178.206.227.17:80");
INSERT INTO updb.rajon_ip VALUES (21,"������ �������", "178.205.133.224:80");
INSERT INTO updb.rajon_ip VALUES (22,"�����������", "217.173.19.69:80");
INSERT INTO updb.rajon_ip VALUES (23,"����������", "178.206.227.118:80");
INSERT INTO updb.rajon_ip VALUES (24,"�������", "178.205.133.168:80");
INSERT INTO updb.rajon_ip VALUES (25,"��������", "178.207.11.104:80");
INSERT INTO updb.rajon_ip VALUES (26,"�������", "78.138.172.247:80");