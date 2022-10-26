echo off
REM get deploy script path
set currentDir=%~dp0
set deployScript=%currentDir%deploy.ps1
echo deploy script file is: %deployScript%

REM get exchange install path	
for /f "delims='	' tokens=2*" %%i in ('reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\ExchangeServer\v15\Setup" /v "MsiInstallPath"') do SET "exchangeInstallPath=%%i"
echo Exchange Install Path is: %exchangeInstallPath%

REM set exchange script file
set exchangeScript=%exchangeInstallPath%bin\RemoteExchange.ps1
echo Exchange Script file is: %exchangeScript%

echo begin Uninstall Agent...
powershell.exe -command ".  '%exchangeScript%'; Connect-ExchangeServer -auto -ClientApplication:ManagementShell; . '%deployScript%' undeploy"

pause