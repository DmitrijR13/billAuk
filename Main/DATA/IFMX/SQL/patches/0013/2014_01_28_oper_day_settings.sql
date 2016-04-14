-- центральный kernel
delete from prm_name where nzp_prm in (1277,1278);
insert into prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1277,'–ежим смены операционного дн€','bool' ,null,10,null,null,null);
insert into prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1278,'¬рем€ автоматической смены операционного дн€','date' ,null,10,null,null,null);

-- центральный data
delete from prm_10 where nzp_prm in (1277,1278);
insert into prm_10 (nzp, nzp_prm, val_prm, dat_s, dat_po, is_actual, nzp_user, dat_when) values (0, 1277,     '0', '01.01.1900', '01.01.3000', 1, null, today);
insert into prm_10 (nzp, nzp_prm, val_prm, dat_s, dat_po, is_actual, nzp_user, dat_when) values (0, 1278, '00:00', '01.01.1900', '01.01.3000', 1, null, today);

