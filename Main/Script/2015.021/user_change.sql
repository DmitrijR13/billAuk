set search_path to 'public';

delete from public.users where nzp_user = 0;
delete from public.users where nzp_user = -1000;
delete from public.users where nzp_user = -88888888;

insert into public.users (nzp_user, login, pwd, uname, is_blocked) values (0, 'program_user_0', '917B43A78E6F58F955DD23A8BB0691E1', 'Пользователь с кодом 0', 1);
insert into public.users (nzp_user, login, pwd, uname, is_blocked) values (-1000, 'program_user_1000', '917B43A78E6F58F955DD23A8BB0691E1', 'Программный пользователь', 1);
insert into public.users (nzp_user, login, pwd, uname, is_blocked) values (-88888888, 'program_user_88888888', '917B43A78E6F58F955DD23A8BB0691E1', 'Автоматическая рассрочка', 1);

--*****************************************************************************************************
drop function if exists public.column_exists (text, text, text);
create function public.column_exists(schm text, tbl text, clmn text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  select 1 into cnt from information_schema.columns where table_schema = schm and table_name = tbl and column_name = clmn limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

-- вспомогательная функция, которая получает по имени колонки название схемы и таблицы с этой колонкой  
---**********************************************************************************************************************************
drop function if exists public.fill_table_user(text);
create function public.fill_table_user(clmn text) returns text as
$BODY$

BEGIN
  RAISE NOTICE 'Колонка, начало: (%)', clmn;    

  insert into public.table_user (table_schema, table_name, column_name)
  select table_schema, table_name, column_name
  from information_schema.columns 
  where column_name = clmn and
  (
    strpos(table_schema, '_charge_') > 0
    OR
    strpos(table_schema, '_kernel') > 0
    OR
    strpos(table_schema, '_data') > 0 
    OR
    strpos(table_schema, '_fin_') > 0
    OR
    strpos(table_schema, '_supg') > 0
    OR
    strpos(table_schema, '_debt') > 0
    OR
    strpos(table_schema, '_upload') > 0   
  );

  RAISE NOTICE 'Колонка, стоп: (%)', clmn;    
  
  return 'Выполнено';
END;
$BODY$
  LANGUAGE plpgsql;

-- формирование таблиц с колонками, которые содержат коды пользователей
---**********************************************************************************************************************************
drop function if exists public.prepare_table_user();
create function public.prepare_table_user() returns text as
$BODY$
BEGIN
  drop table if exists public.table_user;

  create table public.table_user
  (
    table_id      serial,
    pref          text,
    table_schema  text,
    table_name    text,
    column_name   text,
    user_changed  integer default 0
  );

  -- заполнить таблицу table_user_migration
  PERFORM public.fill_table_user('ds_user'); 
  PERFORM public.fill_table_user('kp_nzp_user');
  PERFORM public.fill_table_user('nzp_user');
  PERFORM public.fill_table_user('user_d');
  PERFORM public.fill_table_user('user_downloaded');
  PERFORM public.fill_table_user('user_unloaded');

  PERFORM public.fill_table_user('change_by');  
  PERFORM public.fill_table_user('changed_by');
  PERFORM public.fill_table_user('created_by');
  PERFORM public.fill_table_user('sended_by');

  delete from public.table_user where table_schema like '%copy%';
  
  -- убрать таблицы для пользователей КП 2.0
  delete from public.table_user where table_name = 'users';
  delete from public.table_user where table_name = 's_psswd';
  delete from public.table_user where table_name = 'us_dmn';
  delete from public.table_user where table_name = 'us_pswd';
  delete from public.table_user where table_name = 'us_terms';

  -- убрать архивные таблицы
  delete from public.table_user where substr(table_name, 1, 1) = '_';

  delete from public.table_user where table_name like '%_gilper'; -- +
  delete from public.table_user where table_name like '%_nedop' and table_name <> 'jrn_upg_nedop'; -- +
  delete from public.table_user where table_name like '%_sobstw'; -- +
  delete from public.table_user where table_name like '%_rust';   -- +
  delete from public.table_user where table_name = 'prm_2_arch';  -- +

  delete from public.table_user where table_name = 'counters_2';  -- +
  delete from public.table_user where table_name like '%_back_%'; -- +
  delete from public.table_user where table_name like '%_before_del'; -- +
  delete from public.table_user where table_name = 'counters_arh';    -- +
  delete from public.table_user where table_name = 'counters_copy';   -- +
  delete from public.table_user where table_name = 'counters_spis_arch'; -- +
  delete from public.table_user where table_name = 'pack_ls_arh'; -- +
  delete from public.table_user where table_name = 'pack_ls_arh3'; -- +
  delete from public.table_user where table_name = 'perekidka_20141125'; -- +
  delete from public.table_user where table_name = 'prm_1_20141117';  -- +
  delete from public.table_user where table_name = 'prm_1_arh';   -- +
  delete from public.table_user where table_name = 'prm_1_clone'; -- +
  delete from public.table_user where table_name = 'prm_11_old';  -- +
  delete from public.table_user where table_name = 'tarif_copy_1014_10_21'; -- +
  delete from public.table_user where table_name = 'tvv_xx'; -- +
  delete from public.table_user where table_name = 'counters_spis_2'; -- +

  delete from public.table_user where table_name = 't123';  -- +
  delete from public.table_user where table_name = 't1234'; -- +

  delete from public.table_user where table_name = 'counters0202utro';
  delete from public.table_user where table_name = 'counters2901';
  delete from public.table_user where table_name = 'kart_20141210';
  delete from public.table_user where table_name = 'kart_20141210_v2';
  delete from public.table_user where table_name = 'kart_save';

  delete from public.table_user where table_name = 'pack_12012015';
  delete from public.table_user where table_name = 'pack_120122015';
  delete from public.table_user where table_name = 'pack_20150203';
  delete from public.table_user where table_name = 'pack_20150213';

  delete from public.table_user where table_name = 'pack_ls_12012015';
  delete from public.table_user where table_name = 'pack_ls_120122015';
  delete from public.table_user where table_name = 'pack_ls_17022015';
  delete from public.table_user where table_name = 'pack_ls_20150203';
  delete from public.table_user where table_name = 'pack_ls_20150213';

  delete from public.table_user where table_name = 'prm_2_20141218';
  delete from public.table_user where table_name = 'prm_3_copy_2015_02_14';

  -- таблицы, от которых наследуются другие таблицы
  delete from public.table_user where table_name = 'peni_provodki'; 
  delete from public.table_user where table_name = 'peni_calc'; 
  delete from public.table_user where table_name = 'peni_debt';
  delete from public.table_user where table_name = 'peni_provodki_arch';

  -- другой тип данных
  delete from public.table_user where table_name = 's_vill'; 
  delete from public.table_user where table_name = 'sr_stand_cost';

  -- определить префикс
  update public.table_user set pref = substr(table_schema, 1, strpos(table_schema, '_') - 1);

  create index ix_table_user_table_id on public.table_user(table_id);
  analyze public.table_user;

  return 'Выполнено';
END;
$BODY$
  LANGUAGE plpgsql;

-- получить коды пользователей
---**********************************************************************************************************************************
drop function if exists public.prepare_table_user_key(text);
create function public.prepare_table_user_key(main_pref text) returns text as
$BODY$
DECLARE
  atable_id integer;
  atable_schema text;
  atable_name   text;
  acolumn_name  text;
  cur_cnt       integer;
  cnt_tot       integer;
  apref         text;
BEGIN
  drop table if exists public.table_user_key;
  create table public.table_user_key
  (
    key_id        serial,
    table_id      integer,
    pref          text,
    table_schema  text,
    table_name    text,
    column_name   text,
    old_nzp_user  bigint,
    new_nzp_user  bigint,
    is_add        smallint default 0
  );

  cur_cnt := 0;
  select count(*) into cnt_tot from table_user;
   
  for atable_id, atable_schema, atable_name, acolumn_name, apref in
    select table_id, table_schema, table_name, column_name, pref from table_user
  loop
    RAISE NOTICE 'Определение кодов пользователей. Таблица: (%)', atable_schema || '.' || atable_name;        
    
    execute 'insert into table_user_key (old_nzp_user, table_id, table_schema, table_name, column_name, pref) 
      select ' || acolumn_name || ',' || atable_id || ',' || quote_literal(atable_schema) || ',' || quote_literal(atable_name) || ',' || quote_literal(acolumn_name) || ',' || quote_literal(apref) ||
      ' from ' || atable_schema || '.' || atable_name || 
      ' group by 1';

    cur_cnt := cur_cnt + 1;
    RAISE NOTICE 'Определение кодов пользователей. Выполнено: (%)', cur_cnt::text || '/' || cnt_tot::text;        
  end loop;
 
  create index ix_table_user_key_table_id on public.table_user_key(table_id);
  create index ix_table_user_key_old_nzp_user on public.table_user_key(old_nzp_user);  
  analyze public.table_user_key;

  -- СУПГ
  EXECUTE 'update public.table_user_key a set new_nzp_user = (select max(c.nzp_user) from ' || main_pref || '_supg.users b, public.users c where a.old_nzp_user = b.nzp_user and b.web_user = c.nzp_user)
    where table_schema like ''%supg%''';

  -- пробежаться по банкам данных
  for apref in
    select pref from public.table_user_key group by 1
  loop
    RAISE NOTICE 'Определение кодов пользователей, банк данных: (%)', apref;

    execute 'update public.table_user_key a set new_nzp_user = (select max(c.nzp_user) from ' || apref || '_data.users b, public.users c where a.old_nzp_user = b.nzp_user and b.web_user = c.nzp_user)
      where table_schema like ''%' || apref || '%'' and new_nzp_user is null ';       
  end loop;

  create index ix_table_user_key_new_nzp_user on public.table_user_key(new_nzp_user);  
  analyze public.table_user_key;

  delete from public.table_user_key where old_nzp_user is null;
  
  update public.table_user_key  set new_nzp_user = -88888888  where new_nzp_user is null and old_nzp_user = 88888888;
  update public.table_user_key  set new_nzp_user = 1          where new_nzp_user is null and old_nzp_user >= 0;
  update public.table_user_key  set new_nzp_user = -1000      where new_nzp_user is null and old_nzp_user < 0;

  create index ix_table_user_key_1 on public.table_user_key(old_nzp_user, table_id);
  analyze public.table_user_key;

  return 'Выполнено';  
END;
$BODY$
  LANGUAGE plpgsql;
 
-- cоздать индексы
--*****************************************************************************************************************************************************************************************************
drop function if exists public.user_migrate_create_index();
create function public.user_migrate_create_index() returns text as
$BODY$
DECLARE 
  atable_id integer;
  atable_schema text;
  atable_name   text;
  acolumn_name  text;

  cur_cnt       integer;
  cnt_tot       integer;
  user_cnt      integer;
BEGIN
  cur_cnt := 0;
  select count(*) into cnt_tot from public.table_user a where exists (select 1 from public.table_user_key b where a.table_id = b.table_id);
   
  for atable_id, atable_schema, atable_name, acolumn_name in
    select table_id, table_schema, table_name, column_name 
    from table_user a 
    where exists (select 1 from public.table_user_key b where a.table_id = b.table_id)
  loop
    RAISE NOTICE 'Создание индексов. Таблица: (%)', atable_schema || '.' || atable_name;        

     execute ' create index ix_123456789_' || atable_id || ' on ' || atable_schema || '.' || atable_name || ' (' || acolumn_name || ')';
     execute 'analyze ' || atable_schema || '.' || atable_name;

     cur_cnt := cur_cnt + 1;
     RAISE NOTICE 'Создание индексов. Выполнено: (%) ', cur_cnt::text || '/' || cnt_tot::text;      
  end loop;
  
  return 'Выполнено';
END;
$BODY$
  LANGUAGE plpgsql;

-- удалить ограничение по пользователям
--*****************************************************************************************************************************************************************************************************
drop function if exists public.drop_user_foreign_key(text);
create function public.drop_user_foreign_key(pref text) returns text as
$BODY$
DECLARE 
  aconstraint_name text;
BEGIN
  select kcu.constraint_name into aconstraint_name
  from information_schema.table_constraints tc, information_schema.key_column_usage kcu 
  where tc.constraint_type = 'PRIMARY KEY'
    and tc.constraint_name = kcu.constraint_name
    and kcu.table_schema = lower(trim(pref) || '_data')
    and tc.table_schema  = kcu.table_schema
    and kcu.table_name = lower('users')
    and tc.table_name  = kcu.table_name
    and kcu.column_name = lower('nzp_user');

  aconstraint_name := coalesce(aconstraint_name, '');

  if  aconstraint_name <> '' then  
    EXECUTE 'alter table ' || trim(pref) || '_data.users drop constraint if exists ' || aconstraint_name || ' cascade';
  end if;
  return 'Выполнено';
END;
$BODY$
  LANGUAGE plpgsql;

-- сменить пользователей
--*****************************************************************************************************************************************************************************************************
drop function if exists public.user_migrate();
create function public.user_migrate() returns text as
$BODY$
DECLARE 
  atable_id integer;
  atable_schema text;
  atable_name   text;
  acolumn_name  text;

  cur_cnt       integer;
  cnt_tot       integer;
  user_cnt      integer;
BEGIN
  cur_cnt := 0;
  select count(*) into cnt_tot from public.table_user a where exists (select 1 from public.table_user_key b where a.table_id = b.table_id);
   
  for atable_id, atable_schema, atable_name, acolumn_name in
    select table_id, table_schema, table_name, column_name 
    from table_user a 
    where exists (select 1 from public.table_user_key b where a.table_id = b.table_id)
  loop
    RAISE NOTICE 'Смена пользователей. Таблица: (%)', atable_schema || '.' || atable_name;        

    execute 'update ' || atable_schema || '.' || atable_name || ' a 
      set ' || acolumn_name || ' = (
        select b.new_nzp_user from public.table_user_key b 
        where b.table_id = ' || atable_id || ' 
          and b.old_nzp_user = a.' || acolumn_name || ')  
          where exists (select 1 from public.table_user_key b 
            where b.table_id = ' || atable_id || ' 
              and b.old_nzp_user = a.' || acolumn_name || ' 
              and b.old_nzp_user <> b.new_nzp_user limit 1)';
              
    -- установить признак, что в таблице пользователь изменен
    update public.table_user set user_changed = 1 where table_id = atable_id;

    cur_cnt := cur_cnt + 1;
    RAISE NOTICE 'Смена пользователей. Выполнено: (%) ', cur_cnt::text || '/' || cnt_tot::text;      
  end loop;
  
  return 'Выполнено';
END;
$BODY$
  LANGUAGE plpgsql;

-- перегрузка пользователей
--*****************************************************************************************************************************************************************************************************
drop function if exists public.reload_user();
create function public.reload_user() returns text as
$BODY$
DECLARE 
  sql text;
  atable_schema text;
  atable_name   text;
  acolumn_name  text;
BEGIN
  for atable_schema in 
    select schema_name from information_schema.schemata where schema_name like '%_data'
  loop 
    sql := 'ALTER TABLE ' || atable_schema || '.users ALTER nzp_user DROP DEFAULT;';
    EXECUTE sql;

    sql := 'TRUNCATE ' || atable_schema || '.users';
    EXECUTE sql; 

    sql := 'insert into ' || atable_schema || '.users (nzp_user, name, comment, web_user) 
     select nzp_user, login, uname, nzp_user from public.users';
    EXECUTE sql; 
      
  end loop;
  return 'OK';
END;
$BODY$
  LANGUAGE plpgsql; 

-- создание внешних ключей
---**********************************************************************************************************************************
drop function if exists public.create_foreign_key(text);
create function public.create_foreign_key(central_pref text) returns text as
$BODY$
DECLARE
  atable_id integer;
  atable_schema text;
  atable_name   text;
  acolumn_name  text;
  cur_cnt       integer;
  cnt_tot       integer;
  schm          text;
BEGIN
  schm := central_pref || '_data';

  -- первичный ключ в таблице users
  select 1 into cnt_tot
  from information_schema.table_constraints tc, information_schema.key_column_usage kcu 
  where tc.constraint_name = kcu.constraint_name
    and tc.constraint_type = 'PRIMARY KEY'
    and kcu.table_schema = lower(trim(schm)) 
    and tc.table_schema  = lower(trim(schm)) 
    and kcu.table_name = 'users' 
    and tc.table_name  = 'users' 
    and kcu.column_name = 'nzp_user' limit 1;
	
  cnt_tot := coalesce(cnt_tot, 0);

  if cnt_tot <= 0 then
     EXECUTE 'alter table ' || schm || '.users ADD CONSTRAINT users_pkey PRIMARY KEY (nzp_user)';
  end if; 

  cur_cnt := 0;
  select count(*) into cnt_tot from table_user;
   
  for atable_id, atable_schema, atable_name, acolumn_name in
    select table_id, table_schema, table_name, column_name from table_user
  loop
    RAISE NOTICE 'Создание внешних ключей. Таблица: (%)', atable_schema || '.' || atable_name;        
    
    execute 'alter table ' || atable_schema || '.' || atable_name || ' 
      add CONSTRAINT FK_' || atable_name || '_' || acolumn_name || ' 
      FOREIGN KEY (' || acolumn_name || ') REFERENCES ' || central_pref || '_data.users (nzp_user);';
   
    cur_cnt := cur_cnt + 1;
    RAISE NOTICE 'Создание внешних ключей. Выполнено: (%)', cur_cnt::text || '/' || cnt_tot::text;        
  end loop;
 
  return 'Выполнено';  
END;
$BODY$
  LANGUAGE plpgsql;

-- удалить индексы
--*****************************************************************************************************************************************************************************************************
drop function if exists public.user_migrate_drop_index();
create function public.user_migrate_drop_index() returns text as
$BODY$
DECLARE 
  atable_id integer;
  atable_schema text;
  atable_name   text;
  acolumn_name  text;

  cur_cnt       integer;
  cnt_tot       integer;
  user_cnt      integer;
BEGIN
  cur_cnt := 0;
  select count(*) into cnt_tot from public.table_user a where exists (select 1 from public.table_user_key b where a.table_id = b.table_id);
   
  for atable_id, atable_schema, atable_name, acolumn_name in
    select table_id, table_schema, table_name, column_name 
    from table_user a 
    where exists (select 1 from public.table_user_key b where a.table_id = b.table_id)
  loop
    RAISE NOTICE 'Удаление индекса. Таблица: (%)', atable_schema || '.' || atable_name;        

    execute 'set search_path to ''' || atable_schema || '''';
    execute ' drop index if exists ix_123456789_' || atable_id;
    execute 'analyze ' || atable_schema || '.' || atable_name;
	
    cur_cnt := cur_cnt + 1;
    RAISE NOTICE 'Удаление индексов. Выполнено: (%) ', cur_cnt::text || '/' || cnt_tot::text;  
  end loop;

  execute 'set search_path to ''public''';
  
  return 'Выполнено';
END;
$BODY$
  LANGUAGE plpgsql;

-- главная процедура
--**********************************************************************************************************************
drop function if exists public.main_user_change(text);
create function public.main_user_change(pref text) returns text as 
$BODY$
BEGIN
  perform public.prepare_table_user();
  perform public.prepare_table_user_key(pref);
  perform public.drop_user_foreign_key(pref);
  perform public.user_migrate_create_index();
  perform public.user_migrate();
  perform public.reload_user();
  perform public.create_foreign_key(pref);
  perform public.user_migrate_drop_index();

  if not public.column_exists('public', 's_setups', 'user_migrated') then
    EXECUTE 'alter table public.s_setups add column user_migrated smallint default 1';
  end if;
  
  return 'Выполнено';
END;
$BODY$
  LANGUAGE plpgsql;

--!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
-- ЗАПУСК: раскомментировать нужную строчку
--select public.main_user_change('fbill');
--select public.main_user_change('nftul');