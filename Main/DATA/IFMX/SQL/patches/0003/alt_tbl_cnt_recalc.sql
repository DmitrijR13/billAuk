--local_charge_YY;

alter table  counters_01 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_02 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_03 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_04 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_05 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_06 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_07 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_08 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_09 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_10 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_11 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);
alter table  counters_12 add (ngp_cnt DECIMAL(14,7) default 0.0000000 before val1_g, rash_norm_one DECIMAL(14,7) default 0.0000000 before val1_g);


alter table  calc_gku_01 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_02 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_03 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_04 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_05 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_06 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_07 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_08 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_09 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_10 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_11 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
alter table  calc_gku_12 add (rash_norm_one DECIMAL(14,7) default 0.0000 NOT NULL before nzp_frm_typ,valm  DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   dop87 DECIMAL(15,7) default 0.0000 NOT NULL before nzp_frm_typ,   is_device INTEGER default 0 NOT NULL before nzp_frm_typ);
