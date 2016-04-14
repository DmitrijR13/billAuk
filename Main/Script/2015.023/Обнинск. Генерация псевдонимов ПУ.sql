drop function if exists public.create_counter_pseudonym(text);

create function public.create_counter_pseudonym(pref text) returns text as
$BODY$
DECLARE
  apref text;
  cnt   integer;
BEGIN
  EXECUTE 'set search_path to ' || pref || '_kernel';

  for apref in
    select trim(bd_kernel) from s_point where flag <> 1 order by 1
  loop
    RAISE NOTICE 'Начало. Банк данных: (%)', apref;

    EXECUTE 'select count(*) from ' || apref || '_kernel.prm_name where nzp_prm = 1426 ' into cnt;
    if (cnt <= 0) then
      EXECUTE 'insert into ' || apref || '_kernel.prm_name (nzp_prm, name_prm, old_field, type_prm, prm_num, is_day_uchet) values 
      (1426, ''Псевдоним ПУ для выгрузки'', 0, ''char'', 17, 0)';
    end if;

    EXECUTE 'delete from ' || apref || '_kernel.s_reg_prm where nzp_prm = 1426';
    EXECUTE 'select max(numer) from ' || apref || '_kernel.s_reg_prm where nzp_reg = 6 ' into cnt;
    cnt := coalesce(cnt, 0) + 1;
    EXECUTE 'insert into ' || apref || '_kernel.s_reg_prm (nzp_reg, nzp_prm, nzp_serv, numer, is_show) values (6, 1426, 0, ' || cnt || ', 1)';
     
    drop table if exists tmp_cnts;
    create temp table tmp_cnts (
      nzp_serial  serial, 
      nzp         integer, 
      nzp_counter integer,
      nzp_cnt     integer,
      num         integer
    );

    EXECUTE 'insert into tmp_cnts (nzp_counter, nzp, nzp_cnt)
      select nzp_counter, nzp, nzp_cnt from ' || apref || '_data.counters_spis
      where nzp_cnt in (1,2,3) and dat_close is null and is_actual <> 100 
      order by nzp_counter'; -- сортировка

    create index ix_tmp_cnts_1 on tmp_cnts(nzp);
    analyze tmp_cnts;

    -- определение номеров 
    drop table if exists tmp_num;
    create temp table tmp_num (
      nzp_counter integer,
      nzp         integer,
      nzp_cnt     integer, 
      num         integer
    );

    insert into tmp_num (nzp, nzp_counter, nzp_cnt, num) 
    select a.nzp, a.nzp_counter, nzp_cnt, (select count(*) from tmp_cnts b where a.nzp = b.nzp  and a.nzp_cnt = b.nzp_cnt and a.nzp_serial >= b.nzp_serial) as num
    from tmp_cnts a;

    create index ind_xn1 on tmp_num(nzp);
    analyze tmp_num;

    update tmp_cnts a set num = (select b.num from tmp_num b where b.nzp_counter = a.nzp_counter and b.nzp = a.nzp and b.nzp_cnt = a.nzp_cnt);

    EXECUTE 'update ' || apref || '_data.prm_17 p17 set is_actual = 100 where exists (select 1 from tmp_cnts t where t.nzp_counter = p17.nzp) and p17.nzp_prm = 1426 and p17.is_actual = 1';

    -- 1 Электроэнергия
    -- 2 холодная вод
    -- 3 горячая вода
    EXECUTE 'insert into ' || apref || '_data.prm_17 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)
      select nzp_counter as nzp, 1426 as nzp_prm, ''1900-01-01'' as dat_s, ''3000-01-01'' as dat_po, (case when nzp_cnt = 2 then ''ХВ'' when nzp_cnt = 3 then ''ГВ'' else ''ЭлЭн'' end) || num::text as val_prm, 
        1 as is_actual, 1 as nzp_user, now() as dat_when
      from  tmp_cnts';   

    RAISE NOTICE 'Выполнено. Банк данных: (%)', apref;
  end loop;

  drop table if exists tmp_cnts;
  drop table if exists tmp_num; 

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

select public.create_counter_pseudonym('fbill');