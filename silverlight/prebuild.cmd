@echo off
setlocal enableextensions 
setlocal enabledelayedexpansion 
set projectpath=%1
set projectpath=%projectpath:"=%
set configname=%2
set configname=%configname:"=%
set projname=%3
set projname=%projname:"=%


REM **** Automated Versioning *******
type "%projectpath%properties\AssemblyInfo.cs" | find "//NOTE: Automatic Version"
if ERRORLEVEL 1 goto skipVer
  set rev=0
  for /f "skip=1" %%i in (%projectpath%..\Version.txt) do set ver=%%i
  for /f "delims=Revision: " %%i in ('svn.exe info %projectpath%..\Version.txt ^| find "Revision:"') do echo %%i&set rev=%%i
  set ver=%ver%.%rev%
  echo %projname% Version: %ver% 
  
set sfile="%projectpath%properties\AssemblyInfo.cs"

%projectpath%..\ReplaceLine.exe %sfile% AssemblyVersion "[assembly: AssemblyVersion("""%ver%""")]" 
%projectpath%..\ReplaceLine.exe %sfile% AssemblyFileVersion "[assembly: AssemblyFileVersion("""%ver%""")]"  

set ifile="%projectpath%index.html"

if %projname%==OVPWeb (
   %projectpath%..\ReplaceLine.exe %ifile% //auto "  var version = 'v%ver%'; //auto version"
   rem if exist %projectpath%..\..\packages.cmd %projectpath%..\..\packages.cmd
)

:skipVer
REM **** END Automated Versioning ***
