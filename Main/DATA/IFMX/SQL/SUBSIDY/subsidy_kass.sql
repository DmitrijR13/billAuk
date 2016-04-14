database subs_fin_xx;

drop table subs_kass_plan;  --Кассовый план
drop table subs_kass_plan_det; --Суммы по кассовому плану
drop table subs_month_plan; --Помесячный план распределения
drop table subs_month_plan_det; --Суммы и объемы по помесячному плану распределения

create table subs_kass_plan(
nzp_skp Serial(1) not null,
nzp_contract integer not null,
name_org char(150),
inn char(12),
kpp char(9),
responsible char(100),
date_begin Date,
date_end Date,
is_actual integer,
created_by integer,
created_on Date);

create unique index ix_skp_01 on subs_kass_plan(nzp_skp);
create index ix_skp_02 on subs_kass_plan(nzp_contract, is_actual); 

drop table subs_kass_plan_det; 
create table subs_kass_plan_det(
nzp_skpd Serial(1) not null,
nzp_skp integer not null,
sum_contract Decimal(14,2) default 0.00,
sum_nedop Decimal(14,2) default 0.00,
sum_dop_contract Decimal(14,2) default 0.00,
sum_subsidy01 Decimal(14,2) default 0.00,
sum_subsidy02 Decimal(14,2) default 0.00,
sum_subsidy03 Decimal(14,2) default 0.00,
sum_subsidy04 Decimal(14,2) default 0.00,
sum_subsidy05 Decimal(14,2) default 0.00,
sum_subsidy06 Decimal(14,2) default 0.00,
sum_subsidy07 Decimal(14,2) default 0.00,
sum_subsidy08 Decimal(14,2) default 0.00,
sum_subsidy09 Decimal(14,2) default 0.00,
sum_subsidy10 Decimal(14,2) default 0.00,
sum_subsidy11 Decimal(14,2) default 0.00,
sum_subsidy12 Decimal(14,2) default 0.00);

create unique index ix_skpd_01 on subs_kass_plan_det(nzp_skpd);
create index ix_skpd_02 on subs_kass_plan_det(nzp_skp); 

drop table subs_month_plan; 
create table subs_month_plan(
nzp_smp Serial(1) not null,
nzp_contract integer not null,
name_org char(150),
inn char(12),
kpp char(9),
responsible char(100),
plan_year integer,
date_begin Date,
date_end Date,
is_actual integer,
created_by integer,
created_on Date);

create unique index ix_smp_01 on subs_month_plan(nzp_smp);
create index ix_smp_02 on subs_month_plan(nzp_contract, is_actual); 


create table subs_month_plan_det(
nzp_smpd Serial(1) not null,
nzp_smp integer not null,
nzp_serv integer not null,
nzp_measure integer not null,
c_calc_all Decimal(14,2) default 0.00, 
middle_tarif Decimal(14,2) default 0.00, 
sum_subsidy_all Decimal(14,2) default 0.00,
c_calc01 Decimal(14,2) default 0.00,
c_calc02 Decimal(14,2) default 0.00,
c_calc03 Decimal(14,2) default 0.00,
c_calc04 Decimal(14,2) default 0.00,
c_calc05 Decimal(14,2) default 0.00,
c_calc06 Decimal(14,2) default 0.00,
c_calc07 Decimal(14,2) default 0.00,
c_calc08 Decimal(14,2) default 0.00,
c_calc09 Decimal(14,2) default 0.00,
c_calc10 Decimal(14,2) default 0.00,
c_calc11 Decimal(14,2) default 0.00,
c_calc12 Decimal(14,2) default 0.00,
tarif01 Decimal(14,2) default 0.00,
tarif02 Decimal(14,2) default 0.00,
tarif03 Decimal(14,2) default 0.00,
tarif04 Decimal(14,2) default 0.00,
tarif05 Decimal(14,2) default 0.00,
tarif06 Decimal(14,2) default 0.00,
tarif07 Decimal(14,2) default 0.00,
tarif08 Decimal(14,2) default 0.00,
tarif09 Decimal(14,2) default 0.00,
tarif10 Decimal(14,2) default 0.00,
tarif11 Decimal(14,2) default 0.00,
tarif12 Decimal(14,2) default 0.00,

sum_subsidy01 Decimal(14,2) default 0.00,
sum_subsidy02 Decimal(14,2) default 0.00,
sum_subsidy03 Decimal(14,2) default 0.00,
sum_subsidy04 Decimal(14,2) default 0.00,
sum_subsidy05 Decimal(14,2) default 0.00,
sum_subsidy06 Decimal(14,2) default 0.00,
sum_subsidy07 Decimal(14,2) default 0.00,
sum_subsidy08 Decimal(14,2) default 0.00,
sum_subsidy09 Decimal(14,2) default 0.00,
sum_subsidy10 Decimal(14,2) default 0.00,
sum_subsidy11 Decimal(14,2) default 0.00,
sum_subsidy12 Decimal(14,2) default 0.00);

create unique index ix_smpd_01 on subs_month_plan_det(nzp_smpd);
create index ix_smpd_02 on subs_month_plan_det(nzp_smp); 

