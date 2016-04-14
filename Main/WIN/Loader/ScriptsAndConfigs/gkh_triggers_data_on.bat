@echo off
setlocal ENABLEDELAYEDEXPANSION
chcp 1251>nul
SET work_dir=%~3

SET host=%~4
SET port=%~5
SET db=%~6
SET PGPASSWORD=%~7
SET Path1=%~2
SET schema=%~1

"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.kart enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.counters enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.counters_dom enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.counters_group enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.gil_periods enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.nedop_kvar enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.prm_1 enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.prm_2 enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.prm_3 enable trigger all";
"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "alter table %schema%.tarif enable trigger all";

