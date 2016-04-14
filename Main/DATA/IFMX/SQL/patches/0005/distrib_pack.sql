--изменения в CENTRAL_fin_MM

--DROP TABLE "are".fn_distrib_dom_01;
CREATE TABLE "are".fn_distrib_dom_01(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_01_1 ON "are".fn_distrib_dom_01(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_01_2 ON "are".fn_distrib_dom_01(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_01_3 ON "are".fn_distrib_dom_01(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_01_4 ON "are".fn_distrib_dom_01(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_01_5 ON "are".fn_distrib_dom_01(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_01_6 ON "are".fn_distrib_dom_01(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_01_7 ON "are".fn_distrib_dom_01(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_01_8 ON "are".fn_distrib_dom_01(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_01 TO public AS are;

 

--DROP TABLE "are".fn_distrib_dom_02;
CREATE TABLE "are".fn_distrib_dom_02(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_02_1 ON "are".fn_distrib_dom_02(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_02_2 ON "are".fn_distrib_dom_02(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_02_3 ON "are".fn_distrib_dom_02(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_02_4 ON "are".fn_distrib_dom_02(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_02_5 ON "are".fn_distrib_dom_02(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_02_6 ON "are".fn_distrib_dom_02(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_02_7 ON "are".fn_distrib_dom_02(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_02_8 ON "are".fn_distrib_dom_02(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_02 TO public AS are;



--DROP TABLE "are".fn_distrib_dom_03;
CREATE TABLE "are".fn_distrib_dom_03(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_03_1 ON "are".fn_distrib_dom_03(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_03_2 ON "are".fn_distrib_dom_03(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_03_3 ON "are".fn_distrib_dom_03(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_03_4 ON "are".fn_distrib_dom_03(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_03_5 ON "are".fn_distrib_dom_03(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_03_6 ON "are".fn_distrib_dom_03(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_03_7 ON "are".fn_distrib_dom_03(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_03_8 ON "are".fn_distrib_dom_03(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_03 TO public AS are;


--DROP TABLE "are".fn_distrib_dom_04;
CREATE TABLE "are".fn_distrib_dom_04(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_04_1 ON "are".fn_distrib_dom_04(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_04_2 ON "are".fn_distrib_dom_04(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_04_3 ON "are".fn_distrib_dom_04(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_04_4 ON "are".fn_distrib_dom_04(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_04_5 ON "are".fn_distrib_dom_04(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_04_6 ON "are".fn_distrib_dom_04(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_04_7 ON "are".fn_distrib_dom_04(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_04_8 ON "are".fn_distrib_dom_04(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_04 TO public AS are;


--DROP TABLE "are".fn_distrib_dom_05;
CREATE TABLE "are".fn_distrib_dom_05(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_05_1 ON "are".fn_distrib_dom_05(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_05_2 ON "are".fn_distrib_dom_05(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_05_3 ON "are".fn_distrib_dom_05(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_05_4 ON "are".fn_distrib_dom_05(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_05_5 ON "are".fn_distrib_dom_05(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_05_6 ON "are".fn_distrib_dom_05(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_05_7 ON "are".fn_distrib_dom_05(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_05_8 ON "are".fn_distrib_dom_05(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_05 TO public AS are;



--DROP TABLE "are".fn_distrib_dom_06;
CREATE TABLE "are".fn_distrib_dom_06(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_06_1 ON "are".fn_distrib_dom_06(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_06_2 ON "are".fn_distrib_dom_06(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_06_3 ON "are".fn_distrib_dom_06(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_06_4 ON "are".fn_distrib_dom_06(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_06_5 ON "are".fn_distrib_dom_06(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_06_6 ON "are".fn_distrib_dom_06(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_06_7 ON "are".fn_distrib_dom_06(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_06_8 ON "are".fn_distrib_dom_06(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_06 TO public AS are;


--DROP TABLE "are".fn_distrib_dom_07;
CREATE TABLE "are".fn_distrib_dom_07(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_07_1 ON "are".fn_distrib_dom_07(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_07_2 ON "are".fn_distrib_dom_07(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_07_3 ON "are".fn_distrib_dom_07(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_07_4 ON "are".fn_distrib_dom_07(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_07_5 ON "are".fn_distrib_dom_07(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_07_6 ON "are".fn_distrib_dom_07(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_07_7 ON "are".fn_distrib_dom_07(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_07_8 ON "are".fn_distrib_dom_07(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_07 TO public AS are;



--DROP TABLE "are".fn_distrib_dom_08;
CREATE TABLE "are".fn_distrib_dom_08(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_08_1 ON "are".fn_distrib_dom_08(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_08_2 ON "are".fn_distrib_dom_08(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_08_3 ON "are".fn_distrib_dom_08(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_08_4 ON "are".fn_distrib_dom_08(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_08_5 ON "are".fn_distrib_dom_08(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_08_6 ON "are".fn_distrib_dom_08(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_08_7 ON "are".fn_distrib_dom_08(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_08_8 ON "are".fn_distrib_dom_08(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_08 TO public AS are;




--DROP TABLE "are".fn_distrib_dom_09;
CREATE TABLE "are".fn_distrib_dom_09(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_09_1 ON "are".fn_distrib_dom_09(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_09_2 ON "are".fn_distrib_dom_09(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_09_3 ON "are".fn_distrib_dom_09(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_09_4 ON "are".fn_distrib_dom_09(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_09_5 ON "are".fn_distrib_dom_09(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_09_6 ON "are".fn_distrib_dom_09(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_09_7 ON "are".fn_distrib_dom_09(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_09_8 ON "are".fn_distrib_dom_09(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_09 TO public AS are;


--DROP TABLE "are".fn_distrib_dom_10;
CREATE TABLE "are".fn_distrib_dom_10(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_10_1 ON "are".fn_distrib_dom_10(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_10_2 ON "are".fn_distrib_dom_10(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_10_3 ON "are".fn_distrib_dom_10(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_10_4 ON "are".fn_distrib_dom_10(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_10_5 ON "are".fn_distrib_dom_10(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_10_6 ON "are".fn_distrib_dom_10(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_10_7 ON "are".fn_distrib_dom_10(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_10_8 ON "are".fn_distrib_dom_10(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_10 TO public AS are;



--DROP TABLE "are".fn_distrib_dom_11;
CREATE TABLE "are".fn_distrib_dom_11(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_11_1 ON "are".fn_distrib_dom_11(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_11_2 ON "are".fn_distrib_dom_11(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_11_3 ON "are".fn_distrib_dom_11(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_11_4 ON "are".fn_distrib_dom_11(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_11_5 ON "are".fn_distrib_dom_11(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_11_6 ON "are".fn_distrib_dom_11(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_11_7 ON "are".fn_distrib_dom_11(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_11_8 ON "are".fn_distrib_dom_11(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_11 TO public AS are;



--DROP TABLE "are".fn_distrib_dom_12;
CREATE TABLE "are".fn_distrib_dom_12(
   nzp_dis SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   dat_oper DATE NOT NULL,
   sum_in DECIMAL(14,2) default 0,
   sum_rasp DECIMAL(14,2) default 0,
   sum_ud DECIMAL(14,2) default 0,
   sum_naud DECIMAL(14,2) default 0,
   sum_reval DECIMAL(14,2) default 0,
   sum_charge DECIMAL(14,2) default 0,
   sum_send DECIMAL(14,2) default 0,
   sum_out DECIMAL(14,2) default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnd_dom_12_1 ON "are".fn_distrib_dom_12(nzp_dis);

CREATE INDEX "are".ix_fnd_dom_12_2 ON "are".fn_distrib_dom_12(nzp_payer);

CREATE INDEX "are".ix_fnd_dom_12_3 ON "are".fn_distrib_dom_12(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fnd_dom_12_4 ON "are".fn_distrib_dom_12(nzp_area, nzp_serv);

CREATE INDEX "are".ix_fnd_dom_12_5 ON "are".fn_distrib_dom_12(nzp_serv);

CREATE INDEX "are".ix_fnd_dom_12_6 ON "are".fn_distrib_dom_12(nzp_bank);
CREATE INDEX "are".ix_fnd_dom_12_7 ON "are".fn_distrib_dom_12(nzp_dom);
CREATE INDEX "are".ix_fnd_dom_12_8 ON "are".fn_distrib_dom_12(nzp_area, nzp_dom, nzp_serv);


GRANT select, update, insert, delete, index ON fn_distrib_dom_12 TO public AS are;









--DROP TABLE "are".fn_pa_dom_01;

CREATE TABLE "are".fn_pa_dom_01(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_01_1 ON "are".fn_pa_dom_01(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_01_6 ON "are".fn_pa_dom_01(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_01_1 ON "are".fn_pa_dom_01(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_01_2 ON "are".fn_pa_dom_01(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_01_3 ON "are".fn_pa_dom_01(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_01_4 ON "are".fn_pa_dom_01(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_01 TO public AS are;


--DROP TABLE "are".fn_pa_dom_02;

CREATE TABLE "are".fn_pa_dom_02(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_02_1 ON "are".fn_pa_dom_02(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_02_6 ON "are".fn_pa_dom_02(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_02_1 ON "are".fn_pa_dom_02(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_02_2 ON "are".fn_pa_dom_02(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_02_3 ON "are".fn_pa_dom_02(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_02_4 ON "are".fn_pa_dom_02(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_02 TO public AS are;




--DROP TABLE "are".fn_pa_dom_03;

CREATE TABLE "are".fn_pa_dom_03(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_03_1 ON "are".fn_pa_dom_03(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_03_6 ON "are".fn_pa_dom_03(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_03_1 ON "are".fn_pa_dom_03(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_03_2 ON "are".fn_pa_dom_03(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_03_3 ON "are".fn_pa_dom_03(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_03_4 ON "are".fn_pa_dom_03(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_03 TO public AS are;




--DROP TABLE "are".fn_pa_dom_04;

CREATE TABLE "are".fn_pa_dom_04(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_04_1 ON "are".fn_pa_dom_04(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_04_6 ON "are".fn_pa_dom_04(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_04_1 ON "are".fn_pa_dom_04(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_04_2 ON "are".fn_pa_dom_04(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_04_3 ON "are".fn_pa_dom_04(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_04_4 ON "are".fn_pa_dom_04(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_04 TO public AS are;




--DROP TABLE "are".fn_pa_dom_05;

CREATE TABLE "are".fn_pa_dom_05(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_05_1 ON "are".fn_pa_dom_05(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_05_6 ON "are".fn_pa_dom_05(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_05_1 ON "are".fn_pa_dom_05(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_05_2 ON "are".fn_pa_dom_05(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_05_3 ON "are".fn_pa_dom_05(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_05_4 ON "are".fn_pa_dom_05(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_05 TO public AS are;



--DROP TABLE "are".fn_pa_dom_06;

CREATE TABLE "are".fn_pa_dom_06(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_06_1 ON "are".fn_pa_dom_06(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_06_6 ON "are".fn_pa_dom_06(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_06_1 ON "are".fn_pa_dom_06(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_06_2 ON "are".fn_pa_dom_06(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_06_3 ON "are".fn_pa_dom_06(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_06_4 ON "are".fn_pa_dom_06(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_06 TO public AS are;



--DROP TABLE "are".fn_pa_dom_07;

CREATE TABLE "are".fn_pa_dom_07(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_07_1 ON "are".fn_pa_dom_07(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_07_6 ON "are".fn_pa_dom_07(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_07_1 ON "are".fn_pa_dom_07(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_07_2 ON "are".fn_pa_dom_07(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_07_3 ON "are".fn_pa_dom_07(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_07_4 ON "are".fn_pa_dom_07(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_07 TO public AS are;



--DROP TABLE "are".fn_pa_dom_08;

CREATE TABLE "are".fn_pa_dom_08(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_08_1 ON "are".fn_pa_dom_08(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_08_6 ON "are".fn_pa_dom_08(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_08_1 ON "are".fn_pa_dom_08(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_08_2 ON "are".fn_pa_dom_08(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_08_3 ON "are".fn_pa_dom_08(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_08_4 ON "are".fn_pa_dom_08(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_08 TO public AS are;



--DROP TABLE "are".fn_pa_dom_09;

CREATE TABLE "are".fn_pa_dom_09(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_09_1 ON "are".fn_pa_dom_09(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_09_6 ON "are".fn_pa_dom_09(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_09_1 ON "are".fn_pa_dom_09(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_09_2 ON "are".fn_pa_dom_09(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_09_3 ON "are".fn_pa_dom_09(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_09_4 ON "are".fn_pa_dom_09(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_09 TO public AS are;



--DROP TABLE "are".fn_pa_dom_10;

CREATE TABLE "are".fn_pa_dom_10(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_10_1 ON "are".fn_pa_dom_10(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_10_6 ON "are".fn_pa_dom_10(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_10_1 ON "are".fn_pa_dom_10(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_10_2 ON "are".fn_pa_dom_10(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_10_3 ON "are".fn_pa_dom_10(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_10_4 ON "are".fn_pa_dom_10(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_10 TO public AS are;



--DROP TABLE "are".fn_pa_dom_11;

CREATE TABLE "are".fn_pa_dom_11(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_11_1 ON "are".fn_pa_dom_11(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_11_6 ON "are".fn_pa_dom_11(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_11_1 ON "are".fn_pa_dom_11(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_11_2 ON "are".fn_pa_dom_11(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_11_3 ON "are".fn_pa_dom_11(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_11_4 ON "are".fn_pa_dom_11(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_11 TO public AS are;



--DROP TABLE "are".fn_pa_dom_12;

CREATE TABLE "are".fn_pa_dom_12(
   nzp_pk SERIAL NOT NULL,
   nzp_dom INTEGER,
   nzp_supp INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_r DECIMAL(14,2) default 0 NOT NULL,
   sum_prih_g DECIMAL(14,2) default 0 NOT NULL,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_supp_w INTEGER default 0,
   nzp_area_w INTEGER default 0,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnp_dom_12_1 ON "are".fn_pa_dom_12(nzp_pk);

CREATE INDEX "are".ix_fnp_dom_12_6 ON "are".fn_pa_dom_12(nzp_dom, dat_oper, nzp_area, nzp_serv, nzp_supp);

CREATE INDEX "are".ix_fpt_dom_12_1 ON "are".fn_pa_dom_12(nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_12_2 ON "are".fn_pa_dom_12(nzp_serv);

CREATE INDEX "are".ix_fpt_dom_12_3 ON "are".fn_pa_dom_12(dat_oper, nzp_area, nzp_geu, nzp_supp, nzp_serv);

CREATE INDEX "are".ix_fpt_dom_12_4 ON "are".fn_pa_dom_12(dat_oper, nzp_serv);

GRANT select, update, insert, delete, index ON fn_pa_dom_12 TO public AS are;




--DROP TABLE "are".fn_perc_dom;

CREATE TABLE "are".fn_perc_dom(
   nzp_pr SERIAL NOT NULL,
   nzp_supp INTEGER,
   nzp_payer INTEGER,
   nzp_dom INTEGER,
   nzp_serv INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   sum_prih DECIMAL(14,2) default 0,
   sum_perc DECIMAL(14,2) default 0,
   perc_ud DECIMAL(4,2) default 0,
   dat_oper DATE,
   nzp_bl INTEGER,
   nzp_bank INTEGER default 0)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fnpr_dom_1 ON "are".fn_perc_dom(nzp_pr);

CREATE INDEX "are".ix_fnpr_dom_2 ON "are".fn_perc_dom(dat_oper, nzp_bank);

CREATE INDEX "are".ix_fnpr_dom_3 ON "are".fn_perc_dom(dat_oper, nzp_supp, nzp_dom,nzp_serv);

CREATE INDEX "are".ix_fnpr_dom_4 ON "are".fn_perc_dom(dat_oper, nzp_payer);

CREATE INDEX "are".ix_fnpr_dom_5 ON "are".fn_perc_dom(nzp_bank);

GRANT select, update, insert, delete, index ON fn_perc_dom TO public AS are;





--DROP TABLE "are".fn_naud_dom;

CREATE TABLE "are".fn_naud_dom(
   nzp_naud SERIAL NOT NULL,
   dat_oper DATE,
   nzp_payer INTEGER,
   nzp_dom INTEGER,
   nzp_serv INTEGER,
   nzp_payer_2 INTEGER,
   sum_ud DECIMAL(14,2) default 0 NOT NULL,
   sum_ud_r DECIMAL(14,2) default 0 NOT NULL,
   sum_ud_g DECIMAL(14,2) default 0 NOT NULL,
   sum_naud DECIMAL(14,2) default 0 NOT NULL,
   sum_naud_r DECIMAL(14,2) default 0 NOT NULL,
   sum_naud_g DECIMAL(14,2) default 0 NOT NULL,
   nzp_bl INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_naud_dom_0 ON "are".fn_naud_dom(nzp_naud);

CREATE INDEX "informix".ix_naudt_dom_1 ON "are".fn_naud_dom(dat_oper, nzp_area, nzp_geu, nzp_payer, nzp_serv);

CREATE INDEX "informix".ix_naudt_dom_2 ON "are".fn_naud_dom(dat_oper, nzp_area, nzp_geu, nzp_payer_2, nzp_serv);

CREATE INDEX "informix".ix_naudt_dom_3 ON "are".fn_naud_dom(nzp_payer, nzp_serv);

CREATE INDEX "informix".ix_naudt_dom_4 ON "are".fn_naud_dom(nzp_payer_2, nzp_serv);

GRANT select, update, insert, delete, index ON fn_naud_dom TO public AS are;




--DROP TABLE "are".fn_sended_dom;

CREATE TABLE "are".fn_sended_dom(
   nzp_snd SERIAL NOT NULL,
   dat_oper DATE NOT NULL,
   nzp_area INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_fd INTEGER NOT NULL,
   num_pp INTEGER default 0,
   dat_pp DATE,
   sum_send DECIMAL(13,2),
   nzp_send INTEGER,
   nzp_user INTEGER,
   dat_when DATE,
   nzp_snd_ret INTEGER default 0,
   id_bc_file INTEGER)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_fs_dom_1 ON "are".fn_sended_dom(nzp_snd);

CREATE INDEX "are".ix_fs_dom_2 ON "are".fn_sended_dom(dat_oper, nzp_area, nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fs_dom_3 ON "are".fn_sended_dom(nzp_area, nzp_dom,nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fs_dom_4 ON "are".fn_sended_dom(nzp_serv, nzp_payer);

CREATE INDEX "are".ix_fs_dom_5 ON "are".fn_sended_dom(nzp_payer, nzp_fd);

CREATE INDEX "are".ix_fs_dom_6 ON "are".fn_sended_dom(nzp_fd);

CREATE INDEX "are".ix_fs_dom_7 ON "are".fn_sended_dom(num_pp, dat_pp);

GRANT select, update, insert, delete, index ON fn_sended_dom TO public AS are;



--DROP TABLE "are".fn_reval_dom;

CREATE TABLE "are".fn_reval_dom(
   nzp_reval SERIAL NOT NULL,
   dat_oper DATE,
   nzp_payer INTEGER,
   nzp_dom INTEGER,
   nzp_serv INTEGER,
   nzp_payer_2 INTEGER,
   nzp_reval_2 INTEGER,
   sum_reval DECIMAL(14,2) default 0 NOT NULL,
   sum_reval_r DECIMAL(14,2) default 0 NOT NULL,
   sum_reval_g DECIMAL(14,2) default 0 NOT NULL,
   comment CHAR(60),
   nzp_bl INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   nzp_user INTEGER,
   dat_when DATE,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_reval_dom_01 ON "are".fn_reval_dom(nzp_reval);

CREATE INDEX "informix".ix_revt_dom_1 ON "are".fn_reval_dom(dat_oper, nzp_area, nzp_geu, nzp_payer, nzp_dom,nzp_serv);

CREATE INDEX "informix".ix_revt_dom_2 ON "are".fn_reval_dom(dat_oper, nzp_area, nzp_geu, nzp_payer_2,nzp_dom, nzp_serv);

CREATE INDEX "informix".ix_revt_dom_3 ON "are".fn_reval_dom(nzp_payer, nzp_dom,nzp_serv);

CREATE INDEX "informix".ix_revt_dom_4 ON "are".fn_reval_dom(nzp_payer_2,nzp_dom, nzp_serv);

CREATE INDEX "Administ".ix110_dom_5 ON "are".fn_reval_dom(nzp_reval_2);

GRANT select, update, insert, delete, index ON fn_reval_dom TO public AS are;



ALTER TABLE fn_distrib_dom_01 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_02 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_03 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_04 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_05 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_06 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_07 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_08 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_09 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_10 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_11 LOCK MODE (ROW);
ALTER TABLE fn_distrib_dom_12 LOCK MODE (ROW);

ALTER TABLE fn_pa_dom_01 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_02 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_03 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_04 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_05 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_06 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_07 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_08 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_09 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_10 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_11 LOCK MODE (ROW);
ALTER TABLE fn_pa_dom_12 LOCK MODE (ROW);

ALTER TABLE fn_reval_dom LOCK MODE (ROW);
ALTER TABLE fn_sended_dom LOCK MODE (ROW);
ALTER TABLE fn_naud_dom LOCK MODE (ROW);
ALTER TABLE fn_perc_dom LOCK MODE (ROW);


ALTER TABLE  fn_operday_log LOCK MODE (ROW);
ALTER TABLE  pack LOCK MODE (ROW);
ALTER TABLE  pack_ls LOCK MODE (ROW);
ALTER TABLE  gil_sums LOCK MODE (ROW);

ALTER TABLE  pack_log LOCK MODE (ROW);
ALTER TABLE  pack_ls_log LOCK MODE (ROW);


--изменения в CENTRAL_data

insert into sys_dictionary_values (nzp_dict, name, nzp_dict_parent, nzp_tdict, code, note) values (7427, 'Пересчёт сальдо перечисления', null, 101, null, null);

create table sys_event_detail (nzp_ev_det serial, nzp_event integer,table_ varchar(100), nzp integer);
GRANT select, update, insert, delete, index ON sys_event_detail TO public;
CREATE INDEX ixsys_even_d_1 ON sys_event_detail(nzp_event);
ALTER TABLE  sys_events LOCK MODE (ROW);
ALTER TABLE  sys_event_detail  LOCK MODE (ROW);


