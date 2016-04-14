--central_kernel

delete from prm_name where nzp_prm in (1273,1274);
insert into prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)
 values (1273,'Расщепление - Распределение/расщепление оплат выполнять по','sprav',3020,10,Null,Null,Null);
insert into prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)
 values (1274,'Расщепление - Автоматически рассчитывать суммы к перечислению ','bool' ,null,10,null,null,null);

delete from resolution where nzp_res=3020;
delete from res_y      where nzp_res=3020;
delete from res_x      where nzp_res=3020;
delete from res_values where nzp_res=3020;

insert into resolution (nzp_res,name_short,name_res) values (3020,'ТРасщепОпл','Расщепление - Первоначально распределять оплату по');

insert into res_y (nzp_res,nzp_y,name_y) values (3020, 1, 'положительной части начислено к оплате прошлого месяца');
insert into res_y (nzp_res,nzp_y,name_y) values (3020, 2, 'начислено к оплате прошлого месяца');
insert into res_y (nzp_res,nzp_y,name_y) values (3020, 3, 'положительной части входящего сальдо текущего месяца');
insert into res_y (nzp_res,nzp_y,name_y) values (3020, 4, 'входящему сальдо текущего месяца');

insert into res_x (nzp_res,nzp_x,name_x) values (3020,1,'-');

insert into res_values (nzp_res,nzp_y,nzp_x,Value) values (3020, 1,1,'');
insert into res_values (nzp_res,nzp_y,nzp_x,Value) values (3020, 2,1,'');
insert into res_values (nzp_res,nzp_y,nzp_x,Value) values (3020, 3,1,'');
insert into res_values (nzp_res,nzp_y,nzp_x,Value) values (3020, 4,1,'');
