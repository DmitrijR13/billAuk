@echo off

setlocal ENABLEDELAYEDEXPANSION

chcp 1251>nul

SET kod_uk=%~1
SET dat_calc=%~2
SET pg_dir=%~3
SET work_dir=%~4
SET host=%~5
SET port=%~6
SET db=%~7
SET password=%~8
SET nzp_load=%~9

SET year=%dat_calc:~6,4%
SET month=%dat_calc:~3,2%

for %%i in ("%work_dir%"\*.txt) do call :proc "%%i"

goto :EOF

:proc
  
  SET file_name=%~n1

  echo --------------------------------------------------------------------------------
  echo Выбран файл: %file_name%

  if "%file_name%" == "ChargExpenseServ" ( 
    	
	SET tab_name=charge

  	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "insert into tab_nul (kod_uk,tab_name,dat_calc,dat_load) values (%kod_uk%,'!tab_name!','%dat_calc%',current_date);"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists temp_!tab_name!_%nzp_load% ;"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "create table temp_!tab_name!_%nzp_load% as select * from !tab_name! limit 1;"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "truncate temp_!tab_name!_%nzp_load%;"

  	type "%work_dir%"\"%file_name%".txt | "%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "copy temp_!tab_name!_%nzp_load% (nzp_load,dat_month,kod_uk,pkod,service,measure,ordering,nzp_serv,nzp_serv_base,serv_group,tarif,rashod,rashod_odn,rashod_ipu,rashod_norm,rashod_dom,rashod_dom_kv,rashod_dom_ipu,rashod_dom_norm,rashod_dom_arend,rashod_dom_lift,rashod_dom_odn,rashod_dom_odpu,rsum_tarif,sum_tarif,sum_nedop,rashod_nedop,days_nedop,reval,real_charge,sum_charge,sum_money,sum_outsaldo,sum_insaldo,sum_tarif_odn,sum_insaldo_odn,sum_outsaldo_odn,reval_odn,real_charge_odn,sum_charge_odn,sum_money_odn,sum_tarif_sn,k_odn_ipu,k_odn_norm,supplier,c_okaz,c_nedop) from STDIN with delimiter as '|' null as '' encoding 'WIN-1251'"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "update temp_!tab_name!_%nzp_load% set nzp_load=%nzp_load%;"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "insert into !tab_name!_%kod_uk%_%year%%month% (nzp_load,dat_month,kod_uk,pkod,service,measure,ordering,nzp_serv,nzp_serv_base,serv_group,tarif,rashod,rashod_odn,rashod_ipu,rashod_norm,rashod_dom,rashod_dom_kv,rashod_dom_ipu,rashod_dom_norm,rashod_dom_arend,rashod_dom_lift,rashod_dom_odn,rashod_dom_odpu,rsum_tarif,sum_tarif,sum_nedop,rashod_nedop,days_nedop,reval,real_charge,sum_charge,sum_money,sum_outsaldo,sum_insaldo,sum_tarif_odn,sum_insaldo_odn,sum_outsaldo_odn,reval_odn,real_charge_odn,sum_charge_odn,sum_money_odn,sum_tarif_sn,k_odn_ipu,k_odn_norm,supplier,c_okaz,c_nedop) select nzp_load,dat_month,kod_uk,pkod,service,measure,ordering,nzp_serv,nzp_serv_base,serv_group,tarif,rashod,rashod_odn,rashod_ipu,rashod_norm,rashod_dom,rashod_dom_kv,rashod_dom_ipu,rashod_dom_norm,rashod_dom_arend,rashod_dom_lift,rashod_dom_odn,rashod_dom_odpu,rsum_tarif,sum_tarif,sum_nedop,rashod_nedop,days_nedop,reval,real_charge,sum_charge,sum_money,sum_outsaldo,sum_insaldo,sum_tarif_odn,sum_insaldo_odn,sum_outsaldo_odn,reval_odn,real_charge_odn,sum_charge_odn,sum_money_odn,sum_tarif_sn,k_odn_ipu,k_odn_norm,supplier,c_okaz,c_nedop from temp_!tab_name!_%nzp_load%;"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists temp_!tab_name!_%nzp_load% ;"	

    		if ERRORLEVEL 1 (
      			echo ERROR
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists billinfo_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists charge_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists counters_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists parameters_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists payments_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists sz_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists temp_!tab_name!_%nzp_load% ;"	
                	exit
  		)
  	
	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "insert into public.tab_part (kod_uk,tab_name,dat_calc,dat_load) values (%kod_uk%,'!tab_name!','%dat_calc%',current_date);"
 	
	exit /B
  )

  if "%file_name%" == "InfoDescript" ( 
    exit /B
  )

  if "%file_name%" == "InfoSocProtection" ( 
    SET tab_name=sz
  )

  if "%file_name%" == "Payment" ( 
    SET tab_name=payments
  )

  if "%file_name%" == "PaymentDetails" ( 
    SET tab_name=billinfo
  )

  if "%file_name%" == "Counters" ( 
    SET tab_name=counters
  )

  if "%file_name%" == "CharacterGilFond" ( 
    SET tab_name=parameters
  )                                                                              

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "insert into tab_nul (kod_uk,tab_name,dat_calc,dat_load) values (%kod_uk%,'%tab_name%','%dat_calc%',current_date);"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists temp_!tab_name!_%nzp_load% ;"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "create table temp_!tab_name!_%nzp_load% as select * from !tab_name! limit 1;"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "truncate temp_!tab_name!_%nzp_load%;"

		if ERRORLEVEL 1 (
    			echo ERROR 
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists billinfo_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists charge_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists counters_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists parameters_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists payments_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists sz_%kod_uk%_%year%%month% ;"
       			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists temp_!tab_name!_%nzp_load% ;"
    			exit
  		)

	type "%work_dir%"\"%file_name%".txt | "%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "copy temp_!tab_name!_%nzp_load% from STDIN with delimiter as '|' null as '' encoding 'WIN-1251'";

		if ERRORLEVEL 1 (
    			echo ERROR 
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists billinfo_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists charge_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists counters_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists parameters_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists payments_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists sz_%kod_uk%_%year%%month% ;"
       			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists temp_!tab_name!_%nzp_load% ;"
    			exit
  		)

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "update temp_!tab_name!_%nzp_load% set nzp_load=%nzp_load%;"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "insert into !tab_name!_%kod_uk%_%year%%month% select * from temp_!tab_name!_%nzp_load%;"

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists temp_!tab_name!_%nzp_load% ;"	

		if ERRORLEVEL 1 (
    			echo ERROR 
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists billinfo_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists charge_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists counters_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists parameters_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists payments_%kod_uk%_%year%%month% ;"
			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists sz_%kod_uk%_%year%%month% ;"
       			"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "drop table if exists temp_!tab_name!_%nzp_load% ;"
    			exit
  		)

	"%pg_dir%" -h %host% -p %port% -U %password% -w -d %db% -c "insert into tab_part (kod_uk,tab_name,dat_calc,dat_load) values (%kod_uk%,'%tab_name%','%dat_calc%',current_date);"

  echo Скрипт выполнен для файла: %file_name%
  echo --------------------------------------------------------------------------------

  goto :EOF





