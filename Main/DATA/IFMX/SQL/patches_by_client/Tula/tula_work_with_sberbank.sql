--ВСЕ в central_data

CREATE TABLE "are".tula_file_reestr(
   nzp_reestr_d SERIAL NOT NULL,
   pkod DECIMAL(13,0),
   nzp_kvar INTEGER,
   nzp_kvit_reestr INTEGER,
   sum_charge DECIMAL(14,2),
   transaction_id CHAR(26),
   nomer_plat_poruch varchar(20),
   date_plat_poruch char(12),
   cnt1 CHAR(100),
   val_cnt1 DECIMAL(14,2),
   cnt2 CHAR(100),
   val_cnt2 DECIMAL(14,2),
   cnt3 CHAR(100),
   val_cnt3 DECIMAL(14,2),
   cnt4 CHAR(100),
   val_cnt4 DECIMAL(14,2),
   cnt5 CHAR(100),
   val_cnt5 DECIMAL(14,2),
   cnt6 CHAR(100),
   val_cnt6 DECIMAL(14,2))
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE row;
CREATE UNIQUE INDEX "are".ix_tula_file_reestr_1 ON "are".tula_file_reestr(nzp_reestr_d);

CREATE TABLE "are".tula_kvit_reestr(
   nzp_kvit_reestr SERIAL NOT NULL,
   date_plat DATE,
   file_name CHAR(20),
   kod_dop INTEGER,
   count_rows INTEGER,
   sum_plat DECIMAL(14,2),
   branch_id CHAR(1),
   is_itog INTEGER)
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE row;
 CREATE UNIQUE INDEX "are".ix_tula_kvit_reestr_1 ON "are".tula_kvit_reestr(nzp_kvit_reestr);
 
 
 CREATE TABLE "are".tula_reestr_downloads(
   nzp_download SERIAL NOT NULL,
   file_name CHAR(20),
   nzp_type INTEGER,
   date_download DATETIME YEAR to SECOND,
   user_downloaded INTEGER,
   branch_id CHAR(1),
   day INTEGER,
   month INTEGER)
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE row;
      CREATE UNIQUE INDEX "are".ix_tula_reestr_downloads_1 ON "are".tula_reestr_downloads(nzp_download);
     
     
     
CREATE TABLE "are".tula_reestr_sprav(
   nzp_type SERIAL NOT NULL,
   name_type CHAR(50))
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE PAGE; 
   CREATE UNIQUE INDEX "are".ix_tula_reestr_sprav_1 ON "are".tula_reestr_sprav(nzp_type);
  
   insert into "are".tula_reestr_sprav
(nzp_type, name_type)
 values (1, 'Периодический реестр');
 insert into "are".tula_reestr_sprav
(nzp_type, name_type)
 values (2, 'Квитанция для периодического реестра');
insert into "are".tula_reestr_sprav
(nzp_type, name_type)
 values (3, 'Итоговый реестр');
insert into "are".tula_reestr_sprav
(nzp_type, name_type)
 values (4, 'Квитанция для итогового реестра');
 


 CREATE TABLE "are".tula_reestr_unloads(
   nzp_reestr SERIAL NOT NULL,
   name_file CHAR(20),
   date_unload DATE,
   unloading_date DATE,
   user_unloaded INTEGER,
   nzp_exc INTEGER,
   is_actual integer default 0)
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE row;
   CREATE UNIQUE INDEX "are".ix_ttula_reestr_unloads_1 ON "are".tula_reestr_unloads(nzp_reestr);
   
   
   CREATE TABLE "are".tula_s_bank(
   id SERIAL NOT NULL,
   nzp_bank INTEGER,
   branch_id CHAR(1),
   branch_name CHAR(50))
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE row;
 CREATE UNIQUE INDEX "are".ix_tula_s_bank_1 ON "are".tula_s_bank(id); 
  insert into "are".tula_s_bank
(id, nzp_bank, branch_id, branch_name)
 values (1, 79999, 'E', 'Ефремовское ОСБ № 2639');
 insert into "are".tula_s_bank
(id, nzp_bank, branch_id, branch_name)
 values (2, 80000, 'A', 'Тульское ОСБ № 8604');
 insert into "are".tula_s_bank
(id, nzp_bank, branch_id, branch_name)
 values (3, 80001, 'X', 'Алексинское ОСБ № 2631');
 insert into "are".tula_s_bank
(id, nzp_bank, branch_id, branch_name)
 values (4, 80002, 'N', 'Новомосковское ОСБ № 2697');
 insert into "are".tula_s_bank
(id, nzp_bank, branch_id, branch_name)
 values (5, 80003, 'V', 'Суворовсое ОСБ № 7035');
 (id, nzp_bank, branch_id, branch_name)
 values (6, 1232, 'S', 'СберБанк');
 
  

      