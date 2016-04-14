--central data
create database fXXX_upload with log; 

CREATE TABLE file_area(
   id SERIAL NOT NULL,
   nzp_file INTEGER NOT NULL,
   area CHAR(40),
   jur_address CHAR(100),
   fact_address CHAR(100),
   inn CHAR(12),
   kpp CHAR(9),
   rs CHAR(20),
   bank CHAR(100),
   bik CHAR(20),
   ks CHAR(20),
   nzp_area INTEGER);
CREATE INDEX ix1_file_area ON file_area(id);

CREATE INDEX i1_file_area ON file_area(nzp_file, nzp_area);

CREATE INDEX ix2_file_area ON file_area(nzp_file, id, nzp_area);


CREATE TABLE file_blag(
   id SERIAL NOT NULL,
   id_prm INTEGER,
   name CHAR(100),
   nzp_file INTEGER,
   nzp_prm INTEGER);

CREATE TABLE file_dom(
   id DECIMAL(18,0),
   nzp_file INTEGER NOT NULL,
   ukds INTEGER,
   town CHAR(30),
   rajon CHAR(30),
   ulica CHAR(40),
   ndom CHAR(10),
   nkor CHAR(3),
   area_id DECIMAL(18,0) NOT NULL,
   cat_blago CHAR(30),
   etazh INTEGER NOT NULL,
   build_year DATE,
   total_square DECIMAL(14,2) NOT NULL,
   mop_square DECIMAL(14,2),
   useful_square DECIMAL(14,2),
   mo_id DECIMAL(13,0),
   params CHAR(250),
   ls_row_number INTEGER NOT NULL,
   odpu_row_number INTEGER NOT NULL,
   nzp_ul INTEGER,
   nzp_dom INTEGER,
   comment CHAR(250),
   local_id CHAR(20),
   nzp_raj INTEGER,
   nzp_town INTEGER,
   nzp_geu INTEGER,
   uch INTEGER,
   kod_kladr CHAR(30));

CREATE INDEX ix1_file_dom ON file_dom(id);

CREATE INDEX ix21_file_dom ON file_dom(nzp_file, id);

CREATE INDEX i1_file_dom ON file_dom(nzp_file, nzp_dom);

CREATE INDEX isss1a ON file_dom(nzp_file, area_id, id);


CREATE TABLE file_gaz(
   id SERIAL NOT NULL,
   id_prm INTEGER,
   name CHAR(100),
   nzp_file INTEGER,
   nzp_prm INTEGER);

CREATE INDEX ix1_file_gaz ON file_gaz(id_prm, id);


CREATE TABLE file_gilec(
   nzp_file INTEGER,
   num_ls INTEGER,
   nzp_gil INTEGER,
   nzp_kart INTEGER,
   nzp_tkrt INTEGER,
   fam CHAR(40),
   ima CHAR(40),
   otch CHAR(40),
   dat_rog DATE,
   fam_c CHAR(40),
   ima_c CHAR(40),
   otch_c CHAR(40),
   dat_rog_c DATE,
   gender CHAR(1),
   nzp_dok INTEGER,
   serij CHAR(10),
   nomer CHAR(7),
   vid_dat DATE,
   vid_mes CHAR(70),
   kod_podrazd CHAR(7),
   strana_mr CHAR(30),
   region_mr CHAR(30),
   okrug_mr CHAR(30),
   gorod_mr CHAR(30),
   npunkt_mr CHAR(30),
   rem_mr CHAR(180),
   strana_op CHAR(30),
   region_op CHAR(30),
   okrug_op CHAR(30),
   gorod_op CHAR(30),
   npunkt_op CHAR(30),
   rem_op CHAR(180),
   strana_ku CHAR(30),
   region_ku CHAR(30),
   okrug_ku CHAR(30),
   gorod_ku CHAR(30),
   npunkt_ku CHAR(30),
   rem_ku CHAR(180),
   rem_p CHAR(40),
   tprp CHAR(1),
   dat_prop DATE,
   dat_oprp DATE,
   dat_pvu DATE,
   who_pvu CHAR(40),
   dat_svu DATE,
   namereg CHAR(80),
   kod_namereg CHAR(7),
   rod CHAR(30),
   nzp_celp INTEGER,
   nzp_celu INTEGER,
   dat_sost DATE,
   dat_ofor DATE,
   comment CHAR(40),
   id SERIAL NOT NULL);

CREATE INDEX ix1_file_gilec ON file_gilec(num_ls, id);

