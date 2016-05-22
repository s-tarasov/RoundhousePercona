@echo off

set drive=%~dp0..\..\StrawberryPerl64.5.22.2.1\Tools
set drivep=%drive%
if #%drive:~-1%# == #\# set drivep=%drive:~0,-1%

set PATH=%drivep%\perl\site\bin;%drivep%\perl\bin;%drivep%\c\bin;%PATH%
rem env variables
set TERM=
rem avoid collisions with other perl stuff on your system
set PERL_JSON_BACKEND=
set PERL_YAML_BACKEND=
set PERL5LIB=
set PERL5OPT=
set PERL_MM_OPT=
set PERL_MB_OPT=
if #%1# == ## exit 1

"%drivep%\perl\bin\perl.exe" pt-online-schema-change %*
EXIT /B %errorlevel%