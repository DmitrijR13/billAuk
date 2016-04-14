--*****************************************************************************************************
drop function if exists public.table_exists (text, text);
create function public.table_exists(schm text, tbl text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  select 1 into cnt from information_schema.tables where table_schema = schm and table_name = tbl limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

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

drop function if exists public.create_column (text, text, text, text);
create function public.create_column(schm text, tbl text, clmn text, clmn_type text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  if not public.column_exists(schm, tbl, clmn) then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD ' || clmn || ' ' || clmn_type;
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.constraint_exists (text, text, text);
create function public.constraint_exists(schm text, tbl text, cnstr text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  SELECT 1 into cnt FROM information_schema.table_constraints WHERE table_schema = schm and table_name = tbl and upper(constraint_name) = upper(cnstr) limit 1; 
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_primary_key(text, text, text, text);
drop function if exists public.create_primary_key(text, text, text);
create function public.create_primary_key(schm text, tbl text, clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  select 1 into cnt
  from information_schema.table_constraints t_c, information_schema.key_column_usage kcu 
  where t_c.constraint_type = 'PRIMARY KEY'
    and t_c.constraint_name = kcu.constraint_name
    and kcu.table_schema = lower(trim(schm)) 
    and t_c.table_schema = kcu.table_schema 
    and kcu.table_name = lower(trim(tbl)) 
    and t_c.table_name = kcu.table_name 
	and kcu.column_name = lower(trim(clmn)) limit 1;

	cnt := coalesce(cnt, 0);
  
  if cnt <= 0 then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD CONSTRAINT ' || trim(tbl) || '_pkey PRIMARY KEY (' || clmn|| ')';
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_foreign_key(text, text, text, text, text, text, text);
drop function if exists public.create_foreign_key(text, text, text, text, text, text);
create function public.create_foreign_key(schm text, tbl text, clmn text, ref_schema text, ref_table text, ref_clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
  cnstr text;
BEGIN
  cnstr := 'FK_' || tbl || '_' || clmn;
  if not public.constraint_exists(schm, tbl, cnstr) then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD CONSTRAINT ' || cnstr || ' FOREIGN KEY (' || clmn|| ') REFERENCES ' || ref_schema || '.' || ref_table || ' (' || ref_clmn || ')';
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.can_create_fk(text, text, text, text, text, text);
create function public.can_create_fk(schm text, tbl text, clmn text, ref_schema text, ref_table text, ref_clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  if not public.column_exists(schm, tbl, clmn) then
    return 'Выполнено';
  end if;
  
  EXECUTE 'select 1 from ' || schm || '.' || tbl || 
    ' where ' || clmn || ' not in (select ' || ref_clmn || ' from ' || ref_schema || '.' || ref_table || ' where ' || ref_clmn || ' is not null) 
      and ' || clmn || ' is not null ' into cnt; 
  cnt := coalesce(cnt, 0);
  
  if (cnt <> 0) then
    insert into public.agent_contract_error (err) values 
    ('В колонке ' || clmn || ' таблицы ' || schm || '.' || tbl || ' есть значения, которых нет в колонке ' || ref_clmn || ' таблицы ' || ref_schema || '.' || ref_table);
    return 'Выполнено';
  else
    return 'Выполнено';  
  end if;
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.index_exists (text, text, text);
create function public.index_exists(schm text, tbl text, indx text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  SELECT 1 into cnt FROM pg_class c 
   JOIN pg_index i ON i.indexrelid = c.oid
   JOIN pg_class c2 ON i.indrelid = c2.oid
   LEFT JOIN pg_user u ON u.usesysid = c.relowner
   LEFT JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE c.relkind = 'i' 
   AND n.nspname = schm AND c2.relname = tbl AND upper(c.relname) = upper(indx) limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_index (text, text, text, text);
create function public.create_index(schm text, tbl text, indx text, cols text) 
  RETURNS text AS
$BODY$
BEGIN
  if not public.index_exists(schm, tbl, indx) then
    EXECUTE 'CREATE INDEX ' || indx || ' ON ' || schm || '.' || tbl || '('|| cols ||')';
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_unique_index (text, text, text, text);
create function public.create_unique_index(schm text, tbl text, indx text, cols text) 
  RETURNS text AS
$BODY$
BEGIN
  if not public.index_exists(schm, tbl, indx) then
    EXECUTE 'CREATE UNIQUE INDEX ' || indx || ' ON ' || schm || '.' || tbl || '('|| cols ||')';
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;


--*****************************************************************************************************
drop function if exists public.ac_check_fk(text);
create function public.ac_check_fk(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  drop table if exists public.agent_contract_error;
  create table public.agent_contract_error (err text);

  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  perform public.can_create_fk(kernel, 's_payer', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(kernel, 'payer_types', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(kernel, 'payer_types', 'nzp_payer_type', kernel, 's_payer_types', 'nzp_payer_type');
  perform public.can_create_fk(kernel, 'payer_types', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_scope', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'nzp_fb', sdata, 'fn_bank', 'nzp_fb');
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'nzp_fd', sdata, 'fn_dogovor', 'nzp_fd');
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_payer_bank', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_user', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_user', sdata, 'users', 'nzp_user');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_dom', sdata, 'dom', 'nzp_dom');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_raj', sdata, 's_rajon', 'nzp_raj');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_town', sdata, 's_town', 'nzp_town');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_ul', sdata, 's_ulica', 'nzp_ul');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_wp', kernel, 's_point', 'nzp_wp');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'changed_by', sdata, 'users', 'nzp_user');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', sdata, 'fn_dogovor_bank_lnk', 'id');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_podr', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_supp', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'changed_by', sdata, 'users', 'nzp_user');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.make_archive(text);
create function public.make_archive(pref text) returns text as 
$BODY$
BEGIN
  if not public.table_exists(pref || '_data', 'fn_bank_old') then
    EXECUTE 'create table ' || pref || '_data.fn_bank_old (LIKE ' || pref || '_data.fn_bank)';
    EXECUTE 'insert into ' || pref || '_data.fn_bank_old select * from ' || pref || '_data.fn_bank';
  end if;

  if not public.table_exists(pref || '_data', 'fn_dogovor_old') then
    EXECUTE 'create table ' || pref || '_data.fn_dogovor_old (LIKE ' || pref || '_data.fn_dogovor)';
    EXECUTE 'insert into ' || pref || '_data.fn_dogovor_old select * from ' || pref || '_data.fn_dogovor';
  end if;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_tables(text);
create function public.ac_create_tables(pref text) returns text as
$BODY$
DECLARE
  sdata text;
  kernel text;
  apref text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  -- fn_scope
  if not public.table_exists (sdata, 'fn_scope') then
    EXECUTE 'set search_path to ''' || sdata || '''';
	EXECUTE 'CREATE SEQUENCE ' || sdata || '.fn_scope_nzp_scope_seq INCREMENT 1 START 1;';
    EXECUTE 'CREATE TABLE fn_scope (nzp_scope integer DEFAULT nextval((''' || sdata || '.fn_scope_nzp_scope_seq''::text)::regclass) NOT NULL) WITH (OIDS=FALSE)'; 
  end if;
  
  -- fn_dogovor_bank_lnk
  if not public.table_exists(sdata, 'fn_dogovor_bank_lnk') then
    EXECUTE 'set search_path to ''' || sdata || '''';
    EXECUTE 'CREATE SEQUENCE ' || sdata || '.fn_dogovor_bank_lnk_id_seq INCREMENT 1 START 1';
    EXECUTE 'CREATE TABLE fn_dogovor_bank_lnk ( 
	  id integer DEFAULT nextval((''' || sdata || '.fn_dogovor_bank_lnk_id_seq''::text)::regclass) NOT NULL,
	  nzp_fd integer NOT NULL,
	  nzp_fb integer NOT NULL,
	  changed_by integer NOT NULL,
	  changed_on timestamp DEFAULT now() NOT NULL) WITH (OIDS=FALSE)';
  end if;
  
  -- fn_scope_adres  
  if not public.table_exists(sdata, 'fn_scope_adres') then
    CREATE SEQUENCE fn_scope_adres_nzp_scope_adres_seq INCREMENT 1 START 1;
    CREATE TABLE fn_scope_adres ( 
	  nzp_scope_adres integer DEFAULT nextval(('fn_scope_adres_nzp_scope_adres_seq'::text)::regclass) NOT NULL,
	  nzp_scope integer NOT NULL,
	  nzp_wp integer NOT NULL,
	  nzp_town integer,
	  nzp_raj integer,
	  nzp_ul integer,
	  nzp_dom integer) WITH (OIDS=FALSE);
  end if;
  
  PERFORM public.create_column(sdata, 'fn_scope', 'changed_by', 'integer');
  PERFORM public.create_column(sdata, 'fn_scope', 'changed_on', 'timestamp DEFAULT now() NOT NULL');
  
  PERFORM public.create_column(sdata, 'fn_bank', 'note', 'varchar(1000)');
  
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_payer_agent', 'integer');
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_payer_princip', 'integer');
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_scope', 'integer');
  
  PERFORM public.create_column(sdata, 'fn_scope_adres', 'changed_by', 'integer');
  PERFORM public.create_column(sdata, 'fn_scope_adres', 'changed_on', 'timestamp DEFAULT now() NOT NULL');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'nzp_payer_podr', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'dpd', 'smallint DEFAULT 0'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'nzp_scope', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'changed_on', 'timestamp DEFAULT now() NOT NULL');

    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN adres_supp DROP NOT NULL';
    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN phone_supp DROP NOT NULL';
    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN geton_plat DROP NOT NULL';
  end loop;
  
  -- drop not null
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN num_count DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN bank_name DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN kcount DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN bik DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN npunkt DROP NOT NULL';
 
  -- s_payer
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN nzp_supp DROP NOT NULL';
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN nzp_type DROP NOT NULL';
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN changed_on SET DEFAULT now()';
 
  -- payer_types
  EXECUTE 'alter table ' || kernel || '.payer_types SET WITHOUT OIDS';
  EXECUTE 'alter table ' || kernel || '.payer_types ALTER COLUMN changed_on SET DEFAULT now()';
  
  -- fn_dogovor
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_area DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_payer_ar DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_fb DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_osnov DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN kpp DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN max_sum DROP NOT NULL ';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN priznak_perechisl DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN min_sum DROP NOT NULL ';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN naznplat DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_supp DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_payer DROP NOT NULL';
  
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_indexes(text);
create function public.ac_create_indexes(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_index(kernel, 's_payer', 'ix_s_payer_changed_by', 'changed_by');

  PERFORM public.create_index(kernel, 'payer_types', 'ix_payer_types_changed_by', 'changed_by');

  PERFORM public.create_unique_index(sdata, 'fn_scope', 'IX_fn_scope_nzp_scope', 'nzp_scope');
  PERFORM public.create_index(sdata, 'fn_scope', 'ix_fn_scope_changed_by', 'changed_by');

  PERFORM public.create_unique_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_id', 'id');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_nzp_fd', 'nzp_fd');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_nzp_fb', 'nzp_fb');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_changed_by', 'changed_by');

  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_payer', 'nzp_payer');
  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_payer_bank', 'nzp_payer_bank');
  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_user', 'nzp_user');

  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_payer_agent', 'nzp_payer_agent');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_payer_princip', 'nzp_payer_princip');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_user', 'nzp_user');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_scope', 'nzp_scope');

  PERFORM public.create_unique_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_scope_adres', 'nzp_scope_adres');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_scope', 'nzp_scope');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_wp', 'nzp_wp');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_town', 'nzp_town');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_raj', 'nzp_raj');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_ul', 'nzp_ul');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_dom', 'nzp_dom');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'ix_fn_scope_adres_changed_by', 'changed_by');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_agent', 'nzp_payer_agent');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_princip', 'nzp_payer_princip');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_podr', 'nzp_payer_podr');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_supp', 'nzp_payer_supp');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_fn_dogovor_bank_lnk_id', 'fn_dogovor_bank_lnk_id');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_scope', 'nzp_scope');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_changed_by', 'changed_by');
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_primary_keys(text);
create function public.ac_create_primary_keys(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_primary_key(sdata, 'users', 'nzp_user'); 
  PERFORM public.create_primary_key(kernel, 's_point', 'nzp_wp');  
  PERFORM public.create_primary_key(sdata, 's_town', 'nzp_town'); 
  PERFORM public.create_primary_key(sdata, 's_rajon', 'nzp_raj'); 
  PERFORM public.create_primary_key(sdata, 's_ulica', 'nzp_ul'); 
  PERFORM public.create_primary_key(sdata, 'dom', 'nzp_dom'); 
  
  PERFORM public.create_primary_key(kernel, 's_payer_types', 'nzp_payer_type'); 
  PERFORM public.create_primary_key(kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_primary_key(kernel, 'payer_types', 'nzp_pt');
  PERFORM public.create_primary_key(sdata, 'fn_scope', 'nzp_scope');
  PERFORM public.create_primary_key(sdata, 'fn_dogovor_bank_lnk', 'id');
  PERFORM public.create_primary_key(sdata, 'fn_bank', 'nzp_fb');
  PERFORM public.create_primary_key(sdata, 'fn_dogovor', 'nzp_fd');  
  PERFORM public.create_primary_key(sdata, 'fn_scope_adres', 'nzp_scope_adres');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_primary_key(apref || '_kernel', 'supplier', 'nzp_supp');
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_foreign_keys(text);
create function public.ac_create_foreign_keys(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_foreign_key(kernel, 's_payer', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'nzp_payer_type', kernel, 's_payer_types', 'nzp_payer_type');
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_scope', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'nzp_fb', sdata, 'fn_bank', 'nzp_fb');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'nzp_fd', sdata, 'fn_dogovor', 'nzp_fd');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_payer_bank', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_user', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_user', sdata, 'users', 'nzp_user');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_dom', sdata, 'dom', 'nzp_dom');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_raj', sdata, 's_rajon', 'nzp_raj');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_town', sdata, 's_town', 'nzp_town');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_ul', sdata, 's_ulica', 'nzp_ul');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_wp', kernel, 's_point', 'nzp_wp');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'changed_by', sdata, 'users', 'nzp_user');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', sdata, 'fn_dogovor_bank_lnk', 'id');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_podr', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_supp', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'changed_by', sdata, 'users', 'nzp_user');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_prepare_data(text);
create function public.ac_prepare_data(pref text) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
  cnt integer;
  anzp_fd integer;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';
  
  -- тип подрядчик
  EXECUTE 'select count(*) from ' || kernel || '.s_payer_types where nzp_payer_type = 11' into cnt;
  if (cnt = 0) then
    EXECUTE 'insert into ' || kernel || '.s_payer_types (nzp_payer_type, type_name, is_system) values (11, ''Подрядчик'', 1)'; 
  end if;
  
  -- проставить БИК и расчетные счета
  EXECUTE 'update ' || kernel || '.s_payer s set 
      bik = (select max(trim(bik))   from ' || sdata || '.fn_bank b where s.nzp_payer = b.nzp_payer_bank), 
      ks = (select max(trim(kcount)) from ' || sdata || '.fn_bank b where s.nzp_payer = b.nzp_payer_bank)
    where s.nzp_payer in (select nzp_payer_bank from ' || sdata || '.fn_bank)';
  
  -- для банков довставить типы
  EXECUTE 'insert into ' || kernel || '.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on) 
   select distinct nzp_payer, 8, 1, now() from ' || sdata || '.fn_bank where nzp_payer not in (select nzp_payer from ' || kernel || '.payer_types where nzp_payer_type = 8)';
  
  -- fn_dogovor
  -- nzp_payer_agent
  EXECUTE 'update ' || sdata || '.fn_dogovor d set
    nzp_payer_agent = (select max(s.nzp_payer_agent) from ' || kernel || '.supplier s where s.nzp_supp = d.nzp_supp and s.nzp_payer_agent is not null) 
	where d.nzp_payer_agent is null';
  
  -- nzp_payer_princip
  EXECUTE 'update ' || sdata || '.fn_dogovor d set
    nzp_payer_princip = (select max(s.nzp_payer_princip) from ' || kernel || '.supplier s where s.nzp_supp = d.nzp_supp and s.nzp_payer_princip is not null) 
	where d.nzp_payer_princip is null';

  -- довставить агентов и принципалов	
  EXECUTE 'insert into ' || sdata || '.fn_dogovor (nzp_payer_agent, nzp_payer_princip, nzp_supp, nzp_user, dat_when)
    select nzp_payer_agent, nzp_payer_princip, max(s.nzp_supp), 1, now()
    from ' || kernel || '.supplier s
    where s.nzp_payer_princip is not null 
	  and not exists (select 1 from ' || sdata || '.fn_dogovor d
	    where d.nzp_payer_agent = s.nzp_payer_agent and d.nzp_payer_princip = s.nzp_payer_princip and s.nzp_payer_agent is not null and s.nzp_payer_princip is not null)
	group by 1,2';
  
  -- fn_scope
  EXECUTE 'select max(nzp_scope) from ' || sdata || '.fn_scope' into cnt ;
  cnt := coalesce(cnt, 1);
  
  EXECUTE 'set search_path to ' || sdata;  
  for anzp_fd in 
    select nzp_fd from fn_dogovor 
    where nzp_scope is null 
      and nzp_payer_agent is not null and nzp_payer_princip is not null
  loop
    cnt := cnt + 1;
    insert into fn_scope(nzp_scope, changed_by, changed_on) values (cnt, 1, now());
    EXECUTE 'insert into fn_scope_adres (nzp_scope, nzp_wp, changed_by, changed_on) 
    select ' || cnt || ', nzp_wp, 1, now() from ' || kernel || '.s_point';
    update fn_dogovor set nzp_scope = cnt where nzp_fd = anzp_fd;
  end loop;
  
  EXECUTE 'set search_path to ' || sdata; 
  EXECUTE 'ALTER SEQUENCE fn_scope_nzp_scope_seq RESTART with ' || cnt;
  
  -- fn_dogovor_bank_lnk
  EXECUTE 'set search_path to ' || sdata; 
  insert into fn_dogovor_bank_lnk (nzp_fd, nzp_fb, changed_by, changed_on)
    select a.nzp_fd, max(b.nzp_fb), 1, now() 
  from fn_dogovor a, fn_bank b 
    where b.nzp_payer = a.nzp_payer_princip and not exists (select 1 from fn_dogovor_bank_lnk l where l.nzp_fd = a.nzp_fd and l.nzp_fb = b.nzp_fb)
  group by 1;
  
  -- supplier
  EXECUTE 'set search_path to ' || kernel; 
  for pref in select trim(bd_kernel) from s_point order by 1 
  loop
    EXECUTE 'update ' || pref || '_kernel.supplier s set 
     fn_dogovor_bank_lnk_id = (select max(l.id)
     from ' || sdata || '.fn_dogovor d, ' || sdata || '.fn_dogovor_bank_lnk l
     where s.nzp_payer_agent = d.nzp_payer_agent
       and s.nzp_payer_princip = d.nzp_payer_princip
       and d.nzp_fd = l.nzp_fd)
	 where s.fn_dogovor_bank_lnk_id is null';
  end loop;

  return 'Выполнено'; 
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.insert_absent_payer(text, integer, text); 
create function public.insert_absent_payer(pref text, nzp_payer integer, payer text) returns text as
$BODY$
DECLARE
  cnt integer;  
