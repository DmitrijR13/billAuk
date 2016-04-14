database #pref_data;
--------------------------------------------------------
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".s_dest;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
--     �����������
--------------------------------------------------------
--------------------------------------------------------
--------------------------------------------------------
--     5. ���������� ���������, ������  (��������� (����� �����������))
--------------------------------------------------------
CREATE TABLE "are".s_dest(
   nzp_dest SERIAL NOT NULL,              -- ����
   dest_name CHAR(100),             -- ������������ ���������, ������
   term_days SMALLINT default 0,    -- ����������� ���� ���������� (���)
   term_hours SMALLINT default 0,   -- ����������� ���� ���������� (����)
   nzp_serv INTEGER,              -- ������ �� ������ -> services.nzp_serv
   num_nedop INTEGER,               -- ��� ������������ �� ��������� (������ �� upg_s_kind_nedop.nzp_kind)
   cur_unl INTEGER
   );
create unique index "are".ix_u_dest_no on "are".s_dest (nzp_dest);
create index "are".ix_u_dest_serv on "are".s_dest (nzp_serv);
create index "are".ix_u_dest_nedop on "are".s_dest (num_nedop);
ALTER TABLE "are".s_dest ADD CONSTRAINT PRIMARY KEY 
   (nzp_dest) CONSTRAINT "are".uDest;

insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (7,'����������� �������',0,3,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (8,'���� � ������������� ������',1,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (9,'����������� ��������� ������������� � �� ����������',0,1,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (10,'������������� �������������',1,0,19,38);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (11,'������������� ���� - ������������� ���������� �������',0,1,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (12,'������������',0,3,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (13,'������������� � ������� ��������� ����������� ���������',7,0,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (14,'����������� �������� ���� ������',1,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (15,'���������� ������� ����',0,0,9,8);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (16,'������ �������� � �������',0,0,13,22);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (17,'������ ����������� ���������',1,0,8,6);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (18,'����',1,0,5,19);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (19,'������������� ��������������� ����',1,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (20,'���������� �������� ����',0,0,6,1);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (21,'���������� ����',1,0,10,13);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (22,'���������� ������� ��������������',0,2,25,16);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (23,'���������� ������ � ��������',1,0,17,24);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (24,'���������� ������ �����',1,0,18,25);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (26,'���� � ������� ���������',1,0,22,27);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (27,'����������� ���� ������',0,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (28,'����������� ������ ������',5,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (30,'������������� � ������� ���������',1,0,22,27);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (31,'���������� �����������',0,0,13,22);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (32,'������ ����� �������� ����',0,0,6,40);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (33,'���������� �����������',0,0,20,39);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (34,'����������� ������',1,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (35,'������������� � ������������� ����',0,0,23,28);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (36,'������������� ������������� ����',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (37,'���������� ��������',0,0,26,30);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (39,'���������� ��������',0,0,27,31);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (40,'������������� ��������',0,0,26,30);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (41,'���������� �������� ������� ������ ������',0,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (42,'�������������������� ���������� �����',0,0,18,25);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (43,'�������������� ����������� ������� ���� ���������',0,0,9,9);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (44,'������������ ����� ���',0,0,16,23);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (45,'������������� ����������',0,0,12,21);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (46,'������������� ������� �������',0,0,209,35);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (47,'������ ����� ������� ����',0,0,9,41);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (29,'��������� � ���������������� ���������� ���������',30,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (38,'��������� �� ����������� ����� �� ���-����. ������',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (48,'���������� ������ ���',0,0,24,29);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (49,'���� � ��������������� ����',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (50,'�������������������� ������ ��������',0,0,17,24);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (51,'������� � �������� �����',0,0,6,1);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (52,'������� � ������� �����',0,0,9,8);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (53,'������������� ��������',1,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (54,'���������� ���������',1,0,8,5);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (90,'�������������������� �������� �������� ����',0,0,6,2);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (59,'�������������������� ��������� ��������',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (60,'�������������������� ���������� �����',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (62,'�������� ������������� - ���������, ������',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (63,'���������� �����',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (64,'���������� ��������� ���',7,0,11,20);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (65,'���������� ��������� � ���������',0,0,22,27);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (66,'�������������',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (67,'��������������� ����������, �����',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (70,'���������� ����������, ���������� �����',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (71,'����� �������� ��������',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (72,'���������� ��� � ������ � ���������',0,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (73,'������, ��������� ����������',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (74,'������������� �������� �����',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (75,'�������� ���������� - ���������, ������',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (76,'���� � ������������� ������ (� ���������� ����������)',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (77,'���� � ��������������� ���� (� ���������� ����������)',0,0,21,26);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (78,'���� � ������� ��������� (� ���������� ����������)',0,0,22,27);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (79,'���������� �������',0,0,2,18);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (80,'��������� � ���������������� ���',0,0,1000,0);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (81,'���������� ��������� ���� � ������',7,0,1000,20);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (91,'������ ���������� � ������������� ����',0,0,25,17);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (92,'�������������� ������� ���� �����',0,0,10,14);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (93,'�������������������� �������� ������� ����',0,0,9,11);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (25,'���������� ������ ���',0,0,16,23);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (94,'���������� ��������� ���������',7,0,1000,20);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (128,'���������� ������ �� ����� ���������',0,0,28,32);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1205,'���������� ���������� ����� ������',0,0,205,33);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (100,'���������� ������������ ������� ����� ������',0,0,206,34);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (115,'���������� �����',0,0,15,48);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (131,'���������� ������ ���������� �����',0,0,31,49);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1200,'���������� ���� ��� ������',0,0,200,50);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1201,'���������� ���� ��� �������� ��������',0,0,201,51);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1202,'���������� ���� ��� ����������',0,0,202,52);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1203,'���������� ���� ��� ����',0,0,203,53);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1204,'���������� ���������� �����������',0,0,204,54);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1207,'���������� ��������� ������������� ��������',0,0,207,55);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1210,'���������� ���������������� �������',0,0,210,57);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1211,'���������� �������� �����',0,0,211,58);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1215,'���������� ������ ��������',0,0,215,62);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1230,'���������� �������',0,0,230,36);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1231,'���������� ����������',0,0,231,37);
insert into "are".s_dest ( nzp_dest, dest_name, term_days, term_hours, nzp_serv, num_nedop) values (1243,'���������� �����',0,0,243,72);
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
update statistics for TABLE "are".s_dest;
-----------------------------------------------------------------------------------------------------------------------
