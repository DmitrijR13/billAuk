@ECHO OFF
set path=%path%;c:\Program Files\pgAdmin III\1.18\;

if "%~1"=="" GOTO err
if "%~2"=="" GOTO err
goto run
:err
  echo Usage: copy_schema_structure.cmd old_schema new_schema
  exit /b -1
:run
set old_schema=%1
set new_schema=%2

set tmp_file=fsmr_fin_13.sql
set host=192.168.179.143
set port=5432
set user=postgres
set dbname=websmr
SET PGPASSWORD=postgres

pg_dump --host %host% --port %port% --username %user% --role %role% --format plain --schema-only --file %tmp_file% --schema %old_schema% %dbname%
python replace_schema.py %tmp_file% %old_schema% %new_schema%
psql --host %host% --port %port% --username %user% --file %tmp_file% -d %dbname%