CREATE INDEX ix2_file_gilec ON file_gilec(nzp_file, id, comment);



CREATE TABLE file_head(
   id SERIAL NOT NULL,
   nzp_file INTEGER NOT NULL,
   org_name CHAR(40) NOT NULL,
   branch_name CHAR(40) NOT NULL,
   inn CHAR(12) NOT NULL,
   kpp CHAR(9) NOT NULL,
   file_no INTEGER NOT NULL,
   file_date DATE NOT NULL,
   sender_phone CHAR(20) NOT NULL,
   sender_fio CHAR(80) NOT NULL,
   calc_date DATE NOT NULL,
   row_number INTEGER NOT NULL);



CREATE TABLE file_ipu(
   id SERIAL NOT NULL,
   nzp_file INTEGER NOT NULL,
   ls_id CHAR(20),
   nzp_serv INTEGER,
   rashod_type INTEGER,
   serv_type INTEGER,
   counter_type CHAR(25),
   cnt_stage INTEGER,
   mmnog INTEGER,
   num_cnt CHAR(20),
   dat_uchet DATE,
   val_cnt FLOAT,
   nzp_measure INTEGER,
   dat_prov DATE,
   dat_provnext DATE,
   nzp_kvar INTEGER,
   nzp_counter INTEGER,
   local_id CHAR(20),
   kod_serv CHAR(20),
   doppar CHAR(25));

CREATE INDEX ix1_file_ipu ON file_ipu(local_id, id);


CREATE TABLE file_ipu_p(
   id SERIAL NOT NULL,
   nzp_file INTEGER,
   id_ipu CHAR(20),
   rashod_type INTEGER,
   dat_uchet DATE,
   val_cnt FLOAT,
   kod_serv INTEGER);

CREATE INDEX ix1_file_ipu_p ON file_ipu_p(id_ipu, id);


CREATE TABLE file_kvar(
   id CHAR(20) NOT NULL,
   nzp_file INTEGER NOT NULL,
   ukas INTEGER,
   dom_id DECIMAL(18,0) NOT NULL,
   ls_type INTEGER NOT NULL,
   fam CHAR(40),
   ima CHAR(40),
   otch CHAR(40),
   birth_date DATE,
   nkvar CHAR(10),
   nkvar_n CHAR(3),
   open_date DATE,
   opening_osnov CHAR(100),
   close_date DATE,
   closing_osnov CHAR(100),
   kol_gil INTEGER NOT NULL,
   kol_vrem_prib INTEGER NOT NULL,
   kol_vrem_ub INTEGER NOT NULL,
   room_number INTEGER NOT NULL,
   total_square DECIMAL(14,2) NOT NULL,
   living_square DECIMAL(14,2),
   otapl_square DECIMAL(14,2),
   naim_square DECIMAL(14,2),
   is_communal INTEGER NOT NULL,
   is_el_plita INTEGER,
   is_gas_plita INTEGER,
   is_gas_colonka INTEGER,
   is_fire_plita INTEGER,
   gas_type INTEGER,
   water_type INTEGER,
   hotwater_type INTEGER,
   canalization_type INTEGER,
   is_open_otopl INTEGER,
   params CHAR(250),
   service_row_number INTEGER NOT NULL,
   reval_params_row_number INTEGER NOT NULL,
   ipu_row_number INTEGER NOT NULL,
   nzp_dom INTEGER,
   nzp_kvar INTEGER,
   comment CHAR(250),
   nzp_status INTEGER,
   id_urlic CHAR(20));

CREATE INDEX ix1_file_kvar ON file_kvar(id);

CREATE INDEX ix21_file_kvar ON file_kvar(nzp_file, id);


CREATE TABLE file_kvarp(
   id CHAR(20) NOT NULL,
   reval_month DATE,
   nzp_file INTEGER NOT NULL,
   fam CHAR(40),
   ima CHAR(40),
   otch CHAR(40),
   birth_date DATE,
   nkvar CHAR(10),
   nkvar_n CHAR(3),
   open_date DATE,
   opening_osnov CHAR(100),
   close_date DATE,
   closing_osnov CHAR(100),
   kol_gil INTEGER NOT NULL,
   kol_vrem_prib INTEGER NOT NULL,
   kol_vrem_ub INTEGER NOT NULL,
   room_number INTEGER NOT NULL,
   total_square DECIMAL(14,2) NOT NULL,
   living_square DECIMAL(14,2),
   otapl_square DECIMAL(14,2),
   naim_square DECIMAL(14,2),
   is_communal INTEGER NOT NULL,
   is_el_plita INTEGER,
   is_gas_plita INTEGER,
   is_gas_colonka INTEGER,
   is_fire_plita INTEGER,
   gas_type INTEGER,
   water_type INTEGER,
   hotwater_type INTEGER,
   canalization_type INTEGER,
   is_open_otopl INTEGER,
   params CHAR(250),
   nzp_dom INTEGER,
   nzp_kvar INTEGER,
   comment CHAR(250),
   nzp_status INTEGER,
   local_id CHAR(20));


