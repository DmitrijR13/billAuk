@echo off
setlocal ENABLEDELAYEDEXPANSION

SET work_dir=%~4

SET host=%~6
SET port=%~7
SET db=%~8
SET PGPASSWORD=%~9
SET Path1=%~5

SET schema=%~1
SET month=%~2
SET year=%~3

for %%i in ("%work_dir%"\*.unl) do call :proc "%%i"
goto :EOF

:proc

  SET table_name=%~n1

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

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "delete from %schema%.%table_name% where month_=%month% and year_=%year%";
   
  if ERRORLEVEL 1 (

    "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";
   
    echo FIND_ERROR: column not found 

    exit

  ) 

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "insert into %schema%.%table_name% select * from temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";

  echo complete for table: %schema%.%table_name%

goto :EOF

