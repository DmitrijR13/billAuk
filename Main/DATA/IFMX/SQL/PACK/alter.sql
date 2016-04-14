database xxx_fin_xx;

alter table pack_ls add pkod decimal(13,0) default 0 before num_ls;
alter table pu_vals add num_cnt char(30) before pu_order; 
alter table pu_vals add nzp_serv integer default 0 before num_ls;