BEGIN
 EXECUTE 'select count(*) from ' || pref ||'_kernel.s_payer where nzp_payer = ' || nzp_payer into cnt;

 if cnt <= 0 then
   EXECUTE 'insert into ' || pref ||'_kernel.s_payer (nzp_payer, payer, npayer, changed_by, changed_on) values (' || 
       nzp_payer || ',' || quote_literal(payer) || ',' || quote_literal(payer) || ', 1, now() )';
 end if;
 
 return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.prepare_tula_data(text); 
create function public.prepare_tula_data(pref text) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';

  PERFORM public.insert_absent_payer(pref, 500, 'ООО Наш дом I');
  PERFORM public.insert_absent_payer(pref, 502, 'ООО ЖЕУ');
  PERFORM public.insert_absent_payer(pref, 504, 'Миллениум - 1');
  PERFORM public.insert_absent_payer(pref, 508, 'ООО Управдом');
  PERFORM public.insert_absent_payer(pref, 510, 'УК Алексин');
  PERFORM public.insert_absent_payer(pref, 518, 'УК ПСЦ');

   /*select nzp_payer_bank from nftul_data.fn_bank where nzp_payer_bank > 0 group by 1
    having count(*) > 1 order by nzp_payer_bank*/
  
  EXECUTE 'set search_path to ' || sdata;	
  update fn_bank set bik = '047003608', kcount = '30101810300000000608' where nzp_payer_bank = 7001;    -- ОТДЕЛЕНИЕ N8604 СБЕРБАНКА РОССИИ
  update fn_bank set bik = '047003608', kcount = '30101810300000000608' where nzp_payer_bank = 100049;  -- ОТДЕЛЕНИЕ N8604 СБЕРБАНКА РОССИИ
  update fn_bank set bik = '042007738', kcount = '30101810100000000738' where nzp_payer_bank = 110001;  -- ФИЛИАЛ N 3652 ВТБ 24 (ПАО)
  update fn_bank set bik = '047003715', kcount = '30101810400000000715' where nzp_payer_bank = 110003;  -- ТУЛЬСКИЙ РФ ОАО "РОССЕЛЬХОЗБАНК"
  update fn_bank set bik = '047003001', kcount = '00000000000000000000' where nzp_payer_bank = 110004;  -- ОТДЕЛЕНИЕ ТУЛА  
  update fn_bank set bik = '042908701', kcount = '30101810600000000701' where nzp_payer_bank = 150065;  -- ОАО "ГАЗЭНЕРГОБАНК"
  update fn_bank set bik = '047003756', kcount = '30101810100000000756' where nzp_payer_bank = 150072;  -- ТУЛЬСКИЙ ФИЛИАЛ АПБ "СОЛИДАРНОСТЬ" 
  update fn_bank set bik = '047003764', kcount = '30101810600000000764' where nzp_payer_bank = 150073;  -- ТУЛЬСКИЙ ФИЛИАЛ АБ"РОССИЯ"
  update fn_bank set bik = '047003725', kcount = '30101810500000000725' where nzp_payer_bank = 150075;  -- ОАО "СПИРИТБАНК"
  update fn_bank set bik = '047003726', kcount = '30101810800000000726' where nzp_payer_bank = 154107;  -- ФИЛИАЛ ТРУ ОАО "МИНБ"
  update fn_bank set bik = '123456789', kcount = '12345678999123456789' where nzp_payer_bank = 154525;  -- ТТБ УК ЮР-лица
  update fn_bank set bik = '044585342', kcount = '30101810400000000342' where nzp_payer_bank = 154579;  -- ЗАО МКБ "МОСКОМПРИВАТБАНК"
  update fn_bank set bik = '000000000', kcount = '00000000000000000000' where nzp_payer_bank = 154617;  -- НМУП  Центр КРиС
  update fn_bank set bik = '044585297', kcount = '30101810500000000297' where nzp_payer_bank = 154630;  -- ОАО Банк  Открытие  г.Москва
  update fn_bank set bik = '047003726', kcount = '30101810800000000726' where nzp_payer_bank = 154636;  -- ФИЛИАЛ ТРУ ОАО "МИНБ"
  update fn_bank set bik = '042007738', kcount = '30101810100000000738' where nzp_payer_bank = 154640;  -- ФИЛИАЛ N 3652 ВТБ 24 (ПАО)
  update fn_bank set bik = '125485697', kcount = '55555555555555500000' where nzp_payer_bank = 156645;  -- kontr2843
  update fn_bank set bik = '125485697', kcount = '55555555555555500000' where nzp_payer_bank = 156665;  -- kntr2859
  update fn_bank set bik = '125485697', kcount = '55555555555555500000' where nzp_payer_bank = 156673;  -- 5622

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.agent_contract(text);
create function public.agent_contract(pref text) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
  aerr text;
  cnt integer;
  anzp_fd integer;
  localbank text;
  is_tula integer;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';


  PERFORM public.ac_check_fk(pref);
  
  select count(*) into cnt from public.agent_contract_error;
  
  if  cnt <> 0 then
    RAISE NOTICE 'Помойка в данных';

    for aerr in select err from public.agent_contract_error
    loop
      RAISE NOTICE '%', aerr;
    end loop;
    drop table if exists public.agent_contract_error;

    return 'Не выполнено. См. сообщения';	
  end if;

  -- определить, что это Тула 
  is_tula := 0;
  EXECUTE 'select count(*) from ' || kernel || '.s_point where flag = 1 and substring(cast(bank_number as varchar (250)), 1,2) = ''71''' into is_tula;

  -- Тула
  if (is_tula <> 0) then
    PERFORM public.prepare_tula_data(pref);  	
  end if;

  -- сделать архив для таблиц fn_bank и fn_dogovor
  RAISE NOTICE 'Создание архива таблиц fn_bank и fn_dogovor'; 
  perform public.make_archive(pref);
  RAISE NOTICE 'Архив таблиц fn_bank и fn_dogovor создан'; 
  
  RAISE NOTICE 'Создание таблиц'; 
  PERFORM public.ac_create_tables(pref);
  RAISE NOTICE 'Таблицы созданы'; 

  RAISE NOTICE 'Создание индексов'; 
  PERFORM public.ac_create_indexes(pref);
  RAISE NOTICE 'Индексы созданы'; 

  RAISE NOTICE 'Создание первичных ключей'; 
  PERFORM public.ac_create_primary_keys(pref);
  RAISE NOTICE 'Первичные ключи созданы'; 

  RAISE NOTICE 'Создание внешних ключей'; 
  PERFORM public.ac_create_foreign_keys(pref);
  RAISE NOTICE 'Внешние ключи созданы'; 

  EXECUTE 'drop function if exists ' || kernel || '.trigger_supplier() CASCADE;';

  RAISE NOTICE 'Приведение данных'; 
  PERFORM public.ac_prepare_data(pref);
  RAISE NOTICE 'Данные приведены'; 
  
  RAISE NOTICE 'Создание триггеров'; 
  
  EXECUTE 'CREATE function ' || kernel || '.trigger_supplier() RETURNS trigger AS 
