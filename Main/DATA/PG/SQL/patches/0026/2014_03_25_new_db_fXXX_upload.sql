--central data
CREATE SCHEMA fXXX_upload  AUTHORIZATION postgres; 
SET search_path TO 'fXXX_upload';

CREATE TABLE file_area
(
  id serial NOT NULL,
  nzp_file integer NOT NULL,
  area character(40),
  jur_address character(100),
  fact_address character(100),
  inn character(12),
  kpp character(9),
  rs character(20),
  bank character(100),
  bik character(20),
  ks character(20),
  nzp_area integer
);

CREATE INDEX ix2_file_area
  ON file_area
  USING btree
  (nzp_file, id, nzp_area);
  
  
  CREATE TABLE file_blag
(
  id serial NOT NULL,
  id_prm integer,
  name character(100),
  nzp_file integer,
  nzp_prm integer
);
  
  
  CREATE TABLE file_dom
(
  id numeric(18,0),
  nzp_file integer NOT NULL,
  ukds integer,
  town character(30),
  rajon character(30),
  ulica character(40),
  ndom character(10),
  nkor character(3),
  area_id numeric(18,0) NOT NULL,
  cat_blago character(30),
  etazh integer NOT NULL,
  build_year date,
  total_square numeric(14,2) NOT NULL,
  mop_square numeric(14,2),
  useful_square numeric(14,2),
  mo_id numeric(13,0),
  params character(250),
  ls_row_number integer NOT NULL,
  odpu_row_number integer NOT NULL,
  nzp_ul integer,
  nzp_dom integer,
  comment character(250),
  local_id character(20),
  nzp_raj integer,
  nzp_town integer,
  nzp_geu integer,
  uch integer
);

CREATE INDEX ix2_file_dom
  ON file_dom
  USING btree
  (nzp_file, nzp_dom);
  
  
CREATE TABLE file_gaz
(
  id serial NOT NULL,
  id_prm integer,
  name character(100),
  nzp_file integer,
  nzp_prm integer
);


CREATE TABLE file_gilec
(
  nzp_file integer,
  num_ls integer,
  nzp_gil integer,
  nzp_kart integer,
  nzp_tkrt integer,
  fam character(40),
  ima character(40),
  otch character(40),
  dat_rog date,
  fam_c character(40),
  ima_c character(40),
  otch_c character(40),
  dat_rog_c date,
  gender character(1),
  nzp_dok integer,
  serij character(10),
  nomer character(7),
  vid_dat date,
  vid_mes character(70),
  kod_podrazd character(7),
  strana_mr character(30),
  region_mr character(30),
  okrug_mr character(30),
  gorod_mr character(30),
  npunkt_mr character(30),
  rem_mr character(180),
  strana_op character(30),
  region_op character(30),
  okrug_op character(30),
  gorod_op character(30),
  npunkt_op character(30),
  rem_op character(180),
  strana_ku character(30),
  region_ku character(30),
  okrug_ku character(30),
  gorod_ku character(30),
  npunkt_ku character(30),
  rem_ku character(180),
  rem_p character(40),
  tprp character(1),
  dat_prop date,
  dat_oprp date,
  dat_pvu date,
  who_pvu character(40),
  dat_svu date,
  namereg character(80),
  kod_namereg character(7),
  rod character(30),
  nzp_celp integer,
  nzp_celu integer,
  dat_sost date,
  dat_ofor date,
  comment character(40),
  id serial NOT NULL
);

CREATE INDEX ix2_file_gilec
  ON file_gilec
  USING btree
  (nzp_file, id, comment COLLATE pg_catalog."default");
  
  
  CREATE TABLE file_head
(
  id serial NOT NULL,
  nzp_file integer NOT NULL,
  org_name character(40) NOT NULL,
  branch_name character(40) NOT NULL,
  inn character(12) NOT NULL,
  kpp character(9) NOT NULL,
  file_no integer NOT NULL,
  file_date date NOT NULL,
  sender_phone character(20) NOT NULL,
  sender_fio character(80) NOT NULL,
  calc_date date NOT NULL,
  row_number integer NOT NULL
);


CREATE TABLE file_ipu
(
  id serial NOT NULL,
  nzp_file integer NOT NULL,
  ls_id character(20),
  nzp_serv integer,
  rashod_type integer,
  serv_type integer,
  counter_type character(25),
  cnt_stage integer,
  mmnog integer,
  num_cnt character(20),
  dat_uchet date,
  val_cnt double precision,
  nzp_measure integer,
  dat_prov date,
  dat_provnext date,
  nzp_kvar integer,
  nzp_counter integer,
  local_id character(20),
  kod_serv character(20),
  doppar character(25)
);


