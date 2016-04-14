database #pref_data;
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".s_zvktype;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
  drop TABLE "are".s_result;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".s_slug;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".s_attestation;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP TABLE "are".jrn_upg_nedop;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
--------------------------------------------------------
--     �����������
--------------------------------------------------------
--------------------------------------------------------
--------------------------------------------------------
--     1. ���������� "���������, ���������"
--------------------------------------------------------
CREATE TABLE "are".s_result(
   nzp_res SERIAL NOT NULL,    
   res_name CHAR(30) NOT NULL, 
   res_type SMALLINT);         

create unique index "are".ix_u_res_no on "are".s_result (nzp_res);
create index "are".ix_res_type on "are".s_result (res_type);
ALTER TABLE "are".s_result ADD CONSTRAINT PRIMARY KEY 
   (nzp_res) CONSTRAINT "are".c_res_nzp;

insert into "are".s_result(nzp_res, res_name, res_type)  values (1, '�����������',  null);
insert into "are".s_result(nzp_res, res_name, res_type)  values (2, '�������� ������', 1);
insert into "are".s_result(nzp_res, res_name, res_type)  values (3, '���������',       1);
insert into "are".s_result(nzp_res, res_name, res_type)  values (4, '���������',       1);
insert into "are".s_result(nzp_res, res_name, res_type)  values (5, '�� ���������',    1);
insert into "are".s_result(nzp_res, res_name, res_type)  values (6, '�������������', null);
--------------------------------------------------------
--------------------------------------------------------
--     2. ���������� �������� ��������� (����� ������������)
--------------------------------------------------------
CREATE TABLE "are".s_zvktype(
   nzp_ztype SERIAL NOT NULL,
   zvk_type CHAR(100) NOT NULL
   );

create unique index "are".ix_u_type_no on "are".s_zvktype (nzp_ztype);
ALTER TABLE "are".s_zvktype ADD CONSTRAINT PRIMARY KEY 
   (nzp_ztype) CONSTRAINT "are".uZvktypeNzp;

insert into "are".s_zvktype(nzp_ztype, zvk_type) values (1, '������');
insert into "are".s_zvktype(nzp_ztype, zvk_type) values (2, '��������� �� �������������');
insert into "are".s_zvktype(nzp_ztype, zvk_type) values (3, '�������-������������ ���������');
insert into "are".s_zvktype(nzp_ztype, zvk_type) values (4, '��������� �� ������');
insert into "are".s_zvktype(nzp_ztype, zvk_type) values (5, '������ ���������');
insert into "are".s_zvktype(nzp_ztype, zvk_type) values (99, '��������� � ��������');

--------------------------------------------------------
--     3. ���������� ������ (����� ������������)
--------------------------------------------------------
CREATE TABLE "are".s_slug(
   nzp_slug SERIAL NOT NULL,
   slug_name CHAR(100) NOT NULL,
   phone CHAR(30),
   dat_s DATE,           -- ������ ��������������� ������ (������������ ��� 
   dat_po DATE           -- ����� ����� �������������)
   );         

create unique index "are".ix_u_slug_no on s_slug (nzp_slug);
ALTER TABLE "are".s_slug ADD CONSTRAINT PRIMARY KEY 
   (nzp_slug) CONSTRAINT "are".uSlug;
create index "are".ix_u_slug_dates on "are".s_slug (dat_s, dat_po);

insert into s_slug(nzp_slug, slug_name, phone) values (1, '��� ����������', '2-12-885');
insert into s_slug(nzp_slug, slug_name, phone) values (2, '��� ���', '');

insert into "are".s_slug
(nzp_slug, slug_name, phone, dat_s, dat_po)
 values (3, '��� ������������', '2-31-481', null, null);

insert into "are".s_slug
(nzp_slug, slug_name, phone, dat_s, dat_po)
 values (4, '��� ������� ��������� ������', '2-53-033', null, null);

--------------------------------------------------------
--     4. ���������� ������������� ���������� ������� (���������)
--------------------------------------------------------
CREATE TABLE "are".s_attestation(
   nzp_atts SERIAL NOT NULL,
   atts_name CHAR(30));
create unique index "are".ix_u_att_no on "are".s_attestation (nzp_atts);

insert into "are".s_attestation (nzp_atts, atts_name) values (1, '���������� �����������');
insert into "are".s_attestation (nzp_atts, atts_name) values (2, '������������ �������');
insert into "are".s_attestation (nzp_atts, atts_name) values (3, '�� ������������ �������');


-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------
update statistics for TABLE "are".s_result;
update statistics for TABLE "are".s_zvktype;
update statistics for TABLE "are".s_slug;
update statistics for TABLE "are".s_attestation;
-----------------------------------------------------------------------------------------------------------------------
CREATE TABLE "are".jrn_upg_nedop(
   no SERIAL NOT NULL,
   d_begin DATE,
   d_end DATE,
   reg_begin DATE,
   reg_end DATE,
   d_when DATE,
   nzp_user INTEGER,
   crt_count INTEGER,
   cnc_count INTEGER);
CREATE UNIQUE INDEX "are".uix_grnn_no ON "are".jrn_upg_nedop(no);
CREATE INDEX "are".uix_grnn_dend ON "are".jrn_upg_nedop(d_end);
-----------------------------------------------------------------------------------------------------------------------
  delete from series where kod in (14,15,16,17);
  insert   into   series   (kod,   v_min,   v_max,   cur_val)   values
  (14,1,999999999,1);
  insert   into   series   (kod,   v_min,   v_max,   cur_val)   values
  (15,1,999999999,1);
  insert   into   series   (kod,   v_min,   v_max,   cur_val)   values
  (16,1,999999999,1);
  insert   into   series   (kod,   v_min,   v_max,   cur_val)   values
  (17,1,999999999,1);
-----------------------------------------------------------------------------------------------------------------------
