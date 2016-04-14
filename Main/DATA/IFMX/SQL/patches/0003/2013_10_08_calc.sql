--central_data & local_data

--DROP TABLE "are".link_ls_lit; 
CREATE TABLE "are".link_ls_lit(
   nzp_ls SERIAL NOT NULL,
   nzp_dom  INTEGER NOT NULL,
   nzp_kvar_base INTEGER NOT NULL,
   nzp_kvar INTEGER NOT NULL)
;

CREATE UNIQUE INDEX "are".ix0_link_ls_lit ON link_ls_lit(nzp_ls);
CREATE        INDEX "are".ix1_link_ls_lit ON link_ls_lit(nzp_kvar_base, nzp_kvar);
CREATE        INDEX "are".ix2_link_ls_lit ON link_ls_lit(nzp_dom, nzp_kvar);
CREATE        INDEX "are".ix3_link_ls_lit ON link_ls_lit(nzp_kvar);

update statistics for table link_ls_lit;

--local_charge_YY;

alter table reval_01 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_02 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_03 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_04 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_05 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_06 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_07 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_08 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_09 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_10 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_11 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);
alter table reval_12 add (tarif DECIMAL(14,4) default 0.0000 NOT NULL,tarif_p DECIMAL(14,4) default 0.0000 NOT NULL, sum_tarif DECIMAL(14,2) default 0.00 NOT NULL,sum_tarif_p DECIMAL(14,2) default 0.00 NOT NULL, sum_nedop DECIMAL(14,2) default 0.00 NOT NULL,sum_nedop_p DECIMAL(14,2) default 0.00 NOT NULL,c_calc DECIMAL(14,4) default 0.0000, c_calc_p DECIMAL(14,4) default 0.0000, c_calcm_p DECIMAL(14,4) default 0.0000,nzp_frm INTEGER  default 0,nzp_frm_p INTEGER  default 0,type_rsh INTEGER  default 0,kod_info INTEGER  default 0);

alter table reval_01 add (month_p integer default 0,year_p integer default 0);
alter table reval_02 add (month_p integer default 0,year_p integer default 0);
alter table reval_03 add (month_p integer default 0,year_p integer default 0);
alter table reval_04 add (month_p integer default 0,year_p integer default 0);
alter table reval_05 add (month_p integer default 0,year_p integer default 0);
alter table reval_06 add (month_p integer default 0,year_p integer default 0);
alter table reval_07 add (month_p integer default 0,year_p integer default 0);
alter table reval_08 add (month_p integer default 0,year_p integer default 0);
alter table reval_09 add (month_p integer default 0,year_p integer default 0);
alter table reval_10 add (month_p integer default 0,year_p integer default 0);
alter table reval_11 add (month_p integer default 0,year_p integer default 0);
alter table reval_12 add (month_p integer default 0,year_p integer default 0);


alter table delta_01 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_02 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_03 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_04 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_05 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_06 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_07 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_08 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_09 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_10 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_11 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_12 add (valm DECIMAL(14,4) default 0.0000 NOT NULL,valm_p DECIMAL(14,4) default 0.0000 NOT NULL);

alter table delta_01 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_02 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_03 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_04 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_05 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_06 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_07 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_08 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_09 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_10 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_11 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
alter table delta_12 add (dop87 DECIMAL(14,4) default 0.0000 NOT NULL,dop87_p DECIMAL(14,4) default 0.0000 NOT NULL);
