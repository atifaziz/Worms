@echo off
setlocal
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
setlocal
set VERSION_SUFFIX=VersionSuffix=
set COMMIT_UNIX_TIME=
(for /f %%i in ('git log -1 "--format=%%ct"') do set COMMIT_UNIX_TIME=%%i) || goto :pack
if not defined COMMIT_UNIX_TIME echo Unable to determine the time of commit. Git not installed? & exit /b 1
call :dtjs %COMMIT_UNIX_TIME% > "%temp%\dt.js"
for /f %%i in ('cscript //nologo "%temp%\dt.js"') do set COMMIT_TIME=%%i
set COMMIT_HASH=
for /f %%i in ('git log -1 "--format=%%H"') do set COMMIT_HASH=%%i
if not defined COMMIT_HASH echo Unable to determine the commit hash & exit. Git not installed? /b 1
set COMMIT_HASH=%COMMIT_HASH:~0,4%
set VERSION_SUFFIX=%VERSION_SUFFIX%-alpha-%COMMIT_TIME%-%COMMIT_HASH%
:pack
call build && NuGet pack Worms.nuspec -Symbol -Properties "%VERSION_SUFFIX%"
goto :EOF

:dtjs
echo var d = new Date(0);
echo d.setUTCSeconds(%1);
echo WScript.Echo(d.getUTCFullYear() + ('0' + (d.getUTCMonth() + 1)).slice(-2) + ('0' + d.getUTCDate()).slice(-2))
goto :EOF
