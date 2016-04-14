-- database central_kernel;
-- database local_kernel;

-- type_rcl>100 - перекидка: с учетом объема и тарифа по услуге
alter table s_typercl add (is_volum integer default 0 before typename);

delete from s_typercl where type_rcl=63;
insert into s_typercl (type_rcl, is_volum, typename)  values (63, 0, 'Снятие ОДН');
delete from s_typercl where type_rcl=163;
insert into s_typercl (type_rcl, is_volum, typename) values (163, 1, 'Изменение расхода + Учет ОДН');

--database local_charge_GG;

alter table perekidka add (
 tarif DECIMAL(14,3) default 0 before sum_rcl,
 volum DECIMAL(14,6) default 0 before sum_rcl
);
