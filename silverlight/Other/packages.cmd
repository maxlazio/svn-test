cd ..

REM **Read version from text file
for /f "skip=1" %%i in (Other\Version.txt) do set ver=%%i

REM **Delete old zips..
del /q ..\ovp-sl-%ver%-src.zip
del /q ..\ovp-sl-%ver%-bin.zip

pkzipc -add -attr=-hidden -rec -path -excl=deploy -attributes=-hidden -excl=obj -excl=bin -excl=clientbin -excl=.svn -excl=*Adaptive.sln -excl=*.zip -excl=AdaptiveStreaming -excl=*resharper* -excl=*.user ..\ovp-sl-%ver%-src.zip *.* 

copy /Y *.txt ovpweb 
md ovpweb\docs
copy /Y docs\*.txt ovpweb\docs\ 

pkzipc -add -attr=-hidden -rec -path -excl=deploy -excl=.svn -excl=*.csproj* -excl=obj -excl=Other -excl=*adaptive* -excl=Properties -excl=bin -excl=web.config ..\ovp-sl-%ver%-bin.zip OVPWeb\*.* 

del /q ovpweb\*.txt
rd /q /s ovpweb\docs

pause