drop function if exists check_table_exists (text, text);
create function check_table_exists(schm text, tbl text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt text;
BEGIN
  select table_name into cnt from information_schema.tables where table_schema = schm and table_name = tbl limit 1;
  cnt := coalesce(cnt, '');
  return cnt <> '';
END;
$BODY$
LANGUAGE plpgsql;

------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
drop function if exists create_fn_pa_part(text, integer);
CREATE function create_fn_pa_part(pref text, year integer)
  RETURNS text AS
$BODY$
DECLARE 
  s_oper_date text;
  sql text;

  parent_fn_pa_dom_xx       text;
  parent_fn_pa_dom_xx_short text;
  
  fn_pa_dom_xx       text;
  fn_pa_dom_xx_short text;

  monthCnt integer;
  dayCnt   integer;
  cnt      integer;
  
  dateFrom date;
  dateTo   date;

  fin_yy   text;
BEGIN
  fin_yy := pref || '_fin_' || substring(lpad(year::text, 4, '0'), 3);

  monthCnt := 1;

  WHILE monthCnt < 13 LOOP
    parent_fn_pa_dom_xx := fin_yy || '.fn_pa_dom_' || lpad(monthCnt::text, 2, '0');
    parent_fn_pa_dom_xx_short := 'fn_pa_dom_' || lpad(monthCnt::text, 2, '0');

    dateFrom := (year::text || '-' || lpad(monthCnt::text, 2, '0') || '-01')::DATE;
    dateTo := (dateFrom + interval '1 month')::DATE;
    cnt := dateTo - dateFrom;
    
    dayCnt := 1;
    while dayCnt <= cnt LOOP
      s_oper_date := quote_literal(year || '-' || lpad(monthCnt::text, 2, '0') || '-' || lpad(dayCnt::text, 2, '0')) ||'::DATE';

      fn_pa_dom_xx := parent_fn_pa_dom_xx || '_' || lpad(dayCnt::text, 2, '0');
      fn_pa_dom_xx_short := parent_fn_pa_dom_xx_short || '_' || lpad(dayCnt::text, 2, '0');

      if not check_table_exists(fin_yy, fn_pa_dom_xx_short) then
        sql := ' CREATE TABLE ' || fn_pa_dom_xx || ' (CONSTRAINT CNSTR_' || fn_pa_dom_xx_short || '_dat_oper CHECK (dat_oper = ' || s_oper_date || ')) INHERITS (' || parent_fn_pa_dom_xx || ') WITHOUT OIDS';
        EXECUTE sql;

        sql := 'ALTER TABLE ' || fn_pa_dom_xx || ' ADD CONSTRAINT PK_' || fn_pa_dom_xx_short || ' PRIMARY KEY (nzp_pk)';
        EXECUTE sql;

        sql := 'create UNIQUE index IX_' || fn_pa_dom_xx_short || '_nzp_dis on ' || fn_pa_dom_xx || ' (nzp_pk)';
        EXECUTE sql;

        sql := 'create index IX_' || fn_pa_dom_xx_short || '_dat_oper on ' || fn_pa_dom_xx || ' (dat_oper)';
        EXECUTE sql;

        sql = 'create index IX_' || fn_pa_dom_xx_short || '_nzp_supp on ' || fn_pa_dom_xx || ' (nzp_supp)';
        EXECUTE sql;

        sql = 'create index IX_' || fn_pa_dom_xx_short || '_nzp_dom on ' || fn_pa_dom_xx || ' (nzp_dom)';
        EXECUTE sql;

        sql = 'INSERT INTO ' || fn_pa_dom_xx || ' select * from ' || parent_fn_pa_dom_xx || ' where dat_oper = ' || s_oper_date;
        EXECUTE sql;
      end if;       
      
      dayCnt := dayCnt + 1;
    END LOOP;

    sql := 'TRUNCATE ONLY ' || parent_fn_pa_dom_xx;

    monthCnt := monthCnt + 1;  
  END LOOP;

  return 'OK';  
END;
$BODY$
  LANGUAGE plpgsql;

------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
drop function if exists create_fn_distrib_part(text, integer);
CREATE function create_fn_distrib_part(pref text, year integer)
  RETURNS text AS
$BODY$
DECLARE 
  s_oper_date text;
  sql text;

  parent_fn_distrib_dom_xx       text;
  parent_fn_distrib_dom_xx_short text;
  
  fn_distrib_dom_xx       text;
  fn_distrib_dom_xx_short text;

  monthCnt integer;
  dayCnt   integer;
  cnt      integer;
  
  dateFrom date;
  dateTo   date;

  fin_yy   text;
BEGIN
  fin_yy := pref || '_fin_' || substring(lpad(year::text, 4, '0'), 3);

  monthCnt := 1;

  WHILE monthCnt < 13 LOOP
    parent_fn_distrib_dom_xx := fin_yy || '.fn_distrib_dom_' || lpad(monthCnt::text, 2, '0');
    parent_fn_distrib_dom_xx_short := 'fn_distrib_dom_' || lpad(monthCnt::text, 2, '0');

    dateFrom := (year::text || '-' || lpad(monthCnt::text, 2, '0') || '-01')::DATE;
    dateTo := (dateFrom + interval '1 month')::DATE;
    cnt := dateTo - dateFrom;
    
    dayCnt := 1;
    while dayCnt <= cnt LOOP
      s_oper_date := quote_literal(year || '-' || lpad(monthCnt::text, 2, '0') || '-' || lpad(dayCnt::text, 2, '0')) ||'::DATE';

      fn_distrib_dom_xx := parent_fn_distrib_dom_xx || '_' || lpad(dayCnt::text, 2, '0');
      fn_distrib_dom_xx_short := parent_fn_distrib_dom_xx_short || '_' || lpad(dayCnt::text, 2, '0');

      if not check_table_exists(fin_yy, fn_distrib_dom_xx_short) then
        sql := ' CREATE TABLE ' || fn_distrib_dom_xx || ' (CONSTRAINT CNSTR_' || fn_distrib_dom_xx_short || '_dat_oper CHECK (dat_oper = ' || s_oper_date || ')) INHERITS (' || parent_fn_distrib_dom_xx || ') WITHOUT OIDS';
        EXECUTE sql;

        sql := 'ALTER TABLE ' || fn_distrib_dom_xx || ' ADD CONSTRAINT PK_' || fn_distrib_dom_xx_short || ' PRIMARY KEY (nzp_dis)';
        EXECUTE sql;

        sql := 'create UNIQUE index IX_' || fn_distrib_dom_xx_short || '_nzp_dis on ' || fn_distrib_dom_xx || ' (nzp_dis)';
        EXECUTE sql;

        sql := 'create index IX_' || fn_distrib_dom_xx_short || '_dat_oper on ' || fn_distrib_dom_xx || ' (dat_oper)';
        EXECUTE sql;

        sql = 'create index IX_' || fn_distrib_dom_xx_short || '_nzp_supp on ' || fn_distrib_dom_xx || ' (nzp_payer)';
        EXECUTE sql;

        sql = 'create index IX_' || fn_distrib_dom_xx_short || '_nzp_dom on ' || fn_distrib_dom_xx || ' (nzp_dom)';
        EXECUTE sql;

        sql = 'INSERT INTO ' || fn_distrib_dom_xx || ' select * from ' || parent_fn_distrib_dom_xx || ' where dat_oper = ' || s_oper_date;
        EXECUTE sql;
      end if;       
      
      dayCnt := dayCnt + 1;
    END LOOP;

    sql := 'TRUNCATE ONLY ' || parent_fn_distrib_dom_xx;

    monthCnt := monthCnt + 1;  
  END LOOP;

  return 'OK';  
END;
$BODY$
  LANGUAGE plpgsql;

------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
drop function if exists create_fn_naud_part(text, integer);
CREATE function create_fn_naud_part(pref text, year integer)
  RETURNS text AS
$BODY$
DECLARE 
  s_date_from text; 
  s_date_to   text;
  
  sql text;

  parent_fn_naud       text;
  parent_fn_naud_short text;
  
  fn_naud       text;
  fn_naud_short text;

  monthCnt integer;
  
  dateFrom date;
  dateTo   date;

  fin_yy   text;
BEGIN
  fin_yy := pref || '_fin_' || substring(lpad(year::text, 4, '0'), 3);

  parent_fn_naud := fin_yy || '.fn_naud_dom';
  parent_fn_naud_short := 'fn_naud_dom';

  monthCnt := 1;

  WHILE monthCnt < 13 LOOP
    fn_naud := parent_fn_naud || '_' || lpad(monthCnt::text, 2, '0');
    fn_naud_short := parent_fn_naud_short || '_' || lpad(monthCnt::text, 2, '0');

    dateFrom := (year::text || '-' || lpad(monthCnt::text, 2, '0') || '-01')::DATE;
    dateTo := (dateFrom + interval '1 month')::DATE;

    s_date_from := quote_literal(year::text || '-' || lpad(monthCnt::text, 2, '0') || '-01') || '::DATE';
    s_date_to := quote_literal(year::text || '-' || lpad((extract(month from dateTo))::text, 2, '0') || '-01') || '::DATE';

    if not check_table_exists(fin_yy, fn_naud_short) then
      sql := ' CREATE TABLE ' || fn_naud || ' (CONSTRAINT CNSTR_' || fn_naud_short || '_dat_oper CHECK (dat_oper >= ' || s_date_from || ' and dat_oper < ' || s_date_to ||
         ')) INHERITS (' || parent_fn_naud || ') WITHOUT OIDS';
      EXECUTE sql;

      sql := 'ALTER TABLE ' || fn_naud || ' ADD CONSTRAINT PK_' || fn_naud_short || ' PRIMARY KEY (nzp_naud)';
      EXECUTE sql;

      sql := 'create UNIQUE index IX_' || fn_naud_short || '_nzp_naud on ' || fn_naud || ' (nzp_naud)';
      EXECUTE sql;

      sql := 'create index IX_' || fn_naud_short || '_dat_oper on ' || fn_naud || ' (dat_oper)';
      EXECUTE sql;

      sql = 'create index IX_' || fn_naud_short || '_nzp_dom on ' || fn_naud || ' (nzp_dom)';
      EXECUTE sql;

      sql = 'INSERT INTO ' || fn_naud || ' select * from ' || parent_fn_naud || ' where dat_oper >= ' || s_date_from || ' and dat_oper < ' || s_date_to;
      EXECUTE sql;
    end if;
    
    monthCnt := monthCnt + 1;  
  END LOOP;

  s_date_from := quote_literal(year::text || '-01-01') || '::DATE';
  sql := 'DELETE FROM ONLY ' || parent_fn_naud || ' where dat_oper >= ' || s_date_from;
  EXECUTE sql;
  
  return 'OK';  
END;
$BODY$
  LANGUAGE plpgsql;

------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
drop function if exists create_fn_perc_part(text, integer);
CREATE function create_fn_perc_part(pref text, year integer)
  RETURNS text AS
$BODY$
DECLARE 
  s_date_from text; 
  s_date_to   text;
  
  sql text;

  parent_fn_perc       text;
  parent_fn_perc_short text;
  
  fn_perc       text;
  fn_perc_short text;

  monthCnt integer;
  
  dateFrom date;
  dateTo   date;

  fin_yy   text;
BEGIN
  fin_yy := pref || '_fin_' || substring(lpad(year::text, 4, '0'), 3);

  parent_fn_perc := fin_yy || '.fn_perc_dom';
  parent_fn_perc_short := 'fn_perc_dom';

  monthCnt := 1;

  WHILE monthCnt < 13 LOOP
    fn_perc := parent_fn_perc || '_' || lpad(monthCnt::text, 2, '0');
    fn_perc_short := parent_fn_perc_short || '_' || lpad(monthCnt::text, 2, '0');

    dateFrom := (year::text || '-' || lpad(monthCnt::text, 2, '0') || '-01')::DATE;
    dateTo := (dateFrom + interval '1 month')::DATE;

    s_date_from := quote_literal(year::text || '-' || lpad(monthCnt::text, 2, '0') || '-01') || '::DATE';
    s_date_to := quote_literal(year::text || '-' || lpad((extract(month from dateTo))::text, 2, '0') || '-01') || '::DATE';

    if not check_table_exists(fin_yy, fn_perc_short) then
      sql := ' CREATE TABLE ' || fn_perc || ' (CONSTRAINT CNSTR_' || fn_perc_short || '_dat_oper CHECK (dat_oper >= ' || s_date_from || ' and dat_oper < ' || s_date_to ||
         ')) INHERITS (' || parent_fn_perc || ') WITHOUT OIDS';
      EXECUTE sql;

      sql := 'ALTER TABLE ' || fn_perc || ' ADD CONSTRAINT PK_' || fn_perc_short || ' PRIMARY KEY (nzp_pr)';
      EXECUTE sql;

      sql := 'create UNIQUE index IX_' || fn_perc_short || '_nzp_pr on ' || fn_perc || ' (nzp_pr)';
      EXECUTE sql;

      sql := 'create index IX_' || fn_perc_short || '_dat_oper on ' || fn_perc || ' (dat_oper)';
      EXECUTE sql;

      sql = 'create index IX_' || fn_perc_short || '_nzp_dom on ' || fn_perc || ' (nzp_dom)';
      EXECUTE sql;

      sql = 'INSERT INTO ' || fn_perc || ' select * from ' || parent_fn_perc || ' where dat_oper >= ' || s_date_from || ' and dat_oper < ' || s_date_to;
      EXECUTE sql;
    end if;

    monthCnt := monthCnt + 1;  
  END LOOP;

  s_date_from := quote_literal(year::text || '-01-01') || '::DATE';
  sql := 'DELETE FROM ONLY ' || parent_fn_perc || ' where dat_oper >= ' || s_date_from;
  EXECUTE sql;
   
  return 'OK';  
END;
$BODY$
  LANGUAGE plpgsql;

------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
drop function if exists make_fin_partitioning(text, integer);
create function make_fin_partitioning(pref text, year integer)
  RETURNS text AS
$BODY$
DECLARE
  fin_yy  text;
  cnt     integer;
BEGIN
  fin_yy := pref || '_fin_' || substring(lpad(year::text, 4, '0'), 3);

  select count(*) into cnt from information_schema.schemata where schema_name = fin_yy;
  
  if cnt <= 0 then
    return 'Схема ' || fin_yy || ' не существует';
  end if;
  
  PERFORM create_fn_pa_part(pref, year);
  PERFORM create_fn_distrib_part(pref, year);
  PERFORM create_fn_naud_part(pref, year);
  PERFORM create_fn_perc_part(pref, year);
  
  return 'OK';
END;
$BODY$
LANGUAGE plpgsql;