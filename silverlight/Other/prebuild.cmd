rem @echo off
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
  for /f "skip=1" %%i in (%projectpath%..\Other\Version.txt) do set ver=%%i&echo Update to Version.txt: v%%i
  for /f "delims=Revision: " %%i in ('svn.exe info ^"%projectpath%..\Other\Version.txt^" ^| find "Revision:"') do echo Subversion Revision: %%i&set rev=%%i
  set ver=%ver%.%rev%
  echo %projname% Version: %ver% 
  
set sfile="%projectpath%properties\AssemblyInfo.cs"

attrib -r "%sfile%"
echo "%projectpath%..\Other\ReplaceLine.exe" "%sfile%" AssemblyVersion "[assembly: AssemblyVersion("""%ver%""")]" 
"%projectpath%..\Other\ReplaceLine.exe" "%sfile%" AssemblyVersion "[assembly: AssemblyVersion("""%ver%""")]" 
"%projectpath%..\Other\ReplaceLine.exe" "%sfile%" AssemblyFileVersion "[assembly: AssemblyFileVersion("""%ver%""")]"  

set ifile="%projectpath%index.html"

if %projname%==OVPWeb (
   attrib -r "%ifile%"
   "%projectpath%..\Other\ReplaceLine.exe" "%ifile%" //auto "  var version = 'v%ver%'; //auto version"
   rem if exist "%projectpath%..\..\packages.cmd" "%projectpath%..\..\packages.cmd"
)

:skipVer
REM **** END Automated Versioning ***