$trigger_supplier$
DECLARE
  fn_dogovor_nzp_payer_agent integer;
  fn_dogovor_nzp_payer_princip integer;
BEGIN
  select nzp_payer_agent, nzp_payer_princip into fn_dogovor_nzp_payer_agent, fn_dogovor_nzp_payer_princip 
  from ' || sdata || '.fn_dogovor_bank_lnk l, ' || sdata || '.fn_dogovor d
  where l.nzp_fd = d.nzp_fd
    and l.id = NEW.fn_dogovor_bank_lnk_id;

  if NEW.nzp_payer_agent <> fn_dogovor_nzp_payer_agent 
    and NEW.nzp_payer_agent > 0         and fn_dogovor_nzp_payer_agent > 0
    and NEW.nzp_payer_agent is not null and fn_dogovor_nzp_payer_agent is not null
  then
    RAISE EXCEPTION ''Платежный агент договора ЖКУ не соответствует платежному агента договора ЕРЦ'';
  end if;

  if NEW.nzp_payer_princip <> fn_dogovor_nzp_payer_princip 
    and NEW.nzp_payer_princip > 0          and fn_dogovor_nzp_payer_princip > 0
    and NEW.nzp_payer_princip is not null  and fn_dogovor_nzp_payer_princip is not null
  then
    RAISE EXCEPTION ''Принципал договора ЖКУ не соответствует принципалу договора ЕРЦ'';
  end if;  

  return NEW;
END;
$trigger_supplier$
LANGUAGE  plpgsql;';
 
  EXECUTE 'set search_path to ' || kernel; 
  for localbank in select trim(bd_kernel) from s_point order by 1 
  loop
    EXECUTE 'CREATE TRIGGER ins_supplier BEFORE INSERT ON ' || localbank || '_kernel.supplier FOR EACH ROW EXECUTE PROCEDURE ' || kernel || '.trigger_supplier()';
    EXECUTE 'CREATE TRIGGER upd_supplier BEFORE UPDATE ON ' || localbank || '_kernel.supplier FOR EACH ROW EXECUTE PROCEDURE ' || kernel || '.trigger_supplier()';
  end loop;

  RAISE NOTICE 'Триггеры созданы';

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--select public.agent_contract('fbill');
--select public.agent_contract('nftul');