CREATE TABLE file_measures(
   id SERIAL NOT NULL,
   id_measure INTEGER,
   measure CHAR(100),
   nzp_file INTEGER,
   nzp_measure INTEGER);

CREATE INDEX ix1_file_measures ON file_measures(id_measure, id);



CREATE TABLE file_mo(
   id SERIAL NOT NULL,
   id_mo INTEGER,
   vill CHAR(50),
   nzp_vill DECIMAL(13,0),
   nzp_raj INTEGER,
   nzp_file INTEGER,
   raj CHAR(60),
   mo_name CHAR(60));

CREATE INDEX ix1_file_mo ON file_mo(id_mo, id);



CREATE TABLE file_nedopost(
   id SERIAL NOT NULL,
   nzp_file INTEGER,
   ls_id CHAR(20),
   id_serv CHAR(20),
   type_ned DECIMAL(10,0),
   temper INTEGER,
   dat_nedstart DATE,
   dat_nedstop DATE,
   sum_ned DECIMAL(10,2));

CREATE INDEX ix1_file_nedopost ON file_nedopost(type_ned, id);


CREATE TABLE file_odpu(
   id SERIAL NOT NULL,
   nzp_file INTEGER NOT NULL,
   dom_id DECIMAL(18,0),
   nzp_serv INTEGER,
   rashod_type INTEGER,
   serv_type INTEGER,
   counter_type CHAR(25),
   cnt_stage INTEGER,
   mmnog INTEGER,
   num_cnt CHAR(20),
   dat_uchet DATE,
   val_cnt FLOAT,
   nzp_measure INTEGER,
   dat_prov DATE,
   dat_provnext DATE,
   nzp_dom INTEGER,
   nzp_counter INTEGER,
   local_id CHAR(20),
   doppar CHAR(25));

CREATE INDEX ix1_file_odpu ON file_odpu(local_id, id);


CREATE TABLE file_odpu_p(
   id SERIAL NOT NULL,
   nzp_file INTEGER,
   id_odpu CHAR(20),
   rashod_type INTEGER,
   dat_uchet DATE,
   val_cnt FLOAT,
   id_ipu INTEGER,
   kod_serv DECIMAL(10,0));

CREATE INDEX ix1_file_odpu_p ON file_odpu_p(id_odpu, id);


CREATE TABLE file_oplats(
   id SERIAL NOT NULL,
   ls_id CHAR(20),
   type_oper INTEGER,
   numplat CHAR(80),
   dat_opl DATE,
   dat_uchet DATE,
   dat_izm DATE,
   sum_oplat DECIMAL(14,2),
   ist_opl CHAR(80),
   mes_oplat DATE,
   nzp_file INTEGER,
   nzp_pack INTEGER,
   id_serv INTEGER);

CREATE INDEX ix1_file_oplats ON file_oplats(ls_id, id);


CREATE TABLE file_pack(
   id INTEGER,
   nzp_file INTEGER,
   dat_plat DATE,
   num_plat CHAR(20),
   sum_plat DECIMAL(14,2),
   kol_plat INTEGER);

CREATE INDEX ix1_file_pack ON file_pack(num_plat, id);


CREATE TABLE file_paramsdom(
   id SERIAL NOT NULL,
   id_dom CHAR(20),
   id_prm INTEGER,
   val_prm CHAR(100),
   nzp_dom INTEGER,
   nzp_file INTEGER);

CREATE INDEX ix1_file_paramsdom ON file_paramsdom(id_dom, id);


CREATE TABLE file_paramsls(
   id SERIAL NOT NULL,
   ls_id CHAR(20),
   id_prm INTEGER,
   val_prm CHAR(100),
   num_ls INTEGER,
   nzp_file INTEGER);

CREATE INDEX ix1_file_paramsls ON file_paramsls(ls_id, id);

