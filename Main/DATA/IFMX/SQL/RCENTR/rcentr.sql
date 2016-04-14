database webdata;

set encryption password "IfmxPwd2";


--------------------------------------------------------
--������
--------------------------------------------------------
drop table s_rajon;
create table webdb.s_rajon
( nzp_raj  serial(1) not null,
  rajon    char(20),
  ordering integer default 0
);

 delete from s_rajon where 1=1;

 insert into s_rajon (nzp_raj,rajon) values (1,"������");
 insert into s_rajon (nzp_raj,rajon) values (2,"���������� �����");
 insert into s_rajon (nzp_raj,rajon) values (3,"������������");
 insert into s_rajon (nzp_raj,rajon) values (4,"���������");
 insert into s_rajon (nzp_raj,rajon) values (5,"������������");
 insert into s_rajon (nzp_raj,rajon) values (6,"������������");
 insert into s_rajon (nzp_raj,rajon) values (7,"�����������");
 insert into s_rajon (nzp_raj,rajon) values (8,"������������");
 insert into s_rajon (nzp_raj,rajon) values (9,"�����������");
 insert into s_rajon (nzp_raj,rajon) values (10,"�������������");
 insert into s_rajon (nzp_raj,rajon) values (11,"�����������");
 insert into s_rajon (nzp_raj,rajon) values (12,"������");
 insert into s_rajon (nzp_raj,rajon) values (13,"���������");
 insert into s_rajon (nzp_raj,rajon) values (14,"����������");
 insert into s_rajon (nzp_raj,rajon) values (15,"������������");
 insert into s_rajon (nzp_raj,rajon) values (16,"�������������");
 insert into s_rajon (nzp_raj,rajon) values (17,"��������");
 insert into s_rajon (nzp_raj,rajon) values (18,"���������������");
 insert into s_rajon (nzp_raj,rajon) values (19,"�������������");
 insert into s_rajon (nzp_raj,rajon) values (20,"��������");
 insert into s_rajon (nzp_raj,rajon) values (21,"�������������");
 insert into s_rajon (nzp_raj,rajon) values (22,"����������");
 insert into s_rajon (nzp_raj,rajon) values (23,"��������");
 insert into s_rajon (nzp_raj,rajon) values (24,"������������");
 insert into s_rajon (nzp_raj,rajon) values (25,"���������");
 insert into s_rajon (nzp_raj,rajon) values (26,"������-���������");
 insert into s_rajon (nzp_raj,rajon) values (27,"����������");
 insert into s_rajon (nzp_raj,rajon) values (28,"����������");
 insert into s_rajon (nzp_raj,rajon) values (29,"�������������");
 insert into s_rajon (nzp_raj,rajon) values (30,"�����������");
 insert into s_rajon (nzp_raj,rajon) values (31,"�������������");
 insert into s_rajon (nzp_raj,rajon) values (32,"������������");
 insert into s_rajon (nzp_raj,rajon) values (33,"������������");
 insert into s_rajon (nzp_raj,rajon) values (34,"��������������");
 insert into s_rajon (nzp_raj,rajon) values (35,"����������");
 insert into s_rajon (nzp_raj,rajon) values (36,"�������������");
 insert into s_rajon (nzp_raj,rajon) values (37,"�����-����������");
 insert into s_rajon (nzp_raj,rajon) values (38,"���������");
 insert into s_rajon (nzp_raj,rajon) values (39,"������������");
 insert into s_rajon (nzp_raj,rajon) values (40,"��������");
 insert into s_rajon (nzp_raj,rajon) values (41,"���������");
 insert into s_rajon (nzp_raj,rajon) values (42,"���������");
 insert into s_rajon (nzp_raj,rajon) values (43,"����������");
 insert into s_rajon (nzp_raj,rajon) values (44,"�����������");
 insert into s_rajon (nzp_raj,rajon) values (45,"������������");
 insert into s_rajon (nzp_raj,rajon) values (46,"�������������");
 insert into s_rajon (nzp_raj,rajon) values (47,"����������");
                                              
 update s_rajon
 set ordering = nzp_raj;

create unique index webdb.ix_raj_1 on webdb.s_rajon (nzp_raj);
create        index webdb.ix_raj_2 on webdb.s_rajon (ordering);



