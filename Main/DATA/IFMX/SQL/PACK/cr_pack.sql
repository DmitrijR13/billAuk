database webXXX;

CREATE PROCEDURE pack_drp() on exception return; end exception with resume 
drop table source_pack;
END PROCEDURE; EXECUTE PROCEDURE pack_drp(); DROP PROCEDURE pack_drp;


CREATE PROCEDURE pack_drp() on exception return; end exception with resume 
drop table source_pack_ls;
END PROCEDURE; EXECUTE PROCEDURE pack_drp(); DROP PROCEDURE pack_drp;


CREATE PROCEDURE pack_drp() on exception return; end exception with resume 
drop table source_pu_vals;
END PROCEDURE; EXECUTE PROCEDURE pack_drp(); DROP PROCEDURE pack_drp;


CREATE PROCEDURE pack_drp() on exception return; end exception with resume 
drop table source_gil_sums;
END PROCEDURE; EXECUTE PROCEDURE pack_drp(); DROP PROCEDURE pack_drp;


create table source_pack(
nzp_spack SERIAL(1),
par_pack integer,
nzp_user integer,
nzp_session integer,
place_of_made char(200),
erc_code char(12),
num_pack char(10),
date_pack Date,
time_pack DATETIME HOUR to SECOND ,
date_oper Date,
count_in_pack integer,
sum_pack Decimal(14,2),
sum_nach Decimal(14,2),
sum_geton Decimal(13), 
version char(5),
filename char(250),
date_inp  DATE default Today,
time_inp  DATETIME YEAR to SECOND default Current YEAR to SECOND);

create unique index ix_spk_01 on source_pack(nzp_spack);
create index ix_spk_02 on source_pack(nzp_user);
create index ix_spk_03 on source_pack(nzp_session);
create index ix_spk_04 on source_pack(date_pack);


CREATE TABLE source_pack_ls(
   nzp_spack_ls SERIAL NOT NULL,
   nzp_spack INTEGER,
   paycode DECIMAL(13,0),
   num_ls INTEGER,
   pref CHAR(10),
   g_sum_ls DECIMAL(10,2),
   sum_ls DECIMAL(10,2),
   geton_ls DECIMAL(10,2),
   sum_peni DECIMAL(10,2),
   dat_month DATE,
   kod_sum SMALLINT,
   nzp_supp INTEGER,
   paysource INTEGER default 0,
   id_bill INTEGER,
   dat_vvod DATE,
   anketa CHAR(10),
   info_num INTEGER,
   unl INTEGER,
   erc_code CHAR(12));

create unique index ix_spl_01 on source_pack_ls(nzp_spack_ls);
create index ix_spl_02 on source_pack_ls(nzp_spack);
create index ix_spl_03 on source_pack_ls(paycode);
create index ix_spl_04 on source_pack_ls(num_ls);


CREATE TABLE source_pu_vals(
   nzp_spv SERIAL NOT NULL,
   nzp_spack_ls INTEGER,
   nzp_serv INTEGER,
   num_cnt Char(40),
   val_cnt DECIMAL(14,4),
   pu_order INTEGER, 
   ordering INTEGER);

create unique index ix_spv_01 on source_pu_vals(nzp_spv);
create index ix_spv_02 on source_pu_vals(nzp_spack_ls);
create index ix_spv_04 on source_pu_vals(nzp_serv);


CREATE TABLE source_gil_sums(
   nzp_ssums SERIAL NOT NULL,
   nzp_spack_ls INTEGER,
   days_nedo INTEGER,
   sum_oplat DECIMAL(10,2),
   ordering INTEGER);


create unique index ix_sgs_01 on source_gil_sums(nzp_ssums);
create index ix_sgs_02 on source_gil_sums(nzp_spack_ls);


