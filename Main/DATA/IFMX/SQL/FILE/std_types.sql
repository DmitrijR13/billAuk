--CENTRAL_DATA-

CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_head; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_head
(
id serial not null,
nzp_file integer not null,
org_name char(40) not null,
branch_name char(40) not null,
inn char(12) not null,
kpp char(9) not null,
file_no integer not null,
file_date date not null,
sender_phone char(20) not null,
sender_fio char(80) not null,
calc_date date not null,
row_number integer not null
);

create unique index ix_file_head_1 on file_head(id);
create unique index ix_file_head_2 on file_head(nzp_file);
ALTER TABLE file_head ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_head;


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_area; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_area
(
id decimal(18,0) not null,
nzp_file integer not null,
area char(25) not null,
jur_address char(100),
fact_address char(100),
inn char(12),
kpp char(9),
rs char(20),
bank char(100),
bik char(20),
ks char(20),
nzp_area integer
);

create unique index ix_file_area_1 on file_area(id, nzp_file);
create index ix_file_area_2 on file_area(nzp_file);
--ALTER TABLE file_area ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_area;


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_dom; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_dom
(
id decimal(18,0) not null,
nzp_file integer not null,
ukds integer,
town char(30),
rajon char(30),
ulica char(40),
ndom char(10),
nkor char(3),
area_id decimal(18,0) not null,
cat_blago char(30),
etazh integer not null,
build_year date,
total_square decimal(14,2) not null,
mop_square decimal(14,2),
useful_square decimal(14,2),
mo_id decimal(13,0),
params char(250),
ls_row_number integer not null,
odpu_row_number integer not null,
nzp_ul integer,
nzp_dom integer,
comment char(250)
);

create unique index ix_file_dom_1 on file_dom(id, nzp_file);
create index ix_file_dom_2 on file_dom(nzp_file);
--ALTER TABLE file_dom ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_dom;
--ALTER TABLE file_dom ADD CONSTRAINT (FOREIGN KEY (area_id) REFERENCES file_area CONSTRAINT "are".fk_file_dom_1);


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_kvar; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_kvar
(
id char(20) not null,
nzp_file integer not null,
ukas integer,
dom_id decimal(18,0) not null,
ls_type integer not null,
fam char(40),
ima char(40),
otch char(40),
birth_date date,
nkvar char(10),
nkvar_n char(3),
open_date date,
opening_osnov char(100),
close_date date,
closing_osnov char(100),
kol_gil integer not null,
kol_vrem_prib integer not null,
kol_vrem_ub integer not null,
room_number integer not null,
total_square decimal(14,2) not null,
living_square decimal(14,2),
otapl_square decimal(14,2),
naim_square decimal(14,2),
is_communal integer not null,
is_el_plita integer,
is_gas_plita integer,
is_gas_colonka integer,
is_fire_plita integer,
gas_type integer,
water_type integer,
hotwater_type integer,
canalization_type integer,
is_open_otopl integer,
params char(250),
service_row_number integer not null,
reval_params_row_number integer not null,
ipu_row_number integer not null,
nzp_dom integer,
nzp_kvar integer,
comment char(250),
nzp_status integer
);

create unique index ix_file_kvar_1 on file_kvar(id, nzp_file);
create index ix_file_kvar_2 on file_kvar(nzp_file);
--ALTER TABLE file_kvar ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_kvar;
--ALTER TABLE file_kvar ADD CONSTRAINT (FOREIGN KEY (dom_id) REFERENCES file_dom CONSTRAINT "are".fk_file_kvar_1);


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_supp; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_supp
(
id serial not null,
nzp_file integer not null,
supp_id decimal(18,0) not null,
supp_name char(25) not null,
jur_address char(100),
fact_address char(100),
inn char(12),
kpp char(9),
rs char(20),
bank char(100),
bik char(20),
ks char(20),
nzp_supp integer
);

