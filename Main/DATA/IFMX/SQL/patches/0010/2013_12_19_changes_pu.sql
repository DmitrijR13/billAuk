--выполн€етс€ в центральном _data (fsmr_data)   
create table reestr_changes_serv_supp
(
       nzp_reestr serial not null,
       dat_month date,
       file_name char(100),
	   status integer,
	   nzp_exc integer,
	   comment char(255),
       uploaded_on datetime year to second,
       uploaded_by integer
);
create index ind_dat_month on reestr_changes_serv_supp(dat_month);

--выполн€етс€ в центральной финансовой Ѕƒ (fsmr_fin_12 ... 13)
create table upd_changes_serv_supp
(
       nzp_changes serial not null,
       nzp_reestr integer,
       nzp_serv integer,
	   service char(100),
       nzp_supp integer,
	   name_supp char(100),
       inn CHAR(12),
       kpp CHAR(9),
       rchet CHAR(20),
       bik VARCHAR(9),
       status integer,
	   month_ integer
);
create index ind_nzp_changes_pu on upd_changes_serv_supp(nzp_reestr);

--выполн€етс€ в центральном _data (fsmr_data)   
alter table fn_bank add is_main integer default 0;

select * from fn_bank into temp temp_fn_bank with no log;
update fn_bank set is_main = (case when (select count(*) from temp_fn_bank b where fn_bank.nzp_payer = b.nzp_payer) = 1 then 1 else 0 end) ;
drop table temp_fn_bank;