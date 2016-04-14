@echo off
setlocal ENABLEDELAYEDEXPANSION

SET work_dir=%~3

SET host=%~4
SET port=%~5
SET db=%~6
SET PGPASSWORD=%~7

SET schema=%~1
SET par=nzp_pack_ls
SET Path1=%~2

for %%i in ("%work_dir%"\*.unl) do call :proc "%%i"
goto :EOF

:proc

  SET table_name=%~n1

  SET file_type="%work_dir%"\%table_name%.unl

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "create table temp_%table_name% as select * from %schema%.%table_name% limit 1";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "truncate temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% add column additionaly character(1)";

 "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% alter column date_distr type character(20)";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% alter column date_rdistr type character(20)";


  type %file_type% | "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "copy temp_%table_name% from STDIN  with delimiter as '|' null as '' encoding 'WIN-1251'";

  if ERRORLEVEL 1 (

    "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";
   
    echo FIND_ERROR: temp_%table_name% 

    exit

  )

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% drop column additionaly";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "delete from %schema%.%table_name% d where exists (select 1 from temp_%table_name% s where s.%par% = d.%par%)";
   
  if ERRORLEVEL 1 (

    "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";
   
    echo FIND_ERROR: column not found 


    exit

  ) 

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "insert into %schema%.%table_name% select nzp_pack_ls,nzp_pack,prefix_ls,num_ls,g_sum_ls,sum_ls,geton_ls,sum_peni,dat_month,kod_sum,nzp_supp,paysource,id_bill,dat_vvod,dat_uchet,anketa,info_num,inbasket,alg,unl,cast('2014-11-'||trim(date_distr) as timestamp without time zone),cast('2014-11-'||trim(date_rdistr) as timestamp without time zone),nzp_user,incase,isit,erc_code,distr_month,pkod from temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";

  echo complete for table: %schema%.%table_name%

goto :EOF

