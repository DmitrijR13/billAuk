drop procedure "are".reparation_gil_sum(integer,integer,decimal,integer);
create procedure "are".reparation_gil_sum(pnzp_pack_ls integer,pnzp_serv integer,psum_prih decimal(14,2), pis_union integer) 
-- Версия 1.2 от 04.12.2013
delete from t_gil_sums where 1=1;
insert into  t_gil_sums (nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, isum_charge, sum_prih, sum_prih_u,koeff) 
select nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, sum_charge as isum_charge, psum_prih, 0.00 as sum_u, 0 as koeff  
from t_opl where nzp_pack_ls = pnzp_pack_ls  and nzp_serv = pnzp_serv;

if pis_union=1 then
insert into  t_gil_sums (nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, isum_charge, sum_prih, sum_prih_u,koeff ) 
select nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, sum_charge as isum_charge, psum_prih as sum_prih, 0.00 as sum_u, 0 as koeff  from t_opl
where nzp_pack_ls = pnzp_pack_ls  and nzp_serv in (select nzp_serv_uni from service_union where nzp_serv_base = pnzp_serv) and nzp_serv <> pnzp_serv;
end if;

update t_gil_sums set isum_charge = (select sum(sum_charge) from t_gil_sums);
update t_gil_sums set koeff = sum_charge/isum_charge;                                              
update t_gil_sums set sum_prih_u = koeff*sum_prih;

update t_opl set sum_prih_u = (select a.sum_prih_u from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp)
  where (select  count(*) from t_gil_sums b where b.nzp_pack_ls = t_opl.nzp_pack_ls and b.nzp_serv = t_opl.nzp_serv and b.nzp_supp = t_opl.nzp_supp)>0;

update t_opl set sum_prih = sum_prih+sum_prih_u where nzp_pack_ls = pnzp_pack_ls and sum_prih_u <>0;


END PROCEDURE;                                                                                                                                                    
grant execute on procedure "are".reparation_gil_sum(integer,integer,decimal,integer) to public as are;
