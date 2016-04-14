@echo off
setlocal ENABLEDELAYEDEXPANSION

chcp 1251>nul
             
SET schema=%~1
SET pg_dir=%~2
SET work_dir=%~3
SET host=%~4
SET port=%~5
SET db=%~6
SET PGPASSWORD=%~7
SET nzp=%~8
SET dat_calc=%~9

SET year=%dat_calc:~6,4%

for %%i in ("%work_dir%"\*.unl) do call :proc "%%i"

goto :EOF

:proc
  
  SET table_name=%~n1
  SET file_name=%~n1

  if "%table_name%" == "counters_ord" ( 
    SET par=nzp_ck
  )

  if "%table_name%" == "recalc_%year%_charge_01" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_02" ( 
    SET par=nzp_charge
  )
    
  if "%table_name%" == "recalc_%year%_charge_03" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_04" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_05" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_06" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_07" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_08" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_09" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_10" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_11" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "recalc_%year%_charge_12" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "counters_domspis" ( 
    SET par=nzp_counter
  )

  if "%table_name%" == "counters" ( 
    SET par=nzp_cr
  )

  if "%table_name%" == "counters_dom" ( 
    SET par=nzp_crd
  )

  if "%table_name%" == "counters_group" ( 
    SET par=nzp_cg
  )

  if "%table_name%" == "gil_periods" ( 
    SET par=nzp_glp
  )

  if "%table_name%" == "nedop_kvar" ( 
    SET par=nzp_nedop
  )

  if "%table_name%" == "prm_1" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_2" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_3" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_4" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_5" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_6" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_7" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_8" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_9" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_10" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_11" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_12" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_13" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_14" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_15" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_16" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prm_17" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "prmt_m" ( 
    SET par=nzp_key
  )

  if "%table_name%" == "tarif" ( 
    SET par=nzp_tarif
  )

  if "%table_name%" == "gil_sums" ( 
    SET par=nzp_sums
  )

  if "%table_name%" == "pack" ( 
    SET par=nzp_pack
  )

  if "%table_name%" == "pack_ls" ( 
    SET par=nzp_pack_ls
  )
                                  

  SET file_type="%work_dir%"\%file_name%.unl


  echo --------------------------------------------------------------------------------
  echo ¬˚·‡Ì‡ Ú‡·ÎËˆ‡: %table_name%
  echo ¬˚ÔÓÎÌÂÌ˚ ÒÎÂ‰Û˛˘ËÂ ÓÔÂ‡ˆËË:

	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists temp_%table_name%";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "create table temp_%table_name% as select * from %schema%.%table_name% limit 1";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "truncate temp_%table_name%";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% add column additionaly character(1)";

		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ¬Õ»Ã¿Õ»≈.œ–Œ»«ŒÿÀ¿ Œÿ»¡ ¿ œ–» ¬€œŒÀÕ≈Õ»» — –»œ“¿.
    			exit
    		)

  	type %file_type% | "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "copy t_%table_name%_%nzp% from STDIN  with delimiter as '|' null as '' encoding 'WIN-1251'";

  		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ¬Õ»Ã¿Õ»≈.œ–Œ»«ŒÿÀ¿ Œÿ»¡ ¿ œ–» ¬€œŒÀÕ≈Õ»» — –»œ“¿. 
    			exit
  		)

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% drop column additionaly";
	SET aa=recalc_%year%_
	SET bb=""
	call SET result_string=%%table_name:!aa!=%%
	echo %result_string%
	
  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "delete from %schema%.!result_string! d where exists (select 1 from t_%table_name%_%nzp% s where s.!par! = d.!par!)";
   
  		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ¬Õ»Ã¿Õ»≈.œ–Œ»«ŒÿÀ¿ Œÿ»¡ ¿ œ–» ¬€œŒÀÕ≈Õ»» — –»œ“¿. 
    			exit
  		) 

  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "insert into %schema%.%table_name% select * from temp_%table_name%";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists temp_%table_name%";

  echo —ÍËÔÚ ÛÒÔÂ¯ÌÓ Á‡‚Â¯ÂÌ ‰Îˇ Ú‡·ÎËˆ˚: %table_name%
  echo --------------------------------------------------------------------------------

  goto :EOF