CREATE TABLE file_section(
   id SERIAL NOT NULL,
   num_sec INTEGER,
   sec_name CHAR(100),
   nzp_file INTEGER,
   is_need_load INTEGER default 1);


CREATE TABLE file_serv(
   id SERIAL NOT NULL,
   nzp_file INTEGER NOT NULL,
   ls_id CHAR(20) NOT NULL,
   supp_id DECIMAL(18,0) NOT NULL,
   nzp_serv INTEGER NOT NULL,
   sum_insaldo DECIMAL(14,2) NOT NULL,
   eot DECIMAL(14,3) NOT NULL,
   reg_tarif_percent DECIMAL(14,3) NOT NULL,
   reg_tarif DECIMAL(14,3) NOT NULL,
   nzp_measure INTEGER NOT NULL,
   fact_rashod DECIMAL(18,7) NOT NULL,
   norm_rashod DECIMAL(18,7) NOT NULL,
   is_pu_calc INTEGER NOT NULL,
   sum_nach DECIMAL(14,2) NOT NULL,
   sum_reval DECIMAL(14,2) NOT NULL,
   sum_subsidy DECIMAL(14,2) NOT NULL,
   sum_subsidyp DECIMAL(14,2) NOT NULL,
   sum_lgota DECIMAL(14,2) NOT NULL,
   sum_lgotap DECIMAL(14,2) NOT NULL,
   sum_smo DECIMAL(14,2) NOT NULL,
   sum_smop DECIMAL(14,2) NOT NULL,
   sum_money DECIMAL(14,2) NOT NULL,
   is_del INTEGER NOT NULL,
   sum_outsaldo DECIMAL(14,2) NOT NULL,
   servp_row_number INTEGER NOT NULL,
   nzp_kvar INTEGER,
   nzp_supp INTEGER);

CREATE INDEX ix1_file_serv ON file_serv(ls_id, nzp_serv, nzp_measure, id);

CREATE INDEX ix21_file_serv ON file_serv(nzp_file, ls_id, nzp_serv, nzp_measure, id);

CREATE INDEX ix2_file_serv ON file_serv(nzp_file, ls_id, nzp_serv);


CREATE TABLE file_serv_tuning(
   id SERIAL NOT NULL,
   nzp_serv INTEGER,
   nzp_supp INTEGER,
   nzp_measure INTEGER,
   nzp_frm INTEGER);


CREATE TABLE file_services(
   id SERIAL NOT NULL,
   id_serv INTEGER,
   service CHAR(100),
   service2 CHAR(100),
   nzp_file INTEGER,
   nzp_measure INTEGER,
   ed_izmer CHAR(30),
   type_serv INTEGER,
   nzp_serv INTEGER);

CREATE INDEX ix1_file_services ON file_services(id_serv, id);


CREATE TABLE file_servls(
   id SERIAL NOT NULL,
   nzp_file INTEGER,
   ls_id DECIMAL(14,0),
   id_serv CHAR(100),
   dat_start DATE,
   dat_stop DATE,
   supp_id DECIMAL(18,0));

CREATE INDEX ix1_file_servls ON file_servls(ls_id, id);


CREATE TABLE file_servp(
   id SERIAL NOT NULL,
   nzp_file INTEGER NOT NULL,
   reval_month DATE,
   ls_id CHAR(20) NOT NULL,
   supp_id DECIMAL(18,0) NOT NULL,
   nzp_serv INTEGER NOT NULL,
   eot DECIMAL(14,3) NOT NULL,
   reg_tarif_percent DECIMAL(14,3) NOT NULL,
   reg_tarif DECIMAL(14,3) NOT NULL,
   nzp_measure INTEGER NOT NULL,
   fact_rashod DECIMAL(18,7) NOT NULL,
   norm_rashod DECIMAL(18,7) NOT NULL,
   is_pu_calc INTEGER NOT NULL,
   sum_reval DECIMAL(14,2) NOT NULL,
   sum_subsidyp DECIMAL(14,2) NOT NULL,
   sum_lgotap DECIMAL(14,2) NOT NULL,
   sum_smop DECIMAL(14,2) NOT NULL,
   nzp_kvar INTEGER,
   nzp_supp INTEGER);

CREATE INDEX ix1_file_servp ON file_servp(ls_id, id);



CREATE TABLE file_sql(
   id INTEGER NOT NULL,
   nzp_file INTEGER,
   sql_zapr CHAR(2000))
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE ROW;



