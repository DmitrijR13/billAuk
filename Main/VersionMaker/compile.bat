echo off
rd /s /q newversion
md newversion

rem —борка web
md newversion\web
"C:\Windows\Microsoft.NET\Framework\v2.0.50727\aspnet_compiler.exe" -v none -p ..\WEB\WebKomplat5\ newversion\web\

del /q newversion\web\*.bat
IF "%1"=="-c" del /q newversion\web\*.config
del /s /q newversion\web\*.scc
del /s /q newversion\web\*.csproj
del /s /q newversion\web\*.user
del /s /q newversion\web\*.sln
del /s /q newversion\web\*.pdb
IF "%3" == "rostelek" (
  copy /y editions\rostelek\sprite.png newversion\web\App_Themes\base\images\sprite.png
) 

FOR %%f IN (newversion\web\*config.*) DO IF NOT "%%~xf"==".config" (del /q "%%f")
rd /s /q newversion\web\files
rd /s /q newversion\web\obj
md newversion\web\files

rem —борка хоста
md newversion\host
copy ..\WCF\HOST\AUTOHOST\bin\Release\*.exe newversion\host\
copy ..\WCF\HOST\AUTOHOST\bin\Release\*.dll newversion\host\
IF NOT "%1"=="-c" copy ..\WCF\HOST\AUTOHOST\bin\Release\*.config newversion\host\
xcopy ..\WCF\HOST\AUTOHOST\bin\Release\Template newversion\host\Template  /s /e /i
xcopy ..\WCF\HOST\AUTOHOST\bin\Release\Patches newversion\host\Patches  /s /e /i
xcopy ..\WCF\HOST\AUTOHOST\bin\Release\Updater_exe newversion\host\updater_exe  /s /e /i

del /q newversion\host\KP50.WinSrv.exe
del /q newversion\host\KP50.Host.vshost.exe
del /q /s newversion\host\*.pdb
del /s /q newversion\host\*.scc
del /q newversion\host\KP50.Host.vshost.exe.config
del /q newversion\host\STCLINE.KP50.WinServ.exe.config

rem ”паковка
cd newversion
IF "%3" == "rostelek" (
  del /q ..\rostelek.7z
  ..\7z.exe a -t7z ..\rostelek.7z host\ web\
) ELSE (
    IF NOT "%2"=="-s" (
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