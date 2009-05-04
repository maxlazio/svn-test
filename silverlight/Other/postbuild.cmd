@echo on
setlocal enableextensions 
setlocal enabledelayedexpansion 
set projectpath=%1
set projectpath=%projectpath:"=%
set configname=%2
set configname=%configname:"=%
set projname=%3
set projname=%projname:"=%


   REM Handle deploy folder
   if %projname%==OVPWeb (   
      rd /q /s "%projectpath%\deploy"
      mkdir "%projectpath%\deploy\plugins"
      
      REM if svn installed, use export to avoid the .svn folders
      set rev=NONE
      for /f "delims=Revision: " %%i in ('svn.exe info "%projectpath%\%projname%.csproj" ^| find "Revision:"') do echo SVN Rev: %%i&set rev=%%i
      if NOT '!rev!'=='NONE' (
         svn export %projectpath%\themes\ "%projectpath%\deploy\themes"
         svn export %projectpath%\content\ "%projectpath%\deploy\content"
      ) else (
         echo SVN not installed.
         copy /y "%projectpath%\themes\" "%projectpath%\deploy\themes"
         copy /y "%projectpath%\content\" "%projectpath%\deploy\content"
      )
      
      copy /y "%projectpath%\plugins\*.dll" "%projectpath%\deploy\plugins\"
      copy /y "%projectpath%\plugins\*.xap" "%projectpath%\deploy\plugins\"
      copy /y "%projectpath%\*.html"  "%projectpath%\deploy\"
      copy /y "%projectpath%\*.xml"  "%projectpath%\deploy\"
      copy /y "%projectpath%\*.xap"  "%projectpath%\deploy\"
      
      rem rd /q /s "%projectpath%\obj"
      rem rd /q /s "%projectpath%\bin"
   )
