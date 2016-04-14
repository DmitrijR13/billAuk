@echo off

setlocal ENABLEDELAYEDEXPANSION
chcp 1251>nul
SET work_dir=%~5

SET host=%~6
SET port=5432
SET db=%~8
SET PGPASSWORD=%~9

SET kod_uk=%~1
SET dat_calc=%~2
SET year=%~3
SET month=%~4
SET Path1=%~7


for %%i in ("%work_dir%"\*.txt) do call :proc "%%i"
goto :EOF

:proc
  
  SET file_name=%~n1

  if "%file_name%" == "InfoDescript" ( 
    exit /B
  )

  if "%file_name%" == "InfoSocProtection" ( 
    SET tab_name=sz
  )

  if "%file_name%" == "ChargExpenseServ" ( 
    	
	SET tab_name=charge

  	"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "insert into public.tab_nul (kod_uk,tab_name,dat_calc,dat_load) values (%kod_uk%,'!tab_name!','%dat_calc%',current_date);"

  	type "%work_dir%"\"%file_name%".txt | "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "copy public.!tab_name!_%kod_uk%_%year%%month% (nzp_load,dat_month,kod_uk,pkod,service,measure,ordering,nzp_serv,nzp_serv_base,serv_group,tarif,rashod,rashod_odn,rashod_ipu,rashod_norm,rashod_dom,rashod_dom_kv,rashod_dom_ipu,rashod_dom_norm,rashod_dom_arend,rashod_dom_lift,rashod_dom_odn,rashod_dom_odpu,rsum_tarif,sum_tarif,sum_nedop,rashod_nedop,days_nedop,reval,real_charge,sum_charge,sum_money,sum_outsaldo,sum_insaldo,sum_tarif_odn,sum_insaldo_odn,sum_outsaldo_odn,reval_odn,real_charge_odn,sum_charge_odn,sum_money_odn,sum_tarif_sn,k_odn_ipu,k_odn_norm) from STDIN with delimiter as '|' null as '' encoding 'WIN-1251'"

    	if ERRORLEVEL 1 (
   
      		echo ERROR 
                exit
  	)

  	"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "insert into public.tab_part (kod_uk,tab_name,dat_calc,dat_load) values (%kod_uk%,'!tab_name!','%dat_calc%',current_date);"

 	 exit /B

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

"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "insert into public.tab_nul (kod_uk,tab_name,dat_calc,dat_load) values (%kod_uk%,'%tab_name%','%dat_calc%',current_date);"

type "%work_dir%"\"%file_name%".txt | "%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "copy public.%tab_name%_%kod_uk%_%year%%month% from STDIN with delimiter as '|' null as '' encoding 'WIN-1251'";

if ERRORLEVEL 1 (
   
    echo ERROR 

    exit

  )

"%Path1%" -h %host% -p %port% -U postgres -w -d %db% -c "insert into public.tab_part (kod_uk,tab_name,dat_calc,dat_load) values (%kod_uk%,'%tab_name%','%dat_calc%',current_date);"

goto :EOF





