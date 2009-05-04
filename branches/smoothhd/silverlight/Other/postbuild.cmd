@echo off
setlocal enableextensions 
setlocal enabledelayedexpansion 
set projectpath=%1
set projectpath=%projectpath:"=%
set configname=%2
set configname=%configname:"=%
set projname=%3
set projname=%projname:"=%

svn info "%projectpath%" | find "Revision"

if %errorlevel%==0 (
   REM Handle deploy folder
   if %projname%==OVPWeb (   
      rd /q /s "%projectpath%\bin"
      rd /q /s "%projectpath%\obj"
      rd /q /s "%projectpath%\deploy"
      mkdir "%projectpath%\deploy\plugins"
      
      svn export "%projectpath%\themes\" "%projectpath%\deploy\themes"
      svn export "%projectpath%\content\" "%projectpath%\deploy\content"
      
      copy /y "%projectpath%\plugins\*.dll" "%projectpath%\deploy\plugins\"
      copy /y "%projectpath%\plugins\*.xap" "%projectpath%\deploy\plugins\"
      copy /y "%projectpath%\*.html"  "%projectpath%\deploy\"
      copy /y "%projectpath%\*.xml"  "%projectpath%\deploy\"
      copy /y "%projectpath%\*.xap"  "%projectpath%\deploy\"
   )
)