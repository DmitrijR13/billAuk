@echo off
setlocal ENABLEDELAYEDEXPANSION
chcp 1251>nul
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


"%Path1%"

:proc

  SET table_name=%~n1

  SET file_type=%work_dir%\%table_name%.unl


echo Âûáğàíà ñõåìà: %schema%, âûáğàíà òàáëèöà: %table_name%
echo Âûïîëíåíû ñëåäóşùèå îïåğàöèè:


  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "create table temp_%table_name% as select * from %schema%.%table_name% limit 1";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "truncate temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% add column additionaly character(1)";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% alter column date_distr type character(20)";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% alter column date_rdistr type character(20)";


  type %file_type% | "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "copy temp_%table_name% from STDIN  with delimiter as '|' null as '' encoding 'WIN-1251'";

  if ERRORLEVEL 1 (

    "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";
   
    echo ÂÍÈÌÀÍÈÅ.ÏĞÎÈÇÎØËÀ ÎØÈÁÊÀ ÏĞÈ ÇÀÃĞÓÇÊÅ ÄÀÍÍÛÕ: temp_%table_name% 

    exit

  )

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% drop column additionaly";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "update temp_%table_name% set date_distr=(dat_uchet||' '||reverse(substring(reverse(temp_%table_name%.date_distr::varchar) from 1 for 8))) where date_distr is not null";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "update temp_%table_name% set date_rdistr=(dat_uchet||' '||reverse(substring(reverse(temp_%table_name%.date_rdistr::varchar) from 1 for 8))) where date_rdistr is not null";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%_1";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "create table temp_%table_name%_1 as select nzp_pack_ls, date_distr, date_rdistr from temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% alter column date_distr type timestamp without time zone using null";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table temp_%table_name% alter column date_rdistr type timestamp without time zone using null";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "update temp_%table_name% p SET (date_distr,date_rdistr) = (cast(trim(p1.date_distr) as timestamp without time zone),cast(trim(p1.date_rdistr) as timestamp without time zone)) from temp_%table_name%_1 p1 where p.nzp_pack_ls=p1.nzp_pack_ls";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%_1";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "delete from %schema%.%table_name% d where exists (select 1 from temp_%table_name% s where s.%par% = d.%par%)";
   
  if ERRORLEVEL 1 (

    "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";

    "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%_1";
    
    echo ÂÍÈÌÀÍÈÅ.ÏĞÎÈÇÎØËÀ ÎØÈÁÊÀ ÏĞÈ ÇÀÃĞÓÇÊÅ ÄÀÍÍÛÕ.ÎÁĞÀÒÈÒÅÑÜ Ê ĞÀÇĞÀÁÎÒ×ÈÊÓ 

    pause

    exit

  ) 

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "insert into %schema%.%table_name% select * from temp_%table_name%";

  "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "drop table if exists temp_%table_name%";

  echo complete for table: %schema%.%table_name%

goto :EOF

