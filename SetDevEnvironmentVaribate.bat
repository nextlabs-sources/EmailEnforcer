@echo off

echo 请以管理员身份运行

rem 需要设置的环境变量: WORKSPACE, NLBUILDROOT, BUID_OUTPUT, BUILD_NUMBER

set BAT_DIR=%~dp0
set BAT_DIR=%BAT_DIR:~0,-1%

setx /m WORKSPACE %BAT_DIR%
setx /m NLBUILDROOT %BAT_DIR%
setx /m BUID_OUTPUT %BAT_DIR%\output
setx /m BUILD_NUMBER 100

echo WORKSPACE=%WORKSPACE%
echo NLBUILDROOT=%NLBUILDROOT%
echo BUID_OUTPUT=%BUID_OUTPUT%
echo BUILD_NUMBER=%BUILD_NUMBER%

echo -----------------------
echo 请在新打开的dev.exe中编译
echo 命令提示符窗口可能无法退出，请手动关闭
echo -----------------------

rem set VC_VARS_ALL_BAT=%VS140COMNTOOLS%
rem set VC_VARS_ALL_BAT=%VC_VARS_ALL_BAT:Tools=IDE%devenv.exe

rem mkdir "%BAT_DIR%\.git\hooks"
rem copy /y "%BAT_DIR%\pre-commit" "%BAT_DIR%\.git\hooks\pre-commit"

pause