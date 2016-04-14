--central_kernel

drop procedure "are".reparation_gil_sum(integer,integer,decimal,integer);
create procedure "are".reparation_gil_sum(pnzp_pack_ls integer,pnzp_serv integer,psum_prih decimal(14,2), pis_union integer) 
define count_ integer;

-- Версия 1.2. от 01.09.2013
insert into  t_gil_sums (nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, isum_charge, sum_prih, sum_prih_u,koeff) 
select nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge_prev, sum_charge_prev as isum_charge, psum_prih, 0.00 as sum_u, 
0 as koeff  from t_opl where nzp_pack_ls = pnzp_pack_ls  and nzp_serv = pnzp_serv;



if pis_union=1 then
insert into  t_gil_sums (nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, isum_charge, sum_prih, sum_prih_u,koeff ) 
select nzp_key,nzp_pack_ls,nzp_serv,nzp_supp, isdel, sum_charge, sum_charge_prev as isum_charge, psum_prih as sum_prih, 0.00 as sum_u, 0 as koeff  from t_opl
where nzp_pack_ls = pnzp_pack_ls  and nzp_serv in (select nzp_serv_uni from service_union where nzp_serv_base = pnzp_serv) and nzp_serv <> pnzp_serv;
end if;

let count_=0;
select count(*) into count_ from t_gil_sums;

if count_=1 then
   update t_gil_sums set sum_prih = psum_prih, sum_prih_u=psum_prih;

else 
   update t_gil_sums set isum_charge = (select sum(sum_charge) from t_gil_sums);
   update t_gil_sums set koeff = sum_charge/isum_charge where isum_charge<>0;                                              
   update t_gil_sums set sum_prih_u = koeff*sum_prih;
end if;

   update t_opl set  sum_prih_u = (select SUM(a.sum_prih_u) from t_gil_sums a where a.nzp_pack_ls = t_opl.nzp_pack_ls and a.nzp_serv = t_opl.nzp_serv and a.nzp_supp = t_opl.nzp_supp)
      where (select  count(*) from t_gil_sums b where b.nzp_pack_ls = t_opl.nzp_pack_ls and b.nzp_serv = t_opl.nzp_serv and b.nzp_supp = t_opl.nzp_supp)>0;

   update t_opl set   sum_prih = 0 where nzp_pack_ls = pnzp_pack_ls and nzp_serv =pnzp_serv;
   update t_opl set   sum_prih = sum_prih+sum_prih_u where nzp_pack_ls = pnzp_pack_ls and sum_prih_u <>0 and nzp_serv = pnzp_serv;


END PROCEDURE;                            
grant execute on procedure "are".reparation_gil_sum(integer,integer,decimal,integer) to public as are;


drop function "are".getsumostatok(integer);
create function "are".getSumOstatok(pnzp_pack_ls integer) returning decimal(14,2); 
define sum_out decimal(14,2); 
-- Версия 1.2 от 01.09.2013
let sum_out = 0; 
select AVG(g_sum_ls)-SUM(sum_prih_d+sum_prih_a+sum_prih_u+sum_prih_s) 
into sum_out from t_opl where nzp_pack_ls = pnzp_pack_ls; return sum_out; 
end function;                                                                                                                                                                                                                                 
grant execute on function "are".getsumostatok(integer) to public as are;


drop function "are".getsumprih(decimal,decimal,integer);
create function "are".getSumPrih(sum_etalon decimal(14,2),sum_prih decimal(14,2), isdel integer) 
returning decimal(14,2); 
define sum_out decimal(14,2); 
-- Версия 1.2 от 01.09.2013
if isdel=0  then  let sum_out= sum_prih; 
else 
   if sum_prih > sum_etalon then let sum_out= sum_etalon; 
   else let sum_out= sum_prih; 
   end if; 
end if; 
return sum_out; 
end function;                                                                                                                                                                                          
grant execute on function "are".getsumprih(decimal,decimal,integer) to public as are;
