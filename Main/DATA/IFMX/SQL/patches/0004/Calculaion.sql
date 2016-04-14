-- центр
-- database XXX_kernel

--DROP TABLE "are".services_sg;
CREATE TABLE "are".services_sg(
   nzp_serv INTEGER               
);
delete from services_sg where 1=1;
insert into services_sg(nzp_serv) 
select nzp_serv from fsmr_kernel:services where nzp_serv in (16,19,205,211,221,234,242,243,256,259,290,315);

CREATE INDEX "are".ix_services_sg ON services_sg(nzp_serv);
update statistics for table services_sg;

--локал
-- database XXX_data
ALTER TABLE s_calc_trf add(nzp_area INTEGER before nzp_frm);  
ALTER TABLE s_calc_trf_lnk modify( nzp_tarif INTEGER NOT NULL );
ALTER TABLE  s_calc_trf_lnk add  (nzp_trfl INTEGER before nzp_tarif);
create sequence SeqTrf ;
update s_calc_trf_lnk set nzp_trfl=SeqTrf.nextval;
alter table s_calc_trf_lnk modify nzp_trfl serial not null;