--------------------------------------------------------
--��������� ������
--------------------------------------------------------
drop table s_rcentr;
create table webdb.s_rcentr
(  nzp_rc     serial(1) not null,
   rcentr     char(60),
   pref       integer not null,
   rc_adr     char(90),
   rc_email   char(60),
   rc_ruk     char(60),
   nzp_raj    integer default 0 not null 
);


 delete from s_rcentr where 1=1;

 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (1,24,'��� ��� �.�������������',201,'422550 ���������� ���������, �. ������������, ��.������ 42','irc-zelendol1@yandex.ru','��������� ����� �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (2,2,'��� ��� �.���������� �����',31,'423834 ���������� ���������, �.��������� �����, ������� ��������, �.3','office@grc-chelny.ru','������ �������� ��������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (3,4,'��� ��� ���������� ������ � �.�����',202,'422230 ���������� ���������, �. �����, ��. �������� 70','Gulshat31@yandex.ru','���������� ������� �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (4,5,'�� ��� ��� - ���������',203,' ���������� ���������, �. ���������, ��. �.��������� �.18','departament_azn@mail.ru','������� ����� ��������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (5,6,'��� ������-������ ���������',204,' �� ������������ ����� �.�.�. ��������� ��.������������������� �.3�','erz_aksu@mail.ru','������� ������� �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (6,7,'��� ����������� ��',205,'423740 ���������� ���������, �.�������, ��. �������� ������ �.58','aktan-ahmat@yandex.ru','������������ ������ ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (7,8,'��� �� ������������� ������',206,'422900 ���������� ���������, ������������ �����,�.�.�. ������������, ��. �������� �.5','ykaralerc@rambler.ru','��������� ����� ������������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (8,9,'�� ��� �. �������� ������',207,' ���������� ���������, ����������� ����� ���� �������� ������ ��.������ �.9','alk-ers@bk.ru,alk-ers@mail.ru','�������� �������� ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (9,11,'����������� �� ���',209,'422350 ���������� ���������, ����������� �����, �.�.�. ��������   ��. �. ������� �.13','gulnaz-erc@rambler.ru','�������� ������� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (10,12,'��� ��� ������� ������',210,' ���������� ���������, �. ����, ��. �������� �.9','uk_erc_arsk@mail.ru','�������� ����� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (11,13,'��� ��������� ���',211,'422750 ���������� ���������, ��������� �����, �.������� ����, ��.��������� �.42','mupgkxatnia@bk.ru','������� ����� ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (12,14,'��� ��� �����',212,' ���������� ���������, ���������� �����','ers-bav@yandex.ru','��������� ����� ��������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (13,15,'��� �� ������������� ������',213,'422250 ���������� ���������, ������������ �����, ���. �������, ��.����, �.9','mppbaltasi@mail.ru,mppgkh@baltash.gov.tatarstan.ru','������ ����� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (14,17,'��� �� ������',215,'422430 ���������� ���������, �.������, �.���������� �.50','erz_buinsk@rambler.ru','������ ���� ��������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (15,18,'��� ������������ ���� ���������������� ������',216,' ���������� ���������, �. �. �����, ��. ������, �.11.','kom-seti@mail.ru','��������� ������ ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (16,19,'�� ��� �������������� ������',217,'422700 ���������� ���������, ������������� �����, ������� ������� ���� ��. ������������ �.13','irc.vgora@rambler.ru','�������� ��������� ��������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (17,19,'��� ���������',252,' ���������� ���������, ������������� �����','tatmet@tatmetall.com','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (18,21,'��� ������ ������������� �����',218,'422470 ���������� ���������, ������������� �����,�.������ ���������,��.����������� �.26','veliullov@mail.ru,ooo-zhil@mail.ru','������� ������ �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (19,22,'��� ���� �������',219,' ���������� ���������, �.�������','erc_alabuga@rambler.ru','��������� ������ ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (20,44,'��� ��� ������',240,' ���������� ���������, ����������� �����, �.������, ��.������ �.46','erc-tulyachi@yandex.ru, Iskander.Garipov@tatar.ru','������������� ���� ��������������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (21,25,'��������� ��� ���',221,'422330 ���������� ���������, ��������� �����, �.�.�������, ��.������������ �.2','kaibici-gkh@rambler.ru','����������� ����� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (22,26,'��� ��� ������-����������� ������',222,'422820 ���������� ���������, ���. �������-�����, ��.�.������,105','kam-ust-raschet@mail.ru','����������� �������� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (23,27,'��� ��� ������',223,'422110 ���������� ���������, �.������,�. ������ ��. ������ �.24','erc_kukmor@mail.ru','�������� ��������� �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (24,28,'�� ��� ���������� �����',224,' ���������� ���������, �.���������� �����','Tanzilya.Tavrizova@tatar.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (25,29,'��� ����� �������������� �������� ��� �����������',225,'423250 ���������� ���������, �.�����������, ��.���������� �.2','gkhlen@yandex.ru','������� ����� �������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (26,30,'��� �� ������������ ������',226,'422190 ���������� ���������, �.�������, ��. �. ������ �. 18/23','ukmam@mail.ru','�������� ������ �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (27,31,'�� ��� �����������',227,'423650 ���������� ���������, ������������� �����,�.����������� ��.������� ��� 3�','mend_rc@mail.ru','��������� ���� ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (28,32,'��� ��� �����������',228,' ���������� ���������, ������������ �����,�.����������, ��.�.������� , �.15','meli2004@mail.ru, menzelyk@mail.ru','�������� ������ ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (29,33,'��� ������������ ���',229,'423970 ���������� ���������, ������������ �����, �.���������, ��.���������� �.58','erts.musl@yandex.ru','���������� ����� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (30,3,'��� ��� �.�����������',32,'423570 ���������� ���������, �. ����������,��.����������, �.6�','erc@newmail.ru,erc@inbox.ru','������������� ����� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (31,3,'��� ������������������ �.����������',34,' ���������� ���������, �. ����������','usrneftehim@rambler.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (32,3,'��� �������������� �.����������',33,' ���������� ���������, �. ����������','gkh_shin@mail.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (33,34,'��� �������������� ���',230,'423190 ���������� ���������, �������������� �����, ������������, ��.������� �.6','ukkristall@yandex.ru','�������� ���� ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (34,36,'��� ������ ��������� ����� ��������',232,' ���������� ���������, ������������� �����, ��������, ��.������������, �.17','uk-pest@mail.ru. erc-pestr@yandex.ru','�������� ���� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (35,38,'��� ����� ������������ ��� ����',234,' ���������� ���������, ��������� �����, ���.����, ��.�����, 87','saz@newmail.ru,aidarshax@mail.ru. so_tcj@mail.ru','����������� ����� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (36,39,'��� ������������ ���',235,'423350 ���������� ���������, ������������ �����,�.���������, ��. �����������  ���  30','uksarman@mail.ru','������ ������ ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (37,39,'�� ��� �.�������',236,' ���������� ���������, ��� �������','','');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (38,40,'��� ��� �.������ � ��������� ������',237,'422840 ���������� ���������, �������� �����, �.������, ��.���������� �.21','allakaznacheeva@mail.ru','���������� ���� �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (39,42,'��� ��� ���������',238,'422370 ���������� ���������, �. ������,��. �������� �. �14','rif_tet@rambler.ru','������� ������ ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (40,43,'��� ��������������',239,'423897 ���������� ���������, ���������� �����, �.������ ������ ��.������ �.48','ol-gkh-tuk@mail.ru','��������� ��������� ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (41,45,'��� ������������ ���� ������������� ������',241,'423100 ���������� ���������, ������������ �����,�. ��������, ��. ������, 25','KOMSETI@MAIL.RU','�������� ������ �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (42,46,'�� �� �������� �������� ���������',242,'422980 ���������� ���������, �. ���������, ��. ������, �. 68','dimych@mail.ru,ptgh@mail.ru','������ ����� �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (43,47,'��� ��� �����',243,'423950 ���������� ���������, ���������� �����, �� ������ ��.������, �.1','OOOERC11@mail.ru','������� ������ �����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (44,10,'�� ��� �.������������',208,' ���������� ���������, �.�����������','errc@mail.ru','������ ������� ��������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (45,16,'��� ��� �.��������',214,' ���������� ���������, �.��������','vitalik2003a@yandex.ru','��������� ����� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (46,23,'��� �������� ����������� ��������',220,'423520 ���������� ���������, �.������, ��. �������� �.7','zaiuk@mail.ru','������ ���� ��������������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (47,2,'��� ����������� �.���������� �����',253,' ���������� ���������, �. ���������� ����� , ��. ���������, �.6','eremenko@kamgb.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (48,1,'��� ����� �.������',260,' ���������� ���������, �. ������','kvart@bancorp.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (49,1,'������������ ������� ���',259,' ���������� ���������, �. ������','marina-iyudina@yandex.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (50,35,'��� ��� ������',231,'423040 ���������� ���������, �.������,��. �������������, ��� 15','lil_nur@mail.ru','������������ ����� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (51,18,'����������� ������-��������� �-�',251,' ���������� ���������, ��������������� ����� �. ������ ��������','simen1975@rambler.ru','-');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (52,3,'��� ��� ��� ������� �����',35,'423564 ���������� ���������, ���.������� ������, �.1/38 ���� 23','erc-kpl@mail.ru,leon1978@mail.ru','������ ���� ����������');

 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (53,1,'�� ����������������� ������',270,'420127 ���������� ���������, �.������, ��.����������, �-28','','������������� �.�.');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (54,1,'�� �����-1',271,'420100 �. ������, ��.������� �.9�','azino-1@mail.ru','��������� �.�.');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (55,1,'�� �����-2',272,'420140 ���������� ���������, �.������, ��.������, �114','rustam988@mail.ru','������ ������� ��������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (56,1,'�� ������ ��� ����-�����������',273,'420126 ���������� ���������, �.������, ��.�������, �.17','','�������� ������ ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (57,1,'�� ������������ ������',275,'420110 ���������� ���������, �.������, �������� ������ �.39','priv_rc@mail.ru','������ ���� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (58,1,'�� ��������',277,'420075 ���������� ���������, �.������, ��.��������, �.20','','������������� ����� ������������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (59,1,'�� �������',279,'420033 ���������� ���������, �.������,��.�����������,�.5','','����� ������� ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (60,1,'�� ����������� ������ �.������',278,'420095 ���������� ���������, �.������, ��.�. �������� �.28�','and_sh@mail.ru','���������� ������� ������������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (61,1,'�� ���������',283,'420087 �. ������, ��.��������� �������� �.2�','erctank@mail.ru','������������� ����� ������������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (63,1,'�� ������-������',284,'420061 �.������, ��.����������� 29�','sd2000km.ru','�������� ��������� ��������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (64,1,'�� �����������',286,'420073 �.������, ��.����������� �.22','rodik_lis@mail.ru','��������� ����� ����������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (65,1,'���',285,'420110 �. ������, ��. ������, �. 39','','������ ���� ���������');
 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (66,1,'�� ������������ ������',274,'420110 ���������� ���������, �.������, �������� ������ �.39','priv_rc@mail.ru','������ ���� ���������');

 insert into s_rcentr (nzp_rc,nzp_raj,rcentr,pref,rc_adr,rc_email,rc_ruk)  values (999,1,'�� ����',273,'420126 ��, �.������, ��.�������, �.17','','�������� ������ ����������');



create unique index webdb.ix_r�_1 on webdb.s_rcentr (nzp_rc);
create        index webdb.ix_r�_2 on webdb.s_rcentr (pref);
create        index webdb.ix_r�_3 on webdb.s_rcentr (nzp_raj);



--------------------------------------------------------
--������� ��
--------------------------------------------------------
drop table servers;
create table webd.servers
(  nzp_server serial(1) not null,
   hadr       char(140),   --����� ������
   hadr2      char(140),   --������ �������
   nzp_rc     integer      --��
);


create unique index are.ix_srvs_1 on servers (nzp_server);
create        index are.ix_srvs_2 on servers (nzp_rc);


delete from servers where 1=1;

insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (1, 'net.tcp://localhost:8018/srv', 'Administrator; rubin', 1);

insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (2, 'net.tcp://localhost:8011/srv', 'Administrator; rubin', 63);

set encryption password "IfmxPwd2";
update servers 
set hadr = encrypt_aes(hadr), hadr2 = encrypt_aes(hadr2)
where 1=1;




--------------------------------------------------------
--��������� ������������
--------------------------------------------------------
--���� ������� � rolesval
--






delete from servers where 1=1;

--zarech
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (1, 'net.tcp://178.205.97.176:7047/srv', 'usernet; userweb', 59);

--azino-1
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (2, 'net.tcp://78.138.147.141:7047/srv', 'usernet; userweb', 54);

--azino-2
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (3, 'net.tcp://78.138.130.127:7047/srv', 'webbrk; n2LMDs9', 55);

--derb
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (4, 'net.tcp://81.23.150.68:7047/srv', 'brobro; jmdfJs^2S', 58);

--tank
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (5, 'net.tcp://92.255.193.116:7047/srv', 'webbro; cDafs78s@', 61);



--garant
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (6, 'net.tcp://92.255.194.235:7047/srv', 'wbbrkr; s@#ds4ah9', 63);

--udom
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (7, 'net.tcp://89.184.21.39:7047/srv', 'usrbrok; jFG8nf3we%', 56);



--priv
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (8, 'net.tcp://89.184.21.39:7047/srv', 'usrbro; &sdA342sd', 57);

--vah
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (9, 'net.tcp://89.184.21.39:7048/srv', 'usrbro; &sdA342sd', 66);

--mos          
insert into servers (nzp_server, hadr, hadr2, nzp_rc)
values (10, 'net.tcp://88.82.75.151:7048/srv', 'brouser; s&jfgw%23', 60);



set encryption password "IfmxPwd2";
update servers 
set hadr = encrypt_aes(hadr), hadr2 = encrypt_aes(hadr2)
where 1=1;

