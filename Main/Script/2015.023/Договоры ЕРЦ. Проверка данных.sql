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
    RAISE NOTICE 'Ошибки в данных';

    for aerr in select err from public.agent_contract_error
    loop
      RAISE NOTICE '%', aerr;
    end loop;
    drop table if exists public.agent_contract_error;

    return 'Ошибки в данных. См. сообщения';	
  end if;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--select public.agent_contract('fbill');
--select public.agent_contract('nftul');