create unique index ix_file_supp_1 on file_supp(id);
create unique index ix_file_supp_2 on file_supp(supp_id, nzp_file);
create index ix_file_supp_3 on file_supp(nzp_file);
ALTER TABLE file_supp ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_supp;


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_serv; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_serv
(
id serial not null,
nzp_file integer not null,
ls_id char(20) not null,
supp_id decimal(18,0) not null,
nzp_serv integer not null,
sum_insaldo decimal(14,2) not null,
eot decimal(14,3) not null,
reg_tarif_percent decimal(14,3) not null,
reg_tarif decimal(14,3) not null,
nzp_measure integer not null,
fact_rashod decimal(18,7) not null,
norm_rashod decimal(18,7) not null,
is_pu_calc integer not null,
sum_nach decimal(14,2) not null,
sum_reval decimal(14,2) not null,
sum_subsidy decimal(14,2) not null,
sum_subsidyp decimal(14,2) not null,
sum_lgota decimal(14,2) not null,
sum_lgotap decimal(14,2) not null,
sum_smo decimal(14,2) not null,
sum_smop decimal(14,2) not null,
sum_money decimal(14,2) not null,
is_del integer not null,
sum_outsaldo decimal(14,2) not null,
servp_row_number integer not null,
nzp_kvar integer,
nzp_supp integer
);

create unique index ix_file_serv_1 on file_serv(id);
create index ix_file_serv_2 on file_serv(nzp_file);
ALTER TABLE file_serv ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_serv;


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_kvarp; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_kvarp
(
id char(20) not null,
nzp_file integer not null,
reval_month date,
fam char(40),
ima char(40),
otch char(40),
birth_date date,
nkvar char(10),
nkvar_n char(3),
open_date date,
opening_osnov char(100),
close_date date,
closing_osnov char(100),
kol_gil integer not null,
kol_vrem_prib integer not null,
kol_vrem_ub integer not null,
room_number integer not null,
total_square decimal(14,2) not null,
living_square decimal(14,2),
otapl_square decimal(14,2),
naim_square decimal(14,2),
is_communal integer not null,
is_el_plita integer,
is_gas_plita integer,
is_gas_colonka integer,
is_fire_plita integer,
gas_type integer,
water_type integer,
hotwater_type integer,
canalization_type integer,
is_open_otopl integer,
params char(250),
nzp_dom integer,
nzp_kvar integer,
comment char(250),
nzp_status integer
);

create unique index ix_file_kvarp_1 on file_kvarp(id, nzp_file);
create index ix_file_kvarp_2 on file_kvarp(nzp_file);
--ALTER TABLE file_kvarp ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_kvarp;
--ALTER TABLE file_kvarp ADD CONSTRAINT (FOREIGN KEY (dom_id) REFERENCES file_dom CONSTRAINT "are".fk_file_kvarp_1);


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_servp; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_servp
(
id serial not null,
nzp_file integer not null,
reval_month date,
ls_id char(20) not null,
supp_id decimal(18,0) not null,
nzp_serv integer not null,
eot decimal(14,3) not null,
reg_tarif_percent decimal(14,3) not null,
reg_tarif decimal(14,3) not null,
nzp_measure integer not null,
fact_rashod decimal(18,7) not null,
norm_rashod decimal(18,7) not null,
is_pu_calc integer not null,
sum_reval decimal(14,2) not null,
sum_subsidyp decimal(14,2) not null,
sum_lgotap decimal(14,2) not null,
sum_smop decimal(14,2) not null,
nzp_kvar integer,
nzp_supp integer
);

create unique index ix_file_servp_1 on file_servp(id);
create index ix_file_servp_2 on file_servp(nzp_file);
ALTER TABLE file_servp ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_servp;


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_odpu; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_odpu
(
id serial not null,
nzp_file integer not null,
dom_id DECIMAL(18,0),
nzp_serv integer,
rashod_type integer,
serv_type integer,
counter_type char(25),
cnt_stage integer,
mmnog integer,
num_cnt char(20),
dat_uchet date,
val_cnt FLOAT,
nzp_measure integer,
dat_prov date,
dat_provnext date,
nzp_dom integer,
nzp_counter integer
);

create unique index ix_file_odpu_1 on file_odpu(id);
create index ix_file_odpu_2 on file_odpu(nzp_file);
ALTER TABLE file_odpu ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_odpu;


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
	drop table file_ipu; 
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

create table "are".file_ipu
(
id serial not null,
nzp_file integer not null,
ls_id char(20),
nzp_serv integer,
rashod_type integer,
serv_type integer,
counter_type char(25),
cnt_stage integer,
mmnog integer,
num_cnt char(20),
dat_uchet date,
val_cnt FLOAT,
nzp_measure integer,
dat_prov date,
dat_provnext date,
nzp_kvar integer,
nzp_counter integer
);

create unique index ix_file_ipu_1 on file_ipu(id);
create index ix_file_ipu_2 on file_ipu(nzp_file);
ALTER TABLE file_ipu ADD CONSTRAINT PRIMARY KEY (id) CONSTRAINT "are".pk_file_ipu;
