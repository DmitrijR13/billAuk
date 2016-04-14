@echo off
setlocal ENABLEDELAYEDEXPANSION

SET work_dir=%~3

SET host=%~4
SET port=%~5
SET db=%~6
SET PGPASSWORD=%~7
SET Path1=%~2

SET schema=%~1

for %%i in ("%work_dir%"\*.unl) do call :proc "%%i"
goto :EOF

:proc
  
  SET table_name=%~n1

  if "%table_name%" == "counters_ord" ( 
    SET par=nzp_ck
  )

  if "%table_name%" == "charge_01" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_02" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_02" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_03" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_04" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_05" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_06" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_07" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_08" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_09" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_10" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_11" ( 
    SET par=nzp_charge
  )

  if "%table_name%" == "charge_12" ( 
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
                                  

  SET file_type="%work_dir%"\%table_name%.unl

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "create table temp_%table_name% as select * from %schema%.%table_name% limit 1";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "truncate temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% add column additionaly character(1)";

  type %file_type% | "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "copy temp_%table_name% from STDIN  with delimiter as '|' null as '' encoding 'WIN-1251'";

  if ERRORLEVEL 1 (

    "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";
   
    echo FIND_ERROR: temp_%table_name% 

    exit

  )

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% drop column additionaly";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "delete from %schema%.%table_name% d where exists (select 1 from temp_%table_name% s where s.!par! = d.!par!)";
   
  if ERRORLEVEL 1 (

    "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";
   

    echo FIND_ERROR: column not found 


    exit

  ) 

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "insert into %schema%.%table_name% select * from temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";

  echo complete for table: %schema%.%table_name%

goto :EOF

