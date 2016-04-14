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
SET month=%dat_calc:~3,2%

for %%i in ("%work_dir%"\*.unl) do call :proc "%%i"

goto :EOF

:proc

  SET table_name=%~n1

  SET file_type="%work_dir%"\%table_name%.unl

  echo --------------------------------------------------------------------------------
  echo Выбрана таблица: %table_name%
  echo Выполнены следующие операции:

  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists temp_%table_name%";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "create table temp_%table_name% as select * from %schema%.%table_name% limit 1";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "truncate temp_%table_name%";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% add column additionaly character(1)";

  		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ВНИМАНИЕ.ПРОИЗОШЛА ОШИБКА ПРИ ВЫПОЛНЕНИИ СКРИПТА.
    			exit
    		)
	
  	type %file_type% | "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "copy t_%table_name%_%nzp% from STDIN  with delimiter as '|' null as '' encoding 'WIN-1251'";

  		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ВНИМАНИЕ.ПРОИЗОШЛА ОШИБКА ПРИ ВЫПОЛНЕНИИ СКРИПТА. 
    			exit
  		)

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% drop column additionaly";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "delete from %schema%.%table_name% where month_=%month% and year_=%year%";

		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ВНИМАНИЕ.ПРОИЗОШЛА ОШИБКА ПРИ ВЫПОЛНЕНИИ СКРИПТА. 
    			exit
  		)

  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "insert into %schema%.%table_name% select * from temp_%table_name%";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists temp_%table_name%";

  echo Скрипт успешно завершен для таблицы: %table_name%  
  echo --------------------------------------------------------------------------------

  goto :EOF

