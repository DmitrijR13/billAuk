-- XXX_kernel CENTRAL AND LOCAL

insert into prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)
values (1281,'Вид платежного кода','sprav',3021,10,Null,Null,Null);
 
insert into resolution (nzp_res,name_short,name_res) values (3021,'ТВидПКода','Вид платежного кода');
 
insert into res_y (nzp_res,nzp_y,name_y) values (3021, 1, 'Стандарт');
insert into res_y (nzp_res,nzp_y,name_y) values (3021, 2, 'Самарская область');
insert into res_y (nzp_res,nzp_y,name_y) values (3021, 3, 'Татарстан');
 
insert into res_x (nzp_res,nzp_x,name_x) values (3021,1,'-');
 
insert into res_values (nzp_res,nzp_y,nzp_x,Value) values (3021, 1,1,'');
insert into res_values (nzp_res,nzp_y,nzp_x,Value) values (3021, 2,1,'');
insert into res_values (nzp_res,nzp_y,nzp_x,Value) values (3021, 3,1,'');
