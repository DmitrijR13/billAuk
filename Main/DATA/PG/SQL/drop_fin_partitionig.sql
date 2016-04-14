drop function if exists drop_partitioning();
create function drop_partitioning() returns text as
$BODY$
DECLARE 
  monthCnt integer;
  dayCnt   integer;
  cnt      integer;
  
  dateFrom date;
  dateTo   date;
BEGIN
  monthCnt := 1;

  WHILE monthCnt < 13 LOOP
    dateFrom := (2015::text || '-' || lpad(monthCnt::text, 2, '0') || '-01')::DATE;
    dateTo := (dateFrom + interval '1 month')::DATE;
    cnt := dateTo - dateFrom;
    
    dayCnt := 1;
    while dayCnt <= cnt LOOP
      EXECUTE 'drop table if exists nftul_fin_15.fn_pa_dom_' || lpad(monthCnt::text, 2, '0') || '_' || lpad(dayCnt::text, 2, '0');
      EXECUTE 'drop table if exists nftul_fin_15.fn_distrib_dom_' || lpad(monthCnt::text, 2, '0') || '_' || lpad(dayCnt::text, 2, '0');
      
      dayCnt := dayCnt + 1;
    END LOOP;

    EXECUTE 'drop table if exists nftul_fin_15.fn_naud_dom_' || lpad(monthCnt::text, 2, '0');
    EXECUTE 'drop table if exists nftul_fin_15.fn_perc_dom_' || lpad(monthCnt::text, 2, '0');
    
    monthCnt := monthCnt + 1;  
  END LOOP;

  return 'OK';  
END;
$BODY$
LANGUAGE plpgsql;


select drop_partitioning();