CREATE TABLE file_ipu_p
(
  id serial NOT NULL,
  nzp_file integer,
  id_ipu character(20),
  rashod_type integer,
  dat_uchet date,
  val_cnt double precision,
  kod_serv integer
);

CREATE TABLE file_kvar
(
  id character(20) NOT NULL,
  nzp_file integer NOT NULL,
  ukas integer,
  dom_id numeric(18,0) NOT NULL,
  ls_type integer NOT NULL,
  fam character(40),
  ima character(40),
  otch character(40),
  birth_date date,
  nkvar character(10),
  nkvar_n character(3),
  open_date date,
  opening_osnov character(100),
  close_date date,
  closing_osnov character(100),
  kol_gil integer NOT NULL,
  kol_vrem_prib integer NOT NULL,
  kol_vrem_ub integer NOT NULL,
  room_number integer NOT NULL,
  total_square numeric(14,2) NOT NULL,
  living_square numeric(14,2),
  otapl_square numeric(14,2),
  naim_square numeric(14,2),
  is_communal integer NOT NULL,
  is_el_plita integer,
  is_gas_plita integer,
  is_gas_colonka integer,
  is_fire_plita integer,
  gas_type integer,
  water_type integer,
  hotwater_type integer,
  canalization_type integer,
  is_open_otopl integer,
  params character(250),
  service_row_number integer NOT NULL,
  reval_params_row_number integer NOT NULL,
  ipu_row_number integer NOT NULL,
  nzp_dom integer,
  nzp_kvar integer,
  comment character(250),
  nzp_status integer,
  id_urlic character(20)
);

CREATE INDEX ix2_file_kvar
  ON file_kvar
  USING btree
  (nzp_file, id COLLATE pg_catalog."default");
  
  
  CREATE TABLE file_kvarp
(
  id character(20) NOT NULL,
  reval_month date,
  nzp_file integer NOT NULL,
  fam character(40),
  ima character(40),
  otch character(40),
  birth_date date,
  nkvar character(10),
  nkvar_n character(3),
  open_date date,
  opening_osnov character(100),
  close_date date,
  closing_osnov character(100),
  kol_gil integer NOT NULL,
  kol_vrem_prib integer NOT NULL,
  kol_vrem_ub integer NOT NULL,
  room_number integer NOT NULL,
  total_square numeric(14,2) NOT NULL,
  living_square numeric(14,2),
  otapl_square numeric(14,2),
  naim_square numeric(14,2),
  is_communal integer NOT NULL,
  is_el_plita integer,
  is_gas_plita integer,
  is_gas_colonka integer,
  is_fire_plita integer,
  gas_type integer,
  water_type integer,
  hotwater_type integer,
  canalization_type integer,
  is_open_otopl integer,
  params character(250),
  nzp_dom integer,
  nzp_kvar integer,
  comment character(250),
  nzp_status integer,
  local_id character(20)
);


CREATE TABLE file_measures
(
  id serial NOT NULL,
  id_measure integer,
  measure character(100),
  nzp_file integer,
  nzp_measure integer
);


CREATE TABLE file_mo
(
  id serial NOT NULL,
  id_mo integer,
  vill character(50),
  nzp_vill numeric(13,0),
  nzp_raj integer,
  nzp_file integer,
  raj character(60),
  mo_name character(60)
);


CREATE TABLE file_nedopost
(
  id serial NOT NULL,
  nzp_file integer,
  ls_id character(20),
  id_serv character(20),
  type_ned numeric(10,0),
  temper integer,
  dat_nedstart date,
  dat_nedstop date,
  sum_ned numeric(10,2)
);


CREATE TABLE file_odpu
(
  id serial NOT NULL,
  nzp_file integer NOT NULL,
  dom_id numeric(18,0),
  nzp_serv integer,
  rashod_type integer,
  serv_type integer,
  counter_type character(25),
  cnt_stage integer,
  mmnog integer,
  num_cnt character(20),
  dat_uchet date,
  val_cnt double precision,
  nzp_measure integer,
  dat_prov date,
  dat_provnext date,
  nzp_dom integer,
  nzp_counter integer,
  local_id character(20),
  doppar character(25)
);


CREATE TABLE file_odpu_p
(
  id serial NOT NULL,
  nzp_file integer,
  id_odpu character(20),
  rashod_type integer,
  dat_uchet date,
  val_cnt double precision,
  id_ipu integer,
  kod_serv numeric(10,0)
);


