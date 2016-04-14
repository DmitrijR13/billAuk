rem @echo off
rem —читывание параметров
SET Version=%1
IF "%Version%"=="" SET Version="Debug"
SET WithCfg=%2
SET Single=%3
SET Rostel=%4

rem ”даление старых сборок
rd /s /q newversion
md newversion

rem  омпил€ци€ проектов
C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild "..\WCF\HOST\AUTOHOST\STCLINE.KP50.Host.sln" /t:Rebuild /p:Configuration=%Version% /p:Platform="Any CPU"
IF ERRORLEVEL 1 goto ERROR
C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild "..\WEB\WebKomplat5\STCLINE.KP50.WebKomplat.sln" /m /t:Rebuild /p:Configuration=%Version% /p:Platform="Any CPU"
IF ERRORLEVEL 1 goto ERROR

rem —борка web
md newversion\web
"C:\Windows\Microsoft.NET\Framework\v2.0.50727\aspnet_compiler.exe" -v none -p ..\WEB\WebKomplat5\ newversion\web\
IF ERRORLEVEL 1 goto ERROR


del /q newversion\web\*.bat
IF "%WithCfg%"=="-c" del /q newversion\web\*.config
del /s /q newversion\web\*.scc
del /s /q newversion\web\*.csproj
del /s /q newversion\web\*.user
del /s /q newversion\web\*.sln
del /s /q newversion\web\*.pdb
IF "%Rostel%" == "rostelek" (
  copy /y editions\rostelek\sprite.png newversion\web\App_Themes\base\images\sprite.png
) 

FOR %%f IN (newversion\web\*config.*) DO IF NOT "%%~xf"==".config" (del /q "%%f")
rd /s /q newversion\web\files
rd /s /q newversion\web\obj
rd /s /q newversion\web\Logs
rd /s /q newversion\web\.nuget
rd /s /q newversion\web\packages
md newversion\web\files

rem —борка хоста
md newversion\host
copy "..\WCF\HOST\AUTOHOST\bin\%Version%\*.exe" newversion\host\
copy "..\WCF\HOST\AUTOHOST\bin\%Version%\*.dll" newversion\host\
IF NOT "%WithCfg%"=="-c" copy "..\WCF\HOST\AUTOHOST\bin\%Version%\*.config" newversion\host\
xcopy "..\WCF\HOST\AUTOHOST\bin\%Version%\Template" newversion\host\Template /s /e /i
xcopy "..\WCF\HOST\AUTOHOST\bin\%Version%\Patches" newversion\host\Patches /s /e /i
xcopy "..\WCF\HOST\AUTOHOST\bin\%Version%\Updater_exe" newversion\host\updater_exe /s /e /i

del /q newversion\host\KP50.WinSrv.exe
del /q newversion\host\KP50.Host.vshost.exe
del /q /s newversion\host\*.pdb
del /s /q newversion\host\*.scc
del /q newversion\host\KP50.Host.vshost.exe.config
del /q newversion\host\STCLINE.KP50.WinServ.exe.config

rem ”паковка
cd newversion
IF "%Rostel%" == "rostelek" (
  del /q ..\rostelek.7z
  ..\7z.exe a -t7z ..\rostelek.7z host\ web\
) ELSE (
    IF NOT "%Single%"=="-s" (
    del /q ..\host.7z
    del /q ..\web.7z
    ..\7z.exe a -t7z ..\host.7z host\
    ..\7z.exe a -t7z ..\web.7z web\
  ) ELSE (
    del /q ..\version.7z
    ..\7z.exe a -t7z ..\version.7z host\ web\
  )
)
cd..
goto END

:ERROR
echo ERROR! Build stopped!

:END
