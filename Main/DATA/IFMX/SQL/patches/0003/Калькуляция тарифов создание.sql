--Создание таблицы калькуляции тарифов
--CENTRAL_data
-- drop table tarif_calculation
create table tarif_calculation(
      nzp_prm_calc serial not null,
      dat_s date,
      dat_po date,
      nzp_area integer,
      nzp_serv integer,
      tarif   DECIMAL(12,3),
      nzp_prm integer
);
--drop index ix_prm_calculation_nzp_area
CREATE INDEX ix_prm_calculation_nzp_area ON tarif_calculation(nzp_area);
--drop index ix_prm_calculation_nzp_serv
CREATE INDEX ix_prm_calculation_nzp_serv ON tarif_calculation(nzp_serv);
--drop index ix_prm_calculation_nzp_prm
CREATE INDEX ix_prm_calculation_nzp_prm ON tarif_calculation(nzp_prm);

--перенос данных в таблицу калькуляции тарифов
SELECT s.nzp_serv ,
       se.service ,
       s.nzp_convbd*1000000+17*1000+a.ktr nzp_prm,
       v.val_prm ,
       v.dat_s,
       v.dat_po ,
       a.dt,
       a.kkst,
       a.ktr,
       a.nzp_conv_db ,
       max(a.sum) tarif
FROM smr34_data:arx9 a,
                     smr34_data:s_calc_line s,
                                            smr34_data:prm_5 v ,
                                                             smr34_kernel: services se
WHERE s.nzp_convbd=a.nzp_conv_db
  AND s.nzp_serv = se.nzp_serv
  AND s.nzp_serv = se.nzp_serv
  AND a.kkst=s.kodin
  AND a.sum>0.001
  AND v.nzp_prm=(s.nzp_convbd*1000000+17*1000+a.ktr)
  and a.dt between dat_s and dat_po
  AND v.is_actual<>100
GROUP BY 1,
         2,
         3,
         4,
         5,
         6,
         7,
         8,
         9,
         10
ORDER BY 3,
         4
into temp temp_tarif_calculation with no log;

insert into  fsmr_data:tarif_calculation ( dat_s, dat_po, nzp_area, nzp_serv, tarif, nzp_prm)
select unique dat_s, dat_po, nzp_conv_db, nzp_serv, tarif, nzp_prm from  temp_tarif_calculation;

drop table temp_tarif_calculation;