CREATE TABLE file_oplats
(
  id serial NOT NULL,
  ls_id character(20),
  type_oper integer,
  numplat character(80),
  dat_opl date,
  dat_uchet date,
  dat_izm date,
  sum_oplat numeric(14,2),
  ist_opl character(80),
  mes_oplat date,
  nzp_file integer
);


CREATE TABLE file_pack
(
  id integer,
  nzp_file integer,
  dat_plat date,
  num_plat character(20),
  sum_plat numeric(14,2),
  kol_plat integer
);


CREATE TABLE file_paramsdom
(
  id serial NOT NULL,
  id_dom character(20),
  id_prm integer,
  val_prm character(100),
  nzp_dom integer,
  nzp_file integer
);


CREATE TABLE file_paramsls
(
  id serial NOT NULL,
  ls_id character(20),
  id_prm integer,
  val_prm character(100),
  num_ls integer,
  nzp_file integer
);


CREATE TABLE file_section
(
  id serial NOT NULL,
  num_sec integer,
  sec_name character(100),
  nzp_file integer,
  is_need_load integer DEFAULT 1
);


CREATE TABLE file_serv
(
  id serial NOT NULL,
  nzp_file integer NOT NULL,
  ls_id character(20) NOT NULL,
  supp_id numeric(18,0) NOT NULL,
  nzp_serv integer NOT NULL,
  sum_insaldo numeric(14,2) NOT NULL,
  eot numeric(14,3) NOT NULL,
  reg_tarif_percent numeric(14,3) NOT NULL,
  reg_tarif numeric(14,3) NOT NULL,
  nzp_measure integer NOT NULL,
  fact_rashod numeric(18,7) NOT NULL,
  norm_rashod numeric(18,7) NOT NULL,
  is_pu_calc integer NOT NULL,
  sum_nach numeric(14,2) NOT NULL,
  sum_reval numeric(14,2) NOT NULL,
  sum_subsidy numeric(14,2) NOT NULL,
  sum_subsidyp numeric(14,2) NOT NULL,
  sum_lgota numeric(14,2) NOT NULL,
  sum_lgotap numeric(14,2) NOT NULL,
  sum_smo numeric(14,2) NOT NULL,
  sum_smop numeric(14,2) NOT NULL,
  sum_money numeric(14,2) NOT NULL,
  is_del integer NOT NULL,
  sum_outsaldo numeric(14,2) NOT NULL,
  servp_row_number integer NOT NULL,
  nzp_kvar integer,
  nzp_supp integer
);

CREATE INDEX ix2_file_serv
  ON file_serv
  USING btree
  (nzp_file, ls_id COLLATE pg_catalog."default", nzp_serv);
  
  
  CREATE TABLE file_serv_tuning
(
  id serial NOT NULL,
  nzp_serv integer,
  nzp_supp integer,
  nzp_measure integer,
  nzp_frm integer
);


CREATE TABLE file_services
(
  id serial NOT NULL,
  id_serv integer,
  service character(100),
  service2 character(100),
  nzp_file integer,
  nzp_measure integer,
  ed_izmer character(30),
  type_serv integer,
  nzp_serv integer
);


CREATE TABLE file_servls
(
  id serial NOT NULL,
  nzp_file integer,
  ls_id numeric(14,0),
  id_serv character(100),
  dat_start date,
  dat_stop date
);


CREATE TABLE file_servp
(
  id serial NOT NULL,
  nzp_file integer NOT NULL,
  reval_month date,
  ls_id character(20) NOT NULL,
  supp_id numeric(18,0) NOT NULL,
  nzp_serv integer NOT NULL,
  eot numeric(14,3) NOT NULL,
  reg_tarif_percent numeric(14,3) NOT NULL,
  reg_tarif numeric(14,3) NOT NULL,
  nzp_measure integer NOT NULL,
  fact_rashod numeric(18,7) NOT NULL,
  norm_rashod numeric(18,7) NOT NULL,
  is_pu_calc integer NOT NULL,
  sum_reval numeric(14,2) NOT NULL,
  sum_subsidyp numeric(14,2) NOT NULL,
  sum_lgotap numeric(14,2) NOT NULL,
  sum_smop numeric(14,2) NOT NULL,
  nzp_kvar integer,
  nzp_supp integer
);


CREATE TABLE file_sql
(
  id integer NOT NULL,
  nzp_file integer,
  sql_zapr character(2000)
);


  
 CREATE TABLE file_supp
