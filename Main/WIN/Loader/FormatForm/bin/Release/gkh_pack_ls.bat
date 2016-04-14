@echo off
setlocal ENABLEDELAYEDEXPANSION

chcp 1251>nul

SET par=nzp_pack_ls
           
SET schema=%~1
SET pg_dir=%~2
SET work_dir=%~3
SET host=%~4
SET port=%~5
SET db=%~6
SET PGPASSWORD=%~7
SET nzp=%~8

for %%i in ("%work_dir%"\*.unl) do call :proc "%%i"

goto :EOF

:proc

  SET table_name=%~n1

  SET file_type="%work_dir%"\%table_name%.unl

  echo --------------------------------------------------------------------------------
  echo Âûáğàíà òàáëèöà: %table_name%
  echo Âûïîëíåíû ñëåäóşùèå îïåğàöèè:

  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists temp_%table_name%";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "create table temp_%table_name% as select * from %schema%.%table_name% limit 1";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "truncate temp_%table_name%";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% add column additionaly character(1)";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% alter column date_distr type character(20)";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% alter column date_rdistr type character(20)";

  	 	if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ÂÍÈÌÀÍÈÅ.ÏĞÎÈÇÎØËÀ ÎØÈÁÊÀ ÏĞÈ ÂÛÏÎËÍÅÍÈÈ ÑÊĞÈÏÒÀ.
    			exit
    		)
	
  	type %file_type% | "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "copy t_%table_name%_%nzp% from STDIN  with delimiter as '|' null as '' encoding 'WIN-1251'";

  		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ÂÍÈÌÀÍÈÅ.ÏĞÎÈÇÎØËÀ ÎØÈÁÊÀ ÏĞÈ ÂÛÏÎËÍÅÍÈÈ ÑÊĞÈÏÒÀ. 
    			exit
  		)

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% drop column additionaly";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "update t_%table_name%_%nzp% set date_distr=(dat_uchet||' '||reverse(substring(reverse(t_%table_name%_%nzp%.date_distr::varchar) from 1 for 8))) where date_distr is not null";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "update t_%table_name%_%nzp% set date_rdistr=(dat_uchet||' '||reverse(substring(reverse(t_%table_name%_%nzp%.date_rdistr::varchar) from 1 for 8))) where date_rdistr is not null";

		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
    			echo ÂÍÈÌÀÍÈÅ.ÏĞÎÈÇÎØËÀ ÎØÈÁÊÀ ÏĞÈ ÂÛÏÎËÍÅÍÈÈ ÑÊĞÈÏÒÀ. 
    			exit
  		)  	

	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%_buf";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "create table t_%table_name%_%nzp%_buf as select nzp_pack_ls, date_distr, date_rdistr from t_%table_name%_%nzp%";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% alter column date_distr type timestamp without time zone using null";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "alter table t_%table_name%_%nzp% alter column date_rdistr type timestamp without time zone using null";

		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
                 	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%_buf";
    			echo ÂÍÈÌÀÍÈÅ.ÏĞÎÈÇÎØËÀ ÎØÈÁÊÀ ÏĞÈ ÂÛÏÎËÍÅÍÈÈ ÑÊĞÈÏÒÀ.
    			exit
    		)

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "update t_%table_name%_%nzp% p SET (date_distr,date_rdistr) = (cast(trim(p1.date_distr) as timestamp without time zone),cast(trim(p1.date_rdistr) as timestamp without time zone)) from t_%table_name%_%nzp%_buf p1 where p.nzp_pack_ls=p1.nzp_pack_ls";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%_buf";

  	"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "delete from %schema%.%table_name% d where exists (select 1 from t_%table_name%_%nzp% s where s.%par% = d.%par%)";
   
  		if ERRORLEVEL 1 (
    			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%";
			"%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists t_%table_name%_%nzp%_buf";
    			echo ÂÍÈÌÀÍÈÅ.ÏĞÎÈÇÎØËÀ ÎØÈÁÊÀ ÏĞÈ ÂÛÏÎËÍÅÍÈÈ ÑÊĞÈÏÒÀ. 
    			exit
  		)

  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "insert into %schema%.%table_name% select * from temp_%table_name%";
  	rem "%pg_dir%" -h %host% -p %port% -U %PGPASSWORD% -w -d %db% -c "drop table if exists temp_%table_name%";

  echo Ñêğèïò óñïåøíî çàâåğøåí äëÿ òàáëèöû: %table_name%
  echo --------------------------------------------------------------------------------

  goto :EOF