CREATE TABLE file_supp(
   id SERIAL NOT NULL,
   nzp_file INTEGER NOT NULL,
   supp_id DECIMAL(18,0) NOT NULL,
   supp_name CHAR(25) NOT NULL,
   jur_address CHAR(100),
   fact_address CHAR(100),
   inn CHAR(12),
   kpp CHAR(9),
   rs CHAR(20),
   bank CHAR(100),
   bik CHAR(20),
   ks CHAR(20),
   nzp_supp INTEGER);

CREATE INDEX ix1_file_supp ON file_supp(supp_id, id);



CREATE TABLE file_typenedopost(
   id SERIAL NOT NULL,
   nzp_file INTEGER,
   type_ned DECIMAL(10,0),
   ned_name CHAR(100));

CREATE INDEX ix1_file_typenedopost ON file_typenedopost(type_ned, id);



CREATE TABLE file_typeparams(
   id SERIAL NOT NULL,
   id_prm INTEGER,
   prm_name CHAR(100) default '1',
   level_ INTEGER default 28,
   type_prm INTEGER default 2002,
   nzp_file INTEGER,
   nzp_prm INTEGER);



CREATE TABLE file_uchs(
   uch INTEGER,
   geu CHAR(50),
   iddom CHAR(15),
   nzp_dom INTEGER,
   nzp_geu INTEGER);



CREATE TABLE file_urlic(
   id SERIAL NOT NULL,
   nzp_file INTEGER NOT NULL,
   supp_id DECIMAL(18,0) NOT NULL,
   supp_name CHAR(100) NOT NULL,
   jur_address CHAR(100),
   fact_address CHAR(100),
   inn CHAR(12),
   kpp CHAR(9),
   rs CHAR(20),
   bank CHAR(100),
   bik_bank CHAR(20),
   ks CHAR(20),
   tel_chief CHAR(20),
   tel_b CHAR(20),
   chief_name CHAR(100),
   chief_post CHAR(40),
   b_name CHAR(100),
   okonh1 CHAR(20),
   okonh2 CHAR(20),
   okpo CHAR(20),
   bank_pr CHAR(100),
   bank_adr CHAR(100),
   bik CHAR(20),
   rs_pr CHAR(20),
   ks_pr CHAR(20),
   post_and_name CHAR(200),
   nzp_supp INTEGER);

CREATE INDEX ix1_file_urlic ON file_urlic(supp_id, id);



CREATE TABLE file_voda(
   id SERIAL NOT NULL,
   id_prm INTEGER,
   name CHAR(100),
   nzp_file INTEGER,
   nzp_prm INTEGER);

CREATE INDEX ix1_file_voda ON file_voda(id_prm, id);



CREATE TABLE file_vrub(
   id SERIAL NOT NULL,
   nzp_file INTEGER,
   ls_id CHAR(20),
   gil_id INTEGER,
   dat_vrvib DATE,
   dat_end DATE);

CREATE INDEX ix1_file_vrub ON file_vrub(ls_id, id);



CREATE TABLE files_imported(
   nzp_file SERIAL NOT NULL,
   nzp_version INTEGER NOT NULL,
   loaded_name CHAR(90),
   saved_name CHAR(90),
   nzp_status INTEGER NOT NULL,
   created_by INTEGER NOT NULL,
   created_on DATETIME YEAR to SECOND NOT NULL,
   file_type INTEGER,
   nzp_exc INTEGER,
   nzp_exc_log INTEGER,
   percent DECIMAL(3,2),
   pref CHAR(20),
   diss_status CHAR(50));

CREATE INDEX ix2_files_imported ON files_imported(nzp_file);



CREATE TABLE files_selected(
   nzp_file INTEGER,
   nzp_user INTEGER,
   pref CHAR(20),
   num INTEGER,
   comment VARCHAR(100));

CREATE INDEX fi_sel_1 ON files_selected(nzp_file, nzp_user, pref);



CREATE TABLE file_formats(
   nzp_ff SERIAL NOT NULL,
   format_name CHAR(90));

insert into file_formats
(nzp_ff, format_name)
 values (1, 'Стандартный');


CREATE TABLE file_statuses(
   nzp_stat SERIAL NOT NULL,
   status_name CHAR(90));


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
   version_name CHAR(90));

insert into file_versions
(nzp_version, nzp_ff, version_name)
 values (1, 1, '1.0');


insert into file_versions
(nzp_version, nzp_ff, version_name)
 values (3, 3, '1.2.1');


insert into file_versions
(nzp_version, nzp_ff, version_name)
 values (2, 2, '1.2.2');