(
  id serial NOT NULL,
  nzp_file integer NOT NULL,
  supp_id numeric(18,0) NOT NULL,
  supp_name character(25) NOT NULL,
  jur_address character(100),
  fact_address character(100),
  inn character(12),
  kpp character(9),
  rs character(20),
  bank character(100),
  bik character(20),
  ks character(20),
  nzp_supp integer
); 
  
  
CREATE TABLE file_typenedopost
(
  id serial NOT NULL,
  nzp_file integer,
  type_ned numeric(10,0),
  ned_name character(100)
);  
  
  
  
  CREATE TABLE file_typeparams
(
  id serial NOT NULL,
  id_prm integer,
  prm_name character(100),
  level_ integer,
  type_prm integer,
  nzp_file integer,
  nzp_prm integer
);
  
  
 CREATE TABLE file_uchs
(
  uch integer,
  geu character(50),
  iddom character(15),
  nzp_dom integer,
  nzp_geu integer
); 
  
  
 CREATE TABLE file_urlic
(
  id serial NOT NULL,
  nzp_file integer NOT NULL,
  supp_id numeric(18,0) NOT NULL,
  supp_name character(100) NOT NULL,
  jur_address character(100),
  fact_address character(100),
  inn character(12),
  kpp character(9),
  rs character(20),
  bank character(100),
  bik_bank character(20),
  ks character(20),
  tel_chief character(20),
  tel_b character(20),
  chief_name character(100),
  chief_post character(40),
  b_name character(100),
  okonh1 character(20),
  okonh2 character(20),
  okpo character(20),
  bank_pr character(100),
  bank_adr character(100),
  bik character(20),
  rs_pr character(20),
  ks_pr character(20),
  post_and_name character(200),
  nzp_supp integer
); 
  
  
 CREATE TABLE file_voda
(
  id serial NOT NULL,
  id_prm integer,
  name character(100),
  nzp_file integer,
  nzp_prm integer
); 
  
  
 CREATE TABLE file_vrub
(
  id serial NOT NULL,
  nzp_file integer,
  ls_id character(20),
  gil_id integer,
  dat_vrvib date,
  dat_end date
); 
  
  
CREATE TABLE files_imported
(
  nzp_file serial NOT NULL,
  nzp_version integer NOT NULL,
  loaded_name character(90),
  saved_name character(90),
  nzp_status integer NOT NULL,
  created_by integer NOT NULL,
  created_on timestamp without time zone NOT NULL,
  file_type integer,
  nzp_exc integer,
  nzp_exc_log integer,
  percent numeric(3,2),
  diss_status character(50)
);

CREATE INDEX ix2_files_imported
  ON files_imported
  USING btree
  (nzp_file);
 
  
CREATE TABLE files_selected
(
  nzp_file integer,
  nzp_user integer,
  pref character(20),
  num integer,
  comment character varying(100)
);

CREATE INDEX fi_sel_1
  ON files_selected
  USING btree
  (nzp_file, nzp_user, pref COLLATE pg_catalog."default");  
  
  
  
  
 

CREATE TABLE file_formats(
   nzp_ff SERIAL NOT NULL,
   format_name character(90));

insert into file_formats
(nzp_ff, format_name)
 values (1, 'Стандартный');


CREATE TABLE file_statuses(
   nzp_stat SERIAL NOT NULL,
   status_name character(90));

insert into file_statuses (nzp_stat, status_name)  values (1, 'Загружается');
insert into file_statuses (nzp_stat, status_name)  values (2, 'Загружен');
insert into file_statuses (nzp_stat, status_name)  values (3, 'Загружен с ошибками');
insert into file_statuses (nzp_stat, status_name)  values (4, 'Учитывается');
insert into file_statuses (nzp_stat, status_name)  values (5, 'Учтен');
insert into file_statuses (nzp_stat, status_name)  values (6, 'Учтен с ошибками');
insert into file_statuses (nzp_stat, status_name)  values (7, 'Удален');



CREATE TABLE file_versions(
   nzp_version SERIAL NOT NULL,
   nzp_ff INTEGER,
   version_name character(90));
   
   
insert into file_versions
(nzp_version, nzp_ff, version_name)
 values (1, 1, '1.0');


insert into file_versions
(nzp_version, nzp_ff, version_name)
 values (3, 3, '1.2.1');


insert into file_versions
(nzp_version, nzp_ff, version_name)
 values (2, 2, '1.2.2');



